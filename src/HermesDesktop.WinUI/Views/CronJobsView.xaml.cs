using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HermesDesktop.WinUI.Services;

namespace HermesDesktop.WinUI.Views
{
    public sealed partial class CronJobsView : Page
    {
        public CronJobsView()
        {
            this.InitializeComponent();
            Loaded += CronJobsView_Loaded;
        }

        private async void CronJobsView_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadJobsAsync();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.LoadJobsAsync();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string jobId)
                _ = ViewModel.PauseJobAsync(jobId);
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string jobId)
                _ = ViewModel.ResumeJobAsync(jobId);
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string jobId)
                _ = ViewModel.RunNowAsync(jobId);
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string jobId)
            {
                var dialog = new ContentDialog
                {
                    Title = "Delete Cron Job",
                    Content = $"Are you sure you want to delete this cron job?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel"
                };
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                    await ViewModel.RemoveJobAsync(jobId);
            }
        }
    }
}
