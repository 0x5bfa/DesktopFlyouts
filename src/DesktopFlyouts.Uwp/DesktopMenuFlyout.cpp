#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#include <windows.ui.xaml.hosting.desktopwindowxamlsource.h>
#undef GetCurrentTime

#include "author/DesktopMenuFlyout.author.h"
#include "author/DesktopMenuFlyout.author.impl.h"

#include <winrt/DesktopFlyouts.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.UI.Xaml.Controls.h>
#include <winrt/Windows.UI.Xaml.Controls.Primitives.h>
#include <winrt/Windows.UI.Xaml.Hosting.h>
#include <winrt/Windows.UI.Xaml.Interop.h>

#include <cmath>
#include <mutex>

namespace winrt::DesktopFlyouts::author
{
    struct DesktopMenuFlyoutNativeState
    {
        HWND ownerWindow{};
        HWND window{};
        HWND islandWindow{};
        Windows::UI::Xaml::Hosting::DesktopWindowXamlSource xamlSource{ nullptr };
        Windows::UI::Xaml::Controls::Grid root{ nullptr };
        Windows::UI::Xaml::Controls::Border target{ nullptr };
        winrt::event_token menuClosedToken{};
        DWORD uiThreadId{ GetCurrentThreadId() };
    };
}

namespace
{
    constexpr wchar_t c_menuHostClassName[] = L"DesktopFlyouts.Uwp.NativeMenuFlyoutHost";

    void EnsureUiThread(winrt::DesktopFlyouts::author::DesktopMenuFlyoutNativeState const& state)
    {
        if (state.uiThreadId != GetCurrentThreadId())
        {
            winrt::throw_hresult(RPC_E_WRONG_THREAD);
        }
    }

    void EnsureXamlManager()
    {
        static thread_local winrt::Windows::UI::Xaml::Hosting::WindowsXamlManager manager{ nullptr };
        if (!manager)
        {
            manager = winrt::Windows::UI::Xaml::Hosting::WindowsXamlManager::InitializeForCurrentThread();
        }
    }
}

namespace winrt::DesktopFlyouts::author
{
    class MenuFlyoutHost final
    {
    public:
        static void RegisterWindowClass()
        {
            static std::once_flag registered;
            std::call_once(registered, []
            {
                WNDCLASSEXW windowClass{};
                windowClass.cbSize = sizeof(windowClass);
                windowClass.hInstance = GetModuleHandleW(nullptr);
                windowClass.lpfnWndProc = &WindowProc;
                windowClass.lpszClassName = c_menuHostClassName;
                winrt::check_bool(RegisterClassExW(&windowClass) != 0 ||
                    GetLastError() == ERROR_CLASS_ALREADY_EXISTS);
            });
        }

        static LRESULT CALLBACK WindowProc(
            HWND window,
            UINT message,
            WPARAM wParam,
            LPARAM lParam) noexcept
        {
            if (message == WM_NCCREATE)
            {
                auto* create = reinterpret_cast<CREATESTRUCTW*>(lParam);
                SetWindowLongPtrW(window, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(create->lpCreateParams));
            }
            if (message == WM_NCDESTROY)
            {
                SetWindowLongPtrW(window, GWLP_USERDATA, 0);
            }
            return DefWindowProcW(window, message, wParam, lParam);
        }
    };

    DesktopMenuFlyout::DesktopMenuFlyout()
        : m_native(std::make_unique<DesktopMenuFlyoutNativeState>())
    {
        self(this)->DefaultStyleKey(winrt::box_value(L"DesktopFlyouts.DesktopMenuFlyout"));
    }

    DesktopMenuFlyout::~DesktopMenuFlyout()
    {
        try
        {
            Close();
        }
        catch (...)
        {
        }
    }

