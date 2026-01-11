package com.rustjavamods.examples;

import com.rustjavamods.RustModAPI;
import com.rustjavamods.game.RustGameEvents;
import com.rustjavamods.game.RustGameHooks;
import com.google.gson.JsonObject;

/**
 * Example Rust game mod - Welcome plugin
 * Demonstrates hook registration and event handling for Facepunch Rust game
 */
public class WelcomeModExample {

    public static void main(String[] args) {
        System.out.println("=== Rust Game Modding Example (Facepunch Rust) ===\n");

        // Initialize the modding API
        RustModAPI api = RustModAPI.getInstance();
        api.initialize();

        // Register event handlers for Rust game events
        registerEventHandlers();

        // Register hooks to modify Rust game behavior
        registerHooks();

        System.out.println("\n✓ Mod loaded successfully!");
        System.out.println("✓ Listening for Rust game events...\n");

        // Keep the application running
        try {
            Thread.sleep(Long.MAX_VALUE);
        } catch (InterruptedException e) {
            api.shutdown();
        }
    }

    private static void registerEventHandlers() {
        System.out.println("--- Registering Event Handlers ---");

        // Welcome message when player connects
        RustGameEvents.onPlayerConnected((playerId, playerName, steamId) -> {
            System.out.println(String.format(
                "[EVENT] Player connected: %s (ID: %s, Steam: %s)",
                playerName, playerId, steamId
            ));
            
            // Send welcome message to player (would use game API)
            System.out.println("  → Sending welcome message to " + playerName);
        });

        // Farewell message when player disconnects
        RustGameEvents.onPlayerDisconnected((playerId, reason) -> {
            System.out.println(String.format(
                "[EVENT] Player disconnected: %s (Reason: %s)",
                playerId, reason
            ));
        });

        // Log player damage events
        RustGameEvents.onPlayerDamage((playerId, damage, damageType, attackerId) -> {
            System.out.println(String.format(
                "[EVENT] Player %s took %.1f %s damage from %s",
                playerId, damage, damageType, attackerId
            ));
        });

        // Log player deaths
        RustGameEvents.onPlayerDeath((playerId, killerId, weapon) -> {
            System.out.println(String.format(
                "[EVENT] Player %s was killed by %s using %s",
                playerId, killerId, weapon
            ));
        });

        // Log chat messages
        RustGameEvents.onChatMessage((playerId, playerName, message) -> {
            System.out.println(String.format(
                "[CHAT] %s: %s",
                playerName, message
            ));
            
            // Check for commands
            if (message.startsWith("/help")) {
                System.out.println("  → Sending help information to " + playerName);
            }
        });

        // Log structure placement
        RustGameEvents.onStructurePlaced((playerId, structureType, location) -> {
            System.out.println(String.format(
                "[EVENT] Player %s placed %s at %s",
                playerId, structureType, location
            ));
        });

        System.out.println("✓ Event handlers registered\n");
    }

    private static void registerHooks() {
        System.out.println("--- Registering Game Hooks ---");

        // Hook player connections to allow/deny
        RustGameHooks.onPlayerConnecting((playerId, playerName, steamId, ipAddress) -> {
            System.out.println(String.format(
                "[HOOK] Player connecting: %s from %s",
                playerName, ipAddress
            ));

            // Check if player is banned (example)
            if (playerName.contains("Banned")) {
                JsonObject deny = new JsonObject();
                deny.addProperty("deny", true);
                deny.addProperty("reason", "You are banned from this server");
                System.out.println("  → Denying connection for " + playerName);
                return deny;
            }

            // Allow connection
            System.out.println("  → Allowing connection for " + playerName);
            return null;
        });

        // Hook player damage to modify or cancel
        RustGameHooks.onPlayerTakingDamage((playerId, damage, damageType) -> {
            System.out.println(String.format(
                "[HOOK] Player %s taking %.1f %s damage",
                playerId, damage, damageType
            ));

            // Reduce fall damage by 50% (example)
            if (damageType.equals("Fall")) {
                JsonObject modified = new JsonObject();
                modified.addProperty("damage", damage * 0.5f);
                System.out.println("  → Reducing fall damage by 50%");
                return modified;
            }

            // God mode for admins (example)
            if (playerId.contains("admin")) {
                JsonObject cancel = new JsonObject();
                cancel.addProperty("cancel", true);
                System.out.println("  → Canceling damage (admin god mode)");
                return cancel;
            }

            return null;
        });

        // Hook chat messages to filter/modify
        RustGameHooks.onPlayerChat((playerId, message) -> {
            System.out.println(String.format(
                "[HOOK] Player %s chatting: %s",
                playerId, message
            ));

            // Block spam (example)
            if (message.contains("spam")) {
                JsonObject block = new JsonObject();
                block.addProperty("block", true);
                System.out.println("  → Blocking spam message");
                return block;
            }

            // Auto-replace profanity (example)
            if (message.toLowerCase().contains("badword")) {
                JsonObject modified = new JsonObject();
                modified.addProperty("message", message.replace("badword", "***"));
                System.out.println("  → Filtered profanity");
                return modified;
            }

            return null;
        });

        // Hook building to control where players can build
        RustGameHooks.onPlayerBuild((playerId, structureType, location) -> {
            System.out.println(String.format(
                "[HOOK] Player %s attempting to build %s",
                playerId, structureType
            ));

            // Prevent building in certain areas (example)
            double x = location.get("x").getAsDouble();
            double z = location.get("z").getAsDouble();
            
            if (Math.abs(x) < 100 && Math.abs(z) < 100) {
                JsonObject deny = new JsonObject();
                deny.addProperty("allow", false);
                System.out.println("  → Denying build (spawn area)");
                return deny;
            }

            JsonObject allow = new JsonObject();
            allow.addProperty("allow", true);
            return allow;
        });

        System.out.println("✓ Game hooks registered\n");
    }
}
