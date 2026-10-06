using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

namespace DesktopFlyoutsSample.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        Title = "DesktopFlyoutsSample.WinUI";
        SystemBackdrop = new MicaBackdrop();
        var page = new SamplePage();
        Content = page;
        var handle = WindowNative.GetWindowHandle(this);
        var scale = GetDpiForWindow(handle) / 96.0;
        AppWindow.Resize(new SizeInt32((int)(760 * scale), (int)(820 * scale)));
        Closed += (_, _) => page.Dispose();
        // MenuFlyout must finish dispatching its Click event before we tear down its host.
        page.Initialize(handle, () => DispatcherQueue.TryEnqueue(Close));
    }

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint window);
}
