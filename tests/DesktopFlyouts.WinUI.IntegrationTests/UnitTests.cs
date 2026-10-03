using System.Runtime.InteropServices;
using DesktopFlyouts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

using FlyoutGridLength = Microsoft.UI.Xaml.GridLength;
using FlyoutGridUnitType = Microsoft.UI.Xaml.GridUnitType;
using FlyoutOrientation = Microsoft.UI.Xaml.Controls.Orientation;
using FlyoutBorder = Microsoft.UI.Xaml.Controls.Border;

namespace DesktopFlyouts.WinUI.IntegrationTests;

[TestClass]
public sealed class UnitTests
{
    [UITestMethod]
    public async Task ShowHidePlacementDirectionAndOrientationWorkAtRuntime()
    {
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        await RunBasicChecks(app.TestOwnerWindow);
    }

    [UITestMethod]
    public async Task LegacyXamlContentDependencyPropertiesAndDataContextWorkAtRuntime()
    {
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        using var flyout = new ApiCompatibilityFlyout
        {
            OwnerWindowHandle = app.TestOwnerWindow,
            IsTransitionAnimationEnabled = false,
            DataContext = new FlyoutBindingModel("before show"),
        };

        Ensure(flyout.Placement == DesktopFlyoutPlacementMode.BottomRight,
            "Legacy PascalCase placement names must be available in XAML.");
        Ensure(flyout.FlyoutWidth.Value == 360,
            "A Style Setter must configure FlyoutWidth as a dependency property.");
        Ensure(flyout.Islands.Count == 1,
            "A direct DesktopFlyoutIsland child must be added to Islands.");

        flyout.Show();
        await Task.Delay(100);
        Ensure(flyout.IsOpen, "The XAML-derived flyout must open.");
        Ensure(flyout.BoundLabel == "before show", "The initial DataContext must reach island content.");

        flyout.DataContext = new FlyoutBindingModel("after show");
        await Task.Delay(100);
        Ensure(flyout.BoundLabel == "after show", "DataContext changes must reach open island content.");
    }

    private sealed record FlyoutBindingModel(string Label);

    private const int MonitorDefaultToNearest = 2;
    private const int PositionTolerancePixels = 4;
    public static async Task RunBasicChecks(nint ownerWindow)
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
            Ensure(flyout.IsOpen, "Show must open the flyout.");
            Ensure(IsFlyoutWindowVisible(), "Show must display the native flyout window.");
            flyout.Hide();
            Ensure(!flyout.IsOpen, "Hide must close the flyout.");
            Ensure(!IsFlyoutWindowVisible(), "Hide must hide the native flyout window.");

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
        var margin = 0;
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
                $"Placement {placement} was at an unexpected work-area position: {bounds}, work area {workArea}, " +
                $"horizontal error {horizontalError}px, vertical error {verticalError}px.");
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
            Ensure(flyout.IsOpen, $"Islands orientation {orientation} did not open the flyout.");
            Ensure(flyout.IslandsOrientation == orientation, $"Islands orientation {orientation} was not retained.");

            var firstOrigin = first.TransformToVisual(flyout).TransformPoint(new Windows.Foundation.Point(0, 0));
            var secondOrigin = second.TransformToVisual(flyout).TransformPoint(new Windows.Foundation.Point(0, 0));
            if (orientation == FlyoutOrientation.Vertical)
            {
                Ensure(secondOrigin.Y > firstOrigin.Y, "Vertical islands must be arranged top to bottom.");
            }
            else
            {
                Ensure(secondOrigin.X > firstOrigin.X, "Horizontal islands must be arranged left to right.");
            }

            flyout.Hide();
        }
    }

    private static bool IsFlyoutWindowVisible()
    {
        var window = FindFlyoutWindow();
        return window != 0 && IsWindowVisible(window);
    }

    private static RECT GetFlyoutBounds()
    {
        var window = FindFlyoutWindow();
        if (window == 0 || !GetWindowRect(window, out var bounds))
        {
            throw new InvalidOperationException("The native flyout window was not found.");
        }

        return bounds;
    }

    private static nint FindFlyoutWindow()
    {
        var winUiWindow = FindWindowW(null, "DesktopFlyout");
        return winUiWindow != 0 ? winUiWindow : FindWindowW(null, "DesktopFlyout.Uwp");
    }

    private static RECT GetWorkArea(nint ownerWindow)
    {
        var monitor = MonitorFromWindow(ownerWindow, MonitorDefaultToNearest);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        Ensure(monitor != 0 && GetMonitorInfoW(monitor, ref info), "Could not query the owner monitor work area.");
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

    [DllImport("user32.dll", EntryPoint = "FindWindowW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint FindWindowW(string? className, string? windowName);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(nint window, out RECT bounds);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window, int flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetMonitorInfoW(nint monitor, ref MonitorInfo info);

}
