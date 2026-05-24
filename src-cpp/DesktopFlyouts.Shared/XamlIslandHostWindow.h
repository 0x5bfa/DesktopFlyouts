#pragma once
#include <winrt/DesktopFlyouts.h>

namespace winrt::DesktopFlyouts::details
{
    class XamlIslandHostWindow
    {
    public:
        XamlIslandHostWindow();
        ~XamlIslandHostWindow();

        XamlIslandHostWindow(XamlIslandHostWindow const&) = delete;
        XamlIslandHostWindow& operator=(XamlIslandHostWindow const&) = delete;

        void SetContent(winrt::Microsoft::UI::Xaml::UIElement const& content);
        void PreserveActivationState() noexcept;
        void RestoreActivationState() noexcept;
        void MoveAndResize(winrt::Windows::Graphics::RectInt32 const& rect, bool activate = true) noexcept;
        void Maximize(RECT const& workArea, bool activate = true) noexcept;
        void SetRectRegion(winrt::Windows::Graphics::RectInt32 const& rect) noexcept;
        void SetVisible(bool visible, bool activate = true);
        void SetActivationMode(winrt::DesktopFlyouts::DesktopFlyoutActivationMode value) noexcept;
        bool NavigateFocus(winrt::Microsoft::UI::Xaml::Hosting::XamlSourceFocusNavigationReason reason);
        double RasterizationScale() const noexcept;
        bool HasSource() const noexcept;
        void Close() noexcept;

        std::function<void()> WindowInactivated;
        std::function<void()> SystemSettingsChanged;

    private:
        static LRESULT CALLBACK WindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept;
        LRESULT InstanceWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept;
        static void SetWindowRectRegion(HWND hwnd, winrt::Windows::Graphics::RectInt32 const& rect) noexcept;
        static void SetNoActivateStyle(HWND hwnd, bool enabled) noexcept;
        void ApplyActivationMode() noexcept;

        std::wstring m_className;
        HWND m_hwnd{};
        HWND m_xamlHwnd{};
        HWND m_preservedForeground{};
        HWND m_preservedActive{};
        HWND m_preservedFocus{};
        winrt::Microsoft::UI::Xaml::Hosting::DesktopWindowXamlSource m_source{ nullptr };
        winrt::DesktopFlyouts::DesktopFlyoutActivationMode m_activationMode{ winrt::DesktopFlyouts::DesktopFlyoutActivationMode::Activate };
        bool m_closed{};
    };
}
