using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Carbon.Core;
using Google.FlatBuffers;
using RustJavaMods.Protocol;
using UnityEngine;

namespace Carbon.Plugins
{
    [Info("RustJavaBridge", "RustJavaMods", "0.2.0")]
    [Description("High-performance bridge between Facepunch Rust game and Java modding API using Carbon and FlatBuffers")]
    public class RustJavaBridge : CarbonPlugin
    {
        private IpcServer _ipcServer;
        private volatile bool _isInitialized;

        // Reusable FlatBuffer builder to minimize allocations
        private readonly ThreadLocal<FlatBufferBuilder> _builderPool = 
            new ThreadLocal<FlatBufferBuilder>(() => new FlatBufferBuilder(1024));

        #region Carbon Hooks

        private void OnServerInitialized()
        {
            Puts("Initializing Rust-Java Bridge (Carbon + FlatBuffers)...");
            
            try
            {
                _ipcServer = new IpcServer(this);
                _ipcServer.Start();
                _isInitialized = true;
                
                Puts("✓ Rust-Java Bridge initialized successfully");
                Puts($"  IPC Endpoint: {_ipcServer.GetEndpoint()}");
                Puts("  Protocol: FlatBuffers (zero-copy)");
                Puts("  Using Carbon Framework");
            }
            catch (Exception ex)
            {
                PrintError($"Failed to initialize: {ex.Message}");
            }
        }

        private void Unload()
        {
            Puts("Shutting down Rust-Java Bridge...");
            _ipcServer?.Stop();
            _isInitialized = false;
        }

        #endregion

        #region FlatBuffer Helpers

        private FlatBufferBuilder GetBuilder()
        {
            var builder = _builderPool.Value;
            builder.Clear();
            return builder;
        }

        private static DamageType ConvertDamageType(Rust.DamageType rustType)
        {
            return rustType switch
            {
                Rust.DamageType.Bullet => DamageType.Bullet,
                Rust.DamageType.Slash => DamageType.Slash,
                Rust.DamageType.Blunt => DamageType.Blunt,
                Rust.DamageType.Fall => DamageType.Fall,
                Rust.DamageType.Radiation => DamageType.Radiation,
                Rust.DamageType.Bite => DamageType.Bite,
                Rust.DamageType.Stab => DamageType.Stab,
                Rust.DamageType.Explosion => DamageType.Explosion,
                Rust.DamageType.Heat => DamageType.Heat,
                Rust.DamageType.Cold => DamageType.Cold,
                Rust.DamageType.Bleeding => DamageType.Bleeding,
                Rust.DamageType.Poison => DamageType.Poison,
                Rust.DamageType.Hunger => DamageType.Hunger,
                Rust.DamageType.Thirst => DamageType.Thirst,
                Rust.DamageType.Drowned => DamageType.Drowned,
                Rust.DamageType.ElectricShock => DamageType.ElectricShock,
                _ => DamageType.Generic
            };
        }

        #endregion

        #region Player Hooks

        // uMod/Carbon-style pre-login hook. Returning a string denies connection with that reason.
        private object CanClientLogin(string username, string userId, string ipAddress)
        {
            if (!_isInitialized) return null;

            var builder = GetBuilder();

            var playerIdOffset = builder.CreateString(userId ?? "unknown");
            var playerNameOffset = builder.CreateString(username ?? "unknown");
            var steamIdOffset = builder.CreateString(userId ?? "unknown");
            var ipOffset = builder.CreateString(ipAddress ?? "unknown");

            PlayerConnectingHook.StartPlayerConnectingHook(builder);
            PlayerConnectingHook.AddPlayerId(builder, playerIdOffset);
            PlayerConnectingHook.AddPlayerName(builder, playerNameOffset);
            PlayerConnectingHook.AddSteamId(builder, steamIdOffset);
            PlayerConnectingHook.AddIpAddress(builder, ipOffset);
            var hookPayloadOffset = PlayerConnectingHook.EndPlayerConnectingHook(builder);

            var hookNameOffset = builder.CreateString("player_connecting");
            var hookId = _ipcServer.NextHookId();

            GameHook.StartGameHook(builder);
            GameHook.AddHookId(builder, hookId);
            GameHook.AddHookName(builder, hookNameOffset);
            GameHook.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameHook.AddPayloadType(builder, HookPayload.PlayerConnectingHook);
            GameHook.AddPayload(builder, hookPayloadOffset.Value);
            var gameHookOffset = GameHook.EndGameHook(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Hook);
            Message.AddPayloadType(builder, MessagePayload.GameHook);
            Message.AddPayload(builder, gameHookOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);

            var response = _ipcServer.SendHookAndWait(hookId, builder.SizedByteArray());
            if (response != null)
            {
                var msg = Message.GetRootAsMessage(new ByteBuffer(response));
                if (msg.PayloadType == MessagePayload.HookResponse)
                {
                    var hookResponse = msg.Payload<HookResponse>().Value;
                    if (hookResponse.PayloadType == HookResponsePayload.PlayerConnectingResponse)
                    {
                        var connectResponse = hookResponse.Payload<PlayerConnectingResponse>().Value;
                        if (!connectResponse.Allow)
                        {
                            return connectResponse.DenyReason ?? "Connection denied";
                        }
                    }
                }
            }

