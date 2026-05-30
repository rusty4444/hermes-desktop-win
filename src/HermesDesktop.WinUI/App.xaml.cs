using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace HermesDesktop.WinUI
{
    public partial class App : Application
    {
        public App() { }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                var window = new Window();
                window.Title = "Hermes Desktop";

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var listBox = new ListBox();
                Grid.SetColumn(listBox, 0);
                listBox.Items.Add(new ListBoxItem { Content = "Overview" });
                listBox.Items.Add(new ListBoxItem { Content = "Chat" });

                var frame = new Frame();
                Grid.SetColumn(frame, 1);
                frame.Navigate(typeof(Views.OverviewView));

                grid.Children.Add(listBox);
                grid.Children.Add(frame);
                window.Content = grid;

                window.Activate();
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\hermes_error.txt",
                    ex.ToString());
            }
        }
    }
}
