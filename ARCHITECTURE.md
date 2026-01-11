# Architecture Documentation

## Overview

This framework enables Java-based modding for Facepunch's Rust game using **Carbon** (the official modding framework) as the bridge:

1. **Carbon C# Plugin** - Integrates with Rust game, captures hooks and events
2. **IPC Layer** - OS-native communication (Named Pipes/Unix Sockets)
3. **Java Plugin API** - Type-safe modding interface

## Why Carbon?

Carbon is the **official modding framework** recommended on the Facepunch Rust wiki. It provides:
- Native integration with Rust game
- High performance hook system
- Active development and support
- No third-party dependencies (unlike Oxide/uMod)

## Communication Flow

### Request-Response Pattern (Named Pipes / Unix Sockets)

```
Java Plugin                Carbon Plugin           Rust Game
    │                          │                       │
    │── RegisterHook ──────────>│                       │
    │                          │<── Game Event ────────│
    │                          │ [Process Hook]        │
    │<──────── Result ──────────│                       │
    │                          │── Return to Game ─────>│
```

### Event Pattern (Fire-and-Forget)

```
Rust Game Event            Carbon Plugin          Java Plugin
    │                          │                       │
    │── Player Damage ─────────>│                       │
    │                          │ [Forward via IPC] ────>│
    │                          │                       │ [Event Handler]
    │                          │                       │
```

## OS Auto-Detection

The Carbon plugin automatically selects the appropriate IPC mechanism:

- **Windows**: Named Pipes (`\\.\pipe\rust-java-mods`)
- **Linux**: Unix Domain Sockets (`/tmp/rust-java-mods.sock`)

This is handled using `RuntimeInformation.IsOSPlatform()`.

## IPC Message Format

All messages use JSON (Newtonsoft.Json) for serialization:

**Event Message:**
```json
{
  "Type": "Event",
  "Name": "player_damage",
  "Data": {
    "player_id": "76561198012345678",
    "damage": 25.5,
    "damage_type": "Bullet",
    "attacker_id": "76561198087654321"
  }
}
```

**Hook Message:**
```json
{
  "Type": "Hook",
  "Method": "player_taking_damage",
  "Data": {
    "player_id": "76561198012345678",
    "damage": 25.5,
    "damage_type": "Fall"
  }
}
```

**Hook Response:**
```json
{
  "damage": 12.75,
  "cancel": false
}
```

## Carbon Plugin Hooks

The RustJavaBridge plugin hooks into these Carbon events:

### Player Hooks
- `OnPlayerConnected` - Player joins
- `OnPlayerDisconnected` - Player leaves
- `OnPlayerChat` - Chat messages
- `OnPlayerDeath` - Player death
- `OnPlayerRespawned` - Player respawn
- `OnEntityTakeDamage` - Damage events

### Building Hooks
- `OnEntityBuilt` - Structure placement
- `OnStructureDemolish` - Structure destruction

### Entity Hooks
- `OnEntitySpawned` - Entity spawning
- `OnEntityKill` - Entity destruction

### Loot/Craft Hooks
- `OnLootPlayer` - Looting events
- `OnItemCraftFinished` - Crafting completion

## Threading Model

### Carbon Plugin (C# in Rust Game)

- **Main Unity Thread**: All game hooks execute here (Carbon requirement)
- **IPC Server Thread**: Background thread for accepting connections
- **Client Handler Threads**: ThreadPool for handling individual client requests

### Java Plugin

- **Main Thread**: Plugin initialization
- **Event Dispatch Thread Pool**: Handles event callbacks
- **Hook Execution Thread Pool**: Executes hook handlers
- **IPC Client Thread**: Maintains connection to Carbon plugin

## Hook System

Hooks allow intercepting and modifying game behavior **before** it happens:

```
1. Game calls Carbon hook (e.g., OnEntityTakeDamage)
2. Carbon plugin forwards to Java via IPC (synchronous)
3. Java processes hook and returns modification
4. Carbon plugin applies modification
5. Game continues with modified values
```

