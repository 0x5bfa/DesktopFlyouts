// Polyfill for ManualDefinitions.
// The real implementation provides Win32 helper definitions and global usings.
// This stub provides the minimum API surface needed by shared code compilation.

namespace Windows.Win32;

internal static partial class ManualDefinitions
{
    internal static bool SUCCEEDED(int hr)
    {
        return hr >= 0;
    }

    internal static bool FAILED(int hr)
    {
        return hr < 0;
    }

    internal static int LOWORD(nint value)
    {
        return (int)(short)(value & 0xFFFF);
    }

    internal static int HIWORD(nint value)
    {
        return (int)(short)((value >> 16) & 0xFFFF);
    }
}
