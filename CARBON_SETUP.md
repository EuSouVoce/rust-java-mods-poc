# Carbon Framework Setup Guide

This guide covers installing and configuring the Carbon framework for Rust server modding.

> **Setting up your Rust server from scratch?** See [RUST_SERVER_SETUP.md](RUST_SERVER_SETUP.md) for complete SteamCMD installation, server configuration, and startup scripts.

## What is Carbon?

Carbon is the modding framework **recommended by Facepunch** for Rust servers. It provides:

- Native integration with Rust game
- High-performance hook system
- Active development and support
- Modern C# 10+ features
- Better memory management than alternatives

## Installation

### Method 1: Automatic (Recommended)

```bash
# Download and install Carbon
cd /path/to/rust-server
curl -sL https://github.com/CarbonCommunity/Carbon/releases/latest/download/Carbon.Linux.Release.tar.gz | tar -xz

# Or Windows:
# Download Carbon.Windows.Release.zip from GitHub releases
# Extract to your Rust server folder
```

### Method 2: Manual

1. Download latest release from [Carbon GitHub](https://github.com/CarbonCommunity/Carbon/releases)
2. Extract contents to your Rust server root folder
3. Verify folder structure:

```
RustServer/
├── carbon/
│   ├── managed/           # Carbon core DLLs
│   ├── plugins/           # Plugin folder (deploy here)
│   ├── configs/           # Plugin configs
│   └── logs/              # Log files
├── HarmonyMods/
│   └── Carbon.Loader.dll  # Entry point
└── RustDedicated.exe      # Server executable
```

## Configuration

### Server Launch

Add Carbon to your server startup:

```bash
# Linux
./RustDedicated -batchmode +server.hostname "My Server" ...

# Windows
RustDedicated.exe -batchmode +server.hostname "My Server" ...
```

Carbon automatically hooks into the game on startup via `Carbon.Loader.dll`.

### Carbon Config (Optional)

Edit `carbon/configs/carbon.cfg`:

```json
{
  "Debug": false,
  "HookValidation": false,
  "LogFileMode": 1,
  "AnalyticsEnabled": false
}
```

## Deploying RustJavaBridge Plugin

### Copy the Plugin

```bash
# From rust-java-mods-poc directory
cp csharp-bridge/bin/Release/net472/RustJavaBridge.dll /path/to/rust-server/carbon/plugins/
```

### Hot-Reload (No Restart Needed)

In the Rust server console:

```
c.reload RustJavaBridge
```

### Verify Installation

Check server console for:

```
✓ Rust-Java Bridge initialized successfully
  IPC Endpoint: \\.\pipe\rust-java-mods   (Windows)
  IPC Endpoint: /tmp/rust-java-mods.sock  (Linux)
  Protocol: FlatBuffers (zero-copy)
  Using Carbon Framework
```

## Carbon Console Commands

### Plugin Management

```
c.plugins              # List all loaded plugins
c.reload PluginName    # Hot-reload a specific plugin
c.unload PluginName    # Unload a plugin
c.load PluginName      # Load a plugin from disk
```

### Debugging

```
c.hooks                # List registered hooks
c.find <pattern>       # Search hooks/commands
```

### RustJavaBridge Status

```
c.plugins              # Should show "RustJavaBridge" as loaded
```

## Logs Location

```
carbon/logs/
├── carbon.log         # Main Carbon log
├── carbon.error.log   # Error-specific log
└── [plugin].log       # Per-plugin logs
```

### Watch Logs (Linux)

```bash
tail -f carbon/logs/carbon.log
```

### Filter RustJavaBridge Logs

```bash
grep -i "RustJavaBridge" carbon/logs/carbon.log
```

## Troubleshooting

### Plugin Not Loading

1. Verify DLL is in `carbon/plugins/` folder
2. Check `carbon.log` for errors
3. Ensure .NET Framework 4.7.2+ is installed
4. On Windows, right-click DLL → Properties → Unblock

### Carbon Not Starting

1. Verify `HarmonyMods/Carbon.Loader.dll` exists
2. Check for conflicts with other mod loaders (Oxide, uMod)
3. Verify server has correct permissions

### IPC Connection Issues

1. Plugin must initialize first (wait for server startup)
2. Check firewall settings
3. On Linux, verify socket permissions:

```bash
ls -la /tmp/rust-java-mods.sock
```

## Upgrading Carbon

### Backup First

```bash
cp -r carbon/configs carbon/configs.backup
cp -r carbon/plugins carbon/plugins.backup
```

### Download New Version

```bash
# Stop server first
curl -sL https://github.com/CarbonCommunity/Carbon/releases/latest/download/Carbon.Linux.Release.tar.gz | tar -xz

# Restart server
```

## Performance Tips

- Enable Carbon's `HookValidation: false` for production
- Monitor `carbon.log` size (rotate if needed)
- Use `c.unload` to disable unused plugins
- FlatBuffers protocol minimizes IPC overhead

## Carbon vs Oxide Comparison

| Feature | Carbon | Oxide |
|---------|--------|-------|
| Performance | Better | Good |
| Modern C# | Yes (10+) | Limited |
| Memory | Lower | Higher |
| Hot-reload | Yes | Yes |
| Plugin ecosystem | Growing | Large |
| Official support | Recommended | Community |

## Resources

- [Carbon Documentation](https://carboncommunity.github.io/)
- [Carbon GitHub](https://github.com/CarbonCommunity/Carbon)
- [Carbon Discord](https://discord.gg/carbon)
- [Rust Wiki - Modding](https://wiki.facepunch.com/rust/modding)

## Next Steps

1. ✅ Carbon installed and configured
2. ✅ RustJavaBridge plugin deployed
3. ➡️ Run your Java mod ([QUICKSTART.md](QUICKSTART.md))
4. ➡️ Read architecture details ([ARCHITECTURE.md](ARCHITECTURE.md))
