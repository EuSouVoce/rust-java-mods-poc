@echo off
REM Windows batch script for building Rust-Java Mods Framework
REM For bash users, use build.sh instead

setlocal enabledelayedexpansion

set PROJECT_ROOT=%~dp0
set PROJECT_ROOT=%PROJECT_ROOT:~0,-1%

echo === Building Rust Game Java Modding Framework ===
echo     Protocol: FlatBuffers (zero-copy)
echo.

REM Step 1: Generate FlatBuffers code (if flatc available)
echo Generating FlatBuffers code...

set FLATC_EXE=
if exist "%PROJECT_ROOT%\tools\flatc\flatc.exe" set "FLATC_EXE=%PROJECT_ROOT%\tools\flatc\flatc.exe"
if "%FLATC_EXE%"=="" (
    where flatc >nul 2>&1
    if !ERRORLEVEL! EQU 0 (
        for /f "delims=" %%I in ('where flatc') do set "FLATC_EXE=%%I"
    )
)

set NEED_CODEGEN=0
if not exist "%PROJECT_ROOT%\csharp-bridge\Generated" set NEED_CODEGEN=1

if "%FLATC_EXE%"=="" (
    echo Warning: flatc not found - skipping code generation
    set /p DL_FLATC=Download flatc (FlatBuffers compiler) to tools\flatc now? [Y/n]: 
    if "%DL_FLATC%"=="" set DL_FLATC=Y
    if /I "%DL_FLATC%"=="Y" (
        if not exist "%PROJECT_ROOT%\tools\flatc" mkdir "%PROJECT_ROOT%\tools\flatc"
        set FLATC_VER=23.5.26
        set FLATC_URL=https://github.com/google/flatbuffers/releases/download/v!FLATC_VER!/Windows.flatc.binary.zip
        echo Downloading flatc...
        powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Invoke-WebRequest -Uri '!FLATC_URL!' -OutFile '%PROJECT_ROOT%\\tools\\flatc.zip' -UseBasicParsing } catch { exit 1 }"
        if !ERRORLEVEL! EQU 0 (
            powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive '%PROJECT_ROOT%\\tools\\flatc.zip' -DestinationPath '%PROJECT_ROOT%\\tools\\flatc' -Force"
            del /q "%PROJECT_ROOT%\tools\flatc.zip" >nul 2>&1
            if exist "%PROJECT_ROOT%\tools\flatc\flatc.exe" set "FLATC_EXE=%PROJECT_ROOT%\tools\flatc\flatc.exe"
        )
    )
)

if "%FLATC_EXE%"=="" (
    if "%NEED_CODEGEN%"=="1" (
        echo.
        echo ERROR: FlatBuffers-generated C# sources are missing.
        echo        Install/download flatc and re-run build to generate schema bindings.
        echo.
        exit /b 1
    )
)

if not "%FLATC_EXE%"=="" (
    set "PATH=%PROJECT_ROOT%\tools\flatc;!PATH!"
    pushd "%PROJECT_ROOT%\schema" >nul
    call generate.bat
    popd >nul
)
echo.

REM Step 2: Build C# Carbon plugin
echo Building C# Carbon plugin...
cd csharp-bridge
dotnet build --configuration Release
if %ERRORLEVEL% NEQ 0 (
    cd ..
    echo ERROR: C# bridge build failed
    exit /b 1
)
cd ..
echo Done.
echo.

REM Step 3: Build Java API
echo Building Java API...
cd java-plugin
call mvn clean package -q
cd ..
echo Done.
echo.

echo === Build Complete ===
echo.
echo Artifacts:
echo   - Carbon Plugin: csharp-bridge\bin\Release\net472\RustJavaBridge.dll
echo   - Java API: java-plugin\target\rust-java-mods-api-0.1.0.jar
echo.
echo Protocol: FlatBuffers (zero-copy serialization)
echo.
echo Deployment:
echo   1. Copy RustJavaBridge.dll to your Rust server's carbon\plugins\ folder
echo   2. Restart your Rust server or use Carbon's hot-reload: c.reload RustJavaBridge
echo   3. Run your Java mod with the Java API jar in classpath