            return null;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!_isInitialized) return;

            var builder = GetBuilder();
            
            var playerIdOffset = builder.CreateString(player.UserIDString);
            var playerNameOffset = builder.CreateString(player.displayName);
            var steamIdOffset = builder.CreateString(player.UserIDString);
            var ipOffset = builder.CreateString(player.net?.connection?.ipaddress ?? "unknown");
            
            var pos = player.transform.position;
            
            PlayerInfo.StartPlayerInfo(builder);
            PlayerInfo.AddPlayerId(builder, playerIdOffset);
            PlayerInfo.AddPlayerName(builder, playerNameOffset);
            PlayerInfo.AddSteamId(builder, steamIdOffset);
            PlayerInfo.AddIpAddress(builder, ipOffset);
            PlayerInfo.AddPosition(builder, Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
            var playerInfoOffset = PlayerInfo.EndPlayerInfo(builder);

            PlayerConnectedEvent.StartPlayerConnectedEvent(builder);
            PlayerConnectedEvent.AddPlayer(builder, playerInfoOffset);
            var eventOffset = PlayerConnectedEvent.EndPlayerConnectedEvent(builder);

            var eventNameOffset = builder.CreateString("player_connected");
            GameEvent.StartGameEvent(builder);
            GameEvent.AddEventName(builder, eventNameOffset);
            GameEvent.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameEvent.AddPayloadType(builder, EventPayload.PlayerConnectedEvent);
            GameEvent.AddPayload(builder, eventOffset.Value);
            var gameEventOffset = GameEvent.EndGameEvent(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Event);
            Message.AddPayloadType(builder, MessagePayload.GameEvent);
            Message.AddPayload(builder, gameEventOffset.Value);
            var msgOffset = Message.EndMessage(builder);
            
            builder.Finish(msgOffset.Value);
            _ipcServer.SendMessage(builder.SizedByteArray());
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!_isInitialized) return;

            var builder = GetBuilder();
            
            var playerIdOffset = builder.CreateString(player.UserIDString);
            var playerNameOffset = builder.CreateString(player.displayName);
            var reasonOffset = builder.CreateString(reason ?? "Unknown");

            PlayerDisconnectedEvent.StartPlayerDisconnectedEvent(builder);
            PlayerDisconnectedEvent.AddPlayerId(builder, playerIdOffset);
            PlayerDisconnectedEvent.AddPlayerName(builder, playerNameOffset);
            PlayerDisconnectedEvent.AddReason(builder, reasonOffset);
            var eventOffset = PlayerDisconnectedEvent.EndPlayerDisconnectedEvent(builder);

            var eventNameOffset = builder.CreateString("player_disconnected");
            GameEvent.StartGameEvent(builder);
            GameEvent.AddEventName(builder, eventNameOffset);
            GameEvent.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameEvent.AddPayloadType(builder, EventPayload.PlayerDisconnectedEvent);
            GameEvent.AddPayload(builder, eventOffset.Value);
            var gameEventOffset = GameEvent.EndGameEvent(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Event);
            Message.AddPayloadType(builder, MessagePayload.GameEvent);
            Message.AddPayload(builder, gameEventOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);
            _ipcServer.SendMessage(builder.SizedByteArray());
        }

        private object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (!_isInitialized || player == null) return null;

            var builder = GetBuilder();
            
            var playerIdOffset = builder.CreateString(player.UserIDString);
            var messageOffset = builder.CreateString(message);

            PlayerChatHook.StartPlayerChatHook(builder);
            PlayerChatHook.AddPlayerId(builder, playerIdOffset);
            PlayerChatHook.AddMessage(builder, messageOffset);
            var hookPayloadOffset = PlayerChatHook.EndPlayerChatHook(builder);

            var hookNameOffset = builder.CreateString("player_chat");
            var hookId = _ipcServer.NextHookId();
            
            GameHook.StartGameHook(builder);
            GameHook.AddHookId(builder, hookId);
            GameHook.AddHookName(builder, hookNameOffset);
            GameHook.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameHook.AddPayloadType(builder, HookPayload.PlayerChatHook);
            GameHook.AddPayload(builder, hookPayloadOffset.Value);
            var gameHookOffset = GameHook.EndGameHook(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Hook);
            Message.AddPayloadType(builder, MessagePayload.GameHook);
            Message.AddPayload(builder, gameHookOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);
            
            var response = _ipcServer.SendHookAndWait(hookId, builder.SizedByteArray());
            if (response != null)
            {
                var msg = Message.GetRootAsMessage(new ByteBuffer(response));
                if (msg.PayloadType == MessagePayload.HookResponse)
                {
                    var hookResponse = msg.Payload<HookResponse>().Value;
                    if (hookResponse.PayloadType == HookResponsePayload.PlayerChatResponse)
                    {
                        var chatResponse = hookResponse.Payload<PlayerChatResponse>().Value;
                        if (chatResponse.Block)
                        {
                            return true; // Block message
                        }
                        // Could modify message here if needed
                    }
                }
            }

