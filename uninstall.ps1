try {
    # Uninstall the global tool
    dotnet tool uninstall --global CSharply
}
catch {
    Write-Host "Error during uninstallation: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "You may need to run this script as administrator or check if the tool is actually installed." -ForegroundColor Yellow
    exit 1
}
