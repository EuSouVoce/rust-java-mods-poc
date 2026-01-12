# Developer Guidelines

Concise guidelines for contributing to this project. Prioritize performance, type safety, and clear cross-language contracts.

## Repository Layout

```
schema/           FlatBuffers schema definition (.fbs)
csharp-bridge/    Carbon plugin (C#, .NET 4.7.2)
java-plugin/      Java API and examples
scripts/          Development scripts
```

## Build Commands

```bash
# Full build (schema + C# + Java)
./build.sh

# Regenerate FlatBuffers code only
cd schema && ./generate.sh

# C# only
cd csharp-bridge && dotnet build --configuration Release

# Java only
cd java-plugin && mvn clean package
```

## Development

```bash
# Dev environment with HMR
./scripts/dev-server.sh      # Terminal 1
./scripts/watch-java.sh      # Terminal 2 (auto-rebuilds Java)

# Rebuild C# bridge only
./scripts/rebuild-bridge.sh
```

## Language Conventions

### C# (Carbon Plugin)

- Target `.NET Framework 4.7.2` (Unity compatibility)
- Use `ThreadLocal<FlatBufferBuilder>` for pooling
- Never block main Unity thread — use async/background threads
- Log with `Puts()` / `PrintError()`
- 100ms timeout for hook responses

### Java (Plugin API) - Java 21+

- Package: `com.rustjavamods.*`
- **Virtual Threads**: Use `Thread.ofVirtual()` for I/O operations
- **Pattern Matching**: Use `instanceof` patterns for payload extraction
- **Switch Expressions**: Use arrow syntax for message dispatch
- **Records**: Use for immutable data carriers (e.g., `HookContext`)
- **var**: Use for local type inference
- Use `ThreadLocal<FlatBufferBuilder>` for pooling
- Return `null` from hook handlers for default behavior
- Generated FlatBuffers code: `RustJavaMods.Protocol.*`

## FlatBuffers Schema

- **Location**: `schema/rust_java_mods.fbs`
- **Identifier**: `"RJMS"` (4 chars for validation)
- **Changes**: Regenerate code with `./schema/generate.sh`

### Adding New Messages

1. Add table/enum to `rust_java_mods.fbs`
2. Add to appropriate union (`EventPayload`, `HookPayload`, etc.)
3. Regenerate code
4. Update C# bridge and Java API
5. Add example usage

## Cross-Language Protocol

- All messages: FlatBuffers binary with 4-byte length prefix
- Message framing: `[length:4bytes][flatbuffer:N bytes]`
- Hook correlation: `hook_id` field for request/response matching
- Timeout: 100ms for hooks (fail-safe to default behavior)

## Performance Requirements

- Zero-copy reads from FlatBuffers
- Pooled `FlatBufferBuilder` per thread
- Virtual Threads for all I/O operations (Java)
- No blocking on main game thread
- Hook latency < 1ms typical
- Event delivery: fire-and-forget on virtual threads

## Testing

1. Build all: `./build.sh`
2. Deploy plugin: copy to `carbon/plugins/`
3. Run Java example: `mvn exec:java -Dexec.mainClass=...`
4. Check logs: `carbon/logs/carbon.log`

## Contribution Process

1. Create branch from `main`
2. Make small, focused changes
3. Update docs for API changes
4. Test with example mod
5. Submit PR with description

## Commit Messages

```
Short imperative summary (50 chars)

Optional body with details, rationale, and testing notes.
```

Examples:

- `Add player_respawn hook with location`
- `Fix FlatBuffer builder reuse in IPC server`
- `Update architecture docs for FlatBuffers`

## Security Rules

- IPC: localhost only (no network exposure)
- Message size: max 1MB enforced
- No secrets in code or docs
- Validate all incoming data