    void DesktopMenuFlyout::Close(winrt::author::override)
    {
        if (m_isClosed)
        {
            return;
        }
        if (!m_native)
        {
            m_isClosed = true;
            return;
        }
        try
        {
            EnsureUiThread(*m_native);
            Hide();
        }
        catch (...)
        {
        }
        if (m_menuFlyout && m_native->menuClosedToken.value != 0)
        {
            try { m_menuFlyout.Closed(m_native->menuClosedToken); } catch (...) { }
            m_native->menuClosedToken = {};
        }
        if (m_native->xamlSource)
        {
            try
            {
                m_native->xamlSource.Content(nullptr);
                m_native->xamlSource.Close();
            }
            catch (...)
            {
            }
            m_native->xamlSource = nullptr;
        }
        if (m_native->window != nullptr)
        {
            DestroyWindow(m_native->window);
        }
        m_native.reset();
        m_isClosed = true;
    }

    std::int64_t DesktopMenuFlyout::OwnerWindowHandle(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_ownerWindowHandle;
    }

    winrt::author::setter DesktopMenuFlyout::OwnerWindowHandle(std::int64_t value)
    {
        EnsureUiThread(*m_native);
        m_ownerWindowHandle = value;
        m_native->ownerWindow = reinterpret_cast<HWND>(value);
        if (m_native->window != nullptr && IsWindow(m_native->ownerWindow))
        {
            SetWindowLongPtrW(
                m_native->window,
                GWLP_HWNDPARENT,
                reinterpret_cast<LONG_PTR>(m_native->ownerWindow));
        }
        return {};
    }

    Windows::UI::Xaml::Controls::MenuFlyout DesktopMenuFlyout::MenuFlyout(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_menuFlyout;
    }

    winrt::author::setter DesktopMenuFlyout::MenuFlyout(
        Windows::UI::Xaml::Controls::MenuFlyout const& value)
    {
        EnsureUiThread(*m_native);
        if (m_native->menuClosedToken.value != 0 && m_menuFlyout)
        {
            m_menuFlyout.Closed(m_native->menuClosedToken);
            m_native->menuClosedToken = {};
        }
        m_menuFlyout = value;
        if (m_menuFlyout)
        {
            m_native->menuClosedToken = m_menuFlyout.Closed([this](auto const&, auto const&)
            {
                SetIsOpen(false);
                if (m_native && m_native->window != nullptr)
                {
                    ShowWindow(m_native->window, SW_HIDE);
                }
            });
            RebuildMenuFlyoutItems();
        }
        return {};
    }

    Windows::UI::Xaml::DependencyProperty DesktopMenuFlyout::IsOpenProperty(winrt::author::getter)
    {
        static auto property = Windows::UI::Xaml::DependencyProperty::Register(
            L"IsOpen",
            winrt::xaml_typename<bool>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopMenuFlyout>(),
            Windows::UI::Xaml::PropertyMetadata{ winrt::box_value(false) });
        return property;
    }

    bool DesktopMenuFlyout::IsOpen(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return winrt::unbox_value<bool>(self(this)->GetValue(IsOpenProperty()));
    }

    void DesktopMenuFlyout::SetIsOpen(bool value)
    {
        self(this)->SetValue(IsOpenProperty(), winrt::box_value(value));
    }

    void DesktopMenuFlyout::RebuildMenuFlyoutItems()
    {
        EnsureUiThread(*m_native);
        if (!m_menuFlyout)
        {
            MenuFlyout(Windows::UI::Xaml::Controls::MenuFlyout{});
        }
        auto items = m_menuFlyout.Items();
        items.Clear();
        auto source = self(this)->Items();
        for (std::uint32_t index = 0; index < source.Size(); ++index)
        {
            if (auto item = source.GetAt(index).try_as<Windows::UI::Xaml::Controls::MenuFlyoutItemBase>())
            {
                items.Append(item);
            }
        }
    }

    void DesktopMenuFlyout::Show(Windows::Foundation::Point point)
    {
        if (m_isClosed || !m_native)
        {
            return;
        }
        ShowAt(
            static_cast<std::int32_t>(std::lround(point.X)),
            static_cast<std::int32_t>(std::lround(point.Y)));
    }

