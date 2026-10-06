using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Hosting;
using WinRT;

namespace DesktopFlyoutsSample.Uwp;

// A desktop owner is required: Windows.UI.Xaml.Hosting cannot be used inside an AppContainer app.
internal sealed unsafe partial class XamlIslandWindow : IDisposable
{
    private const string ClassName = "DesktopFlyoutsSample.Uwp.Window";
    private static nint _islandWindow;
    private readonly nint _window;
    private readonly DesktopWindowXamlSource _source;
    private readonly nint _nativeSource;
    private readonly SamplePage _page;

    public XamlIslandWindow()
    {
        var instance = GetModuleHandleW(null);
        fixed (char* className = ClassName)
        {
            var windowClass = new WindowClass
            {
                Size = (uint)sizeof(WindowClass),
                Instance = instance,
                WindowProc = &WindowProc,
                ClassName = className,
            };
            if (RegisterClassExW(in windowClass) == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        _window = CreateWindowExW(0, ClassName, "DesktopFlyoutsSample.Uwp", 0x00CF0000,
            unchecked((int)0x80000000), unchecked((int)0x80000000), 760, 820, 0, 0, instance, 0);
        if (_window == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
        var scale = GetDpiForWindow(_window) / 96.0;
        SetWindowPos(_window, 0, 0, 0, (int)(760 * scale), (int)(820 * scale), 0x0006);

        _source = new DesktopWindowXamlSource();
        using var reference = MarshalInspectable<DesktopWindowXamlSource>.CreateMarshaler(_source);
        var abi = MarshalInspectable<DesktopWindowXamlSource>.GetAbi(reference);
        var iid = new Guid("e3dcd8c7-3057-4692-99c3-7b7720afda31");
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface(abi, in iid, out _nativeSource));
        var vtable = *(nint**)_nativeSource;
        var attach = (delegate* unmanaged[Stdcall]<nint, nint, int>)vtable[3];
        Marshal.ThrowExceptionForHR(attach(_nativeSource, _window));
        var getWindow = (delegate* unmanaged[Stdcall]<nint, nint*, int>)vtable[4];
        nint island = 0;
        Marshal.ThrowExceptionForHR(getWindow(_nativeSource, &island));
        _islandWindow = island;

        _page = new SamplePage();
        _source.Content = _page;
        _page.Initialize(_window, () => PostMessageW(_window, 0x0010, 0, 0));
        GetClientRect(_window, out var bounds);
        SetWindowPos(_islandWindow, 0, 0, 0, bounds.Right, bounds.Bottom, 0x0040);
        ShowWindow(_window, 5);
    }

    public void Run()
    {
        var vtable = *(nint**)_nativeSource;
        var translate = (delegate* unmanaged[Stdcall]<nint, Message*, int*, int>)vtable[5];
        Message message;
        int result;
        while ((result = GetMessageW(&message, 0, 0, 0)) > 0)
        {
            if (_page.TryPreTranslateMessage((nint)(&message))) continue;
            int handled = 0;
            Marshal.ThrowExceptionForHR(translate(_nativeSource, &message, &handled));
            if (handled != 0) continue;
            TranslateMessage(in message);
            DispatchMessageW(in message);
        }
        if (result < 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint WindowProc(nint window, uint message, nuint wParam, nint lParam)
    {
        if (message == 0x0005 && _islandWindow != 0)
        {
            SetWindowPos(_islandWindow, 0, 0, 0, (int)(lParam & 0xffff), (int)((lParam >> 16) & 0xffff), 0x0040);
        }
        else if (message == 0x0010)
        {
            // Dispose XAML before destroying its owner, after the current event has returned.
            PostQuitMessage(0);
            return 0;
        }
        return DefWindowProcW(window, message, wParam, lParam);
    }

    public void Dispose()
    {
        _page.Dispose();
        _source.Content = null;
        _source.Dispose();
        Marshal.Release(_nativeSource);
        _islandWindow = 0;
        DestroyWindow(_window);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowClass
    {
        public uint Size, Style;
        public delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint> WindowProc;
        public int ClassExtra, WindowExtra;
        public nint Instance, Icon, Cursor, Background;
        public char* MenuName;
        public char* ClassName;
        public nint SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public nint Window;
        public uint Id;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public int X, Y;
        public uint Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint GetModuleHandleW(string? moduleName);
    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial ushort RegisterClassExW(in WindowClass windowClass);
    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateWindowExW(uint extendedStyle, string className, string title, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);
    [LibraryImport("user32.dll")]
    private static partial nint DefWindowProcW(nint window, uint message, nuint wParam, nint lParam);
    [LibraryImport("user32.dll")]
    private static partial int SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    [LibraryImport("user32.dll")]
    private static partial int ShowWindow(nint window, int command);
    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint window);
    [LibraryImport("user32.dll")]
    private static partial int GetClientRect(nint window, out Rect bounds);
    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int GetMessageW(Message* message, nint window, uint min, uint max);
    [LibraryImport("user32.dll")]
    private static partial int TranslateMessage(in Message message);
    [LibraryImport("user32.dll")]
    private static partial nint DispatchMessageW(in Message message);
    [LibraryImport("user32.dll")]
    private static partial int PostMessageW(nint window, uint message, nuint wParam, nint lParam);
    [LibraryImport("user32.dll")]
    private static partial void PostQuitMessage(int exitCode);
    [LibraryImport("user32.dll")]
    private static partial int DestroyWindow(nint window);
}
