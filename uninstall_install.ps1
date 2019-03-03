# Connect to cluster before running this command
# Connect-ServiceFabricCluster -ConnectionEndpoint asnegitmp1.eastus.cloudapp.azure.com:19000 -X509Credential -FindType FindByThumbprint -FindValue 32D8BAFB4194EC1657FD2339245B43F510990512 -StoreLocation CurrentUser -Verbose -ServerCommonName asnegitmp1 -StoreName My -ServerCertThumbprint 32D8BAFB4194EC1657FD2339245B43F510990512

param (
  [Parameter(Mandatory=$true)][string]$EventHubConnectionString = "",
  [Parameter(Mandatory=$true)][string]$EventHubName = ""
)

$AppPath = "VotingApp"
$UploadPath = "VotingAppUpload"

Remove-Item -LiteralPath $UploadPath -Force -Recurse

Remove-ServiceFabricApplication fabric:/votingapp -Force
Unregister-ServiceFabricApplicationType VotingType 1.0.0 -Force
Remove-ServiceFabricApplicationPackage VotingType

Copy-ServiceFabricApplicationPackage -ApplicationPackagePathInImageStore VotingType -ApplicationPackagePath $AppPath -ApplicationPackageCopyPath $UploadPath -CompressPackage -TimeoutSec 1800 -ShowProgress
Register-ServiceFabricApplicationType VotingType
New-ServiceFabricApplication fabric:/votingapp VotingType 1.0.0 -ApplicationParameter @{CDC_AzureEventHubsConnectionString="$EventHubConnectionString";CDC_EventHubName="$EventHubName"}

Remove-Item -LiteralPath $UploadPath -Force -Recurse

# Get thumbprint of the certificate
# $certificateObject = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
# $certificateObject.Import("C:\\Users\\asnegi\\Downloads\\asnegitmp1-asnegitmp2-20190225.pfx", "", [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::DefaultKeySet)
# $certificateObject.Thumbprint
