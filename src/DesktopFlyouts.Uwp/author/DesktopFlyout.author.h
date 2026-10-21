#pragma once

#include <cstdint>
#include <functional>
#include <memory>
#include <utility>
#include <vector>

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
        TopCenter = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomCenter = 3,
        BottomLeft = 4,
        BottomRight = 5,
        LeftCenter = 6,
        RightCenter = 7,
    };

    enum class DesktopFlyoutPopupDirection : std::int32_t
    {
        BottomToTop = 0,
        TopToBottom = 1,
        Vertical = 2,
        LeftToRight = 3,
        RightToLeft = 4,
        Horizontal = 5,
    };

    enum class DesktopFlyoutActivationMode : std::int32_t
    {
        Activate = 0,
        NoActivateOnOpen = 1,
        NeverActivate = 2,
    };

    enum class DesktopFlyoutBackdropKind : std::int32_t
    {
        DesktopAcrylic = 0,
        Mica = 1,
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
            winrt::Windows::Foundation::IClosable,
            winrt::author::internal<winrt::non_agile>>,
        winrt::author::apply_attr<
            winrt::author::contentproperty,
            winrt::author::attr_string{ "Islands" }>,
        winrt::author::unsealed
    {
        DesktopFlyout();
        ~DesktopFlyout();
        void Close(winrt::author::override = {});

        std::int64_t OwnerWindowHandle(winrt::author::getter = {});
        winrt::author::setter OwnerWindowHandle(std::int64_t value);

        winrt::Windows::UI::Xaml::GridLength FlyoutWidth(winrt::author::getter = {});
        winrt::author::setter FlyoutWidth(winrt::Windows::UI::Xaml::GridLength value);
        static winrt::Windows::UI::Xaml::DependencyProperty FlyoutWidthProperty(winrt::author::getter = {});
        winrt::Windows::UI::Xaml::GridLength FlyoutHeight(winrt::author::getter = {});
        winrt::author::setter FlyoutHeight(winrt::Windows::UI::Xaml::GridLength value);
        static winrt::Windows::UI::Xaml::DependencyProperty FlyoutHeightProperty(winrt::author::getter = {});

        DesktopFlyoutPlacementMode Placement(winrt::author::getter = {});
        winrt::author::setter Placement(DesktopFlyoutPlacementMode value);
        static winrt::Windows::UI::Xaml::DependencyProperty PlacementProperty(winrt::author::getter = {});
        DesktopFlyoutPopupDirection PopupDirection(winrt::author::getter = {});
        winrt::author::setter PopupDirection(DesktopFlyoutPopupDirection value);
        static winrt::Windows::UI::Xaml::DependencyProperty PopupDirectionProperty(winrt::author::getter = {});
        DesktopFlyoutActivationMode ActivationMode(winrt::author::getter = {});
        winrt::author::setter ActivationMode(DesktopFlyoutActivationMode value);
        static winrt::Windows::UI::Xaml::DependencyProperty ActivationModeProperty(winrt::author::getter = {});
        bool HideOnLostFocus(winrt::author::getter = {});
        winrt::author::setter HideOnLostFocus(bool value);
        static winrt::Windows::UI::Xaml::DependencyProperty HideOnLostFocusProperty(winrt::author::getter = {});

        winrt::Windows::UI::Xaml::UIElement Content(winrt::author::getter = {});
        winrt::author::setter Content(winrt::Windows::UI::Xaml::UIElement const& value);
        winrt::Windows::UI::Xaml::Controls::MenuFlyout MenuFlyout(winrt::author::getter = {});
        winrt::author::setter MenuFlyout(winrt::Windows::UI::Xaml::Controls::MenuFlyout const& value);
        static winrt::Windows::UI::Xaml::DependencyProperty MenuFlyoutProperty(winrt::author::getter = {});

#ifdef IDLGEN_CPP_STATIC_REFLECTION_PHASE
        winrt::Windows::Foundation::Collections::IObservableVector<DesktopFlyoutIsland> Islands(
#else
        winrt::Windows::Foundation::Collections::IObservableVector<winrt::DesktopFlyouts::DesktopFlyoutIsland> Islands(
#endif
            winrt::author::getter = {});
        winrt::Windows::Foundation::IInspectable IslandsSource(winrt::author::getter = {});
        winrt::author::setter IslandsSource(winrt::Windows::Foundation::IInspectable const& value);
        static winrt::Windows::UI::Xaml::DependencyProperty IslandsSourceProperty(winrt::author::getter = {});
        winrt::Windows::UI::Xaml::Controls::Orientation IslandsOrientation(winrt::author::getter = {});
        winrt::author::setter IslandsOrientation(winrt::Windows::UI::Xaml::Controls::Orientation value);
        static winrt::Windows::UI::Xaml::DependencyProperty IslandsOrientationProperty(winrt::author::getter = {});
        std::int32_t IslandSpacing(winrt::author::getter = {});
        winrt::author::setter IslandSpacing(std::int32_t value);

        bool IsBackdropEnabled(winrt::author::getter = {});
        winrt::author::setter IsBackdropEnabled(bool value);
        static winrt::Windows::UI::Xaml::DependencyProperty IsBackdropEnabledProperty(winrt::author::getter = {});
        DesktopFlyoutBackdropKind BackdropKind(winrt::author::getter = {});
        winrt::author::setter BackdropKind(DesktopFlyoutBackdropKind value);
        static winrt::Windows::UI::Xaml::DependencyProperty BackdropKindProperty(winrt::author::getter = {});
        bool IsTransitionAnimationEnabled(winrt::author::getter = {});
        winrt::author::setter IsTransitionAnimationEnabled(bool value);
        static winrt::Windows::UI::Xaml::DependencyProperty IsTransitionAnimationEnabledProperty(winrt::author::getter = {});
        double PressedScale(winrt::author::getter = {});
        winrt::author::setter PressedScale(double value);
        static winrt::Windows::UI::Xaml::DependencyProperty PressedScaleProperty(winrt::author::getter = {});
        bool IsSwipeToDismissEnabled(winrt::author::getter = {});
        winrt::author::setter IsSwipeToDismissEnabled(bool value);
        static winrt::Windows::UI::Xaml::DependencyProperty IsSwipeToDismissEnabledProperty(winrt::author::getter = {});
        double SwipeDismissThreshold(winrt::author::getter = {});
        winrt::author::setter SwipeDismissThreshold(double value);
        static winrt::Windows::UI::Xaml::DependencyProperty SwipeDismissThresholdProperty(winrt::author::getter = {});
        winrt::Windows::Foundation::TimeSpan AutoCloseDelay(winrt::author::getter = {});
        winrt::author::setter AutoCloseDelay(winrt::Windows::Foundation::TimeSpan value);
        static winrt::Windows::UI::Xaml::DependencyProperty AutoCloseDelayProperty(winrt::author::getter = {});

        DesktopFlyoutState State(winrt::author::getter = {});
        bool IsOpen(winrt::author::getter = {});
        static winrt::Windows::UI::Xaml::DependencyProperty IsOpenProperty(winrt::author::getter = {});

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
        DesktopFlyoutPlacementMode m_placement{ DesktopFlyoutPlacementMode::BottomRight };
        DesktopFlyoutPopupDirection m_popupDirection{ DesktopFlyoutPopupDirection::Vertical };
        DesktopFlyoutActivationMode m_activationMode{ DesktopFlyoutActivationMode::Activate };
        bool m_hideOnLostFocus{ true };
        winrt::Windows::UI::Xaml::UIElement m_content{ nullptr };
        winrt::Windows::UI::Xaml::Controls::MenuFlyout m_menuFlyout{ nullptr };
        winrt::Windows::UI::Xaml::Controls::Orientation m_islandsOrientation{
            winrt::Windows::UI::Xaml::Controls::Orientation::Vertical };
        std::int32_t m_islandSpacing{ 12 };
        bool m_isBackdropEnabled{ true };
        DesktopFlyoutBackdropKind m_backdropKind{ DesktopFlyoutBackdropKind::DesktopAcrylic };
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
        std::vector<std::pair<winrt::Windows::UI::Xaml::DependencyProperty, std::int64_t>> m_propertyCallbacks;

        void RefreshContent();
        void UpdateFlyoutLayout();
        void ShowCore();
        void RegisterDependencyPropertyCallbacks();
        void RegisterDependencyPropertyChangedCallback(
            winrt::Windows::UI::Xaml::DependencyProperty const& property,
            std::function<void()> callback);
        void SetIsOpen(bool value);
        bool m_isClosed{};
    };
}
