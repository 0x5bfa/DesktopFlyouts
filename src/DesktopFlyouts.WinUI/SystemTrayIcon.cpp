#include "pch.h"

#include "author/SystemTrayIcon.author.h"
#include "author/SystemTrayIcon.author.impl.h"

#include <winrt/DesktopFlyouts.h>

#include <shellapi.h>

#include <algorithm>
#include <mutex>
#include <string>

namespace winrt::DesktopFlyouts::author
{
    struct SystemTrayIconNativeState
    {
        HWND callbackWindow{};
        HICON icon{};
        UINT callbackMessage{ WM_APP + 0x5BFA };
        UINT taskbarCreatedMessage{};
        bool shellCreated{};
        DWORD uiThreadId{ GetCurrentThreadId() };
    };
}

namespace
{
    constexpr wchar_t c_trayClassName[] = L"DesktopFlyouts.NativeSystemTrayIcon";

    void EnsureUiThread(winrt::DesktopFlyouts::author::SystemTrayIconNativeState const& state)
    {
        if (state.uiThreadId != GetCurrentThreadId())
        {
            winrt::throw_hresult(RPC_E_WRONG_THREAD);
        }
    }

    HICON LoadTrayIcon(std::wstring const& path)
    {
        if (path.empty())
        {
            return CopyIcon(LoadIconW(nullptr, IDI_APPLICATION));
        }

        return static_cast<HICON>(LoadImageW(
            nullptr,
            path.c_str(),
            IMAGE_ICON,
            GetSystemMetrics(SM_CXSMICON),
            GetSystemMetrics(SM_CYSMICON),
            LR_LOADFROMFILE | LR_DEFAULTSIZE));
    }
}

namespace winrt::DesktopFlyouts::author
{
    LRESULT CALLBACK SystemTrayIcon::WindowProc(
        HWND window,
        UINT message,
        WPARAM wParam,
        LPARAM lParam) noexcept
    {
        auto* icon = reinterpret_cast<SystemTrayIcon*>(GetWindowLongPtrW(window, GWLP_USERDATA));
        if (message == WM_NCCREATE)
        {
            auto* create = reinterpret_cast<CREATESTRUCTW*>(lParam);
            icon = static_cast<SystemTrayIcon*>(create->lpCreateParams);
            SetWindowLongPtrW(window, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(icon));
            return DefWindowProcW(window, message, wParam, lParam);
        }

        if (message == WM_NCDESTROY)
        {
            SetWindowLongPtrW(window, GWLP_USERDATA, 0);
        }

        if (icon != nullptr && message == WM_APP + 0x5BFA)
        {
            const auto point = icon->TrayIconPoint();
            auto args = winrt::make<winrt::DesktopFlyouts::implementation::SystemTrayIconEventArgs>(
                point);
            switch (static_cast<UINT>(lParam))
            {
            case WM_LBUTTONUP:
                icon->RaiseLeftClicked(args);
                break;
            case WM_RBUTTONUP:
                icon->RaiseRightClicked(args);
                break;
            case WM_LBUTTONDBLCLK:
                icon->RaiseLeftDoubleClicked(args);
                break;
            case WM_RBUTTONDBLCLK:
                icon->RaiseRightDoubleClicked(args);
                break;
            default:
                break;
            }
            return static_cast<LRESULT>(0);
        }

        if (icon != nullptr && icon->m_native != nullptr &&
            message == icon->m_native->taskbarCreatedMessage)
        {
            icon->RestoreAfterTaskbarRestart();
            return static_cast<LRESULT>(0);
        }

        return DefWindowProcW(window, message, wParam, lParam);
    }

    void SystemTrayIcon::RegisterWindowClass()
    {
        static std::once_flag registered;
        std::call_once(registered, []
        {
            WNDCLASSEXW windowClass{};
            windowClass.cbSize = sizeof(windowClass);
            windowClass.hInstance = GetModuleHandleW(nullptr);
            windowClass.lpfnWndProc = &SystemTrayIcon::WindowProc;
            windowClass.lpszClassName = c_trayClassName;
            windowClass.hInstance = GetModuleHandleW(nullptr);
            windowClass.hCursor = LoadCursorW(nullptr, IDC_ARROW);
            windowClass.style = CS_DBLCLKS;
            winrt::check_bool(RegisterClassExW(&windowClass) != 0 || GetLastError() == ERROR_CLASS_ALREADY_EXISTS);
        });
    }

