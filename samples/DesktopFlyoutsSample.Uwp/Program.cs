using System.Runtime.InteropServices;
using Windows.System;
using Windows.UI.Xaml.Hosting;

namespace DesktopFlyoutsSample.Uwp;

internal static partial class Program
{
    [STAThread]
    private static void Main()
    {
        Marshal.ThrowExceptionForHR(RoInitialize(0));
        try
        {
            var app = new App();
            using var manager = WindowsXamlManager.InitializeForCurrentThread();
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            using var window = new XamlIslandWindow();
            window.Run();
            GC.KeepAlive(app);
        }
        finally
        {
            RoUninitialize();
        }
    }

    [LibraryImport("combase.dll")]
    private static partial int RoInitialize(uint type);

    [LibraryImport("combase.dll")]
    private static partial void RoUninitialize();
}
