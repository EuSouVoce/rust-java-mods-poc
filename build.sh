#!/bin/bash
# Build script for Rust-Java Modding Framework (Carbon-based)

set -e

echo "=== Building Rust Game Java Modding Framework (Carbon) ==="
echo ""

# Build C# Carbon plugin
echo "📦 Building C# Carbon plugin..."
cd csharp-bridge
dotnet build --configuration Release
cd ..
echo "✓ Carbon plugin built"
echo ""

# Build Java API
echo "📦 Building Java API..."
cd java-plugin
mvn clean package -q
cd ..
echo "✓ Java API built"
echo ""

echo "=== Build Complete ==="
echo ""
echo "Artifacts:"
echo "  - Carbon Plugin: csharp-bridge/bin/Release/net472/RustJavaBridge.dll"
echo "  - Java API: java-plugin/target/rust-java-mods-api-0.1.0.jar"
echo ""
echo "Deployment:"
echo "  1. Copy RustJavaBridge.dll to your Rust server's carbon/plugins/ folder"
echo "  2. Restart your Rust server or use Carbon's hot-reload"
echo "  3. Run your Java mod with the Java API jar in classpath"
