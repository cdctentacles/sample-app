rmdir /s /q VotingApp\VotingDataPkg\Code\
pushd VotingData
dotnet publish -o ..\VotingApp\VotingDataPkg\Code\ -r win10-x64
popd

rmdir /s /q VotingApp\VotingWebPkg\Code\
pushd VotingWeb
dotnet publish -o ..\VotingApp\VotingWebPkg\Code\ -r win10-x64
popd