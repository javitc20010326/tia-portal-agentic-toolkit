param(
    [string]$PlanPath = "",
    [ValidateSet("status", "open-project", "focus", "prepare-import", "capture-state", "send-keys")]
    [string]$Action = "status",
    [string]$ProjectPath = "",
    [string]$ImportPackFolder = "",
    [string]$WindowTitleRegex = "TIA|Totally Integrated Automation|Portal",
    [string]$Keys = "",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class Win32WindowTools
{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
}
"@

function Get-TiaPortalExecutables {
    $roots = @(
        $env:ProgramFiles,
        ${env:ProgramFiles(x86)}
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($root in $roots) {
        $portalRoot = Join-Path $root "Siemens\Automation"
        if (-not (Test-Path -LiteralPath $portalRoot)) {
            continue
        }

        Get-ChildItem -LiteralPath $portalRoot -Directory -Filter "Portal V*" -ErrorAction SilentlyContinue |
            ForEach-Object {
                Get-ChildItem -LiteralPath $_.FullName -Recurse -File -ErrorAction SilentlyContinue |
                    Where-Object { $_.Name -match "Portal.*\.exe$|Siemens\.Automation\.Portal.*\.exe$" } |
                    Select-Object -First 5 FullName
            }
    }
}

function Get-TiaProcesses {
    Get-Process -ErrorAction SilentlyContinue |
        Where-Object {
            $_.ProcessName -match "Portal|Siemens|Automation" -or
            $_.MainWindowTitle -match "TIA|Totally Integrated Automation|Portal"
        } |
        Select-Object Id, ProcessName, MainWindowTitle, MainWindowHandle
}

function Invoke-FocusTia {
    param([string]$TitleRegex)

    $candidate = Get-TiaProcesses |
        Where-Object { $_.MainWindowHandle -ne 0 -and $_.MainWindowTitle -match $TitleRegex } |
        Select-Object -First 1

    if (-not $candidate) {
        throw "No TIA Portal window matched regex: $TitleRegex"
    }

    if (-not $DryRun) {
        [Win32WindowTools]::ShowWindowAsync([IntPtr]$candidate.MainWindowHandle, 9) | Out-Null
        [Win32WindowTools]::SetForegroundWindow([IntPtr]$candidate.MainWindowHandle) | Out-Null
    }

    $candidate
}

function Invoke-OpenProject {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "ProjectPath is required for open-project."
    }

    $fullPath = [System.IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($Path))
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Project file not found: $fullPath"
    }

    if (-not $DryRun) {
        Start-Process -FilePath $fullPath | Out-Null
    }

    [pscustomobject]@{
        opened = -not $DryRun
        projectPath = $fullPath
        note = "Opened through Windows file association. If Windows does not know .ap16/.ap17, open TIA Portal once and associate project files."
    }
}

function Invoke-PrepareImport {
    param([string]$Folder)

    if ([string]::IsNullOrWhiteSpace($Folder)) {
        throw "ImportPackFolder is required for prepare-import."
    }

    $fullFolder = [System.IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($Folder))
    if (-not (Test-Path -LiteralPath $fullFolder)) {
        throw "Import pack folder not found: $fullFolder"
    }

    $files = Get-ChildItem -LiteralPath $fullFolder -File |
        Where-Object { $_.Extension -in ".scl", ".xml", ".csv", ".udt", ".db", ".md", ".json" } |
        Select-Object Name, FullName, Length

    $importMapPath = Join-Path $fullFolder "EXPERIMENTAL_IMPORT_MAP.json"
    $importMap = $null
    if (Test-Path -LiteralPath $importMapPath) {
        $importMap = Get-Content -LiteralPath $importMapPath -Raw | ConvertFrom-Json
    }

    [pscustomobject]@{
        importPackFolder = $fullFolder
        candidateFiles = $files
        experimentalImportMap = $importMap
        recommendedOrder = @(
            "UDT/SCL type files for direct source import",
            "DB files for direct source import",
            "FB/FC/OB SCL files for direct source import",
            "PLC tag CSV/XML files, version dependent",
            "Robot LAD/FBD/HMI XML/JSON recipes for guided UI construction",
            "HMI templates or screen plans"
        )
    }
}

function Invoke-SendKeys {
    param([string]$TitleRegex, [string]$KeySequence)

    if ([string]::IsNullOrWhiteSpace($KeySequence)) {
        throw "Keys is required for send-keys."
    }

    $window = Invoke-FocusTia -TitleRegex $TitleRegex
    if (-not $DryRun) {
        $shell = New-Object -ComObject WScript.Shell
        $shell.AppActivate($window.Id) | Out-Null
        Start-Sleep -Milliseconds 300
        $shell.SendKeys($KeySequence)
    }

    [pscustomobject]@{
        sent = -not $DryRun
        targetProcessId = $window.Id
        targetWindow = $window.MainWindowTitle
        keys = $KeySequence
    }
}

function Invoke-CaptureState {
    [pscustomobject]@{
        timestamp = (Get-Date).ToString("o")
        tiaExecutables = @(Get-TiaPortalExecutables)
        tiaProcesses = @(Get-TiaProcesses)
    }
}

function Invoke-Plan {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "PlanPath is required."
    }

    $fullPath = [System.IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($Path))
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Plan file not found: $fullPath"
    }

    $plan = Get-Content -LiteralPath $fullPath -Raw | ConvertFrom-Json
    $results = New-Object System.Collections.Generic.List[object]

    foreach ($phase in $plan.phases) {
        switch ($phase.action) {
            "status" {
                $results.Add([pscustomobject]@{ phase = $phase.id; result = Invoke-CaptureState })
            }
            "open-project" {
                if ($phase.required -and $phase.projectPath) {
                    $results.Add([pscustomobject]@{ phase = $phase.id; result = Invoke-OpenProject -Path $phase.projectPath })
                    Start-Sleep -Seconds 8
                }
            }
            "focus" {
                $regex = if ($phase.windowTitleRegex) { $phase.windowTitleRegex } else { $WindowTitleRegex }
                $results.Add([pscustomobject]@{ phase = $phase.id; result = Invoke-FocusTia -TitleRegex $regex })
            }
            "prepare-import" {
                if ($phase.required -and $phase.importPackFolder) {
                    $results.Add([pscustomobject]@{ phase = $phase.id; result = Invoke-PrepareImport -Folder $phase.importPackFolder })
                }
            }
            "capture-state" {
                $results.Add([pscustomobject]@{ phase = $phase.id; result = Invoke-CaptureState })
            }
            default {
                $results.Add([pscustomobject]@{ phase = $phase.id; skipped = $true; reason = "No stable generic implementation for action '$($phase.action)' yet." })
            }
        }
    }

    $results
}

if (-not [string]::IsNullOrWhiteSpace($PlanPath)) {
    Invoke-Plan -Path $PlanPath | ConvertTo-Json -Depth 8
    exit
}

switch ($Action) {
    "status" { Invoke-CaptureState | ConvertTo-Json -Depth 8 }
    "open-project" { Invoke-OpenProject -Path $ProjectPath | ConvertTo-Json -Depth 8 }
    "focus" { Invoke-FocusTia -TitleRegex $WindowTitleRegex | ConvertTo-Json -Depth 8 }
    "prepare-import" { Invoke-PrepareImport -Folder $ImportPackFolder | ConvertTo-Json -Depth 8 }
    "capture-state" { Invoke-CaptureState | ConvertTo-Json -Depth 8 }
    "send-keys" { Invoke-SendKeys -TitleRegex $WindowTitleRegex -KeySequence $Keys | ConvertTo-Json -Depth 8 }
}
