<#
.SYNOPSIS
    Builds the portable FBCTools zip and the setup for end users (Asama 5, ADR-0012, ADR-0022).

.DESCRIPTION
    Publishes every component into one folder, adds the template, dist\README.txt and the
    licences (ADR-0013), and zips it as artifacts\dist\FBCTools-<version>-win-x64.zip. From the
    same folder Inno Setup builds artifacts\dist\FBCTools-<version>-setup.exe (installer\FBCTools.iss).
    Each gets a .sha256 file next to it. The shell extension, the
    runner, the restarter and the manager are NativeAOT: single files with no runtime to
    install. The editor is WinForms, which NativeAOT cannot compile; by default it is a
    self-contained single file too, so the zip runs on a machine without .NET.

    Written for Windows PowerShell 5.1 and kept to ASCII (see dev-run.ps1).

.PARAMETER Editor
    SelfContained (default): one exe with the .NET runtime inside, about 115 MB.
    FrameworkDependent: a small exe that needs the .NET 10 Desktop Runtime installed.

.PARAMETER IncludeSymbols
    Keep the .pdb files.

.PARAMETER NoZip
    Leave the folder, do not zip it.

.PARAMETER NoSetup
    Do not build the setup.

.PARAMETER Iscc
    The Inno Setup compiler, ISCC.exe. Looked for on the PATH and where Inno Setup 7 or 6
    installs itself when not given.

.EXAMPLE
    publish.cmd
.EXAMPLE
    publish.cmd -Editor FrameworkDependent
#>
[CmdletBinding()]
param(
    [ValidateSet('SelfContained', 'FrameworkDependent')]
    [string] $Editor = 'SelfContained',

    [switch] $IncludeSymbols,

    [switch] $NoZip,

    [switch] $NoSetup,

    [string] $Iscc
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
[xml] $props = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props')
$version = $props.Project.PropertyGroup.VersionPrefix
$copyright = $props.Project.PropertyGroup.Copyright
$projectUrl = $props.Project.PropertyGroup.PackageProjectUrl
$name = "FBCTools-$version-win-x64"
$distRoot = Join-Path $root 'artifacts\dist'
$folder = Join-Path $distRoot $name

function Write-Step([string] $Title) {
    Write-Host ''
    Write-Host "==> $Title" -ForegroundColor Cyan
}

function Invoke-Native([string] $File, [string[]] $Arguments) {
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$File $($Arguments -join ' ') exited with code $LASTEXITCODE"
    }
}

# A file that is not there right after it was written was most likely taken by an antivirus:
# the zip and the setup hold unsigned programs.
function Write-Sha256([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Path was not written, or was removed right after (an antivirus may have quarantined it)"
    }

    $hash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    Set-Content -LiteralPath "$Path.sha256" -Value $hash -Encoding Ascii
    Write-Host "SHA-256: $hash"
}

# Inno Setup records where it went in its own uninstall entry: for the user or the machine,
# version 7 (64- or 32-bit) or 6. The usual folders are the fallback.
function Find-Iscc {
    if ($Iscc) {
        if (-not (Test-Path -LiteralPath $Iscc)) {
            throw "ISCC.exe not found at $Iscc"
        }

        return $Iscc
    }

    $command = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $uninstall = @(
        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall'
    )
    foreach ($name in @('Inno Setup 7_is1', 'Inno Setup 7 (32-bit)_is1', 'Inno Setup 6_is1')) {
        foreach ($key in $uninstall) {
            $entry = Get-ItemProperty -LiteralPath "$key\$name" -ErrorAction SilentlyContinue
            if ($entry -and $entry.PSObject.Properties['InstallLocation'] -and $entry.InstallLocation) {
                # Not Join-Path: it fails on a drive that is not there (a stale entry).
                $candidate = [System.IO.Path]::Combine([string] $entry.InstallLocation, 'ISCC.exe')
                if (Test-Path -LiteralPath $candidate) {
                    return $candidate
                }
            }
        }
    }

    # The parentheses matter: a comma binds tighter than +, which would make one string of the list.
    $folders = @(($env:LOCALAPPDATA + '\Programs'), $env:ProgramFiles, ${env:ProgramFiles(x86)}) | Where-Object { $_ }
    foreach ($folder in $folders) {
        foreach ($name in @('Inno Setup 7', 'Inno Setup 6')) {
            $candidate = Join-Path $folder "$name\ISCC.exe"
            if (Test-Path -LiteralPath $candidate) {
                return $candidate
            }
        }
    }

    throw 'Inno Setup was not found. Install it (winget install --id JRSoftware.InnoSetup.7 --exact), pass -Iscc <path to ISCC.exe>, or build without the setup: -NoSetup'
}

