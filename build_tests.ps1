Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

function Exec
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$cmd,
        [Parameter(Position=1,Mandatory=0)][string]$errorMessage = ("Error executing command {0}" -f $cmd)
    )
    & $cmd
    if ($lastexitcode -ne 0) {
        throw ("Exec: " + $errorMessage)
    }
}

Write-Host "Building Code"
rmdir -Force -Recurse VotingApp\VotingDataPkg\Code\ -ErrorAction Ignore
pushd VotingData
Exec { dotnet publish -o ..\VotingApp\VotingDataPkg\Code\ -r win10-x64 }
popd

rmdir -Force -Recurse VotingApp\VotingWebPkg\Code\ -ErrorAction Ignore
pushd VotingWeb
Exec { dotnet publish -o ..\VotingApp\VotingWebPkg\Code\ -r win10-x64 }
popd
