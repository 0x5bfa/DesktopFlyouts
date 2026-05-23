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

namespace U5BFA.Libraries
{
    /// <summary>
    /// Represents a content section inside a <see cref="DesktopFlyout"/>.
    /// </summary>
    /// <remarks>
    /// Islands are arranged by their owner flyout according to
    /// <see cref="DesktopFlyout.IslandsOrientation"/>. Put the XAML content for one visual section
    /// in each island.
    /// </remarks>
    public partial class DesktopFlyoutIsland : ContentControl
    {
        private const string PART_RootGrid = "PART_RootGrid";
        private const string PART_MainContentPresenter = "PART_MainContentPresenter";

        private Grid? RootGrid;
        private ContentPresenter? MainContentPresenter;

        private WeakReference<DesktopFlyout>? _owner;
        private long _propertyChangedCallbackTokenForContentProperty;
        private long _propertyChangedCallbackTokenForCornerRadiusProperty;

        /// <summary>
        /// Identifies the <see cref="TemplateSettings"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TemplateSettingsProperty =
            DependencyProperty.Register(nameof(TemplateSettings), typeof(DesktopFlyoutIslandTemplateSettings), typeof(DesktopFlyoutIsland), new PropertyMetadata(null));

        /// <summary>
        /// Gets an object that provides calculated values that can be referenced from the island template.
        /// </summary>
        /// <value>The calculated template settings for this island.</value>
        public DesktopFlyoutIslandTemplateSettings TemplateSettings
        {
            get => (DesktopFlyoutIslandTemplateSettings)GetValue(TemplateSettingsProperty);
            private set => SetValue(TemplateSettingsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IslandWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IslandWidthProperty =
            DependencyProperty.Register(nameof(IslandWidth), typeof(GridLength), typeof(DesktopFlyoutIsland), new PropertyMetadata(GridLength.Auto, OnIslandSizePropertyChanged));

        /// <summary>
        /// Gets or sets the island width.
        /// </summary>
        /// <value>The width used when the owner flyout arranges islands horizontally. The default is <see cref="GridLength.Auto"/>.</value>
        public GridLength IslandWidth
        {
            get => (GridLength)GetValue(IslandWidthProperty);
            set => SetValue(IslandWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IslandHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IslandHeightProperty =
            DependencyProperty.Register(nameof(IslandHeight), typeof(GridLength), typeof(DesktopFlyoutIsland), new PropertyMetadata(GridLength.Auto, OnIslandSizePropertyChanged));

        /// <summary>
        /// Gets or sets the island height.
        /// </summary>
        /// <value>The height used when the owner flyout arranges islands vertically. The default is <see cref="GridLength.Auto"/>.</value>
        public GridLength IslandHeight
        {
            get => (GridLength)GetValue(IslandHeightProperty);
            set => SetValue(IslandHeightProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DesktopFlyoutIsland"/>.
        /// </summary>
        /// <remarks>
        /// The island applies its default style when it is loaded by the owning XAML framework.
        /// </remarks>
        public DesktopFlyoutIsland()
        {
            DefaultStyleKey = typeof(DesktopFlyoutIsland);
            TemplateSettings = new();
            UpdateTemplateSettings();
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            RootGrid = GetTemplateChild(PART_RootGrid) as Grid
                ?? throw new MissingFieldException($"Could not find {PART_RootGrid} in the given {nameof(DesktopFlyoutIsland)}'s style.");
            MainContentPresenter = GetTemplateChild(PART_MainContentPresenter) as ContentPresenter
                ?? throw new MissingFieldException($"Could not find {PART_MainContentPresenter} in the given {nameof(DesktopFlyoutIsland)}'s style.");

            _propertyChangedCallbackTokenForContentProperty = RegisterPropertyChangedCallback(ContentProperty, (s, e) => ((DesktopFlyoutIsland)s).OnContentChanged());
            _propertyChangedCallbackTokenForCornerRadiusProperty = RegisterPropertyChangedCallback(CornerRadiusProperty, (s, e) => ((DesktopFlyoutIsland)s).OnCornerRadiusChanged());
            UpdateTemplateSettings();

            Unloaded += DesktopFlyoutIsland_Unloaded;
        }

        internal void SetOwner(DesktopFlyout owner)
        {
            _owner = new(owner);
        }

        private static void OnIslandSizePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not DesktopFlyoutIsland island)
                return;

            if (island._owner is not null && island._owner.TryGetTarget(out var owner))
                owner.OnIslandSizeChanged();
        }

        private void OnContentChanged()
        {
            MainContentPresenter?.Content = Content;
        }

        private void OnCornerRadiusChanged()
        {
            UpdateTemplateSettings();
        }

        private void UpdateTemplateSettings()
        {
            TemplateSettings.BackdropCornerRadius = new(
                CornerRadius.TopLeft > 0D ? CornerRadius.TopLeft : 0,
                CornerRadius.TopRight > 0D ? CornerRadius.TopRight : 0,
                CornerRadius.BottomRight > 0D ? CornerRadius.BottomRight : 0,
                CornerRadius.BottomLeft > 0D ? CornerRadius.BottomLeft : 0);
        }

        private void DesktopFlyoutIsland_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= DesktopFlyoutIsland_Unloaded;

            UnregisterPropertyChangedCallback(ContentProperty, _propertyChangedCallbackTokenForContentProperty);
            UnregisterPropertyChangedCallback(CornerRadiusProperty, _propertyChangedCallbackTokenForCornerRadiusProperty);
        }
    }
}
