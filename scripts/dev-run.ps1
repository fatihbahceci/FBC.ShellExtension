<#
.SYNOPSIS
    Developer loop: build, test, publish the shell extension, the runner, the editor, the
    Explorer restarter and the manager with the template, and optionally install them for
    Explorer and set up the config.

.DESCRIPTION
    Written for Windows PowerShell 5.1 (the "powershell" that ships with Windows) and kept to
    ASCII, because 5.1 reads a script without a byte order mark in the ANSI code page.
    Usually started through dev-run.cmd in the repository root.

.PARAMETER Configuration
    Release (default) or Debug.

.PARAMETER Config
    none (default): with -Install, the installed manager brings %APPDATA%\FBCTools\config.json
    up to date with the template, as the editor does when it starts: it creates a missing
    config, replaces one the user never changed and adds new template entries to a changed one
    (ADR-0009). Without -Install the config is left alone.
    default (or its old name, sample): the manager resets the config to the template,
    dist\config.template.json, and saves the previous one next to it (the installed manager
    with -Install, the one in artifacts without). Unlike a plain copy, the manager also records
    the template it wrote, so later template updates still replace the menu.
    checks: copy samples\runner-checks.json there. An existing config that differs is first
    saved next to it as config.<time>.bak.json.

.PARAMETER Install
    Copy the published files to %LOCALAPPDATA%\Programs\FBCTools and register the shell
    extension for the current user (regsvr32, no elevation). A DLL that Explorer still holds
    is renamed out of the way; Explorer keeps using the old one until it restarts (G-0101).

.PARAMETER RestartExplorer
    With -Install: copy while Explorer is stopped, so the new DLL is used at once. The Explorer
    restarter (FBC.ShellExtension.Restarter.exe, ADR-0008) stops Explorer through Restart
    Manager, runs the copy, starts Explorer again and reopens its windows and tabs. If it cannot
    stop Explorer, the script falls back to stopping it directly and reopens only the folders.

.PARAMETER WhenRunning
    With -Install: what to do when the Runner or the Editor is still running from the install
    folder (a progress window, a summary, a message box or unsaved edits may be waiting for the
    user). Their files cannot be overwritten until they exit.
    ask (default): ask. wait: wait until they are closed. stop: stop them. A stopped Runner
    leaves its job half done and the commands it started without elevation keep running
    (KI-0002); the Editor is asked to close and may still ask about unsaved changes.

.PARAMETER SkipTests
    Do not run dotnet test.

.PARAMETER IncludeSymbols
    Keep the .pdb files. By default they are left out of the published and installed files.

.EXAMPLE
    dev-run.cmd
.EXAMPLE
    dev-run.cmd -Install -RestartExplorer -Config checks
.EXAMPLE
    dev-run.cmd -Install -WhenRunning stop
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release',

    [ValidateSet('none', 'default', 'sample', 'checks')]
    [string] $Config = 'none',

    [switch] $Install,

    [switch] $RestartExplorer,

    [ValidateSet('ask', 'wait', 'stop')]
    [string] $WhenRunning = 'ask',

    [switch] $SkipTests,

    [switch] $IncludeSymbols
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts\FBCTools'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\FBCTools'
$configDir = Join-Path $env:APPDATA 'FBCTools'
$dllName = 'FBC.ShellExtension.ShellExt.dll'
$runnerName = 'FBC.ShellExtension.Runner'
$editorName = 'FBC.ShellExtension.Editor'
$restarterName = 'FBC.ShellExtension.Restarter'
$managerName = 'FBC.ShellExtension.Manager'
$template = Join-Path $root 'dist\config.template.json'
$copyScript = Join-Path $PSScriptRoot 'install-files.ps1'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'

function Write-Step([string] $Title) {
    Write-Host ''
    Write-Host "==> $Title" -ForegroundColor Cyan
}

# Runs a console program and stops the script when it fails.
function Invoke-Native([string] $File, [string[]] $Arguments) {
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$File $($Arguments -join ' ') exited with code $LASTEXITCODE"
    }
}

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

function Wait-Unlocked([string] $Path, [int] $TimeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while (Test-FileLocked $Path) {
        if ($TimeoutSeconds -gt 0 -and (Get-Date) -gt $deadline) {
            return $false
        }

        Start-Sleep -Seconds 1
    }

    return $true
}

