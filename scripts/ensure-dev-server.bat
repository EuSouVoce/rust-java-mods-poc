@echo off
REM Ensure dev-server has Rust + Carbon and populate csharp-bridge\lib dependencies
REM This script DOES NOT start the server.

setlocal enabledelayedexpansion

cd /d "%~dp0"
cd ..
set PROJECT_ROOT=%cd%
set DEV_SERVER=%PROJECT_ROOT%\dev-server
set TOOLS_DIR=%PROJECT_ROOT%\tools

if not exist "%TOOLS_DIR%" mkdir "%TOOLS_DIR%"
if not exist "%DEV_SERVER%" mkdir "%DEV_SERVER%"

echo.
echo ====================================================================
echo      Ensuring dev-server prerequisites (Rust + Carbon + DLLs)
echo ====================================================================
echo.

REM -------------------------
REM SteamCMD
REM -------------------------
echo Checking SteamCMD...
set STEAMCMD_EXE=

where steamcmd.exe >nul 2>&1
if %ERRORLEVEL% EQU 0 set STEAMCMD_EXE=steamcmd.exe

if "%STEAMCMD_EXE%"=="" (
    if exist "%TOOLS_DIR%\steamcmd\steamcmd.exe" set STEAMCMD_EXE=%TOOLS_DIR%\steamcmd\steamcmd.exe
)

if "%STEAMCMD_EXE%"=="" (
    set /p DL_STEAM=SteamCMD not found. Download to tools\steamcmd? [Y/n]: 
    if "%DL_STEAM%"=="" set DL_STEAM=Y

    if /I "%DL_STEAM%"=="Y" (
        if not exist "%TOOLS_DIR%\steamcmd" mkdir "%TOOLS_DIR%\steamcmd"
        set STEAM_URL=https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip
        echo Downloading SteamCMD...
        powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Invoke-WebRequest -Uri '%STEAM_URL%' -OutFile '%TOOLS_DIR%\\steamcmd.zip' -UseBasicParsing } catch { exit 1 }"
        if !ERRORLEVEL! NEQ 0 (
            echo ERROR: Failed to download SteamCMD
            exit /b 1
        )
        echo Extracting SteamCMD...
        powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive '%TOOLS_DIR%\\steamcmd.zip' -DestinationPath '%TOOLS_DIR%\\steamcmd' -Force"
        del /q "%TOOLS_DIR%\steamcmd.zip" >nul 2>&1
        if exist "%TOOLS_DIR%\steamcmd\steamcmd.exe" (
            set STEAMCMD_EXE=%TOOLS_DIR%\steamcmd\steamcmd.exe
        )
    )
)

if "%STEAMCMD_EXE%"=="" (
    echo ERROR: SteamCMD not available
    exit /b 1
)

echo OK - %STEAMCMD_EXE%
echo.

REM -------------------------
REM Carbon
REM -------------------------
if not exist "%DEV_SERVER%\carbon" (
    set /p DL_CARBON=Carbon not found in dev-server. Download Carbon automatically? [Y/n]: 
    if "%DL_CARBON%"=="" set DL_CARBON=Y

    if /I "%DL_CARBON%"=="Y" (
        set CARBON_TAG=%CARBON_TAG%
        if "%CARBON_TAG%"=="" set CARBON_TAG=preview

        set CARBON_BUILD=Debug
        if /I "%CARBON_TAG%"=="production" set CARBON_BUILD=Release

        set CARBON_URL=https://github.com/CarbonCommunity/Carbon.Core/releases/download/%CARBON_TAG%_build/Carbon.Windows.%CARBON_BUILD%.zip
        echo Downloading Carbon (%CARBON_TAG% / %CARBON_BUILD%)...
        powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Invoke-WebRequest -Uri '%CARBON_URL%' -OutFile '%PROJECT_ROOT%\\carbon.zip' -UseBasicParsing } catch { exit 1 }"
        if !ERRORLEVEL! NEQ 0 (
            echo ERROR: Failed to download Carbon
            exit /b 1
        )

        echo Extracting Carbon into dev-server...
        powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive '%PROJECT_ROOT%\\carbon.zip' -DestinationPath '%DEV_SERVER%' -Force"
        del /q "%PROJECT_ROOT%\carbon.zip" >nul 2>&1
    )
)

