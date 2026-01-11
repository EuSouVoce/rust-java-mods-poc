package com.rustjavamods;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;

/**
 * Game hook interface for intercepting Rust game events
 */
@FunctionalInterface
public interface GameHook {
    /**
     * Execute the hook
     * @param context Hook context with game data
     * @return Result to pass back to the game
     */
    JsonElement execute(HookContext context);
}
