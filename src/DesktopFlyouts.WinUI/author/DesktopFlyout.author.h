#pragma once

#include <cstdint>
#include <memory>

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Microsoft.UI.Xaml.h>
#include <winrt/Microsoft.UI.Xaml.Controls.Primitives.h>
#include <winrt/Microsoft.UI.Xaml.Controls.h>
#include <winrt/Microsoft.UI.Xaml.Hosting.h>
#include <winrt/author/base.h>
#undef GetCurrentTime

namespace winrt::DesktopFlyouts::author
{
    struct DesktopFlyoutNativeState;

    enum class DesktopFlyoutState : std::int32_t
    {
        closed,
        open,
    };

    enum class DesktopFlyoutPlacementMode : std::int32_t
    {
        top_center = 0,
        top_left = 1,
        top_right = 2,
        bottom_center = 3,
        bottom_left = 4,
        bottom_right = 5,
        left_center = 6,
        right_center = 7,
    };

    enum class DesktopFlyoutPopupDirection : std::int32_t
    {
        bottom_to_top = 0,
        top_to_bottom = 1,
        vertical = 2,
        left_to_right = 3,
        right_to_left = 4,
        horizontal = 5,
    };

    enum class DesktopFlyoutActivationMode : std::int32_t
    {
        activate = 0,
        no_activate_on_open = 1,
        never_activate = 2,
    };

    enum class DesktopFlyoutOrientation : std::int32_t
    {
        vertical = 0,
        horizontal = 1,
    };

    enum class DesktopFlyoutBackdropKind : std::int32_t
    {
        desktop_acrylic = 0,
        mica = 1,
    };

    // The implementation owns HWND/XAML objects and is bound to the creating UI
    // thread. IdlGen's internal-base escape hatch keeps this out of the public IDL
    // while making the generated C++/WinRT implementation non-agile.
    struct DesktopFlyout : winrt::author::runtimeclass<winrt::author::internal<winrt::non_agile>>
    {
        DesktopFlyout();
        ~DesktopFlyout();

        std::int64_t OwnerWindowHandle(winrt::author::getter = {});
        winrt::author::setter OwnerWindowHandle(std::int64_t value);

        std::int32_t Width(winrt::author::getter = {});
        winrt::author::setter Width(std::int32_t value);

        std::int32_t Height(winrt::author::getter = {});
        winrt::author::setter Height(std::int32_t value);

        winrt::Microsoft::UI::Xaml::GridLength FlyoutWidth(winrt::author::getter = {});
        winrt::author::setter FlyoutWidth(winrt::Microsoft::UI::Xaml::GridLength value);

        winrt::Microsoft::UI::Xaml::GridLength FlyoutHeight(winrt::author::getter = {});
        winrt::author::setter FlyoutHeight(winrt::Microsoft::UI::Xaml::GridLength value);

        winrt::Microsoft::UI::Xaml::Thickness Margin(winrt::author::getter = {});
        winrt::author::setter Margin(winrt::Microsoft::UI::Xaml::Thickness value);

        DesktopFlyoutPlacementMode Placement(winrt::author::getter = {});
        winrt::author::setter Placement(DesktopFlyoutPlacementMode value);

        DesktopFlyoutPopupDirection PopupDirection(winrt::author::getter = {});
        winrt::author::setter PopupDirection(DesktopFlyoutPopupDirection value);

        DesktopFlyoutActivationMode ActivationMode(winrt::author::getter = {});
        winrt::author::setter ActivationMode(DesktopFlyoutActivationMode value);

        bool HideOnLostFocus(winrt::author::getter = {});
        winrt::author::setter HideOnLostFocus(bool value);

        winrt::Microsoft::UI::Xaml::UIElement Content(winrt::author::getter = {});
        winrt::author::setter Content(winrt::Microsoft::UI::Xaml::UIElement const& value);

        winrt::Microsoft::UI::Xaml::Controls::MenuFlyout MenuFlyout(winrt::author::getter = {});
        winrt::author::setter MenuFlyout(winrt::Microsoft::UI::Xaml::Controls::MenuFlyout const& value);

        winrt::Windows::Foundation::Collections::IObservableVector<winrt::Microsoft::UI::Xaml::UIElement> Islands(
            winrt::author::getter = {});

