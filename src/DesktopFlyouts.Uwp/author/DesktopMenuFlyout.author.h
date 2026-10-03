#pragma once

#include <cstdint>
#include <memory>

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.UI.Xaml.h>
#include <winrt/Windows.UI.Xaml.Controls.h>
#include <winrt/Windows.UI.Xaml.Hosting.h>
#include <winrt/author/base.h>

namespace winrt::DesktopFlyouts::author
{
    struct DesktopMenuFlyoutNativeState;

    struct DesktopMenuFlyout :
        winrt::author::runtimeclass<
            winrt::Windows::UI::Xaml::Controls::ItemsControl,
            winrt::Windows::Foundation::IClosable,
            winrt::author::internal<winrt::non_agile>>,
        winrt::author::apply_attr<
            winrt::author::contentproperty,
            winrt::author::attr_string{ "Items" }>,
        winrt::author::unsealed
    {
        DesktopMenuFlyout();
        ~DesktopMenuFlyout();
        void Close(winrt::author::override = {});

        std::int64_t OwnerWindowHandle(winrt::author::getter = {});
        winrt::author::setter OwnerWindowHandle(std::int64_t value);

        winrt::Windows::UI::Xaml::Controls::MenuFlyout MenuFlyout(winrt::author::getter = {});
        winrt::author::setter MenuFlyout(winrt::Windows::UI::Xaml::Controls::MenuFlyout const& value);

        bool IsOpen(winrt::author::getter = {});
        static winrt::Windows::UI::Xaml::DependencyProperty IsOpenProperty(winrt::author::getter = {});

        void Show(winrt::Windows::Foundation::Point point);
        void ShowAt(std::int32_t x, std::int32_t y);
        bool TryPreTranslateMessage(std::int64_t message);
        void Hide();

    private:
        void EnsureHost();
        void RebuildMenuFlyoutItems();
        void SetIsOpen(bool value);

        std::int64_t m_ownerWindowHandle{};
        winrt::Windows::UI::Xaml::Controls::MenuFlyout m_menuFlyout{ nullptr };
        std::unique_ptr<DesktopMenuFlyoutNativeState> m_native;
        bool m_isClosed{};
    };
}

