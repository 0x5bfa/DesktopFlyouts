// Manual dependency property implementation for Uno.
// The GeneratedDependencyProperty source generator does not run under Uno.Sdk.

using Microsoft.UI.Xaml;

namespace DesktopFlyouts;

public partial class DesktopFlyoutIslandTemplateSettings
{
    public partial CornerRadius BackdropCornerRadius
    {
        get => (CornerRadius)GetValue(BackdropCornerRadiusProperty);
        internal set => SetValue(BackdropCornerRadiusProperty, value);
    }

    public static readonly DependencyProperty BackdropCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(BackdropCornerRadius),
            typeof(CornerRadius),
            typeof(DesktopFlyoutIslandTemplateSettings),
            new PropertyMetadata(default(CornerRadius)));
}
