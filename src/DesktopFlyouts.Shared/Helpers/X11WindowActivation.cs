#if HAS_UNO
namespace DesktopFlyouts;

internal static class X11WindowActivation
{
    internal static bool UsesOverrideRedirect(DesktopFlyoutActivationMode activationMode)
        => activationMode is DesktopFlyoutActivationMode.NeverActivate;

    internal static bool ShouldRequestActivation(
        DesktopFlyoutActivationMode activationMode,
        bool activate)
        => activate && activationMode is DesktopFlyoutActivationMode.Activate;
}
#endif
