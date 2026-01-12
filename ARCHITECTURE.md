# Architecture Documentation

## Overview

This framework enables Java 21-based modding for Facepunch's Rust game using **Carbon** (the recommended modding framework) with **FlatBuffers** for zero-copy IPC and **Virtual Threads** for scalable concurrency:

1. **Carbon C# Plugin** - Integrates with Rust game, captures hooks and events
2. **FlatBuffers IPC** - Zero-copy binary serialization over Named Pipes/Unix Sockets
3. **Java 21 Plugin API** - Type-safe modding interface with Virtual Threads

## Why Java 21?

Java 21 provides significant concurrency and language improvements:

| Feature | Benefit |
| ------- | ------- |
| Virtual Threads | Lightweight threads (~KB stack), perfect for I/O-bound IPC |
| Pattern Matching | Cleaner FlatBuffer payload extraction |
| Switch Expressions | Concise message dispatch |
| Records | Immutable data carriers for hook context |
| Sequenced Collections | Ordered data handling |

## Why FlatBuffers?

FlatBuffers provides significant performance benefits over JSON:

| Aspect | JSON (old) | FlatBuffers (new) |
| ------ | ---------- | ----------------- |
| Parsing | Full parse required | Zero-copy access |
| Memory | String allocations | Direct buffer read |
| Latency | ~1-5ms per message | ~0.01-0.1ms |
| Type safety | Runtime errors | Compile-time schema |
| Binary size | Verbose text | Compact binary |

## Communication Flow

### Request-Response Pattern (Hooks)

```
Java Plugin      Carbon Plugin             Rust Game
  │                    │                       │
  │                    │<── OnEntityTakeDamage─│
  │                    │  [Build FlatBuffer]   │
  │<── GameHook ───────│                       │
  │ [Zero-copy read]   │                       │
  │── HookResponse ───>│                       │
  │                    │  [Apply modification] │
  │                    │──── Return value ────>│
```

### Event Pattern (Fire-and-Forget)

```
Rust Event       Carbon Plugin           Java Plugin
  │                    │                     │
  │── Player Death ───>│                     │
  │                    │ [Build FlatBuffer]  │
  │                    │── GameEvent ───────>│
  │                    │                     │ [Zero-copy read]
  │                    │                     │ [No response]
```

## FlatBuffers Schema

The protocol is defined in `schema/rust_java_mods.fbs`:

```flatbuffers
// Message wrapper
table Message {
    msg_type: MessageType;
    payload: MessagePayload;
}

// Events (fire-and-forget)
table GameEvent {
    event_name: string;
    timestamp: ulong;
    payload: EventPayload;  // Union of event types
}

// Hooks (request-response)
table GameHook {
    hook_id: uint;          // Correlation ID
    hook_name: string;
    timestamp: ulong;
    payload: HookPayload;   // Union of hook types
}

// Hook responses
table HookResponse {
    hook_id: uint;          // Matching correlation ID
    payload: HookResponsePayload;
}
```

### Damage Types (Enum)

```flatbuffers
enum DamageType : byte {
    Unknown, Generic, Bullet, Slash, Blunt, Fall,
    Radiation, Bite, Stab, Explosion, Heat, Cold,
    Bleeding, Poison, Hunger, Thirst, Drowned, ElectricShock
}
```

### Vector3 (Struct)

```flatbuffers
struct Vec3 {
    x: float;
    y: float;
    z: float;
}
```

## IPC Protocol

### Message Framing

All messages use length-prefixed framing:

```
┌──────────────┬─────────────────────────┐
│ Length (4B)  │ FlatBuffer payload      │
│ Little-endian│ (variable length)       │
└──────────────┴─────────────────────────┘
```

### OS Auto-Detection

- **Windows**: Named Pipes (`\\.\pipe\rust-java-mods`)
- **Linux**: Unix Domain Sockets (`/tmp/rust-java-mods.sock`)

Selected via `RuntimeInformation.IsOSPlatform()`.

## Threading Model

### Carbon Plugin (C#)

```
┌─────────────────────────────────────────────┐
│ Main Unity Thread                           │
│  - All Carbon hooks execute here            │
│  - FlatBuffer building (pooled builders)    │
│  - Hook timeout enforcement (100ms)         │
└─────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────┐
│ IPC Server Thread                           │
│  - Accept connections                       │
│  - Read hook responses                      │
│  - Signal pending hooks                     │
└─────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────┐
│ ThreadPool (Client Handlers)                │
│  - Per-client message handling              │
│  - Concurrent client support                │
└─────────────────────────────────────────────┘
```

### Java Plugin

```
┌─────────────────────────────────────────────┐
│ Main Thread                                 │
│  - Plugin initialization                    │
│  - Register handlers                        │
└─────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────┐
│ Virtual Thread: carbon-bridge-connection    │
│  - Connection management                    │
│  - Read incoming messages                   │
└─────────────────────────────────────────────┘
         │
         ▼ (spawns per message)
┌─────────────────────────────────────────────┐
│ Virtual Thread: message-handler             │
│  - Dispatch to event/hook handlers          │
│  - Non-blocking, scales to thousands        │
└─────────────────────────────────────────────┘
```

