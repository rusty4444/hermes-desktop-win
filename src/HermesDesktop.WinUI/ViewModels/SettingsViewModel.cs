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
    /// View model for the Settings view.
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly AppState _appState;
        private readonly ConnectionProfileService _connectionService;
        private ObservableCollection<ConnectionProfile> _connectionProfiles = new ObservableCollection<ConnectionProfile>();
        private ConnectionProfile _selectedProfile = null;
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;
        private string _newAlias = string.Empty;
        private string _newHost = string.Empty;
        private string _newUser = string.Empty;
        private int? _newPort = null;
        private string _newHermesProfile = string.Empty;

        public SettingsViewModel()
        {
            _appState = AppState.Instance;
            _connectionService = new ConnectionProfileService();
        }

        public ObservableCollection<ConnectionProfile> ConnectionProfiles
        {
            get => _connectionProfiles;
            set => SetField(ref _connectionProfiles, value);
        }

        public ConnectionProfile SelectedProfile
        {
            get => _selectedProfile;
            set => SetField(ref _selectedProfile, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetField(ref _errorMessage, value);
        }

        public string NewAlias
        {
            get => _newAlias;
            set => SetField(ref _newAlias, value);
        }

        public string NewHost
        {
            get => _newHost;
            set => SetField(ref _newHost, value);
        }

        public string NewUser
        {
            get => _newUser;
            set => SetField(ref _newUser, value);
        }

        public int? NewPort
        {
            get => _newPort;
            set => SetField(ref _newPort, value);
        }

        public string NewHermesProfile
        {
            get => _newHermesProfile;
            set => SetField(ref _newHermesProfile, value);
        }

        /// <summary>
        /// Loads the connection profiles from the local storage.
        /// </summary>
        public async Task LoadConnectionProfilesAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                var profiles = await _connectionService.GetConnectionProfilesAsync();
                ConnectionProfiles.Clear();
                foreach (var profile in profiles)
                {
                    ConnectionProfiles.Add(profile);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load connection profiles: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Adds a new connection profile.
        /// </summary>
        public async Task AddConnectionProfileAsync()
        {
            if (string.IsNullOrWhiteSpace(NewAlias) && string.IsNullOrWhiteSpace(NewHost))
            {
                ErrorMessage = "Either alias or host must be provided.";
                return;
            }

            var profile = new ConnectionProfile
            {
                SshAlias = NewAlias.Trim(),
                Host = NewHost.Trim(),
                User = NewUser.Trim(),
                Port = NewPort,
                HermesProfile = NewHermesProfile.Trim()
            };

            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                await _connectionService.AddConnectionProfileAsync(profile);
                await LoadConnectionProfilesAsync();
                ClearNewProfileFields();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to add connection profile: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates the selected connection profile.
        /// </summary>
        public async Task UpdateConnectionProfileAsync()
        {
            if (SelectedProfile == null)
            {
                ErrorMessage = "No profile selected.";
                return;
            }

            // Update the selected profile with the new values.
            SelectedProfile.SshAlias = NewAlias.Trim();
            SelectedProfile.Host = NewHost.Trim();
            SelectedProfile.User = NewUser.Trim();
            SelectedProfile.Port = NewPort;
            SelectedProfile.HermesProfile = NewHermesProfile.Trim();

            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                await _connectionService.UpdateConnectionProfileAsync(SelectedProfile);
                await LoadConnectionProfilesAsync();
                ClearNewProfileFields();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to update connection profile: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Removes the selected connection profile.
        /// </summary>
        public async Task RemoveConnectionProfileAsync()
        {
            if (SelectedProfile == null)
            {
                ErrorMessage = "No profile selected.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                await _connectionService.RemoveConnectionProfileAsync(SelectedProfile);
                await LoadConnectionProfilesAsync();
                SelectedProfile = null;
                ClearNewProfileFields();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to remove connection profile: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Sets the selected connection profile as the active connection in the app state.
        /// </summary>
        public async Task SetAsActiveConnectionAsync()
        {
            if (SelectedProfile == null)
            {
                ErrorMessage = "No profile selected.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                _appState.ActiveConnection = SelectedProfile;
                // We could also save the active connection to local storage if desired.
                // For now, we just set it in the app state.
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to set active connection: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Clears the fields for adding a new profile.
        /// </summary>
        private void ClearNewProfileFields()
        {
            NewAlias = string.Empty;
            NewHost = string.Empty;
            NewUser = string.Empty;
            NewPort = null;
            NewHermesProfile = string.Empty;
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
