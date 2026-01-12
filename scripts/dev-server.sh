#!/bin/bash
# Development server script for Rust-Java Modding Framework
# Sets up a local dev environment with file watching

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
RUST_SERVER_PATH="${RUST_SERVER_PATH:-$PROJECT_ROOT/dev-server}"

echo "=== Rust-Java Mods Dev Server ==="
echo ""

# Check prerequisites
check_prereqs() {
    echo "Checking prerequisites..."
    
    if ! command -v dotnet &> /dev/null; then
        echo "❌ dotnet not found. Install .NET SDK 6.0+"
        exit 1
    fi
    
    if ! command -v mvn &> /dev/null; then
        echo "❌ Maven not found. Install Apache Maven 3.6+"
        exit 1
    fi
    
    if ! command -v java &> /dev/null; then
        echo "❌ Java not found. Install JDK 17+"
        exit 1
    fi
    
    echo "✓ All prerequisites found"
    echo ""
}

# Build everything
build_all() {
    echo "Building all components..."
    cd "$PROJECT_ROOT"
    bash ./build.sh
    echo ""
}

# Deploy Carbon plugin
deploy_plugin() {
    local target_dir="$RUST_SERVER_PATH/carbon/plugins"
    
    if [[ -d "$target_dir" ]]; then
        echo "Deploying Carbon plugin to $target_dir..."
        cp "$PROJECT_ROOT/csharp-bridge/bin/Release/net472/RustJavaBridge.dll" "$target_dir/"
        echo "✓ Plugin deployed"
        echo ""
        echo "Run 'c.reload RustJavaBridge' in server console to reload"
    else
        echo "⚠ Rust server path not found: $target_dir"
        echo "  Set RUST_SERVER_PATH environment variable to your server directory"
        echo "  Or create a dev-server folder in the project root"
    fi
    echo ""
}

# Main
check_prereqs
build_all
deploy_plugin

echo "=== Dev Server Ready ==="
echo ""
echo "Next steps:"
echo "  1. Start your Rust server (with Carbon installed)"
echo "  2. Run ./scripts/watch-java.sh in another terminal for Java HMR"
echo "  3. Edit Java plugin code - it will auto-reload"
echo ""
echo "Useful commands:"
echo "  c.reload RustJavaBridge    - Reload Carbon plugin in server"
echo "  c.plugins                   - List loaded Carbon plugins"
echo "  tail -f carbon/logs/carbon.log - Watch Carbon logs"