# A running program keeps its exe locked. Deals with that before anything in the install
# folder is changed, so a refused copy cannot leave the folder half updated.
function Close-InstalledProgram([string] $Name, [bool] $Polite) {
    $installed = Join-Path $installDir "$Name.exe"
    if (-not (Test-FileLocked $installed)) {
        return
    }

    $action = $WhenRunning
    if ($action -eq 'ask') {
        Write-Host "$Name in $installDir is still running; a window or a message box may be waiting for you."
        $answer = Read-Host '[W]ait until it is closed, [S]top it, or [C]ancel'
        if ($answer -match '^\s*[wW]') {
            $action = 'wait'
        }
        elseif ($answer -match '^\s*[sS]') {
            $action = 'stop'
        }
        else {
            throw "Cancelled: $Name is still running."
        }
    }

    if ($action -eq 'wait') {
        Write-Host "Waiting for $Name to close (Ctrl+C to give up)..."
        Wait-Unlocked $installed 0 | Out-Null
        return
    }

    # Only copies started from the install folder. The elevated worker (--worker) is left
    # alone even when this script is elevated: it cancels its job and stops the commands it
    # started once its runner is gone (ADR-0006). Its path cannot be read without elevation.
    $target = [System.IO.Path]::GetFullPath($installed)
    foreach ($process in @(Get-CimInstance -ClassName Win32_Process -Filter "Name = '$Name.exe'")) {
        $image = $process.ExecutablePath
        $worker = $process.CommandLine -and $process.CommandLine -match '\s--worker\s'
        if ($image -and -not $worker -and [string]::Equals([System.IO.Path]::GetFullPath($image), $target, [System.StringComparison]::OrdinalIgnoreCase)) {
            if ($Polite) {
                # The editor gets the chance to ask about unsaved changes.
                Write-Host "Asking $Name (process $($process.ProcessId)) to close..."
                $running = Get-Process -Id $process.ProcessId -ErrorAction SilentlyContinue
                if ($running) {
                    $running.CloseMainWindow() | Out-Null
                }
            }
            else {
                Write-Host "Stopping $Name (process $($process.ProcessId))..."
                Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
            }
        }
    }

    $timeout = if ($Polite) { 120 } else { 30 }
    if (-not (Wait-Unlocked $installed $timeout)) {
        throw "$Name in $installDir is still running. An elevated one cannot be stopped from here; close it and run again."
    }
}

# Folder windows Explorer has open: file system folders and shell locations such as This PC.
# Read in a separate process with a time limit: a call into a hung Explorer window never
# returns, and a restart is often meant to rescue exactly that.
function Get-OpenFolders {
    $listFile = Join-Path $env:TEMP "FBCTools-folders-$stamp.txt"
    $reader = @'
$folders = @()
foreach ($window in @((New-Object -ComObject Shell.Application).Windows())) {
    try {
        $path = [string] $window.Document.Folder.Self.Path
    }
    catch {
        continue
    }

    if ($path -and ($path.StartsWith('::') -or (Test-Path -LiteralPath $path -PathType Container)) -and $folders -notcontains $path) {
        $folders += $path
    }
}

[System.IO.File]::WriteAllLines('LIST_FILE', [string[]] $folders, (New-Object System.Text.UTF8Encoding $false))
'@
    $reader = $reader.Replace('LIST_FILE', $listFile.Replace("'", "''"))
    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($reader))

    $folders = @()
    try {
        $process = Start-Process -FilePath powershell.exe -ArgumentList '-NoProfile', '-NonInteractive', '-EncodedCommand', $encoded -NoNewWindow -PassThru
        if (-not $process.WaitForExit(10000)) {
            $process.Kill()
            Write-Warning 'Explorer did not answer; the open folders will not be opened again.'
        }
        elseif (Test-Path -LiteralPath $listFile) {
            $folders = @(Get-Content -LiteralPath $listFile -Encoding UTF8 | Where-Object { $_ })
        }
        else {
            Write-Warning 'The open folders could not be read; they will not be opened again.'
        }
    }
    catch {
        Write-Warning "The open folders could not be read; they will not be opened again. $($_.Exception.Message)"
    }
    finally {
        Remove-Item -LiteralPath $listFile -ErrorAction SilentlyContinue
    }

    return , $folders
}

