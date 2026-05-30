using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HermesDesktop.WinUI.Views;

namespace HermesDesktop.WinUI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            ContentFrame.Navigate(typeof(OverviewView));
        }
    }
}
