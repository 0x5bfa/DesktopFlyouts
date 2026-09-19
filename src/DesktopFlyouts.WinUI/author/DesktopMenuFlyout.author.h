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
    struct DesktopMenuFlyout : winrt::author::runtimeclass<winrt::author::internal<winrt::non_agile>>
    {
        DesktopMenuFlyout();
        ~DesktopMenuFlyout();

        std::int64_t OwnerWindowHandle(winrt::author::getter = {});
        winrt::author::setter OwnerWindowHandle(std::int64_t value);

        winrt::Microsoft::UI::Xaml::Controls::MenuFlyout MenuFlyout(winrt::author::getter = {});
        winrt::author::setter MenuFlyout(winrt::Microsoft::UI::Xaml::Controls::MenuFlyout const& value);

        winrt::Windows::Foundation::Collections::IObservableVector<
            winrt::Microsoft::UI::Xaml::Controls::MenuFlyoutItemBase> Items(
                winrt::author::getter = {});

        bool IsOpen(winrt::author::getter = {});

        void ShowAt(std::int32_t x, std::int32_t y);
        void Hide();

    private:
        void EnsureHost();
        void RebuildMenuFlyoutItems();

        std::int64_t m_ownerWindowHandle{};
        winrt::Microsoft::UI::Xaml::Controls::MenuFlyout m_menuFlyout{ nullptr };
        winrt::Windows::Foundation::Collections::IObservableVector<
            winrt::Microsoft::UI::Xaml::Controls::MenuFlyoutItemBase> m_items{ nullptr };
        winrt::event_token m_itemsChangedToken{};
        bool m_isOpen{};
        std::unique_ptr<DesktopMenuFlyoutNativeState> m_native;
    };
}
