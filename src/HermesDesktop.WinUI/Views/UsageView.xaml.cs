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
    public sealed partial class UsageView : Page
    {
        public UsageView()
        {
            this.InitializeComponent();
            Loaded += UsageView_Loaded;
        }

        private async void UsageView_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadUsageStatsAsync();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.LoadUsageStatsAsync();
        }
    }
}
