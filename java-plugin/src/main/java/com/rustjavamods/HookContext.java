package com.rustjavamods;

import RustJavaMods.Protocol.GameHook;

/**
 * Context for hook execution with Rust game data (FlatBuffers).
 * Provides zero-copy access to hook parameters.
 * 
 * Java 21: Using record for immutable data carrier.
 */
public record HookContext(GameHook hook) {

    public String getHookName() {
        return hook.hookName();
    }

    public long getHookId() {
        return hook.hookId();
    }

    public long getTimestamp() {
        return hook.timestamp();
    }

    public byte getPayloadType() {
        return hook.payloadType();
    }

    /**
     * Get the raw hook for direct FlatBuffer access
     */
    public GameHook getRawHook() {
        return hook;
    }

    @Override
    public String toString() {
        return "HookContext[hookName='%s', hookId=%d]".formatted(getHookName(), getHookId());
    }
}
