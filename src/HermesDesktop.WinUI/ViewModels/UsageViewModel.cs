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
    /// View model for the Usage view.
    /// </summary>
    public class UsageViewModel : INotifyPropertyChanged
    {
        private readonly AppState _appState;
        private UsageStats _stats = new UsageStats();
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;

        public UsageViewModel()
        {
            _appState = AppState.Instance;
        }

        public UsageStats Stats
        {
            get => _stats;
            set => SetField(ref _stats, value);
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

        /// <summary>
        /// Loads the usage statistics from the remote host.
        /// </summary>
        public async Task LoadUsageStatsAsync()
        {
            if (_appState.ActiveConnection == null || string.IsNullOrWhiteSpace(_appState.ActiveConnection.EffectiveTarget))
            {
                ErrorMessage = "No connection configured.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                var stats = await _appState.UsageService.GetUsageStatsAsync();
                Stats = stats;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load usage stats: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
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