Example: Reducing fall damage by 50%

```
Rust Game: Player takes 100 fall damage
    ↓
Carbon: OnEntityTakeDamage hook triggered
    ↓
Java: Hook handler receives damage=100, type="Fall"
Java: Returns {damage: 50}
    ↓
Carbon: Modifies damage to 50
    ↓
Rust Game: Player takes 50 damage instead
```

## Event System

Events provide notifications **after** something happens (fire-and-forget):

```
1. Game event occurs (e.g., player dies)
2. Carbon hook captures it
3. Carbon plugin sends event to Java (async, no wait)
4. Java event handlers process asynchronously
5. No response expected or needed
```

## Performance Optimization

### Low Latency

- Direct OS-level IPC (no network stack)
- JSON serialization (fast with Newtonsoft.Json)
- Connection pooling
- Background threads for I/O

### Hook Performance

- Hooks are synchronous but fast (< 1ms typical)
- Minimal serialization overhead
- Direct socket communication
- No reflection or dynamic code generation

### Event Performance

- Fire-and-forget (zero wait time)
- Events batched when possible
- Background thread processing
- No game thread blocking

### Memory Efficiency

- Reusable buffers for IPC
- Efficient JSON serialization
- Connection pooling
- Minimal allocations in hot paths

## Deployment

### Carbon Plugin Deployment

```
RustServer/
├── carbon/
│   ├── plugins/
│   │   └── RustJavaBridge.dll    ← Deploy here
│   └── ...
└── ...
```

### File Structure

```
rust-java-mods-poc/
├── csharp-bridge/
│   ├── RustJavaBridge.cs          # Carbon plugin source
│   ├── RustJavaModsBridge.csproj  # C# project
│   └── lib/                       # Carbon/Unity references
├── java-plugin/
│   ├── src/main/java/             # Java API source
│   ├── examples/                  # Example mods
│   └── pom.xml                    # Maven config
└── README.md
```

## Security Considerations

- IPC restricted to localhost only
- File permissions (0600) on Unix sockets
- Input validation on all boundaries
- No arbitrary code execution
- Carbon's security model applies
- Plugin runs in game process sandbox

## Debugging

### Carbon Plugin Debugging

```bash
# Watch Carbon console output
tail -f carbon/logs/carbon.log

# Enable verbose logging in RustJavaBridge.cs
_plugin.Puts("Debug message");
```

### Java Plugin Debugging

```bash
# Run with logging
java -Djava.util.logging.level=FINE -jar plugin.jar

# Connect debugger on port 5005
java -agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=5005 -jar plugin.jar
```

### Testing IPC

```bash
# Test Named Pipe (Windows PowerShell)
$pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "rust-java-mods", "InOut")
$pipe.Connect()

# Test Unix Socket (Linux)
nc -U /tmp/rust-java-mods.sock
```

## Future Enhancements

- Bi-directional API calls (Java → Rust game)
- Plugin hot-reload without server restart
- Built-in metrics and monitoring
- Web dashboard for plugin management
- Multiple Java plugin support
- Plugin dependency management
- Automatic Carbon version detection

## Carbon Compatibility

- **Target**: Carbon 1.x and 2.x
- **Unity**: 2021.3 LTS (used by Rust)
- **.NET**: Framework 4.7.2+
- **Rust Game**: Latest stable version

## Troubleshooting

### Plugin not loading
- Check `carbon/logs/carbon.log` for errors
- Verify .NET Framework 4.7.2+ installed
- Ensure DLL is in `carbon/plugins/` directory

### IPC Connection Failed
- Check firewall settings
- Verify socket/pipe path is accessible
- Ensure Carbon plugin initialized successfully

### Hooks not firing
- Verify Java plugin is connected
- Check Carbon console for hook messages
- Enable verbose logging in both layers
