using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

                // Test the connection by running a simple command.
                var result = await _appState.SshTransport.ExecuteAsync("echo 'connected'");
                if (result.ExitCode == 0)
                {
                    IsConnected = true;
                    HostInfo = $"Host: {connection.EffectiveTarget}";
                    if (!string.IsNullOrWhiteSpace(connection.HermesProfile))
                    {
                        HermesProfileInfo = $"Hermes Profile: {connection.HermesProfile}";
                    }
                    else
                    {
                        HermesProfileInfo = "Hermes Profile: (default)";
                    }
                    ConnectionStatus = "Connected";

                    // Discover available Hermes profiles on the host.
                    await DiscoverProfilesAsync();
                }
                else
                {
                    IsConnected = false;
                    HostInfo = $"Host: {connection.EffectiveTarget}";
                    HermesProfileInfo = string.Empty;
                    ConnectionStatus = $"Connection failed: {result.StandardError}";
                    DiscoveredProfiles.Clear();
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                HostInfo = "Error";
                HermesProfileInfo = string.Empty;
                ConnectionStatus = $"Error: {ex.Message}";
                DiscoveredProfiles.Clear();
            }
        }

        private async Task DiscoverProfilesAsync()
        {
            try
            {
                // We'll run a command to list the profiles in the ~/.hermes directory.
                // The command: ls -1 ~/.hermes/profiles/ 2>/dev/null || echo "default"
                // We'll use the SSH transport to run the command and parse the output.
                var result = await _appState.SshTransport.ExecuteAsync(
                    "ls -1 ~/.hermes/profiles/ 2>/dev/null || echo 'default'");
                if (result.ExitCode == 0)
                {
                    var profiles = result.StandardOutput.Split(
                        new[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries);
                    DiscoveredProfiles.Clear();
                    foreach (var profile in profiles)
                    {
                        if (!string.IsNullOrWhiteSpace(profile))
                        {
                            DiscoveredProfiles.Add(profile.Trim());
                        }
                    }
                }
                else
                {
                    // If we can't list the profiles, we'll just show the default.
                    DiscoveredProfiles.Clear();
                    DiscoveredProfiles.Add("default");
                }
            }
            catch
            {
                DiscoveredProfiles.Clear();
                DiscoveredProfiles.Add("default");
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
