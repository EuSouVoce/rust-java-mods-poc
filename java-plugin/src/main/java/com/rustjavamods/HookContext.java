package com.rustjavamods;

import com.google.gson.JsonObject;

import java.util.Map;

/**
 * Context for hook execution with Rust game data
 */
public class HookContext {
    private final String hookName;
    private final JsonObject parameters;
    private final Map<String, String> metadata;

    public HookContext(String hookName, JsonObject parameters, Map<String, String> metadata) {
        this.hookName = hookName;
        this.parameters = parameters;
        this.metadata = metadata;
    }

    public String getHookName() {
        return hookName;
    }

    public JsonObject getParameters() {
        return parameters;
    }

    public Map<String, String> getMetadata() {
        return metadata;
    }

    @Override
    public String toString() {
        return "HookContext{hookName='" + hookName + "', parameters=" + parameters + ", metadata=" + metadata + '}';
    }
}
