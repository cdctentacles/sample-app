pushd VotingData
dotnet publish -o ..\VotingApp\VotingDataPkg\Code\ -r win10-x64
popd

pushd VotingWeb
dotnet publish -o ..\VotingApp\VotingWebPkg\Code\ -r win10-x64
popd