package com.rustjavamods;

import RustJavaMods.Protocol.*;
import com.google.flatbuffers.FlatBufferBuilder;

import java.nio.ByteBuffer;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * Main API for Rust game modding in Java.
 * Uses FlatBuffers for zero-copy high-performance IPC.
 * 
 * Java 21 features:
 * - Virtual Threads for concurrent event/hook handling
 * - Pattern matching switch expressions
 * - var for local type inference
 */
public final class RustModAPI {
    private static volatile RustModAPI instance;

    // Thread-local FlatBufferBuilder for efficient reuse
    private static final ThreadLocal<FlatBufferBuilder> builderPool = ThreadLocal
            .withInitial(() -> new FlatBufferBuilder(1024));

    private final Map<String, Consumer<GameEvent>> eventHandlers = new ConcurrentHashMap<>();
    private final Map<String, Function<HookContext, ByteBuffer>> hookHandlers = new ConcurrentHashMap<>();
    private final NativeBridge bridge;

    private RustModAPI() {
        this.bridge = new NativeBridge(this);
    }

    /**
     * Get the singleton instance (double-checked locking for thread safety)
     */
    public static RustModAPI getInstance() {
        if (instance == null) {
            synchronized (RustModAPI.class) {
                if (instance == null) {
                    instance = new RustModAPI();
                }
            }
        }
        return instance;
    }

    /**
     * Get a thread-local FlatBufferBuilder (cleared and ready to use)
     */
    public static FlatBufferBuilder getBuilder() {
        var builder = builderPool.get();
        builder.clear();
        return builder;
    }

    /**
     * Initialize the modding framework
     */
    public void initialize() {
        bridge.initialize();
        System.out.println("[RustModAPI] Initialized (FlatBuffers + Virtual Threads)");
    }

    /**
     * Register a hook handler that returns a FlatBuffer response
     */
    public void registerHook(String hookName, Function<HookContext, ByteBuffer> hook) {
        hookHandlers.put(hookName, hook);
        bridge.registerHook(hookName);
    }

    /**
     * Register an event handler
     */
    public void registerEventHandler(String eventName, Consumer<GameEvent> handler) {
        eventHandlers.put(eventName, handler);
    }

    /**
     * Handle incoming message from Carbon bridge.
     * Uses Java 21 pattern matching switch for cleaner dispatch.
     */
    void handleMessage(byte[] data) {
        var buffer = ByteBuffer.wrap(data);
        var msg = Message.getRootAsMessage(buffer);

        // Pattern matching switch (Java 21)
        switch (msg.msgType()) {
            case MessageType.Event -> handleEvent(msg);
            case MessageType.Hook -> handleHook(msg);
            case MessageType.CommandResponse -> handleCommandResponse(msg);
            default -> System.err.println("[RustModAPI] Unknown message type: " + msg.msgType());
        }
    }

    private void handleCommandResponse(Message msg) {
        if (msg.payloadType() != MessagePayload.CommandResponse) {
            return;
        }

        var resp = (CommandResponse) msg.payload(new CommandResponse());
        if (resp == null) {
            return;
        }

        bridge.onCommandResponse((int) resp.commandId(), resp.success(), resp.errorMessage());
    }

    private void handleEvent(Message msg) {
        if (msg.payloadType() != MessagePayload.GameEvent)
            return;

        var gameEvent = (GameEvent) msg.payload(new GameEvent());
        if (gameEvent == null)
            return;

        var eventName = gameEvent.eventName();
        var handler = eventHandlers.get(eventName);

        if (handler != null) {
            // Run event handler on virtual thread for non-blocking execution
            Thread.ofVirtual()
                    .name("event-" + eventName)
                    .start(() -> {
                        try {
                            handler.accept(gameEvent);
                        } catch (Exception e) {
                            System.err.println("[RustModAPI] Event handler error: " + e.getMessage());
                        }
                    });
        }
    }

    private void handleHook(Message msg) {
        if (msg.payloadType() != MessagePayload.GameHook)
            return;

        var gameHook = (GameHook) msg.payload(new GameHook());
        if (gameHook == null)
            return;

        var hookName = gameHook.hookName();
        long hookId = gameHook.hookId();

        var handler = hookHandlers.get(hookName);

        ByteBuffer response = null;
        if (handler != null) {
            try {
                var context = new HookContext(gameHook);
                response = handler.apply(context);
            } catch (Exception e) {
                System.err.println("[RustModAPI] Hook handler error: " + e.getMessage());
            }
        }

        // Send response (null = allow default behavior)
        bridge.sendHookResponse(hookId, response);
    }

    /**
     * Shutdown the framework
     */
    public void shutdown() {
        bridge.shutdown();
    }

    // --- Carbon-parity helpers via bridge commands ---

    /** Send a chat message to a player (or broadcast if playerId is null). */
    public CommandResult sendChat(String playerIdOrNull, String message) {
        return bridge.sendChat(playerIdOrNull, message);
    }

    /** Kick a player by their Rust user ID string. */
    public CommandResult kickPlayer(String playerId, String reasonOrNull) {
        return bridge.kickPlayer(playerId, reasonOrNull);
    }

    /** Teleport a player by their Rust user ID string. */
    public CommandResult teleportPlayer(String playerId, float x, float y, float z) {
        return bridge.teleportPlayer(playerId, x, y, z);
    }

    /** Give an item to a player by shortname. */
    public CommandResult giveItem(String playerId, String itemShortName, int amount) {
        return bridge.giveItem(playerId, itemShortName, amount);
    }

    NativeBridge getBridge() {
        return bridge;
    }
}
