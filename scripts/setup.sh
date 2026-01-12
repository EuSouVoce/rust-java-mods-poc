#!/bin/bash
# One-Click Development Environment Setup
# Sets up everything needed for Rust-Java Mods development

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}"
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║    Rust-Java Mods Development Environment Setup                ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""

# Check prerequisites
check_prereqs() {
    echo -e "${YELLOW}Checking prerequisites...${NC}"
    local missing=0
    
    if ! command -v java &> /dev/null; then
        echo -e "${RED}❌ Java not found${NC}"
        echo "   Install JDK 21+ from: https://openjdk.org/"
        missing=1
    else
        local java_version=$(java -version 2>&1 | grep "version" | awk '{print $3}' | tr -d '"')
        echo -e "${GREEN}✓ Java $java_version${NC}"
    fi
    
    if ! command -v mvn &> /dev/null; then
        echo -e "${RED}❌ Maven not found${NC}"
        echo "   Install Maven 3.8+ from: https://maven.apache.org/download.cgi"
        missing=1
    else
        local mvn_version=$(mvn -v 2>&1 | head -1)
        echo -e "${GREEN}✓ $mvn_version${NC}"
    fi
    
    if ! command -v dotnet &> /dev/null; then
        echo -e "${RED}❌ .NET SDK not found${NC}"
        echo "   Install .NET 6.0+ from: https://dotnet.microsoft.com/download"
        missing=1
    else
        local dotnet_version=$(dotnet --version)
        echo -e "${GREEN}✓ .NET $dotnet_version${NC}"
    fi
    
    if ! command -v flatc &> /dev/null; then
        echo -e "${YELLOW}⚠ flatc (FlatBuffers compiler) not found${NC}"
        echo "   Optional - only needed to modify schemas"
        echo "   Install from: https://github.com/google/flatbuffers/releases"
    else
        echo -e "${GREEN}✓ flatc installed${NC}"
    fi
    
    echo ""
    
    if [[ $missing -eq 1 ]]; then
        echo -e "${RED}Missing required tools. Please install them and try again.${NC}"
        exit 1
    fi
}

# Build the framework
build_framework() {
    echo -e "${YELLOW}Building Rust-Java Mods Framework...${NC}"
    cd "$PROJECT_ROOT"
    
    if bash ./build.sh; then
        echo -e "${GREEN}✓ Framework built successfully${NC}"
    else
        echo -e "${RED}❌ Build failed${NC}"
        exit 1
    fi
    echo ""
}

# Create dev server structure
setup_dev_server() {
    local dev_server="$PROJECT_ROOT/dev-server"
    
    echo -e "${YELLOW}Setting up development server directories...${NC}"
    
    mkdir -p "$dev_server/carbon/plugins"
    mkdir -p "$dev_server/carbon/configs"
    mkdir -p "$dev_server/carbon/logs"
    mkdir -p "$dev_server/logs"
    
    echo -e "${GREEN}✓ Dev server directories created: $dev_server${NC}"
    echo ""
    
    echo -e "${YELLOW}Deploying Carbon bridge plugin...${NC}"
    cp "$PROJECT_ROOT/csharp-bridge/bin/Release/net472/RustJavaBridge.dll" \
       "$dev_server/carbon/plugins/" 2>/dev/null || {
        echo -e "${YELLOW}⚠ Plugin not yet built (expected on first run)${NC}"
    }
    echo ""
}

# Create start scripts
create_start_scripts() {
    echo -e "${YELLOW}Creating start scripts...${NC}"
    
    # Make existing scripts executable
    chmod +x "$SCRIPT_DIR/start-dev.sh" 2>/dev/null || true
    chmod +x "$SCRIPT_DIR/watch-java.sh" 2>/dev/null || true
    chmod +x "$SCRIPT_DIR/rebuild-bridge.sh" 2>/dev/null || true
    
    echo -e "${GREEN}✓ Scripts are ready${NC}"
    echo ""
}

# Show next steps
show_next_steps() {
    echo -e "${GREEN}"
    echo "╔════════════════════════════════════════════════════════════════╗"
    echo "║                   Setup Complete! 🎉                          ║"
    echo "╚════════════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
    echo ""
    echo -e "${BLUE}Next Steps:${NC}"
    echo ""
    echo -e "${YELLOW}1. Start Development Server (Terminal 1):${NC}"
    echo "   $SCRIPT_DIR/start-dev.sh"
    echo ""
    echo -e "${YELLOW}2. Watch Java Files for Changes (Terminal 2):${NC}"
    echo "   $SCRIPT_DIR/watch-java.sh"
    echo ""
    echo -e "${YELLOW}3. Edit Java Files:${NC}"
    echo "   Files in java-plugin/src will auto-rebuild and restart"
    echo ""
    echo -e "${YELLOW}4. Rebuild C# Bridge (Terminal 3, if needed):${NC}"
    echo "   $SCRIPT_DIR/rebuild-bridge.sh"
    echo ""
    echo -e "${BLUE}Important:${NC}"
    echo "   • Keep multiple terminals open"
    echo "   • Dev server at: $PROJECT_ROOT/dev-server"
    echo "   • Logs at: $PROJECT_ROOT/dev-server/logs/"
    echo ""
    echo -e "${BLUE}For more info:${NC}"
    echo "   Read: $PROJECT_ROOT/DOCUMENTATION_INDEX.md"
    echo ""
}

# Main execution
main() {
    check_prereqs
    build_framework
    setup_dev_server
    create_start_scripts
    show_next_steps
}

main "$@"
