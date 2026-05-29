using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using HermesDesktop.WinUI.ViewModels;

namespace HermesDesktop.WinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SessionsView : Page
    {
        public SessionsView()
        {
            this.InitializeComponent();
            Loaded += SessionsView_Loaded;
        }

        private async void SessionsView_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadSessionsAsync();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Reload the sessions
            _ = ViewModel.LoadSessionsAsync();
        }

        private async void SessionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is Models.SessionInfo session)
                {
                    await ViewModel.LoadTranscriptAsync(session.Id);
                }
            }
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement resuming the session in the terminal view
            // For now, we just show a message
            var dialog = new ContentDialog
            {
                Title = "Resume Session",
                Content = "This feature is not yet implemented.",
                CloseButtonText = "OK"
            };
            _ = dialog.ShowAsync();
        }

        private void CopyTranscript_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Copy the transcript to clipboard
            var dialog = new ContentDialog
            {
                Title = "Copy Transcript",
                Content = "This feature is not yet implemented.",
                CloseButtonText = "OK"
            };
            _ = dialog.ShowAsync();
        }

        private void ExportTranscript_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Export the transcript to a file
            var dialog = new ContentDialog
            {
                Title = "Export Transcript",
                Content = "This feature is not yet implemented.",
                CloseButtonText = "OK"
            };
            _ = dialog.ShowAsync();
        }
    }
}
