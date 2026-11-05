using Xunit;

namespace DesktopFlyouts.Tests;

public class X11WindowActivationTests
{
    [Theory]
    [InlineData(DesktopFlyoutActivationMode.Activate, false)]
    [InlineData(DesktopFlyoutActivationMode.NoActivateOnOpen, false)]
    [InlineData(DesktopFlyoutActivationMode.NeverActivate, true)]
    public void OverrideRedirectIsReservedForNeverActivate(
        DesktopFlyoutActivationMode mode,
        bool expected)
    {
        Assert.Equal(expected, X11WindowActivation.UsesOverrideRedirect(mode));
    }

    [Theory]
    [InlineData(DesktopFlyoutActivationMode.Activate, true, true)]
    [InlineData(DesktopFlyoutActivationMode.Activate, false, false)]
    [InlineData(DesktopFlyoutActivationMode.NoActivateOnOpen, true, false)]
    [InlineData(DesktopFlyoutActivationMode.NeverActivate, true, false)]
    public void ActivationRequestsRespectMode(
        DesktopFlyoutActivationMode mode,
        bool activate,
        bool expected)
    {
        Assert.Equal(expected, X11WindowActivation.ShouldRequestActivation(mode, activate));
    }
}
