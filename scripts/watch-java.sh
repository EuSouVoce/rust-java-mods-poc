#!/bin/bash
# Java Plugin Hot Module Reload (HMR) Watcher
# Watches for changes in Java source files and automatically rebuilds/restarts

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
JAVA_PLUGIN_DIR="$PROJECT_ROOT/java-plugin"
EXAMPLES_DIR="$JAVA_PLUGIN_DIR/examples"

# Configuration
WATCH_INTERVAL=2  # seconds between checks
MAIN_CLASS="${MAIN_CLASS:-com.rustjavamods.examples.WelcomeModExample}"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${BLUE}"
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║         Java Plugin Hot Module Reload (HMR) Watcher            ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""
echo -e "${YELLOW}Watching:${NC}"
echo "  • $JAVA_PLUGIN_DIR/src"
echo "  • $EXAMPLES_DIR"
echo ""
echo -e "${YELLOW}Main class:${NC} $MAIN_CLASS"
echo -e "${YELLOW}Watch interval:${NC} ${WATCH_INTERVAL}s"
echo ""
echo -e "${GREEN}Press Ctrl+C to stop${NC}"
echo ""

# Track Java process
JAVA_PID=""
FAILED_BUILDS=0

cleanup() {
    echo ""
    echo -e "${YELLOW}Stopping Java plugin...${NC}"
    if [[ -n "$JAVA_PID" ]] && kill -0 "$JAVA_PID" 2>/dev/null; then
        kill "$JAVA_PID" 2>/dev/null || true
        wait "$JAVA_PID" 2>/dev/null || true
    fi
    echo -e "${GREEN}Goodbye!${NC}"
    exit 0
}

trap cleanup SIGINT SIGTERM

build_java() {
    echo -e "[$(date +%H:%M:%S)] ${YELLOW}Building Java plugin...${NC}"
    cd "$JAVA_PLUGIN_DIR"
    
    if mvn package -q -DskipTests 2>/dev/null; then
        echo -e "[$(date +%H:%M:%S)] ${GREEN}✓ Build successful${NC}"
        FAILED_BUILDS=0
        return 0
    else
        echo -e "[$(date +%H:%M:%S)] ${RED}❌ Build failed${NC}"
        FAILED_BUILDS=$((FAILED_BUILDS + 1))
        return 1
    fi
}

start_java() {
    echo -e "[$(date +%H:%M:%S)] ${YELLOW}Starting Java plugin...${NC}"
    cd "$JAVA_PLUGIN_DIR"
    
    # Build classpath
    local classpath="target/rust-java-mods-api-0.1.0.jar:target/classes"
    if [[ -d "examples" ]]; then
        classpath="$classpath:examples"
    fi
    
    # Start Java process in background
    java -cp "$classpath" "$MAIN_CLASS" 2>&1 &
    JAVA_PID=$!
    sleep 1
    
    if kill -0 "$JAVA_PID" 2>/dev/null; then
        echo -e "[$(date +%H:%M:%S)] ${GREEN}✓ Java plugin started (PID: $JAVA_PID)${NC}"
        return 0
    else
        echo -e "[$(date +%H:%M:%S)] ${RED}❌ Failed to start Java plugin${NC}"
        JAVA_PID=""
        return 1
    fi
}

stop_java() {
    if [[ -n "$JAVA_PID" ]] && kill -0 "$JAVA_PID" 2>/dev/null; then
        echo -e "[$(date +%H:%M:%S)] ${YELLOW}Stopping Java plugin (PID: $JAVA_PID)...${NC}"
        kill "$JAVA_PID" 2>/dev/null || true
        wait "$JAVA_PID" 2>/dev/null || true
        JAVA_PID=""
        sleep 1
    fi
}

restart_java() {
    echo ""
    stop_java
    if build_java; then
        start_java
    else
        if [[ $FAILED_BUILDS -ge 3 ]]; then
            echo -e "[$(date +%H:%M:%S)] ${RED}Multiple build failures. Fix errors and save file to retry.${NC}"
        fi
    fi
}

get_latest_mtime() {
    find "$JAVA_PLUGIN_DIR/src" "$EXAMPLES_DIR" -name "*.java" -type f -printf '%T@\n' 2>/dev/null | sort -n | tail -1
}

# Initial build and start
echo -e "${YELLOW}Initial build...${NC}"
if build_java; then
    start_java
else
    echo -e "${YELLOW}Initial build failed. Waiting for file changes...${NC}"
fi

LAST_MTIME=$(get_latest_mtime)

# Watch loop
while true; do
    sleep $WATCH_INTERVAL
    
    CURRENT_MTIME=$(get_latest_mtime)
    
    if [[ "$CURRENT_MTIME" != "$LAST_MTIME" ]]; then
        LAST_MTIME="$CURRENT_MTIME"
        restart_java
    fi
    
    # Check if Java process is still running
    if [[ -n "$JAVA_PID" ]] && ! kill -0 "$JAVA_PID" 2>/dev/null; then
        echo -e "[$(date +%H:%M:%S)] ${YELLOW}⚠ Java plugin crashed, restarting...${NC}"
        JAVA_PID=""
        sleep 1
        build_java && start_java
    fi
done
