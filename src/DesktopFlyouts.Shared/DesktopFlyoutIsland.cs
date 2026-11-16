// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Windows.Foundation;

#if UWP
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
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
#if WASDK && !HAS_UNO
    [WinRT.GeneratedBindableCustomProperty([nameof(TemplateSettings)], [])]
#endif
    public partial class DesktopFlyoutIsland : ContentControl
    {
        private const double LayoutEpsilon = 0.5D;

        private WeakReference<DesktopFlyout>? _owner;
        private long _propertyChangedCallbackTokenForCornerRadiusProperty;
        private Size _lastActualSize;
        private Size _lastDesiredSize;
        private bool _hasLayoutSnapshot;
#if UWP
        private Brush? _ownerBackground;
        private bool _hasOwnerBackground;
#endif

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
            LayoutUpdated += DesktopFlyoutIsland_LayoutUpdated;
            UpdateTemplateSettings();
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Unloaded += DesktopFlyoutIsland_Unloaded;

            _propertyChangedCallbackTokenForCornerRadiusProperty = RegisterPropertyChangedCallback(CornerRadiusProperty, (s, e) => ((DesktopFlyoutIsland)s).OnCornerRadiusChanged());

            UpdateTemplateSettings();
#if UWP
            UpdateOwnerBackdrop();
#endif
        }

        internal void SetOwner(DesktopFlyout owner)
        {
            _owner = new(owner);

#if WASDK && !HAS_UNO
            UpdateOwnerBackdrop();
#elif UWP
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

        private void DesktopFlyoutIsland_LayoutUpdated(object? sender, object e)
        {
            var actualSize = new Size(ActualWidth, ActualHeight);
            var desiredSize = DesiredSize;
            if (_hasLayoutSnapshot &&
                AreClose(_lastActualSize, actualSize) &&
                AreClose(_lastDesiredSize, desiredSize))
            {
                return;
            }

            _hasLayoutSnapshot = true;
            _lastActualSize = actualSize;
            _lastDesiredSize = desiredSize;

            if (_owner is not null && _owner.TryGetTarget(out var owner))
                owner.OnIslandLayoutChanged();
        }

        private void UpdateTemplateSettings()
        {
#if WASDK && !HAS_UNO
            TemplateSettings.BackdropCornerRadius = new(
                GetBackdropCornerRadius(CornerRadius.TopLeft),
                GetBackdropCornerRadius(CornerRadius.TopRight),
                GetBackdropCornerRadius(CornerRadius.BottomRight),
                GetBackdropCornerRadius(CornerRadius.BottomLeft));
#endif
        }

#if WASDK && !HAS_UNO
        private static double GetBackdropCornerRadius(double cornerRadius)
        {
            return Math.Max(0D, cornerRadius > 0D ? cornerRadius - 1D : 0D);
        }

        internal void UpdateOwnerBackdrop(bool forceUpdate = false)
        {
            var backdrop = _owner is not null && _owner.TryGetTarget(out var owner)
                ? owner.CreateIslandSystemBackdrop(RequestedTheme, forceUpdate ? null : TemplateSettings.SystemBackdrop)
                : null;

            if (backdrop is null)
                ClearOwnerBackdrop();
            else
                TemplateSettings.SystemBackdrop = backdrop;
        }

        internal void ClearOwnerBackdrop()
        {
            if (TemplateSettings.SystemBackdrop is DesktopFlyoutSystemBackdrop backdrop)
                backdrop.ClearFrom(TemplateSettings);
            else
                TemplateSettings.SystemBackdrop = null;
        }
#elif UWP
        internal void UpdateOwnerBackdrop()
        {
            Brush? background = null;
            if (_owner is not null
                && _owner.TryGetTarget(out var owner)
                && owner.IsBackdropEnabled
                && owner.BackdropKind is not DesktopFlyoutBackdropKind.Mica
                && GetTemplateChild("PART_RootGrid") is Grid templateRoot)
            {
                var themeKey = new AccessibilitySettings().HighContrast
                    ? "HighContrast"
                    : RequestedTheme switch
                    {
                        ElementTheme.Light => "Light",
                        ElementTheme.Dark => "Default",
                        _ => GeneralHelpers.IsTaskbarLight() ? "Light" : "Default",
                    };
                var brushKey = GeneralHelpers.IsTaskbarColorPrevalenceEnabled()
                    ? "SystemControlAcrylicAccentElementBrush"
                    : "ShellSurfaceBackgroundBrush";
                var themeResources = (ResourceDictionary)templateRoot.Resources.ThemeDictionaries[themeKey];

                background = themeResources[brushKey] as Brush
                    ?? throw new InvalidOperationException($"Could not find {brushKey} in the {themeKey} theme.");
            }

            if (background is null)
            {
                ClearOwnerBackground();
                return;
            }

            var localBackground = ReadLocalValue(BackgroundProperty);
            if (!ReferenceEquals(localBackground, DependencyProperty.UnsetValue)
                && (!_hasOwnerBackground || !ReferenceEquals(localBackground, _ownerBackground)))
            {
                return;
            }

            Background = background;
            _ownerBackground = background;
            _hasOwnerBackground = true;
        }

        private void ClearOwnerBackground()
        {
            if (_hasOwnerBackground && ReferenceEquals(ReadLocalValue(BackgroundProperty), _ownerBackground))
                ClearValue(BackgroundProperty);

            _ownerBackground = null;
            _hasOwnerBackground = false;
        }
#endif

        private static bool AreClose(Size first, Size second)
        {
            return Math.Abs(first.Width - second.Width) < LayoutEpsilon &&
                Math.Abs(first.Height - second.Height) < LayoutEpsilon;
        }

        private void DesktopFlyoutIsland_Unloaded(object sender, RoutedEventArgs e)
        {
#if WASDK && !HAS_UNO
            ClearOwnerBackdrop();
#endif

            UnregisterPropertyChangedCallback(CornerRadiusProperty, _propertyChangedCallbackTokenForCornerRadiusProperty);

            Unloaded -= DesktopFlyoutIsland_Unloaded;
        }
    }
}
