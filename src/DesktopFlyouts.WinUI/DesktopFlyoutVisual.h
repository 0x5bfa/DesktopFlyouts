#pragma once

#include "author/DesktopFlyout.author.h"
#include "FlyoutInteraction.h"

#include <chrono>
#include <functional>
#include <vector>

#include <winrt/Microsoft.UI.Xaml.Controls.h>
#include <winrt/Microsoft.UI.Xaml.Hosting.h>
#include <winrt/Microsoft.UI.Xaml.Media.Animation.h>

namespace winrt::DesktopFlyouts::detail
{
    class DesktopFlyoutVisual final
    {
    public:
        DesktopFlyoutVisual() = default;
        DesktopFlyoutVisual(DesktopFlyoutVisual const&) = delete;
        DesktopFlyoutVisual& operator=(DesktopFlyoutVisual const&) = delete;
        ~DesktopFlyoutVisual();

        void HideCallback(std::function<void()> callback);
        void SwipeDismissCallback(std::function<void()> callback);
        void SwipeDismissStartedCallback(std::function<void()> callback);
        void SwipeDismissRestoredCallback(std::function<void()> callback);
        void RequestedContent(Microsoft::UI::Xaml::UIElement const& value);
        Microsoft::UI::Xaml::UIElement RequestedContent() const noexcept;
        void Margin(Microsoft::UI::Xaml::Thickness value);
        void SetResolvedSize(double width, double height);
        Windows::Foundation::Size Measure(double availableWidth, double availableHeight);

        void RefreshContent(
            Microsoft::UI::Xaml::Hosting::DesktopWindowXamlSource const& xamlSource,
            Windows::Foundation::Collections::IObservableVector<Microsoft::UI::Xaml::UIElement> const& islands,
            author::DesktopFlyoutOrientation orientation,
            std::int32_t spacing);
        void ApplySystemBackdrop(
            Microsoft::UI::Xaml::Hosting::DesktopWindowXamlSource const& xamlSource,
            bool enabled,
            author::DesktopFlyoutBackdropKind kind);
        void ReleaseIslandBackdrops() noexcept;

        void InteractionConfiguration(bool enabled, double pressedScale, double threshold);
        void FocusConfiguration(bool neverActivate);
        void ActiveDirection(desktop_flyouts::core::popup_direction direction) noexcept;
        void IsOpen(bool value) noexcept;
        void AnimationCallback(std::function<void()> callback);
        void LayoutChangedCallback(std::function<void()> callback);

        void SetRestingVisual();
        void SetClosedVisual(std::int32_t width, std::int32_t height);
        bool BeginOpenAnimation(bool enabled, std::int32_t width, std::int32_t height);
        bool BeginCloseAnimation(bool enabled, std::int32_t width, std::int32_t height);
        void StopAnimation() noexcept;
        bool AnimationClosing() const noexcept;

    private:
        void EnsureRoot();
        void UpdateIslandBackdrops() noexcept;
        void AttachInteractionHandlers();
        void DetachInteractionHandlers() noexcept;
        void ResetPointerVisual();
        void RestorePressedScale();
        void ResetPressedScale();
        void AnimateScale(double scale, std::chrono::milliseconds duration);
        void StopScaleAnimation() noexcept;
        void CompleteScaleAnimation();
        void StopSwipeRestoreAnimation() noexcept;
        void AnimateSwipeRestore();
        void CompleteSwipeRestore();
        bool TryGetSwipeTranslation(double deltaX, double deltaY, double& translateX, double& translateY);
        bool CanStartSwipeDrag(double primaryDistance, double secondaryDistance) const noexcept;
        double GetSwipeDistance() const noexcept;
        double GetSwipeMaxDistance(std::int32_t width, std::int32_t height) const noexcept;
        bool IsFlyoutElement(Microsoft::UI::Xaml::DependencyObject const& element) const;
        void OnRootGettingFocus(
            Microsoft::UI::Xaml::UIElement const& sender,
            Microsoft::UI::Xaml::Input::GettingFocusEventArgs const& args);

        Microsoft::UI::Xaml::UIElement m_defaultContent{ nullptr };
        Microsoft::UI::Xaml::UIElement m_content{ nullptr };
        Microsoft::UI::Xaml::UIElement m_requestedContent{ nullptr };
        Microsoft::UI::Xaml::Controls::Grid m_root{ nullptr };
        Microsoft::UI::Xaml::Media::CompositeTransform m_transform{ nullptr };
        winrt::event_token m_pointerPressedToken{};
        winrt::event_token m_pointerMovedToken{};
        winrt::event_token m_pointerReleasedToken{};
        winrt::event_token m_pointerCanceledToken{};
        winrt::event_token m_pointerCaptureLostToken{};
        winrt::event_token m_sizeChangedToken{};
        std::function<void()> m_hideCallback;
        std::function<void()> m_swipeDismissCallback;
        std::function<void()> m_swipeDismissStartedCallback;
        std::function<void()> m_swipeDismissRestoredCallback;
        std::function<void()> m_animationCallback;
        std::function<void()> m_layoutChangedCallback;
        winrt::event_token m_gettingFocusToken{};

        bool m_contentUsesIslands{};
        std::vector<Microsoft::UI::Xaml::Controls::SystemBackdropElement> m_islandBackdrops;
        bool m_interactionEnabled{};
        bool m_neverActivate{};
        bool m_backdropEnabled{};
        author::DesktopFlyoutBackdropKind m_backdropKind{
            author::DesktopFlyoutBackdropKind::desktop_acrylic };
        double m_pressedScale{ 1.0 };
        double m_swipeDismissThreshold{ 80.0 };
        bool m_isOpen{};
        std::uint32_t m_pointerId{};
        Windows::Foundation::Point m_pointerStart{};
        bool m_pointerCaptured{};
        bool m_swipeTracking{};
        bool m_swipeDragging{};
        desktop_flyouts::core::popup_direction m_activeDirection{
            desktop_flyouts::core::popup_direction::bottom_to_top };
        std::int32_t m_activeWidth{};
        std::int32_t m_activeHeight{};

        Microsoft::UI::Xaml::Media::Animation::Storyboard m_animation{ nullptr };
        winrt::event_token m_animationCompletedToken{};
        bool m_animationClosing{};
        Microsoft::UI::Xaml::Media::Animation::Storyboard m_scaleAnimation{ nullptr };
        winrt::event_token m_scaleCompletedToken{};
        double m_scaleTarget{ 1.0 };
        Microsoft::UI::Xaml::Media::Animation::Storyboard m_swipeRestoreAnimation{ nullptr };
        winrt::event_token m_swipeRestoreCompletedToken{};

        void BeginAnimation(bool closing, std::int32_t width, std::int32_t height);
        void AnimationCompleted();
    };
}
