#pragma once

#include "DesktopFlyout.g.h"
#include "XamlIslandHostWindow.h"

namespace winrt::DesktopFlyouts::implementation
{
    struct DesktopFlyout : DesktopFlyoutT<DesktopFlyout>
    {
        DesktopFlyout();
        ~DesktopFlyout();

        static winrt::Microsoft::UI::Xaml::DependencyProperty IslandsProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty IslandsSourceProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty IsBackdropEnabledProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty IsOpenProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty FlyoutWidthProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty FlyoutHeightProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty PopupDirectionProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty IslandsOrientationProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty PlacementProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty MenuFlyoutProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty IsTransitionAnimationEnabledProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty PressedScaleProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty IsSwipeToDismissEnabledProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty SwipeDismissThresholdProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty HideOnLostFocusProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty ActivationModeProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty AutoCloseDelayProperty();
        static winrt::Microsoft::UI::Xaml::DependencyProperty BackdropKindProperty();

        winrt::Windows::Foundation::Collections::IVector<winrt::DesktopFlyouts::DesktopFlyoutIsland> Islands();
        winrt::Windows::Foundation::IInspectable IslandsSource();
        void IslandsSource(winrt::Windows::Foundation::IInspectable const& value);
        bool IsBackdropEnabled();
        void IsBackdropEnabled(bool value);
        bool IsOpen();
        winrt::Microsoft::UI::Xaml::GridLength FlyoutWidth();
        void FlyoutWidth(winrt::Microsoft::UI::Xaml::GridLength const& value);
        winrt::Microsoft::UI::Xaml::GridLength FlyoutHeight();
        void FlyoutHeight(winrt::Microsoft::UI::Xaml::GridLength const& value);
        winrt::DesktopFlyouts::DesktopFlyoutPopupDirection PopupDirection();
        void PopupDirection(winrt::DesktopFlyouts::DesktopFlyoutPopupDirection value);
        winrt::Microsoft::UI::Xaml::Controls::Orientation IslandsOrientation();
        void IslandsOrientation(winrt::Microsoft::UI::Xaml::Controls::Orientation value);
        winrt::DesktopFlyouts::DesktopFlyoutPlacementMode Placement();
        void Placement(winrt::DesktopFlyouts::DesktopFlyoutPlacementMode value);
        winrt::Microsoft::UI::Xaml::Controls::MenuFlyout MenuFlyout();
        void MenuFlyout(winrt::Microsoft::UI::Xaml::Controls::MenuFlyout const& value);
        bool IsTransitionAnimationEnabled();
        void IsTransitionAnimationEnabled(bool value);
        double PressedScale();
        void PressedScale(double value);
        bool IsSwipeToDismissEnabled();
        void IsSwipeToDismissEnabled(bool value);
        double SwipeDismissThreshold();
        void SwipeDismissThreshold(double value);
        bool HideOnLostFocus();
        void HideOnLostFocus(bool value);
        winrt::DesktopFlyouts::DesktopFlyoutActivationMode ActivationMode();
        void ActivationMode(winrt::DesktopFlyouts::DesktopFlyoutActivationMode value);
        winrt::Windows::Foundation::TimeSpan AutoCloseDelay();
        void AutoCloseDelay(winrt::Windows::Foundation::TimeSpan const& value);
        winrt::DesktopFlyouts::DesktopFlyoutBackdropKind BackdropKind();
        void BackdropKind(winrt::DesktopFlyouts::DesktopFlyoutBackdropKind value);

        void Show();
        void Show(winrt::Windows::Foundation::Point const& bottomCenterPoint);
        void Hide();
        void NavigateFocus(winrt::Microsoft::UI::Xaml::Hosting::XamlSourceFocusNavigationReason reason);
        void Close();
        void OnApplyTemplate();

        void OnIslandSizeChanged();
        winrt::Microsoft::UI::Xaml::Media::SystemBackdrop CreateIslandSystemBackdrop();

