# Rust Server Setup Guide for Development

Complete guide to setting up a Facepunch Rust dedicated server for developing Java mods with the Rust-Java Mods framework.

## Table of Contents

1. [System Requirements](#system-requirements)
2. [SteamCMD Installation](#steamcmd-installation)
3. [Server Installation](#server-installation)
4. [Server Configuration](#server-configuration)
5. [Starting the Server](#starting-the-server)
6. [Connecting to Your Server](#connecting-to-your-server)
7. [RCON Administration](#rcon-administration)
8. [Oxide/uMod Plugin Support (Optional)](#oxide-plugin-support)
9. [Linux Server Setup](#linux-server-setup)

## System Requirements

For a development server, minimum specs:

- **RAM**: 6-12 GB (depending on map size and player count)
- **Disk Space**: 15+ GB free (SSD/NVMe recommended)
- **CPU**: Modern multi-core processor
- **OS**: Windows, Linux, or macOS

**Optional Tools**:

- RAM Calculator: <https://pinehosting.com/tools/rust-ram-calculator>
- Resource Calculator: <https://physgun.com/tools/rust-resource-calculator>

## SteamCMD Installation

### Windows Installation

1. Create a directory for SteamCMD:

   ```batch
   mkdir C:\SteamCMD
   cd C:\SteamCMD
   ```

2. Download SteamCMD:
   - Visit: <https://steamcmd.net/>
   - Download `steamcmd.zip`
   - Extract to `C:\SteamCMD`

3. Run SteamCMD to initialize:

   ```batch
   steamcmd.exe
   ```

   This creates necessary directories. You can close it after initialization.

### Linux Installation

```bash
# Create directories
mkdir ~/steamcmd
cd ~/steamcmd

# Download and extract SteamCMD
wget https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz
tar xvfz steamcmd_linux.tar.gz

# Make executable
chmod +x steamcmd.sh
```

## Server Installation

### Windows Installation

1. Create server directory:

   ```batch
   mkdir C:\RustServer
   cd C:\RustServer
   ```

2. Use SteamCMD to download the server:

   ```batch
   C:\SteamCMD\steamcmd.exe +force_install_dir C:\RustServer +login anonymous +app_update 258550 validate +quit
   ```

   > **Note**: You do NOT need a Steam account to download and run a Rust server.

3. Verify installation:

   ```batch
   dir
   ```

   You should see `RustDedicated.exe` and other server files.

### Linux Installation

```bash
# Create server directory
mkdir ~/rust-server
cd ~/rust-server

# Download server via SteamCMD
~/steamcmd/steamcmd.sh +force_install_dir ~/rust-server +login anonymous +app_update 258550 validate +quit

# This takes several minutes - wait for it to complete
```

## Server Configuration

### Basic Server.cfg

Create `C:\RustServer\server\YourServerName\cfg\server.cfg` (Windows) or `~/rust-server/server/YourServerName/cfg/server.cfg` (Linux):

```cfg
# Server Identity and Display
server.hostname "My Dev Rust Server"
server.description "A development server for Java mods"
server.url "https://github.com/YourUsername/rust-java-mods-poc"

# Network Settings
server.port 28015
server.queryport 28016
server.maxplayers 10

# Gameplay
server.level "Procedural Map"
server.seed 1234
server.worldsize 4000
server.tickrate 30

# RCON (Remote Console - for admin control)
rcon.port 28017
rcon.password YourSecurePassword123
rcon.web 1

# Logging
server.logfile "logs/rust_%Y%m%d_%H%M%S.txt"

# Optional: Oxide/uMod
# (Add plugin settings here if using Oxide)
```

**Key Parameters**:

| Setting | Function | Notes |
|---------|----------|-------|
| `server.hostname` | Server name in browser | Max 64 chars |
| `server.port` | Connection port (UDP) | Default: 28015 |
| `server.queryport` | Query port (UDP) | Must differ from server.port |
| `server.maxplayers` | Max concurrent players | Higher = more CPU/RAM |
| `server.seed` | Map generation seed | 0-2147483647 |
| `server.worldsize` | Map size | 1000-6000 (MB) |
| `server.level` | Map type | "Procedural Map" only |
| `rcon.port` | Admin console port (TCP) | For remote admin control |
| `rcon.password` | Admin password | **Change this!** |
| `rcon.web` | Enable web RCON | 1 = enabled |

### Advanced Settings (Optional)

Add to `server.cfg`:

```cfg
# Difficulty
server.gravity 9.81
server.censorplayerlist false

# Decay
decay.scale 1.0
decay.month 1

# Wipe Settings
server.wipe_empty true
server.wipe_randomization 10

# Environmental
weather.rain 0.2
weather.wind 0.5

# PvP/PvE
PvP true
```

## Starting the Server

### Windows: Batch Script

Create `C:\RustServer\start_server.bat`:

```batch
@echo off
cd /D C:\RustServer

REM Update server first
echo Updating Rust server...
C:\SteamCMD\steamcmd.exe +force_install_dir C:\RustServer +login anonymous +app_update 258550 validate +quit

REM Start server with your config
echo Starting Rust server...
RustDedicated.exe ^
    -batchmode ^
    -nographics ^
    +server.port 28015 ^
    +server.queryport 28016 ^
    +server.level "Procedural Map" ^
    +server.seed 1234 ^
    +server.worldsize 4000 ^
    +server.maxplayers 10 ^
    +server.hostname "My Dev Rust Server" ^
    +server.identity "dev-server" ^
    +rcon.port 28017 ^
    +rcon.password YourSecurePassword123 ^
    +rcon.web 1 ^
    -logfile logs/rust_server.txt

pause
```

**Save and run:**

```batch
start_server.bat
```

### Linux: Shell Script

Create `~/rust-server/start_server.sh`:

```bash
#!/bin/bash

cd ~/rust-server

# Update server first
echo "Updating Rust server..."
~/steamcmd/steamcmd.sh +force_install_dir ~/rust-server +login anonymous +app_update 258550 validate +quit

# Start server in background or screen session
echo "Starting Rust server..."

# Option 1: Direct (foreground)
./RustDedicated \
    -batchmode \
    -nographics \
    +server.port 28015 \
    +server.queryport 28016 \
    +server.level "Procedural Map" \
    +server.seed 1234 \
    +server.worldsize 4000 \
    +server.maxplayers 10 \
    +server.hostname "My Dev Rust Server" \
    +server.identity "dev-server" \
    +rcon.port 28017 \
    +rcon.password YourSecurePassword123 \
    +rcon.web 1 \
    -logfile logs/rust_server.txt

# Option 2: In a screen session (detachable)
# screen -S rust_server -d -m bash -c './RustDedicated ... '
```

Make executable:

```bash
chmod +x ~/rust-server/start_server.sh
```

Run:

```bash
./start_server.sh
```

### Auto-Restart on Crash

Wrap the startup script:

```bash
#!/bin/bash
# auto_restart.sh

while true; do
    echo "Starting Rust server..."
    ~/rust-server/start_server.sh
    
    echo "Server crashed or restarted"
    sleep 5  # Wait 5 seconds before restarting
done
```

## Connecting to Your Server

### Method 1: Direct (Same Computer)

In Rust, press **F1** and type:

```
connect localhost:28015
```

### Method 2: Via IP Address

**Find your IP:**

**Windows:**

```batch
ipconfig
```

Look for "IPv4 Address" (e.g., `192.168.1.100`)

**Linux:**

```bash
hostname -I
```

**Then connect:**

```
connect 192.168.1.100:28015
```

### Method 3: Via Public IP (Remote)

1. Find your public IP: <https://www.whatsmyip.org/>
2. Configure port forwarding on your router (forward UDP port 28015 to your server's local IP)
3. Connect: `connect YOUR_PUBLIC_IP:28015`

> **Note**: IPs starting with `192.168.x.x`, `172.16-31.x.x`, or `10.x.x.x` are private (LAN only).

## RCON Administration

### Using RCON Web Console

Rust has an official web RCON (but not HTTPS - use on trusted networks only).

### Using RCON Client

Popular RCON clients:

- **Facepunch Web RCON**: Built into Carbon
- **Oxide Logs**: Via uMod dashboard
- **RustAdmin**: Standalone application

### Server Console Commands

Once connected as admin, use server console commands:

```
# Get player info
users

# Add yourself as owner
ownerid 76561198123456789 "YourName"

# Add as moderator
moderatorid 76561198123456789 "YourName"

# Kick player
kick "PlayerName"

# Ban player
ban 76561198123456789

# Server info
status
```

Get your SteamID:

1. Connect to your server
2. Check server console: `[USERID:STEAMID] "YOUR_NAME" has auth level`
3. Copy the SteamID number

## Oxide Plugin Support (Optional)

### Installation

1. Download from: <https://umod.org/games/rust>
2. Select your OS (Windows/Linux)
3. Extract `.zip` to your server root (overwrite files)
4. Restart server

> **Important**: If you run SteamCMD during updates, Oxide files will be overwritten. Keep a backup or re-install Oxide after updates.

## Linux Server Setup

### Complete Linux Example

```bash
# 1. Create directories
mkdir -p ~/rust/{server,steamcmd,logs,configs}
cd ~/rust/steamcmd

# 2. Install SteamCMD
wget https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz
tar xvfz steamcmd_linux.tar.gz
chmod +x steamcmd.sh

# 3. Download Rust server
./steamcmd.sh +force_install_dir ~/rust/server +login anonymous +app_update 258550 validate +quit

# 4. Create start script
cat > ~/rust/server/start.sh << 'EOF'
#!/bin/bash
cd ~/rust/server
./RustDedicated \
    -batchmode \
    -nographics \
    +server.port 28015 \
    +server.queryport 28016 \
    +server.identity "rust" \
    +server.maxplayers 10 \
    +rcon.port 28017 \
    +rcon.password MyPassword123 \
    +rcon.web 1 \
    -logfile ~/rust/logs/server.txt
EOF

chmod +x ~/rust/server/start.sh

# 5. Start server
screen -S rust_server -d -m ~/rust/server/start.sh

# 6. Attach to console
screen -x rust_server

# 7. Detach (in screen): Ctrl+A then D
```

### Running in Screen

```bash
# Start
screen -S rust_server -d -m ./start.sh

# Attach (view console)
screen -x rust_server

# Detach (leave running)
# Press Ctrl+A then D

# List sessions
screen -ls

# Kill session
screen -S rust_server -X quit
```

## Troubleshooting

### "Port already in use"

- Change `server.port` to a different port (e.g., 28115, 28215)
- Check what's using the port:
    - **Windows**: `netstat -ano | findstr :28015`
    - **Linux**: `lsof -i :28015`

### Server crashes on startup

- Check `logs/rust_server.txt` for errors
- Verify `server.identity` directory exists
- Ensure sufficient RAM/disk space

### Can't connect externally

- Open firewall ports (28015 UDP, 28016 UDP)
- Configure port forwarding on router
- Verify server is running: `netstat -an | grep 28015`

### No RCON connection

- Verify `rcon.web 1` is set
- Check `rcon.port` and password
- Ensure firewall allows RCON port (TCP)

## Integration with Rust-Java Mods

Once your server is running:

1. **Install Carbon**:

   ```bash
   # Download from: https://github.com/CarbonCommunity/Carbon/releases
   # Extract to your rust-server/carbon folder
   ```

2. **Deploy RustJavaBridge plugin**:

   ```bash
   cp rust-java-mods-poc/csharp-bridge/bin/Release/net472/RustJavaBridge.dll \
      ~/rust-server/carbon/plugins/
   ```

3. **Hot-reload in server console**:

   ```
   c.reload RustJavaBridge
   ```

4. **Run Java mod**:

   ```bash
   cd rust-java-mods-poc/java-plugin
   mvn exec:java -Dexec.mainClass="com.rustjavamods.examples.WelcomeModExample"
   ```

## Resources

- **Facepunch Wiki**: <https://wiki.facepunch.com/rust/Creating_a_server>
- **Carbon Framework**: <https://github.com/CarbonCommunity/Carbon>
- **uMod (Oxide)**: <https://umod.org/>
- **SteamCMD**: <https://steamcmd.net/>
- **Rust+ Server Setup**: <https://wiki.facepunch.com/rust/Rust+_Server>

## Next Steps

1. Verify server starts and stays running
2. Connect as a player and test
3. Deploy Carbon framework
4. Deploy RustJavaBridge plugin
5. Test Java mod integration
6. See [QUICKSTART.md](QUICKSTART.md) for framework setup
