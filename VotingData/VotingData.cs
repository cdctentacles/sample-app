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

    using CDC.EventCollector;
    using CDC.AzureEventCollector;
    using EventHubsConsumer;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using ProducerPlugin;

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
                                var cdcIsSourceCluster = configurationPackage.Settings
                                    .Sections["CDCConfigSection"].Parameters["CDC_SourceCluster"].Value;
                                var isSouceCluster = true;
                                if (!bool.TryParse(cdcIsSourceCluster, out isSouceCluster))
                                {
                                    throw new InvalidDataException($"CDC Parameter : CDC_SourceCluster {cdcIsSourceCluster} is not parsable as bool.");
                                }

                                if (String.IsNullOrEmpty(cDCAzureEventHubsConnectionString) ||
                                    String.IsNullOrEmpty(cDCEventHubName))
                                {
                                    throw new InvalidDataException($"EventHubs ConnectionString and HubsName is empty : {cDCAzureEventHubsConnectionString} {cDCEventHubName}");
                                }

                                this.SetCDCEventCollector(this.StateManager, this.Partition.PartitionInfo.Id,
                                    cDCAzureEventHubsConnectionString, cDCEventHubName, isSouceCluster);

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

        private void SetCDCEventCollector(IReliableStateManager stateManager, Guid partitionId,
            string eventHubConnectionString, string eventHubName, bool isSouceCluster)
        {
            ISourceFactory sourceFactory = null;
            var healthStore = new ServiceFabricHealthStore();
            List<IPersistentCollector> persistentCollectors = null;
            var messageConverter = new JsonMessageConverter(new List<Type>());

            if (isSouceCluster)
            {
                sourceFactory = new ServiceFabricSourceFactory(stateManager, partitionId, "VotingDataSource", messageConverter);
                var persistentCollector = new Collector(partitionId,
                    eventHubConnectionString,
                    eventHubName,
                    5,
                    healthStore,
                    TimeSpan.FromSeconds(5));

                persistentCollectors = new List<IPersistentCollector>() { persistentCollector };
            }
            else
            {
                var eventHubsConfiguration = new EventHubsConfiguration(eventHubConnectionString, eventHubName, "", "", "");
                sourceFactory = new CollectorEventsProducerFactory(eventHubsConfiguration);
                var persistentCollector = new ServiceFabricPersistentCollector(partitionId, stateManager,
                    healthStore, messageConverter);
                persistentCollectors = new List<IPersistentCollector>() { persistentCollector };
            }

            var conf = new Configuration(sourceFactory, persistentCollectors).SetHealthStore(healthStore);
            CDCCollector.NewCollectorConfiguration(conf);
        }
    }
}