function Open-Folders([string[]] $Folders) {
    if ($Folders.Count -eq 0) {
        return
    }

    # Give the new shell time to start; a folder opened before it is up may start a second
    # explorer.exe of its own.
    Start-Sleep -Seconds 3
    Write-Host "Opening $($Folders.Count) folder(s) again..."
    foreach ($folder in $Folders) {
        $argument = if ($folder -match '[\s,]') { "`"$folder`"" } else { $folder }
        Start-Process -FilePath explorer.exe -ArgumentList $argument
    }
}

# The fallback when the restarter cannot stop Explorer: stop it directly, copy, start it again
# and reopen the folders that were open (no positions or tabs).
function Restart-ExplorerDirectly([hashtable] $CopyParameters) {
    $folders = Get-OpenFolders
    Write-Host 'Stopping Explorer...'
    Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    try {
        & $copyScript @CopyParameters
    }
    finally {
        if (-not (Get-Process -Name explorer -ErrorAction SilentlyContinue)) {
            # Windows usually restarts the shell by itself; start it if it has not.
            Start-Process explorer.exe
        }

        Open-Folders $folders
    }
}

function Install-Build {
    Write-Step "Install to $installDir"
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
    Close-InstalledProgram $runnerName $false
    Close-InstalledProgram $editorName $true

    $installedDll = Join-Path $installDir $dllName
    # A hashtable: splatting an array to a script passes '-Source' as a value, not a name.
    $copyParameters = @{ Source = $artifacts; Target = $installDir; DllName = $dllName }
    if ($RestartExplorer) {
        # Run from artifacts, so the copy in the install folder is free to be replaced. The copy
        # runs in Windows PowerShell even when this script was started from pwsh.
        $restarter = Join-Path $artifacts "$restarterName.exe"
        $powershell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $exitCode = -1
        if (Test-Path -LiteralPath $restarter) {
            & $restarter --file $installedDll -- $powershell -NoProfile -ExecutionPolicy Bypass -File $copyScript -Source $artifacts -Target $installDir -DllName $dllName
            $exitCode = $LASTEXITCODE
        }

        switch ($exitCode) {
            0 { }
            1 { Write-Warning 'The restarter reported warnings; see above.' }
            5 { throw 'Copying the files failed; see above. Explorer was started again.' }
            default {
                Write-Warning "The restarter could not restart Explorer (exit code $exitCode); stopping it directly instead."
                Restart-ExplorerDirectly $copyParameters
            }
        }
    }
    else {
        & $copyScript @copyParameters
    }

    $registration = Start-Process -FilePath regsvr32.exe -ArgumentList '/s', "`"$installedDll`"" -Wait -PassThru
    if ($registration.ExitCode -ne 0) {
        throw "regsvr32 failed with exit code $($registration.ExitCode) for $installedDll"
    }

    Write-Host "Registered $installedDll for the current user."
}

# The config goes through the manager, not a copy: it keeps template-state.json in step, so a
# later template still replaces a menu nobody changed (ADR-0009, ADR-0010).
function Invoke-Manager([string] $Folder, [string] $Command, [string] $Title) {
    $manager = Join-Path $Folder "$managerName.exe"
    Write-Step $Title
    & $manager $Command
    switch ($LASTEXITCODE) {
        0 { }
        1 { Write-Warning 'The manager has something to report; see above.' }
        default { throw "$manager $Command exited with code $LASTEXITCODE" }
    }
}

function Copy-Config {
    $sourcePath = Join-Path $root 'samples\runner-checks.json'
    $target = Join-Path $configDir 'config.json'
    Write-Step "Copy $sourcePath to $target"
    New-Item -ItemType Directory -Force -Path $configDir | Out-Null

    if (Test-Path -LiteralPath $target) {
        $same = (Get-FileHash -LiteralPath $target).Hash -eq (Get-FileHash -LiteralPath $sourcePath).Hash
        if (-not $same) {
            $backup = Join-Path $configDir "config.$stamp.bak.json"
            Copy-Item -LiteralPath $target -Destination $backup
            Write-Host "The previous config was saved as $backup"
        }
    }

    Copy-Item -LiteralPath $sourcePath -Destination $target -Force
}

Push-Location $root
try {
    Write-Step "Build ($Configuration)"
    Invoke-Native 'dotnet' @('build', '-c', $Configuration)

    if (-not $SkipTests) {
        Write-Step 'Test'
        Invoke-Native 'dotnet' @('test', '-c', $Configuration)
    }

    Write-Step "Publish to $artifacts"
    foreach ($project in @('src\FBC.ShellExtension.ShellExt', 'src\FBC.ShellExtension.Runner', 'src\FBC.ShellExtension.Editor', 'src\FBC.ShellExtension.Restarter', 'src\FBC.ShellExtension.Manager')) {
        Invoke-Native 'dotnet' @('publish', $project, '-c', $Configuration, '-o', $artifacts)
    }

    # The menu FBCTools starts with; the manager applies it (ADR-0009).
    Copy-Item -LiteralPath $template -Destination (Join-Path $artifacts 'config.template.json') -Force

    if (-not $IncludeSymbols) {
        Get-ChildItem -LiteralPath $artifacts -Filter '*.pdb' | Remove-Item -Force
    }

    if ($Install) {
        Install-Build
    }
    elseif ($RestartExplorer) {
        Write-Warning '-RestartExplorer has no effect without -Install.'
    }

    if ($Config -eq 'checks') {
        Copy-Config
    }
    elseif ($Config -ne 'none') {
        $managerFolder = if ($Install) { $installDir } else { $artifacts }
        Invoke-Manager $managerFolder 'reset' 'Reset the config to the template'
    }
    elseif ($Install) {
        # What the editor does when it starts: create the config from the template, or bring
        # it up to date with it.
        Invoke-Manager $installDir 'check' 'Bring the config up to date with the template'
    }

    Write-Host ''
    Write-Host 'Done.' -ForegroundColor Green
}
finally {
    Pop-Location
}
