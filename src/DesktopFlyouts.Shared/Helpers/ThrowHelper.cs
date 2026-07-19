#if HAS_UNO
using System;

namespace DesktopFlyouts
{
    internal static class ThrowHelper
    {
        internal static void ThrowIfNotLinux()
        {
            if (!OperatingSystem.IsLinux())
                throw new PlatformNotSupportedException("DesktopFlyouts X11 APIs are only supported on Linux.");
        }
    }
}
#endif
