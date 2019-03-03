// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace VotingData
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using ProducerPlugin;
    using CDC.EventCollector;
    using CDC.AzureEventCollector;

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class VotingData : StatefulService
    {
        public VotingData(StatefulServiceContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[]
            {
                new ServiceReplicaListener(
                    serviceContext =>
                        new KestrelCommunicationListener(
                            serviceContext,
                            (url, listener) =>
                            {
                                ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                                var configurationPackage = this.Context.CodePackageActivationContext.
                                    GetConfigurationPackageObject("Config");
                                var cDCAzureEventHubsConnectionString = configurationPackage.Settings
                                    .Sections["CDCConfigSection"].Parameters["CDC_AzureEventHubsConnectionString"].Value;
                                var cDCEventHubName = configurationPackage.Settings
                                    .Sections["CDCConfigSection"].Parameters["CDC_EventHubName"].Value;

                                if (String.IsNullOrEmpty(cDCAzureEventHubsConnectionString) ||
                                    String.IsNullOrEmpty(cDCEventHubName))
                                {
                                    throw new InvalidDataException($"EventHubs ConnectionString and HubsName is empty : {cDCAzureEventHubsConnectionString} {cDCEventHubName}");
                                }

                                this.SetCDCEventCollector(this.StateManager, this.Partition.PartitionInfo.Id,
                                    cDCAzureEventHubsConnectionString, cDCEventHubName);

                                return new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatefulServiceContext>(serviceContext)
                                            .AddSingleton<IReliableStateManager>(this.StateManager))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseApplicationInsights()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                                    .UseUrls(url)
                                    .Build();
                            }))
            };
        }

        private void SetCDCEventCollector(IReliableStateManager stateManager, Guid partitionId, string eventHubConnectionString, string eventHubName)
        {
            var sfEventSource = new ServiceFabricSourceFactory(stateManager, partitionId, "VotingDataSource");
            var healthStore = new ServiceFabricHealthStore();
            var persistentCollector = new Collector(partitionId,
                eventHubConnectionString,
                eventHubName,
                5,
                healthStore,
                TimeSpan.FromSeconds(5));

            var persistentCollectors = new List<IPersistentCollector>() { persistentCollector };
            var conf = new Configuration(sfEventSource, persistentCollectors).SetHealthStore(healthStore);
            CDCCollector.NewCollectorConfiguration(conf);
        }
    }
}