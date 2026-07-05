#pragma once

#include "SystemTrayIcon.g.h"

#include <optional>

namespace winrt::DesktopFlyouts::implementation
{
    struct SystemTrayIcon : SystemTrayIconT<SystemTrayIcon>
    {
        SystemTrayIcon(winrt::hstring const& iconPath, winrt::hstring const& tooltip, winrt::guid const& id);
        SystemTrayIcon(winrt::hstring const& iconPath, winrt::hstring const& tooltip, winrt::guid const& id, bool isVisible);
        ~SystemTrayIcon();

        winrt::hstring IconPath() const;
        void IconPath(winrt::hstring const& value);

        winrt::hstring Tooltip() const;
        void Tooltip(winrt::hstring const& value);

        bool IsVisible() const noexcept;
        void IsVisible(bool value);

        winrt::guid Id() const noexcept;

        void Show();
        void Destroy();

        winrt::event_token IconDestroyed(winrt::Windows::Foundation::TypedEventHandler<winrt::DesktopFlyouts::SystemTrayIcon, winrt::Windows::Foundation::IInspectable> const& handler);
        void IconDestroyed(winrt::event_token const& token) noexcept;

        winrt::event_token LeftClicked(winrt::Windows::Foundation::TypedEventHandler<winrt::DesktopFlyouts::SystemTrayIcon, winrt::DesktopFlyouts::MouseEventReceivedEventArgs> const& handler);
        void LeftClicked(winrt::event_token const& token) noexcept;

        winrt::event_token RightClicked(winrt::Windows::Foundation::TypedEventHandler<winrt::DesktopFlyouts::SystemTrayIcon, winrt::DesktopFlyouts::MouseEventReceivedEventArgs> const& handler);
        void RightClicked(winrt::event_token const& token) noexcept;

    private:
        static constexpr uint32_t WM_UNIQUE_MESSAGE = WM_APP + 0x5BFA;

        static wchar_t const* WindowClassName() noexcept;
        static void EnsureWindowClass();
        static LRESULT CALLBACK WindowProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept;

        void CreateMessageWindow();
        void DestroyMessageWindow() noexcept;
        void FillNotifyIconData(NOTIFYICONDATAW& data, HICON hIcon = nullptr) const noexcept;
        std::optional<winrt::Windows::Foundation::Point> GetCenterPointOfTrayIcon() const noexcept;
        LRESULT InstanceWindowProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam);
        void DestroyCurrentIcon() noexcept;
        void RaiseMouseEvent(winrt::event<winrt::Windows::Foundation::TypedEventHandler<winrt::DesktopFlyouts::SystemTrayIcon, winrt::DesktopFlyouts::MouseEventReceivedEventArgs>>& mouseEvent);

        winrt::hstring m_iconPath;
        winrt::hstring m_tooltip;
        winrt::guid m_id{};
        uint32_t m_taskbarRestartMessageId{};
        HWND m_hWnd{};
        HICON m_currentHIcon{};
        bool m_isVisible{ true };
        bool m_created{};

        winrt::event<winrt::Windows::Foundation::TypedEventHandler<winrt::DesktopFlyouts::SystemTrayIcon, winrt::Windows::Foundation::IInspectable>> m_iconDestroyed;
        winrt::event<winrt::Windows::Foundation::TypedEventHandler<winrt::DesktopFlyouts::SystemTrayIcon, winrt::DesktopFlyouts::MouseEventReceivedEventArgs>> m_leftClicked;
        winrt::event<winrt::Windows::Foundation::TypedEventHandler<winrt::DesktopFlyouts::SystemTrayIcon, winrt::DesktopFlyouts::MouseEventReceivedEventArgs>> m_rightClicked;
    };
}

namespace winrt::DesktopFlyouts::factory_implementation
{
    struct SystemTrayIcon : SystemTrayIconT<SystemTrayIcon, implementation::SystemTrayIcon>
    {
    };
}
