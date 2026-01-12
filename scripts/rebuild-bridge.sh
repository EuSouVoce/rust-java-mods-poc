#!/bin/bash
# Rebuild and deploy Carbon bridge plugin
# Use after making changes to C# code

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DEV_SERVER="${DEV_SERVER:-$PROJECT_ROOT/dev-server}"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${BLUE}"
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║              Rebuilding Carbon Bridge Plugin                   ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""

# Check dotnet
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ dotnet not found${NC}"
    echo "Install .NET 6.0+ from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Build C# plugin
echo -e "${YELLOW}Building C# Carbon plugin...${NC}"
cd "$PROJECT_ROOT/csharp-bridge"

if dotnet build --configuration Release 2>&1 | grep -E "error|Error|ERROR" > /dev/null; then
    echo -e "${RED}❌ Build failed${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Build successful${NC}"
echo ""

# Deploy if server path exists
TARGET_DIR="$DEV_SERVER/carbon/plugins"
PLUGIN_DLL="$PROJECT_ROOT/csharp-bridge/bin/Release/net472/RustJavaBridge.dll"

if [[ -d "$TARGET_DIR" ]]; then
    echo -e "${YELLOW}Deploying to $TARGET_DIR...${NC}"
    if cp "$PLUGIN_DLL" "$TARGET_DIR/"; then
        echo -e "${GREEN}✓ Plugin deployed${NC}"
        echo ""
        echo -e "${YELLOW}Next steps:${NC}"
        echo "  In your Rust server console, run:"
        echo "    c.reload RustJavaBridge"
        echo ""
        echo "This will hot-reload the plugin without restarting the server."
    else
        echo -e "${RED}❌ Deployment failed${NC}"
        exit 1
    fi
else
    echo -e "${YELLOW}Dev server not found at: $DEV_SERVER${NC}"
    echo ""
    echo -e "${GREEN}Plugin built at:${NC}"
    echo "  $PLUGIN_DLL"
    echo ""
    echo -e "${YELLOW}To deploy, copy the DLL to your Rust server's carbon/plugins/ folder${NC}"
    echo "Then run: c.reload RustJavaBridge"
fi
