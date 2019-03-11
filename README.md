# sample-cdc-sf-app
Sample Service Fabric app that integrates with CDCTentacles.

SF uses RC Collection. This data is pushed to Azure Event hubs.
Same app can read data from Azure Event hubs and write to RC Collection
with configuration change.

This app can be used to show disaster recovery scenario.

Todo:
* Done. Use VotingApp that uses RC.
* Done. Integrate with CDC.
* Done. Parameterize the main-or-backup app.
* Done. Parameterize the configuration around azure-event-hubs.
* Linux not working. so drop this : Deploy https://hub.docker.com/r/microsoft/service-fabric-reverse-proxy/tags on linux for reverse proxy.
* Handle the data migration story to main cluster after main cluster is recovered.
* Run 2 apps in same cluster syncing data from each other as POC.