    private:
        void RegisterPropertyCallbacks();
        void UnregisterPropertyCallbacks();
        void SynchronizeIslandsSource();
        void ClearIslandSubscriptions();
        void UpdateIslands();
        void UpdateIslandBackdrops();
        void UpdateFlyoutTheme();
        void UpdateOpenFlyoutLayout();
        void ResetResolvedFlyoutSize();
        void ApplyResolvedFlyoutSize();
        bool HasStarIslandWidth();
        bool HasStarIslandHeight();
        void ContinueOpenAfterInitialLayout(bool activate);
        void ContinueOpenAfterResolvedLayout(bool activate);
        void PresentOpenFlyout(bool activate);
        bool CanContinueOpening();
        void CancelPendingOpen(bool restoreActivation);
        winrt::DesktopFlyouts::DesktopFlyoutPopupDirection UpdateFlyoutRegion();
        void StartOpenTransition();
        void StartCloseTransition();
        void CompleteOpen();
        void CompleteClose();
        void SetOpenTransform();
        void SetClosedTransform(winrt::DesktopFlyouts::DesktopFlyoutPopupDirection value);
        winrt::Microsoft::UI::Xaml::Media::CompositeTransform RootTransform();
        int ClosedXOffset(winrt::DesktopFlyouts::DesktopFlyoutPopupDirection value);
        int ClosedYOffset(winrt::DesktopFlyouts::DesktopFlyoutPopupDirection value);
        void RestartAutoCloseTimer();
        void StopAutoCloseTimer();
        void AttachRootPointerHandlers();
        void DetachRootPointerHandlers();
        void OnRootPointerPressed(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const&);
        void OnRootPointerMoved(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const&);
        void OnRootPointerReleased(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const&);
        void OnRootPointerCanceled(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const&);
        void OnRootPointerCaptureLost(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const&);
        void OnRootPointerExited(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const&);
        bool CanStartSwipeDismiss();
        void StartSwipeDismiss(winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const&);
        bool UpdateSwipeDismiss(winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const&);
        bool CompleteSwipeDismiss(winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const&);
        void CancelSwipeDismiss();
        void ResetSwipeDismissTracking();
        bool TryGetSwipeDismissTranslation(double deltaX, double deltaY, double& translateX, double& translateY);
        double GetSwipeDismissDistance();
        double GetSwipeDismissMaxDistance();
        double GetResolvedSwipeDismissThreshold();
        void AnimateSwipeDismissRestore();
        void StopSwipeDismissRestoreStoryboard();
        void RestorePressedScale();
        void ResetPressedScale();
        void AnimatePressedScale(double scale, int milliseconds);
        void StopPressedScaleStoryboard();
        double GetResolvedPressedScale();

        static double ResolveLength(winrt::Microsoft::UI::Xaml::GridLength const&, double available, bool stretchWhenAuto);
        static double Clamp(double value, double minimum, double maximum);

        static winrt::Microsoft::UI::Xaml::DependencyProperty s_islandsProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_islandsSourceProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_isBackdropEnabledProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_isOpenProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_flyoutWidthProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_flyoutHeightProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_popupDirectionProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_islandsOrientationProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_placementProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_menuFlyoutProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_isTransitionAnimationEnabledProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_pressedScaleProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_isSwipeToDismissEnabledProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_swipeDismissThresholdProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_hideOnLostFocusProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_activationModeProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_autoCloseDelayProperty;
        static winrt::Microsoft::UI::Xaml::DependencyProperty s_backdropKindProperty;

        struct IslandSubscription
        {
            winrt::DesktopFlyouts::DesktopFlyoutIsland Island{ nullptr };
            int64_t WidthToken{};
            int64_t HeightToken{};
        };

        std::unique_ptr<winrt::DesktopFlyouts::details::XamlIslandHostWindow> m_host;
        std::vector<IslandSubscription> m_islandSubscriptions;
        winrt::event_token m_vectorChangedToken{};
        int64_t m_islandsSourceToken{};
        int64_t m_flyoutWidthToken{};
        int64_t m_flyoutHeightToken{};
        int64_t m_popupDirectionToken{};
        int64_t m_islandsOrientationToken{};
        int64_t m_placementToken{};
        int64_t m_isBackdropEnabledToken{};
        int64_t m_backdropKindToken{};
        int64_t m_activationModeToken{};
        int64_t m_autoCloseDelayToken{};
        winrt::Microsoft::UI::Xaml::Controls::Grid m_rootGrid{ nullptr };
        winrt::Microsoft::UI::Xaml::Controls::Grid m_islandsGrid{ nullptr };
        winrt::Microsoft::UI::Xaml::DispatcherTimer m_autoCloseTimer{ nullptr };
        winrt::event_token m_autoCloseToken{};
        winrt::Microsoft::UI::Xaml::Media::Animation::Storyboard m_storyboard{ nullptr };
        winrt::event_token m_storyboardToken{};
        winrt::Microsoft::UI::Xaml::Media::Animation::Storyboard m_pressScaleStoryboard{ nullptr };
        winrt::event_token m_pressScaleStoryboardToken{};
        winrt::Microsoft::UI::Xaml::Media::Animation::Storyboard m_swipeRestoreStoryboard{ nullptr };
        winrt::event_token m_swipeRestoreStoryboardToken{};
        winrt::event_token m_pointerPressedToken{};
        winrt::event_token m_pointerMovedToken{};
        winrt::event_token m_pointerReleasedToken{};
        winrt::event_token m_pointerCanceledToken{};
        winrt::event_token m_pointerCaptureLostToken{};
        winrt::event_token m_pointerExitedToken{};
        std::optional<winrt::Windows::Foundation::Point> m_customPlacement;
        winrt::Windows::Foundation::Point m_swipeDismissStartPoint{};
        winrt::DesktopFlyouts::DesktopFlyoutPopupDirection m_activeDirection{ winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::BottomToTop };
        uint32_t m_swipeDismissPointerId{};
        bool m_pointerHandlersAttached{};
        bool m_isSwipeDismissTracking{};
        bool m_isSwipeDismissDragging{};
        bool m_isPressAnimationActive{};
        bool m_transitioning{};
        bool m_closed{};
    };
}

namespace winrt::DesktopFlyouts::factory_implementation
{
    struct DesktopFlyout : DesktopFlyoutT<DesktopFlyout, implementation::DesktopFlyout>
    {
    };
}
