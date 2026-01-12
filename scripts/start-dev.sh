#!/bin/bash
# Start Rust Development Server
# Automatically updates and starts the server
# Works on Windows (Git Bash/WSL) and Linux

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DEV_SERVER="$PROJECT_ROOT/dev-server"
TOOLS_DIR="$PROJECT_ROOT/tools"
RUST_SERVER_BIN=""
STEAMCMD_BIN=""

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}"
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║         Rust Development Server Starter                        ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""

# Detect OS and set paths
detect_os() {
    if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
        echo "OS: Windows (Git Bash)"
        RUST_SERVER_BIN="$DEV_SERVER/RustDedicated.exe"
    elif [[ "$OSTYPE" == "cygwin" ]]; then
        echo "OS: Windows (Cygwin)"
        RUST_SERVER_BIN="$DEV_SERVER/RustDedicated.exe"
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        echo "OS: Linux"
        RUST_SERVER_BIN="$DEV_SERVER/RustDedicated"
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        echo "OS: macOS"
        RUST_SERVER_BIN="$DEV_SERVER/RustDedicated"
    else
        echo "Unknown OS: $OSTYPE"
        exit 1
    fi
    echo ""
}

prompt_yes_no() {
    local prompt="$1"
    local default_yes="$2" # "Y" or "N"
    local reply
    read -r -p "$prompt [$default_yes/n]: " reply
    if [[ -z "$reply" ]]; then
        reply="$default_yes"
    fi
    [[ "$reply" =~ ^[Yy]$ ]]
}

# Check prerequisites
check_steamcmd() {
    mkdir -p "$TOOLS_DIR" 2>/dev/null || true

    if command -v steamcmd.exe &> /dev/null; then
        STEAMCMD_BIN="steamcmd.exe"
        return
    fi

    if command -v steamcmd &> /dev/null; then
        STEAMCMD_BIN="steamcmd"
        return
    fi

    if [[ -x "$TOOLS_DIR/steamcmd/steamcmd.exe" ]]; then
        STEAMCMD_BIN="$TOOLS_DIR/steamcmd/steamcmd.exe"
        return
    fi

    if [[ -x "$TOOLS_DIR/steamcmd/steamcmd.sh" ]]; then
        STEAMCMD_BIN="$TOOLS_DIR/steamcmd/steamcmd.sh"
        return
    fi

    echo -e "${YELLOW}⚠ SteamCMD not found${NC}"
    if prompt_yes_no "Download SteamCMD into ./tools/steamcmd?" "Y"; then
        local url="https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip"
        mkdir -p "$TOOLS_DIR/steamcmd"

        echo -e "${YELLOW}Downloading SteamCMD...${NC}"

        if command -v powershell.exe &> /dev/null; then
            powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "try { Invoke-WebRequest -Uri '$url' -OutFile '$TOOLS_DIR\\steamcmd.zip' -UseBasicParsing } catch { exit 1 }" || true
            powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive '$TOOLS_DIR\\steamcmd.zip' -DestinationPath '$TOOLS_DIR\\steamcmd' -Force" || true
            rm -f "$TOOLS_DIR/steamcmd.zip" 2>/dev/null || true
        else
            curl -fsSL "$url" -o "$TOOLS_DIR/steamcmd.zip"
            unzip -o "$TOOLS_DIR/steamcmd.zip" -d "$TOOLS_DIR/steamcmd" >/dev/null
            rm -f "$TOOLS_DIR/steamcmd.zip"
        fi

        if [[ -f "$TOOLS_DIR/steamcmd/steamcmd.exe" ]]; then
            STEAMCMD_BIN="$TOOLS_DIR/steamcmd/steamcmd.exe"
            return
        fi

        if [[ -f "$TOOLS_DIR/steamcmd/steamcmd.sh" ]]; then
            chmod +x "$TOOLS_DIR/steamcmd/steamcmd.sh" 2>/dev/null || true
            STEAMCMD_BIN="$TOOLS_DIR/steamcmd/steamcmd.sh"
            return
        fi
    fi

    echo -e "${RED}❌ SteamCMD not available${NC}"
    echo "Install SteamCMD from https://steamcmd.net/ or run scripts/setup.*"
    exit 1
}

