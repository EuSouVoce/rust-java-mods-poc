# LLM Development Guide

Guidelines for using AI assistants to contribute to this project.

## Context to Provide

When asking an LLM to make changes:

```
Project: Rust-Java 21 modding framework using Carbon + FlatBuffers + Virtual Threads
Build: ./build.sh (generates schema, builds C# and Java)
Java: 21+ with Virtual Threads, pattern matching, records, switch expressions
Protocol: FlatBuffers (zero-copy binary) over Named Pipes/Unix Sockets
```

## Prompt Templates

### Schema Change

```
Add a new [event/hook] to the FlatBuffers schema for [description].

Schema file: schema/rust_java_mods.fbs
Protocol: FlatBuffers with unions for EventPayload/HookPayload

Requirements:
- Add table definition
- Add to appropriate union
- Include all necessary fields with types
- Return the modified .fbs section
```

### C# Bridge Change

```
Update the Carbon plugin to handle [new event/hook].

File: csharp-bridge/RustJavaBridge.cs
Pattern: Use GetBuilder(), build FlatBuffer, call _ipcServer.SendMessage()

Requirements:
- Hook into Carbon event
- Build FlatBuffer message
- For hooks: use SendHookAndWait() with timeout
- Return the new method
```

### Java API Change (Java 21)

```
Add Java 21 API for [new event/hook].

Files:
- java-plugin/src/main/java/com/rustjavamods/game/RustGameEvents.java (events)
- java-plugin/src/main/java/com/rustjavamods/game/RustGameHooks.java (hooks)

Java 21 patterns to use:
- var for local type inference
- Pattern matching instanceof for payload extraction
- Switch expressions with arrow syntax
- Virtual threads via Thread.ofVirtual() for I/O

Requirements:
- Add handler registration method with pattern matching
- Add functional interface
- Add response builder (for hooks) using var
```

### Documentation Update

```
Update [file] to document [change].

Style: Concise, technical, include code examples
Format: Markdown with fenced code blocks
```

## Code Style

### C# (Carbon)

```csharp
// Use pooled builder
var builder = GetBuilder();

// Create strings before using them
var nameOffset = builder.CreateString(value);

// Build table
SomeType.StartSomeType(builder);
SomeType.AddField(builder, nameOffset);
var offset = SomeType.EndSomeType(builder);

// Wrap in Message
Message.StartMessage(builder);
Message.AddMsgType(builder, MessageType.Event);
Message.AddPayloadType(builder, MessagePayload.GameEvent);
Message.AddPayload(builder, offset.Value);
var msgOffset = Message.EndMessage(builder);

builder.Finish(msgOffset.Value);
_ipcServer.SendMessage(builder.SizedByteArray());
```

### Java (Java 21)

```java
// Event handler with pattern matching
RustGameEvents.onSomeEvent((param1, param2) -> {
    // Zero-copy read from FlatBuffer via pattern matching
    System.out.printf("[EVENT] %s: %s%n", param1, param2);
});

// Hook handler with switch expression
RustGameHooks.onSomeHook((param1, param2) -> {
    return switch (param1) {
        case "special" -> RustGameHooks.someResponseBuilder(newValue);
        default -> null; // Allow default
    };
});

// Virtual thread for I/O
Thread.ofVirtual()
    .name("task-name")
    .start(() -> performIoOperation());

// Pattern matching instanceof
if (event.payload(new SomePayload()) instanceof SomePayload payload) {
    var data = payload.someField();
}
```

## Safety Rules for LLMs

- Never generate secrets or credentials
- Maintain 100ms hook timeout
- Always validate incoming data
- Keep IPC localhost-only
- Test changes with example mod
- Use Virtual Threads for I/O, not platform threads

## Output Format

Request:

- Unified diff for small changes
- Full file for new files
- Code block with language tag
- One-line commit message

## Verification

After LLM changes:

1. `./build.sh` must succeed
2. No new compiler warnings
3. Example mod still works
4. Logs show expected output
