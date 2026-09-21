#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#undef GetCurrentTime

#include "ModulePreamble.h"
#include "DesktopFlyoutHost.h"

#include <winrt/Microsoft.UI.Content.h>
#include <winrt/Microsoft.UI.Interop.h>

#include <algorithm>
#include <chrono>
#include <mutex>

namespace
{
    constexpr wchar_t c_hostWindowClassName[] = L"DesktopFlyouts.NativeFlyoutHost";
    constexpr wchar_t c_hostPropertyName[] = L"DesktopFlyouts.NativeFlyoutHost.Instance";
    constexpr UINT_PTR c_autoCloseTimerId = 0xDF01;

    void SetNoActivateStyle(HWND window, bool enabled) noexcept
    {
        if (window == nullptr)
        {
            return;
        }

        auto exStyle = static_cast<LONG_PTR>(GetWindowLongPtrW(window, GWL_EXSTYLE));
        if (enabled)
        {
            exStyle |= WS_EX_NOACTIVATE;
        }
        else
        {
            exStyle &= ~static_cast<LONG_PTR>(WS_EX_NOACTIVATE);
        }

        SetWindowLongPtrW(window, GWL_EXSTYLE, exStyle);
        SetWindowPos(
            window,
            nullptr,
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
    }
}

namespace winrt::DesktopFlyouts::detail
{
    DesktopFlyoutHost::~DesktopFlyoutHost()
    {
        Destroy();
    }

    void DesktopFlyoutHost::RegisterWindowClass()
    {
        static std::once_flag registered;
        std::call_once(registered, []
        {
            WNDCLASSEXW windowClass{};
            windowClass.cbSize = sizeof(windowClass);
            windowClass.hInstance = GetModuleHandleW(nullptr);
            windowClass.lpfnWndProc = &DesktopFlyoutHost::WindowProc;
            windowClass.lpszClassName = c_hostWindowClassName;
            windowClass.hCursor = LoadCursorW(nullptr, IDC_ARROW);
            winrt::check_bool(RegisterClassExW(&windowClass) != 0 || GetLastError() == ERROR_CLASS_ALREADY_EXISTS);
        });
    }

    LRESULT CALLBACK DesktopFlyoutHost::WindowProc(
        HWND window,
        UINT message,
        WPARAM wParam,
        LPARAM lParam) noexcept
    {
        auto* host = reinterpret_cast<DesktopFlyoutHost*>(GetWindowLongPtrW(window, GWLP_USERDATA));

        if (message == WM_NCCREATE)
        {
            auto* create = reinterpret_cast<CREATESTRUCTW*>(lParam);
            host = static_cast<DesktopFlyoutHost*>(create->lpCreateParams);
            SetWindowLongPtrW(window, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(host));
        }

        if (message == WM_MOUSEACTIVATE)
        {
            if (host != nullptr && host->m_activationMode == author::DesktopFlyoutActivationMode::never_activate)
            {
                host->RestoreActivationState();
                return MA_NOACTIVATE;
            }

            return MA_ACTIVATE;
        }

        if (message == WM_SETFOCUS &&
            host != nullptr &&
            host->m_activationMode == author::DesktopFlyoutActivationMode::never_activate)
        {
            host->RestoreActivationState();
            return 0;
        }

        if (message == WM_ACTIVATE &&
            LOWORD(wParam) == WA_INACTIVE &&
            host != nullptr &&
            host->m_isOpen &&
            host->m_hideOnLostFocus &&
            host->m_hideCallback)
        {
            host->m_hideCallback();
            return 0;
        }

        if (message == WM_KEYDOWN && wParam == VK_ESCAPE && host != nullptr)
        {
            if (host->m_hideCallback)
            {
                host->m_hideCallback();
            }
            else
            {
                ShowWindow(window, SW_HIDE);
            }
            return 0;
        }

        if (message == WM_TIMER && host != nullptr && wParam == host->m_autoCloseTimerId)
        {
            host->StopAutoCloseTimer();
            if (host->m_hideCallback)
            {
                host->m_hideCallback();
            }
            return 0;
        }

        if ((message == WM_SETTINGCHANGE || message == WM_THEMECHANGED) &&
            host != nullptr &&
            host->m_systemSettingsCallback)
        {
            host->m_systemSettingsCallback();
        }

        if (message == WM_NCDESTROY)
        {
            SetWindowLongPtrW(window, GWLP_USERDATA, 0);
        }

        return DefWindowProcW(window, message, wParam, lParam);
    }

