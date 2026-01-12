# Rust Game Java Modding Framework

> **High-performance Java 21 modding for Facepunch's Rust game using Carbon and FlatBuffers**

A zero-copy, cross-platform modding framework that brings modern Java capabilities to the Rust game. Uses **Carbon** (the recommended modding framework) as the C# bridge layer with **FlatBuffers** for maximum IPC performance and **Java 21 Virtual Threads** for scalable concurrency.

## 🚀 Performance Highlights

- **Zero-copy serialization** with FlatBuffers (no JSON parsing overhead)
- **Virtual Threads (Java 21)** for lightweight, scalable concurrency
- **Direct memory access** for event/hook data
- **Sub-millisecond latency** for hook responses
- **Minimal allocations** with pooled FlatBufferBuilders
- **OS-native IPC**: Named Pipes (Windows) / Unix Sockets (Linux)

## 🎯 Features

- **Carbon Integration**: Official Rust modding framework (no Oxide/uMod dependency)
- **Java 21**: Virtual Threads, pattern matching, records, switch expressions
- **FlatBuffers Protocol**: Zero-copy binary serialization
- **Cross-Platform IPC**: Auto-detects Windows/Linux transport
- **Async-Safe**: Non-blocking architecture with virtual threads
- **Game Hook System**: Intercept and modify Rust game behavior
- **Event Dispatcher**: React to game events with zero overhead
- **Type-Safe API**: Strong typing with FlatBuffers schema
- **HMR Development**: Hot module reload for Java plugins

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────┐
│    Facepunch Rust Game Server                       │
│                                                     │
│  ┌───────────────────────────────────────────────┐  │
│  │   Carbon Framework                            │  │
│  │                                               │  │
│  │  ┌─────────────────────────────────────────┐  │  │
│  │  │   RustJavaBridge.cs Plugin              │  │  │
│  │  │  - FlatBuffers serialization            │  │  │
│  │  │  - Named Pipes (Windows)                │  │  │
│  │  │  - Unix Sockets (Linux)                 │  │  │
│  │  │  - Hook all game events                 │  │  │
│  │  └─────────────┬───────────────────────────┘  │  │
│  └────────────────┼──────────────────────────────┘  │
└───────────────────┼─────────────────────────────────┘
                    │ IPC (FlatBuffers over pipes/sockets)
                    │
          ┌─────────▼─────────┐
          │   Java Plugin API │
          │  - Zero-copy read │
          │  - Hook handlers  │
          │  - Event system   │
          └───────────────────┘
```

## 📦 Quick Start

### Prerequisites

Before starting, ensure you have a running Rust server with Carbon installed.

> **First time setting up a Rust server?** See [RUST_SERVER_SETUP.md](RUST_SERVER_SETUP.md) for complete SteamCMD installation, server configuration, and startup scripts.

**Requirements**:

- **Rust Game Server** with Carbon framework installed
- **.NET Framework 4.7.2+** (for Carbon)
- **Java 21+** JDK (for Virtual Threads)
- **FlatBuffers compiler** (`flatc`) for schema updates

### Build Everything

```bash
# Clone and build
git clone https://github.com/EuSouVoce/rust-java-mods-poc.git
cd rust-java-mods-poc

# Full build (schema + C# + Java)
./build.sh          # Linux/macOS/WSL
# or
build.bat           # Windows
```

### Deploy

```bash
# Copy Carbon plugin to server
cp csharp-bridge/bin/Release/net472/RustJavaBridge.dll /path/to/rust-server/carbon/plugins/

# Hot-reload in server console
c.reload RustJavaBridge
```

### Run Example Mod

```bash
cd java-plugin
java -cp target/rust-java-mods-api-0.1.0.jar:examples \
     com.rustjavamods.examples.WelcomeModExample
```

## 🛠️ Development

### Dev Server with HMR

```bash
# Terminal 1: Start dev environment
./scripts/dev-server.sh

# Terminal 2: Watch Java for hot-reload
./scripts/watch-java.sh

