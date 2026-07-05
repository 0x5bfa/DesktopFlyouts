#pragma once

#include "DesktopMenuFlyout.g.h"
#include "XamlIslandHostWindow.h"

namespace winrt::DesktopFlyouts::implementation
{
    struct DesktopMenuFlyout : DesktopMenuFlyoutT<DesktopMenuFlyout>
    {
        DesktopMenuFlyout();
        ~DesktopMenuFlyout();

        static winrt::Microsoft::UI::Xaml::DependencyProperty IsOpenProperty();
        bool IsOpen();
        void Show(winrt::Windows::Foundation::Point const& point);
        void Hide();
        void Close();
        void OnApplyTemplate();

    private:
        void RebuildMenu();
        void UpdateTheme();

        static winrt::Microsoft::UI::Xaml::DependencyProperty s_isOpenProperty;
        std::unique_ptr<winrt::DesktopFlyouts::details::XamlIslandHostWindow> m_host;
        winrt::Microsoft::UI::Xaml::Controls::Border m_target{ nullptr };
        winrt::Microsoft::UI::Xaml::Controls::MenuFlyout m_menu{ nullptr };
        winrt::event_token m_closedToken{};
        bool m_closed{};
    };
}

namespace winrt::DesktopFlyouts::factory_implementation
{
    struct DesktopMenuFlyout : DesktopMenuFlyoutT<DesktopMenuFlyout, implementation::DesktopMenuFlyout>
    {
    };
}