    SystemTrayIconEventArgs::SystemTrayIconEventArgs(Windows::Foundation::Point point)
        : m_point(point)
    {
    }

    Windows::Foundation::Point SystemTrayIconEventArgs::Point(winrt::author::getter)
    {
        return m_point;
    }

    SystemTrayIcon::SystemTrayIcon()
        : SystemTrayIcon({}, L"DesktopFlyouts", winrt::guid{})
    {
    }

    SystemTrayIcon::SystemTrayIcon(winrt::hstring iconPath, winrt::hstring tooltip, winrt::guid id)
        : m_iconPath(std::move(iconPath)),
          m_tooltip(std::move(tooltip)),
          m_id(id),
          m_native(std::make_unique<SystemTrayIconNativeState>())
    {
        RegisterWindowClass();
        m_native->taskbarCreatedMessage = RegisterWindowMessageW(L"TaskbarCreated");
        m_native->callbackWindow = CreateWindowExW(
            0,
            c_trayClassName,
            L"DesktopFlyouts system tray icon",
            WS_OVERLAPPED,
            0,
            0,
            1,
            1,
            nullptr,
            nullptr,
            GetModuleHandleW(nullptr),
            this);
        winrt::check_bool(m_native->callbackWindow != nullptr);
        m_native->icon = LoadTrayIcon(std::wstring(m_iconPath));
        winrt::check_bool(m_native->icon != nullptr);
    }

    SystemTrayIcon::~SystemTrayIcon()
    {
        if (!m_native)
        {
            return;
        }

        try
        {
            EnsureUiThread(*m_native);
            Destroy();
        }
        catch (...)
        {
        }
        if (m_native->icon != nullptr)
        {
            DestroyIcon(m_native->icon);
            m_native->icon = nullptr;
        }
        m_native.reset();
    }

    winrt::hstring SystemTrayIcon::IconPath(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_iconPath;
    }

    winrt::hstring SystemTrayIcon::Tooltip(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_tooltip;
    }

    winrt::author::setter SystemTrayIcon::Tooltip(winrt::hstring value)
    {
        EnsureUiThread(*m_native);
        m_tooltip = std::move(value);
        if (m_isVisible)
        {
            Show();
        }
        return {};
    }

    bool SystemTrayIcon::IsVisible(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_isVisible;
    }

    winrt::author::setter SystemTrayIcon::IsVisible(bool value)
    {
        EnsureUiThread(*m_native);
        value ? Show() : Hide();
        return {};
    }

    winrt::guid SystemTrayIcon::Id(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_id;
    }

    Windows::Foundation::Point SystemTrayIcon::TrayIconPoint() const noexcept
    {
        if (!m_native || !m_native->callbackWindow)
        {
            return {};
        }

        NOTIFYICONIDENTIFIER identifier{};
        identifier.cbSize = sizeof(identifier);
        identifier.hWnd = m_native->callbackWindow;
        identifier.guidItem = m_id;

        RECT rect{};
        if (SUCCEEDED(Shell_NotifyIconGetRect(&identifier, &rect)))
        {
            return Windows::Foundation::Point{
                static_cast<float>(rect.left + ((rect.right - rect.left) / 2.0)),
                static_cast<float>(rect.top + ((rect.bottom - rect.top) / 2.0)) };
        }

        POINT point{};
        if (GetCursorPos(&point))
        {
            return Windows::Foundation::Point{
                static_cast<float>(point.x),
                static_cast<float>(point.y) };
        }

        return {};
    }

    void SystemTrayIcon::RestoreAfterTaskbarRestart() noexcept
    {
        if (m_isVisible)
        {
            m_native->shellCreated = false;
            Show();
        }
    }

    winrt::event_token SystemTrayIcon::LeftClicked(
        Windows::Foundation::EventHandler<Windows::Foundation::IInspectable> const& handler)
    {
        return m_leftClicked.add(handler);
    }

    void SystemTrayIcon::LeftClicked(winrt::event_token token)
    {
        m_leftClicked.remove(token);
    }

