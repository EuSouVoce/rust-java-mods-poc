# Project Philosophy

Guiding principles for the Rust-Java Modding Framework.

## Core Values

**Performance First**

- Zero-copy with FlatBuffers
- No JSON parsing overhead
- Minimal allocations on hot paths
- Sub-millisecond hook latency

**Safety by Default**

- Fail-safe to default behavior on errors
- 100ms hook timeout prevents game hangs
- Validate all incoming data
- Localhost-only IPC

**Explicit Over Implicit**

- Typed FlatBuffers schema
- Clear hook/event contracts
- Documented response formats
- No hidden side effects

**Simple Surface Area**

- Minimal API exposure
- Granular, single-purpose hooks
- Clear separation: events (notify) vs hooks (intercept)

**Observable Behavior**

- Log important events
- Deterministic failure modes
- Easy debugging paths

**Testable Design**

- Every change testable via example mod
- HMR for rapid iteration
- Clear logs for verification

## Design Questions

When adding features, ask:

1. Can this be zero-copy?
2. Does it need to block the game thread?
3. Is the schema additive (backward compatible)?
4. Can it be tested with the example mod?
5. Is the failure mode safe for the game?

## Trade-offs

| We Prefer | Over |
|-----------|------|
| Performance | Convenience |
| Type safety | Dynamic flexibility |
| Explicit API | Magic behavior |
| Zero-copy | Easy serialization |
| Fail-safe | Fail-loud |

## Breaking Changes

This is a POC — breaking changes are acceptable now.

For future stability:

- Schema evolution via FlatBuffers
- Versioned message types
- Migration documentation
