using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace DesktopFlyouts_WinUI_IntegrationTests;

public partial class IntegrationTestApp : Application
{
    private IntegrationTestWindow? _window;

    public IntegrationTestApp()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Environment.ExitCode = 1;
        _window = new IntegrationTestWindow();
        _window.Activate();
        _ = RuntimeScenarioRunner.RunAsync(_window, WindowNative.GetWindowHandle(_window));
    }
}
