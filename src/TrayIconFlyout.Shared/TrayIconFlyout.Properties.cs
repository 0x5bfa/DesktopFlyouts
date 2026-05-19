// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
#endif

namespace U5BFA.Libraries
{
    public partial class TrayIconFlyout
    {
        private readonly ObservableCollection<TrayIconFlyoutIsland> _islands = [];

        /// <summary>
        /// Gets the islands displayed in the flyout.
        /// </summary>
        public IList<TrayIconFlyoutIsland> Islands => _islands;

        /// <summary>
        /// Gets or sets a source collection for flyout islands.
        /// </summary>
        [GeneratedDependencyProperty]
        public partial object? IslandsSource { get; set; }

        /// <summary>
        /// Gets or sets whether island backdrops are enabled.
        /// </summary>
        [GeneratedDependencyProperty(DefaultValue = true)]
        public partial bool IsBackdropEnabled { get; set; }

        /// <summary>
        /// Gets whether the flyout is currently open.
        /// </summary>
        [GeneratedDependencyProperty]
        public partial bool IsOpen { get; private set; }

        /// <summary>
        /// Identifies the <see cref="FlyoutWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FlyoutWidthProperty =
            DependencyProperty.Register(nameof(FlyoutWidth), typeof(GridLength), typeof(TrayIconFlyout), new PropertyMetadata(GridLength.Auto, OnFlyoutSizePropertyChanged));

        /// <summary>
        /// Gets or sets the requested flyout width.
        /// </summary>
        public GridLength FlyoutWidth
        {
            get => (GridLength)GetValue(FlyoutWidthProperty);
            set => SetValue(FlyoutWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FlyoutHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FlyoutHeightProperty =
            DependencyProperty.Register(nameof(FlyoutHeight), typeof(GridLength), typeof(TrayIconFlyout), new PropertyMetadata(GridLength.Auto, OnFlyoutSizePropertyChanged));

        /// <summary>
        /// Gets or sets the requested flyout height.
        /// </summary>
        public GridLength FlyoutHeight
        {
            get => (GridLength)GetValue(FlyoutHeightProperty);
            set => SetValue(FlyoutHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the preferred popup direction.
        /// </summary>
        [GeneratedDependencyProperty(DefaultValue = FlyoutPopupDirection.Vertical)]
        public partial FlyoutPopupDirection PopupDirection { get; set; }

        /// <summary>
        /// Gets or sets how islands are arranged.
        /// </summary>
        [GeneratedDependencyProperty(DefaultValue = Orientation.Vertical)]
        public partial Orientation IslandsOrientation { get; set; }

        /// <summary>
        /// Gets or sets the flyout placement on the work area.
        /// </summary>
        [GeneratedDependencyProperty(DefaultValue = FlyoutPlacementMode.BottomRight)]
        public partial FlyoutPlacementMode Placement { get; set; }

        /// <summary>
        /// Gets or sets the menu flyout associated with the tray icon.
        /// </summary>
        [GeneratedDependencyProperty]
        public partial MenuFlyout? MenuFlyout { get; set; }

        /// <summary>
        /// Gets or sets whether open and close transitions are enabled.
        /// </summary>
        [GeneratedDependencyProperty(DefaultValue = true)]
        public partial bool IsTransitionAnimationEnabled { get; set; }

        /// <summary>
        /// Gets or sets the scale applied while the flyout is pressed. Set to 1.0 to disable press scaling.
        /// </summary>
        [GeneratedDependencyProperty(DefaultValue = 1.0D)]
        public partial double PressedScale { get; set; }

        /// <summary>
        /// Gets or sets whether the flyout closes when it loses focus.
        /// </summary>
        [GeneratedDependencyProperty(DefaultValue = true)]
        public partial bool HideOnLostFocus { get; set; }

        /// <summary>
        /// Gets or sets how the flyout participates in activation and focus.
        /// </summary>
        [GeneratedDependencyProperty(DefaultValue = FlyoutActivationMode.Activate)]
        public partial FlyoutActivationMode ActivationMode { get; set; }

        /// <summary>
        /// Identifies the <see cref="AutoCloseDelay"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoCloseDelayProperty =
            DependencyProperty.Register(nameof(AutoCloseDelay), typeof(TimeSpan), typeof(TrayIconFlyout), new PropertyMetadata(TimeSpan.Zero, OnAutoCloseDelayPropertyChanged));

        /// <summary>
        /// Gets or sets the delay before the flyout closes automatically.
        /// </summary>
        public TimeSpan AutoCloseDelay
        {
            get => (TimeSpan)GetValue(AutoCloseDelayProperty);
            set => SetValue(AutoCloseDelayProperty, value);
        }

        /// <summary>
        /// Gets or sets the backdrop kind used by flyout islands.
        /// </summary>
        [GeneratedDependencyProperty(DefaultValue = BackdropKind.Acrylic)]
        public partial BackdropKind BackdropKind { get; set; }

        private static void OnFlyoutSizePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is TrayIconFlyout flyout)
                flyout.UpdateOpenFlyoutLayout();
        }

        private static void OnAutoCloseDelayPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is TrayIconFlyout flyout)
                flyout.RestartAutoCloseTimer();
        }

        partial void OnIslandsSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not IEnumerable<TrayIconFlyoutIsland> newIslands)
                return;

            Islands.Clear();

            foreach (var island in newIslands)
                Islands.Add(island);

            UpdateIslands();
        }

        partial void OnIslandsOrientationPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((Orientation)e.NewValue == (Orientation)e.OldValue)
                return;

            UpdateIslands();
            UpdateOpenFlyoutLayout();
        }

        partial void OnIsBackdropEnabledPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == (bool)e.OldValue)
                return;

#if WASDK
            UpdateBackdropManager(true);
#endif
        }

        partial void OnBackdropKindPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((BackdropKind)e.NewValue == (BackdropKind)e.OldValue)
                return;

#if WASDK
            UpdateBackdropManager(true);
#endif
        }

        partial void OnActivationModePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((FlyoutActivationMode)e.NewValue == (FlyoutActivationMode)e.OldValue)
                return;

            _host?.SetActivationMode((FlyoutActivationMode)e.NewValue);
            UpdateFocusSuppression();
        }
    }
}
