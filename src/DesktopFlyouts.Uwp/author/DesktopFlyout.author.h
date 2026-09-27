#pragma once

#include <cstdint>
#include <memory>

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.UI.Xaml.h>
#include <winrt/Windows.UI.Xaml.Controls.h>
#include <winrt/Windows.UI.Xaml.Hosting.h>
#include <winrt/author/base.h>
#ifndef IDLGEN_CPP_STATIC_REFLECTION_PHASE
#include <winrt/DesktopFlyouts.h>
#endif
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

    enum class DesktopFlyoutBackdropKind : std::int32_t
    {
        desktop_acrylic = 0,
        mica = 1,
    };

    struct DesktopFlyoutIslandTemplateSettings :
        winrt::author::runtimeclass<
            winrt::Windows::UI::Xaml::DependencyObject,
            winrt::author::internal<winrt::non_agile>>,
        winrt::author::unsealed
    {
        DesktopFlyoutIslandTemplateSettings();

        winrt::Windows::UI::Xaml::CornerRadius BackdropCornerRadius(winrt::author::getter = {});
        winrt::author::setter BackdropCornerRadius(winrt::Windows::UI::Xaml::CornerRadius value);

        static winrt::Windows::UI::Xaml::DependencyProperty BackdropCornerRadiusProperty(winrt::author::getter = {});
    };

    struct DesktopFlyoutIsland :
        winrt::author::runtimeclass<
            winrt::Windows::UI::Xaml::Controls::ContentControl,
            winrt::author::internal<winrt::non_agile>>,
        winrt::author::unsealed
    {
        DesktopFlyoutIsland();
        ~DesktopFlyoutIsland();

        winrt::Windows::UI::Xaml::GridLength IslandWidth(winrt::author::getter = {});
        winrt::author::setter IslandWidth(winrt::Windows::UI::Xaml::GridLength value);
        winrt::Windows::UI::Xaml::GridLength IslandHeight(winrt::author::getter = {});
        winrt::author::setter IslandHeight(winrt::Windows::UI::Xaml::GridLength value);
#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
        DesktopFlyoutIslandTemplateSettings TemplateSettings(winrt::author::getter = {});
#else
        winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings TemplateSettings(winrt::author::getter = {});
#endif

        static winrt::Windows::UI::Xaml::DependencyProperty IslandWidthProperty(winrt::author::getter = {});
        static winrt::Windows::UI::Xaml::DependencyProperty IslandHeightProperty(winrt::author::getter = {});

    private:
#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
        winrt::Windows::Foundation::IInspectable m_templateSettings{ nullptr };
#else
        winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings m_templateSettings{ nullptr };
#endif
        std::int64_t m_cornerRadiusToken{};
    };

#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
}

namespace winrt::impl
{
    template <>
    struct category<winrt::DesktopFlyouts::author::DesktopFlyoutIsland>
    {
        using type = class_category;
    };

    template <>
    inline constexpr auto& name_v<winrt::DesktopFlyouts::author::DesktopFlyoutIsland> =
        L"DesktopFlyouts.DesktopFlyoutIsland";

    template <>
    struct default_interface<winrt::DesktopFlyouts::author::DesktopFlyoutIsland>
    {
        using type = winrt::Windows::Foundation::IInspectable;
    };
}

namespace winrt::DesktopFlyouts::author
{
#endif

    struct DesktopFlyoutIslandsPanel :
        winrt::author::runtimeclass<
            winrt::Windows::UI::Xaml::Controls::Panel,
            winrt::author::internal<winrt::non_agile>>,
        winrt::author::unsealed
    {
        DesktopFlyoutIslandsPanel();

        winrt::Windows::UI::Xaml::Controls::Orientation Orientation(winrt::author::getter = {});
        winrt::author::setter Orientation(winrt::Windows::UI::Xaml::Controls::Orientation value);
        double Spacing(winrt::author::getter = {});
        winrt::author::setter Spacing(double value);

        static winrt::Windows::UI::Xaml::DependencyProperty OrientationProperty(winrt::author::getter = {});
        static winrt::Windows::UI::Xaml::DependencyProperty SpacingProperty(winrt::author::getter = {});

        winrt::Windows::Foundation::Size MeasureOverride(
            winrt::Windows::Foundation::Size availableSize,
            winrt::author::override = {});
        winrt::Windows::Foundation::Size ArrangeOverride(
            winrt::Windows::Foundation::Size finalSize,
            winrt::author::override = {});
    };

