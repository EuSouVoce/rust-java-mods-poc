# Future-Proofing Strategy

Versioning and schema evolution approach for when this POC becomes production-ready.

## Current State (POC)

- Breaking changes allowed
- No versioning required
- Schema can change freely
- No migration support

## Future Versioning Strategy

### FlatBuffers Schema Evolution

FlatBuffers supports backward-compatible evolution:

**Safe Changes (Additive)**

- Add new tables
- Add new fields at end of tables (with defaults)
- Add new enum values
- Add new union members

**Breaking Changes (Avoid)**

- Remove fields
- Change field types
- Reorder fields
- Change field IDs
- Remove enum values

### Recommended Approach

1. **Schema Version Field**

```fbs
table ProtocolInfo {
    schema_version: uint16 = 1;
    min_compatible: uint16 = 1;
}

table Message {
    protocol: ProtocolInfo;
    // ... existing fields
}
```

1. **Deprecation Pattern**

```fbs
table PlayerInfo {
    id: uint64;
    name: string;
    health: float;
    
    // DEPRECATED in v2 - use 'max_health' instead
    old_max_health: float (deprecated);
    max_health: float = 100.0;
}
```

1. **Union Versioning**

```fbs
// Add new events at end, never remove
union EventPayload {
    // v1
    PlayerConnected,
    PlayerDisconnected,
    ChatMessage,
    
    // v2 additions
    PlayerRespawned,
    BuildingPlaced,
}
```

### Version Negotiation

Future protocol handshake:

```
1. Java sends: VersionRequest { client_version: 2 }
2. C# responds: VersionResponse { server_version: 3, min_supported: 1 }
3. Both use min(client_version, server_version) features
```

## Migration Path

When transitioning from POC to stable:

### Phase 1: Feature Freeze

- Lock schema structure
- Document all tables/unions
- Add schema_version field

### Phase 2: Compatibility Layer

- Add version negotiation
- Implement fallback handlers
- Log version mismatches

### Phase 3: Deprecation Cycle

- Mark old fields as deprecated
- Add replacement fields
- Document migration steps

### Phase 4: Removal

- Remove deprecated fields after 2 major versions
- Bump min_compatible version

## File Identifier

The `"RJMS"` identifier validates buffer type:

```fbs
file_identifier "RJMS";
```

Future: Include version in identifier (e.g., `"RJM2"`) for major incompatibilities.

## Tooling Recommendations

### Schema Diff Tool

```bash
# Compare schema versions
flatc --schema -o diff/ old.fbs new.fbs
diff diff/old.bfbs diff/new.bfbs
```

### Compatibility Check

```bash
# Verify backward compatibility
flatc --conform prev_version.bfbs rust_java_mods.fbs
```

## Documentation Requirements

For each release:

- CHANGELOG.md with schema changes
- Migration guide for breaking changes
- Min/max compatible version matrix
- Example upgrade code

## Summary

| Phase | Breaking OK | Version Field | Migration |
|-------|-------------|---------------|-----------|
| POC (now) | Yes | No | None |
| Alpha | Limited | Yes | Notes |
| Beta | No | Yes | Guide |
| Stable | Never | Yes + Negotiation | Full |
