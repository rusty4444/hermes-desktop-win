using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using HermesDesktop.WinUI.Models;
using HermesDesktop.WinUI.Services;

namespace HermesDesktop.WinUI.ViewModels
{
    /// <summary>
    /// View model for the Overview view.
    /// </summary>
    public class OverviewViewModel : INotifyPropertyChanged
    {
        private readonly AppState _appState;
        private string _hostInfo = string.Empty;
        private string _hermesProfileInfo = string.Empty;
        private bool _isConnected = false;
        private string _connectionStatus = string.Empty;
        private ObservableCollection<string> _discoveredProfiles = new ObservableCollection<string>();

        public OverviewViewModel()
        {
            _appState = AppState.Instance;
            // We'll initialize the view model by trying to get the connection info.
            // However, note that the connection might not be set yet.
            // We'll provide a method to update the connection info.
        }

        public string HostInfo
        {
            get => _hostInfo;
            set => SetField(ref _hostInfo, value);
        }

        public string HermesProfileInfo
        {
            get => _hermesProfileInfo;
            set => SetField(ref _hermesProfileInfo, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetField(ref _isConnected, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetField(ref _connectionStatus, value);
        }

        public ObservableCollection<string> DiscoveredProfiles
        {
            get => _discoveredProfiles;
            set => SetField(ref _discoveredProfiles, value);
        }

        /// <summary>
        /// Updates the view model based on the current connection in AppState.
        /// Uses RemoteHermesService for comprehensive workspace discovery.
        /// </summary>
        public async Task UpdateConnectionInfoAsync()
        {
            try
            {
                var connection = _appState.ActiveConnection;
                if (string.IsNullOrWhiteSpace(connection.EffectiveTarget))
                {
                    HostInfo = "No host configured";
                    HermesProfileInfo = string.Empty;
                    IsConnected = false;
                    ConnectionStatus = "Please configure a connection in Settings.";
                    DiscoveredProfiles.Clear();
                    return;
                }

                // Use RemoteHermesService for discovery
                var discovery = await _appState.RemoteHermesService.DiscoverAsync(connection);
                if (discovery.Ok)
                {
                    IsConnected = true;
                    HostInfo = $"Host: {connection.EffectiveTarget}";
                    HermesProfileInfo = connection.HermesProfile != null
                        ? $"Hermes Profile: {connection.HermesProfile}"
                        : "Hermes Profile: (default)";
                    ConnectionStatus = "Connected";

                    if (!string.IsNullOrEmpty(discovery.HermesVersion))
                        ConnectionStatus += $" | Hermes {discovery.HermesVersion}";

                    DiscoveredProfiles.Clear();
                    foreach (var profile in discovery.AvailableProfiles ?? new List<Services.ProfileInfo>())
                    {
                        DiscoveredProfiles.Add(profile.Name + (profile.IsDefault ? " (default)" : ""));
                    }
                }
                else
                {
                    IsConnected = false;
                    HostInfo = $"Host: {connection.EffectiveTarget}";
                    ConnectionStatus = $"Discovery failed: {discovery.Error}";
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                HostInfo = "Error";
                ConnectionStatus = $"Error: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
