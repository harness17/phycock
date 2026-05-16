param(
    [int]$Port = 5000,
    [switch]$SkipDatabaseInitialization
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$env:ASPNETCORE_ENVIRONMENT = "Development"

if ($SkipDatabaseInitialization) {
    $env:AppSettings__SkipDatabaseInitialization = "true"
} else {
    Remove-Item Env:AppSettings__SkipDatabaseInitialization -ErrorAction SilentlyContinue
}

dotnet run `
    --project (Join-Path $repoRoot "Phycock") `
    --no-build `
    --no-launch-profile `
    --urls "http://localhost:$Port"
