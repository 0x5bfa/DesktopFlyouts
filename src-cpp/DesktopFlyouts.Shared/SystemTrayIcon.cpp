#include "pch.h"
#include "SystemTrayIcon.h"
#include "MouseEventReceivedEventArgs.h"
#if __has_include("SystemTrayIcon.g.cpp")
#include "SystemTrayIcon.g.cpp"
#endif

#include <mutex>

namespace winrt::DesktopFlyouts::implementation
{
    namespace
    {
        HINSTANCE ModuleInstance() noexcept
        {
            return GetModuleHandleW(nullptr);
        }
    }

    SystemTrayIcon::SystemTrayIcon(winrt::hstring const& iconPath, winrt::hstring const& tooltip, winrt::guid const& id) :
        SystemTrayIcon(iconPath, tooltip, id, true)
    {
    }

    SystemTrayIcon::SystemTrayIcon(winrt::hstring const& iconPath, winrt::hstring const& tooltip, winrt::guid const& id, bool isVisible) :
        m_iconPath(iconPath),
        m_tooltip(tooltip),
        m_id(id),
        m_taskbarRestartMessageId(RegisterWindowMessageW(L"TaskbarCreated")),
        m_isVisible(isVisible)
    {
        EnsureWindowClass();
        CreateMessageWindow();
    }

    SystemTrayIcon::~SystemTrayIcon()
    {
        Destroy();
        DestroyMessageWindow();
    }

    winrt::hstring SystemTrayIcon::IconPath() const
    {
        return m_iconPath;
    }

    void SystemTrayIcon::IconPath(winrt::hstring const& value)
    {
        m_iconPath = value;
        Show();
    }

    winrt::hstring SystemTrayIcon::Tooltip() const
    {
        return m_tooltip;
    }

    void SystemTrayIcon::Tooltip(winrt::hstring const& value)
    {
        m_tooltip = value;
        Show();
    }

    bool SystemTrayIcon::IsVisible() const noexcept
    {
        return m_isVisible;
    }

    void SystemTrayIcon::IsVisible(bool value)
    {
        m_isVisible = value;
        Show();
    }

    winrt::guid SystemTrayIcon::Id() const noexcept
    {
        return m_id;
    }

    void SystemTrayIcon::Show()
    {
        HICON icon = reinterpret_cast<HICON>(LoadImageW(
            nullptr,
            m_iconPath.c_str(),
            IMAGE_ICON,
            0,
            0,
            LR_LOADFROMFILE | LR_DEFAULTSIZE));

        if (!icon)
        {
            winrt::throw_hresult(HRESULT_FROM_WIN32(GetLastError()));
        }

        NOTIFYICONDATAW data{};
        FillNotifyIconData(data, icon);

        bool updated = false;
        if (m_created)
        {
            updated = Shell_NotifyIconW(NIM_MODIFY, &data) != FALSE;
            if (!updated)
            {
                m_created = false;
            }
        }

        if (!m_created)
        {
            Shell_NotifyIconW(NIM_DELETE, &data);
            updated = Shell_NotifyIconW(NIM_ADD, &data) != FALSE;
            if (updated)
            {
                data.uVersion = NOTIFYICON_VERSION_4;
                Shell_NotifyIconW(NIM_SETVERSION, &data);
                m_created = true;
            }
        }

        if (updated)
        {
            DestroyCurrentIcon();
            m_currentHIcon = icon;
        }
        else
        {
            DestroyIcon(icon);
        }
    }

    void SystemTrayIcon::Destroy()
    {
        if (m_created)
        {
            NOTIFYICONDATAW data{};
            FillNotifyIconData(data);
            Shell_NotifyIconW(NIM_DELETE, &data);
            m_created = false;
        }

        DestroyCurrentIcon();
    }

    winrt::event_token SystemTrayIcon::IconDestroyed(winrt::Windows::Foundation::TypedEventHandler<winrt::DesktopFlyouts::SystemTrayIcon, winrt::Windows::Foundation::IInspectable> const& handler)
    {
        return m_iconDestroyed.add(handler);
    }

    void SystemTrayIcon::IconDestroyed(winrt::event_token const& token) noexcept
    {
        m_iconDestroyed.remove(token);
    }

    winrt::event_token SystemTrayIcon::LeftClicked(winrt::Windows::Foundation::TypedEventHandler<winrt::DesktopFlyouts::SystemTrayIcon, winrt::DesktopFlyouts::MouseEventReceivedEventArgs> const& handler)
    {
        return m_leftClicked.add(handler);
    }

    void SystemTrayIcon::LeftClicked(winrt::event_token const& token) noexcept
    {
        m_leftClicked.remove(token);
    }

    winrt::event_token SystemTrayIcon::RightClicked(winrt::Windows::Foundation::TypedEventHandler<winrt::DesktopFlyouts::SystemTrayIcon, winrt::DesktopFlyouts::MouseEventReceivedEventArgs> const& handler)
    {
        return m_rightClicked.add(handler);
    }

    void SystemTrayIcon::RightClicked(winrt::event_token const& token) noexcept
    {
        m_rightClicked.remove(token);
    }

    wchar_t const* SystemTrayIcon::WindowClassName() noexcept
    {
        return L"DesktopFlyouts.SystemTrayIconWindow";
    }

