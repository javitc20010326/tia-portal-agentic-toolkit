param(
    [string]$Repository = "javitc20010326/tia-portal-agentic-toolkit"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "TIA Portal Agentic Toolkit - GitHub upload" -ForegroundColor Cyan
Write-Host "Repository: $Repository"
Write-Host ""
Write-Host "Paste your fine-grained GitHub token below. It will not be shown on screen."
Write-Host "Required permission: Contents = Read and write for this repository."
Write-Host ""

$secureToken = Read-Host "GitHub token" -AsSecureString
$plainTokenPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)

try {
    $env:GITHUB_TOKEN = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($plainTokenPtr)
    $script = Join-Path $PSScriptRoot "upload-github.ps1"
    & $script -Repository $Repository
    Write-Host ""
    Write-Host "Upload finished. Press Enter to close." -ForegroundColor Green
    Read-Host
}
finally {
    if ($plainTokenPtr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($plainTokenPtr)
    }
    Remove-Item Env:\GITHUB_TOKEN -ErrorAction SilentlyContinue
}
