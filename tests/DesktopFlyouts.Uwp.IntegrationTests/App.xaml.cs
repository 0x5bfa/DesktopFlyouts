using Microsoft.VisualStudio.TestPlatform.TestExecutor;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRT.Interop;

namespace DesktopFlyouts.Uwp.IntegrationTests;

public partial class App : Application
{
    public nint TestOwnerWindow => WindowNative.GetWindowHandle(Window.Current);

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
}