    void DesktopMenuFlyout::ShowAt(std::int32_t x, std::int32_t y)
    {
        if (m_isClosed || !m_native)
        {
            return;
        }
        EnsureUiThread(*m_native);
        auto owner = m_native->ownerWindow;
        if (owner == nullptr || !IsWindow(owner))
        {
            owner = GetForegroundWindow();
        }
        winrt::check_bool(owner != nullptr);
        if (!m_menuFlyout)
        {
            MenuFlyout(Windows::UI::Xaml::Controls::MenuFlyout{});
        }

        EnsureHost();
        RebuildMenuFlyoutItems();
        m_native->ownerWindow = owner;
        SetWindowLongPtrW(m_native->window, GWLP_HWNDPARENT, reinterpret_cast<LONG_PTR>(owner));
        SetWindowPos(m_native->window, HWND_TOP, x, y, 1, 1, SWP_SHOWWINDOW);
        SetWindowPos(m_native->islandWindow, HWND_TOP, 0, 0, 1, 1, SWP_SHOWWINDOW);
        m_menuFlyout.ShowAt(m_native->target);
        SetIsOpen(true);
    }

    bool DesktopMenuFlyout::TryPreTranslateMessage(std::int64_t message)
    {
        if (m_isClosed || !m_native)
        {
            return false;
        }
        EnsureUiThread(*m_native);
        if (!m_native->xamlSource || message == 0)
        {
            return false;
        }
        try
        {
            auto native = m_native->xamlSource.as<IDesktopWindowXamlSourceNative2>();
            BOOL handled = FALSE;
            winrt::check_hresult(native->PreTranslateMessage(reinterpret_cast<MSG const*>(message), &handled));
            return handled != FALSE;
        }
        catch (...)
        {
            return false;
        }
    }

    void DesktopMenuFlyout::EnsureHost()
    {
        EnsureUiThread(*m_native);
        if (m_native->window != nullptr)
        {
            return;
        }
        EnsureXamlManager();
        MenuFlyoutHost::RegisterWindowClass();
        m_native->window = CreateWindowExW(
            WS_EX_NOREDIRECTIONBITMAP | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
            c_menuHostClassName,
            L"DesktopMenuFlyout.Uwp",
            WS_POPUP | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
            0,
            0,
            1,
            1,
            nullptr,
            nullptr,
            GetModuleHandleW(nullptr),
            m_native.get());
        winrt::check_bool(m_native->window != nullptr);

        m_native->xamlSource = Windows::UI::Xaml::Hosting::DesktopWindowXamlSource{};
        auto native = m_native->xamlSource.as<IDesktopWindowXamlSourceNative2>();
        winrt::check_hresult(native->AttachToWindow(m_native->window));
        winrt::check_hresult(native->get_WindowHandle(&m_native->islandWindow));
        winrt::check_bool(m_native->islandWindow != nullptr);
        SetWindowLongPtrW(m_native->islandWindow, GWL_STYLE, WS_CHILD | WS_VISIBLE);

        m_native->root = Windows::UI::Xaml::Controls::Grid{};
        m_native->target = Windows::UI::Xaml::Controls::Border{};
        m_native->target.Width(1.0);
        m_native->target.Height(1.0);
        m_native->root.Children().Append(m_native->target);
        m_native->xamlSource.Content(m_native->root);
        ShowWindow(m_native->islandWindow, SW_HIDE);
    }

    void DesktopMenuFlyout::Hide()
    {
        if (!m_native)
        {
            return;
        }
        EnsureUiThread(*m_native);
        if (m_menuFlyout)
        {
            m_menuFlyout.Hide();
        }
        SetIsOpen(false);
        if (m_native->islandWindow != nullptr)
        {
            ShowWindow(m_native->islandWindow, SW_HIDE);
        }
        if (m_native->window != nullptr)
        {
            ShowWindow(m_native->window, SW_HIDE);
        }
    }
}
