// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopFlyouts.SampleApp.Shared;
using System.ComponentModel;
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

        private bool _initializing = true;

        [ObservableProperty]
        internal partial bool HasError { get; set; }

        [ObservableProperty]
        internal partial string ErrorMessage { get; set; } = string.Empty;

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
            SystemBackdrops.Add(DesktopFlyoutBackdropKind.DesktopAcrylic, "Acrylic");
            SelectedSystemBackdropIndex = 0;

            ToggleFlyoutOpenCommand = new RelayCommand(ExecuteToggleFlyoutOpenCommand);
            _initializing = false;
            TrayIconManager.Default.OperationFailed += (_, message) => ShowError(message);
            TrayIconManager.Default.SwitchFlyout(DesktopFlyoutSampleKind.Customizable, GetSettings());
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (_initializing)
                return;

            try
            {
                switch (e.PropertyName)
                {
                    case nameof(IconPath):
                        TrayIconManager.Default.SystemTrayIcon?.SetIcon(IconPath ?? string.Empty);
                        break;
                    case nameof(TooltipText):
                        TrayIconManager.Default.SystemTrayIcon?.Tooltip = TooltipText ?? string.Empty;
                        break;
                    case nameof(SelectedFlyoutExampleIndex):
                        if (SelectedFlyoutExampleIndex < 0 || SelectedFlyoutExampleIndex >= FlyoutExamples.Count)
                            return;
                        var example = FlyoutExamples.ElementAt(SelectedFlyoutExampleIndex).Key;
                        IsModifiable = example is DesktopFlyoutSampleKind.Customizable;
                        TrayIconManager.Default.SwitchFlyout(example, GetSettings());
                        break;
                    case nameof(HideOnLostFocus):
                    case nameof(IsSwipeToDismissEnabled):
                    case nameof(SwipeDismissThresholdValue):
                    case nameof(SelectedActivationModeIndex):
                    case nameof(AutoCloseDelaySecondsValue):
                    case nameof(FlyoutWidthValue):
                    case nameof(FlyoutHeightValue):
                    case nameof(IsBackdropEnabled):
                    case nameof(SelectedSystemBackdropIndex):
                        if (IsModifiable)
                            TrayIconManager.Default.UpdateSettings(GetSettings());
                        break;
                }
            }
            catch (Exception exception)
            {
                ShowError(exception.Message);
            }
        }

        private FlyoutSettings GetSettings() => new(
            HideOnLostFocus, IsSwipeToDismissEnabled,
            double.IsFinite(SwipeDismissThresholdValue) && SwipeDismissThresholdValue > 0 ? SwipeDismissThresholdValue : 80,
            SelectedActivationModeIndex >= 0 && SelectedActivationModeIndex < ActivationModes.Count
                ? ActivationModes.ElementAt(SelectedActivationModeIndex).Key : DesktopFlyoutActivationMode.Activate,
            AutoCloseDelaySecondsValue, FlyoutWidthValue, FlyoutHeightValue,
            IsBackdropEnabled,
            SelectedSystemBackdropIndex >= 0 && SelectedSystemBackdropIndex < SystemBackdrops.Count
                ? SystemBackdrops.ElementAt(SelectedSystemBackdropIndex).Key
                : DesktopFlyoutBackdropKind.Mica);

        private void ExecuteToggleFlyoutOpenCommand()
        {
            if (SelectedPositionIndex >= 0 && SelectedPositionIndex < FlyoutPositions.Count)
                TrayIconManager.Default.ToggleFlyout(FlyoutPositions.ElementAt(SelectedPositionIndex).Key);
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }
    }
}
