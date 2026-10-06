using DesktopFlyouts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopFlyoutsUwpProjectionTests;

[TestClass]
public sealed class ProjectionTests
{
    [TestMethod]
    public void SetIconRejectsNullTarget()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => SystemTrayIconExtensions.SetIcon(null!, nint.Zero));
    }

    [TestMethod]
    public void FlyoutUsesThePlatformXamlProjection()
    {
        Assert.AreEqual(typeof(Windows.UI.Xaml.UIElement),
            typeof(DesktopFlyout).GetProperty(nameof(DesktopFlyout.Content))!.PropertyType);
        Assert.IsTrue(typeof(Windows.UI.Xaml.Controls.Control).IsAssignableFrom(typeof(DesktopFlyout)));
        Assert.AreEqual(typeof(Windows.UI.Xaml.GridLength),
            typeof(DesktopFlyout).GetProperty(nameof(DesktopFlyout.FlyoutWidth))!.PropertyType);
    }

    // This method is a real C# consumer compile fixture. Building the test assembly
    // verifies the generated UWP projection supports these normal call patterns.
    private static void CompileTypicalConsumerUsage(
        SystemTrayIcon trayIcon,
        DesktopFlyout flyout,
        DesktopMenuFlyout menuFlyout)
    {
        EventHandler<MouseEventReceivedEventArgs> handler = OnTrayIconClicked;
        trayIcon.LeftClicked += handler;
        trayIcon.SetIcon((nint)0);
        trayIcon.LeftClicked -= handler;

        using (flyout)
        {
            flyout.Show(new System.Drawing.Point(10, 20));
            _ = DesktopFlyout.FlyoutWidthProperty;
            _ = DesktopFlyout.AutoCloseDelayProperty;
        }

        flyout.HideOnLostFocus = !flyout.HideOnLostFocus;
        flyout.Content = new Windows.UI.Xaml.Controls.Border();
        flyout.Placement = flyout.Placement;
        _ = flyout.State;
        flyout.Hide();

        using (menuFlyout)
        {
            menuFlyout.Show(new System.Drawing.Point(10, 20));
            menuFlyout.OwnerWindowHandle = menuFlyout.OwnerWindowHandle;
            _ = menuFlyout.IsOpen;
            menuFlyout.Hide();
        }

        using (trayIcon)
        {
            trayIcon.SetIcon((nint)0);
        }
    }

    private static void OnTrayIconClicked(object? sender, MouseEventReceivedEventArgs args)
    {
        _ = args.Point;
    }
}
