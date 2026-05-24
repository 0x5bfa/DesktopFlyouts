#pragma once

#include "DesktopFlyoutIslandTemplateSettings.g.h"

namespace winrt::DesktopFlyouts::implementation
{
    struct DesktopFlyoutIslandTemplateSettings : DesktopFlyoutIslandTemplateSettingsT<DesktopFlyoutIslandTemplateSettings>
    {
        DesktopFlyoutIslandTemplateSettings() = default;

        static winrt::Microsoft::UI::Xaml::DependencyProperty BackdropCornerRadiusProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty SystemBackdropProperty();

        winrt::Microsoft::UI::Xaml::CornerRadius BackdropCornerRadius();
        winrt::Microsoft::UI::Xaml::Media::SystemBackdrop SystemBackdrop();

        void SetBackdropCornerRadius(winrt::Microsoft::UI::Xaml::CornerRadius const& value);
        void SetSystemBackdrop(winrt::Microsoft::UI::Xaml::Media::SystemBackdrop const& value);

    private:
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_backdropCornerRadiusProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_systemBackdropProperty;
    };
}

namespace winrt::DesktopFlyouts::factory_implementation
{
    struct DesktopFlyoutIslandTemplateSettings : DesktopFlyoutIslandTemplateSettingsT<DesktopFlyoutIslandTemplateSettings, implementation::DesktopFlyoutIslandTemplateSettings>
    {
    };
}
