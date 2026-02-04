using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;

namespace DedicatedServerMod.Client.Data
{
    /// <summary>
    /// Stores and manages history of recently connected servers.
    /// Persists to JSON file in UserData directory.
    /// </summary>
    [Serializable]
    public class ServerHistoryEntry
    {
        [JsonProperty("ip")]
        public string IP { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("lastConnected")]
        public DateTime LastConnected { get; set; }

        [JsonProperty("serverName")]
        public string ServerName { get; set; }

        /// <summary>
        /// Display text for the history entry
        /// </summary>
        [JsonIgnore]
        public string DisplayText =>
            string.IsNullOrEmpty(ServerName) 
                ? $"{IP}:{Port}" 
                : $"{ServerName} ({IP}:{Port})";
    }

    /// <summary>
    /// Manages server connection history
    /// </summary>
    public static class ServerHistory
    {
        private const int MAX_HISTORY_SIZE = 10;
        private const string HISTORY_FILE_NAME = "server_history.json";

        private static readonly string HistoryFilePath = 
            Path.Combine(MelonEnvironment.UserDataDirectory, HISTORY_FILE_NAME);

        private static List<ServerHistoryEntry> _history;
        private static MelonLogger.Instance _logger;

        /// <summary>
        /// Initializes the server history system
        /// </summary>
        /// <param name="logger">Logger instance to use</param>
        public static void Initialize(MelonLogger.Instance logger)
        {
            _logger = logger;
            LoadHistory();
        }

        /// <summary>
        /// Gets the list of recent servers (most recent first)
        /// </summary>
        public static List<ServerHistoryEntry> GetHistory()
        {
            if (_history == null)
            {
                LoadHistory();
            }

            return new List<ServerHistoryEntry>(_history);
        }

        /// <summary>
        /// Adds a server to the connection history
        /// </summary>
        /// <param name="ip">Server IP address</param>
        /// <param name="port">Server port</param>
        /// <param name="serverName">Optional server name</param>
        public static void AddServer(string ip, int port, string serverName = null)
        {
            try
            {
                if (_history == null)
                {
                    LoadHistory();
                }

                // Remove existing entry for this server if it exists
                _history.RemoveAll(e => e.IP == ip && e.Port == port);

                // Add new entry at the front
                _history.Insert(0, new ServerHistoryEntry
                {
                    IP = ip,
                    Port = port,
                    LastConnected = DateTime.Now,
                    ServerName = serverName
                });

                // Trim to max size (remove oldest entries)
                while (_history.Count > MAX_HISTORY_SIZE)
                {
                    _history.RemoveAt(_history.Count - 1);
                }

                SaveHistory();
                _logger?.Msg($"Added server to history: {ip}:{port}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error adding server to history: {ex}");
            }
        }

        /// <summary>
        /// Updates the server name for an existing history entry
        /// </summary>
        /// <param name="ip">Server IP address</param>
        /// <param name="port">Server port</param>
        /// <param name="serverName">Server name to set</param>
        public static void UpdateServerName(string ip, int port, string serverName)
        {
            try
            {
                if (_history == null)
                {
                    LoadHistory();
                }

                var entry = _history.FirstOrDefault(e => e.IP == ip && e.Port == port);
                if (entry != null)
                {
                    entry.ServerName = serverName;
                    SaveHistory();
                    _logger?.Msg($"Updated server name in history: {serverName} ({ip}:{port})");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error updating server name in history: {ex}");
            }
        }

        /// <summary>
        /// Clears all server history
        /// </summary>
        public static void ClearHistory()
        {
            try
            {
                _history = new List<ServerHistoryEntry>();
                SaveHistory();
                _logger?.Msg("Server history cleared");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error clearing server history: {ex}");
            }
        }

        /// <summary>
        /// Removes a specific server from history
        /// </summary>
        /// <param name="ip">Server IP address</param>
        /// <param name="port">Server port</param>
        public static void RemoveServer(string ip, int port)
        {
            try
            {
                if (_history == null)
                {
                    LoadHistory();
                }

                int removed = _history.RemoveAll(e => e.IP == ip && e.Port == port);
                if (removed > 0)
                {
                    SaveHistory();
                    _logger?.Msg($"Removed server from history: {ip}:{port}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error removing server from history: {ex}");
            }
        }

        /// <summary>
        /// Loads server history from disk
        /// </summary>
        private static void LoadHistory()
        {
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    string json = File.ReadAllText(HistoryFilePath);
                    _history = JsonConvert.DeserializeObject<List<ServerHistoryEntry>>(json) 
                        ?? new List<ServerHistoryEntry>();
                    
                    // Sort by most recent first
                    _history = _history.OrderByDescending(e => e.LastConnected).ToList();
                    
                    _logger?.Msg($"Loaded {_history.Count} servers from history");
                }
                else
                {
                    _history = new List<ServerHistoryEntry>();
                    _logger?.Msg("No server history file found, created empty history");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error loading server history: {ex}");
                _history = new List<ServerHistoryEntry>();
            }
        }

        /// <summary>
        /// Saves server history to disk
        /// </summary>
        private static void SaveHistory()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_history, Formatting.Indented);
                File.WriteAllText(HistoryFilePath, json);
                _logger?.Msg("Server history saved");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error saving server history: {ex}");
            }
        }
    }
}
