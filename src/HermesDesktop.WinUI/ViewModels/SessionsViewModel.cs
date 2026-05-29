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
    /// View model for the Sessions view.
    /// </summary>
    public class SessionsViewModel : INotifyPropertyChanged
    {
        private readonly AppState _appState;
        private ObservableCollection<SessionInfo> _sessions = new ObservableCollection<SessionInfo>();
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;
        private SessionTranscript _selectedTranscript = null;
        private bool _isTranscriptLoading = false;

        public SessionsViewModel()
        {
            _appState = AppState.Instance;
        }

        public ObservableCollection<SessionInfo> Sessions
        {
            get => _sessions;
            set => SetField(ref _sessions, value);
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

        public SessionTranscript SelectedTranscript
        {
            get => _selectedTranscript;
            set => SetField(ref _selectedTranscript, value);
        }

        public bool IsTranscriptLoading
        {
            get => _isTranscriptLoading;
            set => SetField(ref _isTranscriptLoading, value);
        }

        /// <summary>
        /// Loads the list of sessions from the remote host.
        /// </summary>
        public async Task LoadSessionsAsync(int limit = 50, int offset = 0)
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
                var sessions = await _appState.SessionBrowserService.GetSessionsAsync(limit, offset);
                Sessions.Clear();
                foreach (var session in sessions)
                {
                    Sessions.Add(session);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load sessions: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Loads the transcript for the selected session.
        /// </summary>
        public async Task LoadTranscriptAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                SelectedTranscript = null;
                return;
            }

            IsTranscriptLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                var transcript = await _appState.SessionBrowserService.GetSessionTranscriptAsync(sessionId);
                SelectedTranscript = transcript;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load transcript: {ex.Message}";
                SelectedTranscript = null;
            }
            finally
            {
                IsTranscriptLoading = false;
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
