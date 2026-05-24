param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $PSScriptRoot "ClickLight.Windows/ClickLight.Windows.csproj"
$publishDir = Join-Path $PSScriptRoot "artifacts/$Configuration/$RuntimeIdentifier"

dotnet restore $appProject
dotnet publish $appProject `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained false `
    -p:PublishSingleFile=false `
    -o $publishDir

Write-Host "Published Windows app to $publishDir"
