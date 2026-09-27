#pragma once

#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windef.h>
#include <windows.ui.xaml.hosting.desktopwindowxamlsource.h>

#include "author/DesktopFlyout.author.h"

#include <functional>

#include <winrt/Windows.UI.Xaml.Hosting.h>

namespace winrt::DesktopFlyouts::detail
{
    class DesktopFlyoutHost final
    {
    public:
        DesktopFlyoutHost() = default;
        DesktopFlyoutHost(DesktopFlyoutHost const&) = delete;
        DesktopFlyoutHost& operator=(DesktopFlyoutHost const&) = delete;
        ~DesktopFlyoutHost();

        void EnsureCreated(HWND ownerWindow, author::DesktopFlyoutActivationMode activationMode);
        void Destroy() noexcept;

        void OwnerWindow(HWND ownerWindow) noexcept;
        void ActivationMode(author::DesktopFlyoutActivationMode value) noexcept;
        void HideOnLostFocus(bool value) noexcept;
        void IsOpen(bool value) noexcept;

        void Content(Windows::UI::Xaml::UIElement const& content);
        void Hide() noexcept;
        void MoveAndResize(int x, int y, int width, int height);
        void Show(author::DesktopFlyoutActivationMode activationMode) noexcept;

        void ConfigureAutoCloseTimer(Windows::Foundation::TimeSpan delay) noexcept;
        void StopAutoCloseTimer() noexcept;

        void PreserveActivationState() noexcept;
        void RestoreActivationState() noexcept;
        bool NavigateFocus(Windows::UI::Xaml::Hosting::XamlSourceFocusNavigationReason reason) noexcept;
        bool TryPreTranslateMessage(MSG const* message) noexcept;

        void HideCallback(std::function<void()> callback);

        HWND Window() const noexcept;
        HWND IslandWindow() const noexcept;
        double RasterizationScale() const noexcept;
        Windows::UI::Xaml::Hosting::DesktopWindowXamlSource XamlSource() const noexcept;

    private:
        static void RegisterWindowClass();
        static LRESULT CALLBACK WindowProc(HWND window, UINT message, WPARAM wParam, LPARAM lParam) noexcept;
        static void EnsureXamlManager();

        HWND m_ownerWindow{};
        HWND m_window{};
        HWND m_islandWindow{};
        Windows::UI::Xaml::Hosting::DesktopWindowXamlSource m_xamlSource{ nullptr };
        std::function<void()> m_hideCallback;
        author::DesktopFlyoutActivationMode m_activationMode{ author::DesktopFlyoutActivationMode::activate };
        bool m_hideOnLostFocus{ true };
        bool m_isOpen{};
        UINT_PTR m_autoCloseTimerId{};
        HWND m_preservedForegroundWindow{};
        HWND m_preservedActiveWindow{};
        HWND m_preservedFocusWindow{};
    };
}

