// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace DesktopFlyouts
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
        internal partial bool IsSwipeToDismissEnabled { get; set; }

        [ObservableProperty]
        internal partial double SwipeDismissThresholdValue { get; set; } = 80D;

        [ObservableProperty]
        internal partial int SelectedActivationModeIndex { get; set; }

        [ObservableProperty]
        internal partial double AutoCloseDelaySecondsValue { get; set; }

        [ObservableProperty]
        internal partial int SelectedPositionIndex { get; set; }

        [ObservableProperty]
        internal partial int SelectedFlyoutExampleIndex { get; set; }

        [ObservableProperty]
        internal partial double FlyoutWidthValue { get; set; } = 360D;

        [ObservableProperty]
        internal partial double FlyoutHeightValue { get; set; } = double.NaN;

        [ObservableProperty]
        internal partial int SelectedSystemBackdropIndex { get; set; }

        public Dictionary<DesktopFlyoutPosition, string> FlyoutPositions { get; private set; } = [];
        public Dictionary<DesktopFlyoutActivationMode, string> ActivationModes { get; private set; } = [];
        public Dictionary<DesktopFlyoutSampleKind, string> FlyoutExamples { get; private set; } = [];
        public Dictionary<DesktopFlyoutBackdropKind, string> SystemBackdrops { get; private set; } = [];

        public ICommand ToggleFlyoutOpenCommand { get; }

        internal RootViewModel()
        {
            IconPath = TrayIconManager.Default.SystemTrayIcon?.IconPath;
            TooltipText = TrayIconManager.Default.SystemTrayIcon?.Tooltip;

            IsBackdropEnabled = true;
            HideOnLostFocus = true;

            ActivationModes.Add(DesktopFlyoutActivationMode.Activate, "Activate");
            ActivationModes.Add(DesktopFlyoutActivationMode.NoActivateOnOpen, "No activate on open");
            ActivationModes.Add(DesktopFlyoutActivationMode.NeverActivate, "Never activate");
            SelectedActivationModeIndex = 0;

            FlyoutExamples.Add(DesktopFlyoutSampleKind.Customizable, "Default");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.Button, "Button");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.IndicatorStyle, "Indicator");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.NotificationCenterStyle, "Notification Center");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.StartMenuStyle, "Start Menu");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.StickySmallStyle, "Sticky small");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.WidgetStyle, "Widget");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.Severity, "Severity");
            SelectedFlyoutExampleIndex = 0;

            FlyoutPositions.Add(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.TopLeft, DesktopFlyoutMonitorSelection.Cursor), "Top left");
            FlyoutPositions.Add(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.TopCenter, DesktopFlyoutMonitorSelection.Cursor), "Top center");
            FlyoutPositions.Add(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.TopRight, DesktopFlyoutMonitorSelection.Cursor), "Top right");
            FlyoutPositions.Add(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.LeftCenter, DesktopFlyoutMonitorSelection.Cursor), "Left center");
            FlyoutPositions.Add(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.RightCenter, DesktopFlyoutMonitorSelection.Cursor), "Right center");
            FlyoutPositions.Add(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomLeft, DesktopFlyoutMonitorSelection.Cursor), "Bottom left");
            FlyoutPositions.Add(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomCenter, DesktopFlyoutMonitorSelection.Cursor), "Bottom center");
            FlyoutPositions.Add(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight, DesktopFlyoutMonitorSelection.Cursor), "Bottom right");
            FlyoutPositions.Add(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight, DesktopFlyoutMonitorSelection.Cursor, DesktopFlyoutPopupDirection.RightToLeft), "Bottom right (from right)");
            SelectedPositionIndex = FlyoutPositions.Count - 2;

            SystemBackdrops.Add(DesktopFlyoutBackdropKind.Mica, "Mica");
            SystemBackdrops.Add(DesktopFlyoutBackdropKind.DesktopAcrylic, "Desktop Acrylic");
            SelectedSystemBackdropIndex = 0;

            ToggleFlyoutOpenCommand = new RelayCommand(ExecuteToggleFlyoutOpenCommand);
        }

        partial void OnIconPathChanged(string? value)
        {
            TrayIconManager.Default.SystemTrayIcon?.SetIcon(value ?? string.Empty);
        }

        partial void OnTooltipTextChanged(string? value)
        {
            TrayIconManager.Default.SystemTrayIcon?.Tooltip = value ?? string.Empty;
        }

        partial void OnIsBackdropEnabledChanged(bool value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.DesktopFlyout?.IsBackdropEnabled = value;
        }

        partial void OnHideOnLostFocusChanged(bool value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.DesktopFlyout?.HideOnLostFocus = value;
        }

        partial void OnIsSwipeToDismissEnabledChanged(bool value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.DesktopFlyout!.IsSwipeToDismissEnabled = value;
        }

        partial void OnSwipeDismissThresholdValueChanged(double value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.DesktopFlyout!.SwipeDismissThreshold = value;
        }

        partial void OnAutoCloseDelaySecondsValueChanged(double value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.DesktopFlyout!.AutoCloseDelay = ToAutoCloseDelay(value);
        }

        partial void OnSelectedActivationModeIndexChanged(int value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.DesktopFlyout!.ActivationMode = ActivationModes.ElementAt(value).Key;
        }

        partial void OnSelectedFlyoutExampleIndexChanged(int value)
        {
            var flyoutKind = FlyoutExamples.ElementAt(value).Key;

            TrayIconManager.Default.SwitchFlyout(flyoutKind);
            if (flyoutKind is DesktopFlyoutSampleKind.Customizable)
            {
                IsModifiable = true;
                ApplyDefaultFlyoutSettings();
            }
            else
            {
                IsModifiable = false;
            }
        }

        partial void OnSelectedSystemBackdropIndexChanged(int value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.DesktopFlyout!.BackdropKind = SystemBackdrops.ElementAt(value).Key;
        }

        partial void OnFlyoutWidthValueChanged(double value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.DesktopFlyout!.FlyoutWidth = ToGridLength(value);
        }

        partial void OnFlyoutHeightValueChanged(double value)
        {
            if (IsDefaultFlyoutSelected())
                TrayIconManager.Default.DesktopFlyout!.FlyoutHeight = ToGridLength(value);
        }

        private void ApplyDefaultFlyoutSettings()
        {
            if (!IsDefaultFlyoutSelected())
                return;

            var flyout = TrayIconManager.Default.DesktopFlyout!;
            flyout.BackdropKind = SystemBackdrops.ElementAt(SelectedSystemBackdropIndex).Key;
            flyout.FlyoutHeight = ToGridLength(FlyoutHeightValue);
            flyout.FlyoutWidth = ToGridLength(FlyoutWidthValue);
            flyout.AutoCloseDelay = ToAutoCloseDelay(AutoCloseDelaySecondsValue);
            flyout.ActivationMode = ActivationModes.ElementAt(SelectedActivationModeIndex).Key;
            flyout.HideOnLostFocus = HideOnLostFocus;
            flyout.IsSwipeToDismissEnabled = IsSwipeToDismissEnabled;
            flyout.SwipeDismissThreshold = SwipeDismissThresholdValue;
            flyout.IsBackdropEnabled = IsBackdropEnabled;
        }

        private static bool IsDefaultFlyoutSelected()
        {
            return TrayIconManager.Default.DesktopFlyout is CustomizableFlyout;
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
            TrayIconManager.Default.ToggleFlyout(position: FlyoutPositions.ElementAt(SelectedPositionIndex).Key);
        }
    }
}
