#include "pch.h"
#include "DesktopFlyoutIslandTemplateSettings.h"
#if __has_include("DesktopFlyoutIslandTemplateSettings.g.cpp")
#include "DesktopFlyoutIslandTemplateSettings.g.cpp"
#endif

namespace winrt::DesktopFlyouts::implementation
{
    winrt::Microsoft::UI::Xaml::DependencyProperty DesktopFlyoutIslandTemplateSettings::s_backdropCornerRadiusProperty =
        winrt::Microsoft::UI::Xaml::DependencyProperty::Register(
            L"BackdropCornerRadius",
            winrt::xaml_typename<winrt::Microsoft::UI::Xaml::CornerRadius>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings>(),
            winrt::Microsoft::UI::Xaml::PropertyMetadata{ winrt::box_value(winrt::Microsoft::UI::Xaml::CornerRadius{}) });

    winrt::Microsoft::UI::Xaml::DependencyProperty DesktopFlyoutIslandTemplateSettings::s_systemBackdropProperty =
        winrt::Microsoft::UI::Xaml::DependencyProperty::Register(
            L"SystemBackdrop",
            winrt::xaml_typename<winrt::Microsoft::UI::Xaml::Media::SystemBackdrop>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings>(),
            winrt::Microsoft::UI::Xaml::PropertyMetadata{ nullptr });

    winrt::Microsoft::UI::Xaml::DependencyProperty DesktopFlyoutIslandTemplateSettings::BackdropCornerRadiusProperty()
    {
        return s_backdropCornerRadiusProperty;
    }

    winrt::Microsoft::UI::Xaml::DependencyProperty DesktopFlyoutIslandTemplateSettings::SystemBackdropProperty()
    {
        return s_systemBackdropProperty;
    }

    winrt::Microsoft::UI::Xaml::CornerRadius DesktopFlyoutIslandTemplateSettings::BackdropCornerRadius()
    {
        return winrt::unbox_value<winrt::Microsoft::UI::Xaml::CornerRadius>(GetValue(s_backdropCornerRadiusProperty));
    }

    winrt::Microsoft::UI::Xaml::Media::SystemBackdrop DesktopFlyoutIslandTemplateSettings::SystemBackdrop()
    {
        return GetValue(s_systemBackdropProperty).try_as<winrt::Microsoft::UI::Xaml::Media::SystemBackdrop>();
    }

    void DesktopFlyoutIslandTemplateSettings::SetBackdropCornerRadius(winrt::Microsoft::UI::Xaml::CornerRadius const& value)
    {
        SetValue(s_backdropCornerRadiusProperty, winrt::box_value(value));
    }

    void DesktopFlyoutIslandTemplateSettings::SetSystemBackdrop(winrt::Microsoft::UI::Xaml::Media::SystemBackdrop const& value)
    {
        SetValue(s_systemBackdropProperty, value);
    }
}
