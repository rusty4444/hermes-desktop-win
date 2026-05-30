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
            NavList.SelectedIndex = 0;
            ContentFrame.Navigate(typeof(OverviewView));
        }

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavList.SelectedItem is ListBoxItem item && item.Tag != null)
            {
                switch (item.Tag.ToString())
                {
                    case "overview":
                        ContentFrame.Navigate(typeof(OverviewView));
                        break;
                    case "chat":
                        ContentFrame.Navigate(typeof(ChatView));
                        break;
                    case "sessions":
                        ContentFrame.Navigate(typeof(SessionsView));
                        break;
                    case "files":
                        ContentFrame.Navigate(typeof(FilesView));
                        break;
                    case "settings":
                        ContentFrame.Navigate(typeof(SettingsView));
                        break;
                }
            }
        }
    }
}
