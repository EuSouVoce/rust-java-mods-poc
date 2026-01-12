package com.rustjavamods;

import RustJavaMods.Protocol.*;
import com.google.flatbuffers.FlatBufferBuilder;

import java.io.*;
import java.net.UnixDomainSocketAddress;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.channels.SocketChannel;
import java.nio.file.Path;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.locks.ReentrantLock;

/**
 * Native bridge to communicate with Rust game through C# Carbon layer.
 * Uses FlatBuffers for zero-copy serialization over Named Pipes / Unix Sockets.
 * 
 * Java 21 features:
 * - Virtual Threads for lightweight, non-blocking I/O
 * - Pattern matching with instanceof
 * - var for local type inference
 */
final class NativeBridge {

    private final RustModAPI api;
    private final AtomicBoolean running = new AtomicBoolean(false);
    private final ReentrantLock writeLock = new ReentrantLock();
    private final boolean isWindows;
    private final String endpoint;

    // Connection state
    private volatile Thread connectionThread;
    private OutputStream outputStream;
    private InputStream inputStream;
    private SocketChannel unixChannel;
    private RandomAccessFile pipeFile;

    // Reusable buffers (thread-confined to connection thread)
    private final byte[] lengthBuffer = new byte[4];
    private final ByteBuffer lengthByteBuffer = ByteBuffer.wrap(lengthBuffer).order(ByteOrder.LITTLE_ENDIAN);

    // Command correlation
    private final AtomicInteger commandIdCounter = new AtomicInteger(0);
    private final ConcurrentHashMap<Integer, CompletableFuture<CommandResult>> pendingCommands = new ConcurrentHashMap<>();

    NativeBridge(RustModAPI api) {
        this.api = api;
        this.isWindows = System.getProperty("os.name").toLowerCase().contains("win");
        this.endpoint = isWindows
                ? "\\\\.\\pipe\\rust-java-mods"
                : "/tmp/rust-java-mods.sock";
    }

    void initialize() {
        System.out.println("[NativeBridge] Connecting to Carbon bridge...");
        System.out.println("[NativeBridge] Endpoint: " + endpoint);
        System.out.println("[NativeBridge] Protocol: FlatBuffers (zero-copy)");
        System.out.println("[NativeBridge] Threading: Virtual Threads (Java 21)");

        running.set(true);

        // Use Virtual Thread for connection loop - lightweight and efficient for I/O
        connectionThread = Thread.ofVirtual()
                .name("carbon-bridge-connection")
                .start(this::connectionLoop);
    }

    private void connectionLoop() {
        while (running.get()) {
            try {
                connect();
                readLoop();
            } catch (Exception e) {
                if (running.get()) {
                    System.err.println("[NativeBridge] Connection error: " + e.getMessage());
                    sleep(1000); // Reconnect delay
                }
            } finally {
                disconnect();
            }
        }
    }

    private void connect() throws IOException {
        if (isWindows) {
            connectNamedPipe();
        } else {
            connectUnixSocket();
        }
        System.out.println("[NativeBridge] Connected to Carbon bridge");
    }

    private void connectNamedPipe() throws IOException {
        pipeFile = new RandomAccessFile(endpoint, "rw");
        outputStream = new FileOutputStream(pipeFile.getFD());
        inputStream = new FileInputStream(pipeFile.getFD());
    }

    private void connectUnixSocket() throws IOException {
        var address = UnixDomainSocketAddress.of(Path.of(endpoint));
        unixChannel = SocketChannel.open(address);
        unixChannel.configureBlocking(true);

        var socket = unixChannel.socket();
        outputStream = socket.getOutputStream();
        inputStream = socket.getInputStream();
    }

    private void disconnect() {
        closeQuietly(outputStream);
        closeQuietly(inputStream);
        closeQuietly(unixChannel);
        closeQuietly(pipeFile);

        outputStream = null;
        inputStream = null;
        unixChannel = null;
        pipeFile = null;
    }

    private void readLoop() throws IOException {
        while (running.get()) {
            // Read length prefix (4 bytes, little-endian)
            readFully(lengthBuffer);

            lengthByteBuffer.clear();
            int length = lengthByteBuffer.getInt();

            if (length <= 0 || length > 1_048_576) {
                throw new IOException("Invalid message length: " + length);
            }

            // Read FlatBuffer message
            byte[] data = new byte[length];
            readFully(data);

            // Process message on a virtual thread for non-blocking handling
            Thread.ofVirtual()
                    .name("message-handler")
                    .start(() -> api.handleMessage(data));
        }
    }

    private void readFully(byte[] buffer) throws IOException {
        int totalRead = 0;
        while (totalRead < buffer.length) {
            int bytesRead = inputStream.read(buffer, totalRead, buffer.length - totalRead);
            if (bytesRead == -1) {
                throw new IOException("Connection closed");
            }
            totalRead += bytesRead;
        }
    }

    void registerHook(String hookName) {
        System.out.println("[NativeBridge] Registered hook: " + hookName);
    }

    CommandResult sendChat(String playerIdOrNull, String message) {
        var builder = RustModAPI.getBuilder();
        int playerIdOffset = playerIdOrNull != null ? builder.createString(playerIdOrNull) : 0;
        int messageOffset = builder.createString(message);
        int payloadOffset = SendChatCommand.createSendChatCommand(builder, playerIdOffset, messageOffset);
        return sendCommandInternal("send_chat", CommandPayload.SendChatCommand, payloadOffset, builder);
    }

