package com.rustjavamods.game;

import com.rustjavamods.GameEvent;
import com.rustjavamods.RustModAPI;
import com.google.gson.JsonObject;

/**
 * Rust game-specific events (Facepunch Rust game)
 */
public class RustGameEvents {

    /**
     * Player events
     */
    public static void onPlayerConnected(PlayerConnectHandler handler) {
        RustModAPI.getInstance().registerEventHandler("player_connected", event -> {
            JsonObject data = event.getData();
            handler.handle(
                data.get("player_id").getAsString(),
                data.get("player_name").getAsString(),
                data.get("steam_id").getAsString()
            );
        });
    }

    public static void onPlayerDisconnected(PlayerDisconnectHandler handler) {
        RustModAPI.getInstance().registerEventHandler("player_disconnected", event -> {
            JsonObject data = event.getData();
            handler.handle(
                data.get("player_id").getAsString(),
                data.get("reason").getAsString()
            );
        });
    }

    public static void onPlayerDamage(PlayerDamageHandler handler) {
        RustModAPI.getInstance().registerEventHandler("player_damage", event -> {
            JsonObject data = event.getData();
            handler.handle(
                data.get("player_id").getAsString(),
                data.get("damage").getAsFloat(),
                data.get("damage_type").getAsString(),
                data.get("attacker_id").getAsString()
            );
        });
    }

    public static void onPlayerDeath(PlayerDeathHandler handler) {
        RustModAPI.getInstance().registerEventHandler("player_death", event -> {
            JsonObject data = event.getData();
            handler.handle(
                data.get("player_id").getAsString(),
                data.get("killer_id").getAsString(),
                data.get("weapon").getAsString()
            );
        });
    }

    public static void onPlayerRespawn(PlayerRespawnHandler handler) {
        RustModAPI.getInstance().registerEventHandler("player_respawn", event -> {
            JsonObject data = event.getData();
            handler.handle(
                data.get("player_id").getAsString(),
                data.get("location").getAsJsonObject()
            );
        });
    }

    /**
     * Entity events
     */
    public static void onEntitySpawned(EntitySpawnHandler handler) {
        RustModAPI.getInstance().registerEventHandler("entity_spawned", event -> {
            JsonObject data = event.getData();
            handler.handle(
                data.get("entity_id").getAsString(),
                data.get("entity_type").getAsString(),
                data.get("location").getAsJsonObject()
            );
        });
    }

    public static void onEntityKilled(EntityKillHandler handler) {
        RustModAPI.getInstance().registerEventHandler("entity_killed", event -> {
            JsonObject data = event.getData();
            handler.handle(
                data.get("entity_id").getAsString(),
                data.get("killer_id").getAsString()
            );
        });
    }

    /**
     * Building events
     */
    public static void onStructurePlaced(StructurePlaceHandler handler) {
        RustModAPI.getInstance().registerEventHandler("structure_placed", event -> {
            JsonObject data = event.getData();
            handler.handle(
                data.get("player_id").getAsString(),
                data.get("structure_type").getAsString(),
                data.get("location").getAsJsonObject()
            );
        });
    }

    public static void onStructureDestroyed(StructureDestroyHandler handler) {
        RustModAPI.getInstance().registerEventHandler("structure_destroyed", event -> {
            JsonObject data = event.getData();
            handler.handle(
                data.get("structure_id").getAsString(),
                data.get("destroyer_id").getAsString()
            );
        });
    }

    /**
     * Chat events
     */
    public static void onChatMessage(ChatMessageHandler handler) {
        RustModAPI.getInstance().registerEventHandler("chat_message", event -> {
            JsonObject data = event.getData();
            handler.handle(
                data.get("player_id").getAsString(),
                data.get("player_name").getAsString(),
                data.get("message").getAsString()
            );
        });
    }

    // Handler interfaces
    @FunctionalInterface
    public interface PlayerConnectHandler {
        void handle(String playerId, String playerName, String steamId);
    }

    @FunctionalInterface
    public interface PlayerDisconnectHandler {
        void handle(String playerId, String reason);
    }

    @FunctionalInterface
    public interface PlayerDamageHandler {
        void handle(String playerId, float damage, String damageType, String attackerId);
    }

    @FunctionalInterface
    public interface PlayerDeathHandler {
        void handle(String playerId, String killerId, String weapon);
    }

    @FunctionalInterface
    public interface PlayerRespawnHandler {
        void handle(String playerId, JsonObject location);
    }

    @FunctionalInterface
    public interface EntitySpawnHandler {
        void handle(String entityId, String entityType, JsonObject location);
    }

    @FunctionalInterface
    public interface EntityKillHandler {
        void handle(String entityId, String killerId);
    }

    @FunctionalInterface
    public interface StructurePlaceHandler {
        void handle(String playerId, String structureType, JsonObject location);
    }

    @FunctionalInterface
    public interface StructureDestroyHandler {
        void handle(String structureId, String destroyerId);
    }

    @FunctionalInterface
    public interface ChatMessageHandler {
        void handle(String playerId, String playerName, String message);
    }
}
