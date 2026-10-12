using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestPlatform.TestExecutor;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRT;

namespace DesktopFlyouts.Uwp.IntegrationTests;

public partial class App : Application
{
    public nint TestOwnerWindow => GetWindowHandle(Window.Current.CoreWindow);

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        // This app only hosts unit tests, so initialize the minimum XAML content.
        Window.Current.Content ??= new Frame();

        UnitTestClient.CreateDefaultUI();
        Window.Current.Activate();
        UnitTestClient.Run(e.Arguments);
    }

    private static unsafe nint GetWindowHandle(Windows.UI.Core.CoreWindow coreWindow)
    {
        using var coreWindowReference = MarshalInspectable<Windows.UI.Core.CoreWindow>.CreateMarshaler(coreWindow);
        var coreWindowAbi = MarshalInspectable<Windows.UI.Core.CoreWindow>.GetAbi(coreWindowReference);
        var interfaceId = new Guid("45D64A29-A63E-4CB6-B498-5781D298CB4F");
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface(coreWindowAbi, in interfaceId, out var interop));

        try
        {
            var vtable = *(nint**)interop;
            var getWindowHandle = (delegate* unmanaged[Stdcall]<nint, nint*, int>)vtable[3];
            nint windowHandle = 0;
            Marshal.ThrowExceptionForHR(getWindowHandle(interop, &windowHandle));
            return windowHandle;
        }
        finally
        {
            Marshal.Release(interop);
        }
    }
}
