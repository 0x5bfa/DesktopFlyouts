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

    // This method is a real C# consumer compile fixture. Building the test assembly
    // verifies the generated UWP projection supports these normal call patterns.
    private static void CompileTypicalConsumerUsage(
        SystemTrayIcon trayIcon,
        DesktopFlyout flyout,
        DesktopMenuFlyout menuFlyout)
    {
        EventHandler<SystemTrayIconEventArgs> handler = OnTrayIconClicked;
        trayIcon.LeftClicked += handler;
        trayIcon.SetIcon((nint)0);
        trayIcon.LeftClicked -= handler;

        flyout.HideOnLostFocus = !flyout.HideOnLostFocus;
        flyout.Placement = flyout.Placement;
        _ = flyout.State;
        flyout.Hide();

        menuFlyout.OwnerWindowHandle = menuFlyout.OwnerWindowHandle;
        _ = menuFlyout.IsOpen;
        menuFlyout.Hide();
    }

    private static void OnTrayIconClicked(object? sender, SystemTrayIconEventArgs args)
    {
        _ = args.Point;
    }
}
