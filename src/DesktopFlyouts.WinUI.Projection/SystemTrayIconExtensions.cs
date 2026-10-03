namespace DesktopFlyouts;

public static class SystemTrayIconExtensions
{
    public static void SetIcon(this SystemTrayIcon icon, nint iconHandle)
    {
        ArgumentNullException.ThrowIfNull(icon);
        icon.SetIconHandle(iconHandle);
    }

    public static SystemTrayIcon CreateFromIconHandle(nint iconHandle, string tooltip, Guid id)
    {
        return SystemTrayIcon.CreateFromIconHandle(iconHandle, tooltip, id);
    }
}
