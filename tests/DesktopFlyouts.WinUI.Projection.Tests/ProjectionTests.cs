using DesktopFlyouts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopFlyoutsWinUIProjectionTests;

[TestClass]
public sealed class ProjectionTests
{
    [TestMethod]
    public void WinUiFlyoutProjectionExposesStateProperty()
    {
        Assert.IsNotNull(typeof(DesktopFlyout).GetProperty(nameof(DesktopFlyout.State)));
    }

    [TestMethod]
    public void SystemTrayIconProjectionKeepsTheNativeHandleWidth()
    {
        var method = typeof(SystemTrayIcon).GetMethod(nameof(SystemTrayIcon.SetIconHandle));

        Assert.IsNotNull(method);
        Assert.AreEqual(typeof(long), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void SetIconExtensionRejectsNullBeforeCallingTheNativeComponent()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => SystemTrayIconExtensions.SetIcon(null!, nint.Zero));
    }
}
