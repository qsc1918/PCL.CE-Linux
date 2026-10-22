@echo off
rem PCL.CE-Linux 一键构建（Windows 双击版）
rem
rem   tools\build-release.cmd                  全部目标 + 四合一 zip
rem   tools\build-release.cmd --linux          只要 Linux
rem   tools\build-release.cmd --windows        只要 Windows
rem   tools\build-release.cmd --single         只要单文件版
rem   tools\build-release.cmd --flat           只要非单文件版
rem   tools\build-release.cmd --no-bundle      不打 zip
rem
rem 产物：dist\linux-x64-single\PCL、dist\win-x64-single\PCL.exe、
rem       dist\release\PCL-CE-<版本>-all.zip

setlocal EnableDelayedExpansion
cd /d "%~dp0\.."

rem ---- 有 bash 就直接用主脚本（推荐，逻辑只维护一份）----
where bash >nul 2>nul
if %errorlevel%==0 (
  bash "%~dp0build-release.sh" %*
  exit /b %errorlevel%
)

rem ---- 没有 bash 时的纯 cmd 实现 ----
set PROJ=src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj
set META=src\Plain Craft Launcher 2\metadata.json
set LOG=dist\_build.log
if not exist dist mkdir dist
if not exist dist\release mkdir dist\release
type nul > "%LOG%"

set BUILD_LINUX=0
set BUILD_WINDOWS=0
set WANT_SINGLE=1
set WANT_FLAT=1
set BUNDLE=1
if "%~1"=="" (set BUILD_LINUX=1& set BUILD_WINDOWS=1)
:parse
if "%~1"=="" goto done_parse
if /i "%~1"=="--linux"     set BUILD_LINUX=1
if /i "%~1"=="--windows"   set BUILD_WINDOWS=1
if /i "%~1"=="--single"    (set WANT_SINGLE=1& set WANT_FLAT=0& set BUILD_LINUX=1& set BUILD_WINDOWS=1)
if /i "%~1"=="--flat"      (set WANT_FLAT=1& set WANT_SINGLE=0& set BUILD_LINUX=1& set BUILD_WINDOWS=1)
if /i "%~1"=="--no-bundle" set BUNDLE=0
shift
goto parse
:done_parse
if "%BUILD_LINUX%%BUILD_WINDOWS%"=="00" (set BUILD_LINUX=1& set BUILD_WINDOWS=1)

rem ---- 版本号 ----
for /f "usebackq delims=" %%v in (`python -c "import json;m=json.load(open(r'%META%',encoding='utf-8'));v=m['version'];print(v['base']+(v.get('suffix') or '')+('-'+str(v['code']) if v.get('code') else ''))"`) do set VER=%%v
echo version: %VER%

rem ---- Linux ----
if "%BUILD_LINUX%"=="1" (
  if "%WANT_SINGLE%"=="1" (
    echo ^>^> Linux single-file
    dotnet publish "%PROJ%" -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o dist\linux-x64-single >> "%LOG%" 2>&1
    if errorlevel 1 goto fail
    del /q dist\linux-x64-single\*.pdb 2>nul
  )
  if "%WANT_FLAT%"=="1" (
    echo ^>^> Linux flat
    dotnet publish "%PROJ%" -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -o dist\linux-x64-flat >> "%LOG%" 2>&1
    if errorlevel 1 goto fail
    del /q dist\linux-x64-flat\*.pdb 2>nul
  )
)

rem ---- Windows ----
if "%BUILD_WINDOWS%"=="1" (
  if "%WANT_SINGLE%"=="1" (
    echo ^>^> Windows single-file
    dotnet publish "%PROJ%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist\win-x64-single >> "%LOG%" 2>&1
    if errorlevel 1 goto fail
    del /q dist\win-x64-single\*.pdb 2>nul
  )
  if "%WANT_FLAT%"=="1" (
    echo ^>^> Windows flat
    dotnet publish "%PROJ%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o dist\win-x64-flat >> "%LOG%" 2>&1
    if errorlevel 1 goto fail
    del /q dist\win-x64-flat\*.pdb 2>nul
  )
)

rem ---- 四合一 zip ----
if "%BUNDLE%"=="1" if "%BUILD_LINUX%%BUILD_WINDOWS%%WANT_SINGLE%%WANT_FLAT%"=="1111" (
  echo ^>^> bundling all four into one zip
  if exist dist\_bundle rmdir /s /q dist\_bundle
  mkdir "dist\_bundle\PCL-CE-%VER%"
  xcopy /e /i /q /y dist\win-x64-single   "dist\_bundle\PCL-CE-%VER%\windows-x64-single" >nul
  xcopy /e /i /q /y dist\win-x64-flat     "dist\_bundle\PCL-CE-%VER%\windows-x64-flat"   >nul
  xcopy /e /i /q /y dist\linux-x64-single "dist\_bundle\PCL-CE-%VER%\linux-x64-single"   >nul
  xcopy /e /i /q /y dist\linux-x64-flat   "dist\_bundle\PCL-CE-%VER%\linux-x64-flat"     >nul
  python tools\zipdir.py "dist\release\PCL-CE-%VER%-all.zip" "dist\_bundle\PCL-CE-%VER%"
  if errorlevel 1 goto fail
  rmdir /s /q dist\_bundle
)

echo.
echo ================ build done ================
if exist dist\release\*.zip dir /b dist\release
echo log: %LOG%
exit /b 0

:fail
echo !!! build failed, tail of %LOG%:
powershell -NoProfile -Command "Get-Content -Tail 30 '%LOG%'"
exit /b 1
