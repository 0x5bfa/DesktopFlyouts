// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace DesktopFlyouts
{
    public partial class DesktopFlyout
    {
        private readonly ObservableCollection<DesktopFlyoutIsland> _islands = [];

        /// <summary>
        /// Gets the islands displayed in the flyout.
        /// </summary>
        /// <value>The ordered collection of <see cref="DesktopFlyoutIsland"/> sections.</value>
        /// <remarks>
        /// This is the XAML content collection for <see cref="DesktopFlyout"/>. Add islands directly
        /// in XAML or populate the collection before opening the flyout.
        /// </remarks>
        public IList<DesktopFlyoutIsland> Islands => _islands;

        /// <summary>
        /// Gets or sets a source collection for flyout islands.
        /// </summary>
        /// <value>An <see cref="IEnumerable{T}"/> containing <see cref="DesktopFlyoutIsland"/> instances.</value>
        /// <remarks>
        /// Values that are not an <see cref="IEnumerable{T}"/> of <see cref="DesktopFlyoutIsland"/>
        /// are ignored. Replacing the source clears the current <see cref="Islands"/> collection.
        /// </remarks>
        [GeneratedDependencyProperty]
        public partial object? IslandsSource { get; set; }

        /// <summary>
        /// Gets or sets whether island backdrops are enabled.
        /// </summary>
        /// <value><see langword="true"/> to enable island backdrops; otherwise, <see langword="false"/>. The default is <see langword="true"/>.</value>
        /// <remarks>
        /// Backdrops are applied by the Windows App SDK package. UWP builds keep the property for API
        /// compatibility, but do not create a Windows App SDK system backdrop.
        /// </remarks>
        [GeneratedDependencyProperty(DefaultValue = true)]
        public partial bool IsBackdropEnabled { get; set; }

        /// <summary>
        /// Gets whether the flyout is currently open.
        /// </summary>
        /// <value><see langword="true"/> after the open transition completes; otherwise, <see langword="false"/>.</value>
        [GeneratedDependencyProperty]
        public partial bool IsOpen { get; private set; }

        /// <summary>
        /// Identifies the <see cref="FlyoutWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FlyoutWidthProperty =
            DependencyProperty.Register(nameof(FlyoutWidth), typeof(GridLength), typeof(DesktopFlyout), new PropertyMetadata(GridLength.Auto, OnFlyoutSizePropertyChanged));

        /// <summary>
        /// Gets or sets the requested flyout width.
        /// </summary>
        /// <value>The desired flyout width. The default is <see cref="GridLength.Auto"/>.</value>
        /// <remarks>
        /// Use pixel, auto, or star sizing. Star sizing stretches to the available work-area width
        /// after subtracting <see cref="FrameworkElement.Margin"/>.
        /// </remarks>
        public GridLength FlyoutWidth
        {
            get => (GridLength)GetValue(FlyoutWidthProperty);
            set => SetValue(FlyoutWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FlyoutHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FlyoutHeightProperty =
            DependencyProperty.Register(nameof(FlyoutHeight), typeof(GridLength), typeof(DesktopFlyout), new PropertyMetadata(GridLength.Auto, OnFlyoutSizePropertyChanged));

        /// <summary>
        /// Gets or sets the requested flyout height.
        /// </summary>
        /// <value>The desired flyout height. The default is <see cref="GridLength.Auto"/>.</value>
        /// <remarks>
        /// Use pixel, auto, or star sizing. Star sizing stretches to the available work-area height
        /// after subtracting <see cref="FrameworkElement.Margin"/>.
        /// </remarks>
        public GridLength FlyoutHeight
        {
            get => (GridLength)GetValue(FlyoutHeightProperty);
            set => SetValue(FlyoutHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the preferred popup direction.
        /// </summary>
        /// <value>The preferred direction for open and close transitions. The default is <see cref="DesktopFlyoutPopupDirection.Vertical"/>.</value>
        /// <remarks>
        /// Automatic directions are resolved from the final flyout position. A flyout in the bottom
        /// half of the work area opens upward; a flyout in the top half opens downward.
        /// </remarks>
        [GeneratedDependencyProperty(DefaultValue = DesktopFlyoutPopupDirection.Vertical)]
        public partial DesktopFlyoutPopupDirection PopupDirection { get; set; }

        /// <summary>
        /// Gets or sets how islands are arranged.
        /// </summary>
        /// <value>The orientation used to arrange <see cref="Islands"/>. The default is <see cref="Orientation.Vertical"/>.</value>
        [GeneratedDependencyProperty(DefaultValue = Orientation.Vertical)]
        public partial Orientation IslandsOrientation { get; set; }

        /// <summary>
        /// Gets or sets the flyout placement on the work area.
        /// </summary>
        /// <value>The work-area placement used by <see cref="DesktopFlyout.Show()"/>. The default is <see cref="DesktopFlyoutPlacementMode.BottomRight"/>.</value>
        /// <remarks>
        /// This property is ignored for the one open operation started by the point-based
        /// <see cref="DesktopFlyout.Show()"/> overload.
        /// </remarks>
        [GeneratedDependencyProperty(DefaultValue = DesktopFlyoutPlacementMode.BottomRight)]
        public partial DesktopFlyoutPlacementMode Placement { get; set; }

        /// <summary>
        /// Gets or sets the menu flyout associated with the desktop flyout.
        /// </summary>
        /// <value>An optional XAML <see cref="MenuFlyout"/> reference for application code.</value>
        /// <remarks>
        /// <see cref="DesktopFlyout"/> does not display this menu automatically. Use
        /// <see cref="DesktopMenuFlyout"/> when you need a menu hosted in a desktop island window.
        /// </remarks>
        [GeneratedDependencyProperty]
        public partial MenuFlyout? MenuFlyout { get; set; }

        /// <summary>
        /// Gets or sets whether open and close transitions are enabled.
        /// </summary>
        /// <value><see langword="true"/> to animate open and close transitions; otherwise, <see langword="false"/>. The default is <see langword="true"/>.</value>
        [GeneratedDependencyProperty(DefaultValue = true)]
        public partial bool IsTransitionAnimationEnabled { get; set; }

        /// <summary>
        /// Gets or sets the scale applied while the flyout is pressed. Set to 1.0 to disable press scaling.
        /// </summary>
        /// <value>The pressed scale factor. The default is <c>1.0</c>.</value>
        /// <remarks>
        /// Values are clamped to the range <c>0.1</c> through <c>2.0</c>. <see cref="double.NaN"/>
        /// and infinity disable press scaling.
        /// </remarks>
        [GeneratedDependencyProperty(DefaultValue = 1.0D)]
        public partial double PressedScale { get; set; }

        /// <summary>
        /// Gets or sets whether the flyout can be dismissed by swiping in the opposite direction of the active popup direction.
        /// </summary>
        /// <value><see langword="true"/> to allow swipe-to-dismiss; otherwise, <see langword="false"/>. The default is <see langword="false"/>.</value>
        [GeneratedDependencyProperty(DefaultValue = false)]
        public partial bool IsSwipeToDismissEnabled { get; set; }

        /// <summary>
        /// Gets or sets the swipe distance in DIPs required to dismiss the flyout.
        /// </summary>
        /// <value>The swipe threshold in device-independent pixels. The default is <c>80</c>.</value>
        /// <remarks>
        /// The effective threshold is clamped to the available dismiss distance. <see cref="double.NaN"/>
        /// and infinity use a default threshold based on that available distance.
        /// </remarks>
        [GeneratedDependencyProperty(DefaultValue = 80.0D)]
        public partial double SwipeDismissThreshold { get; set; }

        /// <summary>
        /// Gets or sets whether the flyout closes when it loses focus.
        /// </summary>
        /// <value><see langword="true"/> to close when the host window is deactivated; otherwise, <see langword="false"/>. The default is <see langword="true"/>.</value>
        [GeneratedDependencyProperty(DefaultValue = true)]
        public partial bool HideOnLostFocus { get; set; }

        /// <summary>
        /// Gets or sets how the flyout participates in activation and focus.
        /// </summary>
        /// <value>The activation behavior used when opening the flyout. The default is <see cref="DesktopFlyoutActivationMode.Activate"/>.</value>
        [GeneratedDependencyProperty(DefaultValue = DesktopFlyoutActivationMode.Activate)]
        public partial DesktopFlyoutActivationMode ActivationMode { get; set; }

        /// <summary>
        /// Identifies the <see cref="AutoCloseDelay"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoCloseDelayProperty =
            DependencyProperty.Register(nameof(AutoCloseDelay), typeof(TimeSpan), typeof(DesktopFlyout), new PropertyMetadata(TimeSpan.Zero, OnAutoCloseDelayPropertyChanged));

        /// <summary>
        /// Gets or sets the delay before the flyout closes automatically.
        /// </summary>
        /// <value>The delay before automatic close. The default is <see cref="TimeSpan.Zero"/>.</value>
        /// <remarks>
        /// Set to <see cref="TimeSpan.Zero"/> or a negative value to disable automatic close. The
        /// timer starts after the flyout has opened and restarts when the property changes while open.
        /// </remarks>
        public TimeSpan AutoCloseDelay
        {
            get => (TimeSpan)GetValue(AutoCloseDelayProperty);
            set => SetValue(AutoCloseDelayProperty, value);
        }

        /// <summary>
        /// Gets or sets the backdrop material used by flyout islands.
        /// </summary>
        /// <value>The backdrop material used by flyout islands. The default is <see cref="DesktopFlyoutBackdropKind.DesktopAcrylic"/>.</value>
        /// <remarks>
        /// Windows App SDK builds create library-owned system backdrop instances from this value.
        /// UWP builds keep the property for API compatibility, but do not create a Windows App SDK
        /// system backdrop. Custom <c>SystemBackdrop</c> instances are intentionally not exposed so
        /// flyout islands can keep a stable transient surface when the host window loses activation.
        /// </remarks>
        [GeneratedDependencyProperty(DefaultValue = DesktopFlyoutBackdropKind.DesktopAcrylic)]
        public partial DesktopFlyoutBackdropKind BackdropKind { get; set; }

        private static void OnFlyoutSizePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is DesktopFlyout flyout)
                flyout.UpdateOpenFlyoutLayout();
        }

        private static void OnAutoCloseDelayPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is DesktopFlyout flyout)
                flyout.RestartAutoCloseTimer();
        }

        partial void OnIslandsSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not IEnumerable<DesktopFlyoutIsland> newIslands)
                return;

            Islands.Clear();

            foreach (var island in newIslands)
                Islands.Add(island);

            UpdateIslands();
        }

        partial void OnIslandsOrientationPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (ReferenceEquals(e.NewValue, e.OldValue))
                return;

            UpdateIslands();
            UpdateOpenFlyoutLayout();
        }

        partial void OnIsBackdropEnabledPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (ReferenceEquals(e.NewValue, e.OldValue))
                return;

            UpdateIslandBackdrops();
        }

        partial void OnBackdropKindPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((DesktopFlyoutBackdropKind)e.NewValue == (DesktopFlyoutBackdropKind)e.OldValue)
                return;

            UpdateIslandBackdrops();
        }

        partial void OnActivationModePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((DesktopFlyoutActivationMode)e.NewValue == (DesktopFlyoutActivationMode)e.OldValue)
                return;

            _host?.SetActivationMode((DesktopFlyoutActivationMode)e.NewValue);
            UpdateFocusSuppression();
        }
    }
}
