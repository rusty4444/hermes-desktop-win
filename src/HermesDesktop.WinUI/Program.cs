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
            var options = new MddBootstrapInitializeOptions
            {
                AutoInitialize = false,
                ForcePackageBreakaway = false
            };

            int hr = MddBootstrapInitialize2(0x00010005, null, options, out _);
            if (hr != 0) hr = MddBootstrapInitialize2(0x00010004, null, options, out _);
            if (hr != 0) hr = MddBootstrapInitialize2(0x00010003, null, options, out _);

            Application.Start((p) =>
            {
                new App();
            });
        }
    }
}
