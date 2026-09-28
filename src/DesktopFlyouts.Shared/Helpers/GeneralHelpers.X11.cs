#if HAS_UNO
// Detects the system theme via D-Bus org.freedesktop.portal.Settings.
// https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Settings.html

using DesktopFlyouts.DBus;
using Tmds.DBus.Protocol;

namespace DesktopFlyouts;

internal static class GeneralHelpers
{
    private const string Service = "org.freedesktop.portal.Desktop";
    private const string ObjectPath = "/org/freedesktop/portal/desktop";

    private static readonly object SyncRoot = new();
    private static bool _isTaskbarLight;
    private static Task? _initializationTask;
    private static DBusConnection? _connection;
    private static IDisposable? _settingsWatch;

    static GeneralHelpers()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DisposeResources();
    }

    internal static event EventHandler? SystemSettingsChanged;

    internal static bool IsTaskbarLight()
    {
        ThrowHelper.ThrowIfNotLinux();

        lock (SyncRoot)
        {
            _initializationTask ??= InitializeAsync();
            return _isTaskbarLight;
        }
    }

    internal static bool IsTaskbarColorPrevalenceEnabled()
    {
        // Linux desktop environments do not have a Windows-style "accent color on taskbar" setting.
        return false;
    }

    private static async Task InitializeAsync()
    {
        DBusConnection? connection = null;
        IDisposable? settingsWatch = null;
        try
        {
            var sessionAddress = DBusAddress.Session;
            if (sessionAddress is null)
                return;

            connection = new DBusConnection(sessionAddress);
            await connection.ConnectAsync();

            var desktopService = new DBusService(connection, Service);
            var settings = desktopService.CreateSettings(ObjectPath);
            if (await settings.GetVersionAsync() < 2)
                return;

            var result = await settings.ReadOneAsync(
                "org.freedesktop.appearance",
                "color-scheme");
            UpdateTheme(result.GetUInt32());

            settingsWatch = await settings.WatchSettingChangedAsync(tuple =>
            {
                if (tuple is { Namespace: "org.freedesktop.appearance", Key: "color-scheme" })
                    UpdateTheme(tuple.Value.GetUInt32());
            });

            lock (SyncRoot)
            {
                _connection = connection;
                _settingsWatch = settingsWatch;
                connection = null;
                settingsWatch = null;
            }
        }
        catch
        {
            // D-Bus or the portal is unavailable. Keep the dark fallback for this process.
        }
        finally
        {
            settingsWatch?.Dispose();
            connection?.Dispose();
        }
    }

    private static void UpdateTheme(uint colorScheme)
    {
        // 0 = no preference, 1 = dark, 2 = light.
        var isLight = colorScheme != 1;
        var changed = false;
        lock (SyncRoot)
        {
            if (_isTaskbarLight != isLight)
            {
                _isTaskbarLight = isLight;
                changed = true;
            }
        }

        if (changed)
            SystemSettingsChanged?.Invoke(null, EventArgs.Empty);
    }

    private static void DisposeResources()
    {
        IDisposable? settingsWatch;
        DBusConnection? connection;
        lock (SyncRoot)
        {
            settingsWatch = _settingsWatch;
            connection = _connection;
            _settingsWatch = null;
            _connection = null;
        }

        try
        {
            settingsWatch?.Dispose();
        }
        catch
        {
        }

        try
        {
            connection?.Dispose();
        }
        catch
        {
        }
    }
}
#endif
