// Manual dependency property implementation for Uno.
// The GeneratedDependencyProperty source generator does not run under Uno.Sdk.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopFlyouts;

public partial class DesktopMenuFlyout
{
    public partial bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        private set => SetValue(IsOpenProperty, value);
    }
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(DesktopMenuFlyout), new PropertyMetadata(false));
}
