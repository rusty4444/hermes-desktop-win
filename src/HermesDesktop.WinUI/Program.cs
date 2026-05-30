using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;

namespace HermesDesktop.WinUI
{
    static class Program
    {
        [DllImport("Microsoft.WindowsAppRuntime.Bootstrap.dll")]
        private static extern int MddBootstrapInitialize2(
            uint majorMinorVersion,
            [MarshalAs(UnmanagedType.LPWStr)] string versionTag,
            MddBootstrapInitializeOptions options,
            out long packageVersion);

        [StructLayout(LayoutKind.Sequential)]
        private struct MddBootstrapInitializeOptions
        {
            public IntPtr OnInitializationComplete;
            public IntPtr OnError;
            public bool AutoInitialize;
            public bool ForcePackageBreakaway;
        }

        [STAThread]
        static void Main(string[] args)
        {
            // Initialize Windows App Runtime from bundled DLLs
            var options = new MddBootstrapInitializeOptions
            {
                AutoInitialize = false,
                ForcePackageBreakaway = false
            };

            int hr = MddBootstrapInitialize2(0x00010005, null, options, out _);
            if (hr != 0)
                hr = MddBootstrapInitialize2(0x00010004, null, options, out _);
            if (hr != 0)
                hr = MddBootstrapInitialize2(0x00010003, null, options, out _);

            if (hr != 0 && hr != -2003309301) // -2003309301 = already initialized
            {
                System.Diagnostics.Debug.WriteLine($"Bootstrap failed: 0x{hr:X8}");
            }

            WinRT.ComWrappersSupport.InitializeComWrappers();
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
