package com.rustjavamods.game;

import com.rustjavamods.GameHook;
import com.rustjavamods.HookContext;
import com.rustjavamods.RustModAPI;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;

/**
 * Rust game-specific hooks (Facepunch Rust game)
 * These hooks allow intercepting and modifying game behavior
 */
public class RustGameHooks {

    /**
     * Hook for when a player attempts to connect to the server
     * Return null to allow, or JsonObject with "deny" and "reason" to reject
     */
    public static void onPlayerConnecting(PlayerConnectingHook hook) {
        RustModAPI.getInstance().registerHook("player_connecting", context -> {
            JsonObject params = context.getParameters();
            String playerId = params.get("player_id").getAsString();
            String playerName = params.get("player_name").getAsString();
            String steamId = params.get("steam_id").getAsString();
            String ipAddress = params.get("ip_address").getAsString();
            
            return hook.handle(playerId, playerName, steamId, ipAddress);
        });
    }

    /**
     * Hook for when a player takes damage
     * Can modify damage amount or cancel damage
     */
    public static void onPlayerTakingDamage(PlayerDamageHook hook) {
        RustModAPI.getInstance().registerHook("player_taking_damage", context -> {
            JsonObject params = context.getParameters();
            String playerId = params.get("player_id").getAsString();
            float damage = params.get("damage").getAsFloat();
            String damageType = params.get("damage_type").getAsString();
            
            return hook.handle(playerId, damage, damageType);
        });
    }

    /**
     * Hook for when a player attempts to chat
     * Can modify message or block it
     */
    public static void onPlayerChat(PlayerChatHook hook) {
        RustModAPI.getInstance().registerHook("player_chat", context -> {
            JsonObject params = context.getParameters();
            String playerId = params.get("player_id").getAsString();
            String message = params.get("message").getAsString();
            
            return hook.handle(playerId, message);
        });
    }

    /**
     * Hook for when a player attempts to build
     * Can allow or deny construction
     */
    public static void onPlayerBuild(PlayerBuildHook hook) {
        RustModAPI.getInstance().registerHook("player_build", context -> {
            JsonObject params = context.getParameters();
            String playerId = params.get("player_id").getAsString();
            String structureType = params.get("structure_type").getAsString();
            JsonObject location = params.get("location").getAsJsonObject();
            
            return hook.handle(playerId, structureType, location);
        });
    }

    /**
     * Hook for when a player loots a container
     */
    public static void onPlayerLoot(PlayerLootHook hook) {
        RustModAPI.getInstance().registerHook("player_loot", context -> {
            JsonObject params = context.getParameters();
            String playerId = params.get("player_id").getAsString();
            String containerId = params.get("container_id").getAsString();
            
            return hook.handle(playerId, containerId);
        });
    }

    /**
     * Hook for when a player crafts an item
     */
    public static void onPlayerCraft(PlayerCraftHook hook) {
        RustModAPI.getInstance().registerHook("player_craft", context -> {
            JsonObject params = context.getParameters();
            String playerId = params.get("player_id").getAsString();
            String itemName = params.get("item_name").getAsString();
            int amount = params.get("amount").getAsInt();
            
            return hook.handle(playerId, itemName, amount);
        });
    }

    // Hook interfaces
    @FunctionalInterface
    public interface PlayerConnectingHook {
        /**
         * @return null to allow connection, or JsonObject with deny reason
         */
        JsonElement handle(String playerId, String playerName, String steamId, String ipAddress);
    }

    @FunctionalInterface
    public interface PlayerDamageHook {
        /**
         * @return JsonObject with modified "damage" value, or "cancel": true
         */
        JsonElement handle(String playerId, float damage, String damageType);
    }

    @FunctionalInterface
    public interface PlayerChatHook {
        /**
         * @return JsonObject with modified "message" or "block": true
         */
        JsonElement handle(String playerId, String message);
    }

    @FunctionalInterface
    public interface PlayerBuildHook {
        /**
         * @return JsonObject with "allow": true/false
         */
        JsonElement handle(String playerId, String structureType, JsonObject location);
    }

    @FunctionalInterface
    public interface PlayerLootHook {
        /**
         * @return JsonObject with "allow": true/false
         */
        JsonElement handle(String playerId, String containerId);
    }

    @FunctionalInterface
    public interface PlayerCraftHook {
        /**
         * @return JsonObject with modified "amount" or "cancel": true
         */
        JsonElement handle(String playerId, String itemName, int amount);
    }
}
