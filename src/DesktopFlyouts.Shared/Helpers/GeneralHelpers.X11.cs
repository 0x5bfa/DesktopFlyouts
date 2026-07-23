#if HAS_UNO
// Real implementation: detects system theme via D-Bus org.freedesktop.portal.Settings.
// https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Settings.html

using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using DesktopFlyouts.DBus;

namespace DesktopFlyouts
{
    internal static class GeneralHelpers
    {
        private const string Service = "org.freedesktop.portal.Desktop";
        private const string ObjectPath = "/org/freedesktop/portal/desktop";

        private static bool? _isTaskbarLight;
        private static readonly object _lock = new();
        private static bool _initialized;

        internal static bool IsTaskbarLight()
        {
            ThrowHelper.ThrowIfNotLinux();

            if (!_initialized)
            {
                _ = InitAsync().ConfigureAwait(false);
            }

            lock (_lock)
            {
                return _isTaskbarLight ?? false;
            }
        }

        internal static bool IsTaskbarColorPrevalenceEnabled()
        {
            // Linux desktop environments do not have a Windows-style "accent color on taskbar" setting.
            return false;
        }

        private static async Task InitAsync()
        {
            try
            {
                var sessionAddress = DBusAddress.Session;
                if (sessionAddress is null)
                    return;

                var connection = new DBusConnection(sessionAddress);
                await connection.ConnectAsync();

                var desktopService = new DBusService(connection, Service);
                var settings = desktopService.CreateSettings(ObjectPath);

                var version = await settings.GetVersionAsync();
                if (version < 2)
                    return;

                var result = await settings.ReadOneAsync("org.freedesktop.appearance", "color-scheme");
                var colorScheme = result.GetUInt32();
                // 0 = no preference, 1 = dark, 2 = light
                lock (_lock)
                {
                    _isTaskbarLight = colorScheme != 1;
                    _initialized = true;
                }

                _ = settings.WatchSettingChangedAsync(tuple =>
                {
                    if (tuple is { Namespace: "org.freedesktop.appearance", Key: "color-scheme" })
                    {
                        lock (_lock)
                        {
                            _isTaskbarLight = tuple.Value.GetUInt32() != 1;
                        }
                    }
                });
            }
            catch
            {
                // D-Bus not available or portal not present; fall back to default (dark).
                lock (_lock)
                {
                    _isTaskbarLight = false;
                    _initialized = true;
                }
            }
        }
    }
}
#endif
