package com.rustjavamods.game;

import RustJavaMods.Protocol.*;
import com.google.flatbuffers.FlatBufferBuilder;
import com.rustjavamods.HookContext;
import com.rustjavamods.RustModAPI;

import java.nio.ByteBuffer;

/**
 * Rust game-specific hooks (FlatBuffers version).
 * Zero-copy hook handling for maximum performance.
 * 
 * Java 21 features:
 * - Pattern matching instanceof
 * - var for local type inference
 * - Switch expressions
 */
public final class RustGameHooks {

    private RustGameHooks() {
    } // Utility class

    // ========== Player Hooks ==========

    /**
     * Hook for when a player attempts to connect to the server.
     * Return null to allow, or use denyConnection() to reject.
     */
    public static void onPlayerConnecting(PlayerConnectingHookHandler hook) {
        RustModAPI.getInstance().registerHook("player_connecting", context -> {
            var gameHook = context.getRawHook();
            if (gameHook.payloadType() != HookPayload.PlayerConnectingHook)
                return null;

            if (gameHook.payload(new PlayerConnectingHook()) instanceof PlayerConnectingHook payload) {
                return hook.handle(
                        payload.playerId(),
                        payload.playerName(),
                        payload.steamId(),
                        payload.ipAddress());
            }
            return null;
        });
    }

    /**
     * Hook for when a player takes damage.
     * Return null for default, modifyDamage() to change, or cancelDamage() to
     * prevent.
     */
    public static void onPlayerTakingDamage(PlayerDamageHookHandler hook) {
        RustModAPI.getInstance().registerHook("player_taking_damage", context -> {
            var gameHook = context.getRawHook();
            if (gameHook.payloadType() != HookPayload.PlayerTakingDamageHook)
                return null;

            if (gameHook.payload(new PlayerTakingDamageHook()) instanceof PlayerTakingDamageHook payload) {
                return hook.handle(
                        payload.playerId(),
                        payload.damage(),
                        payload.damageType(),
                        payload.attackerId());
            }
            return null;
        });
    }

    /**
     * Hook for when a player attempts to chat.
     * Return null for default, blockChat() to prevent, or modifyChat() to change.
     */
    public static void onPlayerChat(PlayerChatHookHandler hook) {
        RustModAPI.getInstance().registerHook("player_chat", context -> {
            var gameHook = context.getRawHook();
            if (gameHook.payloadType() != HookPayload.PlayerChatHook)
                return null;

            if (gameHook.payload(new PlayerChatHook()) instanceof PlayerChatHook payload) {
                return hook.handle(
                        payload.playerId(),
                        payload.message());
            }
            return null;
        });
    }

    /**
     * Hook for when a player attempts to build.
     * Return null to allow, or denyBuild() to prevent.
     */
    public static void onPlayerBuild(PlayerBuildHookHandler hook) {
        RustModAPI.getInstance().registerHook("player_build", context -> {
            var gameHook = context.getRawHook();
            if (gameHook.payloadType() != HookPayload.PlayerBuildHook)
                return null;

            if (gameHook.payload(new PlayerBuildHook()) instanceof PlayerBuildHook payload) {
                return hook.handle(
                        payload.playerId(),
                        payload.structureType(),
                        payload.location());
            }
            return null;
        });
    }

    /**
     * Hook for when a player loots a container.
     * Return null to allow, or denyLoot() to prevent.
     */
    public static void onPlayerLoot(PlayerLootHookHandler hook) {
        RustModAPI.getInstance().registerHook("player_loot", context -> {
            var gameHook = context.getRawHook();
            if (gameHook.payloadType() != HookPayload.PlayerLootHook)
                return null;

            if (gameHook.payload(new PlayerLootHook()) instanceof PlayerLootHook payload) {
                return hook.handle(
                        payload.playerId(),
                        payload.containerId());
            }
            return null;
        });
    }

    /**
     * Hook for when a player crafts an item.
     * Return null for default, or a response to modify/cancel.
     */
    public static void onPlayerCraft(PlayerCraftHookHandler hook) {
        RustModAPI.getInstance().registerHook("player_craft", context -> {
            var gameHook = context.getRawHook();
            if (gameHook.payloadType() != HookPayload.PlayerCraftHook)
                return null;

            if (gameHook.payload(new PlayerCraftHook()) instanceof PlayerCraftHook payload) {
                return hook.handle(
                        payload.playerId(),
                        payload.itemName(),
                        payload.amount());
            }
            return null;
        });
    }

    // ========== Response Builders ==========

    /** Deny player connection with reason */
    public static ByteBuffer denyConnection(String reason) {
        var builder = RustModAPI.getBuilder();
        var reasonOffset = builder.createString(reason);
        var responseOffset = PlayerConnectingResponse.createPlayerConnectingResponse(builder, false, reasonOffset);
        builder.finish(responseOffset);
        return builder.dataBuffer();
    }

    /** Modify damage amount */
    public static ByteBuffer modifyDamage(float newDamage) {
        var builder = RustModAPI.getBuilder();
        var responseOffset = PlayerDamageResponse.createPlayerDamageResponse(builder, newDamage, false);
        builder.finish(responseOffset);
        return builder.dataBuffer();
    }

    /** Cancel all damage */
    public static ByteBuffer cancelDamage() {
        var builder = RustModAPI.getBuilder();
        var responseOffset = PlayerDamageResponse.createPlayerDamageResponse(builder, 0, true);
        builder.finish(responseOffset);
        return builder.dataBuffer();
    }

    /** Block chat message */
    public static ByteBuffer blockChat() {
        var builder = RustModAPI.getBuilder();
        var responseOffset = PlayerChatResponse.createPlayerChatResponse(builder, 0, true);
        builder.finish(responseOffset);
        return builder.dataBuffer();
    }

    /** Modify chat message content */
    public static ByteBuffer modifyChat(String newMessage) {
        var builder = RustModAPI.getBuilder();
        var msgOffset = builder.createString(newMessage);
        var responseOffset = PlayerChatResponse.createPlayerChatResponse(builder, msgOffset, false);
        builder.finish(responseOffset);
        return builder.dataBuffer();
    }

    /** Deny building placement */
    public static ByteBuffer denyBuild() {
        var builder = RustModAPI.getBuilder();
        var responseOffset = PlayerBuildResponse.createPlayerBuildResponse(builder, false);
        builder.finish(responseOffset);
        return builder.dataBuffer();
    }

    /** Deny container looting */
    public static ByteBuffer denyLoot() {
        var builder = RustModAPI.getBuilder();
        var responseOffset = PlayerLootResponse.createPlayerLootResponse(builder, false);
        builder.finish(responseOffset);
        return builder.dataBuffer();
    }

    // ========== Hook Interfaces ==========

    @FunctionalInterface
    public interface PlayerConnectingHookHandler {
        ByteBuffer handle(String playerId, String playerName, String steamId, String ipAddress);
    }

    @FunctionalInterface
    public interface PlayerDamageHookHandler {
        ByteBuffer handle(String playerId, float damage, byte damageType, String attackerId);
    }

    @FunctionalInterface
    public interface PlayerChatHookHandler {
        ByteBuffer handle(String playerId, String message);
    }

    @FunctionalInterface
    public interface PlayerBuildHookHandler {
        ByteBuffer handle(String playerId, String structureType, Vec3 location);
    }

    @FunctionalInterface
    public interface PlayerLootHookHandler {
        ByteBuffer handle(String playerId, String containerId);
    }

    @FunctionalInterface
    public interface PlayerCraftHookHandler {
        ByteBuffer handle(String playerId, String itemName, int amount);
    }
}
