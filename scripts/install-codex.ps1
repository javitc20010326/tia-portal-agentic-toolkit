param(
    [string]$ToolkitRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ToolkitRoot)) {
    $ToolkitRoot = Split-Path -Parent $PSScriptRoot
}

& (Join-Path $ToolkitRoot "skills-catalog\toolkit\tia-agentic-toolkit-setup\scripts\install-global-skills.ps1") -ToolkitRoot $ToolkitRoot
& (Join-Path $ToolkitRoot "skills-catalog\toolkit\tia-agentic-toolkit-setup\scripts\install-codex-mcp.ps1") -ToolkitRoot $ToolkitRoot

Write-Output ""
Write-Output "Installed TIA Portal Agentic Toolkit for Codex."
Write-Output "Restart Codex, then ask: Check my TIA Portal Openness environment."