        DesktopFlyoutOrientation IslandsOrientation(winrt::author::getter = {});
        winrt::author::setter IslandsOrientation(DesktopFlyoutOrientation value);

        std::int32_t IslandSpacing(winrt::author::getter = {});
        winrt::author::setter IslandSpacing(std::int32_t value);

        bool IsBackdropEnabled(winrt::author::getter = {});
        winrt::author::setter IsBackdropEnabled(bool value);

        DesktopFlyoutBackdropKind BackdropKind(winrt::author::getter = {});
        winrt::author::setter BackdropKind(DesktopFlyoutBackdropKind value);

        bool IsTransitionAnimationEnabled(winrt::author::getter = {});
        winrt::author::setter IsTransitionAnimationEnabled(bool value);

        double PressedScale(winrt::author::getter = {});
        winrt::author::setter PressedScale(double value);

        bool IsSwipeToDismissEnabled(winrt::author::getter = {});
        winrt::author::setter IsSwipeToDismissEnabled(bool value);

        double SwipeDismissThreshold(winrt::author::getter = {});
        winrt::author::setter SwipeDismissThreshold(double value);

        winrt::Windows::Foundation::TimeSpan AutoCloseDelay(winrt::author::getter = {});
        winrt::author::setter AutoCloseDelay(winrt::Windows::Foundation::TimeSpan value);

        DesktopFlyoutState State(winrt::author::getter = {});
        bool IsOpen(winrt::author::getter = {});

        void Show();
        void ShowAt(std::int32_t x, std::int32_t y);
        void NavigateFocus();
        void Hide();

    private:
        std::int64_t m_ownerWindowHandle{};
        std::int32_t m_width{ 360 };
        std::int32_t m_height{ 190 };
        bool m_legacyWidthExplicit{};
        bool m_legacyHeightExplicit{};
        winrt::Microsoft::UI::Xaml::GridLength m_flyoutWidth{ 1.0, winrt::Microsoft::UI::Xaml::GridUnitType::Auto };
        winrt::Microsoft::UI::Xaml::GridLength m_flyoutHeight{ 1.0, winrt::Microsoft::UI::Xaml::GridUnitType::Auto };
        winrt::Microsoft::UI::Xaml::Thickness m_margin{ 12.0, 12.0, 12.0, 12.0 };
        DesktopFlyoutPlacementMode m_placement{ DesktopFlyoutPlacementMode::bottom_right };
        DesktopFlyoutPopupDirection m_popupDirection{ DesktopFlyoutPopupDirection::vertical };
        DesktopFlyoutActivationMode m_activationMode{ DesktopFlyoutActivationMode::activate };
        bool m_hideOnLostFocus{ true };
        winrt::Microsoft::UI::Xaml::Controls::MenuFlyout m_menuFlyout{ nullptr };
        DesktopFlyoutOrientation m_islandsOrientation{ DesktopFlyoutOrientation::vertical };
        std::int32_t m_islandSpacing{ 12 };
        bool m_isBackdropEnabled{ true };
        DesktopFlyoutBackdropKind m_backdropKind{ DesktopFlyoutBackdropKind::desktop_acrylic };
        bool m_isTransitionAnimationEnabled{ true };
        double m_pressedScale{ 1.0 };
        bool m_isSwipeToDismissEnabled{ false };
        double m_swipeDismissThreshold{ 80.0 };
        winrt::Windows::Foundation::TimeSpan m_autoCloseDelay{};
        DesktopFlyoutState m_state{ DesktopFlyoutState::closed };
        winrt::Windows::Foundation::Collections::IObservableVector<winrt::Microsoft::UI::Xaml::UIElement> m_islands{ nullptr };
        winrt::event_token m_islandsChangedToken{};
        std::unique_ptr<DesktopFlyoutNativeState> m_native;

        void RefreshContent();
        void UpdateFlyoutLayout(bool opening);
        void ShowCore();
        void AttachInteractionHandlers();
        void DetachInteractionHandlers();
        void BeginOpenAnimation();
        void BeginCloseAnimation(bool fromCurrentTransform = false);
        void TickAnimation();
        void CompleteOpen();
        void CompleteClose();
    };
}