    LRESULT CALLBACK DesktopFlyoutHost::IslandWindowProc(
        HWND window,
        UINT message,
        WPARAM wParam,
        LPARAM lParam) noexcept
    {
        auto* host = reinterpret_cast<DesktopFlyoutHost*>(GetPropW(window, c_hostPropertyName));
        if (host != nullptr && host->m_activationMode == author::DesktopFlyoutActivationMode::never_activate)
        {
            if (message == WM_MOUSEACTIVATE)
            {
                host->RestoreActivationState();
                return MA_NOACTIVATE;
            }

            if (message == WM_SETFOCUS)
            {
                host->RestoreActivationState();
                return 0;
            }
        }

        if (message == WM_KEYDOWN && wParam == VK_ESCAPE && host != nullptr)
        {
            if (host->m_hideCallback)
            {
                host->m_hideCallback();
            }
            return 0;
        }

        const auto previous = host != nullptr
            ? (window == host->m_inputWindow
                ? host->m_previousInputWindowProc
                : host->m_previousIslandWindowProc)
            : 0;
        if (previous != 0)
        {
            return CallWindowProcW(
                reinterpret_cast<WNDPROC>(previous),
                window,
                message,
                wParam,
                lParam);
        }

        return DefWindowProcW(window, message, wParam, lParam);
    }

    void DesktopFlyoutHost::SubclassIslandWindow() noexcept
    {
        if (m_islandWindow == nullptr || m_previousIslandWindowProc != 0 ||
            m_previousInputWindowProc != 0)
        {
            return;
        }

        if (!SetPropW(m_islandWindow, c_hostPropertyName, this))
        {
            return;
        }

        m_previousIslandWindowProc = SetWindowLongPtrW(
            m_islandWindow,
            GWLP_WNDPROC,
            reinterpret_cast<LONG_PTR>(&DesktopFlyoutHost::IslandWindowProc));
        if (m_previousIslandWindowProc == 0)
        {
            RemovePropW(m_islandWindow, c_hostPropertyName);
            return;
        }

        m_inputWindow = FindWindowExW(
            m_islandWindow,
            nullptr,
            L"InputSiteWindowClass",
            nullptr);
        if (m_inputWindow != nullptr && SetPropW(m_inputWindow, c_hostPropertyName, this))
        {
            m_previousInputWindowProc = SetWindowLongPtrW(
                m_inputWindow,
                GWLP_WNDPROC,
                reinterpret_cast<LONG_PTR>(&DesktopFlyoutHost::IslandWindowProc));
            if (m_previousInputWindowProc == 0)
            {
                RemovePropW(m_inputWindow, c_hostPropertyName);
                m_inputWindow = nullptr;
            }
        }
    }

    void DesktopFlyoutHost::UnsubclassIslandWindow() noexcept
    {
        if (m_islandWindow == nullptr)
        {
            m_previousIslandWindowProc = 0;
            m_previousInputWindowProc = 0;
            m_inputWindow = nullptr;
            return;
        }

        if (m_inputWindow != nullptr && m_previousInputWindowProc != 0)
        {
            SetWindowLongPtrW(
                m_inputWindow,
                GWLP_WNDPROC,
                m_previousInputWindowProc);
            m_previousInputWindowProc = 0;
            RemovePropW(m_inputWindow, c_hostPropertyName);
        }
        else if (m_inputWindow != nullptr)
        {
            RemovePropW(m_inputWindow, c_hostPropertyName);
        }
        m_inputWindow = nullptr;

        if (m_previousIslandWindowProc != 0)
        {
            SetWindowLongPtrW(
                m_islandWindow,
                GWLP_WNDPROC,
                m_previousIslandWindowProc);
            m_previousIslandWindowProc = 0;
        }

        RemovePropW(m_islandWindow, c_hostPropertyName);
    }

