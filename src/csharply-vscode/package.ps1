[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = "Version number in semver format (e.g., 1.2.3)")]
    [ValidatePattern('^\d+\.\d+\.\d+(-\w+)?$')]
    [string]$Version,

    [Parameter(HelpMessage = "Skip npm install for vsce")]
    [switch]$SkipVsceInstall,

    [Parameter(HelpMessage = "Skip compilation step")]
    [switch]$SkipCompile,

    [Parameter(HelpMessage = "Output directory for the packaged extension")]
    [string]$OutputDirectory = "$PSScriptRoot\..\..\artifacts"
)

# Set error action preference for better error handling
$ErrorActionPreference = "Stop"

function Write-StepMessage {
    param([string]$Message)
    Write-Host "🔧 $Message" -ForegroundColor Cyan
}

function Write-SuccessMessage {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-ErrorMessage {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Test-CommandExists {
    param([string]$Command)
    try {
        Get-Command $Command -ErrorAction Stop | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Invoke-SafeCommand {
    param(
        [string]$Command,
        [string]$Description
    )

    Write-StepMessage $Description
    try {
        Invoke-Expression $Command
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code $LASTEXITCODE"
        }
        Write-SuccessMessage "$Description completed successfully"
    }
    catch {
        Write-ErrorMessage "$Description failed: $($_.Exception.Message)"
        throw
    }
}

$OutputDirectory = Resolve-Path -Path $OutputDirectory -ErrorAction SilentlyContinue

# Main script execution
Write-Host "🚀 Starting VS Code Extension Packaging Process" -ForegroundColor Yellow
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host "Output Directory: $OutputDirectory" -ForegroundColor Yellow

# Validate prerequisites
if (-not (Test-CommandExists "npm")) {
    Write-ErrorMessage "npm is not installed or not in PATH"
    exit 1
}

if (-not (Test-Path "package.json")) {
    Write-ErrorMessage "package.json not found in current directory"
    exit 1
}

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputDirectory)) {
    Write-StepMessage "Creating output directory: $OutputDirectory"
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}

# Resolve output directory path
$outDir = Resolve-Path $OutputDirectory
$outputFile = Join-Path $outDir "csharply-v$Version.vsix"

Push-Location $PSScriptRoot

try {
    # Install vsce globally if not skipped
    if (-not $SkipVsceInstall) {
        if (-not (Test-CommandExists "vsce")) {
            Invoke-SafeCommand "npm install -g vsce" "Installing vsce globally"
        } else {
            Write-SuccessMessage "vsce is already installed"
        }
    }

    # Compile the extension if not skipped
    if (-not $SkipCompile) {
        Invoke-SafeCommand "npm run compile" "Compiling TypeScript code"
    }

    # Update package.json version
    Invoke-SafeCommand "npm version $Version --no-git-tag-version" "Updating package version to $Version"

    # Package the extension
    Invoke-SafeCommand "vsce package --out `"$outputFile`"" "Packaging extension"

    # Success summary
    Write-Host "`n🎉 Extension packaged successfully!" -ForegroundColor Green
    Write-Host "📦 Output file: $outputFile" -ForegroundColor Green
    Write-Host "📁 Output directory: $outDir" -ForegroundColor Green
    Write-Host "🌐 Upload to: https://marketplace.visualstudio.com/manage/publishers/csim" -ForegroundColor Yellow

    # Show file info
    if (Test-Path $outputFile) {
        $fileInfo = Get-Item $outputFile
        Write-Host "📊 File size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Green
    }

} catch {
    Write-ErrorMessage "Packaging failed: $($_.Exception.Message)"
    exit 1
} finally {
    Pop-Location
}
