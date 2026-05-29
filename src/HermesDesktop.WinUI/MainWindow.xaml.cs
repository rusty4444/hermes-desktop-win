using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using HermesDesktop.WinUI.Views;

namespace HermesDesktop.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            // Set the initial content
            ContentFrame.Navigate(typeof(OverviewView));
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(SettingsView));
                return;
            }

            // Get the NavigationViewItem that was invoked
            var invokedItem = args.InvokedItemContainer as NavigationViewItem;

            if (invokedItem != null)
            {
                switch (invokedItem.Tag?.ToString())
                {
                    case "overview":
                        ContentFrame.Navigate(typeof(OverviewView));
                        break;
                    case "sessions":
                        ContentFrame.Navigate(typeof(SessionsView));
                        break;
                    case "workflows":
                        ContentFrame.Navigate(typeof(WorkflowsView));
                        break;
                    case "kanban":
                        ContentFrame.Navigate(typeof(KanbanView));
                        break;
                    case "files":
                        ContentFrame.Navigate(typeof(FilesView));
                        break;
                    case "usage":
                        ContentFrame.Navigate(typeof(UsageView));
                        break;
                    case "skills":
                        ContentFrame.Navigate(typeof(SkillsView));
                        break;
                    case "terminal":
                        ContentFrame.Navigate(typeof(TerminalView));
                        break;
                }
            }
        }

        private void NavView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            // Toggle the title bar visibility when the pane changes
            if (args.DisplayMode == NavigationViewDisplayMode.Minimal)
            {
                // In minimal mode, the pane is hidden and the title bar might overlap
                // We can adjust the margin if needed
            }
            else
            {
                // In expanded or compact mode, revert any changes
            }
        }
    }
}
