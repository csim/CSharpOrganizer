dotnet build $PSScriptRoot\src\CSharply\CSharply.csproj --configuration debug

dotnet tool uninstall --global CSharply

dotnet pack $PSScriptRoot\src\CSharply\CSharply.csproj --configuration debug --output $PSScriptRoot\artifacts\packages

dotnet tool install --global --add-source $PSScriptRoot\artifacts\packages CSharply
