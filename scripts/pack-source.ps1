param(
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path (Split-Path -Parent $root) "tia-portal-agentic-toolkit-source.zip"
}

if (Test-Path -LiteralPath $OutputPath) {
    Remove-Item -LiteralPath $OutputPath -Force
}

$files = Get-ChildItem -LiteralPath $root -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$files | Compress-Archive -DestinationPath $OutputPath -Force
Get-Item -LiteralPath $OutputPath
