param ([Parameter(Mandatory)]$functionAppName, [Parameter(Mandatory)]$environmentUrl, $createApplicationUser=$false)

$applicationId = az ad sp list --filter "displayName eq '$($functionAppName)'" --query '[].appId' --output tsv

if($applicationId -ne $null) {
    Write-Output "Function App Application Id: $($applicationId)"
    cd ../src
    func azure functionapp publish $functionAppName

    if($createApplicationUser) {
        if (-not (Get-Module -Name "Microsoft.Xrm.Data.Powershell")) {
            Install-Module Microsoft.Xrm.Data.PowerShell -Scope CurrentUser
        }    
        Import-Module Microsoft.Xrm.Data.PowerShell
        $conn = Get-CrmConnection -InteractiveMode
        $applicationUsers = Get-CrmRecords -EntityLogicalName systemuser -conn $conn -Fields fullname -FilterAttribute applicationid -FilterOperator eq -FilterValue $applicationId
        if($applicationUsers.CrmRecords.Count -eq 0) {
            Write-Output "Creating Application User"
            $rootBusinessUnit = (Get-CrmRecords -EntityLogicalName businessunit -conn $conn -Fields businessunitid -FilterAttribute parentbusinessunitid -FilterOperator null).CrmRecords[0].businessunitid;
            $rootBusinessUnitLookup = New-CrmEntityReference -EntityLogicalName businessunit -Id $rootBusinessUnit
            New-CrmRecord systemuser @{"applicationid"=[guid]$applicationId; businessunitid=$rootBusinessUnitLookup} -conn $conn
        }
        else {
            Write-Output "Application User $($applicationUsers.CrmRecords[0].fullname) already exists"
        }
    }
}
else {
    Write-Output "Function App $($functionAppName)' not found. Please check the name."
}