#if HAS_UNO
using System.Runtime.InteropServices;

namespace DesktopFlyouts;

partial class X11PInvoke
{
    private const string LibX11 = "libX11.so.6";
    private const string LibXext = "libXext.so.6";

    [LibraryImport(LibX11)]
    public static partial nint XOpenDisplay(nint display);

    [LibraryImport(LibX11)]
    public static partial int XCloseDisplay(nint display);

    [LibraryImport(LibX11)]
    public static partial nint XDefaultRootWindow(nint display);

    [LibraryImport(LibX11)]
    public static partial int XQueryPointer(
        nint display,
        nint window,
        out nint rootReturn,
        out nint childReturn,
        out int rootXReturn,
        out int rootYReturn,
        out int winXReturn,
        out int winYReturn,
        out nuint maskReturn);

    [LibraryImport(LibX11, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint XInternAtom(nint display, string atomName, [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

    [LibraryImport(LibX11)]
    public static partial int XChangeProperty(
        nint display, nint window, nint property, nint type,
        int format, PropertyMode mode, byte[] data, int nelements);

    [LibraryImport(LibX11)]
    public static partial int XMapWindow(nint display, nint window);

    [LibraryImport(LibX11)]
    public static partial int XUnmapWindow(nint display, nint window);

    [LibraryImport(LibX11)]
    public static partial int XMoveWindow(nint display, nint window, int x, int y);

    [LibraryImport(LibX11)]
    public static partial int XRaiseWindow(nint display, nint window);

    [LibraryImport(LibX11)]
    public static partial int XGetWindowAttributes(nint display, nint window, ref XWindowAttributes attributes);
    [LibraryImport(LibX11)]
    public static partial int XSelectInput(nint display, nint window, nint mask);

    [LibraryImport(LibX11)]
    public static partial int XFlush(nint display);

    [LibraryImport(LibX11)]
    public static partial int XSync(nint display, [MarshalAs(UnmanagedType.Bool)] bool discard);

    [LibraryImport(LibX11)]
    public static partial int XChangeWindowAttributes(nint display, nint window, nint valuemask, ref XSetWindowAttributes attributes);

    [LibraryImport(LibX11)]
    public static partial int XSendEvent(nint display, nint window,
        [MarshalAs(UnmanagedType.Bool)] bool propagate, nint eventMask, ref XEvent sendEvent);

    [LibraryImport(LibX11)]
    public static partial int XQueryTree(
        nint display, nint window,
        out nint rootReturn, out nint parentReturn,
        out nint childrenReturn, out int nchildrenReturn);

    [LibraryImport(LibX11)]
    public static partial int XFree(nint data);

    [LibraryImport(LibX11)]
    public static partial int XTranslateCoordinates(
        nint display, nint srcWindow, nint destWindow,
        int srcX, int srcY,
        out int destX, out int destY,
        out nint childReturn);


    [LibraryImport(LibXext)]
    public static partial int XShapeCombineRegion(nint display, nint window, int destKind,
        int xOff, int yOff, nint srcRegion, int op);
        
    
    [LibraryImport(LibX11)]
    public static partial nint XCreateRegion();

    [LibraryImport(LibX11)]
    public static partial int XDestroyRegion(nint region);

    [LibraryImport(LibX11)]
    public static unsafe partial int XUnionRectWithRegion(XRegionRectangle* rectangle, nint srcRegion, nint destRegionReturn);

    [LibraryImport(LibX11)]
    public static partial int XSetInputFocus(nint display, nint focus, int revertTo, nint time);

    [LibraryImport(LibX11)]
    public static partial int XGetWindowProperty(
        nint display,
        nuint window,
        nuint property,
        nint longOffset,
        nint longLength,
        [MarshalAs(UnmanagedType.Bool)] bool delete,
        nuint reqType,
        out nuint actualType,
        out int actualFormat,
        out nuint nItems,
        out nuint bytesAfter,
        out nint prop);

    public enum PropertyMode : int { Replace = 0, Prepend = 1, Append = 2 }

    [StructLayout(LayoutKind.Sequential)]
    public struct XWindowAttributes
    {
        public int X, Y, Width, Height, BorderWidth, Depth;
        public nint Visual, Root;
        public int Class, BitGravity, WinGravity, BackingStore;
        public nuint BackingPlanes, BackingPixel;
        public int SaveUnder;
        public nint Colormap;
        public int MapInstalled, MapState;
        public nint AllEventMasks, YourEventMask, DoNotPropagateMask;
        public int OverrideRedirect;
        public nint Screen;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XSetWindowAttributes
    {
        public nint background_pixmap, background_pixel, border_pixmap, border_pixel;
        public int bit_gravity, win_gravity, backing_store;
        public uint backing_planes, backing_pixel;
        public int save_under;
        public nint event_mask, do_not_propagate_mask;
        public int override_redirect;
        public nint colormap, cursor;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct XEvent
    {
        [FieldOffset(0)] public int type;
        [FieldOffset(0)] public XClientMessageEvent xclient;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XClientMessageEvent
    {
        public int type;
        public long serial;
        public int send_event;
        public nint display;
        public nint window;
        public nint message_type;
        public int format;
        public nint ptr1, ptr2, ptr3, ptr4, ptr5;
    }
        
    [StructLayout(LayoutKind.Sequential)]
    public struct XRegionRectangle
    {
        public short x, y;
        public ushort width, height;
    }

    public const int ClientMessage = 33;
    public const long StructureNotifyMask = 1L << 17;
    public const long FocusChangeMask = 1L << 21;
    public const long PropertyChangeMask = 1L << 22;
    public const nint CWBackPixmap = 1 << 0;
    public const nint CWOverrideRedirect = 1 << 9;
    // Predefined X11 atom ID for type ATOM.
    public const nint XA_ATOM = 4;

    // background_pixmap values
    public const nint None = 0;            // no background — transparent under compositors
    public const nint ParentRelative = 1;  // copy parent's background
}
#endif
