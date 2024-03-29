$functionAppId = az ad sp list --filter "displayName eq '$env:FUNCTIONS_NAME'" --query '[].appId' --output tsv;
if ($null -ne $env:CI) {
  pac auth create -env $env:DATAVERSE_URL -mi

  # Give Application permission to create Service Principal
  pac admin application register -id $env:AZURE_CLIENT_ID

  # Create Function App as a Service Principal with System Admin role (implicit) in the environment
  pac admin assign-user -u $functionAppId -au -env $env:DATAVERSE_URL -r "System Administrator";

  # This code was required before pac 1.29.6 to register the app as a admin application. It is no longer required, but keeping here for reference.
  # $token = az account get-access-token --resource=https://api.bap.microsoft.com/ --query accessToken --output tsv
  # $headers = @{
  #   Authorization = "Bearer $token"
  # };
  # Write-Output "Invoke PUT on https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/adminApplications/$($env:AZURE_CLIENT_ID)?api-version=2020-10-01"
  # # Give Application permission to create Service Principal
  # Invoke-WebRequest -Headers $headers "https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/adminApplications/$($env:AZURE_CLIENT_ID)?api-version=2020-10-01" -Method Put
  # $environmentId = (pac org who --json | ConvertFrom-Json).EnvironmentId;
  # $body = ConvertTo-Json -InputObject @{servicePrincipalAppId = $functionAppId }
  # Write-Host "`n‼ Creating Function App ""$env:FUNCTIONS_NAME"" with AppId ""$functionAppId"" as an application user in $env:DATAVERSE_URL ‼" -ForegroundColor Yellow;
  # Write-Output "Invoke POST on https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/scopes/admin/environments/$($environmentId)/addAppUser?api-version=2020-10-01"
  # # Create Function App as a Service Principal with System Admin role (implicit) in the environment
  # Invoke-WebRequest -Headers $headers https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/scopes/admin/environments/$($environmentId)/addAppUser?api-version=2020-10-01 -Method Post -Body $body -ContentType "application/json"
}
else {
  Write-Host "`n‼ Assigning Function App ""$env:FUNCTIONS_NAME"" with AppId ""$functionAppId"" System Administrator role in $env:DATAVERSE_URL ‼" -ForegroundColor Yellow;
  pac admin assign-user -u $functionAppId -au -env $env:DATAVERSE_URL -r "System Administrator";
}