            // Also send as event for logging
            SendChatEvent(player, message);
            return null;
        }

        private void SendChatEvent(BasePlayer player, string message)
        {
            var builder = GetBuilder();
            
            var playerIdOffset = builder.CreateString(player.UserIDString);
            var playerNameOffset = builder.CreateString(player.displayName);
            var messageOffset = builder.CreateString(message);

            ChatMessageEvent.StartChatMessageEvent(builder);
            ChatMessageEvent.AddPlayerId(builder, playerIdOffset);
            ChatMessageEvent.AddPlayerName(builder, playerNameOffset);
            ChatMessageEvent.AddMessage(builder, messageOffset);
            var eventOffset = ChatMessageEvent.EndChatMessageEvent(builder);

            var eventNameOffset = builder.CreateString("chat_message");
            GameEvent.StartGameEvent(builder);
            GameEvent.AddEventName(builder, eventNameOffset);
            GameEvent.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameEvent.AddPayloadType(builder, EventPayload.ChatMessageEvent);
            GameEvent.AddPayload(builder, eventOffset.Value);
            var gameEventOffset = GameEvent.EndGameEvent(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Event);
            Message.AddPayloadType(builder, MessagePayload.GameEvent);
            Message.AddPayload(builder, gameEventOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);
            _ipcServer.SendMessage(builder.SizedByteArray());
        }

        private object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (!_isInitialized) return null;

            var builder = GetBuilder();
            var killer = info?.InitiatorPlayer;
            var weapon = info?.WeaponPrefab?.ShortPrefabName ?? "unknown";

            var playerIdOffset = builder.CreateString(player.UserIDString);
            var killerIdOffset = builder.CreateString(killer?.UserIDString ?? "environment");
            var weaponOffset = builder.CreateString(weapon);

            PlayerDeathEvent.StartPlayerDeathEvent(builder);
            PlayerDeathEvent.AddPlayerId(builder, playerIdOffset);
            PlayerDeathEvent.AddKillerId(builder, killerIdOffset);
            PlayerDeathEvent.AddWeapon(builder, weaponOffset);
            var eventOffset = PlayerDeathEvent.EndPlayerDeathEvent(builder);

            var eventNameOffset = builder.CreateString("player_death");
            GameEvent.StartGameEvent(builder);
            GameEvent.AddEventName(builder, eventNameOffset);
            GameEvent.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameEvent.AddPayloadType(builder, EventPayload.PlayerDeathEvent);
            GameEvent.AddPayload(builder, eventOffset.Value);
            var gameEventOffset = GameEvent.EndGameEvent(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Event);
            Message.AddPayloadType(builder, MessagePayload.GameEvent);
            Message.AddPayload(builder, gameEventOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);
            _ipcServer.SendMessage(builder.SizedByteArray());
            return null;
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (!_isInitialized) return;

            var builder = GetBuilder();
            var pos = player.transform.position;

            var playerIdOffset = builder.CreateString(player.UserIDString);

            PlayerRespawnEvent.StartPlayerRespawnEvent(builder);
            PlayerRespawnEvent.AddPlayerId(builder, playerIdOffset);
            PlayerRespawnEvent.AddLocation(builder, Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
            var eventOffset = PlayerRespawnEvent.EndPlayerRespawnEvent(builder);

            var eventNameOffset = builder.CreateString("player_respawn");
            GameEvent.StartGameEvent(builder);
            GameEvent.AddEventName(builder, eventNameOffset);
            GameEvent.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameEvent.AddPayloadType(builder, EventPayload.PlayerRespawnEvent);
            GameEvent.AddPayload(builder, eventOffset.Value);
            var gameEventOffset = GameEvent.EndGameEvent(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Event);
            Message.AddPayloadType(builder, MessagePayload.GameEvent);
            Message.AddPayload(builder, gameEventOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);
            _ipcServer.SendMessage(builder.SizedByteArray());
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!_isInitialized) return null;
            
            var player = entity as BasePlayer;
            if (player == null) return null;

            var builder = GetBuilder();
            var totalDamage = info.damageTypes.Total();
            var majorType = info.damageTypes.GetMajorityDamageType();
            var damageType = ConvertDamageType(majorType);

            var playerIdOffset = builder.CreateString(player.UserIDString);
            var attackerIdOffset = builder.CreateString(info.InitiatorPlayer?.UserIDString ?? "unknown");

            PlayerTakingDamageHook.StartPlayerTakingDamageHook(builder);
            PlayerTakingDamageHook.AddPlayerId(builder, playerIdOffset);
            PlayerTakingDamageHook.AddDamage(builder, totalDamage);
            PlayerTakingDamageHook.AddDamageType(builder, damageType);
            PlayerTakingDamageHook.AddAttackerId(builder, attackerIdOffset);
            var hookPayloadOffset = PlayerTakingDamageHook.EndPlayerTakingDamageHook(builder);

            var hookNameOffset = builder.CreateString("player_taking_damage");
            var hookId = _ipcServer.NextHookId();

            GameHook.StartGameHook(builder);
            GameHook.AddHookId(builder, hookId);
            GameHook.AddHookName(builder, hookNameOffset);
            GameHook.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameHook.AddPayloadType(builder, HookPayload.PlayerTakingDamageHook);
            GameHook.AddPayload(builder, hookPayloadOffset.Value);
            var gameHookOffset = GameHook.EndGameHook(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Hook);
            Message.AddPayloadType(builder, MessagePayload.GameHook);
            Message.AddPayload(builder, gameHookOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);

            var response = _ipcServer.SendHookAndWait(hookId, builder.SizedByteArray());
            if (response != null)
            {
                var msg = Message.GetRootAsMessage(new ByteBuffer(response));
                if (msg.PayloadType == MessagePayload.HookResponse)
                {
                    var hookResponse = msg.Payload<HookResponse>().Value;
                    if (hookResponse.PayloadType == HookResponsePayload.PlayerDamageResponse)
                    {
                        var damageResponse = hookResponse.Payload<PlayerDamageResponse>().Value;
                        if (damageResponse.Cancel)
                        {
                            return true; // Cancel damage
                        }
                        // Modify damage
                        var newDamage = damageResponse.ModifiedDamage;
                        if (totalDamage > 0)
                        {
                            info.damageTypes.ScaleAll(newDamage / totalDamage);
                        }
                    }
                }
            }

            // Also send as event
            SendDamageEvent(player, info);
            return null;
        }

        private void SendDamageEvent(BasePlayer player, HitInfo info)
        {
            var builder = GetBuilder();
            var totalDamage = info.damageTypes.Total();
            var majorType = info.damageTypes.GetMajorityDamageType();
            var damageType = ConvertDamageType(majorType);

            var playerIdOffset = builder.CreateString(player.UserIDString);
            var attackerIdOffset = builder.CreateString(info.InitiatorPlayer?.UserIDString ?? "unknown");

            PlayerDamageEvent.StartPlayerDamageEvent(builder);
            PlayerDamageEvent.AddPlayerId(builder, playerIdOffset);
            PlayerDamageEvent.AddDamage(builder, totalDamage);
            PlayerDamageEvent.AddDamageType(builder, damageType);
            PlayerDamageEvent.AddAttackerId(builder, attackerIdOffset);
            var eventOffset = PlayerDamageEvent.EndPlayerDamageEvent(builder);

            var eventNameOffset = builder.CreateString("player_damage");
            GameEvent.StartGameEvent(builder);
            GameEvent.AddEventName(builder, eventNameOffset);
            GameEvent.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameEvent.AddPayloadType(builder, EventPayload.PlayerDamageEvent);
            GameEvent.AddPayload(builder, eventOffset.Value);
            var gameEventOffset = GameEvent.EndGameEvent(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Event);
            Message.AddPayloadType(builder, MessagePayload.GameEvent);
            Message.AddPayload(builder, gameEventOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);
            _ipcServer.SendMessage(builder.SizedByteArray());
        }

        #endregion

        #region Building Hooks

        private object OnEntityBuilt(Planner plan, GameObject go)
        {
            if (!_isInitialized) return null;

            var player = plan.GetOwnerPlayer();
            if (player == null) return null;

            var builder = GetBuilder();
            var entity = go.ToBaseEntity();
            var pos = go.transform.position;

            var playerIdOffset = builder.CreateString(player.UserIDString);
            var structureTypeOffset = builder.CreateString(entity?.ShortPrefabName ?? "unknown");

            PlayerBuildHook.StartPlayerBuildHook(builder);
            PlayerBuildHook.AddPlayerId(builder, playerIdOffset);
            PlayerBuildHook.AddStructureType(builder, structureTypeOffset);
            PlayerBuildHook.AddLocation(builder, Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
            var hookPayloadOffset = PlayerBuildHook.EndPlayerBuildHook(builder);

            var hookNameOffset = builder.CreateString("player_build");
            var hookId = _ipcServer.NextHookId();

            GameHook.StartGameHook(builder);
            GameHook.AddHookId(builder, hookId);
            GameHook.AddHookName(builder, hookNameOffset);
            GameHook.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameHook.AddPayloadType(builder, HookPayload.PlayerBuildHook);
            GameHook.AddPayload(builder, hookPayloadOffset.Value);
            var gameHookOffset = GameHook.EndGameHook(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Hook);
            Message.AddPayloadType(builder, MessagePayload.GameHook);
            Message.AddPayload(builder, gameHookOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);

            var response = _ipcServer.SendHookAndWait(hookId, builder.SizedByteArray());
            if (response != null)
            {
                var msg = Message.GetRootAsMessage(new ByteBuffer(response));
                if (msg.PayloadType == MessagePayload.HookResponse)
                {
                    var hookResponse = msg.Payload<HookResponse>().Value;
                    if (hookResponse.PayloadType == HookResponsePayload.PlayerBuildResponse)
                    {
                        var buildResponse = hookResponse.Payload<PlayerBuildResponse>().Value;
                        if (!buildResponse.Allow)
                        {
                            return true; // Deny building
                        }
                    }
                }
            }

            // Also send as event
            SendStructurePlacedEvent(player, entity, pos);
            return null;
        }

        private void SendStructurePlacedEvent(BasePlayer player, BaseEntity entity, Vector3 pos)
        {
            var builder = GetBuilder();

            var playerIdOffset = builder.CreateString(player.UserIDString);
            var structureTypeOffset = builder.CreateString(entity?.ShortPrefabName ?? "unknown");

            StructurePlacedEvent.StartStructurePlacedEvent(builder);
            StructurePlacedEvent.AddPlayerId(builder, playerIdOffset);
            StructurePlacedEvent.AddStructureType(builder, structureTypeOffset);
            StructurePlacedEvent.AddLocation(builder, Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
            var eventOffset = StructurePlacedEvent.EndStructurePlacedEvent(builder);

            var eventNameOffset = builder.CreateString("structure_placed");
            GameEvent.StartGameEvent(builder);
            GameEvent.AddEventName(builder, eventNameOffset);
            GameEvent.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameEvent.AddPayloadType(builder, EventPayload.StructurePlacedEvent);
            GameEvent.AddPayload(builder, eventOffset.Value);
            var gameEventOffset = GameEvent.EndGameEvent(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Event);
            Message.AddPayloadType(builder, MessagePayload.GameEvent);
            Message.AddPayload(builder, gameEventOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);
            _ipcServer.SendMessage(builder.SizedByteArray());
        }

        private void OnStructureDemolish(BuildingBlock block, BasePlayer player)
        {
            if (!_isInitialized) return;

            var builder = GetBuilder();

            var structureIdOffset = builder.CreateString(block.net.ID.ToString());
            var destroyerIdOffset = builder.CreateString(player?.UserIDString ?? "unknown");

            StructureDestroyedEvent.StartStructureDestroyedEvent(builder);
            StructureDestroyedEvent.AddStructureId(builder, structureIdOffset);
            StructureDestroyedEvent.AddDestroyerId(builder, destroyerIdOffset);
            var eventOffset = StructureDestroyedEvent.EndStructureDestroyedEvent(builder);

            var eventNameOffset = builder.CreateString("structure_destroyed");
            GameEvent.StartGameEvent(builder);
            GameEvent.AddEventName(builder, eventNameOffset);
            GameEvent.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameEvent.AddPayloadType(builder, EventPayload.StructureDestroyedEvent);
            GameEvent.AddPayload(builder, eventOffset.Value);
            var gameEventOffset = GameEvent.EndGameEvent(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Event);
            Message.AddPayloadType(builder, MessagePayload.GameEvent);
            Message.AddPayload(builder, gameEventOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);
            _ipcServer.SendMessage(builder.SizedByteArray());
        }

        #endregion

        #region Entity Hooks

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!_isInitialized) return;

            // Limit to important entities to avoid spam
            if (entity is BasePlayer || entity is BaseNpc || entity is ResourceEntity)
            {
                var builder = GetBuilder();
                var pos = entity.transform.position;

                var entityIdOffset = builder.CreateString(entity.net.ID.ToString());
                var entityTypeOffset = builder.CreateString(entity.ShortPrefabName);

                EntitySpawnedEvent.StartEntitySpawnedEvent(builder);
                EntitySpawnedEvent.AddEntityId(builder, entityIdOffset);
                EntitySpawnedEvent.AddEntityType(builder, entityTypeOffset);
                EntitySpawnedEvent.AddLocation(builder, Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
                var eventOffset = EntitySpawnedEvent.EndEntitySpawnedEvent(builder);

                var eventNameOffset = builder.CreateString("entity_spawned");
                GameEvent.StartGameEvent(builder);
                GameEvent.AddEventName(builder, eventNameOffset);
                GameEvent.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                GameEvent.AddPayloadType(builder, EventPayload.EntitySpawnedEvent);
                GameEvent.AddPayload(builder, eventOffset.Value);
                var gameEventOffset = GameEvent.EndGameEvent(builder);

                Message.StartMessage(builder);
                Message.AddMsgType(builder, MessageType.Event);
                Message.AddPayloadType(builder, MessagePayload.GameEvent);
                Message.AddPayload(builder, gameEventOffset.Value);
                var msgOffset = Message.EndMessage(builder);

                builder.Finish(msgOffset.Value);
                _ipcServer.SendMessage(builder.SizedByteArray());
            }
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (!_isInitialized) return;

            if (entity is BasePlayer || entity is BaseNpc || entity is ResourceEntity)
            {
                var builder = GetBuilder();

                var entityIdOffset = builder.CreateString(entity.net.ID.ToString());
                var entityTypeOffset = builder.CreateString(entity.ShortPrefabName);

                EntityKilledEvent.StartEntityKilledEvent(builder);
                EntityKilledEvent.AddEntityId(builder, entityIdOffset);
                EntityKilledEvent.AddEntityType(builder, entityTypeOffset);
                var eventOffset = EntityKilledEvent.EndEntityKilledEvent(builder);

                var eventNameOffset = builder.CreateString("entity_killed");
                GameEvent.StartGameEvent(builder);
                GameEvent.AddEventName(builder, eventNameOffset);
                GameEvent.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                GameEvent.AddPayloadType(builder, EventPayload.EntityKilledEvent);
                GameEvent.AddPayload(builder, eventOffset.Value);
                var gameEventOffset = GameEvent.EndGameEvent(builder);

                Message.StartMessage(builder);
                Message.AddMsgType(builder, MessageType.Event);
                Message.AddPayloadType(builder, MessagePayload.GameEvent);
                Message.AddPayload(builder, gameEventOffset.Value);
                var msgOffset = Message.EndMessage(builder);

                builder.Finish(msgOffset.Value);
                _ipcServer.SendMessage(builder.SizedByteArray());
            }
        }

        #endregion

        #region Loot and Crafting Hooks

        private object OnLootPlayer(BasePlayer player, BasePlayer target)
        {
            if (!_isInitialized) return null;

            var builder = GetBuilder();

            var playerIdOffset = builder.CreateString(player.UserIDString);
            var containerIdOffset = builder.CreateString(target.UserIDString);

            PlayerLootHook.StartPlayerLootHook(builder);
            PlayerLootHook.AddPlayerId(builder, playerIdOffset);
            PlayerLootHook.AddContainerId(builder, containerIdOffset);
            var hookPayloadOffset = PlayerLootHook.EndPlayerLootHook(builder);

            var hookNameOffset = builder.CreateString("player_loot");
            var hookId = _ipcServer.NextHookId();

            GameHook.StartGameHook(builder);
            GameHook.AddHookId(builder, hookId);
            GameHook.AddHookName(builder, hookNameOffset);
            GameHook.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameHook.AddPayloadType(builder, HookPayload.PlayerLootHook);
            GameHook.AddPayload(builder, hookPayloadOffset.Value);
            var gameHookOffset = GameHook.EndGameHook(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Hook);
            Message.AddPayloadType(builder, MessagePayload.GameHook);
            Message.AddPayload(builder, gameHookOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);

            var response = _ipcServer.SendHookAndWait(hookId, builder.SizedByteArray());
            if (response != null)
            {
                var msg = Message.GetRootAsMessage(new ByteBuffer(response));
                if (msg.PayloadType == MessagePayload.HookResponse)
                {
                    var hookResponse = msg.Payload<HookResponse>().Value;
                    if (hookResponse.PayloadType == HookResponsePayload.PlayerLootResponse)
                    {
                        var lootResponse = hookResponse.Payload<PlayerLootResponse>().Value;
                        if (!lootResponse.Allow)
                        {
                            return true; // Deny looting
                        }
                    }
                }
            }

            return null;
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (!_isInitialized) return;

            var player = task.owner;
            if (player == null) return;

            var builder = GetBuilder();

            var playerIdOffset = builder.CreateString(player.UserIDString);
            var itemNameOffset = builder.CreateString(item.info.displayName.english);

            ItemCraftedEvent.StartItemCraftedEvent(builder);
            ItemCraftedEvent.AddPlayerId(builder, playerIdOffset);
            ItemCraftedEvent.AddItemName(builder, itemNameOffset);
            ItemCraftedEvent.AddAmount(builder, item.amount);
            var eventOffset = ItemCraftedEvent.EndItemCraftedEvent(builder);

            var eventNameOffset = builder.CreateString("item_crafted");
            GameEvent.StartGameEvent(builder);
            GameEvent.AddEventName(builder, eventNameOffset);
            GameEvent.AddTimestamp(builder, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            GameEvent.AddPayloadType(builder, EventPayload.ItemCraftedEvent);
            GameEvent.AddPayload(builder, eventOffset.Value);
            var gameEventOffset = GameEvent.EndGameEvent(builder);

            Message.StartMessage(builder);
            Message.AddMsgType(builder, MessageType.Event);
            Message.AddPayloadType(builder, MessagePayload.GameEvent);
            Message.AddPayload(builder, gameEventOffset.Value);
            var msgOffset = Message.EndMessage(builder);

            builder.Finish(msgOffset.Value);
            _ipcServer.SendMessage(builder.SizedByteArray());
        }

        #endregion
    }

    #region IPC Server Implementation

    public class IpcServer
    {
        private readonly CarbonPlugin _plugin;
        private Thread _serverThread;
        private Thread _clientThread;
        private volatile bool _isRunning;
        private readonly string _endpoint;
        private readonly bool _isWindows;
        
        // Hook correlation
        private uint _hookIdCounter;
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<byte[]>> _pendingHooks = 
            new ConcurrentDictionary<uint, TaskCompletionSource<byte[]>>();
        
        // Connected clients for event broadcasting
        private readonly ConcurrentBag<Stream> _connectedClients = new ConcurrentBag<Stream>();
        
        // Timeout for hook responses (ms)
        private const int HookTimeoutMs = 100;

        public IpcServer(CarbonPlugin plugin)
        {
            _plugin = plugin;
            _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _endpoint = _isWindows 
                ? "rust-java-mods" // Pipe name only for Windows
                : "/tmp/rust-java-mods.sock";
        }

        public string GetEndpoint() => _isWindows ? $"\\\\.\\pipe\\{_endpoint}" : _endpoint;

        public uint NextHookId() => Interlocked.Increment(ref _hookIdCounter);

        public void Start()
        {
            _isRunning = true;
            _serverThread = new Thread(ServerLoop) { IsBackground = true, Name = "RJM-IPC-Server" };
            _serverThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _serverThread?.Join(1000);
            
            if (!_isWindows && File.Exists(_endpoint))
            {
                try { File.Delete(_endpoint); } catch { }
            }
        }

        private void ServerLoop()
        {
            try
            {
                if (_isWindows)
                {
                    WindowsServerLoop();
                }
                else
                {
                    UnixServerLoop();
                }
            }
            catch (Exception ex)
            {
                _plugin.PrintError($"IPC Server error: {ex.Message}");
            }
        }

        private void WindowsServerLoop()
        {
            while (_isRunning)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(_endpoint, 
                        PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        server.WaitForConnection();
                        HandleClient(server);
                    }
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        _plugin.PrintError($"Named Pipe error: {ex.Message}");
                    }
                }
            }
        }

        private void UnixServerLoop()
        {
            // Clean up existing socket
            if (File.Exists(_endpoint))
            {
                try { File.Delete(_endpoint); } catch { }
            }

            Socket socket = null;
            try
            {
                socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                socket.Bind(new UnixDomainSocketEndPoint(_endpoint));
                socket.Listen(5);

                while (_isRunning)
                {
                    Socket client = null;
                    try
                    {
                        client = socket.Accept();
                        var clientCopy = client;
                        client = null; // Ownership transferred
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            try
                            {
                                using (var stream = new NetworkStream(clientCopy, true))
                                {
                                    HandleClient(stream);
                                }
                            }
                            catch (Exception ex)
                            {
                                _plugin.PrintError($"Client handling error: {ex.Message}");
                            }
                            finally
                            {
                                clientCopy?.Dispose();
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        client?.Dispose();
                        if (_isRunning)
                        {
                            _plugin.PrintError($"Unix Socket error: {ex.Message}");
                        }
                    }
                }
            }
            finally
            {
                socket?.Close();
                socket?.Dispose();
            }
        }

        private void HandleClient(Stream stream)
        {
            _connectedClients.Add(stream);
            
            try
            {
                while (_isRunning && stream.CanRead)
                {
                    // Read length prefix (4 bytes, little-endian)
                    var lengthBuffer = new byte[4];
                    var totalRead = 0;
                    while (totalRead < 4)
                    {
                        var bytesRead = stream.Read(lengthBuffer, totalRead, 4 - totalRead);
                        if (bytesRead == 0) return; // Connection closed
                        totalRead += bytesRead;
                    }

                    var length = BitConverter.ToInt32(lengthBuffer, 0);
                    if (length <= 0 || length > 1048576) return; // Max 1MB

                    // Read FlatBuffer message
                    var buffer = new byte[length];
                    totalRead = 0;
                    while (totalRead < length)
                    {
                        var bytesRead = stream.Read(buffer, totalRead, length - totalRead);
                        if (bytesRead == 0) return;
                        totalRead += bytesRead;
                    }

                    // Parse and handle response
                    var msg = Message.GetRootAsMessage(new ByteBuffer(buffer));
                    if (msg.MsgType == MessageType.HookResponse && msg.PayloadType == MessagePayload.HookResponse)
                    {
                        var hookResponse = msg.Payload<HookResponse>().Value;
                        var hookId = hookResponse.HookId;
                        
                        if (_pendingHooks.TryRemove(hookId, out var tcs))
                        {
                            tcs.TrySetResult(buffer);
                        }
                    }
                    else if (msg.MsgType == MessageType.Command && msg.PayloadType == MessagePayload.GameCommand)
                    {
                        HandleGameCommand(stream, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                _plugin.PrintError($"Client handler error: {ex.Message}");
            }
        }

        private void HandleGameCommand(Stream client, Message msg)
        {
            uint commandId = 0;
            var success = false;
            string errorMessage = null;

            try
            {
                var gameCommand = msg.Payload<GameCommand>().Value;
                commandId = gameCommand.CommandId;

                switch (gameCommand.PayloadType)
                {
                    case CommandPayload.SendChatCommand:
                    {
                        var cmd = gameCommand.Payload<SendChatCommand>().Value;
                        var playerId = cmd.PlayerId;
                        var message = cmd.Message;

                        if (string.IsNullOrWhiteSpace(message))
                        {
                            errorMessage = "message is required";
                            break;
                        }

                        if (string.IsNullOrWhiteSpace(playerId))
                        {
                            foreach (var p in BasePlayer.activePlayerList)
                            {
                                try { p?.ChatMessage(message); } catch { }
                            }
                            success = true;
                            break;
                        }

                        if (!ulong.TryParse(playerId, out var uid))
                        {
                            errorMessage = $"invalid player_id: {playerId}";
                            break;
                        }

                        var player = BasePlayer.FindByID(uid) ?? BasePlayer.FindSleeping(uid);
                        if (player == null)
                        {
                            errorMessage = $"player not found: {playerId}";
                            break;
                        }

                        player.ChatMessage(message);
                        success = true;
                        break;
                    }
                    case CommandPayload.KickPlayerCommand:
                    {
                        var cmd = gameCommand.Payload<KickPlayerCommand>().Value;
                        var playerId = cmd.PlayerId;
                        var reason = cmd.Reason ?? "Kicked";

                        if (!ulong.TryParse(playerId, out var uid))
                        {
                            errorMessage = $"invalid player_id: {playerId}";
                            break;
                        }

                        var player = BasePlayer.FindByID(uid) ?? BasePlayer.FindSleeping(uid);
                        if (player == null)
                        {
                            errorMessage = $"player not found: {playerId}";
                            break;
                        }

                        player.Kick(reason);
                        success = true;
                        break;
                    }
                    case CommandPayload.TeleportPlayerCommand:
                    {
                        var cmd = gameCommand.Payload<TeleportPlayerCommand>().Value;
                        var playerId = cmd.PlayerId;
                        var loc = cmd.Location;

                        if (!ulong.TryParse(playerId, out var uid))
                        {
                            errorMessage = $"invalid player_id: {playerId}";
                            break;
                        }

                        var player = BasePlayer.FindByID(uid) ?? BasePlayer.FindSleeping(uid);
                        if (player == null)
                        {
                            errorMessage = $"player not found: {playerId}";
                            break;
                        }

                        player.Teleport(new Vector3(loc.X, loc.Y, loc.Z));
                        success = true;
                        break;
                    }
                    case CommandPayload.GiveItemCommand:
                    {
                        var cmd = gameCommand.Payload<GiveItemCommand>().Value;
                        var playerId = cmd.PlayerId;
                        var itemName = cmd.ItemName;
                        var amount = cmd.Amount;

                        if (string.IsNullOrWhiteSpace(itemName))
                        {
                            errorMessage = "item_name is required";
                            break;
                        }

                        if (!ulong.TryParse(playerId, out var uid))
                        {
                            errorMessage = $"invalid player_id: {playerId}";
                            break;
                        }

                        var player = BasePlayer.FindByID(uid) ?? BasePlayer.FindSleeping(uid);
                        if (player == null)
                        {
                            errorMessage = $"player not found: {playerId}";
                            break;
                        }

                        var item = ItemManager.CreateByName(itemName, amount <= 0 ? 1 : amount);
                        if (item == null)
                        {
                            errorMessage = $"unknown item: {itemName}";
                            break;
                        }

                        player.GiveItem(item);
                        success = true;
                        break;
                    }
                    default:
                        errorMessage = $"unsupported command payload: {gameCommand.PayloadType}";
                        break;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                success = false;
            }

            try
            {
                var builder = new FlatBufferBuilder(256);
                var errOffset = errorMessage != null ? builder.CreateString(errorMessage) : default(StringOffset?);

                CommandResponse.StartCommandResponse(builder);
                CommandResponse.AddCommandId(builder, commandId);
                CommandResponse.AddSuccess(builder, success);
                if (errOffset.HasValue)
                {
                    CommandResponse.AddErrorMessage(builder, errOffset.Value);
                }
                var respOffset = CommandResponse.EndCommandResponse(builder);

                Message.StartMessage(builder);
                Message.AddMsgType(builder, MessageType.CommandResponse);
                Message.AddPayloadType(builder, MessagePayload.CommandResponse);
                Message.AddPayload(builder, respOffset.Value);
                var msgOffset = Message.EndMessage(builder);

                builder.Finish(msgOffset.Value);
                SendToClient(client, builder.SizedByteArray());
            }
            catch (Exception ex)
            {
                _plugin.PrintError($"Failed to send command response: {ex.Message}");
            }
        }

        private static void SendToClient(Stream client, byte[] data)
        {
            if (client == null || !client.CanWrite) return;
            var lengthBytes = BitConverter.GetBytes(data.Length);
            client.Write(lengthBytes, 0, 4);
            client.Write(data, 0, data.Length);
            client.Flush();
        }

        public void SendMessage(byte[] data)
        {
            // Fire-and-forget broadcast to all connected clients
            foreach (var client in _connectedClients)
            {
                try
                {
                    if (client.CanWrite)
                    {
                        var lengthBytes = BitConverter.GetBytes(data.Length);
                        client.Write(lengthBytes, 0, 4);
                        client.Write(data, 0, data.Length);
                        client.Flush();
                    }
                }
                catch
                {
                    // Client disconnected, ignore
                }
            }
        }

        public byte[] SendHookAndWait(uint hookId, byte[] data)
        {
            var tcs = new TaskCompletionSource<byte[]>();
            _pendingHooks[hookId] = tcs;

            try
            {
                SendMessage(data);
                
                // Wait with timeout
                if (tcs.Task.Wait(HookTimeoutMs))
                {
                    return tcs.Task.Result;
                }
            }
            catch (Exception ex)
            {
                _plugin.PrintError($"Hook wait error: {ex.Message}");
            }
            finally
            {
                _pendingHooks.TryRemove(hookId, out _);
            }

            return null; // Timeout or error - allow default behavior
        }
    }

    #endregion
}
