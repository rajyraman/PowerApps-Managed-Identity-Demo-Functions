#Credit: https://github.com/Azure/azure-dev/issues/1697#issue-1617610507
$output = azd env get-values
foreach ($line in $output) {
  $name, $value = $line.Split("=")
  $value = $value -replace '^\"|\"$'
  [Environment]::SetEnvironmentVariable($name, $value)
}
$functionAppId = az ad sp list --filter "displayName eq '$env:FUNCTIONS_NAME'" --query '[].appId' --output tsv;
if ($null -ne $env:CI) {
  # Set FUNCTIONS_NAME as a variable on the environment
  gh variable set FUNCTIONS_NAME --env $env:GITHUB_ENV_NAME --body $env:FUNCTIONS_NAME

  $token = az account get-access-token --resource=https://api.bap.microsoft.com/ --query accessToken --output tsv
  $headers = @{
    Authorization = "Bearer $token"
  };
  Write-Output "Invoke PUT on https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/adminApplications/$($env:AZURE_CLIENT_ID)?api-version=2020-10-01"
  # Give Application permission to create Service Principal
  Invoke-WebRequest -Headers $headers "https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/adminApplications/$($env:AZURE_CLIENT_ID)?api-version=2020-10-01" -Method Put
  $environmentId = (pac org who --json | ConvertFrom-Json).EnvironmentId;
  $body = ConvertTo-Json -InputObject @{servicePrincipalAppId = $functionAppId }
  Write-Host "`n‼ Creating Function App ""$env:FUNCTIONS_NAME"" with AppId ""$functionAppId"" as an application user in $env:DATAVERSE_URL ‼" -ForegroundColor Yellow;
  Write-Output "Invoke POST on https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/scopes/admin/environments/$($environmentId)/addAppUser?api-version=2020-10-01"
  # Create Function App as a Service Principal with System Admin role (implicit) in the environment
  Invoke-WebRequest -Headers $headers https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/scopes/admin/environments/$($environmentId)/addAppUser?api-version=2020-10-01 -Method Post -Body $body -ContentType "application/json"
}
else {
  Write-Host "`n‼ Assigning Function App ""$env:FUNCTIONS_NAME"" with AppId ""$functionAppId"" System Administrator role in $env:DATAVERSE_URL ‼" -ForegroundColor Yellow;
  pac admin assign-user -u $functionAppId -au -env $env:DATAVERSE_URL -r "System Administrator";
}