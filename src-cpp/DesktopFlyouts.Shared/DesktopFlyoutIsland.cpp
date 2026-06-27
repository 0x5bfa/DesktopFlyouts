#include "pch.h"
#include "DesktopFlyoutIsland.h"
#include "DesktopFlyoutIslandTemplateSettings.h"
#include "DesktopFlyout.h"
#if __has_include("DesktopFlyoutIsland.g.cpp")
#include "DesktopFlyoutIsland.g.cpp"
#endif

namespace winrt::DesktopFlyouts::implementation
{
    winrt::Microsoft::UI::Xaml::DependencyProperty DesktopFlyoutIsland::s_islandWidthProperty =
        winrt::Microsoft::UI::Xaml::DependencyProperty::Register(
            L"IslandWidth",
            winrt::xaml_typename<winrt::Microsoft::UI::Xaml::GridLength>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIsland>(),
            winrt::Microsoft::UI::Xaml::PropertyMetadata{ winrt::box_value(winrt::Microsoft::UI::Xaml::GridLengthHelper::Auto()) });

    winrt::Microsoft::UI::Xaml::DependencyProperty DesktopFlyoutIsland::s_islandHeightProperty =
        winrt::Microsoft::UI::Xaml::DependencyProperty::Register(
            L"IslandHeight",
            winrt::xaml_typename<winrt::Microsoft::UI::Xaml::GridLength>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIsland>(),
            winrt::Microsoft::UI::Xaml::PropertyMetadata{ winrt::box_value(winrt::Microsoft::UI::Xaml::GridLengthHelper::Auto()) });

    DesktopFlyoutIsland::DesktopFlyoutIsland() :
        m_templateSettings(winrt::make<implementation::DesktopFlyoutIslandTemplateSettings>())
    {
        DefaultStyleKey(winrt::box_value(L"DesktopFlyouts.DesktopFlyoutIsland"));
        m_cornerRadiusToken = RegisterPropertyChangedCallback(
            winrt::Microsoft::UI::Xaml::Controls::Control::CornerRadiusProperty(),
            [this](auto const&, auto const&) { UpdateTemplateSettings(); });
        UpdateTemplateSettings();
    }

    DesktopFlyoutIsland::~DesktopFlyoutIsland()
    {
        if (m_cornerRadiusToken != 0)
        {
            UnregisterPropertyChangedCallback(winrt::Microsoft::UI::Xaml::Controls::Control::CornerRadiusProperty(), m_cornerRadiusToken);
        }
    }

    winrt::Microsoft::UI::Xaml::DependencyProperty DesktopFlyoutIsland::IslandWidthProperty()
    {
        return s_islandWidthProperty;
    }

    winrt::Microsoft::UI::Xaml::DependencyProperty DesktopFlyoutIsland::IslandHeightProperty()
    {
        return s_islandHeightProperty;
    }

    winrt::Microsoft::UI::Xaml::GridLength DesktopFlyoutIsland::IslandWidth()
    {
        return winrt::unbox_value<winrt::Microsoft::UI::Xaml::GridLength>(GetValue(s_islandWidthProperty));
    }

    void DesktopFlyoutIsland::IslandWidth(winrt::Microsoft::UI::Xaml::GridLength const& value)
    {
        SetValue(s_islandWidthProperty, winrt::box_value(value));
    }

    winrt::Microsoft::UI::Xaml::GridLength DesktopFlyoutIsland::IslandHeight()
    {
        return winrt::unbox_value<winrt::Microsoft::UI::Xaml::GridLength>(GetValue(s_islandHeightProperty));
    }

    void DesktopFlyoutIsland::IslandHeight(winrt::Microsoft::UI::Xaml::GridLength const& value)
    {
        SetValue(s_islandHeightProperty, winrt::box_value(value));
    }

    winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings DesktopFlyoutIsland::TemplateSettings()
    {
        return m_templateSettings;
    }

    void DesktopFlyoutIsland::UpdateTemplateSettings()
    {
        auto radius = CornerRadius();
        auto inner = [](double value) { return std::max(0.0, value - 1.0); };
        m_templateSettings.SetValue(
            winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings::BackdropCornerRadiusProperty(),
            winrt::box_value(winrt::Microsoft::UI::Xaml::CornerRadius{
                inner(radius.TopLeft), inner(radius.TopRight), inner(radius.BottomRight), inner(radius.BottomLeft) }));
    }
}
