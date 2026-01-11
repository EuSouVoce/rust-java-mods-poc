# Rust Game Java Modding Framework

> **POC framework to enable Java modding for Facepunch's Rust game using Carbon**

A high-performance, cross-platform modding framework that brings Java capabilities to the Rust game (by Facepunch Studios). Uses **Carbon** (the official modding framework from Facepunch wiki) as the C# bridge layer. Like PaperMC for Rust - it forwards game hooks through a Carbon plugin using OS-native IPC mechanisms: Named Pipes on Windows and Unix Domain Sockets on Linux for request-response, plus memory-mapped files for high-throughput events.

## 🎯 Features

- **Carbon Integration**: Uses official Carbon modding framework (no Oxide/uMod dependency)
- **Cross-Platform IPC**: Auto-detects Windows/Linux and uses optimal transport (Named Pipes or Unix Sockets)
- **High-Performance Events**: Memory-mapped files for low-latency, send-and-forget event dispatch
- **Async-Safe**: Fully asynchronous, non-blocking architecture
- **Game Hook System**: Intercept and modify Rust game behavior (player connections, damage, chat, building, etc.)
- **Event Dispatcher**: React to game events (player join/leave, deaths, entity spawns, chat messages)
- **Extensible API**: Dynamically extendable events and types
- **Type-Safe Java API**: Clean, idiomatic Java interface for Rust game modding
- **Carbon Plugin**: Lightweight C# plugin that integrates directly with Rust game

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────┐
│    Facepunch Rust Game Server                       │
│                                                     │
│  ┌───────────────────────────────────────────────┐ │
│  │   Carbon Framework (Official Modding)         │ │
│  │                                               │ │
│  │  ┌─────────────────────────────────────────┐ │ │
│  │  │   RustJavaBridge.cs Plugin              │ │ │
│  │  │  - OS Auto-detection                    │ │ │
│  │  │  - Named Pipes (Windows)                │ │ │
│  │  │  - Unix Sockets (Linux)                 │ │ │
│  │  │  - Hook all game events                 │ │ │
│  │  └─────────────┬───────────────────────────┘ │ │
│  └────────────────┼─────────────────────────────┘ │
└───────────────────┼───────────────────────────────┘
                    │ IPC (Named Pipes/Unix Sockets)
                    │
          ┌─────────▼─────────┐
          │   Java Plugin API │
          │  - Hook Manager   │
          │  - Event System   │
          │  - Game API       │
          └───────────────────┘
```

## 🚀 Quick Start

### Prerequisites

- **Rust Game Server** with Carbon framework installed
- **.NET Framework 4.7.2+** (for Carbon plugins)
- **Java 11+** JDK for Java plugin development

### Installation

1. **Install Carbon Framework** on your Rust server (see [Carbon documentation](https://docs.carbonmod.gg/))

2. **Build the C# Carbon Plugin**:
```bash
cd csharp-bridge
dotnet build --configuration Release
```

3. **Deploy the Plugin**:
   - Copy `RustJavaBridge.dll` to your Carbon plugins folder: `carbon/plugins/`
   - Restart your Rust server or use Carbon's hot-reload

4. **Build Java API**:
```bash
cd java-plugin
mvn clean package
```

### Example Java Mod

```java
import com.rustjavamods.RustModAPI;
import com.rustjavamods.game.*;

public class MyRustMod {
    public static void main(String[] args) {
        RustModAPI api = RustModAPI.getInstance();
        api.initialize();

        // Listen for player connections
        RustGameEvents.onPlayerConnected((playerId, playerName, steamId) -> {
            System.out.println("Player joined: " + playerName);
        });

        // Hook player damage to reduce fall damage
        RustGameHooks.onPlayerTakingDamage((playerId, damage, damageType) -> {
            if (damageType.equals("Fall")) {
                JsonObject modified = new JsonObject();
                modified.addProperty("damage", damage * 0.5f);
                return modified; // Reduce by 50%
            }
            return null; // Allow unchanged
        });
    }
}
```

## 📚 Documentation

### Available Game Events

- `player_connected` - Player joins server
- `player_disconnected` - Player leaves server
- `player_damage` - Player takes damage
- `player_death` - Player dies
- `player_respawn` - Player respawns
- `entity_spawned` - Entity spawns in world
- `entity_killed` - Entity is killed
- `structure_placed` - Player places structure
- `structure_destroyed` - Structure is destroyed
- `chat_message` - Player sends chat message
- `item_crafted` - Player crafts an item

### Available Game Hooks

- `player_connecting` - Intercept player connection (allow/deny)
- `player_taking_damage` - Modify or cancel damage
- `player_chat` - Filter or modify chat messages
- `player_build` - Control building permissions
- `player_loot` - Control looting access
- `player_craft` - Modify crafting behavior

## 🎮 Example Mods

See `java-plugin/examples/` for complete examples:

- **WelcomeModExample.java** - Welcome messages, damage modification, chat filtering, building restrictions

## 🛠️ Development

### Testing with Carbon

1. Place your compiled `RustJavaBridge.dll` in `carbon/plugins/`
2. Start your Rust server
3. Check console for: "✓ Rust-Java Bridge initialized successfully"
4. Run your Java mod application
5. Watch events flow from Rust game to Java!

### Building All Components

```bash
# Build C# Carbon plugin
cd csharp-bridge
dotnet build --configuration Release

# Build Java API
cd ../java-plugin
mvn clean package
```

## 📋 Requirements

- **Facepunch Rust Game Server**: Latest version
- **Carbon Framework**: Latest version from https://carbonmod.gg/
- **.NET Framework**: 4.7.2+ (comes with Carbon)
- **Java**: 11+ JDK for plugin development
- **OS**: Windows or Linux (macOS support pending)

## 🔒 Performance & Safety

- **Lock-Free Events**: Memory-mapped files for zero-allocation event dispatch
- **Async Architecture**: Non-blocking I/O throughout
- **Type Safety**: Strong typing in all layers
- **Low Latency**: Direct IPC without middleware overhead
- **Carbon Native**: Uses official modding framework, no third-party dependencies

## 📄 License

MIT License - See LICENSE file for details

## 🤝 Contributing

Contributions welcome! This is a proof-of-concept framework for enabling Java-based modding in Facepunch's Rust game using the official Carbon framework.

## 🔗 Links

- [Carbon Framework](https://carbonmod.gg/) - Official Rust modding framework
- [Facepunch Rust](https://rust.facepunch.com/) - The game
- [Carbon Documentation](https://docs.carbonmod.gg/) - Learn about Carbon plugins
