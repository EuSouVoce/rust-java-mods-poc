package com.rustjavamods.examples;

import RustJavaMods.Protocol.*;
import com.rustjavamods.RustModAPI;
import com.rustjavamods.game.RustGameEvents;
import com.rustjavamods.game.RustGameHooks;

/**
 * Example Rust game mod - Welcome plugin (FlatBuffers version).
 * Demonstrates high-performance hook and event handling.
 * 
 * Java 21 features used:
 * - Virtual Threads (via RustModAPI)
 * - Pattern matching switch
 * - var for local type inference
 */
public final class WelcomeModExample {

    public static void main(String[] args) {
        System.out.println("=== Rust Game Modding Example (Java 21 + FlatBuffers) ===\n");

        // Initialize the modding API
        var api = RustModAPI.getInstance();
        api.initialize();

        // Register event handlers
        registerEventHandlers();

        // Register hooks
        registerHooks();

        System.out.println("""

                ✓ Mod loaded successfully!
                ✓ Using FlatBuffers protocol (zero-copy)
                ✓ Using Virtual Threads (Java 21)
                ✓ Listening for Rust game events...
                """);

        // Keep the application running using virtual thread
        Thread.ofVirtual().start(() -> {
            try {
                Thread.sleep(Long.MAX_VALUE);
            } catch (InterruptedException e) {
                api.shutdown();
            }
        });

        // Main thread waits
        try {
            Thread.currentThread().join();
        } catch (InterruptedException e) {
            api.shutdown();
        }
    }

    private static void registerEventHandlers() {
        System.out.println("--- Registering Event Handlers ---");

        // Welcome message when player connects
        RustGameEvents.onPlayerConnected((playerId, playerName, steamId, position) -> {
            System.out.printf("[EVENT] Player connected: %s (ID: %s, Steam: %s)%n",
                    playerName, playerId, steamId);
            if (position != null) {
                System.out.printf("  → Position: (%.1f, %.1f, %.1f)%n",
                        position.x(), position.y(), position.z());
            }
            System.out.println("  → Sending welcome message to " + playerName);

            var result = RustModAPI.getInstance().sendChat(playerId, "Welcome, " + playerName + "! (Java mod)");
            if (!result.success()) {
                System.err.println("  → Failed to send welcome message: " + result.errorMessage());
            }
        });

        // Farewell message when player disconnects
        RustGameEvents.onPlayerDisconnected((playerId, playerName, reason) -> System.out
                .printf("[EVENT] Player disconnected: %s (%s)%n", playerName, reason));

        // Log player damage events with damage type
        RustGameEvents.onPlayerDamage((playerId, damage, damageType, attackerId) -> {
            var typeName = getDamageTypeName(damageType);
            System.out.printf("[EVENT] Player %s took %.1f %s damage from %s%n",
                    playerId, damage, typeName, attackerId);
        });

        // Log player deaths
        RustGameEvents.onPlayerDeath(
                (playerId, killerId, weapon) -> System.out.printf("[EVENT] Player %s was killed by %s using %s%n",
                        playerId, killerId, weapon));

        // Log chat messages with command detection
        RustGameEvents.onChatMessage((playerId, playerName, message) -> {
            System.out.printf("[CHAT] %s: %s%n", playerName, message);

            if (message.startsWith("/")) {
                var command = message.split(" ")[0];
                System.out.println("  → Command detected: " + command);
            }
        });

        // Log structure placement
        RustGameEvents.onStructurePlaced((playerId, structureType, location) -> {
            var locationStr = location != null
                    ? "(%.1f, %.1f, %.1f)".formatted(location.x(), location.y(), location.z())
                    : "unknown";
            System.out.printf("[EVENT] Player %s placed %s at %s%n", playerId, structureType, locationStr);
        });

        System.out.println("✓ Event handlers registered\n");
    }

    private static void registerHooks() {
        System.out.println("--- Registering Game Hooks ---");

        // Hook player connections
        RustGameHooks.onPlayerConnecting((playerId, playerName, steamId, ipAddress) -> {
            System.out.printf("[HOOK] Player connecting: %s from %s%n", playerName, ipAddress);

            // Example: deny players with "Banned" in name
            if (playerName.contains("Banned")) {
                System.out.println("  → Denying connection for " + playerName);
                return RustGameHooks.denyConnection("You are banned from this server");
            }

            System.out.println("  → Allowing connection for " + playerName);
            return null; // Allow
        });

        // Hook player damage with Java 21 switch expression
        RustGameHooks.onPlayerTakingDamage((playerId, damage, damageType, attackerId) -> {
            var typeName = getDamageTypeName(damageType);
            System.out.printf("[HOOK] Player %s taking %.1f %s damage%n", playerId, damage, typeName);

            // Handle different damage scenarios
            return switch (damageType) {
                case DamageType.Fall -> {
                    System.out.println("  → Reducing fall damage by 50%");
                    yield RustGameHooks.modifyDamage(damage * 0.5f);
                }
                default -> {
                    // God mode for admins
                    if (playerId.contains("admin")) {
                        System.out.println("  → Canceling damage (admin god mode)");
                        yield RustGameHooks.cancelDamage();
                    }
                    yield null; // Default behavior
                }
            };
        });

        // Hook chat messages with profanity filter
        RustGameHooks.onPlayerChat((playerId, message) -> {
            System.out.printf("[HOOK] Player %s chatting: %s%n", playerId, message);

            // Block spam
            if (message.contains("spam")) {
                System.out.println("  → Blocking spam message");
                return RustGameHooks.blockChat();
            }

            // Filter profanity using case-insensitive check
            var lowerMessage = message.toLowerCase();
            if (lowerMessage.contains("badword")) {
                var filtered = message.replaceAll("(?i)badword", "***");
                System.out.println("  → Filtered profanity");
                return RustGameHooks.modifyChat(filtered);
            }

            return null;
        });

        // Hook building - prevent in spawn area
        RustGameHooks.onPlayerBuild((playerId, structureType, location) -> {
            System.out.printf("[HOOK] Player %s attempting to build %s%n", playerId, structureType);

            // Prevent building in spawn area using pattern matching
            if (location instanceof Vec3 loc && Math.abs(loc.x()) < 100 && Math.abs(loc.z()) < 100) {
                System.out.println("  → Denying build (spawn area)");
                return RustGameHooks.denyBuild();
            }

            return null; // Allow
        });

        System.out.println("✓ Game hooks registered\n");
    }

    /**
     * Convert damage type byte to human-readable name using Java 21 switch
     * expression.
     */
    private static String getDamageTypeName(byte damageType) {
        return switch (damageType) {
            case DamageType.Bullet -> "Bullet";
            case DamageType.Slash -> "Slash";
            case DamageType.Blunt -> "Blunt";
            case DamageType.Fall -> "Fall";
            case DamageType.Radiation -> "Radiation";
            case DamageType.Bite -> "Bite";
            case DamageType.Stab -> "Stab";
            case DamageType.Explosion -> "Explosion";
            case DamageType.Heat -> "Heat";
            case DamageType.Cold -> "Cold";
            case DamageType.Bleeding -> "Bleeding";
            case DamageType.Poison -> "Poison";
            case DamageType.Hunger -> "Hunger";
            case DamageType.Thirst -> "Thirst";
            case DamageType.Drowned -> "Drowned";
            case DamageType.ElectricShock -> "Electric Shock";
            default -> "Generic";
        };
    }
}