    void DesktopFlyoutHost::EnsureCreated(
        HWND ownerWindow,
        author::DesktopFlyoutActivationMode activationMode)
    {
        m_ownerWindow = ownerWindow;
        ActivationMode(activationMode);

        if (m_window != nullptr)
        {
            SetWindowLongPtrW(m_window, GWLP_HWNDPARENT, reinterpret_cast<LONG_PTR>(m_ownerWindow));
            return;
        }

        RegisterWindowClass();

        m_window = CreateWindowExW(
            WS_EX_NOREDIRECTIONBITMAP | WS_EX_TOOLWINDOW | WS_EX_TOPMOST |
                (activationMode == author::DesktopFlyoutActivationMode::never_activate
                ? WS_EX_NOACTIVATE
                : 0),
            c_hostWindowClassName,
            L"DesktopFlyout",
            WS_POPUP | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
            0,
            0,
            1,
            1,
            m_ownerWindow,
            nullptr,
            GetModuleHandleW(nullptr),
            this);
        winrt::check_bool(m_window != nullptr);

        m_xamlSource = Microsoft::UI::Xaml::Hosting::DesktopWindowXamlSource{};
        m_xamlSource.Initialize(winrt::Microsoft::UI::GetWindowIdFromWindow(m_window));
        m_islandWindow = winrt::Microsoft::UI::GetWindowFromWindowId(m_xamlSource.SiteBridge().WindowId());
        winrt::check_bool(m_islandWindow != nullptr);

        SetWindowLongPtrW(m_islandWindow, GWL_STYLE, WS_CHILD | WS_VISIBLE);
        SetNoActivateStyle(
            m_islandWindow,
            activationMode == author::DesktopFlyoutActivationMode::never_activate);
        SubclassIslandWindow();
    }

    void DesktopFlyoutHost::Destroy() noexcept
    {
        m_hideCallback = {};
        m_systemSettingsCallback = {};
        StopAutoCloseTimer();

        UnsubclassIslandWindow();

        if (m_xamlSource)
        {
            try
            {
                m_xamlSource.Close();
                m_xamlSource = nullptr;
            }
            catch (...)
            {
                m_xamlSource = nullptr;
            }
        }

        if (m_window != nullptr)
        {
            DestroyWindow(m_window);
            m_window = nullptr;
        }
        m_islandWindow = nullptr;
        m_ownerWindow = nullptr;
        m_isOpen = false;
    }

    void DesktopFlyoutHost::OwnerWindow(HWND ownerWindow) noexcept
    {
        m_ownerWindow = ownerWindow;
        if (m_window != nullptr)
        {
            SetWindowLongPtrW(m_window, GWLP_HWNDPARENT, reinterpret_cast<LONG_PTR>(m_ownerWindow));
        }
    }

    void DesktopFlyoutHost::ActivationMode(author::DesktopFlyoutActivationMode value) noexcept
    {
        m_activationMode = value;
        const auto noActivate = value == author::DesktopFlyoutActivationMode::never_activate;
        SetNoActivateStyle(m_window, noActivate);
        SetNoActivateStyle(m_islandWindow, noActivate);
    }

    void DesktopFlyoutHost::HideOnLostFocus(bool value) noexcept
    {
        m_hideOnLostFocus = value;
    }

    void DesktopFlyoutHost::IsOpen(bool value) noexcept
    {
        m_isOpen = value;
    }

    void DesktopFlyoutHost::Hide() noexcept
    {
        if (m_window != nullptr)
        {
            ShowWindow(m_window, SW_HIDE);
        }
    }

    void DesktopFlyoutHost::MoveAndResize(int x, int y, int width, int height)
    {
        winrt::check_bool(m_window != nullptr);
        winrt::check_bool(m_xamlSource != nullptr);

        const auto flags = (m_activationMode == author::DesktopFlyoutActivationMode::activate)
            ? 0U
            : SWP_NOACTIVATE;

        SetWindowPos(
            m_window,
            HWND_TOP,
            x,
            y,
            width,
            height,
            flags);
        m_xamlSource.SiteBridge().MoveAndResize({ 0, 0, width, height });

        // The C# host sizes both the popup and the XAML island HWND.  The
        // SiteBridge resize alone is not sufficient when the popup uses
        // WS_EX_NOREDIRECTIONBITMAP; without this, the child remains at its
        // initial 1x1 size and the transparent popup appears empty.
        if (m_islandWindow != nullptr)
        {
            SetWindowPos(
                m_islandWindow,
                HWND_TOP,
                0,
                0,
                width,
                height,
                flags | SWP_SHOWWINDOW);
        }
    }

