using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;

namespace HermesDesktop.WinUI
{
    public partial class App : Application
    {
        [DllImport("Microsoft.WindowsAppRuntime.Bootstrap.dll", CharSet = CharSet.Unicode)]
        private static extern int MddBootstrapInitialize(uint majorMinorVersion, string versionTag, IntPtr minVersion);

        static App()
        {
            // Initialize Windows App Runtime bootstrapper for self-contained deployment
            int hr = MddBootstrapInitialize(0x00010005, null, IntPtr.Zero);
            if (hr != 0) hr = MddBootstrapInitialize(0x00010004, null, IntPtr.Zero);
            if (hr != 0) hr = MddBootstrapInitialize(0x00010003, null, IntPtr.Zero);
        }

        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        private MainWindow m_window;
    }
}
