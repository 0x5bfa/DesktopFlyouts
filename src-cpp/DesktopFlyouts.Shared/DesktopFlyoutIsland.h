#pragma once

#include "DesktopFlyoutIsland.g.h"

namespace winrt::DesktopFlyouts::implementation
{
    struct DesktopFlyoutIsland : DesktopFlyoutIslandT<DesktopFlyoutIsland>
    {
        DesktopFlyoutIsland();
        ~DesktopFlyoutIsland();

        static winrt::Microsoft::UI::Xaml::DependencyProperty IslandWidthProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty IslandHeightProperty();

        winrt::Microsoft::UI::Xaml::GridLength IslandWidth();
        void IslandWidth(winrt::Microsoft::UI::Xaml::GridLength const& value);
        winrt::Microsoft::UI::Xaml::GridLength IslandHeight();
        void IslandHeight(winrt::Microsoft::UI::Xaml::GridLength const& value);
        winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings TemplateSettings();

    private:
        void UpdateTemplateSettings();

        static winrt::Microsoft::UI::Xaml::DependencyProperty s_islandWidthProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_islandHeightProperty;
        winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings m_templateSettings{ nullptr };
        int64_t m_cornerRadiusToken{};
    };
}

namespace winrt::DesktopFlyouts::factory_implementation
{
    struct DesktopFlyoutIsland : DesktopFlyoutIslandT<DesktopFlyoutIsland, implementation::DesktopFlyoutIsland>
    {
    };
}
