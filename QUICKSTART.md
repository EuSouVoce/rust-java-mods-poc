# Quick Start Guide

Get your Java mod running on Facepunch Rust in 5 minutes!

## Prerequisites

✅ Rust dedicated server with Carbon installed  
✅ .NET Framework 4.7.2+ / Mono  
✅ Java 11+ JDK

## 1. Build the Framework (2 minutes)

```bash
# Clone the repository
git clone https://github.com/EuSouVoce/rust-java-mods-poc.git
cd rust-java-mods-poc

# Build everything
./build.sh
```

## 2. Deploy Carbon Plugin (1 minute)

```bash
# Copy the plugin to your Rust server
cp csharp-bridge/bin/Release/net472/RustJavaBridge.dll /path/to/rust-server/carbon/plugins/

# Restart Rust server or hot-reload
# Server console: c.reload RustJavaBridge
```

## 3. Run the Example Mod (2 minutes)

```bash
# Compile the example
cd java-plugin
javac -cp target/rust-java-mods-api-0.1.0.jar examples/WelcomeModExample.java

# Run it
java -cp target/rust-java-mods-api-0.1.0.jar:examples WelcomeModExample
```

## 4. See It Work!

Connect to your Rust server and watch:

**Server Console:**
```
[RustJavaBridge] Event: player_connected
```

**Java Console:**
```
[EVENT] Player connected: YourName (ID: 76561..., Steam: 76561...)
→ Sending welcome message to YourName
```

## What's Happening?

1. **Carbon Plugin** hooks into Rust game events
2. **IPC** forwards events to Java via Named Pipes/Unix Sockets  
3. **Java Mod** receives events and can respond with actions

## Next Steps

### Add More Event Handlers

```java
RustGameEvents.onPlayerDeath((playerId, killerId, weapon) -> {
    System.out.println(playerId + " was killed by " + killerId + " with " + weapon);
});

RustGameEvents.onChatMessage((playerId, playerName, message) -> {
    if (message.startsWith("!help")) {
        System.out.println("Player " + playerName + " needs help!");
    }
});
```

### Create Game Hooks

```java
// Reduce fall damage by 50%
RustGameHooks.onPlayerTakingDamage((playerId, damage, damageType) -> {
    if (damageType.equals("Fall")) {
        JsonObject response = new JsonObject();
        response.addProperty("damage", damage * 0.5f);
        return response;
    }
    return null;
});

// Block building in spawn area
RustGameHooks.onPlayerBuild((playerId, structureType, location) -> {
    double x = location.get("x").getAsDouble();
    double z = location.get("z").getAsDouble();
    
    if (Math.abs(x) < 100 && Math.abs(z) < 100) {
        JsonObject response = new JsonObject();
        response.addProperty("allow", false);
        return response;
    }
    return null;
});
```

## Troubleshooting

### Plugin not loading?
Check `carbon/logs/carbon.log` for errors

### Java can't connect?
Verify IPC endpoint exists:
- Windows: Named pipe should be active
- Linux: Check `/tmp/rust-java-mods.sock`

### Need more help?
See [CARBON_SETUP.md](CARBON_SETUP.md) for detailed setup  
See [ARCHITECTURE.md](ARCHITECTURE.md) for technical details

## Example Mod Structure

```java
import com.rustjavamods.RustModAPI;
import com.rustjavamods.game.*;

public class MyAwesomeMod {
    public static void main(String[] args) {
        // Initialize
        RustModAPI api = RustModAPI.getInstance();
        api.initialize();

        // Register events
        RustGameEvents.onPlayerConnected((id, name, steam) -> {
            // Welcome player
        });

        // Register hooks
        RustGameHooks.onPlayerTakingDamage((id, dmg, type) -> {
            // Modify damage
            return null;
        });

        // Keep running
        System.out.println("Mod loaded!");
        try {
            Thread.sleep(Long.MAX_VALUE);
        } catch (InterruptedException e) {
            api.shutdown();
        }
    }
}
```

## Available Events

- `player_connected`, `player_disconnected`
- `player_damage`, `player_death`, `player_respawn`
- `chat_message`
- `structure_placed`, `structure_destroyed`
- `entity_spawned`, `entity_killed`
- `item_crafted`

## Available Hooks

- `player_chat` - Filter/modify messages
- `player_taking_damage` - Modify/cancel damage
- `player_build` - Allow/deny building
- `player_loot` - Control looting

## Pro Tips

💡 **Performance**: Events are fire-and-forget (fast!)  
💡 **Hooks**: Return `null` to allow default behavior  
💡 **Testing**: Enable verbose logging with `-Djava.util.logging.level=FINE`  
💡 **Debugging**: Check both Carbon and Java console outputs  
💡 **Hot Reload**: Carbon supports plugin hot-reload with `c.reload`

---

**Ready to build awesome mods? Let's go! 🚀**
