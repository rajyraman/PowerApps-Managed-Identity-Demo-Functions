#Credit: https://github.com/Azure/azure-dev/issues/1697#issue-1617610507
$output = azd env get-values
foreach ($line in $output) {
  $name, $value = $line.Split("=")
  $value = $value -replace '^\"|\"$'
  [Environment]::SetEnvironmentVariable($name, $value)
}
$functionAppId = az ad sp list --filter "displayName eq '$env:FUNCTIONS_NAME'" --query '[].appId' --output tsv;
Write-Host "`n‼ Assigning Function App ""$env:FUNCTIONS_NAME"" with AppId ""$functionAppId"" System Administrator role in $env:DATAVERSE_URL ‼" -ForegroundColor Yellow;
pac admin assign-user -u $functionAppId -au -env $env:DATAVERSE_URL -r "System Administrator";