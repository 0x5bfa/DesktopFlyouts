using System;
using System.Diagnostics;
using Windows.Foundation;

namespace DesktopFlyouts;

internal partial class TrayIconManager : IDisposable
{
    private static readonly Lazy<TrayIconManager> _default = new(() => new TrayIconManager());
    internal static TrayIconManager Default => _default.Value;

    internal SystemTrayIcon? SystemTrayIcon { get; set; }
    internal DesktopFlyout? DesktopFlyout { get; set; }
    internal DesktopMenuFlyout? DesktopMenuFlyout { get; set; }
    internal DesktopFlyoutSampleKind SelectedFlyoutExample { get; private set; }

    private bool _disposed;

    private TrayIconManager() { }

    internal void Initialize(SystemTrayIcon trayIcon)
    {
        DesktopFlyout = CreateFlyout(SelectedFlyoutExample);
        DesktopMenuFlyout = new MainDesktopMenuFlyout();

        SystemTrayIcon = trayIcon;
        SystemTrayIcon.Show();
        SystemTrayIcon.LeftClicked += SystemTrayIcon_LeftClicked;
        SystemTrayIcon.RightClicked += SystemTrayIcon_RightClicked;
    }

    internal void SwitchFlyout(DesktopFlyoutSampleKind example)
    {
        if (_disposed || (DesktopFlyout is not null && SelectedFlyoutExample == example))
            return;

        var oldFlyout = DesktopFlyout;
        var newFlyout = CreateFlyout(example);

        DesktopFlyout = newFlyout;
        SelectedFlyoutExample = example;
        oldFlyout?.Dispose();
    }

    private static DesktopFlyout CreateFlyout(DesktopFlyoutSampleKind example)
    {
        return example switch
        {
            DesktopFlyoutSampleKind.Button => new ButtonFlyout(),
            DesktopFlyoutSampleKind.IndicatorStyle => new IndicatorStyleFlyout(),
            DesktopFlyoutSampleKind.NotificationCenterStyle => new NotificationCenterStyleFlyout(),
            DesktopFlyoutSampleKind.StartMenuStyle => new StartMenuStyleFlyout(),
            DesktopFlyoutSampleKind.StickySmallStyle => new StickySmallFlyout(),
            DesktopFlyoutSampleKind.WidgetStyle => new WidgetStyleFlyout(),
            DesktopFlyoutSampleKind.Severity => new SeverityFlyout(),
            _ => new CustomizableFlyout(),
        };
    }

    internal void ToggleFlyout(Point? point = null)
    {
        if (DesktopFlyout is null)
            return;

        if (DesktopFlyout.IsOpen)
        {
            DesktopFlyout.Hide();
        }
        else
        {
            if (DesktopFlyout is StickySmallFlyout && point is not null)
            {
                DesktopFlyout.Show(new((int)point.Value.X, (int)point.Value.Y));
            }
            else
            {
                DesktopFlyout.Show();
            }
        }
    }

    private void SystemTrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
    {
        ToggleFlyout(new((int)e.Point.X, (int)e.Point.Y));
    }

    private void SystemTrayIcon_RightClicked(object? sender, MouseEventReceivedEventArgs e)
    {
        if (DesktopMenuFlyout is null)
            return;

        if (DesktopMenuFlyout.IsOpen)
            DesktopMenuFlyout.Hide();

        DesktopMenuFlyout.Show(new(e.Point.X, e.Point.Y - 32));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        SystemTrayIcon?.LeftClicked -= SystemTrayIcon_LeftClicked;
        SystemTrayIcon?.RightClicked -= SystemTrayIcon_RightClicked;
        SystemTrayIcon?.Dispose();
        DesktopFlyout?.Dispose();
        DesktopMenuFlyout?.Dispose();

        SystemTrayIcon = null;
        DesktopFlyout = null;
        DesktopMenuFlyout = null;

        GC.SuppressFinalize(this);
    }
}
