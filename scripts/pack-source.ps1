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
    Where-Object {
        $relative = $_.FullName.Substring($root.Length + 1).Replace('\', '/')
        $blockedExtensions = @(
            ".zip", ".rar", ".pdf", ".xlsx", ".download",
            ".ap16", ".ap17", ".ap18", ".ap19", ".ap20", ".ap21",
            ".zap16", ".zap17", ".zap18", ".zap19", ".zap20", ".zap21",
            ".al"
        )

        ($blockedExtensions -notcontains $_.Extension.ToLowerInvariant()) -and
        ($relative -notmatch '(^|/)(bin|obj|\.git|exports|backups|user-exports|analysis|tia-user-exports-analysis)(/|$)')
    }

$files | Compress-Archive -DestinationPath $OutputPath -Force
Get-Item -LiteralPath $OutputPath
