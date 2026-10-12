extern alias UwpProjection;

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using Windows.UI.Xaml;
using DesktopFlyout = UwpProjection::DesktopFlyouts.DesktopFlyout;
using DesktopFlyoutIsland = UwpProjection::DesktopFlyouts.DesktopFlyoutIsland;
using DesktopFlyoutPlacementMode = UwpProjection::DesktopFlyouts.DesktopFlyoutPlacementMode;
using DesktopFlyoutPopupDirection = UwpProjection::DesktopFlyouts.DesktopFlyoutPopupDirection;
using FlyoutBorder = Windows.UI.Xaml.Controls.Border;
using FlyoutGridLength = Windows.UI.Xaml.GridLength;
using FlyoutGridUnitType = Windows.UI.Xaml.GridUnitType;
using FlyoutOrientation = Windows.UI.Xaml.Controls.Orientation;

namespace DesktopFlyouts.Uwp.IntegrationTests;

[TestClass]
public sealed partial class UnitTests
{
    [UITestMethod]
    public async Task ShowHidePlacementDirectionAndOrientationWorkAtRuntime()
    {
        var app = (App)Application.Current;
        await RunBasicChecks(app.TestOwnerWindow);
    }

    private const int MonitorDefaultToNearest = 2;
    private const int PositionTolerancePixels = 4;
    private static async Task RunBasicChecks(nint ownerWindow)
    {
        var flyout = new DesktopFlyout
        {
            OwnerWindowHandle = ownerWindow,
            FlyoutWidth = new FlyoutGridLength(280, FlyoutGridUnitType.Pixel),
            FlyoutHeight = new FlyoutGridLength(180, FlyoutGridUnitType.Pixel),
            IsTransitionAnimationEnabled = false,
            Content = new FlyoutBorder { Width = 180, Height = 100 },
        };

        try
        {
            Ensure(!flyout.IsOpen, "A new flyout must start closed.");
            flyout.Show();
            Ensure(flyout.IsOpen && IsFlyoutWindowVisible(), "Show must display the flyout window.");
            flyout.Hide();
            Ensure(!flyout.IsOpen && !IsFlyoutWindowVisible(), "Hide must close and hide the flyout window.");

            CheckPlacements(flyout, ownerWindow);
            CheckPopupDirections(flyout);
            await CheckIslandsOrientations(flyout);
        }
        finally
        {
            flyout.Hide();
        }
    }

    private static void CheckPlacements(DesktopFlyout flyout, nint ownerWindow)
    {
        var workArea = GetWorkArea(ownerWindow);
        var margins = flyout.Margin;
        var marginDip = Math.Max(Math.Max(margins.Left, margins.Right), Math.Max(margins.Top, margins.Bottom));
        var margin = (int)Math.Ceiling(marginDip * Math.Max(1.0, GetDpiForWindow(ownerWindow) / 96.0));

        foreach (var placement in Enum.GetValues<DesktopFlyoutPlacementMode>())
        {
            flyout.Placement = placement;
            flyout.Show();
            Ensure(flyout.IsOpen, $"Placement {placement} did not open the flyout.");

            var bounds = GetFlyoutBounds();
            Ensure(bounds.Left >= workArea.Left && bounds.Top >= workArea.Top &&
                bounds.Right <= workArea.Right && bounds.Bottom <= workArea.Bottom,
                $"Placement {placement} put the flyout outside the monitor work area.");
            var horizontalError = (int)placement switch
            {
                0 or 3 => Math.Abs((bounds.Left + bounds.Right) / 2 - (workArea.Left + workArea.Right) / 2),
                1 or 4 or 6 => Math.Abs(bounds.Left - (workArea.Left + margin)),
                2 or 5 or 7 => Math.Abs(bounds.Right - (workArea.Right - margin)),
                _ => int.MaxValue,
            };
            var verticalError = (int)placement switch
            {
                0 or 1 or 2 => Math.Abs(bounds.Top - (workArea.Top + margin)),
                3 or 4 or 5 => Math.Abs(bounds.Bottom - (workArea.Bottom - margin)),
                6 or 7 => Math.Abs((bounds.Top + bounds.Bottom) / 2 - (workArea.Top + workArea.Bottom) / 2),
                _ => int.MaxValue,
            };
            Ensure(horizontalError <= PositionTolerancePixels && verticalError <= PositionTolerancePixels,
                $"Placement {placement} was unexpected: bounds {bounds}, work area {workArea}.");

            flyout.Hide();
            Ensure(!flyout.IsOpen, $"Hide did not close the flyout after placement {placement}.");
        }
    }

