# Quick Start Guide

Get your Java 21 mod running on Facepunch Rust in 5 minutes!

## Prerequisites

> **New to Rust servers?** Start with [RUST_SERVER_SETUP.md](RUST_SERVER_SETUP.md) for complete installation from scratch (SteamCMD, server config, startup scripts).

✅ Rust dedicated server with Carbon installed  
✅ .NET Framework 4.7.2+ / Mono  
✅ Java 21+ JDK (for Virtual Threads)  
✅ FlatBuffers compiler (optional, for schema changes)

## 1. Build the Framework (2 minutes)

```bash
# Clone the repository
git clone https://github.com/EuSouVoce/rust-java-mods-poc.git
cd rust-java-mods-poc

# Build everything (generates FlatBuffers code, builds C# and Java)
./build.sh          # Linux/macOS/WSL
# or
build.bat           # Windows
```

## 2. Deploy Carbon Plugin (1 minute)

```bash
# Copy the plugin to your Rust server
cp csharp-bridge/bin/Release/net472/RustJavaBridge.dll /path/to/rust-server/carbon/plugins/

# Hot-reload in server console (or restart server)
c.reload RustJavaBridge
```

**Expected console output:**

```
✓ Rust-Java Bridge initialized successfully
  IPC Endpoint: \\.\pipe\rust-java-mods
  Protocol: FlatBuffers (zero-copy)
  Threading: Virtual Threads (Java 21)
  Using Carbon Framework
```

## 3. Run the Example Mod (2 minutes)

```bash
cd java-plugin

# Run with Maven
mvn exec:java -Dexec.mainClass="com.rustjavamods.examples.WelcomeModExample"

# Or run directly
java -cp target/rust-java-mods-api-0.1.0.jar:examples \
     com.rustjavamods.examples.WelcomeModExample
```

## 4. See It Work

Connect to your Rust server and watch:

**Server Console:**

```
[RustJavaBridge] Event: player_connected
```

**Java Console:**

```
[EVENT] Player connected: YourName (ID: 76561..., Steam: 76561...)
  → Position: (100.5, 20.0, -50.3)
  → Sending welcome message to YourName
```

## What's Happening?

1. **Carbon Plugin** hooks into Rust game events
2. **FlatBuffers** serializes data (zero-copy, no JSON parsing)
3. **IPC** transfers data via Named Pipes/Unix Sockets
4. **Java Plugin** reads data directly from buffer (zero-copy)

## Development Workflow

### Hot Module Reload (HMR)

```bash
# Terminal 1: Start dev environment
./scripts/dev-server.sh

# Terminal 2: Watch Java files for changes
./scripts/watch-java.sh

# Now edit Java files - they auto-rebuild and restart!
```

### Rebuild C# Bridge Only

```bash
./scripts/rebuild-bridge.sh
```

## Example: Damage Modification Hook

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
                System.out.printf("Reducing fall damage from %.1f to %.1f%n", damage, damage * 0.5f);
                return RustGameHooks.modifyDamage(damage * 0.5f);
            }
            return null; // Allow default behavior
        });

        // Block building in spawn area
        RustGameHooks.onPlayerBuild((playerId, structureType, location) -> {
            if (location != null) {
                float x = Math.abs(location.x());
                float z = Math.abs(location.z());
                if (x < 100 && z < 100) {
                    System.out.println("Blocking build in spawn area");
                    return RustGameHooks.denyBuild();
                }
            }
            return null; // Allow
        });

        // Keep running
        try {
            Thread.sleep(Long.MAX_VALUE);
        } catch (InterruptedException e) {
            api.shutdown();
        }
    }
}
```

## Available Events

```java
// Player events
RustGameEvents.onPlayerConnected((playerId, playerName, steamId, position) -> { });
RustGameEvents.onPlayerDisconnected((playerId, playerName, reason) -> { });
RustGameEvents.onPlayerDamage((playerId, damage, damageType, attackerId) -> { });
RustGameEvents.onPlayerDeath((playerId, killerId, weapon) -> { });
RustGameEvents.onPlayerRespawn((playerId, location) -> { });

// Chat events
RustGameEvents.onChatMessage((playerId, playerName, message) -> { });

// Building events
RustGameEvents.onStructurePlaced((playerId, structureType, location) -> { });
RustGameEvents.onStructureDestroyed((structureId, destroyerId) -> { });

// Entity events
RustGameEvents.onEntitySpawned((entityId, entityType, location) -> { });
RustGameEvents.onEntityKilled((entityId, entityType, killerId) -> { });

// Crafting events
RustGameEvents.onItemCrafted((playerId, itemName, amount) -> { });
```

## Available Hooks

```java
// Connection control
RustGameHooks.onPlayerConnecting((playerId, playerName, steamId, ipAddress) -> {
    return null;  // Allow
    // or: return RustGameHooks.denyConnection("Reason");
});

// Damage modification
RustGameHooks.onPlayerTakingDamage((playerId, damage, damageType, attackerId) -> {
    return null;  // Default damage
    // or: return RustGameHooks.modifyDamage(newDamage);
    // or: return RustGameHooks.cancelDamage();
});

// Chat filtering
RustGameHooks.onPlayerChat((playerId, message) -> {
    return null;  // Allow message
    // or: return RustGameHooks.blockChat();
    // or: return RustGameHooks.modifyChat("new message");
});

// Building control
RustGameHooks.onPlayerBuild((playerId, structureType, location) -> {
    return null;  // Allow build
    // or: return RustGameHooks.denyBuild();
});

// Loot control
RustGameHooks.onPlayerLoot((playerId, containerId) -> {
    return null;  // Allow loot
    // or: return RustGameHooks.denyLoot();
});
```

## Troubleshooting

### Plugin not loading?

- Check `carbon/logs/carbon.log` for errors
- Verify .NET Framework 4.7.2+ installed
- Ensure DLL is unblocked (Windows: Right-click → Properties → Unblock)

### Java can't connect?

- Verify Carbon plugin initialized (check console)
- Check IPC endpoint exists:
    - Windows: Named pipe should be active
    - Linux: Check `/tmp/rust-java-mods.sock`

### Need more help?

- See [ARCHITECTURE.md](ARCHITECTURE.md) for technical details
- See [CARBON_SETUP.md](CARBON_SETUP.md) for detailed setup

## Performance Tips

💡 **FlatBuffers**: Data is read directly from buffer (no parsing)  
💡 **Hooks**: Return `null` to allow default behavior (fastest)  
💡 **Events**: Fire-and-forget (no response needed)  
💡 **Timeouts**: Hooks have 100ms timeout to prevent game hangs  
💡 **HMR**: Use watch-java.sh for instant reloads during development

---

**Ready to build awesome mods? Let's go! 🚀**
