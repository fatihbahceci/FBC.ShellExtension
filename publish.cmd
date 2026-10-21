@echo off
rem Builds the portable FBCTools zip for end users into artifacts\dist.
rem Arguments go to scripts\publish.ps1, e.g.: publish.cmd -Editor FrameworkDependent
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\publish.ps1" %*
exit /b %ERRORLEVEL%
