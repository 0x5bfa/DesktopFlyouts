#pragma once

#include <cstdint>
#include <memory>

#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Microsoft.UI.Xaml.Controls.Primitives.h>
#include <winrt/Microsoft.UI.Xaml.Controls.h>
#include <winrt/Microsoft.UI.Xaml.Hosting.h>
#include <winrt/author/base.h>
#undef GetCurrentTime

namespace winrt::DesktopFlyouts::author
{
    struct DesktopMenuFlyoutNativeState;

    // A desktop-hosted MenuFlyout. The MenuFlyout remains a normal WinUI
    // MenuFlyout; this class only supplies the desktop HWND/XAML-island host
    // needed to show it at a screen coordinate.
    struct DesktopMenuFlyout :
        winrt::author::runtimeclass<
            winrt::Microsoft::UI::Xaml::Controls::ItemsControl,
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

        winrt::Microsoft::UI::Xaml::Controls::MenuFlyout MenuFlyout(winrt::author::getter = {});
        winrt::author::setter MenuFlyout(winrt::Microsoft::UI::Xaml::Controls::MenuFlyout const& value);

        bool IsOpen(winrt::author::getter = {});
        static winrt::Microsoft::UI::Xaml::DependencyProperty IsOpenProperty(
            winrt::author::getter = {});

        void Show(winrt::Windows::Foundation::Point point);
        void ShowAt(std::int32_t x, std::int32_t y);
        bool TryPreTranslateMessage(std::int64_t message);
        void Hide();

    private:
        void EnsureHost();
        void RebuildMenuFlyoutItems();
        void SetIsOpen(bool value);

        std::int64_t m_ownerWindowHandle{};
        winrt::Microsoft::UI::Xaml::Controls::MenuFlyout m_menuFlyout{ nullptr };
        std::unique_ptr<DesktopMenuFlyoutNativeState> m_native;
        bool m_isClosed{};
    };
}
