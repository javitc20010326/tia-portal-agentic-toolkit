param(
    [Parameter(Mandatory = $true)]
    [string]$Repository,

    [string]$Branch = "main"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
    Write-Error "GITHUB_TOKEN is not set. Set it in your local PowerShell session before running this script."
}

$root = Split-Path -Parent $PSScriptRoot
$headers = @{
    Authorization = "Bearer $env:GITHUB_TOKEN"
    Accept = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

function ConvertTo-Base64Utf8([string]$Text) {
    [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Text))
}

function Get-RemoteFileSha([string]$Path) {
    $encodedPath = ($Path -replace '\\','/')
    $uri = "https://api.github.com/repos/$Repository/contents/$encodedPath"
    if (-not [string]::IsNullOrWhiteSpace($Branch)) {
        $uri += "?ref=$Branch"
    }

    try {
        $response = Invoke-RestMethod -Method Get -Uri $uri -Headers $headers
        return $response.sha
    } catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) {
            return $null
        }
        throw
    }
}

$files = Get-ChildItem -LiteralPath $root -Recurse -File |
    Where-Object {
        $_.FullName -notmatch '\\(bin|obj)\\' -and
        $_.FullName -notmatch '\\.git\\'
    } |
    Sort-Object FullName

foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($root.Length + 1).Replace('\', '/')
    $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    $sha = Get-RemoteFileSha $relativePath

    $body = @{
        message = if ($sha) { "Update $relativePath" } else { "Add $relativePath" }
        content = ConvertTo-Base64Utf8 $content
        branch = $Branch
    }
    if ($sha) {
        $body.sha = $sha
    }

    $json = $body | ConvertTo-Json -Depth 5
    $uri = "https://api.github.com/repos/$Repository/contents/$relativePath"
    Invoke-RestMethod -Method Put -Uri $uri -Headers $headers -Body $json -ContentType "application/json" | Out-Null
    Write-Output "Uploaded $relativePath"
}

Write-Output ""
Write-Output "Upload complete: https://github.com/$Repository"
