# sample-cdc-sf-app
Sample Service Fabric app that integrates with CDCTentacles.

SF uses RC Collection. This data is pushed to Azure Event hubs.
Same app can read data from Azure Event hubs and write to RC Collection
with configuration change.

This app can be used to show disaster recovery scenario.

Todo:
* Use VotingApp that uses RC.
* Integrate with CDC.
* Parameterize the main-or-backup app.
* Parameterize the configuration around azure-event-hubs.
* Deploy https://hub.docker.com/r/microsoft/service-fabric-reverse-proxy/tags on linux for reverse proxy.