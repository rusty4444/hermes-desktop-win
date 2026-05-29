using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using HermesDesktop.WinUI.ViewModels;

namespace HermesDesktop.WinUI.Views
{
    public sealed partial class ChatView : Page
    {
        public ChatView()
        {
            this.InitializeComponent();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SendMessageAsync();
            // Scroll to bottom
            MessageScroller.ScrollToVerticalOffset(MessageScroller.ScrollableHeight);
        }

        private async void InputBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                await ViewModel.SendMessageAsync();
                MessageScroller.ScrollToVerticalOffset(MessageScroller.ScrollableHeight);
            }
        }

        private void NewSession_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NewSession();
            SessionLabel.Text = "(new session)";
        }

        private async void ResumeSession_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Resume Session",
                Content = new TextBox
                {
                    PlaceholderText = "Enter session ID to resume",
                    Name = "SessionIdInput"
                },
                PrimaryButtonText = "Resume",
                CloseButtonText = "Cancel"
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var textBox = dialog.Content as TextBox;
                var sessionId = textBox?.Text?.Trim();
                if (!string.IsNullOrEmpty(sessionId))
                {
                    ViewModel.SessionId = sessionId;
                    SessionLabel.Text = $"Session: {sessionId}";
                }
            }
        }
    }
}
