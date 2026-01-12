package com.rustjavamods.game;

import RustJavaMods.Protocol.*;
import com.rustjavamods.RustModAPI;

/**
 * Rust game-specific events (FlatBuffers version).
 * Zero-copy event handling for maximum performance.
 * 
 * Java 21 features:
 * - Pattern matching instanceof
 * - var for local type inference
 * - Cleaner null checks with early returns
 */
public final class RustGameEvents {

    private RustGameEvents() {
    } // Utility class

    // ========== Player Events ==========

    public static void onPlayerConnected(PlayerConnectHandler handler) {
        RustModAPI.getInstance().registerEventHandler("player_connected", event -> {
            if (event.payloadType() != EventPayload.PlayerConnectedEvent)
                return;

            if (event.payload(new PlayerConnectedEvent()) instanceof PlayerConnectedEvent payload) {
                if (payload.player() instanceof PlayerInfo player) {
                    handler.handle(
                            player.playerId(),
                            player.playerName(),
                            player.steamId(),
                            player.position());
                }
            }
        });
    }

    public static void onPlayerDisconnected(PlayerDisconnectHandler handler) {
        RustModAPI.getInstance().registerEventHandler("player_disconnected", event -> {
            if (event.payloadType() != EventPayload.PlayerDisconnectedEvent)
                return;

            if (event.payload(new PlayerDisconnectedEvent()) instanceof PlayerDisconnectedEvent payload) {
                handler.handle(
                        payload.playerId(),
                        payload.playerName(),
                        payload.reason());
            }
        });
    }

    public static void onPlayerDamage(PlayerDamageHandler handler) {
        RustModAPI.getInstance().registerEventHandler("player_damage", event -> {
            if (event.payloadType() != EventPayload.PlayerDamageEvent)
                return;

            if (event.payload(new PlayerDamageEvent()) instanceof PlayerDamageEvent payload) {
                handler.handle(
                        payload.playerId(),
                        payload.damage(),
                        payload.damageType(),
                        payload.attackerId());
            }
        });
    }

    public static void onPlayerDeath(PlayerDeathHandler handler) {
        RustModAPI.getInstance().registerEventHandler("player_death", event -> {
            if (event.payloadType() != EventPayload.PlayerDeathEvent)
                return;

            if (event.payload(new PlayerDeathEvent()) instanceof PlayerDeathEvent payload) {
                handler.handle(
                        payload.playerId(),
                        payload.killerId(),
                        payload.weapon());
            }
        });
    }

    public static void onPlayerRespawn(PlayerRespawnHandler handler) {
        RustModAPI.getInstance().registerEventHandler("player_respawn", event -> {
            if (event.payloadType() != EventPayload.PlayerRespawnEvent)
                return;

            if (event.payload(new PlayerRespawnEvent()) instanceof PlayerRespawnEvent payload) {
                handler.handle(
                        payload.playerId(),
                        payload.location());
            }
        });
    }

    // ========== Chat Events ==========

    public static void onChatMessage(ChatMessageHandler handler) {
        RustModAPI.getInstance().registerEventHandler("chat_message", event -> {
            if (event.payloadType() != EventPayload.ChatMessageEvent)
                return;

            if (event.payload(new ChatMessageEvent()) instanceof ChatMessageEvent payload) {
                handler.handle(
                        payload.playerId(),
                        payload.playerName(),
                        payload.message());
            }
        });
    }

    // ========== Structure Events ==========

    public static void onStructurePlaced(StructurePlaceHandler handler) {
        RustModAPI.getInstance().registerEventHandler("structure_placed", event -> {
            if (event.payloadType() != EventPayload.StructurePlacedEvent)
                return;

            if (event.payload(new StructurePlacedEvent()) instanceof StructurePlacedEvent payload) {
                handler.handle(
                        payload.playerId(),
                        payload.structureType(),
                        payload.location());
            }
        });
    }

    public static void onStructureDestroyed(StructureDestroyHandler handler) {
        RustModAPI.getInstance().registerEventHandler("structure_destroyed", event -> {
            if (event.payloadType() != EventPayload.StructureDestroyedEvent)
                return;

            if (event.payload(new StructureDestroyedEvent()) instanceof StructureDestroyedEvent payload) {
                handler.handle(
                        payload.structureId(),
                        payload.destroyerId());
            }
        });
    }

    // ========== Entity Events ==========

    public static void onEntitySpawned(EntitySpawnHandler handler) {
        RustModAPI.getInstance().registerEventHandler("entity_spawned", event -> {
            if (event.payloadType() != EventPayload.EntitySpawnedEvent)
                return;

            if (event.payload(new EntitySpawnedEvent()) instanceof EntitySpawnedEvent payload) {
                handler.handle(
                        payload.entityId(),
                        payload.entityType(),
                        payload.location());
            }
        });
    }

    public static void onEntityKilled(EntityKillHandler handler) {
        RustModAPI.getInstance().registerEventHandler("entity_killed", event -> {
            if (event.payloadType() != EventPayload.EntityKilledEvent)
                return;

            if (event.payload(new EntityKilledEvent()) instanceof EntityKilledEvent payload) {
                handler.handle(
                        payload.entityId(),
                        payload.entityType(),
                        payload.killerId());
            }
        });
    }

    // ========== Crafting Events ==========

    public static void onItemCrafted(ItemCraftedHandler handler) {
        RustModAPI.getInstance().registerEventHandler("item_crafted", event -> {
            if (event.payloadType() != EventPayload.ItemCraftedEvent)
                return;

            if (event.payload(new ItemCraftedEvent()) instanceof ItemCraftedEvent payload) {
                handler.handle(
                        payload.playerId(),
                        payload.itemName(),
                        payload.amount());
            }
        });
    }

    // ========== Handler Interfaces ==========

    @FunctionalInterface
    public interface PlayerConnectHandler {
        void handle(String playerId, String playerName, String steamId, Vec3 position);
    }

    @FunctionalInterface
    public interface PlayerDisconnectHandler {
        void handle(String playerId, String playerName, String reason);
    }

    @FunctionalInterface
    public interface PlayerDamageHandler {
        void handle(String playerId, float damage, byte damageType, String attackerId);
    }

    @FunctionalInterface
    public interface PlayerDeathHandler {
        void handle(String playerId, String killerId, String weapon);
    }

    @FunctionalInterface
    public interface PlayerRespawnHandler {
        void handle(String playerId, Vec3 location);
    }

    @FunctionalInterface
    public interface ChatMessageHandler {
        void handle(String playerId, String playerName, String message);
    }

    @FunctionalInterface
    public interface StructurePlaceHandler {
        void handle(String playerId, String structureType, Vec3 location);
    }

    @FunctionalInterface
    public interface StructureDestroyHandler {
        void handle(String structureId, String destroyerId);
    }

    @FunctionalInterface
    public interface EntitySpawnHandler {
        void handle(String entityId, String entityType, Vec3 location);
    }

    @FunctionalInterface
    public interface EntityKillHandler {
        void handle(String entityId, String entityType, String killerId);
    }

    @FunctionalInterface
    public interface ItemCraftedHandler {
        void handle(String playerId, String itemName, int amount);
    }
}
