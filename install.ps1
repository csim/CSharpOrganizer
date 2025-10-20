dotnet build $PSScriptRoot\src\CSharpOrganizer\CSharpOrganizer.csproj --configuration release

dotnet pack $PSScriptRoot\src\CSharpOrganizer\CSharpOrganizer.csproj --configuration Release --output $PSScriptRoot\packages

dotnet tool install --global --add-source $PSScriptRoot\packages CSharpOrganizer
