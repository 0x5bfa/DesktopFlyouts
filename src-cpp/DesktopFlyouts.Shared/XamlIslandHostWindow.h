#pragma once
#include <mutex>
#include <unordered_map>
#include <vector>
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
        static LRESULT CALLBACK CbtHookProc(int code, WPARAM wParam, LPARAM lParam) noexcept;
        static LRESULT CALLBACK WindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept;
        static LRESULT CALLBACK XamlWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept;
        LRESULT InstanceWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept;
        LRESULT InstanceXamlWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept;
        LRESULT CallPreviousXamlWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) noexcept;
        bool IsFlyoutWindow(HWND hwnd) const noexcept;
        static void SetWindowRectRegion(HWND hwnd, winrt::Windows::Graphics::RectInt32 const& rect) noexcept;
        static void SetNoActivateStyle(HWND hwnd, bool enabled) noexcept;
        void ApplyActivationMode() noexcept;
        void UpdateCbtHook(bool enabled) noexcept;
        void EnsureCbtHook() noexcept;
        void RemoveCbtHook() noexcept;
        void RegisterCbtHookTarget(DWORD threadId) noexcept;
        void UnregisterCbtHookTarget(DWORD threadId) noexcept;
        void RefreshXamlIslandWindowSubclasses() noexcept;
        void SubclassChildWindows(HWND parentHwnd) noexcept;
        void SubclassXamlIslandWindow(HWND hwnd) noexcept;
        void UnsubclassXamlIslandWindows() noexcept;

        static std::mutex s_cbtTargetsMutex;
        static std::unordered_map<DWORD, std::vector<XamlIslandHostWindow*>> s_cbtTargetsByThread;

        std::wstring m_className;
        HWND m_hwnd{};
        HWND m_xamlHwnd{};
        HWND m_preservedForeground{};
        HWND m_preservedActive{};
        HWND m_preservedFocus{};
        HHOOK m_cbtHook{};
        DWORD m_cbtHookThreadId{};
        std::unordered_map<HWND, WNDPROC> m_subclassedXamlWindowProcedures;
        winrt::Microsoft::UI::Xaml::Hosting::DesktopWindowXamlSource m_source{ nullptr };
        winrt::DesktopFlyouts::DesktopFlyoutActivationMode m_activationMode{ winrt::DesktopFlyouts::DesktopFlyoutActivationMode::Activate };
        bool m_closed{};
    };
}
