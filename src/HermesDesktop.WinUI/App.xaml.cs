using Microsoft.UI.Xaml;
using System;
using System.IO;

namespace HermesDesktop.WinUI
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "hermes_debug.txt"),
                    $"OnLaunched fired at {DateTime.Now}\n");

                m_window = new MainWindow();
                File.AppendAllText(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "hermes_debug.txt"),
                    "MainWindow created\n");

                m_window.Activate();
                File.AppendAllText(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "hermes_debug.txt"),
                    "Window activated\n");
            }
            catch (Exception ex)
            {
                File.WriteAllText(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "hermes_error.txt"),
                    ex.ToString());
            }
        }

        private MainWindow m_window;
    }
}
