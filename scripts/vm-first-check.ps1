$ErrorActionPreference = "Continue"

Write-Output "=== User ==="
whoami

Write-Output ""
Write-Output "=== TIA Portal Siemens.Engineering.dll candidates ==="
Get-ChildItem 'C:\Program Files\Siemens\Automation' -Recurse -Filter Siemens.Engineering.dll -ErrorAction SilentlyContinue | Select-Object FullName

Write-Output ""
Write-Output "=== TIA Portal Openness registry ==="
Get-ChildItem 'HKLM:\SOFTWARE\Siemens\Automation\Openness' -Recurse -ErrorAction SilentlyContinue

Write-Output ""
Write-Output "=== Siemens TIA Openness group ==="
net localgroup "Siemens TIA Openness"

Write-Output ""
Write-Output "=== TIA/Siemens processes ==="
Get-Process | Where-Object { $_.ProcessName -like '*Portal*' -or $_.ProcessName -like '*Siemens*' } | Select-Object Id,ProcessName,MainWindowTitle

Write-Output ""
Write-Output "=== .NET SDKs ==="
dotnet --list-sdks

Write-Output ""
Write-Output "=== .NET runtimes ==="
dotnet --list-runtimes
