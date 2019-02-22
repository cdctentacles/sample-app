pushd VotingData
dotnet publish -o ..\VotingApp\VotingDataPkg\Code\
popd

pushd VotingWeb
dotnet publish -o ..\VotingApp\VotingWebPkg\Code\
popd