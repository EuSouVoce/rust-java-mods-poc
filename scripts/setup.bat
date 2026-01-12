@echo off
REM One-Click Development Environment Setup (Windows)
REM Sets up everything needed for Rust-Java Mods development

setlocal enabledelayedexpansion

cd /d "%~dp0"
cd ..
set PROJECT_ROOT=%cd%
set TOOLS_DIR=%PROJECT_ROOT%\tools

if not exist "%TOOLS_DIR%" mkdir "%TOOLS_DIR%"

REM Prefer locally downloaded tools
if exist "%TOOLS_DIR%\steamcmd\steamcmd.exe" set PATH=%TOOLS_DIR%\steamcmd;%PATH%
if exist "%TOOLS_DIR%\flatc\flatc.exe" set PATH=%TOOLS_DIR%\flatc;%PATH%

echo.
echo ====================================================================
echo     Rust-Java Mods Development Environment Setup (Windows)
echo ====================================================================
echo.

REM Check prerequisites (with optional auto-install)
echo Checking prerequisites...
set MISSING=0

call :ensure_java
call :ensure_maven
call :ensure_dotnet
call :ensure_flatc
call :ensure_steamcmd

echo.

if !MISSING! EQU 1 (
    echo.
    echo Missing required tools. Please install them and try again.
    pause
    exit /b 1
)

REM Ensure dev-server and required DLLs for C# compilation
echo Ensuring dev-server dependencies (Rust + Carbon + DLLs)...
call "%PROJECT_ROOT%\scripts\ensure-dev-server.bat"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to prepare dev-server dependencies
    pause
    exit /b 1
)

REM Build the framework
echo Building Rust-Java Mods Framework...
cd /d "%PROJECT_ROOT%"
call build.bat
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)
echo.

REM Create dev server structure
set DEV_SERVER=%PROJECT_ROOT%\dev-server

echo Setting up development server directories...
if not exist "%DEV_SERVER%\carbon\plugins" mkdir "%DEV_SERVER%\carbon\plugins"
if not exist "%DEV_SERVER%\carbon\configs" mkdir "%DEV_SERVER%\carbon\configs"
if not exist "%DEV_SERVER%\carbon\logs" mkdir "%DEV_SERVER%\carbon\logs"
if not exist "%DEV_SERVER%\logs" mkdir "%DEV_SERVER%\logs"
echo OK
echo.

REM Deploy Carbon bridge
echo Deploying Carbon bridge plugin...
set BRIDGE_DLL=%PROJECT_ROOT%\csharp-bridge\bin\Release\net472\RustJavaBridge.dll
if exist "%BRIDGE_DLL%" (
    copy "%BRIDGE_DLL%" "%DEV_SERVER%\carbon\plugins\" >nul 2>&1
    echo OK
) else (
    echo WARNING - Plugin not yet built (expected on first run^)
)
echo.

REM Show next steps
echo.
echo ====================================================================
echo                    Setup Complete! ^^!
echo ====================================================================
echo.
echo Next Steps:
echo.
echo 1. Start Development Server (Command Prompt):
echo    scripts\start-dev.bat
echo.
echo 2. Watch Java Files for Changes (Another Command Prompt):
echo    scripts\watch-java.sh
echo    (or use WSL/Git Bash for this)
echo.
echo 3. Edit Java Files:
echo    Files in java-plugin\src will auto-rebuild and restart
echo.
echo 4. Rebuild C# Bridge (if needed):
echo    scripts\rebuild-bridge.sh
echo.
echo Important:
echo   * Keep multiple command prompts open
echo   * Dev server at: %DEV_SERVER%
echo   * Logs at: %DEV_SERVER%\logs\
echo.
echo For more info:
echo   Read: %PROJECT_ROOT%\DOCUMENTATION_INDEX.md
echo.
pause

exit /b 0

REM =========================
REM Helper functions
REM =========================

:prompt_yes_no
REM Args: question, default(Y/N), outVar
set "Q=%~1"
set "DEF=%~2"
set "OUT=%~3"
:prompt_yes_no_loop
set "ANS="
set /p "ANS=%Q% [%DEF%/n]: "
if "%ANS%"=="" set "ANS=%DEF%"
if /I "%ANS%"=="Y" (set "%OUT%=1" & exit /b 0)
if /I "%ANS%"=="N" (set "%OUT%=0" & exit /b 0)
echo Please answer Y or N.
goto prompt_yes_no_loop

:have_winget
where winget >nul 2>&1
if %ERRORLEVEL% EQU 0 (exit /b 0) else (exit /b 1)

:ensure_java
where java >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo OK - Java found
    exit /b 0
)

echo ERROR: Java not found
call :have_winget
if %ERRORLEVEL% NEQ 0 (
    echo   Install JDK 21+ from: https://adoptium.net/
    set MISSING=1
    exit /b 0
)

call :prompt_yes_no "Auto-install JDK 21 (Temurin) using winget?" Y DO_INSTALL
if !DO_INSTALL! EQU 1 (
    winget install --id EclipseAdoptium.Temurin.21.JDK -e --accept-package-agreements --accept-source-agreements
    where java >nul 2>&1
    if !ERRORLEVEL! EQU 0 (
        echo OK - Java installed
        exit /b 0
    )
)

