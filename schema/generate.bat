@echo off
REM Windows batch script for generating FlatBuffers code

echo Generating FlatBuffers code...

where flatc >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Error: flatc not found. Please install FlatBuffers compiler.
    echo Download from: https://github.com/google/flatbuffers/releases
    exit /b 1
)

REM Create output directories
if not exist "..\csharp-bridge\Generated" mkdir "..\csharp-bridge\Generated"

echo Generating C# code...
flatc --csharp -o "..\csharp-bridge\Generated" rust_java_mods.fbs
echo Done.

echo Generating Java code...
flatc --java -o "..\java-plugin\src\main\java" rust_java_mods.fbs
echo Done.

echo.
echo === FlatBuffers Code Generation Complete ===
echo.
echo Generated files:
echo   C#:   ..\csharp-bridge\Generated\RustJavaMods\Protocol\*.cs
echo   Java: ..\java-plugin\src\main\java\RustJavaMods\Protocol\*.java
