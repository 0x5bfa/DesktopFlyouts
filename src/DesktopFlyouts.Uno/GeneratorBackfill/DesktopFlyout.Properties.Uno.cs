// Manual dependency property implementations for Uno.
// The GeneratedDependencyProperty source generator does not run under Uno.Sdk.
// Each property below is the implementation part matching the partial declaration in the shared file.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopFlyouts
{
    public partial class DesktopFlyout
    {
        partial void OnIslandsSourcePropertyChanged(DependencyPropertyChangedEventArgs e);
        partial void OnIslandsOrientationPropertyChanged(DependencyPropertyChangedEventArgs e);
        partial void OnIsBackdropEnabledPropertyChanged(DependencyPropertyChangedEventArgs e);
        partial void OnBackdropKindPropertyChanged(DependencyPropertyChangedEventArgs e);
        partial void OnActivationModePropertyChanged(DependencyPropertyChangedEventArgs e);
        partial void OnHideOnLostFocusPropertyChanged(DependencyPropertyChangedEventArgs e);

        public partial object? IslandsSource
        {
            get => (object?)GetValue(IslandsSourceProperty);
            set => SetValue(IslandsSourceProperty, value);
        }
        public static readonly DependencyProperty IslandsSourceProperty =
            DependencyProperty.Register(nameof(IslandsSource), typeof(object), typeof(DesktopFlyout), new PropertyMetadata(null, (d, e) => ((DesktopFlyout)d).OnIslandsSourcePropertyChanged(e)));

        public partial bool IsBackdropEnabled
        {
            get => (bool)GetValue(IsBackdropEnabledProperty);
            set => SetValue(IsBackdropEnabledProperty, value);
        }
        public static readonly DependencyProperty IsBackdropEnabledProperty =
            DependencyProperty.Register(nameof(IsBackdropEnabled), typeof(bool), typeof(DesktopFlyout), new PropertyMetadata(true, (d, e) => ((DesktopFlyout)d).OnIsBackdropEnabledPropertyChanged(e)));

        public partial bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            private set => SetValue(IsOpenProperty, value);
        }
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(DesktopFlyout), new PropertyMetadata(false));

        public partial DesktopFlyoutPopupDirection PopupDirection
        {
            get => (DesktopFlyoutPopupDirection)GetValue(PopupDirectionProperty);
            set => SetValue(PopupDirectionProperty, value);
        }
        public static readonly DependencyProperty PopupDirectionProperty =
            DependencyProperty.Register(nameof(PopupDirection), typeof(DesktopFlyoutPopupDirection), typeof(DesktopFlyout), new PropertyMetadata(DesktopFlyoutPopupDirection.Vertical));

        public partial Orientation IslandsOrientation
        {
            get => (Orientation)GetValue(IslandsOrientationProperty);
            set => SetValue(IslandsOrientationProperty, value);
        }
        public static readonly DependencyProperty IslandsOrientationProperty =
            DependencyProperty.Register(nameof(IslandsOrientation), typeof(Orientation), typeof(DesktopFlyout), new PropertyMetadata(Orientation.Vertical, (d, e) => ((DesktopFlyout)d).OnIslandsOrientationPropertyChanged(e)));

        public partial DesktopFlyoutPlacementMode Placement
        {
            get => (DesktopFlyoutPlacementMode)GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }
        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register(nameof(Placement), typeof(DesktopFlyoutPlacementMode), typeof(DesktopFlyout), new PropertyMetadata(DesktopFlyoutPlacementMode.BottomRight));

        public partial MenuFlyout? MenuFlyout
        {
            get => (MenuFlyout?)GetValue(MenuFlyoutProperty);
            set => SetValue(MenuFlyoutProperty, value);
        }
        public static readonly DependencyProperty MenuFlyoutProperty =
            DependencyProperty.Register(nameof(MenuFlyout), typeof(MenuFlyout), typeof(DesktopFlyout), new PropertyMetadata(null));

        public partial bool IsTransitionAnimationEnabled
        {
            get => (bool)GetValue(IsTransitionAnimationEnabledProperty);
            set => SetValue(IsTransitionAnimationEnabledProperty, value);
        }
        public static readonly DependencyProperty IsTransitionAnimationEnabledProperty =
            DependencyProperty.Register(nameof(IsTransitionAnimationEnabled), typeof(bool), typeof(DesktopFlyout), new PropertyMetadata(true));

        public partial double PressedScale
        {
            get => (double)GetValue(PressedScaleProperty);
            set => SetValue(PressedScaleProperty, value);
        }
        public static readonly DependencyProperty PressedScaleProperty =
            DependencyProperty.Register(nameof(PressedScale), typeof(double), typeof(DesktopFlyout), new PropertyMetadata(1.0D));

        public partial bool IsSwipeToDismissEnabled
        {
            get => (bool)GetValue(IsSwipeToDismissEnabledProperty);
            set => SetValue(IsSwipeToDismissEnabledProperty, value);
        }
        public static readonly DependencyProperty IsSwipeToDismissEnabledProperty =
            DependencyProperty.Register(nameof(IsSwipeToDismissEnabled), typeof(bool), typeof(DesktopFlyout), new PropertyMetadata(false));

        public partial double SwipeDismissThreshold
        {
            get => (double)GetValue(SwipeDismissThresholdProperty);
            set => SetValue(SwipeDismissThresholdProperty, value);
        }
        public static readonly DependencyProperty SwipeDismissThresholdProperty =
            DependencyProperty.Register(nameof(SwipeDismissThreshold), typeof(double), typeof(DesktopFlyout), new PropertyMetadata(80.0D));

        public partial bool HideOnLostFocus
        {
            get => (bool)GetValue(HideOnLostFocusProperty);
            set => SetValue(HideOnLostFocusProperty, value);
        }
        public static readonly DependencyProperty HideOnLostFocusProperty =
            DependencyProperty.Register(nameof(HideOnLostFocus), typeof(bool), typeof(DesktopFlyout), new PropertyMetadata(true, (d, e) => ((DesktopFlyout)d).OnHideOnLostFocusPropertyChanged(e)));

        public partial DesktopFlyoutActivationMode ActivationMode
        {
            get => (DesktopFlyoutActivationMode)GetValue(ActivationModeProperty);
            set => SetValue(ActivationModeProperty, value);
        }
        public static readonly DependencyProperty ActivationModeProperty =
            DependencyProperty.Register(nameof(ActivationMode), typeof(DesktopFlyoutActivationMode), typeof(DesktopFlyout), new PropertyMetadata(DesktopFlyoutActivationMode.Activate, (d, e) => ((DesktopFlyout)d).OnActivationModePropertyChanged(e)));

        public partial DesktopFlyoutBackdropKind BackdropKind
        {
            get => (DesktopFlyoutBackdropKind)GetValue(BackdropKindProperty);
            set => SetValue(BackdropKindProperty, value);
        }
        public static readonly DependencyProperty BackdropKindProperty =
            DependencyProperty.Register(nameof(BackdropKind), typeof(DesktopFlyoutBackdropKind), typeof(DesktopFlyout), new PropertyMetadata(DesktopFlyoutBackdropKind.DesktopAcrylic, (d, e) => ((DesktopFlyout)d).OnBackdropKindPropertyChanged(e)));
    }
}
