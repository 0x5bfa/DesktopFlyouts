using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using WinRT.Interop;

namespace DesktopFlyouts.WinUI.IntegrationTests;

public partial class App : Application
{
    private MainWindow? _window;
    public nint TestOwnerWindow => _window is null ? 0 : WindowNative.GetWindowHandle(_window);

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();

        _window = new MainWindow();
        _window.Activate();

        UITestMethodAttribute.DispatcherQueue = _window.DispatcherQueue;

        Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(Environment.CommandLine);
    }
}
