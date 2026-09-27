#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#include <windows.ui.xaml.hosting.desktopwindowxamlsource.h>
#undef GetCurrentTime

#include "DesktopFlyoutHost.h"

#include <algorithm>
#include <chrono>
#include <mutex>

namespace
{
    constexpr wchar_t c_hostWindowClassName[] = L"DesktopFlyouts.Uwp.NativeFlyoutHost";
    constexpr UINT_PTR c_autoCloseTimerId = 0xDF11;

    void SetNoActivateStyle(HWND window, bool enabled) noexcept
    {
        if (window == nullptr)
        {
            return;
        }

        auto exStyle = static_cast<LONG_PTR>(GetWindowLongPtrW(window, GWL_EXSTYLE));
        exStyle = enabled
            ? exStyle | WS_EX_NOACTIVATE
            : exStyle & ~static_cast<LONG_PTR>(WS_EX_NOACTIVATE);
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

    void DesktopFlyoutHost::EnsureXamlManager()
    {
        static thread_local Windows::UI::Xaml::Hosting::WindowsXamlManager manager{ nullptr };
        if (!manager)
        {
            manager = Windows::UI::Xaml::Hosting::WindowsXamlManager::InitializeForCurrentThread();
        }
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

        if (host != nullptr)
        {
            if (message == WM_MOUSEACTIVATE && host->m_activationMode == author::DesktopFlyoutActivationMode::never_activate)
            {
                host->RestoreActivationState();
                return MA_NOACTIVATE;
            }
            if (message == WM_SETFOCUS && host->m_activationMode == author::DesktopFlyoutActivationMode::never_activate)
            {
                host->RestoreActivationState();
                return 0;
            }
            if (message == WM_ACTIVATE && LOWORD(wParam) == WA_INACTIVE && host->m_isOpen &&
                host->m_hideOnLostFocus && host->m_hideCallback)
            {
                host->m_hideCallback();
                return 0;
            }
            if (message == WM_KEYDOWN && wParam == VK_ESCAPE && host->m_hideCallback)
            {
                host->m_hideCallback();
                return 0;
            }
            if (message == WM_TIMER && wParam == host->m_autoCloseTimerId)
            {
                host->StopAutoCloseTimer();
                if (host->m_hideCallback)
                {
                    host->m_hideCallback();
                }
                return 0;
            }
            if (message == WM_SIZE && host->m_islandWindow != nullptr)
            {
                SetWindowPos(
                    host->m_islandWindow,
                    nullptr,
                    0,
                    0,
                    LOWORD(lParam),
                    HIWORD(lParam),
                    SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            if (message == WM_NCDESTROY)
            {
                SetWindowLongPtrW(window, GWLP_USERDATA, 0);
            }
        }

        return DefWindowProcW(window, message, wParam, lParam);
    }

    void DesktopFlyoutHost::EnsureCreated(HWND ownerWindow, author::DesktopFlyoutActivationMode activationMode)
    {
        m_ownerWindow = ownerWindow;
        ActivationMode(activationMode);
        if (m_window != nullptr)
        {
            SetWindowLongPtrW(m_window, GWLP_HWNDPARENT, reinterpret_cast<LONG_PTR>(m_ownerWindow));
            return;
        }

        EnsureXamlManager();
        RegisterWindowClass();
        m_window = CreateWindowExW(
            WS_EX_NOREDIRECTIONBITMAP | WS_EX_TOOLWINDOW | WS_EX_TOPMOST |
                (activationMode == author::DesktopFlyoutActivationMode::never_activate ? WS_EX_NOACTIVATE : 0),
            c_hostWindowClassName,
            L"DesktopFlyout.Uwp",
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

        m_xamlSource = Windows::UI::Xaml::Hosting::DesktopWindowXamlSource{};
        auto native = m_xamlSource.as<IDesktopWindowXamlSourceNative2>();
        winrt::check_hresult(native->AttachToWindow(m_window));
        winrt::check_hresult(native->get_WindowHandle(&m_islandWindow));
        winrt::check_bool(m_islandWindow != nullptr);
        SetWindowLongPtrW(m_islandWindow, GWL_STYLE, WS_CHILD | WS_VISIBLE);
        SetNoActivateStyle(m_islandWindow, activationMode == author::DesktopFlyoutActivationMode::never_activate);
    }

    void DesktopFlyoutHost::Destroy() noexcept
    {
        m_hideCallback = {};
        StopAutoCloseTimer();
        if (m_xamlSource)
        {
            try
            {
                m_xamlSource.Content(nullptr);
                m_xamlSource.Close();
            }
            catch (...)
            {
            }
            m_xamlSource = nullptr;
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

    void DesktopFlyoutHost::Content(Windows::UI::Xaml::UIElement const& content)
    {
        winrt::check_bool(m_xamlSource != nullptr);
        m_xamlSource.Content(content);
    }

    void DesktopFlyoutHost::Hide() noexcept
    {
        if (m_window != nullptr)
        {
            ShowWindow(m_window, SW_HIDE);
        }
        if (m_islandWindow != nullptr)
        {
            ShowWindow(m_islandWindow, SW_HIDE);
        }
    }

    void DesktopFlyoutHost::MoveAndResize(int x, int y, int width, int height)
    {
        winrt::check_bool(m_window != nullptr);
        const auto flags = m_activationMode == author::DesktopFlyoutActivationMode::activate ? 0U : SWP_NOACTIVATE;
        SetWindowPos(m_window, HWND_TOP, x, y, width, height, flags);
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
        if (m_islandWindow != nullptr)
        {
            ShowWindow(m_islandWindow, activationMode == author::DesktopFlyoutActivationMode::activate ? SW_SHOW : SW_SHOWNOACTIVATE);
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
        m_autoCloseTimerId = SetTimer(m_window, c_autoCloseTimerId, static_cast<UINT>(milliseconds), nullptr);
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

    bool DesktopFlyoutHost::NavigateFocus(
        Windows::UI::Xaml::Hosting::XamlSourceFocusNavigationReason reason) noexcept
    {
        if (!m_xamlSource || m_islandWindow == nullptr ||
            m_activationMode == author::DesktopFlyoutActivationMode::never_activate)
        {
            return false;
        }
        SetFocus(m_islandWindow);
        try
        {
            auto result = m_xamlSource.NavigateFocus(
                Windows::UI::Xaml::Hosting::XamlSourceFocusNavigationRequest{ reason });
            return result && result.WasFocusMoved();
        }
        catch (...)
        {
            return false;
        }
    }

    bool DesktopFlyoutHost::TryPreTranslateMessage(MSG const* message) noexcept
    {
        if (!m_xamlSource || message == nullptr)
        {
            return false;
        }
        try
        {
            auto native = m_xamlSource.as<IDesktopWindowXamlSourceNative2>();
            BOOL handled = FALSE;
            winrt::check_hresult(native->PreTranslateMessage(message, &handled));
            return handled != FALSE;
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

    Windows::UI::Xaml::Hosting::DesktopWindowXamlSource DesktopFlyoutHost::XamlSource() const noexcept
    {
        return m_xamlSource;
    }
}