ensure_carbon() {
    if [[ -d "$DEV_SERVER/carbon" ]]; then
        return
    fi

    echo -e "${YELLOW}⚠ Carbon not found in dev-server${NC}"
    if ! prompt_yes_no "Download Carbon into ./dev-server now?" "Y"; then
        return
    fi

    local tag="${CARBON_TAG:-preview}"
    local build="Debug"
    if [[ "$tag" == "production" ]]; then
        build="Release"
    fi

    mkdir -p "$DEV_SERVER"

    local url=""
    local tmp="$TOOLS_DIR/carbon.tmp"
    mkdir -p "$TOOLS_DIR"

    if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" || "$OSTYPE" == "cygwin" ]]; then
        url="https://github.com/CarbonCommunity/Carbon.Core/releases/download/${tag}_build/Carbon.Windows.${build}.zip"
        echo -e "${YELLOW}Downloading Carbon (${tag}/${build})...${NC}"
        curl -fsSL "$url" -o "$tmp.zip"
        unzip -o "$tmp.zip" -d "$DEV_SERVER" >/dev/null
        rm -f "$tmp.zip"
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        url="https://github.com/CarbonCommunity/Carbon.Core/releases/download/${tag}_build/Carbon.Linux.${build}.tar.gz"
        echo -e "${YELLOW}Downloading Carbon (${tag}/${build})...${NC}"
        curl -fsSL "$url" -o "$tmp.tar.gz"
        tar xzf "$tmp.tar.gz" -C "$DEV_SERVER"
        rm -f "$tmp.tar.gz"
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        echo -e "${YELLOW}⚠ Carbon auto-download not implemented for macOS in this script yet.${NC}"
        echo "Use the official Carbon QuickStart scripts in references/Carbon.QuickStart-main"
        return
    fi

    if [[ -d "$DEV_SERVER/carbon" ]]; then
        echo -e "${GREEN}✓ Carbon installed${NC}"
        echo ""
    else
        echo -e "${RED}❌ Carbon install failed (carbon folder not found)${NC}"
        echo "You can install Carbon manually using references/Carbon.QuickStart-main"
        exit 1
    fi
}

# Create/update Rust server
update_server() {
    echo -e "${YELLOW}Checking Rust server...${NC}"
    
    mkdir -p "$DEV_SERVER"
    
    if [[ ! -f "$RUST_SERVER_BIN" ]]; then
        echo -e "${YELLOW}Rust server not found. Installing...${NC}"
        echo "This may take several minutes..."
        echo ""
        
        "$STEAMCMD_BIN" +force_install_dir "$DEV_SERVER" +login anonymous +app_update 258550 validate +quit
        
        if [[ -f "$RUST_SERVER_BIN" ]]; then
            echo -e "${GREEN}✓ Server installed${NC}"
        else
            echo -e "${RED}❌ Server installation failed${NC}"
            exit 1
        fi
    else
        echo -e "${YELLOW}Updating Rust server...${NC}"
        
        "$STEAMCMD_BIN" +force_install_dir "$DEV_SERVER" +login anonymous +app_update 258550 validate +quit
        
        echo -e "${GREEN}✓ Server updated${NC}"
    fi
    echo ""
}

# Deploy Carbon bridge
deploy_bridge() {
    local bridge_dll="$PROJECT_ROOT/csharp-bridge/bin/Release/net472/RustJavaBridge.dll"
    local target_dir="$DEV_SERVER/carbon/plugins"
    
    echo -e "${YELLOW}Deploying Carbon bridge...${NC}"
    
    mkdir -p "$target_dir"
    
    if [[ -f "$bridge_dll" ]]; then
        cp "$bridge_dll" "$target_dir/"
        echo -e "${GREEN}✓ Bridge deployed${NC}"
    else
        echo -e "${YELLOW}⚠ Bridge DLL not found (run setup.sh first)${NC}"
    fi
    echo ""
}