    void SystemTrayIcon::EnsureWindowClass()
    {
        static std::once_flag registered;
        std::call_once(registered, []()
        {
            WNDCLASSW windowClass{};
            windowClass.style = CS_DBLCLKS;
            windowClass.lpfnWndProc = WindowProc;
            windowClass.hInstance = ModuleInstance();
            windowClass.lpszClassName = WindowClassName();

            if (!RegisterClassW(&windowClass))
            {
                uint32_t const error = GetLastError();
                if (error != ERROR_CLASS_ALREADY_EXISTS)
                {
                    winrt::throw_hresult(HRESULT_FROM_WIN32(error));
                }
            }
        });
    }

    LRESULT CALLBACK SystemTrayIcon::WindowProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept
    {
        if (message == WM_NCCREATE)
        {
            auto createStruct = reinterpret_cast<CREATESTRUCTW const*>(lParam);
            auto instance = static_cast<SystemTrayIcon*>(createStruct->lpCreateParams);
            SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(instance));
            return TRUE;
        }

        auto instance = reinterpret_cast<SystemTrayIcon*>(GetWindowLongPtrW(hWnd, GWLP_USERDATA));
        if (instance)
        {
            try
            {
                return instance->InstanceWindowProc(hWnd, message, wParam, lParam);
            }
            catch (...)
            {
                return DefWindowProcW(hWnd, message, wParam, lParam);
            }
        }

        return DefWindowProcW(hWnd, message, wParam, lParam);
    }

    void SystemTrayIcon::CreateMessageWindow()
    {
        m_hWnd = CreateWindowExW(
            WS_EX_LEFT,
            WindowClassName(),
            nullptr,
            WS_OVERLAPPED,
            0,
            0,
            1,
            1,
            nullptr,
            nullptr,
            ModuleInstance(),
            this);

        if (!m_hWnd)
        {
            winrt::throw_hresult(HRESULT_FROM_WIN32(GetLastError()));
        }
    }

    void SystemTrayIcon::DestroyMessageWindow() noexcept
    {
        if (m_hWnd)
        {
            HWND hWnd = m_hWnd;
            m_hWnd = nullptr;
            DestroyWindow(hWnd);
        }
    }

    void SystemTrayIcon::FillNotifyIconData(NOTIFYICONDATAW& data, HICON hIcon) const noexcept
    {
        data.cbSize = sizeof(data);
        data.hWnd = m_hWnd;
        data.uCallbackMessage = WM_UNIQUE_MESSAGE;
        data.hIcon = hIcon;
        data.guidItem = m_id;
        data.dwState = m_isVisible ? 0u : NIS_HIDDEN;
        data.dwStateMask = NIS_HIDDEN;
        data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_STATE | NIF_GUID | NIF_SHOWTIP;

        if (!m_tooltip.empty())
        {
            wcsncpy_s(data.szTip, _countof(data.szTip), m_tooltip.c_str(), _TRUNCATE);
        }
    }

    std::optional<winrt::Windows::Foundation::Point> SystemTrayIcon::GetCenterPointOfTrayIcon() const noexcept
    {
        NOTIFYICONIDENTIFIER identifier{};
        identifier.cbSize = sizeof(identifier);
        identifier.hWnd = m_hWnd;
        identifier.guidItem = m_id;

        RECT rect{};
        if (FAILED(Shell_NotifyIconGetRect(&identifier, &rect)))
        {
            return std::nullopt;
        }

        return winrt::Windows::Foundation::Point
        {
            static_cast<float>(rect.left + ((rect.right - rect.left) / 2.0f)),
            static_cast<float>(rect.top + ((rect.bottom - rect.top) / 2.0f))
        };
    }

    LRESULT SystemTrayIcon::InstanceWindowProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
    {
        UNREFERENCED_PARAMETER(wParam);

        if (message == WM_UNIQUE_MESSAGE)
        {
            switch (LOWORD(lParam))
            {
            case WM_LBUTTONUP:
                SetForegroundWindow(hWnd);
                RaiseMouseEvent(m_leftClicked);
                break;

            case WM_RBUTTONUP:
                SetForegroundWindow(hWnd);
                RaiseMouseEvent(m_rightClicked);
                break;
            }

            return 0;
        }

        if (message == WM_DESTROY)
        {
            Destroy();
            m_iconDestroyed(*this, nullptr);
            return 0;
        }

        if (message == m_taskbarRestartMessageId)
        {
            Destroy();
            Show();
        }

        if (message == WM_NCDESTROY)
        {
            SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
        }

        return DefWindowProcW(hWnd, message, wParam, lParam);
    }

    void SystemTrayIcon::DestroyCurrentIcon() noexcept
    {
        if (m_currentHIcon)
        {
            DestroyIcon(m_currentHIcon);
            m_currentHIcon = nullptr;
        }
    }

    void SystemTrayIcon::RaiseMouseEvent(winrt::event<winrt::Windows::Foundation::TypedEventHandler<winrt::DesktopFlyouts::SystemTrayIcon, winrt::DesktopFlyouts::MouseEventReceivedEventArgs>>& mouseEvent)
    {
        auto point = GetCenterPointOfTrayIcon();
        if (!point)
        {
            return;
        }

        mouseEvent(*this, winrt::make<MouseEventReceivedEventArgs>(*point));
    }
}
