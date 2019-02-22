pushd VotingData
dotnet publish -o ../VotingApp/VotingDataPkg/Code/ -r ubuntu.16.04-x64
popd

pushd VotingWeb
dotnet publish -o ../VotingApp/VotingWebPkg/Code/ -r ubuntu.16.04-x64
popd