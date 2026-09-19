#include "pch.h"

#include "author/DesktopMenuFlyout.author.h"

#include <winrt/DesktopFlyouts.h>
#include <winrt/Microsoft.UI.Content.h>
#include <winrt/Microsoft.UI.Interop.h>
#include <winrt/Microsoft.UI.Xaml.Controls.h>
#include <winrt/Microsoft.UI.Xaml.Hosting.h>

#include <mutex>

namespace winrt::DesktopFlyouts::author
{
    struct DesktopMenuFlyoutNativeState
    {
        HWND ownerWindow{};
        HWND window{};
        HWND islandWindow{};
        Microsoft::UI::Xaml::Hosting::DesktopWindowXamlSource xamlSource{ nullptr };
        Microsoft::UI::Xaml::Controls::Grid root{ nullptr };
        Microsoft::UI::Xaml::Controls::Border target{ nullptr };
        winrt::event_token menuClosedToken{};
        DWORD uiThreadId{ GetCurrentThreadId() };
    };
}

namespace
{
    constexpr wchar_t c_menuHostClassName[] = L"DesktopFlyouts.NativeMenuFlyoutHost";

    void EnsureUiThread(winrt::DesktopFlyouts::author::DesktopMenuFlyoutNativeState const& state)
    {
        if (state.uiThreadId != GetCurrentThreadId())
        {
            winrt::throw_hresult(RPC_E_WRONG_THREAD);
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
                SetWindowLongPtrW(
                    window,
                    GWLP_USERDATA,
                    reinterpret_cast<LONG_PTR>(create->lpCreateParams));
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
        m_items = winrt::single_threaded_observable_vector<
            Microsoft::UI::Xaml::Controls::MenuFlyoutItemBase>();
        m_itemsChangedToken = m_items.VectorChanged([this](auto const&, auto const&)
        {
            RebuildMenuFlyoutItems();
        });
    }

    DesktopMenuFlyout::~DesktopMenuFlyout()
    {
        if (m_items && m_itemsChangedToken.value != 0)
        {
            try
            {
                m_items.VectorChanged(m_itemsChangedToken);
            }
            catch (...)
            {
            }
            m_itemsChangedToken = {};
        }

        if (!m_native)
        {
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
        if (m_menuFlyout)
        {
            if (m_native->menuClosedToken.value != 0)
            {
                try
                {
                    m_menuFlyout.Closed(m_native->menuClosedToken);
                }
                catch (...)
                {
                }
                m_native->menuClosedToken = {};
            }

            try
            {
                m_menuFlyout.Items().Clear();
            }
            catch (...)
            {
            }
            m_menuFlyout = nullptr;
        }
        m_items = nullptr;

        if (m_native->xamlSource)
        {
            try
            {
                m_native->xamlSource.Close();
                m_native->xamlSource = nullptr;
            }
            catch (...)
            {
                m_native->xamlSource = nullptr;
            }
        }

        if (m_native->window != nullptr)
        {
            DestroyWindow(m_native->window);
        }
        m_native.reset();
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

    Microsoft::UI::Xaml::Controls::MenuFlyout DesktopMenuFlyout::MenuFlyout(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_menuFlyout;
    }

    winrt::author::setter DesktopMenuFlyout::MenuFlyout(
        Microsoft::UI::Xaml::Controls::MenuFlyout const& value)
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
                m_isOpen = false;
                if (m_native && m_native->window != nullptr)
                {
                    ShowWindow(m_native->window, SW_HIDE);
                }
            });

            if (m_items && m_items.Size() > 0)
            {
                RebuildMenuFlyoutItems();
            }
        }
        return {};
    }

    Windows::Foundation::Collections::IObservableVector<
        Microsoft::UI::Xaml::Controls::MenuFlyoutItemBase> DesktopMenuFlyout::Items(
            winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        if (!m_items)
        {
            m_items = winrt::single_threaded_observable_vector<
                Microsoft::UI::Xaml::Controls::MenuFlyoutItemBase>();
        }
        return m_items;
    }

    void DesktopMenuFlyout::RebuildMenuFlyoutItems()
    {
        EnsureUiThread(*m_native);

        if (!m_items)
        {
            return;
        }

        if (!m_menuFlyout)
        {
            MenuFlyout(Microsoft::UI::Xaml::Controls::MenuFlyout{});
        }

        auto items = m_menuFlyout.Items();
        items.Clear();
        for (std::uint32_t index = 0; index < m_items.Size(); ++index)
        {
            if (auto item = m_items.GetAt(index))
            {
                items.Append(item);
            }
        }
    }

    bool DesktopMenuFlyout::IsOpen(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_isOpen;
    }

    void DesktopMenuFlyout::ShowAt(std::int32_t x, std::int32_t y)
    {
        EnsureUiThread(*m_native);

        auto owner = m_native->ownerWindow;
        if (owner == nullptr || !IsWindow(owner))
        {
            owner = GetForegroundWindow();
        }
        winrt::check_bool(owner != nullptr);

        if (!m_menuFlyout)
        {
            MenuFlyout(Microsoft::UI::Xaml::Controls::MenuFlyout{});
        }

        EnsureHost();
        m_native->ownerWindow = owner;

        SetWindowLongPtrW(m_native->window, GWLP_HWNDPARENT, reinterpret_cast<LONG_PTR>(owner));
        SetWindowPos(
            m_native->window,
            HWND_TOP,
            x,
            y,
            1,
            1,
            SWP_SHOWWINDOW);
        SetWindowPos(
            m_native->islandWindow,
            HWND_TOP,
            0,
            0,
            1,
            1,
            SWP_SHOWWINDOW);
        m_native->xamlSource.SiteBridge().MoveAndResize({ 0, 0, 1, 1 });
        m_native->xamlSource.SiteBridge().Show();
        m_menuFlyout.ShowAt(m_native->target);
        m_isOpen = true;
    }

    void DesktopMenuFlyout::EnsureHost()
    {
        EnsureUiThread(*m_native);
        if (m_native->window != nullptr)
        {
            return;
        }

        MenuFlyoutHost::RegisterWindowClass();
        m_native->window = CreateWindowExW(
            WS_EX_NOREDIRECTIONBITMAP | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
            c_menuHostClassName,
            L"DesktopMenuFlyout",
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

        m_native->xamlSource = Microsoft::UI::Xaml::Hosting::DesktopWindowXamlSource{};
        m_native->xamlSource.Initialize(
            winrt::Microsoft::UI::GetWindowIdFromWindow(m_native->window));
        m_native->islandWindow = winrt::Microsoft::UI::GetWindowFromWindowId(
            m_native->xamlSource.SiteBridge().WindowId());
        winrt::check_bool(m_native->islandWindow != nullptr);

        SetWindowLongPtrW(m_native->islandWindow, GWL_STYLE, WS_CHILD | WS_VISIBLE);
        m_native->root = Microsoft::UI::Xaml::Controls::Grid{};
        m_native->target = Microsoft::UI::Xaml::Controls::Border{};
        m_native->target.Width(1.0);
        m_native->target.Height(1.0);
        m_native->root.Children().Append(m_native->target);
        m_native->xamlSource.Content(m_native->root);
        m_native->xamlSource.SiteBridge().MoveAndResize({ 0, 0, 1, 1 });
        m_native->xamlSource.SiteBridge().Hide();
    }

    void DesktopMenuFlyout::Hide()
    {
        EnsureUiThread(*m_native);
        if (m_menuFlyout)
        {
            if (m_native->xamlSource)
            {
                m_native->xamlSource.SiteBridge().Hide();
            }
            m_menuFlyout.Hide();
        }
        m_isOpen = false;
        if (m_native->window != nullptr)
        {
            ShowWindow(m_native->window, SW_HIDE);
        }
    }
}
