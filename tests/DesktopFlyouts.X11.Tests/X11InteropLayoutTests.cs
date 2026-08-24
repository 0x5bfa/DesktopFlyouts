using System.Runtime.InteropServices;
using Xunit;

namespace DesktopFlyouts.Tests;

public class X11InteropLayoutTests
{
    [Fact]
    public void XSetWindowAttributesMatchesNativeLayout()
    {
        if (IntPtr.Size is 8)
        {
            Assert.Equal(112, Marshal.SizeOf<X11PInvoke.XSetWindowAttributes>());
            AssertOffset<X11PInvoke.XSetWindowAttributes>("backing_planes", 48);
            AssertOffset<X11PInvoke.XSetWindowAttributes>("backing_pixel", 56);
            AssertOffset<X11PInvoke.XSetWindowAttributes>("save_under", 64);
            AssertOffset<X11PInvoke.XSetWindowAttributes>("event_mask", 72);
            AssertOffset<X11PInvoke.XSetWindowAttributes>("override_redirect", 88);
            AssertOffset<X11PInvoke.XSetWindowAttributes>("cursor", 104);
        }
        else
        {
            Assert.Equal(60, Marshal.SizeOf<X11PInvoke.XSetWindowAttributes>());
            AssertOffset<X11PInvoke.XSetWindowAttributes>("backing_planes", 28);
            AssertOffset<X11PInvoke.XSetWindowAttributes>("backing_pixel", 32);
            AssertOffset<X11PInvoke.XSetWindowAttributes>("save_under", 36);
            AssertOffset<X11PInvoke.XSetWindowAttributes>("event_mask", 40);
            AssertOffset<X11PInvoke.XSetWindowAttributes>("override_redirect", 48);
            AssertOffset<X11PInvoke.XSetWindowAttributes>("cursor", 56);
        }
    }

    [Fact]
    public void XRandRMonitorInfoMatchesNativeLayout()
    {
        Assert.Equal(IntPtr.Size is 8 ? 56 : 44, Marshal.SizeOf<X11PInvoke.XRRMonitorInfo>());
        AssertOffset<X11PInvoke.XRRMonitorInfo>("outputs", IntPtr.Size is 8 ? 48 : 40);
    }

    [Fact]
    public void XClientMessageEventMatchesNativeLayout()
    {
        Assert.Equal(IntPtr.Size is 8 ? 96 : 48, Marshal.SizeOf<X11PInvoke.XClientMessageEvent>());
        AssertOffset<X11PInvoke.XClientMessageEvent>("ptr1", IntPtr.Size is 8 ? 56 : 28);
    }

    private static void AssertOffset<T>(string fieldName, int expected)
        => Assert.Equal(expected, Marshal.OffsetOf<T>(fieldName).ToInt32());
}