if not exist "%DEV_SERVER%\carbon" (
    echo ERROR: Carbon not installed under dev-server\carbon
    exit /b 1
)

echo OK - Carbon present
echo.

REM -------------------------
REM Rust server
REM -------------------------
if not exist "%DEV_SERVER%\RustDedicated.exe" (
    echo Installing Rust server (this can take several minutes)...
    "%STEAMCMD_EXE%" +force_install_dir "%DEV_SERVER%" +login anonymous +app_update 258550 validate +quit
) else (
    echo Updating Rust server...
    "%STEAMCMD_EXE%" +force_install_dir "%DEV_SERVER%" +login anonymous +app_update 258550 validate +quit
)

if not exist "%DEV_SERVER%\RustDedicated.exe" (
    echo ERROR: Rust server installation failed
    exit /b 1
)

echo OK - Rust server present
echo.

REM -------------------------
REM Copy required DLLs for C# build
REM -------------------------
set LIB_DIR=%PROJECT_ROOT%\csharp-bridge\lib
if not exist "%LIB_DIR%" mkdir "%LIB_DIR%"

set MANAGED_DIR=%DEV_SERVER%\RustDedicated_Data\Managed
set CARBON_MANAGED=%DEV_SERVER%\carbon\managed

echo Populating csharp-bridge\lib dependencies...

set MISSING_LIBS=0

call :copy_carbon_dll "Carbon.Common.dll" "%LIB_DIR%\Carbon.Common.dll"
call :copy_carbon_dll "Carbon.Core.dll" "%LIB_DIR%\Carbon.Core.dll"
call :copy_if_exists "%MANAGED_DIR%\Facepunch.Rust.dll" "%LIB_DIR%\Facepunch.Rust.dll"
call :copy_if_exists "%MANAGED_DIR%\Assembly-CSharp.dll" "%LIB_DIR%\Assembly-CSharp.dll"
call :copy_if_exists "%MANAGED_DIR%\UnityEngine.CoreModule.dll" "%LIB_DIR%\UnityEngine.CoreModule.dll"

if !MISSING_LIBS! NEQ 0 (
    echo.
    echo ERROR: One or more required DLLs were not found.
    echo Expected locations:
    echo   - %CARBON_MANAGED%\Carbon.Common.dll
    echo   - %CARBON_MANAGED%\Carbon.Core.dll
    echo   - %MANAGED_DIR%\Facepunch.Rust.dll
    echo   - %MANAGED_DIR%\Assembly-CSharp.dll
    echo   - %MANAGED_DIR%\UnityEngine.CoreModule.dll
    exit /b 1
)

echo OK - csharp-bridge\lib ready
echo.

echo Done.
exit /b 0

:copy_if_exists
set "SRC=%~1"
set "DST=%~2"
if exist "%SRC%" (
    copy /y "%SRC%" "%DST%" >nul
) else (
    echo MISSING: %SRC%
    set /a MISSING_LIBS+=1
)
exit /b 0

:copy_carbon_dll
set "NAME=%~1"
set "DST=%~2"

set "SRC=%DEV_SERVER%\carbon\managed\%NAME%"
if exist "%SRC%" (
    copy /y "%SRC%" "%DST%" >nul
    exit /b 0
)

REM Fallback: search anywhere under dev-server\carbon
for /r "%DEV_SERVER%\carbon" %%F in (%NAME%) do (
    copy /y "%%F" "%DST%" >nul
    exit /b 0
)

echo MISSING: %DEV_SERVER%\carbon\**\%NAME%
set /a MISSING_LIBS+=1
exit /b 0