    private static void CheckPopupDirections(DesktopFlyout flyout)
    {
        flyout.Placement = (DesktopFlyoutPlacementMode)5;
        foreach (var direction in Enum.GetValues<DesktopFlyoutPopupDirection>())
        {
            flyout.PopupDirection = direction;
            flyout.Show();
            Ensure(flyout.IsOpen, $"Popup direction {direction} did not open the flyout.");
            Ensure(flyout.PopupDirection == direction, $"Popup direction {direction} was not retained.");
            flyout.Hide();
        }
    }

    private static async Task CheckIslandsOrientations(DesktopFlyout flyout)
    {
        flyout.Content = null;
        flyout.Islands.Clear();
        var first = new DesktopFlyoutIsland
        {
            IslandWidth = new FlyoutGridLength(140, FlyoutGridUnitType.Pixel),
            IslandHeight = new FlyoutGridLength(48, FlyoutGridUnitType.Pixel),
            Content = new FlyoutBorder { Width = 140, Height = 48 },
        };
        var second = new DesktopFlyoutIsland
        {
            IslandWidth = new FlyoutGridLength(140, FlyoutGridUnitType.Pixel),
            IslandHeight = new FlyoutGridLength(48, FlyoutGridUnitType.Pixel),
            Content = new FlyoutBorder { Width = 140, Height = 48 },
        };
        flyout.Islands.Add(first);
        flyout.Islands.Add(second);

        foreach (var orientation in new[] { FlyoutOrientation.Vertical, FlyoutOrientation.Horizontal })
        {
            flyout.IslandsOrientation = orientation;
            flyout.Show();
            await Task.Delay(100);
            Ensure(flyout.IsOpen && flyout.IslandsOrientation == orientation,
                $"Islands orientation {orientation} was not applied.");

            var firstOrigin = first.TransformToVisual(flyout).TransformPoint(new Windows.Foundation.Point(0, 0));
            var secondOrigin = second.TransformToVisual(flyout).TransformPoint(new Windows.Foundation.Point(0, 0));
            Ensure(orientation == FlyoutOrientation.Vertical
                    ? secondOrigin.Y > firstOrigin.Y
                    : secondOrigin.X > firstOrigin.X,
                $"Islands were not arranged with {orientation} orientation.");
            flyout.Hide();
        }
    }

    private static bool IsFlyoutWindowVisible()
    {
        var window = FindFlyoutWindow();
        return window != 0 && IsWindowVisible(window) != 0;
    }

    private static RECT GetFlyoutBounds()
    {
        var window = FindFlyoutWindow();
        if (window == 0 || GetWindowRect(window, out var bounds) == 0)
        {
            throw new InvalidOperationException("The native UWP flyout window was not found.");
        }

        return bounds;
    }

    private static nint FindFlyoutWindow()
    {
        var window = FindWindowW(null, "DesktopFlyout.Uwp");
        return window != 0 ? window : FindWindowW(null, "DesktopFlyout");
    }

    private static RECT GetWorkArea(nint ownerWindow)
    {
        var monitor = MonitorFromWindow(ownerWindow, MonitorDefaultToNearest);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        Ensure(monitor != 0 && GetMonitorInfoW(monitor, ref info) != 0, "Could not query the owner monitor work area.");
        return info.WorkArea;
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public override readonly string ToString() => $"({Left}, {Top})-({Right}, {Bottom})";
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public RECT Monitor;
        public RECT WorkArea;
        public uint Flags;
    }

    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint FindWindowW(string? className, string? windowName);

    [LibraryImport("user32.dll")]
    private static partial int IsWindowVisible(nint window);

    [LibraryImport("user32.dll")]
    private static partial int GetWindowRect(nint window, out RECT bounds);

    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromWindow(nint window, int flags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    private static partial int GetMonitorInfoW(nint monitor, ref MonitorInfo info);

    [LibraryImport("user32.dll", EntryPoint = "GetDpiForWindow")]
    private static partial uint GetDpiForWindow(nint window);

}
