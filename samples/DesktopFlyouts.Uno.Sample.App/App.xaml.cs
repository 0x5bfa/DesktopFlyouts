using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace DesktopFlyouts;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Resolve the icon path relative to the executable.
        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        var iconPath = Path.Combine(exeDir, "Assets", "Tray.ico");

        TrayIconManager.Default.Initialize(new(
            iconPath,
            "DesktopFlyouts sample app (Uno)",
            "28DE460A-8BD6-4539-A406-5F685584FD4D"));

        _window = new Window();
        _window.Content = new RootView();
        _window.Closed += (_, _) => TrayIconManager.Default.Dispose();
        _window.Activate();
    }

    private void AppDomain_ProcessExit(object? sender, EventArgs e)
    {
        TrayIconManager.Default.Dispose();
    }
}