    CommandResult kickPlayer(String playerId, String reasonOrNull) {
        var builder = RustModAPI.getBuilder();
        int playerIdOffset = builder.createString(playerId);
        int reasonOffset = reasonOrNull != null ? builder.createString(reasonOrNull) : 0;
        int payloadOffset = KickPlayerCommand.createKickPlayerCommand(builder, playerIdOffset, reasonOffset);
        return sendCommandInternal("kick_player", CommandPayload.KickPlayerCommand, payloadOffset, builder);
    }

    CommandResult teleportPlayer(String playerId, float x, float y, float z) {
        var builder = RustModAPI.getBuilder();
        int playerIdOffset = builder.createString(playerId);
        int locOffset = Vec3.createVec3(builder, x, y, z);
        TeleportPlayerCommand.startTeleportPlayerCommand(builder);
        TeleportPlayerCommand.addPlayerId(builder, playerIdOffset);
        TeleportPlayerCommand.addLocation(builder, locOffset);
        int payloadOffset = TeleportPlayerCommand.endTeleportPlayerCommand(builder);
        return sendCommandInternal("teleport_player", CommandPayload.TeleportPlayerCommand, payloadOffset, builder);
    }

    CommandResult giveItem(String playerId, String itemName, int amount) {
        var builder = RustModAPI.getBuilder();
        int playerIdOffset = builder.createString(playerId);
        int itemOffset = builder.createString(itemName);
        int payloadOffset = GiveItemCommand.createGiveItemCommand(builder, playerIdOffset, itemOffset, amount);
        return sendCommandInternal("give_item", CommandPayload.GiveItemCommand, payloadOffset, builder);
    }

    void onCommandResponse(int commandId, boolean success, String errorMessage) {
        var future = pendingCommands.remove(commandId);
        if (future != null) {
            future.complete(new CommandResult(success, errorMessage));
        }
    }

    private CommandResult sendCommandInternal(String commandName, byte payloadType, int payloadOffset,
            FlatBufferBuilder builder) {
        var commandId = commandIdCounter.incrementAndGet();
        var future = new CompletableFuture<CommandResult>();
        pendingCommands.put(commandId, future);

        try {
            int nameOffset = builder.createString(commandName);

            GameCommand.startGameCommand(builder);
            GameCommand.addCommandId(builder, commandId);
            GameCommand.addCommandName(builder, nameOffset);
            GameCommand.addPayloadType(builder, payloadType);
            GameCommand.addPayload(builder, payloadOffset);
            int cmdOffset = GameCommand.endGameCommand(builder);

            Message.startMessage(builder);
            Message.addMsgType(builder, MessageType.Command);
            Message.addPayloadType(builder, MessagePayload.GameCommand);
            Message.addPayload(builder, cmdOffset);
            int msgOffset = Message.endMessage(builder);
            builder.finish(msgOffset);

            sendBytes(builder.sizedByteArray());

            // Safe to block: runs on a virtual thread in typical usage
            return future.get(2, TimeUnit.SECONDS);
        } catch (Exception e) {
            pendingCommands.remove(commandId);
            return CommandResult.error(e.getMessage());
        }
    }

    void sendHookResponse(long hookId, ByteBuffer responsePayload) {
        // Run on virtual thread to avoid blocking caller
        Thread.ofVirtual()
                .name("hook-response-" + hookId)
                .start(() -> sendHookResponseInternal(hookId, responsePayload));
    }

    private void sendHookResponseInternal(long hookId, ByteBuffer responsePayload) {
        try {
            var builder = RustModAPI.getBuilder();

            int responseOffset = 0;
            if (responsePayload != null) {
                // Copy the response payload into our builder
                byte[] responseBytes = new byte[responsePayload.remaining()];
                responsePayload.get(responseBytes);
                responseOffset = builder.createByteVector(responseBytes);
            }

            // Build HookResponse
            int hookResponseOffset = HookResponse.createHookResponse(
                    builder, (int) hookId, (byte) 0, responseOffset);

            // Build Message wrapper
            Message.startMessage(builder);
            Message.addMsgType(builder, MessageType.HookResponse);
            Message.addPayloadType(builder, MessagePayload.HookResponse);
            Message.addPayload(builder, hookResponseOffset);
            int msgOffset = Message.endMessage(builder);

            builder.finish(msgOffset);

            sendBytes(builder.sizedByteArray());
        } catch (Exception e) {
            System.err.println("[NativeBridge] Failed to send hook response: " + e.getMessage());
        }
    }

    private void sendBytes(byte[] data) throws IOException {
        writeLock.lock();
        try {
            if (outputStream == null) {
                throw new IOException("Not connected");
            }

            // Write length prefix (4 bytes, little-endian)
            lengthByteBuffer.clear();
            lengthByteBuffer.putInt(data.length);
            outputStream.write(lengthBuffer);

            // Write data
            outputStream.write(data);
            outputStream.flush();
        } finally {
            writeLock.unlock();
        }
    }

    void shutdown() {
        System.out.println("[NativeBridge] Shutting down...");
        running.set(false);
        disconnect();

        if (connectionThread != null) {
            connectionThread.interrupt();
        }
    }

    // --- Utility methods ---

    private static void sleep(long millis) {
        try {
            Thread.sleep(millis);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }
    }

    private static void closeQuietly(AutoCloseable closeable) {
        if (closeable != null) {
            try {
                closeable.close();
            } catch (Exception ignored) {
                // Ignore close errors
            }
        }
    }
}
