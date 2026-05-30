#include "pch.h"
#include "XamlIslandHostWindow.h"

namespace winrt::DesktopFlyouts::details
{
    namespace
    {
        constexpr wchar_t OwnerPropertyName[] = L"DesktopFlyouts.XamlIslandHostWindow.Owner";
    }

    std::mutex XamlIslandHostWindow::s_cbtTargetsMutex;
    std::unordered_map<DWORD, std::vector<XamlIslandHostWindow*>> XamlIslandHostWindow::s_cbtTargetsByThread;

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
        RemoveCbtHook();
        UnsubclassXamlIslandWindows();
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

    LRESULT CALLBACK XamlIslandHostWindow::XamlWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept
    {
        auto instance = reinterpret_cast<XamlIslandHostWindow*>(GetPropW(hwnd, OwnerPropertyName));
        return instance ? instance->InstanceXamlWindowProc(hwnd, message, wParam, lParam) : DefWindowProcW(hwnd, message, wParam, lParam);
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

    LRESULT XamlIslandHostWindow::InstanceXamlWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept
    {
        if (m_activationMode == winrt::DesktopFlyouts::DesktopFlyoutActivationMode::NeverActivate)
        {
            if (message == WM_MOUSEACTIVATE)
            {
                RestoreActivationState();
                return MA_NOACTIVATE;
            }
            if (message == WM_SETFOCUS)
            {
                RestoreActivationState();
                return 0;
            }
        }

        auto result = CallPreviousXamlWindowProc(hwnd, message, wParam, lParam);
        if (message == WM_NCDESTROY)
        {
            RemovePropW(hwnd, OwnerPropertyName);
            m_subclassedXamlWindowProcedures.erase(hwnd);
        }
        return result;
    }

    LRESULT XamlIslandHostWindow::CallPreviousXamlWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept
    {
        auto const found = m_subclassedXamlWindowProcedures.find(hwnd);
        return found == m_subclassedXamlWindowProcedures.end()
            ? DefWindowProcW(hwnd, message, wParam, lParam)
            : CallWindowProcW(found->second, hwnd, message, wParam, lParam);
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
        UpdateCbtHook(neverActivate);
        if (neverActivate)
        {
            RefreshXamlIslandWindowSubclasses();
        }

        SetNoActivateStyle(m_hwnd, neverActivate);
        SetNoActivateStyle(m_xamlHwnd, neverActivate);
        for (auto const& [hwnd, previousWindowProc] : m_subclassedXamlWindowProcedures)
        {
            (void)previousWindowProc;
            SetNoActivateStyle(hwnd, neverActivate);
        }

        if (!neverActivate)
        {
            UnsubclassXamlIslandWindows();
        }
    }

    void XamlIslandHostWindow::UpdateCbtHook(bool enabled) noexcept
    {
        if (enabled)
        {
            EnsureCbtHook();
        }
        else
        {
            RemoveCbtHook();
        }
    }

    void XamlIslandHostWindow::EnsureCbtHook() noexcept
    {
        if (m_cbtHook)
        {
            return;
        }

        m_cbtHookThreadId = GetCurrentThreadId();
        m_cbtHook = SetWindowsHookExW(WH_CBT, CbtHookProc, nullptr, m_cbtHookThreadId);
        if (m_cbtHook)
        {
            RegisterCbtHookTarget(m_cbtHookThreadId);
        }
        else
        {
            m_cbtHookThreadId = 0;
        }
    }

    void XamlIslandHostWindow::RemoveCbtHook() noexcept
    {
        if (!m_cbtHook)
        {
            return;
        }

        UnhookWindowsHookEx(m_cbtHook);
        m_cbtHook = nullptr;
        UnregisterCbtHookTarget(m_cbtHookThreadId);
        m_cbtHookThreadId = 0;
    }

    void XamlIslandHostWindow::RegisterCbtHookTarget(DWORD threadId) noexcept
    {
        try
        {
            std::scoped_lock lock(s_cbtTargetsMutex);
            auto& targets = s_cbtTargetsByThread[threadId];
            if (std::find(targets.begin(), targets.end(), this) == targets.end())
            {
                targets.push_back(this);
            }
        }
        catch (...)
        {
            // The window styles and subclass path still prevent normal activation.
        }
    }

    void XamlIslandHostWindow::UnregisterCbtHookTarget(DWORD threadId) noexcept
    {
        if (threadId == 0)
        {
            return;
        }

        std::scoped_lock lock(s_cbtTargetsMutex);
        auto const found = s_cbtTargetsByThread.find(threadId);
        if (found == s_cbtTargetsByThread.end())
        {
            return;
        }

        auto& targets = found->second;
        targets.erase(std::remove(targets.begin(), targets.end(), this), targets.end());
        if (targets.empty())
        {
            s_cbtTargetsByThread.erase(found);
        }
    }

    LRESULT CALLBACK XamlIslandHostWindow::CbtHookProc(int code, WPARAM wParam, LPARAM lParam) noexcept
    {
        if (code >= 0 && (code == HCBT_ACTIVATE || code == HCBT_SETFOCUS))
        {
            std::vector<XamlIslandHostWindow*> targets;
            try
            {
                std::scoped_lock lock(s_cbtTargetsMutex);
                auto const found = s_cbtTargetsByThread.find(GetCurrentThreadId());
                if (found != s_cbtTargetsByThread.end())
                {
                    targets = found->second;
                }
            }
            catch (...)
            {
                return CallNextHookEx(nullptr, code, wParam, lParam);
            }

            auto const targetHwnd = reinterpret_cast<HWND>(wParam);
            for (auto const target : targets)
            {
                if (target &&
                    !target->m_closed &&
                    target->m_activationMode == winrt::DesktopFlyouts::DesktopFlyoutActivationMode::NeverActivate &&
                    target->IsFlyoutWindow(targetHwnd))
                {
                    target->RestoreActivationState();
                    return 1;
                }
            }
        }

        return CallNextHookEx(nullptr, code, wParam, lParam);
    }

    bool XamlIslandHostWindow::IsFlyoutWindow(HWND hwnd) const noexcept
    {
        if (!hwnd)
        {
            return false;
        }
        if (hwnd == m_hwnd || hwnd == m_xamlHwnd ||
            m_subclassedXamlWindowProcedures.find(hwnd) != m_subclassedXamlWindowProcedures.end())
        {
            return true;
        }
        if ((m_hwnd && IsChild(m_hwnd, hwnd)) || (m_xamlHwnd && IsChild(m_xamlHwnd, hwnd)))
        {
            return true;
        }

        auto const root = GetAncestor(hwnd, GA_ROOT);
        if (root == m_hwnd || root == m_xamlHwnd)
        {
            return true;
        }
        auto const rootOwner = GetAncestor(hwnd, GA_ROOTOWNER);
        return rootOwner == m_hwnd || rootOwner == m_xamlHwnd;
    }

    void XamlIslandHostWindow::RefreshXamlIslandWindowSubclasses() noexcept
    {
        if (!m_xamlHwnd)
        {
            return;
        }

        SubclassXamlIslandWindow(m_xamlHwnd);
        SubclassChildWindows(m_hwnd);
        SubclassChildWindows(m_xamlHwnd);
    }

    void XamlIslandHostWindow::SubclassChildWindows(HWND parentHwnd) noexcept
    {
        for (auto childHwnd = GetWindow(parentHwnd, GW_CHILD); childHwnd; childHwnd = GetWindow(childHwnd, GW_HWNDNEXT))
        {
            SubclassXamlIslandWindow(childHwnd);
            SubclassChildWindows(childHwnd);
        }
    }

    void XamlIslandHostWindow::SubclassXamlIslandWindow(HWND hwnd) noexcept
    {
        if (!hwnd || hwnd == m_hwnd ||
            m_subclassedXamlWindowProcedures.find(hwnd) != m_subclassedXamlWindowProcedures.end())
        {
            return;
        }

        if (!SetPropW(hwnd, OwnerPropertyName, reinterpret_cast<HANDLE>(this)))
        {
            return;
        }

        auto const previousWindowProc = reinterpret_cast<WNDPROC>(
            SetWindowLongPtrW(hwnd, GWLP_WNDPROC, reinterpret_cast<LONG_PTR>(&XamlWindowProc)));
        if (!previousWindowProc)
        {
            RemovePropW(hwnd, OwnerPropertyName);
            return;
        }

        try
        {
            m_subclassedXamlWindowProcedures.emplace(hwnd, previousWindowProc);
        }
        catch (...)
        {
            SetWindowLongPtrW(hwnd, GWLP_WNDPROC, reinterpret_cast<LONG_PTR>(previousWindowProc));
            RemovePropW(hwnd, OwnerPropertyName);
        }
    }

    void XamlIslandHostWindow::UnsubclassXamlIslandWindows() noexcept
    {
        for (auto const& [hwnd, previousWindowProc] : m_subclassedXamlWindowProcedures)
        {
            if (IsWindow(hwnd))
            {
                SetWindowLongPtrW(hwnd, GWLP_WNDPROC, reinterpret_cast<LONG_PTR>(previousWindowProc));
                RemovePropW(hwnd, OwnerPropertyName);
            }
        }
        m_subclassedXamlWindowProcedures.clear();
    }
}
