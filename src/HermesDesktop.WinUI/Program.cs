using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using System.Runtime.InteropServices;

namespace HermesDesktop.WinUI
{
    public static class Program
    {
        [DllImport("Microsoft.WindowsAppRuntime.Bootstrap.dll", CharSet = CharSet.Unicode)]
        private static extern int MddBootstrapInitialize(uint majorMinorVersion, 
            string versionTag, 
            PackageVersion minVersion);

        [StructLayout(LayoutKind.Sequential)]
        private struct PackageVersion
        {
            public ushort Major;
            public ushort Minor;
            public ushort Build;
            public ushort Revision;
        }

        [STAThread]
        static void Main(string[] args)
        {
            // Initialize Windows App Runtime bootstrapper for self-contained deployment
            // Try version 1.5 first, fall back to 1.4, then 1.3
            int hr = 0;

            // Try Microsoft.WindowsAppRuntime 1.5
            hr = MddBootstrapInitialize(0x00010005, "prerelease", default);
            if (hr != 0)
                hr = MddBootstrapInitialize(0x00010005, null, default);

            // Fall back to 1.4
            if (hr != 0)
            {
                hr = MddBootstrapInitialize(0x00010004, "prerelease", default);
                if (hr != 0)
                    hr = MddBootstrapInitialize(0x00010004, null, default);
            }

            // Fall back to 1.3
            if (hr != 0)
            {
                hr = MddBootstrapInitialize(0x00010003, "prerelease", default);
                if (hr != 0)
                    hr = MddBootstrapInitialize(0x00010003, null, default);
            }

            // Initialize WinRT COM wrappers
            WinRT.ComWrappersSupport.InitializeComWrappers();

            // Start the XAML application
            Application.Start((p) =>
            {
                var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
    }
}
