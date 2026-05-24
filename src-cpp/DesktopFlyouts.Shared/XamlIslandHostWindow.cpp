#include "pch.h"
#include "XamlIslandHostWindow.h"

namespace winrt::DesktopFlyouts::details
{
    XamlIslandHostWindow::XamlIslandHostWindow()
    {
        m_className = L"DesktopFlyouts.Host.";
        m_className += std::to_wstring(reinterpret_cast<uintptr_t>(this));

        WNDCLASSW windowClass{};
        windowClass.lpfnWndProc = WindowProc;
        windowClass.hInstance = GetModuleHandleW(nullptr);
        windowClass.lpszClassName = m_className.c_str();
        winrt::check_bool(RegisterClassW(&windowClass) != 0);

        m_hwnd = CreateWindowExW(
            WS_EX_NOREDIRECTIONBITMAP | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
            m_className.c_str(),
            L"DesktopFlyoutHostWindow",
            WS_POPUP,
            0,
            0,
            0,
            0,
            nullptr,
            nullptr,
            windowClass.hInstance,
            this);
        winrt::check_bool(m_hwnd != nullptr);

        m_source = winrt::Microsoft::UI::Xaml::Hosting::DesktopWindowXamlSource{};
        m_source.Initialize(winrt::Microsoft::UI::GetWindowIdFromWindow(m_hwnd));
        m_xamlHwnd = winrt::Microsoft::UI::GetWindowFromWindowId(m_source.SiteBridge().WindowId());
    }

    XamlIslandHostWindow::~XamlIslandHostWindow()
    {
        Close();
    }

    void XamlIslandHostWindow::SetContent(winrt::Microsoft::UI::Xaml::UIElement const& content)
    {
        if (!m_closed)
        {
            m_source.Content(content);
            ApplyActivationMode();
        }
    }

    void XamlIslandHostWindow::PreserveActivationState() noexcept
    {
        m_preservedForeground = GetForegroundWindow();
        m_preservedActive = GetActiveWindow();
        m_preservedFocus = GetFocus();
    }

    void XamlIslandHostWindow::RestoreActivationState() noexcept
    {
        if (m_preservedForeground)
        {
            SetForegroundWindow(m_preservedForeground);
        }
        if (m_preservedActive)
        {
            SetActiveWindow(m_preservedActive);
        }
        if (m_preservedFocus)
        {
            SetFocus(m_preservedFocus);
        }
    }

    void XamlIslandHostWindow::MoveAndResize(winrt::Windows::Graphics::RectInt32 const& rect, bool activate) noexcept
    {
        UINT flags = activate ? 0 : SWP_NOACTIVATE;
        SetWindowPos(m_hwnd, HWND_TOP, rect.X, rect.Y, rect.Width, rect.Height, flags);
        SetWindowPos(m_xamlHwnd, HWND_TOP, 0, 0, rect.Width, rect.Height, flags);
    }

    void XamlIslandHostWindow::Maximize(RECT const& workArea, bool activate) noexcept
    {
        MoveAndResize(
            { workArea.left, workArea.top, workArea.right - workArea.left, workArea.bottom - workArea.top },
            activate);
    }

    void XamlIslandHostWindow::SetRectRegion(winrt::Windows::Graphics::RectInt32 const& rect) noexcept
    {
        SetWindowRectRegion(m_hwnd, rect);
        SetWindowRectRegion(m_xamlHwnd, rect);
    }

    void XamlIslandHostWindow::SetVisible(bool visible, bool activate)
    {
        if (m_closed)
        {
            return;
        }

        ShowWindow(m_hwnd, visible ? (activate ? SW_SHOW : SW_SHOWNOACTIVATE) : SW_HIDE);
        if (visible && activate)
        {
            m_source.SiteBridge().Show();
        }
        else if (visible)
        {
            ShowWindow(m_xamlHwnd, SW_SHOWNOACTIVATE);
        }
        else
        {
            m_source.SiteBridge().Hide();
        }

        if (visible)
        {
            ApplyActivationMode();
        }
    }

    void XamlIslandHostWindow::SetActivationMode(winrt::DesktopFlyouts::DesktopFlyoutActivationMode value) noexcept
    {
        m_activationMode = value;
        ApplyActivationMode();
    }