# Now edit Java files - they auto-reload!
```

### Rebuild Carbon Bridge Only

```bash
./scripts/rebuild-bridge.sh
```

### Regenerate FlatBuffers Code

```bash
cd schema
./generate.sh       # Regenerates C# and Java code
```

## 📚 Example: Damage Modification Hook

```java
import com.rustjavamods.RustModAPI;
import com.rustjavamods.game.RustGameHooks;
import RustJavaMods.Protocol.DamageType;

public class MyMod {
    public static void main(String[] args) {
        RustModAPI api = RustModAPI.getInstance();
        api.initialize();

        // Reduce fall damage by 50%
        RustGameHooks.onPlayerTakingDamage((playerId, damage, damageType, attackerId) -> {
            if (damageType == DamageType.Fall) {
                return RustGameHooks.modifyDamage(damage * 0.5f);
            }
            return null; // Default behavior
        });

        // Keep running
        Thread.sleep(Long.MAX_VALUE);
    }
}
```

## 📋 Available Events

| Event | Description |
|-------|-------------|
| `player_connected` | Player joins server |
| `player_disconnected` | Player leaves server |
| `player_damage` | Player takes damage |
| `player_death` | Player dies |
| `player_respawn` | Player respawns |
| `chat_message` | Chat message sent |
| `structure_placed` | Building placed |
| `structure_destroyed` | Building destroyed |
| `entity_spawned` | Entity spawns |
| `entity_killed` | Entity killed |
| `item_crafted` | Item crafted |

## 🔧 Available Hooks

| Hook | Can Modify |
|------|------------|
| `player_connecting` | Allow/deny connection |
| `player_taking_damage` | Modify/cancel damage |
| `player_chat` | Modify/block message |
| `player_build` | Allow/deny building |
| `player_loot` | Allow/deny looting |
| `player_craft` | Modify/cancel crafting |

## 📚 Documentation

Comprehensive guides for setup and development:

| Guide | Purpose |
|-------|---------|
| [RUST_SERVER_SETUP.md](RUST_SERVER_SETUP.md) | **→ Start here:** Set up a Rust server from scratch using SteamCMD |
| [QUICKSTART.md](QUICKSTART.md) | Build and deploy the framework in 5 minutes |
| [CARBON_SETUP.md](CARBON_SETUP.md) | Install and configure the Carbon framework |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Technical architecture and design decisions |
| [GUIDELINES.md](GUIDELINES.md) | Code style and development guidelines |
| [LLM_GUIDE.md](LLM_GUIDE.md) | Prompts for AI-assisted development |

## 📂 Project Structure

```
rust-java-mods-poc/
├── schema/                    # FlatBuffers schema
│   ├── rust_java_mods.fbs    # Protocol definition
│   └── generate.sh           # Code generator
├── csharp-bridge/            # Carbon plugin
│   ├── RustJavaBridge.cs     # Main plugin
│   └── Generated/            # Generated FlatBuffers code
├── java-plugin/
│   ├── src/main/java/        # Java API
│   ├── examples/             # Example mods
│   └── pom.xml               # Maven config
├── scripts/                  # Dev scripts
│   ├── dev-server.sh        # Dev environment
│   ├── watch-java.sh        # Java HMR
│   └── rebuild-bridge.sh    # C# rebuild
└── build.sh                  # Full build
```

## 🔒 Performance & Safety

- **Zero-copy reads**: FlatBuffers accesses data directly from buffer
- **Pooled builders**: Reusable FlatBufferBuilder per thread
- **Non-blocking**: Async IPC with background threads
- **Type-safe**: Schema-enforced data types
- **Hook timeout**: 100ms max wait prevents game hangs

## 📄 License

MIT License - See LICENSE file

## 🔗 Resources

- [Carbon Framework](https://carbonmod.gg/)
- [FlatBuffers](https://google.github.io/flatbuffers/)
- [Facepunch Rust](https://rust.facepunch.com/)
- [Rust Wiki](https://wiki.facepunch.com/rust/)
- [Java 21 Features](https://openjdk.org/projects/jdk/21/)