    void DesktopFlyoutHost::Show(author::DesktopFlyoutActivationMode activationMode) noexcept
    {
        if (m_window == nullptr)
        {
            return;
        }

        if (activationMode == author::DesktopFlyoutActivationMode::activate)
        {
            ShowWindow(m_window, SW_SHOW);
            SetForegroundWindow(m_window);
        }
        else
        {
            ShowWindow(m_window, SW_SHOWNOACTIVATE);
        }
    }

    void DesktopFlyoutHost::ConfigureAutoCloseTimer(Windows::Foundation::TimeSpan delay) noexcept
    {
        StopAutoCloseTimer();

        if (m_window == nullptr || delay.count() <= 0)
        {
            return;
        }

        auto milliseconds = std::chrono::duration_cast<std::chrono::milliseconds>(delay).count();
        milliseconds = std::clamp<std::int64_t>(milliseconds, 1, USER_TIMER_MAXIMUM);
        m_autoCloseTimerId = SetTimer(
            m_window,
            c_autoCloseTimerId,
            static_cast<UINT>(milliseconds),
            nullptr);
    }

    void DesktopFlyoutHost::StopAutoCloseTimer() noexcept
    {
        if (m_window != nullptr && m_autoCloseTimerId != 0)
        {
            KillTimer(m_window, m_autoCloseTimerId);
        }
        m_autoCloseTimerId = 0;
    }

    void DesktopFlyoutHost::PreserveActivationState() noexcept
    {
        m_preservedForegroundWindow = GetForegroundWindow();
        m_preservedActiveWindow = GetActiveWindow();
        m_preservedFocusWindow = GetFocus();
    }

    void DesktopFlyoutHost::RestoreActivationState() noexcept
    {
        if (m_preservedForegroundWindow != nullptr && IsWindow(m_preservedForegroundWindow))
        {
            SetForegroundWindow(m_preservedForegroundWindow);
        }

        if (m_preservedActiveWindow != nullptr && IsWindow(m_preservedActiveWindow))
        {
            SetActiveWindow(m_preservedActiveWindow);
        }

        if (m_preservedFocusWindow != nullptr && IsWindow(m_preservedFocusWindow))
        {
            SetFocus(m_preservedFocusWindow);
        }
    }

    bool DesktopFlyoutHost::NavigateFocus() noexcept
    {
        if (!m_xamlSource || m_islandWindow == nullptr ||
            m_activationMode == author::DesktopFlyoutActivationMode::never_activate)
        {
            return false;
        }

        SetFocus(m_islandWindow);

        try
        {
            auto request = Microsoft::UI::Xaml::Hosting::XamlSourceFocusNavigationRequest{
                Microsoft::UI::Xaml::Hosting::XamlSourceFocusNavigationReason::Programmatic };
            auto result = m_xamlSource.NavigateFocus(request);
            return result && result.WasFocusMoved();
        }
        catch (...)
        {
            return false;
        }
    }

    void DesktopFlyoutHost::HideCallback(std::function<void()> callback)
    {
        m_hideCallback = std::move(callback);
    }

    void DesktopFlyoutHost::SystemSettingsCallback(std::function<void()> callback)
    {
        m_systemSettingsCallback = std::move(callback);
    }

    HWND DesktopFlyoutHost::Window() const noexcept
    {
        return m_window;
    }

    HWND DesktopFlyoutHost::IslandWindow() const noexcept
    {
        return m_islandWindow;
    }

    double DesktopFlyoutHost::RasterizationScale() const noexcept
    {
        const auto dpi = m_window != nullptr ? GetDpiForWindow(m_window) : USER_DEFAULT_SCREEN_DPI;
        return std::max(1.0, static_cast<double>(dpi) / USER_DEFAULT_SCREEN_DPI);
    }

    Microsoft::UI::Xaml::Hosting::DesktopWindowXamlSource DesktopFlyoutHost::XamlSource() const noexcept
    {
        return m_xamlSource;
    }
}
