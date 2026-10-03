#pragma once

#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windef.h>

#include <cstdint>
#include <memory>

#include <winrt/Windows.Foundation.h>
#include <winrt/author/base.h>
#ifndef IDLGEN_CPP_STATIC_REFLECTION_PHASE
#include <winrt/DesktopFlyouts.h>
#endif
#undef GetCurrentTime

namespace winrt::DesktopFlyouts::author
{
    struct SystemTrayIconNativeState;

    struct MouseEventReceivedEventArgs : winrt::author::runtimeclass<winrt::author::internal<winrt::non_agile>>
    {
        MouseEventReceivedEventArgs(winrt::Windows::Foundation::Point point);

        winrt::Windows::Foundation::Point Point(winrt::author::getter = {});

    private:
        winrt::Windows::Foundation::Point m_point{};
    };

    struct SystemTrayIcon : winrt::author::runtimeclass<
        winrt::Windows::Foundation::IClosable,
        winrt::author::internal<winrt::non_agile>>
    {
        SystemTrayIcon();
        SystemTrayIcon(
            winrt::hstring iconPath,
            winrt::hstring tooltip,
            winrt::guid id);
        static
#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
            SystemTrayIcon
#else
            winrt::DesktopFlyouts::SystemTrayIcon
#endif
            CreateFromIconHandle(
                std::int64_t iconHandle,
                winrt::hstring tooltip,
                winrt::guid id);
        ~SystemTrayIcon();
        void Close(winrt::author::override = {});

        winrt::hstring IconPath(winrt::author::getter = {});
        winrt::hstring Tooltip(winrt::author::getter = {});
        winrt::author::setter Tooltip(winrt::hstring value);
        bool IsVisible(winrt::author::getter = {});
        winrt::author::setter IsVisible(bool value);
        winrt::guid Id(winrt::author::getter = {});

        winrt::event_token LeftClicked(
            winrt::Windows::Foundation::EventHandler<
#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
                MouseEventReceivedEventArgs
#else
                winrt::DesktopFlyouts::MouseEventReceivedEventArgs
#endif
            > const& handler);
        void LeftClicked(winrt::event_token token);
        winrt::event_token RightClicked(
            winrt::Windows::Foundation::EventHandler<
#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
                MouseEventReceivedEventArgs
#else
                winrt::DesktopFlyouts::MouseEventReceivedEventArgs
#endif
            > const& handler);
        void RightClicked(winrt::event_token token);
        winrt::event_token LeftDoubleClicked(
            winrt::Windows::Foundation::EventHandler<
#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
                MouseEventReceivedEventArgs
#else
                winrt::DesktopFlyouts::MouseEventReceivedEventArgs
#endif
            > const& handler);
        void LeftDoubleClicked(winrt::event_token token);
        winrt::event_token RightDoubleClicked(
            winrt::Windows::Foundation::EventHandler<
#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
                MouseEventReceivedEventArgs
#else
                winrt::DesktopFlyouts::MouseEventReceivedEventArgs
#endif
            > const& handler);
        void RightDoubleClicked(winrt::event_token token);

        void RaiseLeftClicked(
#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
            MouseEventReceivedEventArgs const& args,
#else
            winrt::DesktopFlyouts::MouseEventReceivedEventArgs const& args,
#endif
            winrt::author::ignore = {});
        void RaiseRightClicked(
#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
            MouseEventReceivedEventArgs const& args,
#else
            winrt::DesktopFlyouts::MouseEventReceivedEventArgs const& args,
#endif
            winrt::author::ignore = {});
        void RaiseLeftDoubleClicked(
#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
            MouseEventReceivedEventArgs const& args,
#else
            winrt::DesktopFlyouts::MouseEventReceivedEventArgs const& args,
#endif
            winrt::author::ignore = {});
        void RaiseRightDoubleClicked(
#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
            MouseEventReceivedEventArgs const& args,
#else
            winrt::DesktopFlyouts::MouseEventReceivedEventArgs const& args,
#endif
            winrt::author::ignore = {});

        void Show();
        void Hide();
        void SetIcon(winrt::hstring iconPath);
        void SetIconHandle(std::int64_t iconHandle);
        void Destroy();

    private:
        static void RegisterWindowClass();
        static LRESULT CALLBACK WindowProc(HWND window, UINT message, WPARAM wParam, LPARAM lParam) noexcept;
        void InitializeCallbackWindow();

        winrt::hstring m_iconPath{};
        winrt::hstring m_tooltip{};
        winrt::guid m_id{};
        bool m_isVisible{};
        bool m_isClosed{};
        std::unique_ptr<SystemTrayIconNativeState> m_native;
        winrt::event<winrt::Windows::Foundation::EventHandler<
 #ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
            MouseEventReceivedEventArgs
 #else
            winrt::DesktopFlyouts::MouseEventReceivedEventArgs
 #endif
            >> m_leftClicked;
        winrt::event<winrt::Windows::Foundation::EventHandler<
 #ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
            MouseEventReceivedEventArgs
 #else
            winrt::DesktopFlyouts::MouseEventReceivedEventArgs
 #endif
            >> m_rightClicked;
        winrt::event<winrt::Windows::Foundation::EventHandler<
 #ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
            MouseEventReceivedEventArgs
 #else
            winrt::DesktopFlyouts::MouseEventReceivedEventArgs
 #endif
            >> m_leftDoubleClicked;
        winrt::event<winrt::Windows::Foundation::EventHandler<
 #ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
            MouseEventReceivedEventArgs
 #else
            winrt::DesktopFlyouts::MouseEventReceivedEventArgs
 #endif
            >> m_rightDoubleClicked;

        winrt::Windows::Foundation::Point TrayIconPoint() const noexcept;
        void RestoreAfterTaskbarRestart() noexcept;
    };
}
