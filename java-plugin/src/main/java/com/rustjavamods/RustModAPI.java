package com.rustjavamods;

import com.google.gson.Gson;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;

import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.CompletableFuture;
import java.util.function.Consumer;

/**
 * Main API for Rust game modding in Java
 * Provides hooks and event handlers for the Facepunch Rust game
 */
public class RustModAPI {
    private static RustModAPI instance;
    private final Gson gson;
    private final Map<String, Consumer<GameEvent>> eventHandlers;
    private final NativeBridge bridge;

    private RustModAPI() {
        this.gson = new Gson();
        this.eventHandlers = new HashMap<>();
        this.bridge = new NativeBridge();
    }

    public static synchronized RustModAPI getInstance() {
        if (instance == null) {
            instance = new RustModAPI();
        }
        return instance;
    }

    /**
     * Initialize the modding framework
     */
    public void initialize() {
        bridge.initialize();
        System.out.println("[RustModAPI] Initialized for Facepunch Rust game");
    }

    /**
     * Register a hook for Rust game events
     */
    public void registerHook(String hookName, GameHook hook) {
        bridge.registerHook(hookName, hook);
    }

    /**
     * Register an event handler for game events
     */
    public void registerEventHandler(String eventName, Consumer<GameEvent> handler) {
        eventHandlers.put(eventName, handler);
    }

    /**
     * Trigger a game event (called from native bridge)
     */
    public void handleEvent(GameEvent event) {
        Consumer<GameEvent> handler = eventHandlers.get(event.getName());
        if (handler != null) {
            handler.accept(event);
        }
    }

    /**
     * Call a Rust game API method
     */
    public CompletableFuture<JsonElement> callGameAPI(String method, JsonObject params) {
        return bridge.sendRequest(method, params);
    }

    /**
     * Shutdown the framework
     */
    public void shutdown() {
        bridge.shutdown();
    }
}
