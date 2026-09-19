#pragma once

#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windef.h>

#include <cstdint>
#include <memory>

#include <winrt/Windows.Foundation.h>
#include <winrt/author/base.h>
#undef GetCurrentTime

namespace winrt::DesktopFlyouts::author
{
    struct SystemTrayIconNativeState;

    struct SystemTrayIconEventArgs : winrt::author::runtimeclass<winrt::author::internal<winrt::non_agile>>
    {
        SystemTrayIconEventArgs(winrt::Windows::Foundation::Point point);

        winrt::Windows::Foundation::Point Point(winrt::author::getter = {});

    private:
        winrt::Windows::Foundation::Point m_point{};
    };

    struct SystemTrayIcon : winrt::author::runtimeclass<winrt::author::internal<winrt::non_agile>>
    {
        SystemTrayIcon();
        SystemTrayIcon(
            winrt::hstring iconPath,
            winrt::hstring tooltip,
            winrt::guid id);
        ~SystemTrayIcon();

        winrt::hstring IconPath(winrt::author::getter = {});
        winrt::hstring Tooltip(winrt::author::getter = {});
        winrt::author::setter Tooltip(winrt::hstring value);
        bool IsVisible(winrt::author::getter = {});
        winrt::author::setter IsVisible(bool value);
        winrt::guid Id(winrt::author::getter = {});

        winrt::event_token LeftClicked(
            winrt::Windows::Foundation::EventHandler<winrt::Windows::Foundation::IInspectable> const& handler);
        void LeftClicked(winrt::event_token token);
        winrt::event_token RightClicked(
            winrt::Windows::Foundation::EventHandler<winrt::Windows::Foundation::IInspectable> const& handler);
        void RightClicked(winrt::event_token token);
        winrt::event_token LeftDoubleClicked(
            winrt::Windows::Foundation::EventHandler<winrt::Windows::Foundation::IInspectable> const& handler);
        void LeftDoubleClicked(winrt::event_token token);
        winrt::event_token RightDoubleClicked(
            winrt::Windows::Foundation::EventHandler<winrt::Windows::Foundation::IInspectable> const& handler);
        void RightDoubleClicked(winrt::event_token token);

        void RaiseLeftClicked(
            winrt::Windows::Foundation::IInspectable const& args,
            winrt::author::ignore = {});
        void RaiseRightClicked(
            winrt::Windows::Foundation::IInspectable const& args,
            winrt::author::ignore = {});
        void RaiseLeftDoubleClicked(
            winrt::Windows::Foundation::IInspectable const& args,
            winrt::author::ignore = {});
        void RaiseRightDoubleClicked(
            winrt::Windows::Foundation::IInspectable const& args,
            winrt::author::ignore = {});

        void Show();
        void Hide();
        void SetIcon(winrt::hstring iconPath);
        void Destroy();

    private:
        static void RegisterWindowClass();
        static LRESULT CALLBACK WindowProc(HWND window, UINT message, WPARAM wParam, LPARAM lParam) noexcept;

        winrt::hstring m_iconPath{};
        winrt::hstring m_tooltip{};
        winrt::guid m_id{};
        bool m_isVisible{};
        std::unique_ptr<SystemTrayIconNativeState> m_native;
        winrt::event<winrt::Windows::Foundation::EventHandler<
            winrt::Windows::Foundation::IInspectable>> m_leftClicked;
        winrt::event<winrt::Windows::Foundation::EventHandler<
            winrt::Windows::Foundation::IInspectable>> m_rightClicked;
        winrt::event<winrt::Windows::Foundation::EventHandler<
            winrt::Windows::Foundation::IInspectable>> m_leftDoubleClicked;
        winrt::event<winrt::Windows::Foundation::EventHandler<
            winrt::Windows::Foundation::IInspectable>> m_rightDoubleClicked;

        winrt::Windows::Foundation::Point TrayIconPoint() const noexcept;
        void RestoreAfterTaskbarRestart() noexcept;
    };
}
