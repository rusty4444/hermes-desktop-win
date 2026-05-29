using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;

namespace HermesDesktop.WinUI.Services
{
    /// <summary>
    /// Service for managing connection profiles (stored locally).
    /// </summary>
    public class ConnectionProfileService
    {
        private readonly string _connectionsFilePath;

        public ConnectionProfileService()
        {
            // Store connections in the local application data folder.
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDataPath = Path.Combine(localAppData, "HermesDesktop.WinUI");
            Directory.CreateDirectory(appDataPath);
            _connectionsFilePath = Path.Combine(appDataPath, "connections.json");
        }

        /// <summary>
        /// Gets all connection profiles.
        /// </summary>
        public async Task<ObservableCollection<ConnectionProfile>> GetConnectionProfilesAsync()
        {
            if (!File.Exists(_connectionsFilePath))
            {
                return new ObservableCollection<ConnectionProfile>();
            }

            var json = await File.ReadAllTextAsync(_connectionsFilePath);
            var list = JsonSerializer.Deserialize<List<ConnectionProfile>>(json) ?? new List<ConnectionProfile>();
            return new ObservableCollection<ConnectionProfile>(list);
        }

        /// <summary>
        /// Saves the list of connection profiles.
        /// </summary>
        public async Task SaveConnectionProfilesAsync(ObservableCollection<ConnectionProfile> profiles)
        {
            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_connectionsFilePath, json);
        }

        /// <summary>
        /// Adds a connection profile.
        /// </summary>
        public async Task AddConnectionProfileAsync(ConnectionProfile profile)
        {
            var profiles = await GetConnectionProfilesAsync();
            // If a profile with the same alias or host already exists, we replace it? Or we can allow duplicates.
            // For simplicity, we'll just add it.
            profiles.Add(profile);
            await SaveConnectionProfilesAsync(profiles);
        }

        /// <summary>
        /// Updates a connection profile.
        /// </summary>
        public async Task UpdateConnectionProfileAsync(ConnectionProfile profile)
        {
            var profiles = await GetConnectionProfilesAsync();
            var existing = profiles.FirstOrDefault(p => 
                (p.SshAlias == profile.SshAlias && !string.IsNullOrEmpty(p.SshAlias)) ||
                (p.Host == profile.Host && p.User == profile.User && p.Port == profile.Port));
            if (existing != null)
            {
                profiles.Remove(existing);
            }
            profiles.Add(profile);
            await SaveConnectionProfilesAsync(profiles);
        }

        /// <summary>
        /// Removes a connection profile by alias or host/user/port.
        /// </summary>
        public async Task RemoveConnectionProfileAsync(ConnectionProfile profile)
        {
            var profiles = await GetConnectionProfilesAsync();
            var toRemove = profiles.FirstOrDefault(p => 
                (p.SshAlias == profile.SshAlias && !string.IsNullOrEmpty(p.SshAlias)) ||
                (p.Host == profile.Host && p.User == profile.User && p.Port == profile.Port));
            if (toRemove != null)
            {
                profiles.Remove(toRemove);
                await SaveConnectionProfilesAsync(profiles);
            }
        }
    }
}
