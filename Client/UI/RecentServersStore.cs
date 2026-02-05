using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader;
using Newtonsoft.Json;

namespace DedicatedServerMod.Client.UI
{
    /// <summary>
    /// Manages persistence of recently connected servers.
    /// Stores up to 5 most recent server connections.
    /// </summary>
    public class RecentServersStore
    {
        private const int MAX_RECENT_SERVERS = 5;
        private const string RECENT_SERVERS_FILE = "RecentServers.json";
        
        private readonly string filePath;
        private readonly MelonLogger.Instance logger;
        private List<ServerEntry> recentServers;

        /// <summary>
        /// Represents a recently connected server entry.
        /// </summary>
        public class ServerEntry
        {
            [JsonProperty("ip")]
            public string Ip { get; set; }
            
            [JsonProperty("port")]
            public int Port { get; set; }
            
            [JsonProperty("lastConnected")]
            public DateTime LastConnected { get; set; }

            /// <summary>
            /// Gets a display string for the server entry.
            /// </summary>
            public string DisplayText => $"{Ip}:{Port}";
        }

        /// <summary>
        /// Initializes a new instance of the RecentServersStore class.
        /// </summary>
        /// <param name="logger">Logger instance for error reporting.</param>
        public RecentServersStore(MelonLogger.Instance logger)
        {
            this.logger = logger;
            this.filePath = Path.Combine(MelonUtils.UserDataDirectory, RECENT_SERVERS_FILE);
            this.recentServers = new List<ServerEntry>();
            Load();
        }

        /// <summary>
        /// Gets the list of recent servers, most recent first.
        /// </summary>
        /// <returns>List of recent server entries.</returns>
        public List<ServerEntry> GetRecentServers()
        {
            return recentServers.ToList();
        }

        /// <summary>
        /// Adds a server to the recent servers list.
        /// If the server already exists, updates its last connected timestamp.
        /// Maintains a maximum of 5 entries.
        /// </summary>
        /// <param name="ip">Server IP address.</param>
        /// <param name="port">Server port.</param>
        public void AddServer(string ip, int port)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ip))
                {
                    return;
                }

                // Remove existing entry if it exists
                recentServers.RemoveAll(s => 
                    s.Ip.Equals(ip, StringComparison.OrdinalIgnoreCase) && s.Port == port);

                // Add new entry at the beginning
                recentServers.Insert(0, new ServerEntry
                {
                    Ip = ip,
                    Port = port,
                    LastConnected = DateTime.UtcNow
                });

                // Keep only the most recent MAX_RECENT_SERVERS entries
                if (recentServers.Count > MAX_RECENT_SERVERS)
                {
                    recentServers = recentServers.Take(MAX_RECENT_SERVERS).ToList();
                }

                Save();
            }
            catch (Exception ex)
            {
                logger.Error($"Error adding server to recent list: {ex}");
            }
        }

        /// <summary>
        /// Loads the recent servers list from disk.
        /// </summary>
        private void Load()
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    logger.Msg("No recent servers file found, starting fresh");
                    recentServers = new List<ServerEntry>();
                    return;
                }

                string json = File.ReadAllText(filePath);
                var loaded = JsonConvert.DeserializeObject<List<ServerEntry>>(json);
                
                if (loaded != null)
                {
                    recentServers = loaded.Take(MAX_RECENT_SERVERS).ToList();
                    logger.Msg($"Loaded {recentServers.Count} recent servers");
                }
                else
                {
                    recentServers = new List<ServerEntry>();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading recent servers: {ex}");
                recentServers = new List<ServerEntry>();
            }
        }

        /// <summary>
        /// Saves the recent servers list to disk.
        /// </summary>
        private void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(recentServers, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                logger.Error($"Error saving recent servers: {ex}");
            }
        }
    }
}
