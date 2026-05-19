// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

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
        internal partial int SelectedActivationModeIndex { get; set; }

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

        public Dictionary<FlyoutPopupDirection, string> PopupDirections { get; private set; } = [];
        public Dictionary<FlyoutPlacementMode, string> FlyoutPlacements { get; private set; } = [];
        public Dictionary<FlyoutActivationMode, string> ActivationModes { get; private set; } = [];
        public Dictionary<FlyoutSampleKinds, string> FlyoutExamples { get; private set; } = [];
        public Dictionary<BackdropKind, string> Backdrops { get; private set; } = [];

        public ICommand ToggleFlyoutOpenCommand { get; }

        internal RootViewModel()
        {
            IconPath = TrayIconManager.Default.SystemTrayIcon?.IconPath;
            TooltipText = TrayIconManager.Default.SystemTrayIcon?.Tooltip;

            IsBackdropEnabled = true;
            HideOnLostFocus = true;

            ActivationModes.Add(FlyoutActivationMode.Activate, "Activate");
            ActivationModes.Add(FlyoutActivationMode.NoActivateOnOpen, "No activate on open");
            ActivationModes.Add(FlyoutActivationMode.NeverActivate, "Never activate");
            SelectedActivationModeIndex = 0;

            FlyoutExamples.Add(FlyoutSampleKinds.Customizable, "Default");
            FlyoutExamples.Add(FlyoutSampleKinds.Button, "Button");
            FlyoutExamples.Add(FlyoutSampleKinds.IndicatorStyle, "Indicator");
            FlyoutExamples.Add(FlyoutSampleKinds.NotificationCenterStyle, "Notification Center");
            FlyoutExamples.Add(FlyoutSampleKinds.StartMenuStyle, "Start Menu");
            FlyoutExamples.Add(FlyoutSampleKinds.StickySmallStyle, "Sticky small");
            FlyoutExamples.Add(FlyoutSampleKinds.WidgetStyle, "Widget");
            FlyoutExamples.Add(FlyoutSampleKinds.Severity, "Severity");
            SelectedFlyoutExampleIndex = 0;

            PopupDirections.Add(FlyoutPopupDirection.Vertical, "Vertical");
            PopupDirections.Add(FlyoutPopupDirection.BottomToTop, "Bottom to top");
            PopupDirections.Add(FlyoutPopupDirection.TopToBottom, "Top to bottom");
            PopupDirections.Add(FlyoutPopupDirection.Horizontal, "Horizontal");
            PopupDirections.Add(FlyoutPopupDirection.LeftToRight, "Left to right");
            PopupDirections.Add(FlyoutPopupDirection.RightToLeft, "Right to left");
            SelectedPopupDirectionIndex = 0;

            FlyoutPlacements.Add(FlyoutPlacementMode.TopLeft, "Top left");
            FlyoutPlacements.Add(FlyoutPlacementMode.TopCenter, "Top center");
            FlyoutPlacements.Add(FlyoutPlacementMode.TopRight, "Top right");
            FlyoutPlacements.Add(FlyoutPlacementMode.BottomLeft, "Bottom left");
            FlyoutPlacements.Add(FlyoutPlacementMode.BottomCenter, "Bottom center");
            FlyoutPlacements.Add(FlyoutPlacementMode.BottomRight, "Bottom right");
            SelectedFlyoutPlacementIndex = 5;

            Backdrops.Add(BackdropKind.Acrylic, "Acrylic");
            Backdrops.Add(BackdropKind.Mica, "Mica");
            SelectedBackdropIndex = 0;

            ToggleFlyoutOpenCommand = new RelayCommand(ExecuteToggleFlyoutOpenCommand);
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

        partial void OnSelectedActivationModeIndexChanged(int value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.TrayIconFlyout!.ActivationMode = ActivationModes.ElementAt(value).Key;
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
            if (flyoutKind is FlyoutSampleKinds.Customizable)
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
            flyout.ActivationMode = ActivationModes.ElementAt(SelectedActivationModeIndex).Key;
            flyout.HideOnLostFocus = HideOnLostFocus;
            flyout.IsBackdropEnabled = IsBackdropEnabled;
            flyout.Placement = FlyoutPlacements.ElementAt(SelectedFlyoutPlacementIndex).Key;
            flyout.PopupDirection = PopupDirections.ElementAt(SelectedPopupDirectionIndex).Key;
        }

        private static bool IsDefaultFlyoutSelected()
        {
            return TrayIconManager.Default.TrayIconFlyout is CustomizableFlyout;
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

        private void ExecuteToggleFlyoutOpenCommand()
        {
            TrayIconManager.Default.ToggleFlyout();
        }
    }
}
