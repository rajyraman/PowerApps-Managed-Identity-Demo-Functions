#Credit: https://github.com/Azure/azure-dev/issues/1697#issue-1617610507
$output = azd env get-values
foreach ($line in $output) {
  $name, $value = $line.Split("=")
  $value = $value -replace '^\"|\"$'
  [Environment]::SetEnvironmentVariable($name, $value)
}
Write-Host "`n‼ Functions code deployed. Swagger URL App https://$($env:FUNCTIONS_NAME).azurewebsites.net/api/swagger/ui ‼" -ForegroundColor Yellow;