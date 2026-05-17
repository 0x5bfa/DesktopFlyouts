// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace U5BFA.Libraries
{
    internal partial class RootViewModel : ObservableObject
    {
        [ObservableProperty]
        internal partial bool IsModifiable { get; set; } = true;

        [ObservableProperty]
        internal partial string? IconPath { get; set; }

        [ObservableProperty]
        internal partial string? TooltipText { get; set; }

        [ObservableProperty]
        internal partial bool IsBackdropEnabled { get; set; }

        [ObservableProperty]
        internal partial bool HideOnLostFocus { get; set; }

        [ObservableProperty]
        internal partial double AutoCloseDelaySecondsValue { get; set; }

        [ObservableProperty]
        internal partial int SelectedPopupDirectionIndex { get; set; }

        [ObservableProperty]
        internal partial int SelectedFlyoutPlacementIndex { get; set; }

        [ObservableProperty]
        internal partial int SelectedFlyoutExampleIndex { get; set; }

        [ObservableProperty]
        internal partial double FlyoutWidthValue { get; set; } = 360D;

        [ObservableProperty]
        internal partial double FlyoutHeightValue { get; set; } = double.NaN;

        [ObservableProperty]
        internal partial int SelectedBackdropIndex { get; set; }

        public Dictionary<TrayIconFlyoutPopupDirection, string> PopupDirections { get; private set; } = [];
        public Dictionary<FlyoutPlacementMode, string> FlyoutPlacements { get; private set; } = [];
        public Dictionary<FlyoutSampleKinds, string> FlyoutExamples { get; private set; } = [];
        public Dictionary<BackdropKind, string> Backdrops { get; private set; } = [];

        internal RootViewModel()
        {
            IconPath = TrayIconManager.Default.SystemTrayIcon?.IconPath;
            TooltipText = TrayIconManager.Default.SystemTrayIcon?.Tooltip;

            IsBackdropEnabled = true;
            HideOnLostFocus = true;

            FlyoutExamples.Add(FlyoutSampleKinds.DefaultStyle, "Default");
            FlyoutExamples.Add(FlyoutSampleKinds.StickySmallStyle, "Sticky small");
            FlyoutExamples.Add(FlyoutSampleKinds.StartMenuStyle, "Start menu");
            FlyoutExamples.Add(FlyoutSampleKinds.WidgetStyle, "Widget");
            FlyoutExamples.Add(FlyoutSampleKinds.IndicatorStyle, "Indicator");
            SelectedFlyoutExampleIndex = 0;

            PopupDirections.Add(TrayIconFlyoutPopupDirection.Vertical, "Vertical");
            PopupDirections.Add(TrayIconFlyoutPopupDirection.BottomToTop, "Bottom to top");
            PopupDirections.Add(TrayIconFlyoutPopupDirection.TopToBottom, "Top to bottom");
            PopupDirections.Add(TrayIconFlyoutPopupDirection.Horizontal, "Horizontal");
            PopupDirections.Add(TrayIconFlyoutPopupDirection.LeftToRight, "Left to right");
            PopupDirections.Add(TrayIconFlyoutPopupDirection.RightToLeft, "Right to left");
            SelectedPopupDirectionIndex = 0;

            FlyoutPlacements.Add(FlyoutPlacementMode.TopEdgeAlignedLeft, "Top left");
            FlyoutPlacements.Add(FlyoutPlacementMode.Top, "Top center");
            FlyoutPlacements.Add(FlyoutPlacementMode.TopEdgeAlignedRight, "Top right");
            FlyoutPlacements.Add(FlyoutPlacementMode.BottomEdgeAlignedLeft, "Bottom left");
            FlyoutPlacements.Add(FlyoutPlacementMode.Bottom, "Bottom center");
            FlyoutPlacements.Add(FlyoutPlacementMode.BottomEdgeAlignedRight, "Bottom right");
            SelectedFlyoutPlacementIndex = 5;

            Backdrops.Add(BackdropKind.Acrylic, "Acrylic");
            Backdrops.Add(BackdropKind.Mica, "Mica");
            SelectedBackdropIndex = 0;
        }

        partial void OnIconPathChanged(string? value)
        {
            TrayIconManager.Default.SystemTrayIcon?.IconPath = value ?? string.Empty;
        }

        partial void OnTooltipTextChanged(string? value)
        {
            TrayIconManager.Default.SystemTrayIcon?.Tooltip = value ?? string.Empty;
        }

        partial void OnIsBackdropEnabledChanged(bool value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.TrayIconFlyout?.IsBackdropEnabled = value;
        }

        partial void OnHideOnLostFocusChanged(bool value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.TrayIconFlyout?.HideOnLostFocus = value;
        }

        partial void OnAutoCloseDelaySecondsValueChanged(double value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.TrayIconFlyout!.AutoCloseDelay = ToAutoCloseDelay(value);
        }

        partial void OnSelectedPopupDirectionIndexChanged(int value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.TrayIconFlyout?.PopupDirection = PopupDirections.ElementAt(value).Key;
        }

        partial void OnSelectedFlyoutPlacementIndexChanged(int value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.TrayIconFlyout?.Placement = FlyoutPlacements.ElementAt(value).Key;
        }

        partial void OnSelectedFlyoutExampleIndexChanged(int value)
        {
            var flyoutKind = FlyoutExamples.ElementAt(value).Key;

            TrayIconManager.Default.SwitchFlyout(flyoutKind);
            if (flyoutKind is FlyoutSampleKinds.DefaultStyle)
            {
                IsModifiable = true;
                ApplyDefaultFlyoutSettings();
            }
            else
            {
                IsModifiable = false;
            }
        }

        partial void OnSelectedBackdropIndexChanged(int value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.TrayIconFlyout?.BackdropKind = Backdrops.ElementAt(value).Key;
        }

        partial void OnFlyoutWidthValueChanged(double value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.TrayIconFlyout!.FlyoutWidth = ToGridLength(value);
        }

        partial void OnFlyoutHeightValueChanged(double value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.TrayIconFlyout!.FlyoutHeight = ToGridLength(value);
        }

        private void ApplyDefaultFlyoutSettings()
        {
            if (!IsDefaultFlyoutSelected())
                return;

            var flyout = TrayIconManager.Default.TrayIconFlyout!;
            flyout.BackdropKind = Backdrops.ElementAt(SelectedBackdropIndex).Key;
            flyout.FlyoutHeight = ToGridLength(FlyoutHeightValue);
            flyout.FlyoutWidth = ToGridLength(FlyoutWidthValue);
            flyout.AutoCloseDelay = ToAutoCloseDelay(AutoCloseDelaySecondsValue);
            flyout.HideOnLostFocus = HideOnLostFocus;
            flyout.IsBackdropEnabled = IsBackdropEnabled;
            flyout.Placement = FlyoutPlacements.ElementAt(SelectedFlyoutPlacementIndex).Key;
            flyout.PopupDirection = PopupDirections.ElementAt(SelectedPopupDirectionIndex).Key;
        }

        private static bool IsDefaultFlyoutSelected()
        {
            return TrayIconManager.Default.TrayIconFlyout is MainTrayIconFlyout;
        }

        private static GridLength ToGridLength(double value)
        {
            return double.IsNaN(value) || value <= 0
                ? GridLength.Auto
                : new(value);
        }

        private static TimeSpan ToAutoCloseDelay(double value)
        {
            return double.IsNaN(value) || value <= 0
                ? TimeSpan.Zero
                : TimeSpan.FromSeconds(value);
        }
    }
}
