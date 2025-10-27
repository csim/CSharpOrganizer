#dotnet build $PSScriptRoot\src\CSharply\CSharply.csproj --configuration release

dotnet pack $PSScriptRoot\src\CSharply\CSharply.csproj --configuration Release --output $PSScriptRoot\packages

dotnet tool install --global --add-source $PSScriptRoot\packages CSharply
