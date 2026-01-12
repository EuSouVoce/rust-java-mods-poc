#!/bin/bash
# Generate FlatBuffers code for C# and Java
# Requires flatc (FlatBuffers compiler) to be installed
# Install: https://github.com/google/flatbuffers/releases

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCHEMA_DIR="$SCRIPT_DIR"
CSHARP_OUT="$SCRIPT_DIR/../csharp-bridge/Generated"
JAVA_OUT="$SCRIPT_DIR/../java-plugin/src/main/java"

echo "=== Generating FlatBuffers Code ==="

# Check if flatc is available
if ! command -v flatc &> /dev/null; then
    echo "Error: flatc not found. Please install FlatBuffers compiler."
    echo "  Download from: https://github.com/google/flatbuffers/releases"
    echo "  Or: brew install flatbuffers (macOS)"
    echo "  Or: apt-get install flatbuffers-compiler (Debian/Ubuntu)"
    exit 1
fi

# Create output directories
mkdir -p "$CSHARP_OUT"
mkdir -p "$JAVA_OUT"

echo "Generating C# code..."
flatc --csharp -o "$CSHARP_OUT" "$SCHEMA_DIR/rust_java_mods.fbs"
echo "✓ C# code generated in $CSHARP_OUT"

echo "Generating Java code..."
flatc --java -o "$JAVA_OUT" "$SCHEMA_DIR/rust_java_mods.fbs"
echo "✓ Java code generated in $JAVA_OUT"

echo ""
echo "=== FlatBuffers Code Generation Complete ==="
echo ""
echo "Generated files:"
echo "  C#:   $CSHARP_OUT/RustJavaMods/Protocol/*.cs"
echo "  Java: $JAVA_OUT/RustJavaMods/Protocol/*.java"