Push-Location $root
try {
    # Before the tests and the publishing, so that a missing Inno Setup stops the run at once.
    $isccPath = if ($NoSetup) { $null } else { Find-Iscc }

    Write-Step "Test"
    Invoke-Native 'dotnet' @('test', '-c', 'Release')

    if (Test-Path -LiteralPath $folder) {
        Remove-Item -LiteralPath $folder -Recurse -Force
    }

    Write-Step "Publish $name ($Editor editor)"
    foreach ($project in @('ShellExt', 'Runner', 'Restarter', 'Manager')) {
        Invoke-Native 'dotnet' @('publish', "src\FBC.ShellExtension.$project", '-c', 'Release', '-o', $folder)
    }

    $editorArguments = @('publish', 'src\FBC.ShellExtension.Editor', '-c', 'Release', '-o', $folder, '-p:PublishSingleFile=true')
    if ($Editor -eq 'SelfContained') {
        $editorArguments += @('--self-contained', 'true', '-p:IncludeNativeLibrariesForSelfExtract=true')
    }
    else {
        $editorArguments += @('--self-contained', 'false')
    }

    Invoke-Native 'dotnet' $editorArguments

    # The menu FBCTools starts with (ADR-0009), the end-user notes and the licence (ADR-0013).
    Copy-Item -LiteralPath (Join-Path $root 'dist\config.template.json') -Destination (Join-Path $folder 'config.template.json')
    Copy-Item -LiteralPath (Join-Path $root 'dist\README.txt') -Destination (Join-Path $folder 'README.txt')
    Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $folder 'LICENSE.txt')

    # Directory.Build.targets copies the .NET runtime's notices; the zip does not go out without them.
    $notices = @('dotnet-LICENSE.txt', 'dotnet-THIRD-PARTY-NOTICES.txt')
    if ($Editor -eq 'SelfContained') {
        $notices += 'winforms-THIRD-PARTY-NOTICES.txt'
    }

    foreach ($notice in $notices) {
        if (-not (Test-Path -LiteralPath (Join-Path $folder $notice))) {
            throw "$notice is missing from $folder (see Directory.Build.targets)"
        }
    }

    if (-not $IncludeSymbols) {
        Get-ChildItem -LiteralPath $folder -Filter '*.pdb' | Remove-Item -Force
    }

    # Linker leftovers of the NativeAOT DLL are not needed at run time. Not -Include: next to
    # -LiteralPath Windows PowerShell 5.1 does not filter by it, and this line emptied the folder
    # (G-0112).
    Get-ChildItem -LiteralPath $folder -File -Recurse | Where-Object { @('.lib', '.exp') -contains $_.Extension } | Remove-Item -Force

    # What an install cannot do without; a cleanup that went too far stops here, not in the zip.
    $required = @(
        'FBC.ShellExtension.ShellExt.dll', 'FBC.ShellExtension.Runner.exe', 'FBC.ShellExtension.Restarter.exe',
        'FBC.ShellExtension.Manager.exe', 'FBC.ShellExtension.Editor.exe', 'config.template.json', 'LICENSE.txt'
    )
    foreach ($file in $required) {
        if (-not (Test-Path -LiteralPath (Join-Path $folder $file))) {
            throw "$file is missing from $folder"
        }
    }

    if (-not $NoZip) {
        $zip = Join-Path $distRoot "$name.zip"
        if (Test-Path -LiteralPath $zip) {
            Remove-Item -LiteralPath $zip -Force
        }

        # ZipFile rather than Compress-Archive, which throws nothing for a folder it finds empty and
        # writes no zip at all (G-0112). ZipFile throws when it fails.
        Write-Step "Zip $zip"
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory($folder, $zip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
        Write-Sha256 $zip
    }

    if (-not $NoSetup) {
        $setup = Join-Path $distRoot "FBCTools-$version-setup.exe"
        Write-Step "Setup $setup"
        $isccArguments = @('/Q', "/DAppVersion=$version", "/DSourceDir=$folder", "/DCopyright=$copyright", "/O$distRoot")
        if ($projectUrl) {
            $isccArguments += "/DAppUrl=$projectUrl"
        }

        Invoke-Native $isccPath ($isccArguments + (Join-Path $root 'installer\FBCTools.iss'))
        Write-Sha256 $setup
    }

    Write-Host ''
    Write-Host "Done: $folder" -ForegroundColor Green
}
finally {
    Pop-Location
}
