param ($resourceGroup='rgazfunc', $location='australiasoutheast', [Parameter(Mandatory)]$environmentUrl, $createApplicationUser=$false)

az group create --location $location --resource-group $resourceGroup
$deploymentName = $(Get-Date -Format "yyyy_MM_dd_hh_mm")

az deployment group create --resource-group $resourceGroup --template-file main.bicep --parameters environmentUrl=$environmentUrl --name $deploymentName --query properties.outputs.functionName.value
$output = az deployment group show --name $deploymentName --resource-group $resourceGroup --query ['properties.outputs.functionName.value,properties.outputs.applicationId.value'] --output tsv

$applicationId = az ad sp list --filter "displayName eq '$($output[0])'" --query '[].appId' --output tsv
Write-Output "Function app created: $($output[0]), Application Id: $($applicationId)"

cd ../src
func azure functionapp publish $output[0]

if($createApplicationUser) {
    Write-Output "Creating Application User"
    if (-not (Get-Module -Name "Microsoft.Xrm.Data.Powershell")) {
         Install-Module Microsoft.Xrm.Data.PowerShell -Scope CurrentUser
    }    
    Import-Module Microsoft.Xrm.Data.PowerShell
    $conn = Get-CrmConnection -InteractiveMode
    $rootBusinessUnit = (Get-CrmRecords -EntityLogicalName businessunit -conn $conn -Fields businessunitid -FilterAttribute parentbusinessunitid -FilterOperator null).CrmRecords[0].businessunitid;
    $rootBusinessUnitLookup = New-CrmEntityReference -EntityLogicalName businessunit -Id $rootBusinessUnit
    New-CrmRecord systemuser @{"applicationid"=[guid]$applicationId; businessunitid=$rootBusinessUnitLookup} -conn $conn
}