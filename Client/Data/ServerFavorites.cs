using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;

namespace DedicatedServerMod.Client.Data
{
    [Serializable]
    public class ServerFavoriteEntry
    {
        [JsonProperty("ip")]
        public string IP { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("lastConnected")]
        public DateTime LastConnected { get; set; }

        [JsonProperty("serverName")]
        public string ServerName { get; set; }

        [JsonIgnore]
        public string DisplayText => string.IsNullOrEmpty(ServerName) ? $"{IP}:{Port}" : $"{ServerName} ({IP}:{Port})";
    }

    /// <summary>
    /// Manages the list of favorite servers
    /// </summary>
    public static class ServerFavorites
    {
        private const int MAX_FAVORITES_SIZE = 100;
        private const string FAVORITES_FILE_NAME = "server_favorites.json";
        private static readonly string FavoritesFilePath = Path.Combine(MelonEnvironment.UserDataDirectory, FAVORITES_FILE_NAME);

        private static List<ServerFavoriteEntry> _favorites;
        private static MelonLogger.Instance _logger;

        public static void Initialize(MelonLogger.Instance logger)
        {
            _logger = logger;
            LoadFavorites();
        }

        public static List<ServerFavoriteEntry> GetFavorites()
        {
            if (_favorites == null)
            {
                LoadFavorites();
            }
            return new List<ServerFavoriteEntry>(_favorites ?? new List<ServerFavoriteEntry>());
        }

        public static void AddFavorite(string ip, int port, string serverName = null)
        {
            if (_favorites == null)
            {
                LoadFavorites();
            }

            var existing = _favorites.FirstOrDefault(h => h.IP == ip && h.Port == port);
            if (existing != null)
            {
                existing.LastConnected = DateTime.Now;
                if (!string.IsNullOrEmpty(serverName))
                {
                    existing.ServerName = serverName;
                }
            }
            else
            {
                _favorites.Add(new ServerFavoriteEntry
                {
                    IP = ip,
                    Port = port,
                    ServerName = serverName,
                    LastConnected = DateTime.Now
                });

                while (_favorites.Count > MAX_FAVORITES_SIZE)
                {
                    _favorites.RemoveAt(0);
                }
            }

            SaveFavorites();
        }

        public static void RemoveFavorite(string ip, int port)
        {
            if (_favorites == null)
            {
                LoadFavorites();
            }

            var existing = _favorites.FirstOrDefault(h => h.IP == ip && h.Port == port);
            if (existing != null)
            {
                _favorites.Remove(existing);
                SaveFavorites();
            }
        }

        public static void ClearFavorites()
        {
            _favorites?.Clear();
            SaveFavorites();
        }

        private static void LoadFavorites()
        {
            try
            {
                if (File.Exists(FavoritesFilePath))
                {
                    var json = File.ReadAllText(FavoritesFilePath);
                    _favorites = JsonConvert.DeserializeObject<List<ServerFavoriteEntry>>(json) ?? new List<ServerFavoriteEntry>();
                    _logger?.Msg($"Loaded {_favorites.Count} favorites from {FAVORITES_FILE_NAME}");
                }
                else
                {
                    _favorites = new List<ServerFavoriteEntry>();
                    _logger?.Msg($"No favorites file found, starting with empty list");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to load favorites: {ex}");
                _favorites = new List<ServerFavoriteEntry>();
            }
        }

        private static void SaveFavorites()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_favorites, Formatting.Indented);
                File.WriteAllText(FavoritesFilePath, json);
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to save favorites: {ex}");
            }
        }
    }
}