# Create server config
create_server_config() {
    local cfg_dir="$DEV_SERVER/server/dev-server/cfg"
    local server_cfg="$cfg_dir/server.cfg"
    
    echo -e "${YELLOW}Setting up server configuration...${NC}"
    
    mkdir -p "$cfg_dir"
    
    if [[ ! -f "$server_cfg" ]]; then
        cat > "$server_cfg" << 'EOF'
# Rust Development Server Configuration
# Auto-generated by start-dev.sh

# Server Identity and Display
server.hostname "Dev - Java Mods Testing"
server.description "Development server for Rust-Java Mods"
server.url "https://github.com/EuSouVoce/rust-java-mods-poc"

# Network Settings
server.port 28015
server.queryport 28016
server.maxplayers 10

# Gameplay
server.level "Procedural Map"
server.seed 12345
server.worldsize 2000
server.tickrate 30

# RCON
rcon.port 28017
rcon.password dev123456
rcon.web 1

# Logging
server.logfile "logs/rust_%Y%m%d_%H%M%S.txt"

# Development settings
server.wipe_empty true
server.censorplayerlist false
decay.scale 0.5
EOF
        echo -e "${GREEN}✓ Server config created: $server_cfg${NC}"
    else
        echo -e "${GREEN}✓ Server config exists: $server_cfg${NC}"
    fi
    echo ""
}

# Start the server
start_server() {
    echo -e "${YELLOW}Starting Rust server...${NC}"
    echo "Server directory: $DEV_SERVER"
    echo "Connect via: connect localhost:28015 (in Rust, press F1)"
    echo ""
    echo -e "${GREEN}Server is starting...${NC}"
    echo ""
    
    cd "$DEV_SERVER"
    
    # Run with proper executable
    if [[ -f "$RUST_SERVER_BIN" ]]; then
        if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" || "$OSTYPE" == "cygwin" ]]; then
            # Windows
            "$RUST_SERVER_BIN" \
                -batchmode \
                -nographics \
                +server.port 28015 \
                +server.queryport 28016 \
                +server.level "Procedural Map" \
                +server.seed 12345 \
                +server.worldsize 2000 \
                +server.maxplayers 10 \
                +server.hostname "Dev - Java Mods Testing" \
                +server.identity "dev-server" \
                +rcon.port 28017 \
                +rcon.password dev123456 \
                +rcon.web 1 \
                -logfile logs/rust_server.txt
        else
            # Linux/macOS
            ./RustDedicated \
                -batchmode \
                -nographics \
                +server.port 28015 \
                +server.queryport 28016 \
                +server.level "Procedural Map" \
                +server.seed 12345 \
                +server.worldsize 2000 \
                +server.maxplayers 10 \
                +server.hostname "Dev - Java Mods Testing" \
                +server.identity "dev-server" \
                +rcon.port 28017 \
                +rcon.password dev123456 \
                +rcon.web 1 \
                -logfile logs/rust_server.txt
        fi
    else
        echo -e "${RED}❌ Rust server executable not found: $RUST_SERVER_BIN${NC}"
        exit 1
    fi
}

# Main
main() {
    detect_os
    check_steamcmd
    ensure_carbon
    update_server
    deploy_bridge
    create_server_config
    
    echo -e "${BLUE}"
    echo "╔════════════════════════════════════════════════════════════════╗"
    echo "║                   Ready to Start! 🚀                          ║"
    echo "╚════════════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
    echo ""
    echo -e "${YELLOW}In another terminal, run:${NC}"
    echo "  $SCRIPT_DIR/watch-java.sh"
    echo ""
    echo -e "${YELLOW}Then connect to the server:${NC}"
    echo "  • In Rust: Press F1, type: connect localhost:28015"
    echo "  • RCON: http://localhost:28017 (password: dev123456)"
    echo ""
    echo -e "${YELLOW}Server logs:${NC}"
    echo "  tail -f $DEV_SERVER/logs/rust_server.txt"
    echo ""
    
    start_server
}

main "$@"