## Hook System

Hooks intercept game behavior **before** it happens:

1. Game calls Carbon hook (e.g., `OnEntityTakeDamage`)
2. Carbon builds FlatBuffer with hook data
3. Carbon sends to Java via IPC (with 100ms timeout)
4. Java handler reads data (zero-copy)
5. Java builds response FlatBuffer (if modifying)
6. Carbon applies modification or uses default
7. Game continues

### Example: Fall Damage Reduction

```
Rust: Player takes 100 fall damage
  ↓
Carbon: OnEntityTakeDamage triggered
  ↓
Carbon: Build PlayerTakingDamageHook FlatBuffer
        { hook_id: 42, damage: 100, damage_type: Fall }
  ↓
Java: Receive and read (zero-copy)
Java: Build PlayerDamageResponse { modified_damage: 50 }
  ↓
Carbon: Receive response, match hook_id: 42
Carbon: Scale damage to 50
  ↓
Rust: Player takes 50 damage
```

## Event System

Events notify **after** something happens (no response):

1. Game event occurs
2. Carbon captures via hook
3. Carbon builds FlatBuffer
4. Carbon sends to Java (fire-and-forget)
5. Java handlers process asynchronously

## Memory Efficiency

### C# Side

```csharp
// Thread-local pooled builders
private readonly ThreadLocal<FlatBufferBuilder> _builderPool = 
    new ThreadLocal<FlatBufferBuilder>(() => new FlatBufferBuilder(1024));

private FlatBufferBuilder GetBuilder() {
    var builder = _builderPool.Value;
    builder.Clear();  // Reset for reuse
    return builder;
}
```

### Java Side

```java
// Thread-local pooled builders
private static final ThreadLocal<FlatBufferBuilder> builderPool = 
    ThreadLocal.withInitial(() -> new FlatBufferBuilder(1024));

public static FlatBufferBuilder getBuilder() {
    FlatBufferBuilder builder = builderPool.get();
    builder.clear();
    return builder;
}
```

## Deployment

### Carbon Plugin

```
RustServer/
├── carbon/
│   ├── plugins/
│   │   └── RustJavaBridge.dll    ← Deploy here
│   └── logs/
│       └── carbon.log            ← Check for errors
└── ...
```

### File Structure

```
rust-java-mods-poc/
├── schema/
│   ├── rust_java_mods.fbs        # Protocol definition
│   ├── generate.sh               # Linux code generator
│   └── generate.bat              # Windows code generator
├── csharp-bridge/
│   ├── RustJavaBridge.cs         # Carbon plugin
│   ├── RustJavaModsBridge.csproj # Project file
│   └── Generated/                # Generated FlatBuffers C#
├── java-plugin/
│   ├── src/main/java/
│   │   ├── com/rustjavamods/     # API classes
│   │   └── RustJavaMods/Protocol/# Generated FlatBuffers Java
│   ├── examples/                 # Example mods
│   └── pom.xml                   # Maven config
├── scripts/
│   ├── dev-server.sh            # Dev environment
│   ├── watch-java.sh            # Java HMR
│   └── rebuild-bridge.sh        # C# rebuild
└── build.sh                     # Full build
```

## Security Considerations

- IPC restricted to localhost only
- File permissions (0600) on Unix sockets
- FlatBuffer verifier validates message integrity
- Hook timeout prevents game hangs (100ms)
- No arbitrary code execution

## Debugging

### Carbon Plugin

```bash
# Watch Carbon logs
tail -f carbon/logs/carbon.log

# Enable verbose logging (in code)
_plugin.Puts("Debug: received hook response");
```

### Java Plugin

```bash
# Run with debug output
java -Djava.util.logging.level=FINE -jar plugin.jar

# Remote debugging
java -agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=5005 -jar plugin.jar
```

### Schema Debugging

```bash
# Dump FlatBuffer contents (requires flatc)
flatc --json --raw-binary schema/rust_java_mods.fbs -- message.bin
```

## Future Enhancements

- [ ] Bi-directional API calls (Java → Rust game commands)
- [ ] Multiple Java plugin support
- [ ] Schema versioning/evolution
- [ ] Built-in metrics (latency, throughput)
- [ ] Web dashboard for monitoring
- [ ] Plugin dependency management

## Compatibility

- **Carbon**: 1.x and 2.x
- **Unity**: 2021.3 LTS (used by Rust)
- **.NET**: Framework 4.7.2+
- **Java**: 17+
- **FlatBuffers**: 24.3.25
- **Rust Game**: Latest stable

## Troubleshooting

### Plugin not loading

- Check `carbon/logs/carbon.log`
- Verify .NET Framework 4.7.2+
- Ensure DLL is in `carbon/plugins/`

### IPC Connection Failed

- Check firewall settings
- Verify socket/pipe path exists
- Ensure Carbon plugin initialized

### Hooks not firing

- Verify Java plugin is connected
- Check hook registration logs
- Enable verbose logging
