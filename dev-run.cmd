@echo off
rem Developer loop: build, test, publish, and optionally install and copy a config.
rem Arguments go to scripts\dev-run.ps1, e.g.: dev-run.cmd -Install -RestartExplorer -Config checks
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\dev-run.ps1" %*
exit /b %ERRORLEVEL%
