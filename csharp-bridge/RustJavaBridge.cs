using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Carbon.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Carbon.Plugins
{
    [Info("RustJavaBridge", "RustJavaMods", "0.1.0")]
    [Description("Bridge between Facepunch Rust game and Java modding API using Carbon")]
    public class RustJavaBridge : CarbonPlugin
    {
        private IpcServer _ipcServer;
        private bool _isInitialized;

        #region Carbon Hooks

        private void OnServerInitialized()
        {
            Puts("Initializing Rust-Java Bridge (Carbon)...");
            
            try
            {
                _ipcServer = new IpcServer(this);
                _ipcServer.Start();
                _isInitialized = true;
                
                Puts("✓ Rust-Java Bridge initialized successfully");
                Puts($"  IPC Endpoint: {_ipcServer.GetEndpoint()}");
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

        #region Player Hooks

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!_isInitialized) return;

            var eventData = new Dictionary<string, object>
            {
                ["player_id"] = player.UserIDString,
                ["player_name"] = player.displayName,
                ["steam_id"] = player.UserIDString,
                ["ip_address"] = player.net?.connection?.ipaddress ?? "unknown"
            };

            _ipcServer.SendEvent("player_connected", eventData);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!_isInitialized) return;

            var eventData = new Dictionary<string, object>
            {
                ["player_id"] = player.UserIDString,
                ["player_name"] = player.displayName,
                ["reason"] = reason ?? "Unknown"
            };

            _ipcServer.SendEvent("player_disconnected", eventData);
        }

        private object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (!_isInitialized) return null;
            
            if (player == null) return null;

            var hookData = new Dictionary<string, object>
            {
                ["player_id"] = player.UserIDString,
                ["message"] = message
            };

            var response = _ipcServer.SendHook("player_chat", hookData);

            if (response != null)
            {
                if (response.ContainsKey("block") && Convert.ToBoolean(response["block"]))
                {
                    return true; // Block the message
                }
                if (response.ContainsKey("message"))
                {
                    // Message was modified
                    var newMessage = response["message"].ToString();
                    // Would need to implement message replacement in Carbon
                }
            }

            // Also send as event
            var eventData = new Dictionary<string, object>
            {
                ["player_id"] = player.UserIDString,
                ["player_name"] = player.displayName,
                ["message"] = message
            };
            _ipcServer.SendEvent("chat_message", eventData);

            return null; // Allow unchanged
        }

        private object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (!_isInitialized) return null;

            var killer = info?.InitiatorPlayer;
            var weapon = info?.WeaponPrefab?.ShortPrefabName ?? "unknown";

            var eventData = new Dictionary<string, object>
            {
                ["player_id"] = player.UserIDString,
                ["killer_id"] = killer?.UserIDString ?? "environment",
                ["weapon"] = weapon
            };

            _ipcServer.SendEvent("player_death", eventData);
            return null;
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (!_isInitialized) return;

            var eventData = new Dictionary<string, object>
            {
                ["player_id"] = player.UserIDString,
                ["location"] = new Dictionary<string, object>
                {
                    ["x"] = player.transform.position.x,
                    ["y"] = player.transform.position.y,
                    ["z"] = player.transform.position.z
                }
            };

            _ipcServer.SendEvent("player_respawn", eventData);
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!_isInitialized) return null;
            
            var player = entity as BasePlayer;
            if (player == null) return null;

            var hookData = new Dictionary<string, object>
            {
                ["player_id"] = player.UserIDString,
                ["damage"] = info.damageTypes.Total(),
                ["damage_type"] = info.damageTypes.GetMajorityDamageType().ToString()
            };

            var response = _ipcServer.SendHook("player_taking_damage", hookData);

            if (response != null)
            {
                if (response.ContainsKey("cancel") && Convert.ToBoolean(response["cancel"]))
                {
                    return true; // Cancel damage
                }
                if (response.ContainsKey("damage"))
                {
                    // Modify damage
                    var newDamage = Convert.ToSingle(response["damage"]);
                    var totalDamage = info.damageTypes.Total();
                    if (totalDamage > 0)
                    {
                        info.damageTypes.ScaleAll(newDamage / totalDamage);
                    }
                }
            }

            // Also send as event
            var eventData = new Dictionary<string, object>
            {
                ["player_id"] = player.UserIDString,
                ["damage"] = info.damageTypes.Total(),
                ["damage_type"] = info.damageTypes.GetMajorityDamageType().ToString(),
                ["attacker_id"] = info.InitiatorPlayer?.UserIDString ?? "unknown"
            };
            _ipcServer.SendEvent("player_damage", eventData);

            return null;
        }

        #endregion

        #region Building Hooks

        private object OnEntityBuilt(Planner plan, GameObject go)
        {
            if (!_isInitialized) return null;

            var player = plan.GetOwnerPlayer();
            if (player == null) return null;

            var entity = go.ToBaseEntity();
            var hookData = new Dictionary<string, object>
            {
                ["player_id"] = player.UserIDString,
                ["structure_type"] = entity?.ShortPrefabName ?? "unknown",
                ["location"] = new Dictionary<string, object>
                {
                    ["x"] = go.transform.position.x,
                    ["y"] = go.transform.position.y,
                    ["z"] = go.transform.position.z
                }
            };

            var response = _ipcServer.SendHook("player_build", hookData);

            if (response != null && response.ContainsKey("allow"))
            {
                if (!Convert.ToBoolean(response["allow"]))
                {
                    return true; // Deny building
                }
            }

            // Also send as event
            var eventData = new Dictionary<string, object>
            {
                ["player_id"] = player.UserIDString,
                ["structure_type"] = entity?.ShortPrefabName ?? "unknown",
                ["location"] = hookData["location"]
            };
            _ipcServer.SendEvent("structure_placed", eventData);

            return null;
        }

        private void OnStructureDemolish(BuildingBlock block, BasePlayer player)
        {
            if (!_isInitialized) return;

            var eventData = new Dictionary<string, object>
            {
                ["structure_id"] = block.net.ID.ToString(),
                ["destroyer_id"] = player?.UserIDString ?? "unknown"
            };

            _ipcServer.SendEvent("structure_destroyed", eventData);
        }

        #endregion

        #region Entity Hooks

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!_isInitialized) return;

            // Limit to important entities to avoid spam
            if (entity is BasePlayer || entity is BaseNpc || entity is ResourceEntity)
            {
                var eventData = new Dictionary<string, object>
                {
                    ["entity_id"] = entity.net.ID.ToString(),
                    ["entity_type"] = entity.ShortPrefabName,
                    ["location"] = new Dictionary<string, object>
                    {
                        ["x"] = entity.transform.position.x,
                        ["y"] = entity.transform.position.y,
                        ["z"] = entity.transform.position.z
                    }
                };

                _ipcServer.SendEvent("entity_spawned", eventData);
            }
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (!_isInitialized) return;

            if (entity is BasePlayer || entity is BaseNpc || entity is ResourceEntity)
            {
                var eventData = new Dictionary<string, object>
                {
                    ["entity_id"] = entity.net.ID.ToString(),
                    ["entity_type"] = entity.ShortPrefabName
                };

                _ipcServer.SendEvent("entity_killed", eventData);
            }
        }

        #endregion

        #region Loot and Crafting Hooks

        private object OnLootPlayer(BasePlayer player, BasePlayer target)
        {
            if (!_isInitialized) return null;

            var hookData = new Dictionary<string, object>
            {
                ["player_id"] = player.UserIDString,
                ["container_id"] = target.UserIDString
            };

            var response = _ipcServer.SendHook("player_loot", hookData);

            if (response != null && response.ContainsKey("allow"))
            {
                if (!Convert.ToBoolean(response["allow"]))
                {
                    return true; // Deny looting
                }
            }

            return null;
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (!_isInitialized) return;

            var player = task.owner;
            if (player == null) return;

            var eventData = new Dictionary<string, object>
            {
                ["player_id"] = player.UserIDString,
                ["item_name"] = item.info.displayName.english,
                ["amount"] = item.amount
            };

            _ipcServer.SendEvent("item_crafted", eventData);
        }

        #endregion
    }

    #region IPC Server Implementation

    public class IpcServer
    {
        private readonly CarbonPlugin _plugin;
        private Thread _serverThread;
        private bool _isRunning;
        private readonly string _endpoint;
        private readonly bool _isWindows;

        public IpcServer(CarbonPlugin plugin)
        {
            _plugin = plugin;
            _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _endpoint = _isWindows 
                ? "rust-java-mods" // Pipe name only for Windows
                : "/tmp/rust-java-mods.sock";
        }

        public string GetEndpoint() => _isWindows ? $"\\\\.\\pipe\\{_endpoint}" : _endpoint;

        public void Start()
        {
            _isRunning = true;
            _serverThread = new Thread(ServerLoop) { IsBackground = true };
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

            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Bind(new UnixDomainSocketEndPoint(_endpoint));
            socket.Listen(5);

            while (_isRunning)
            {
                try
                {
                    var client = socket.Accept();
                    ThreadPool.QueueUserWorkItem(_ => HandleClient(new NetworkStream(client, true)));
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        _plugin.PrintError($"Unix Socket error: {ex.Message}");
                    }
                }
            }

            socket.Close();
        }

        private void HandleClient(Stream stream)
        {
            try
            {
                var lengthBuffer = new byte[4];
                var bytesRead = stream.Read(lengthBuffer, 0, 4);
                if (bytesRead != 4) return;

                var length = BitConverter.ToInt32(lengthBuffer, 0);
                if (length <= 0 || length > 1048576) return; // Max 1MB

                var buffer = new byte[length];
                bytesRead = stream.Read(buffer, 0, length);
                if (bytesRead != length) return;

                var json = Encoding.UTF8.GetString(buffer);
                _plugin.Puts($"Received from Java: {json.Substring(0, Math.Min(100, json.Length))}...");

                // Echo back for now (Java can send requests)
                var response = Encoding.UTF8.GetBytes(json);
                var responseLength = BitConverter.GetBytes(response.Length);
                stream.Write(responseLength, 0, 4);
                stream.Write(response, 0, response.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                _plugin.PrintError($"Client handler error: {ex.Message}");
            }
        }

        public void SendEvent(string eventName, Dictionary<string, object> data)
        {
            try
            {
                var message = new
                {
                    Type = "Event",
                    Name = eventName,
                    Data = data
                };

                var json = JsonConvert.SerializeObject(message);
                // Events are fire-and-forget, logged only
                _plugin.Puts($"Event: {eventName}");
            }
            catch (Exception ex)
            {
                _plugin.PrintError($"Send event error: {ex.Message}");
            }
        }

        public Dictionary<string, object> SendHook(string hookName, Dictionary<string, object> data)
        {
            try
            {
                var message = new
                {
                    Type = "Hook",
                    Method = hookName,
                    Data = data
                };

                var json = JsonConvert.SerializeObject(message);
                _plugin.Puts($"Hook: {hookName}");

                // For now, return null (no synchronous Java response yet)
                // In full implementation, would wait for Java response
                return null;
            }
            catch (Exception ex)
            {
                _plugin.PrintError($"Send hook error: {ex.Message}");
                return null;
            }
        }
    }

    #endregion
}
