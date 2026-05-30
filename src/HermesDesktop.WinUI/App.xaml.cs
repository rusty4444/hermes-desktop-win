using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;

namespace HermesDesktop.WinUI
{
    public partial class App : Application
    {
        private static string LogPath => 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "hermes_debug.txt");

        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += (s, e) =>
            {
                File.AppendAllText(LogPath, $"UNHANDLED: {e.Exception}\n");
                e.Handled = true;
            };
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                File.WriteAllText(LogPath, $"OnLaunched at {DateTime.Now}\n");

                var window = new Window();
                window.Title = "Hermes Desktop";
                window.Closed += (s, e) => File.AppendAllText(LogPath, "Window closed\n");

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var listBox = new ListBox();
                Grid.SetColumn(listBox, 0);
                listBox.Items.Add(new ListBoxItem { Content = "Overview" });
                listBox.Items.Add(new ListBoxItem { Content = "Chat" });

                var frame = new Frame();
                Grid.SetColumn(frame, 1);

                File.AppendAllText(LogPath, "Navigating to OverviewView...\n");
                frame.Navigate(typeof(Views.OverviewView));
                File.AppendAllText(LogPath, "Navigation OK\n");

                grid.Children.Add(listBox);
                grid.Children.Add(frame);
                window.Content = grid;

                File.AppendAllText(LogPath, "Activating window...\n");
                window.Activate();
                File.AppendAllText(LogPath, "Window activated\n");
            }
            catch (Exception ex)
            {
                File.WriteAllText(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "hermes_error.txt"),
                    $"OnLaunched ERROR: {ex}");
            }
        }
    }
}
