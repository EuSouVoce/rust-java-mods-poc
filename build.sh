#!/bin/bash
# Full build script for Rust-Java Modding Framework (FlatBuffers version)
# Builds schema, C# bridge, and Java API

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== Building Rust Game Java Modding Framework ==="
echo "    Protocol: FlatBuffers (zero-copy)"
echo ""

# Step 1: Generate FlatBuffers code
echo "📦 Generating FlatBuffers code..."
cd "$SCRIPT_DIR/schema"
if command -v flatc &> /dev/null; then
    bash ./generate.sh
else
    echo "⚠ flatc not found - skipping code generation"
    echo "  Install: https://github.com/google/flatbuffers/releases"
fi
cd "$SCRIPT_DIR"
echo ""

# Step 2: Build C# Carbon plugin
echo "📦 Building C# Carbon plugin..."
cd csharp-bridge
dotnet build --configuration Release
cd ..
echo "✓ Carbon plugin built"
echo ""

# Step 3: Build Java API
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
echo "Protocol: FlatBuffers (zero-copy serialization)"
echo ""
echo "Deployment:"
echo "  1. Copy RustJavaBridge.dll to your Rust server's carbon/plugins/ folder"
echo "  2. Restart your Rust server or use Carbon's hot-reload: c.reload RustJavaBridge"
echo "  3. Run your Java mod with the Java API jar in classpath"
echo ""
echo "Development:"
echo "  - Use ./scripts/dev-server.sh to run local dev server"
echo "  - Use ./scripts/watch-java.sh for Java plugin hot-reload"
