<#
.SYNOPSIS
    Copies a published FBCTools folder over an installed one.

.DESCRIPTION
    A shell extension DLL that a process still has loaded cannot be overwritten, but it can be
    renamed: it is moved aside as <name>.old-<time>, the way Microsoft documents DLL updates,
    and the process keeps the old copy until it exits (G-0101, ADR-0008). Copies left by earlier
    runs are deleted once nothing holds them. If the copy fails, the old DLL is put back so the
    registration does not point at a missing file.

    Written for Windows PowerShell 5.1 and kept to ASCII (see dev-run.ps1). dev-run.ps1 calls
    it directly, or through the Explorer restarter so that it runs while Explorer is stopped.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Source,

    [Parameter(Mandatory = $true)]
    [string] $Target,

    [string] $DllName = 'FBC.ShellExtension.ShellExt.dll'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-FileLocked([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        return $false
    }

    try {
        $stream = [System.IO.File]::Open($Path, 'Open', 'ReadWrite', 'None')
        $stream.Dispose()
        return $false
    }
    catch [System.IO.IOException] {
        return $true
    }
}

New-Item -ItemType Directory -Force -Path $Target | Out-Null

# Copies renamed by earlier runs; still locked ones stay until their process lets go.
Get-ChildItem -LiteralPath $Target -Filter "$DllName.old-*" -ErrorAction SilentlyContinue | ForEach-Object {
    Remove-Item -LiteralPath $_.FullName -ErrorAction SilentlyContinue
}

$installedDll = Join-Path $Target $DllName
$aside = $null
if (Test-FileLocked $installedDll) {
    $aside = "$installedDll.old-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Move-Item -LiteralPath $installedDll -Destination $aside
}

try {
    Copy-Item -Path (Join-Path $Source '*') -Destination $Target -Force
}
catch {
    if ($aside -and -not (Test-Path -LiteralPath $installedDll)) {
        # Put the old DLL back, so the registration does not point at a missing file.
        Move-Item -LiteralPath $aside -Destination $installedDll
    }

    throw
}

# Symbol files an earlier install left; dev-run leaves them out unless asked (-IncludeSymbols).
Get-ChildItem -LiteralPath $Target -Filter '*.pdb' | Where-Object {
    -not (Test-Path -LiteralPath (Join-Path $Source $_.Name))
} | Remove-Item -Force -ErrorAction SilentlyContinue

if ($aside) {
    Write-Warning "The installed DLL was in use (Explorer or an Open dialog). It was renamed to $(Split-Path -Leaf $aside); whatever holds it keeps the old version until it exits (G-0101)."
}
