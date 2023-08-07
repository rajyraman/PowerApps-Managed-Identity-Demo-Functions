if ($null -ne $env:CI) {
    azd env refresh
}
else {
    Write-Host "`n‼ Functions code deployed. Swagger URL is https://$env:FUNCTIONS_NAME.azurewebsites.net/api/swagger/ui ‼" -ForegroundColor Yellow;
}