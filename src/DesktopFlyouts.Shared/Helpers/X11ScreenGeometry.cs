#if HAS_UNO
using System.Drawing;

namespace DesktopFlyouts;

internal readonly record struct X11MonitorGeometry(Rectangle Bounds, bool IsPrimary);

internal static class X11ScreenGeometry
{
    internal static Rectangle SelectMonitor(
        IReadOnlyList<X11MonitorGeometry> monitors,
        Point? anchorPoint)
    {
        if (monitors.Count is 0)
            return default;

        if (anchorPoint is { } anchor)
        {
            foreach (var monitor in monitors)
            {
                if (monitor.Bounds.Contains(anchor))
                    return monitor.Bounds;
            }

            var nearest = monitors[0].Bounds;
            var nearestDistance = GetSquaredDistance(anchor, nearest);
            for (var i = 1; i < monitors.Count; i++)
            {
                var candidate = monitors[i].Bounds;
                var candidateDistance = GetSquaredDistance(anchor, candidate);
                if (candidateDistance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = candidateDistance;
                }
            }

            return nearest;
        }

        foreach (var monitor in monitors)
        {
            if (monitor.IsPrimary)
                return monitor.Bounds;
        }

        return monitors[0].Bounds;
    }

    internal static bool TrySelectWorkArea(
        ReadOnlySpan<nuint> values,
        nuint currentDesktop,
        out Rectangle workArea)
    {
        workArea = default;
        if (values.Length < 4)
            return false;

        var desktopOffset = currentDesktop <= int.MaxValue / 4
            ? (int)currentDesktop * 4
            : 0;
        if (desktopOffset < 0 || desktopOffset + 3 >= values.Length)
            desktopOffset = 0;

        workArea = new Rectangle(
            ToCoordinate(values[desktopOffset]),
            ToCoordinate(values[desktopOffset + 1]),
            ToDimension(values[desktopOffset + 2]),
            ToDimension(values[desktopOffset + 3]));
        return workArea.Width > 0 && workArea.Height > 0;
    }

    internal static Rectangle ApplyWorkArea(Rectangle monitor, Rectangle workArea)
    {
        if (monitor.Width <= 0 || monitor.Height <= 0)
            return workArea;

        if (workArea.Width <= 0 || workArea.Height <= 0)
            return monitor;

        var intersection = Rectangle.Intersect(monitor, workArea);
        return intersection.Width > 0 && intersection.Height > 0
            ? intersection
            : monitor;
    }

    private static long GetSquaredDistance(Point point, Rectangle rectangle)
    {
        var nearestX = Math.Clamp(point.X, rectangle.Left, rectangle.Right - 1);
        var nearestY = Math.Clamp(point.Y, rectangle.Top, rectangle.Bottom - 1);
        var deltaX = (long)point.X - nearestX;
        var deltaY = (long)point.Y - nearestY;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }

    private static int ToCoordinate(nuint value)
        => unchecked((int)(uint)value);

    private static int ToDimension(nuint value)
        => value > int.MaxValue ? int.MaxValue : (int)value;
}
#endif
