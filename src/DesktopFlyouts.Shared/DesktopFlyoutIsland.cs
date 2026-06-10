// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace DesktopFlyouts
{
    /// <summary>
    /// Represents a content section inside a <see cref="DesktopFlyout"/>.
    /// </summary>
    /// <remarks>
    /// Islands are arranged by their owner flyout according to
    /// <see cref="DesktopFlyout.IslandsOrientation"/>. Put the XAML content for one visual section
    /// in each island.
    /// </remarks>
#if WASDK
    [WinRT.GeneratedBindableCustomProperty([nameof(TemplateSettings)], [])]
#endif
    public partial class DesktopFlyoutIsland : ContentControl
    {
        private WeakReference<DesktopFlyout>? _owner;
        private long _propertyChangedCallbackTokenForCornerRadiusProperty;

        /// <summary>
        /// Gets an object that provides calculated values that can be referenced from the island template.
        /// </summary>
        /// <value>The calculated template settings for this island.</value>
        public DesktopFlyoutIslandTemplateSettings TemplateSettings { get; } = new();

        /// <summary>
        /// Initializes a new instance of <see cref="DesktopFlyoutIsland"/>.
        /// </summary>
        /// <remarks>
        /// The island applies its default style when it is loaded by the owning XAML framework.
        /// </remarks>
        public DesktopFlyoutIsland()
        {
            DefaultStyleKey = typeof(DesktopFlyoutIsland);
            RegisterPropertyChangedCallback(VisibilityProperty, (s, e) => ((DesktopFlyoutIsland)s).OnIslandVisibilityChanged());
            UpdateTemplateSettings();
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Unloaded += DesktopFlyoutIsland_Unloaded;

            _propertyChangedCallbackTokenForCornerRadiusProperty = RegisterPropertyChangedCallback(CornerRadiusProperty, (s, e) => ((DesktopFlyoutIsland)s).OnCornerRadiusChanged());

            UpdateTemplateSettings();
        }

        internal void SetOwner(DesktopFlyout owner)
        {
            _owner = new(owner);

#if WASDK
            UpdateOwnerBackdrop();
#endif
        }

        private static void OnIslandSizePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not DesktopFlyoutIsland island)
                return;

            if (island._owner is not null && island._owner.TryGetTarget(out var owner))
                owner.OnIslandSizeChanged();
        }

        private void OnCornerRadiusChanged()
        {
            UpdateTemplateSettings();
        }

        private void OnIslandVisibilityChanged()
        {
            if (_owner is not null && _owner.TryGetTarget(out var owner))
                owner.OnIslandVisibilityChanged();
        }

        private void UpdateTemplateSettings()
        {
#if WASDK
            TemplateSettings.BackdropCornerRadius = new(
                GetBackdropCornerRadius(CornerRadius.TopLeft),
                GetBackdropCornerRadius(CornerRadius.TopRight),
                GetBackdropCornerRadius(CornerRadius.BottomRight),
                GetBackdropCornerRadius(CornerRadius.BottomLeft));
#endif
        }

#if WASDK
        private static double GetBackdropCornerRadius(double cornerRadius)
        {
            return Math.Max(0D, cornerRadius > 0D ? cornerRadius - 1D : 0D);
        }

        internal void UpdateOwnerBackdrop()
        {
            TemplateSettings.SystemBackdrop = _owner is not null && _owner.TryGetTarget(out var owner)
                ? owner.CreateIslandSystemBackdrop()
                : null;
        }

        internal void ClearOwnerBackdrop()
        {
            TemplateSettings.SystemBackdrop = null;
        }
#endif

        private void DesktopFlyoutIsland_Unloaded(object sender, RoutedEventArgs e)
        {
#if WASDK
            ClearOwnerBackdrop();
#endif

            UnregisterPropertyChangedCallback(CornerRadiusProperty, _propertyChangedCallbackTokenForCornerRadiusProperty);

            Unloaded -= DesktopFlyoutIsland_Unloaded;
        }
    }
}