    winrt::event_token SystemTrayIcon::RightClicked(
        Windows::Foundation::EventHandler<Windows::Foundation::IInspectable> const& handler)
    {
        return m_rightClicked.add(handler);
    }

    void SystemTrayIcon::RightClicked(winrt::event_token token)
    {
        m_rightClicked.remove(token);
    }

    winrt::event_token SystemTrayIcon::LeftDoubleClicked(
        Windows::Foundation::EventHandler<Windows::Foundation::IInspectable> const& handler)
    {
        return m_leftDoubleClicked.add(handler);
    }

    void SystemTrayIcon::LeftDoubleClicked(winrt::event_token token)
    {
        m_leftDoubleClicked.remove(token);
    }

    winrt::event_token SystemTrayIcon::RightDoubleClicked(
        Windows::Foundation::EventHandler<Windows::Foundation::IInspectable> const& handler)
    {
        return m_rightDoubleClicked.add(handler);
    }

    void SystemTrayIcon::RightDoubleClicked(winrt::event_token token)
    {
        m_rightDoubleClicked.remove(token);
    }

    void SystemTrayIcon::RaiseLeftClicked(Windows::Foundation::IInspectable const& args, winrt::author::ignore)
    {
        m_leftClicked(nullptr, args);
    }

    void SystemTrayIcon::RaiseRightClicked(Windows::Foundation::IInspectable const& args, winrt::author::ignore)
    {
        m_rightClicked(nullptr, args);
    }

    void SystemTrayIcon::RaiseLeftDoubleClicked(
        Windows::Foundation::IInspectable const& args,
        winrt::author::ignore)
    {
        m_leftDoubleClicked(nullptr, args);
    }

    void SystemTrayIcon::RaiseRightDoubleClicked(
        Windows::Foundation::IInspectable const& args,
        winrt::author::ignore)
    {
        m_rightDoubleClicked(nullptr, args);
    }

    void SystemTrayIcon::Show()
    {
        EnsureUiThread(*m_native);
        m_isVisible = true;

        NOTIFYICONDATAW data{};
        data.cbSize = sizeof(data);
        data.hWnd = m_native->callbackWindow;
        data.uID = 1;
        data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_GUID;
        data.uCallbackMessage = m_native->callbackMessage;
        data.hIcon = m_native->icon;
        data.guidItem = m_id;
        wcsncpy_s(data.szTip, std::wstring(m_tooltip).c_str(), _TRUNCATE);

        if (!m_native->shellCreated)
        {
            m_native->shellCreated = Shell_NotifyIconW(NIM_ADD, &data) != FALSE;
            if (m_native->shellCreated)
            {
                data.uVersion = NOTIFYICON_VERSION_4;
                Shell_NotifyIconW(NIM_SETVERSION, &data);
            }
        }
        else
        {
            Shell_NotifyIconW(NIM_MODIFY, &data);
        }
    }

    void SystemTrayIcon::Hide()
    {
        EnsureUiThread(*m_native);
        m_isVisible = false;
        if (m_native->shellCreated)
        {
            NOTIFYICONDATAW data{};
            data.cbSize = sizeof(data);
            data.hWnd = m_native->callbackWindow;
            data.uID = 1;
            data.guidItem = m_id;
            data.uFlags = NIF_GUID;
            Shell_NotifyIconW(NIM_DELETE, &data);
            m_native->shellCreated = false;
        }
    }

    void SystemTrayIcon::SetIcon(winrt::hstring iconPath)
    {
        EnsureUiThread(*m_native);
        auto newIcon = LoadTrayIcon(std::wstring(iconPath));
        winrt::check_bool(newIcon != nullptr);
        if (m_native->icon != nullptr)
        {
            DestroyIcon(m_native->icon);
        }
        m_native->icon = newIcon;
        m_iconPath = std::move(iconPath);
        if (m_isVisible)
        {
            Show();
        }
    }

    void SystemTrayIcon::Destroy()
    {
        EnsureUiThread(*m_native);
        Hide();
        if (m_native->callbackWindow != nullptr)
        {
            DestroyWindow(m_native->callbackWindow);
            m_native->callbackWindow = nullptr;
        }
    }
}
