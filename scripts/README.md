# Development Scripts

One-click setup and development automation for Rust-Java Mods framework.

## Quick Start

### Windows

```cmd
scripts\setup.bat
scripts\start-dev.bat
```

### macOS / Linux / WSL

```bash
scripts/setup.sh
scripts/start-dev.sh
```

## Scripts Overview

### 1. **setup.sh / setup.bat** - Initial Setup

Prepares your development environment in one click:

- ✅ Checks prerequisites (Java, Maven, .NET)
- ✅ Builds the entire framework
- ✅ Creates dev server directories
- ✅ Deploys Carbon bridge plugin
- ✅ Shows next steps

**Usage:**

```bash
./setup.sh          # Linux/macOS/WSL
setup.bat           # Windows
```

**Time:** ~3-5 minutes on first run

---

### 2. **start-dev.sh / start-dev.bat** - Start Development Server

Launches a fully configured Rust development server.

**Features:**

- 📦 Automatically downloads/updates Rust server via SteamCMD
- 📦 Automatically downloads Carbon (if missing, prompts `Y/n`)
- 🔄 Deploys latest Carbon bridge plugin
- ⚙️ Creates server configuration (server.cfg)
- 📝 Sets up logging
- 🌐 Server runs on `localhost:28015`
- 🔐 RCON at `http://localhost:28017` (password: `dev123456`)

**Usage:**

```bash
./start-dev.sh      # Linux/macOS/WSL
start-dev.bat       # Windows
```

**Server Details:**

- **Address:** localhost:28015
- **Players:** 10 max
- **Map:** Procedural Map, seed 12345, size 2000
- **RCON:** <http://localhost:28017> (password: dev123456)
- **Config:** `dev-server/server/dev-server/cfg/server.cfg`
- **Logs:** `dev-server/logs/rust_*.txt`

**Requirements:**

- SteamCMD and Carbon are auto-downloaded if missing (you’ll be prompted `Y/n`)
    - Download locations:
        - `tools/steamcmd` (SteamCMD)
        - `dev-server/` (Carbon + Rust server files)

---

### 3. **watch-java.sh** - Hot Module Reload (HMR)

Watches Java files and auto-rebuilds/restarts on changes.

**Features:**

- 👀 Monitors `java-plugin/src` and `java-plugin/examples`
- 🔨 Auto-builds with Maven on file changes
- 🔄 Restarts Java plugin automatically
- 📊 Shows build status and timing
- 💥 Handles crashes and rebuilds

**Usage:**

```bash
./watch-java.sh
```

**Configuration (Environment Variables):**

```bash
# Run specific mod class (default: WelcomeModExample)
MAIN_CLASS=com.rustjavamods.examples.MyMod ./watch-java.sh

# Change watch interval (default: 2 seconds)
WATCH_INTERVAL=5 ./watch-java.sh
```

**Output Example:**

```
[14:32:15] 🔄 Changes detected, reloading...
[14:32:15] Building Java plugin...
[14:32:18] ✓ Build successful
[14:32:18] Starting Java plugin...
[14:32:19] ✓ Java plugin started (PID: 12345)
```

---

### 4. **rebuild-bridge.sh** - Rebuild C# Bridge

