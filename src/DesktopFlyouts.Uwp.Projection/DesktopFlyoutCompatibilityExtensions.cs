using System.Drawing;

namespace DesktopFlyouts;

/// <summary>
/// Compatibility overloads for APIs that previously accepted System.Drawing.Point.
/// </summary>
public static class DesktopFlyoutCompatibilityExtensions
{
    public static void Show(this DesktopFlyout flyout, Point bottomCenterPoint)
    {
        ArgumentNullException.ThrowIfNull(flyout);
        flyout.Show(new Windows.Foundation.Point(bottomCenterPoint.X, bottomCenterPoint.Y));
    }

    public static void Show(this DesktopMenuFlyout flyout, Point point)
    {
        ArgumentNullException.ThrowIfNull(flyout);
        flyout.Show(new Windows.Foundation.Point(point.X, point.Y));
    }
}
