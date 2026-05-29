using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HermesDesktop.WinUI.Views
{
    public sealed partial class OverviewView : Page
    {
        public OverviewView()
        {
            this.InitializeComponent();
            Loaded += OverviewView_Loaded;
        }

        private async void OverviewView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = new ViewModels.OverviewViewModel();
            await ((ViewModels.OverviewViewModel)DataContext).UpdateConnectionInfoAsync();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.OverviewViewModel vm)
                _ = vm.UpdateConnectionInfoAsync();
        }
    }
}