Rebuilds the Carbon bridge plugin (C# code).

**Usage:**

```bash
./rebuild-bridge.sh
```

**What It Does:**

1. ✅ Builds C# plugin with dotnet
2. ✅ Deploys DLL to dev server (if exists)
3. ℹ️ Shows hot-reload command

**Hot Reload in Server Console:**

```
c.reload RustJavaBridge
```

---

## Development Workflow

### Terminal Setup

Open **3 terminals**:

**Terminal 1: Start Server**

```bash
cd rust-java-mods-poc
./scripts/start-dev.sh
```

- Server starts on `localhost:28015`
- Wait for "Server startup complete"

**Terminal 2: Watch Java Files**

```bash
cd rust-java-mods-poc
./scripts/watch-java.sh
```

- Watches for Java file changes
- Auto-rebuilds and restarts plugin

**Terminal 3: Rebuild Bridge (if needed)**

```bash
cd rust-java-mods-poc
./scripts/rebuild-bridge.sh
```

- Only needed if you modify C# code
- Hot-reload with `c.reload RustJavaBridge` in server console

### Typical Development Flow

1. **Start the server** (Terminal 1)

   ```bash
   ./start-dev.sh
   ```

2. **Start Java watcher** (Terminal 2)

   ```bash
   ./watch-java.sh
   ```

3. **Edit Java files** (Your editor)

   ```
   java-plugin/src/main/java/com/rustjavamods/game/RustGameEvents.java
   ```

   → Changes auto-compile and restart within 2-5 seconds

4. **Connect to server** (In Rust)

   ```
   Press F1
   Type: connect localhost:28015
   Press Enter
   ```

5. **Test your changes** in-game

6. **See output** in Terminal 2

   ```
   [14:35:42] ✓ Player connected: YourName
   [14:35:47] [EVENT] Player connected: YourName
   ```

---

## Troubleshooting

### "SteamCMD not found"

- **Windows:** Download from <https://steamcmd.net/> and extract to `C:\SteamCMD`
- **Linux:** Run `mkdir -p ~/steamcmd && cd ~/steamcmd && wget ... && tar -xz`
- **Verify:** Run `steamcmd` or `steamcmd.exe` in terminal

### "Java build failed"

- Check syntax errors in your Java files
- Watch script will auto-retry on next file save
- Check Maven output for detailed errors

### "Server won't start"

- Ensure port 28015 is not in use: `netstat -an | grep 28015`
- Check logs: `tail -f dev-server/logs/rust_*.txt`
- Verify Rust server installed: `dev-server/RustDedicated.exe` exists

### "Can't connect to server"

- Make sure server has "Server startup complete" in console
- Try: `connect 127.0.0.1:28015` (in Rust, press F1)
- Check firewall allows port 28015

### "RCON not working"

- Verify server.cfg has `rcon.web 1`
- Try: <http://localhost:28017> in browser
- Default password is `dev123456`

---

## Environment Variables

### MAIN_CLASS

Specify the main Java class to run:

```bash
MAIN_CLASS=com.rustjavamods.examples.MyMod ./watch-java.sh
```

Default: `com.rustjavamods.examples.WelcomeModExample`

### RUST_SERVER_PATH

Specify dev server location:

```bash
RUST_SERVER_PATH=/custom/path ./start-dev.sh
```

Default: `./dev-server`

### WATCH_INTERVAL

Change file watch interval (seconds):

```bash
WATCH_INTERVAL=5 ./watch-java.sh
```

Default: `2` seconds

---

## Performance Tips

💡 **Fast Builds**

- Keep `WATCH_INTERVAL` at 2 seconds
- Avoid large FlatBuffers schema changes
- Use `mvn package` instead of `mvn clean package`

💡 **Stable Server**

- Run on SSD for faster file I/O
- Keep logs in separate disk
- Monitor CPU/RAM in dev-server logs

💡 **Quick Iteration**

- Keep editor and terminals visible
- Use split-screen terminals
- Ctrl+C to stop watching, fix errors, run again

---

## Advanced Usage

### Run Multiple Java Classes

```bash
# Terminal 2A
MAIN_CLASS=com.rustjavamods.examples.WelcomeModExample ./watch-java.sh

# Terminal 2B (different terminal)
MAIN_CLASS=com.rustjavamods.examples.CustomMod ./watch-java.sh
```

### Debug with Logs

```bash
# In separate terminal, tail logs
tail -f dev-server/logs/rust_*.txt
```

### Connect via IP

```
# Find your IP (Linux/macOS)
hostname -I

# Find your IP (Windows)
ipconfig

# Connect from another machine
connect 192.168.1.100:28015
```

### RCON Commands

```
# In server console or via RCON
users                          # List players
ownerid 76561198123456789      # Make admin
ban 76561198123456789          # Ban player
status                         # Server info
```

---

## Cleanup

### Stop Everything

- **Terminal 1:** Ctrl+C (stop server)
- **Terminal 2:** Ctrl+C (stop watcher)
- **Terminal 3:** Ctrl+C (stop builder)

### Remove Dev Server

```bash
rm -rf dev-server/
```

### Full Reset

```bash
rm -rf dev-server/
./setup.sh
```

---

## Scripts Workflow Diagram

```
┌──────────────────┐
│  setup.sh/bat    │  ← Run ONCE on first setup
│                  │
│ • Check prereqs  │
│ • Build framework│
│ • Create folders │
│ • Deploy bridge  │
└────────┬─────────┘
         │
         ↓
┌──────────────────┐
│  start-dev.sh    │  ← Run in Terminal 1
│  (keep running)  │
│                  │
│ • Download server│
│ • Update files   │
│ • Deploy bridge  │
│ • Start server   │
└──────────────────┘
         ↓
┌──────────────────┐
│  watch-java.sh   │  ← Run in Terminal 2
│  (keep running)  │
│                  │
│ • Watch files    │
│ • Auto-rebuild   │
│ • Auto-restart   │
└──────────────────┘
         ↓
    [Edit Files]
         ↓
    [Auto-rebuild]
         ↓
    [Auto-restart]
```

---

## Need Help?

- **Setup Questions:** See [RUST_SERVER_SETUP.md](../RUST_SERVER_SETUP.md)
- **Framework Guide:** See [QUICKSTART.md](../QUICKSTART.md)
- **Architecture:** See [ARCHITECTURE.md](../ARCHITECTURE.md)
- **All Docs:** See [DOCUMENTATION_INDEX.md](../DOCUMENTATION_INDEX.md)

---

**Happy developing! 🚀**