echo   Install JDK 21+ from: https://adoptium.net/
set MISSING=1
exit /b 0

:ensure_maven
where mvn >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo OK - Maven found
    exit /b 0
)

echo ERROR: Maven not found
call :have_winget
if %ERRORLEVEL% NEQ 0 (
    echo   Install Maven 3.8+ from: https://maven.apache.org/download.cgi
    set MISSING=1
    exit /b 0
)

call :prompt_yes_no "Auto-install Maven using winget?" Y DO_INSTALL
if !DO_INSTALL! EQU 1 (
    winget install --id Apache.Maven -e --accept-package-agreements --accept-source-agreements
    where mvn >nul 2>&1
    if !ERRORLEVEL! EQU 0 (
        echo OK - Maven installed
        exit /b 0
    )
)

echo   Install Maven 3.8+ from: https://maven.apache.org/download.cgi
set MISSING=1
exit /b 0

:ensure_dotnet
where dotnet >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo OK - .NET SDK found
    exit /b 0
)

echo ERROR: .NET SDK not found
call :have_winget
if %ERRORLEVEL% NEQ 0 (
    echo   Install .NET 6.0+ from: https://dotnet.microsoft.com/download
    set MISSING=1
    exit /b 0
)

call :prompt_yes_no "Auto-install .NET SDK using winget?" Y DO_INSTALL
if !DO_INSTALL! EQU 1 (
    winget install --id Microsoft.DotNet.SDK.8 -e --accept-package-agreements --accept-source-agreements
    where dotnet >nul 2>&1
    if !ERRORLEVEL! EQU 0 (
        echo OK - .NET SDK installed
        exit /b 0
    )
)

echo   Install .NET 6.0+ from: https://dotnet.microsoft.com/download
set MISSING=1
exit /b 0

:ensure_flatc
where flatc >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo OK - flatc found (optional, for schema changes)
    exit /b 0
)

echo WARNING - flatc not found (optional, only needed to modify schemas^)
call :prompt_yes_no "Download flatc (FlatBuffers compiler) to tools\\flatc?" Y DO_DL
if !DO_DL! NEQ 1 exit /b 0

set FLATC_VER=23.5.26
set FLATC_URL=https://github.com/google/flatbuffers/releases/download/v%FLATC_VER%/Windows.flatc.binary.zip
set FLATC_DIR=%TOOLS_DIR%\flatc
if not exist "%FLATC_DIR%" mkdir "%FLATC_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Invoke-WebRequest -Uri '%FLATC_URL%' -OutFile '%TOOLS_DIR%\\flatc.zip' -UseBasicParsing } catch { exit 1 }"
if %ERRORLEVEL% NEQ 0 (
    echo WARNING - failed to download flatc from GitHub. Install manually: https://github.com/google/flatbuffers/releases
    exit /b 0
)
powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive '%TOOLS_DIR%\\flatc.zip' -DestinationPath '%FLATC_DIR%' -Force"
del /q "%TOOLS_DIR%\flatc.zip" >nul 2>&1

if exist "%FLATC_DIR%\flatc.exe" (
    set "PATH=%FLATC_DIR%;!PATH!"
    echo OK - flatc downloaded
) else (
    echo WARNING - flatc download extracted but flatc.exe not found
)
exit /b 0

:ensure_steamcmd
where steamcmd.exe >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo OK - SteamCMD found
    exit /b 0
)

if exist "%TOOLS_DIR%\steamcmd\steamcmd.exe" (
    set PATH=%TOOLS_DIR%\steamcmd;%PATH%
    echo OK - SteamCMD found (tools)
    exit /b 0
)

echo WARNING - SteamCMD not found (needed to download/update Rust server^)
call :prompt_yes_no "Download SteamCMD to tools\\steamcmd?" Y DO_DL
if !DO_DL! NEQ 1 exit /b 0

set STEAM_URL=https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip
set STEAM_DIR=%TOOLS_DIR%\steamcmd
if not exist "%STEAM_DIR%" mkdir "%STEAM_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Invoke-WebRequest -Uri '%STEAM_URL%' -OutFile '%TOOLS_DIR%\\steamcmd.zip' -UseBasicParsing } catch { exit 1 }"
if %ERRORLEVEL% NEQ 0 (
    echo WARNING - failed to download SteamCMD. Install manually: https://steamcmd.net/
    exit /b 0
)
powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive '%TOOLS_DIR%\\steamcmd.zip' -DestinationPath '%STEAM_DIR%' -Force"
del /q "%TOOLS_DIR%\steamcmd.zip" >nul 2>&1

if exist "%STEAM_DIR%\steamcmd.exe" (
    set "PATH=%STEAM_DIR%;!PATH!"
    echo OK - SteamCMD downloaded
) else (
    echo WARNING - SteamCMD download extracted but steamcmd.exe not found
)
exit /b 0
