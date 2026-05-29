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
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly AppState _appState;
        private ObservableCollection<ChatMessage> _messages = new ObservableCollection<ChatMessage>();
        private string _inputText = string.Empty;
        private string _sessionId = null;
        private bool _isSending = false;
        private string _errorMessage = string.Empty;
        private bool _autoApprove = false;

        public ChatViewModel()
        {
            _appState = AppState.Instance;
            Messages.Add(new ChatMessage
            {
                Role = "system",
                Content = "Welcome to Hermes Chat. Type a prompt and press Send to start a conversation with the remote Hermes agent.",
                Timestamp = DateTime.Now
            });
        }

        public ObservableCollection<ChatMessage> Messages { get => _messages; set => SetField(ref _messages, value); }
        public string InputText { get => _inputText; set => SetField(ref _inputText, value); }
        public string SessionId { get => _sessionId; set => SetField(ref _sessionId, value); }
        public bool IsSending { get => _isSending; set => SetField(ref _isSending, value); }
        public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }
        public bool AutoApprove { get => _autoApprove; set => SetField(ref _autoApprove, value); }

        public bool HasActiveSession => !string.IsNullOrEmpty(SessionId);

        public async Task SendMessageAsync()
        {
            var text = InputText?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (_appState.ActiveConnection == null || string.IsNullOrWhiteSpace(_appState.ActiveConnection.EffectiveTarget))
            {
                ErrorMessage = "No connection configured. Go to Settings first.";
                return;
            }

            // Add user message
            Messages.Add(new ChatMessage { Role = "user", Content = text, Timestamp = DateTime.Now });
            InputText = string.Empty;
            IsSending = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _appState.HermesChatService.SendMessageAsync(
                    prompt: text,
                    sessionId: SessionId,
                    autoApproveCommands: AutoApprove,
                    timeoutSeconds: 300);

                if (!string.IsNullOrEmpty(result.Error))
                {
                    Messages.Add(new ChatMessage { Role = "error", Content = result.Error, Timestamp = DateTime.Now });
                    return;
                }

                // Track session
                if (!string.IsNullOrEmpty(result.SessionId))
                    SessionId = result.SessionId;

                // Add agent response
                var output = result.Output ?? result.Stdout ?? "(no output)";
                if (result.TimedOut)
                    output = "[Timed out]\n" + output;

                Messages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = output,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage { Role = "error", Content = $"Error: {ex.Message}", Timestamp = DateTime.Now });
            }
            finally
            {
                IsSending = false;
            }
        }

        public void NewSession()
        {
            SessionId = null;
            Messages.Clear();
            Messages.Add(new ChatMessage
            {
                Role = "system",
                Content = "New session started. What would you like to do?",
                Timestamp = DateTime.Now
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        protected bool SetField<T>(ref T f, T v, [CallerMemberName] string n = null) { if (EqualityComparer<T>.Default.Equals(f, v)) return false; f = v; OnPropertyChanged(n); return true; }
    }

    public class ChatMessage
    {
        public string Role { get; set; }  // "user", "assistant", "system", "error"
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }

        public string TimeDisplay => Timestamp.ToString("HH:mm:ss");
    }
}
