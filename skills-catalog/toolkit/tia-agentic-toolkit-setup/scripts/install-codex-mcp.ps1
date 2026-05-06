param(
    [string]$ToolkitRoot = "",
    [string]$ConfigPath = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ToolkitRoot)) {
    $ToolkitRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
} else {
    $ToolkitRoot = (Resolve-Path $ToolkitRoot).Path
}

if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $ConfigPath = Join-Path $HOME ".codex\config.toml"
}

$server = Join-Path $ToolkitRoot "src\TiaPortalAgenticToolkit.McpServer\bin\Debug\net8.0-windows\TiaPortalAgenticToolkit.McpServer.exe"
if (-not (Test-Path $server)) {
    dotnet build (Join-Path $ToolkitRoot "src\TiaPortalAgenticToolkit.McpServer\TiaPortalAgenticToolkit.McpServer.csproj") | Out-Host
}

if (-not (Test-Path $server)) {
    Write-Error "MCP server executable not found after build: $server"
}

New-Item -ItemType Directory -Force -Path (Split-Path $ConfigPath -Parent) | Out-Null
if (-not (Test-Path $ConfigPath)) {
    New-Item -ItemType File -Path $ConfigPath | Out-Null
}

$content = Get-Content -LiteralPath $ConfigPath -Raw
$block = @(
    "[mcp_servers.tia_portal]",
    "command = '$server'",
    "tool_timeout_sec = 600",
    "env_vars = ['WINDIR', 'ProgramFiles', 'ProgramFiles(x86)', 'USERNAME', 'USERDOMAIN']"
) -join "`r`n"

$pattern = '(?ms)^\[mcp_servers\.tia_portal\]\r?\n.*?(?=^\[|\z)'
if ([regex]::IsMatch($content, $pattern)) {
    $content = [regex]::Replace($content, $pattern, $block + "`r`n")
    $action = "updated"
} else {
    if (-not $content.EndsWith("`n")) { $content += "`r`n" }
    $content += "`r`n" + $block + "`r`n"
    $action = "created"
}

Set-Content -LiteralPath $ConfigPath -Value $content -Encoding UTF8
Write-Output "Codex MCP config $action in $ConfigPath"
Write-Output $block
