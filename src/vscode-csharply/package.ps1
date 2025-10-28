param([string]$Version)

if (-not $Version) {
    "Version is required"
    return;
}

Push-Location $PSScriptRoot

try {
    npm install vsce --global

    npm run compile

    npm version $Version --no-git-tag-version

    $outDir = Resolve-Path "$PSScriptRoot\..\..\artifacts"
    $out = "$outDir\csharply-v$($Version).vsix"
    vsce package --out $out

    ""
    "ouput dir: $PSScriptRoot/dist"
    "upload to: https://marketplace.visualstudio.com/manage/publishers/csim"
} finally {
    Pop-Location
}
