@echo off
rem PCL.CE-Linux 一键构建脚本（Windows 版）
rem
rem 用法：
rem   tools\build-release.cmd                全部目标
rem   tools\build-release.cmd --linux        只要 Linux
rem   tools\build-release.cmd --windows      只要 Windows
rem   tools\build-release.cmd --single       只要单文件版（发版用）
rem   tools\build-release.cmd --flat         只要非单文件版（排查用）
rem
rem 产物：dist\linux-x64-single\PCL、dist\win-x64-single\PCL.exe、dist\release\*.tar.gz

setlocal EnableDelayedExpansion
cd /d "%~dp0\.."

where bash >nul 2>nul
if %errorlevel%==0 (
  bash "%~dp0build-release.sh" %*
  exit /b %errorlevel%
)

rem 没有 bash 时退回纯 cmd 实现
set PROJ=src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj
set LOG=dist\_build.log
if not exist dist mkdir dist
if not exist dist\release mkdir dist\release
type nul > "%LOG%"

set BUILD_LINUX=0
set BUILD_WINDOWS=0
set WANT_SINGLE=1
set WANT_FLAT=1
if "%~1"=="" (set BUILD_LINUX=1& set BUILD_WINDOWS=1)
:parse
if "%~1"=="" goto done_parse
if /i "%~1"=="--linux" (set BUILD_LINUX=1)
if /i "%~1"=="--windows" (set BUILD_WINDOWS=1)
if /i "%~1"=="--single" (set WANT_SINGLE=1& set WANT_FLAT=0)
if /i "%~1"=="--flat" (set WANT_FLAT=1& set WANT_SINGLE=0)
shift
goto parse
:done_parse

if "%BUILD_LINUX%%BUILD_WINDOWS%"=="00" (set BUILD_LINUX=1& set BUILD_WINDOWS=1)

rem ---- Linux ----
if "%BUILD_LINUX%"=="1" (
  if "%WANT_SINGLE%"=="1" (
    echo ^>^> Linux single-file ^(linux-x64^)
    dotnet publish "%PROJ%" -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o dist\linux-x64-single >> "%LOG%" 2>&1
    if errorlevel 1 goto fail
    del /q dist\linux-x64-single\*.pdb 2>nul
    tar czf dist\release\PCL-Linux-x64-single.tar.gz -C dist linux-x64-single
  )
  if "%WANT_FLAT%"=="1" (
    echo ^>^> Linux flat ^(linux-x64^)
    dotnet publish "%PROJ%" -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -o dist\linux-x64-flat >> "%LOG%" 2>&1
    if errorlevel 1 goto fail
  )
)

rem ---- Windows ----
if "%BUILD_WINDOWS%"=="1" (
  if "%WANT_SINGLE%"=="1" (
    echo ^>^> Windows single-file ^(win-x64^)
    dotnet publish "%PROJ%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist\win-x64-single >> "%LOG%" 2>&1
    if errorlevel 1 goto fail
    del /q dist\win-x64-single\*.pdb 2>nul
  )
  if "%WANT_FLAT%"=="1" (
    echo ^>^> Windows flat ^(win-x64^)
    dotnet publish "%PROJ%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o dist\win-x64-flat >> "%LOG%" 2>&1
    if errorlevel 1 goto fail
  )
)

echo.
echo ================ build done ================
for %%D in (dist\linux-x64-single dist\linux-x64-flat dist\win-x64-single dist\win-x64-flat) do (
  if exist "%%D" echo   %%D
)
if exist dist\release\*.tar.gz dir /b dist\release
echo log: %LOG%
exit /b 0

:fail
echo !!! build failed, tail of %LOG%:
powershell -NoProfile -Command "Get-Content -Tail 30 '%LOG%'"
exit /b 1
