using System.Drawing;
using Xunit;

namespace DesktopFlyouts.Tests;

public class X11ScreenGeometryTests
{
    private static readonly X11MonitorGeometry[] Monitors =
    [
        new(new Rectangle(-1920, 0, 1920, 1080), false),
        new(new Rectangle(0, 0, 2560, 1440), true),
    ];

    [Fact]
    public void SelectMonitorUsesPrimaryWithoutAnchor()
    {
        Assert.Equal(new Rectangle(0, 0, 2560, 1440),
            X11ScreenGeometry.SelectMonitor(Monitors, null));
    }

    [Fact]
    public void SelectMonitorUsesAnchorIncludingNegativeCoordinates()
    {
        Assert.Equal(new Rectangle(-1920, 0, 1920, 1080),
            X11ScreenGeometry.SelectMonitor(Monitors, new Point(-800, 500)));
    }

    [Fact]
    public void SelectMonitorUsesNearestMonitorForOffscreenAnchor()
    {
        Assert.Equal(new Rectangle(0, 0, 2560, 1440),
            X11ScreenGeometry.SelectMonitor(Monitors, new Point(4000, 500)));
    }

    [Fact]
    public void SelectWorkAreaUsesCurrentVirtualDesktop()
    {
        nuint[] values =
        [
            0, 0, 2560, 1400,
            unchecked((nuint)(uint)-1920), 24, 4480, 1056,
        ];

        Assert.True(X11ScreenGeometry.TrySelectWorkArea(values, 1, out var workArea));
        Assert.Equal(new Rectangle(-1920, 24, 4480, 1056), workArea);
    }

    [Fact]
    public void ApplyWorkAreaClipsSelectedMonitor()
    {
        var monitor = new Rectangle(0, 0, 2560, 1440);
        var workArea = new Rectangle(-1920, 24, 4480, 1380);

        Assert.Equal(new Rectangle(0, 24, 2560, 1380),
            X11ScreenGeometry.ApplyWorkArea(monitor, workArea));
    }
}