    bool XamlIslandHostWindow::NavigateFocus(winrt::Microsoft::UI::Xaml::Hosting::XamlSourceFocusNavigationReason reason)
    {
        if (m_closed || !m_xamlHwnd || m_activationMode == winrt::DesktopFlyouts::DesktopFlyoutActivationMode::NeverActivate)
        {
            return false;
        }

        SetFocus(m_xamlHwnd);
        return m_source.NavigateFocus(winrt::Microsoft::UI::Xaml::Hosting::XamlSourceFocusNavigationRequest{ reason }).WasFocusMoved();
    }

    double XamlIslandHostWindow::RasterizationScale() const noexcept
    {
        try
        {
            return m_source ? m_source.SiteBridge().SiteView().RasterizationScale() : 1.0;
        }
        catch (...)
        {
            return 1.0;
        }
    }

    bool XamlIslandHostWindow::HasSource() const noexcept
    {
        return !m_closed && m_source != nullptr;
    }

    void XamlIslandHostWindow::Close() noexcept
    {
        if (m_closed)
        {
            return;
        }

        m_closed = true;
        try
        {
            m_source.Close();
        }
        catch (...)
        {
        }
        m_source = nullptr;
        if (m_hwnd)
        {
            DestroyWindow(m_hwnd);
            m_hwnd = nullptr;
        }
        UnregisterClassW(m_className.c_str(), GetModuleHandleW(nullptr));
        m_xamlHwnd = nullptr;
    }

    LRESULT CALLBACK XamlIslandHostWindow::WindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept
    {
        if (message == WM_NCCREATE)
        {
            auto create = reinterpret_cast<CREATESTRUCTW const*>(lParam);
            auto instance = static_cast<XamlIslandHostWindow*>(create->lpCreateParams);
            SetWindowLongPtrW(hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(instance));
        }

        auto instance = reinterpret_cast<XamlIslandHostWindow*>(GetWindowLongPtrW(hwnd, GWLP_USERDATA));
        return instance ? instance->InstanceWindowProc(hwnd, message, wParam, lParam) : DefWindowProcW(hwnd, message, wParam, lParam);
    }

    LRESULT XamlIslandHostWindow::InstanceWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept
    {
        switch (message)
        {
        case WM_SETTINGCHANGE:
        case WM_THEMECHANGED:
            if (SystemSettingsChanged)
            {
                SystemSettingsChanged();
            }
            return 0;
        case WM_ACTIVATE:
            if (LOWORD(wParam) == WA_INACTIVE && WindowInactivated)
            {
                WindowInactivated();
            }
            return 0;
        case WM_MOUSEACTIVATE:
            if (m_activationMode == winrt::DesktopFlyouts::DesktopFlyoutActivationMode::NeverActivate)
            {
                RestoreActivationState();
                return MA_NOACTIVATE;
            }
            break;
        case WM_SETFOCUS:
            if (m_activationMode == winrt::DesktopFlyouts::DesktopFlyoutActivationMode::NeverActivate)
            {
                RestoreActivationState();
                return 0;
            }
            break;
        case WM_NCDESTROY:
            SetWindowLongPtrW(hwnd, GWLP_USERDATA, 0);
            break;
        }

        return DefWindowProcW(hwnd, message, wParam, lParam);
    }

    void XamlIslandHostWindow::SetWindowRectRegion(HWND hwnd, winrt::Windows::Graphics::RectInt32 const& rect) noexcept
    {
        auto region = CreateRectRgn(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
        if (region && SetWindowRgn(hwnd, region, FALSE) == 0)
        {
            DeleteObject(region);
        }
    }

    void XamlIslandHostWindow::SetNoActivateStyle(HWND hwnd, bool enabled) noexcept
    {
        if (!hwnd)
        {
            return;
        }

        auto style = GetWindowLongPtrW(hwnd, GWL_EXSTYLE);
        style = enabled ? style | WS_EX_NOACTIVATE : style & ~static_cast<LONG_PTR>(WS_EX_NOACTIVATE);
        SetWindowLongPtrW(hwnd, GWL_EXSTYLE, style);
        SetWindowPos(hwnd, nullptr, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
    }

    void XamlIslandHostWindow::ApplyActivationMode() noexcept
    {
        auto neverActivate = m_activationMode == winrt::DesktopFlyouts::DesktopFlyoutActivationMode::NeverActivate;
        SetNoActivateStyle(m_hwnd, neverActivate);
        SetNoActivateStyle(m_xamlHwnd, neverActivate);
    }
}