    struct DesktopFlyout :
        winrt::author::runtimeclass<
            winrt::Windows::UI::Xaml::Controls::Control,
            winrt::author::internal<winrt::non_agile>>,
        winrt::author::unsealed
    {
        DesktopFlyout();
        ~DesktopFlyout();

        std::int64_t OwnerWindowHandle(winrt::author::getter = {});
        winrt::author::setter OwnerWindowHandle(std::int64_t value);

        winrt::Windows::UI::Xaml::GridLength FlyoutWidth(winrt::author::getter = {});
        winrt::author::setter FlyoutWidth(winrt::Windows::UI::Xaml::GridLength value);
        winrt::Windows::UI::Xaml::GridLength FlyoutHeight(winrt::author::getter = {});
        winrt::author::setter FlyoutHeight(winrt::Windows::UI::Xaml::GridLength value);

        DesktopFlyoutPlacementMode Placement(winrt::author::getter = {});
        winrt::author::setter Placement(DesktopFlyoutPlacementMode value);
        DesktopFlyoutPopupDirection PopupDirection(winrt::author::getter = {});
        winrt::author::setter PopupDirection(DesktopFlyoutPopupDirection value);
        DesktopFlyoutActivationMode ActivationMode(winrt::author::getter = {});
        winrt::author::setter ActivationMode(DesktopFlyoutActivationMode value);
        bool HideOnLostFocus(winrt::author::getter = {});
        winrt::author::setter HideOnLostFocus(bool value);

        winrt::Windows::UI::Xaml::UIElement Content(winrt::author::getter = {});
        winrt::author::setter Content(winrt::Windows::UI::Xaml::UIElement const& value);
        winrt::Windows::UI::Xaml::Controls::MenuFlyout MenuFlyout(winrt::author::getter = {});
        winrt::author::setter MenuFlyout(winrt::Windows::UI::Xaml::Controls::MenuFlyout const& value);

#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
        winrt::Windows::Foundation::Collections::IObservableVector<DesktopFlyoutIsland> Islands(
#else
        winrt::Windows::Foundation::Collections::IObservableVector<winrt::DesktopFlyouts::DesktopFlyoutIsland> Islands(
#endif
            winrt::author::getter = {});
        winrt::Windows::Foundation::IInspectable IslandsSource(winrt::author::getter = {});
        winrt::author::setter IslandsSource(winrt::Windows::Foundation::IInspectable const& value);
        winrt::Windows::UI::Xaml::Controls::Orientation IslandsOrientation(winrt::author::getter = {});
        winrt::author::setter IslandsOrientation(winrt::Windows::UI::Xaml::Controls::Orientation value);
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
        void Show(winrt::Windows::Foundation::Point bottomCenterPoint);
        void ShowAt(std::int32_t x, std::int32_t y);
        void NavigateFocus();
        void NavigateFocus(winrt::Windows::UI::Xaml::Hosting::XamlSourceFocusNavigationReason reason);
        bool TryPreTranslateMessage(std::int64_t message);
        void Hide();

    private:
        std::int64_t m_ownerWindowHandle{};
        winrt::Windows::UI::Xaml::GridLength m_flyoutWidth{ 1.0, winrt::Windows::UI::Xaml::GridUnitType::Auto };
        winrt::Windows::UI::Xaml::GridLength m_flyoutHeight{ 1.0, winrt::Windows::UI::Xaml::GridUnitType::Auto };
        DesktopFlyoutPlacementMode m_placement{ DesktopFlyoutPlacementMode::bottom_right };
        DesktopFlyoutPopupDirection m_popupDirection{ DesktopFlyoutPopupDirection::vertical };
        DesktopFlyoutActivationMode m_activationMode{ DesktopFlyoutActivationMode::activate };
        bool m_hideOnLostFocus{ true };
        winrt::Windows::UI::Xaml::UIElement m_content{ nullptr };
        winrt::Windows::UI::Xaml::Controls::MenuFlyout m_menuFlyout{ nullptr };
        winrt::Windows::UI::Xaml::Controls::Orientation m_islandsOrientation{
            winrt::Windows::UI::Xaml::Controls::Orientation::Vertical };
        std::int32_t m_islandSpacing{ 12 };
        bool m_isBackdropEnabled{ true };
        DesktopFlyoutBackdropKind m_backdropKind{ DesktopFlyoutBackdropKind::desktop_acrylic };
        bool m_isTransitionAnimationEnabled{ true };
        double m_pressedScale{ 1.0 };
        bool m_isSwipeToDismissEnabled{ false };
        double m_swipeDismissThreshold{ 80.0 };
        winrt::Windows::Foundation::TimeSpan m_autoCloseDelay{};
#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
        winrt::Windows::Foundation::Collections::IObservableVector<DesktopFlyoutIsland> m_islands{ nullptr };
#else
        winrt::Windows::Foundation::Collections::IObservableVector<winrt::DesktopFlyouts::DesktopFlyoutIsland> m_islands{ nullptr };
#endif
        winrt::Windows::Foundation::IInspectable m_islandsSource{ nullptr };
        winrt::event_token m_islandsChangedToken{};
        std::unique_ptr<DesktopFlyoutNativeState> m_native;

        void RefreshContent();
        void UpdateFlyoutLayout();
        void ShowCore();
    };
}
