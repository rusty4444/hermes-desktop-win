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
    public sealed partial class SettingsView : Page
    {
        public SettingsView()
        {
            this.InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        private async void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadConnectionProfilesAsync();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.AddConnectionProfileAsync();
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.UpdateConnectionProfileAsync();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.RemoveConnectionProfileAsync();
        }

        private void SetAsActiveButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.SetAsActiveConnectionAsync();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NewAlias = string.Empty;
            ViewModel.NewHost = string.Empty;
            ViewModel.NewUser = string.Empty;
            ViewModel.NewPort = null;
            ViewModel.NewHermesProfile = string.Empty;
        }
    }
}
