param(
    [string]$ToolkitRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ToolkitRoot)) {
    $ToolkitRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
} else {
    $ToolkitRoot = (Resolve-Path $ToolkitRoot).Path
}

if (-not (Test-Path (Join-Path $ToolkitRoot "skills-catalog"))) {
    Write-Error "skills-catalog not found in $ToolkitRoot"
}

$skillsRoot = Join-Path $HOME ".agents\skills"
New-Item -ItemType Directory -Force -Path $skillsRoot | Out-Null

$skillDirs = Get-ChildItem -Recurse -Filter "SKILL.md" -Path (Join-Path $ToolkitRoot "skills-catalog") |
    Where-Object { ($_.FullName -replace '\\','/') -match 'skills-catalog/[^/]+/[^/]+/SKILL\.md$' } |
    ForEach-Object { $_.Directory } |
    Sort-Object FullName

foreach ($skillDir in $skillDirs) {
    $linkPath = Join-Path $skillsRoot $skillDir.Name
    if (Test-Path $linkPath) {
        Remove-Item -Force -Recurse $linkPath
    }

    try {
        New-Item -ItemType SymbolicLink -Path $linkPath -Target $skillDir.FullName | Out-Null
    } catch {
        New-Item -ItemType Junction -Path $linkPath -Target $skillDir.FullName | Out-Null
    }

    Write-Output ("Linked {0} -> {1}" -f $linkPath, $skillDir.FullName)
}

Write-Output ""
Write-Output "Skills directory: $skillsRoot"
