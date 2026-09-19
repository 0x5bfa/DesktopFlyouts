#include "pch.h"

#include "author/DesktopFlyout.author.h"
#include "DesktopFlyoutHost.h"
#include "DesktopFlyoutVisual.h"
#include "FlyoutLayout.h"
#include "winrt/DesktopFlyouts.h"

#include <winrt/Microsoft.UI.Xaml.Hosting.h>

#include <algorithm>
#include <cmath>
#include <limits>
#include <optional>
#include <utility>

namespace winrt::DesktopFlyouts::author
{
    struct DesktopFlyoutNativeState
    {
        detail::DesktopFlyoutHost host;
        detail::DesktopFlyoutVisual visual;
        std::optional<POINT> customPlacementPoint;
        std::optional<desktop_flyouts::core::rect> activeBounds;
        desktop_flyouts::core::popup_direction activeDirection{
            desktop_flyouts::core::popup_direction::bottom_to_top };
        desktop_flyouts::core::flyout_state_machine lifecycle{};
        DesktopFlyoutActivationMode activationMode{ DesktopFlyoutActivationMode::activate };
        bool hideOnLostFocus{ true };
        bool isOpen{};
        std::int32_t activeWidth{};
        std::int32_t activeHeight{};
        bool updatingLayout{};
        bool refreshingContent{};
        DWORD uiThreadId{ GetCurrentThreadId() };
    };
}

namespace
{
    desktop_flyouts::core::placement_mode ToCorePlacement(
        winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode placement) noexcept
    {
        switch (placement)
        {
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::top_center:
            return desktop_flyouts::core::placement_mode::top_center;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::top_left:
            return desktop_flyouts::core::placement_mode::top_left;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::top_right:
            return desktop_flyouts::core::placement_mode::top_right;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::bottom_center:
            return desktop_flyouts::core::placement_mode::bottom_center;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::bottom_left:
            return desktop_flyouts::core::placement_mode::bottom_left;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::bottom_right:
            return desktop_flyouts::core::placement_mode::bottom_right;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::left_center:
            return desktop_flyouts::core::placement_mode::left_center;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::right_center:
            return desktop_flyouts::core::placement_mode::right_center;
        default:
            return desktop_flyouts::core::placement_mode::bottom_right;
        }
    }

    desktop_flyouts::core::popup_direction ToCoreDirection(
        winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection direction) noexcept
    {
        switch (direction)
        {
        case winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection::bottom_to_top:
            return desktop_flyouts::core::popup_direction::bottom_to_top;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection::top_to_bottom:
            return desktop_flyouts::core::popup_direction::top_to_bottom;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection::vertical:
            return desktop_flyouts::core::popup_direction::vertical;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection::left_to_right:
            return desktop_flyouts::core::popup_direction::left_to_right;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection::right_to_left:
            return desktop_flyouts::core::popup_direction::right_to_left;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection::horizontal:
            return desktop_flyouts::core::popup_direction::horizontal;
        default:
            return desktop_flyouts::core::popup_direction::vertical;
        }
    }

    void EnsureUiThread(winrt::DesktopFlyouts::author::DesktopFlyoutNativeState const& state)
    {
        if (state.uiThreadId != GetCurrentThreadId())
        {
            winrt::throw_hresult(RPC_E_WRONG_THREAD);
        }
    }

    RECT GetFlyoutWorkArea(HWND ownerWindow, std::optional<POINT> const& anchorPoint) noexcept
    {
        HMONITOR monitor = anchorPoint.has_value()
            ? MonitorFromPoint(*anchorPoint, MONITOR_DEFAULTTONEAREST)
            : MonitorFromWindow(ownerWindow, MONITOR_DEFAULTTONEAREST);

        MONITORINFO monitorInfo{ sizeof(monitorInfo) };
        if (monitor != nullptr && GetMonitorInfoW(monitor, &monitorInfo))
        {
            return monitorInfo.rcWork;
        }

        RECT workArea{};
        if (SystemParametersInfoW(SPI_GETWORKAREA, 0, &workArea, 0))
        {
            return workArea;
        }

        return { 0, 0, GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN) };
    }

    double ResolveFlyoutLength(
        winrt::Microsoft::UI::Xaml::GridLength length,
        double available,
        double fallback) noexcept
    {
        if (length.GridUnitType == winrt::Microsoft::UI::Xaml::GridUnitType::Star)
        {
            return available;
        }

        if (length.GridUnitType == winrt::Microsoft::UI::Xaml::GridUnitType::Pixel &&
            std::isfinite(length.Value))
        {
            return std::clamp(length.Value, 0.0, available);
        }

        return fallback;
    }
}

namespace winrt::DesktopFlyouts::author
{
    DesktopFlyout::DesktopFlyout()
        : m_native(std::make_unique<DesktopFlyoutNativeState>())
    {
        m_islands = winrt::single_threaded_observable_vector<Microsoft::UI::Xaml::UIElement>();
        m_islandsChangedToken = m_islands.VectorChanged([this](auto const&, auto const&)
        {
            RefreshContent();
        });

        m_native->host.HideCallback([this]
        {
            Hide();
        });
        m_native->host.SystemSettingsCallback([this]
        {
            if (!m_native)
            {
                return;
            }

            RefreshContent();
            m_native->visual.ApplySystemBackdrop(
                m_native->host.XamlSource(),
                m_isBackdropEnabled,
                m_backdropKind);
        });
        m_native->visual.AnimationCallback([this]
        {
            TickAnimation();
        });
        m_native->visual.LayoutChangedCallback([this]
        {
            UpdateFlyoutLayout(false);
        });
        m_native->visual.HideCallback([this]
        {
            Hide();
        });
        m_native->visual.SwipeDismissCallback([this]
        {
            BeginCloseAnimation(true);
        });
        m_native->visual.SwipeDismissStartedCallback([this]
        {
            if (m_native)
            {
                m_native->host.StopAutoCloseTimer();
            }
        });
        m_native->visual.SwipeDismissRestoredCallback([this]
        {
            if (m_native && m_native->isOpen)
            {
                m_native->host.ConfigureAutoCloseTimer(m_autoCloseDelay);
            }
        });
        m_native->host.ActivationMode(m_activationMode);
        m_native->host.HideOnLostFocus(m_hideOnLostFocus);
    }

    DesktopFlyout::~DesktopFlyout()
    {
        if (m_islands && m_islandsChangedToken.value != 0)
        {
            try
            {
                m_islands.VectorChanged(m_islandsChangedToken);
            }
            catch (...)
            {
            }
            m_islandsChangedToken = {};
        }

        if (m_native)
        {
            try { m_native->host.HideCallback({}); } catch (...) { }
            try { m_native->visual.HideCallback({}); } catch (...) { }
            try { m_native->visual.SwipeDismissCallback({}); } catch (...) { }
            try { m_native->visual.SwipeDismissStartedCallback({}); } catch (...) { }
            try { m_native->visual.SwipeDismissRestoredCallback({}); } catch (...) { }
            try { m_native->visual.AnimationCallback({}); } catch (...) { }
            try { m_native->visual.LayoutChangedCallback({}); } catch (...) { }
            try { m_native->host.SystemSettingsCallback({}); } catch (...) { }
            // DesktopFlyoutNativeState stores the visual after the host, so
            // reset destroys the visual first. Its XAML event handlers and
            // island children must be detached while the XAML source is still
            // alive; closing the host before that teardown leaves XAML with a
            // dangling root and can fault during source shutdown.
            m_native.reset();
        }
    }

    std::int64_t DesktopFlyout::OwnerWindowHandle(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_ownerWindowHandle;
    }

    winrt::author::setter DesktopFlyout::OwnerWindowHandle(std::int64_t value)
    {
        EnsureUiThread(*m_native);
        m_ownerWindowHandle = value;
        m_native->host.OwnerWindow(reinterpret_cast<HWND>(value));
        return {};
    }

    std::int32_t DesktopFlyout::Width(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_width;
    }

    winrt::author::setter DesktopFlyout::Width(std::int32_t value)
    {
        EnsureUiThread(*m_native);
        m_width = std::clamp(value, 220, 1200);
        m_legacyWidthExplicit = true;
        UpdateFlyoutLayout(false);
        return {};
    }

    std::int32_t DesktopFlyout::Height(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_height;
    }

    winrt::author::setter DesktopFlyout::Height(std::int32_t value)
    {
        EnsureUiThread(*m_native);
        m_height = std::clamp(value, 120, 900);
        m_legacyHeightExplicit = true;
        UpdateFlyoutLayout(false);
        return {};
    }

    Microsoft::UI::Xaml::GridLength DesktopFlyout::FlyoutWidth(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_flyoutWidth;
    }

    winrt::author::setter DesktopFlyout::FlyoutWidth(Microsoft::UI::Xaml::GridLength value)
    {
        EnsureUiThread(*m_native);
        m_flyoutWidth = value;
        m_legacyWidthExplicit = false;
        if (value.GridUnitType == Microsoft::UI::Xaml::GridUnitType::Pixel &&
            std::isfinite(value.Value))
        {
            m_width = std::clamp(static_cast<std::int32_t>(std::lround(value.Value)), 220, 1200);
        }
        UpdateFlyoutLayout(false);
        return {};
    }

    Microsoft::UI::Xaml::GridLength DesktopFlyout::FlyoutHeight(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_flyoutHeight;
    }

    winrt::author::setter DesktopFlyout::FlyoutHeight(Microsoft::UI::Xaml::GridLength value)
    {
        EnsureUiThread(*m_native);
        m_flyoutHeight = value;
        m_legacyHeightExplicit = false;
        if (value.GridUnitType == Microsoft::UI::Xaml::GridUnitType::Pixel &&
            std::isfinite(value.Value))
        {
            m_height = std::clamp(static_cast<std::int32_t>(std::lround(value.Value)), 120, 900);
        }
        UpdateFlyoutLayout(false);
        return {};
    }

    Microsoft::UI::Xaml::Thickness DesktopFlyout::Margin(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_margin;
    }

    winrt::author::setter DesktopFlyout::Margin(Microsoft::UI::Xaml::Thickness value)
    {
        EnsureUiThread(*m_native);
        m_margin = {
            std::max(0.0, std::isfinite(value.Left) ? value.Left : 0.0),
            std::max(0.0, std::isfinite(value.Top) ? value.Top : 0.0),
            std::max(0.0, std::isfinite(value.Right) ? value.Right : 0.0),
            std::max(0.0, std::isfinite(value.Bottom) ? value.Bottom : 0.0) };
        m_native->visual.Margin(m_margin);
        UpdateFlyoutLayout(false);
        return {};
    }

    DesktopFlyoutPlacementMode DesktopFlyout::Placement(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_placement;
    }

    winrt::author::setter DesktopFlyout::Placement(DesktopFlyoutPlacementMode value)
    {
        EnsureUiThread(*m_native);
        m_placement = value;
        UpdateFlyoutLayout(false);
        return {};
    }

    DesktopFlyoutPopupDirection DesktopFlyout::PopupDirection(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_popupDirection;
    }

    winrt::author::setter DesktopFlyout::PopupDirection(DesktopFlyoutPopupDirection value)
    {
        EnsureUiThread(*m_native);
        m_popupDirection = value;
        UpdateFlyoutLayout(false);
        return {};
    }

    DesktopFlyoutActivationMode DesktopFlyout::ActivationMode(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_activationMode;
    }

    winrt::author::setter DesktopFlyout::ActivationMode(DesktopFlyoutActivationMode value)
    {
        EnsureUiThread(*m_native);
        m_activationMode = value;
        m_native->activationMode = value;
        m_native->host.ActivationMode(value);
        m_native->visual.FocusConfiguration(value == DesktopFlyoutActivationMode::never_activate);
        return {};
    }

    bool DesktopFlyout::HideOnLostFocus(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_hideOnLostFocus;
    }

    winrt::author::setter DesktopFlyout::HideOnLostFocus(bool value)
    {
        EnsureUiThread(*m_native);
        m_hideOnLostFocus = value;
        m_native->hideOnLostFocus = value;
        m_native->host.HideOnLostFocus(value);
        return {};
    }

    Microsoft::UI::Xaml::UIElement DesktopFlyout::Content(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_native->visual.RequestedContent();
    }

    winrt::author::setter DesktopFlyout::Content(Microsoft::UI::Xaml::UIElement const& value)
    {
        EnsureUiThread(*m_native);
        m_native->visual.RequestedContent(value);
        RefreshContent();
        return {};
    }

    Microsoft::UI::Xaml::Controls::MenuFlyout DesktopFlyout::MenuFlyout(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_menuFlyout;
    }

    winrt::author::setter DesktopFlyout::MenuFlyout(
        Microsoft::UI::Xaml::Controls::MenuFlyout const& value)
    {
        EnsureUiThread(*m_native);
        m_menuFlyout = value;
        return {};
    }

    Windows::Foundation::Collections::IObservableVector<Microsoft::UI::Xaml::UIElement>
        DesktopFlyout::Islands(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        if (!m_islands)
        {
            m_islands = winrt::single_threaded_observable_vector<Microsoft::UI::Xaml::UIElement>();
        }
        return m_islands;
    }

    DesktopFlyoutOrientation DesktopFlyout::IslandsOrientation(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_islandsOrientation;
    }

    winrt::author::setter DesktopFlyout::IslandsOrientation(DesktopFlyoutOrientation value)
    {
        EnsureUiThread(*m_native);
        m_islandsOrientation = value;
        RefreshContent();
        return {};
    }

    std::int32_t DesktopFlyout::IslandSpacing(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_islandSpacing;
    }

    winrt::author::setter DesktopFlyout::IslandSpacing(std::int32_t value)
    {
        EnsureUiThread(*m_native);
        m_islandSpacing = std::clamp(value, 0, 120);
        RefreshContent();
        return {};
    }

    bool DesktopFlyout::IsBackdropEnabled(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_isBackdropEnabled;
    }

    winrt::author::setter DesktopFlyout::IsBackdropEnabled(bool value)
    {
        EnsureUiThread(*m_native);
        m_isBackdropEnabled = value;
        m_native->visual.ApplySystemBackdrop(
            m_native->host.XamlSource(),
            m_isBackdropEnabled,
            m_backdropKind);
        return {};
    }

    DesktopFlyoutBackdropKind DesktopFlyout::BackdropKind(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_backdropKind;
    }

    winrt::author::setter DesktopFlyout::BackdropKind(DesktopFlyoutBackdropKind value)
    {
        EnsureUiThread(*m_native);
        m_backdropKind = value;
        m_native->visual.ApplySystemBackdrop(
            m_native->host.XamlSource(),
            m_isBackdropEnabled,
            m_backdropKind);
        return {};
    }

    bool DesktopFlyout::IsTransitionAnimationEnabled(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_isTransitionAnimationEnabled;
    }

    winrt::author::setter DesktopFlyout::IsTransitionAnimationEnabled(bool value)
    {
        EnsureUiThread(*m_native);
        m_isTransitionAnimationEnabled = value;
        return {};
    }

    double DesktopFlyout::PressedScale(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_pressedScale;
    }

    winrt::author::setter DesktopFlyout::PressedScale(double value)
    {
        EnsureUiThread(*m_native);
        m_pressedScale = std::isfinite(value) ? std::clamp(value, 0.1, 2.0) : 1.0;
        return {};
    }

    bool DesktopFlyout::IsSwipeToDismissEnabled(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_isSwipeToDismissEnabled;
    }

    winrt::author::setter DesktopFlyout::IsSwipeToDismissEnabled(bool value)
    {
        EnsureUiThread(*m_native);
        m_isSwipeToDismissEnabled = value;
        return {};
    }

    double DesktopFlyout::SwipeDismissThreshold(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_swipeDismissThreshold;
    }

    winrt::author::setter DesktopFlyout::SwipeDismissThreshold(double value)
    {
        EnsureUiThread(*m_native);
        m_swipeDismissThreshold = std::isfinite(value) ? std::clamp(value, 1.0, 2000.0) : 80.0;
        return {};
    }

    Windows::Foundation::TimeSpan DesktopFlyout::AutoCloseDelay(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_autoCloseDelay;
    }

    winrt::author::setter DesktopFlyout::AutoCloseDelay(Windows::Foundation::TimeSpan value)
    {
        EnsureUiThread(*m_native);
        m_autoCloseDelay = value;
        if (m_native->isOpen)
        {
            m_native->host.ConfigureAutoCloseTimer(m_autoCloseDelay);
        }
        return {};
    }

    void DesktopFlyout::RefreshContent()
    {
        EnsureUiThread(*m_native);
        // Keep the existing island surfaces and their backdrop elements alive
        // for the complete close storyboard. Content changes made after a
        // close has started are picked up by the next ShowCore/RefreshContent
        // pass, after CompleteClose has released the old surfaces.
        if (m_native->lifecycle.state() == desktop_flyouts::core::flyout_lifecycle_state::closing)
        {
            return;
        }

        if (m_native->refreshingContent)
        {
            return;
        }

        m_native->refreshingContent = true;
        m_native->visual.InteractionConfiguration(
            m_isSwipeToDismissEnabled,
            m_pressedScale,
            m_swipeDismissThreshold);
        m_native->visual.Margin(m_margin);
        m_native->visual.RefreshContent(
            m_native->host.XamlSource(),
            m_islands,
            m_islandsOrientation,
            m_islandSpacing);
        m_native->refreshingContent = false;
        UpdateFlyoutLayout(false);
    }

    void DesktopFlyout::BeginOpenAnimation()
    {
        if (m_native->visual.BeginOpenAnimation(
            m_isTransitionAnimationEnabled,
            m_native->activeWidth,
            m_native->activeHeight))
        {
            CompleteOpen();
        }
    }

    void DesktopFlyout::BeginCloseAnimation(bool fromCurrentTransform)
    {
        if (m_native->lifecycle.state() != desktop_flyouts::core::flyout_lifecycle_state::open)
        {
            return;
        }

        if (!m_native->lifecycle.begin_close())
        {
            return;
        }

        m_native->isOpen = false;
        m_native->host.IsOpen(false);
        m_native->host.StopAutoCloseTimer();
        m_native->visual.IsOpen(false);

        if (!fromCurrentTransform)
        {
            m_native->visual.SetRestingVisual();
        }

        if (m_native->visual.BeginCloseAnimation(
            m_isTransitionAnimationEnabled,
            m_native->activeWidth,
            m_native->activeHeight))
        {
            CompleteClose();
        }
    }

    void DesktopFlyout::TickAnimation()
    {
        if (m_native->visual.AnimationClosing())
        {
            CompleteClose();
        }
        else
        {
            CompleteOpen();
        }
    }

    void DesktopFlyout::CompleteOpen()
    {
        m_native->visual.SetRestingVisual();
        (void)m_native->lifecycle.complete_open();
        m_native->isOpen = true;
        m_native->host.IsOpen(true);
        m_native->visual.IsOpen(true);
        m_state = DesktopFlyoutState::open;
        m_native->host.ConfigureAutoCloseTimer(m_autoCloseDelay);
        if (m_activationMode == DesktopFlyoutActivationMode::activate)
        {
            (void)m_native->host.NavigateFocus();
        }
        else
        {
            m_native->host.RestoreActivationState();
        }
    }

    void DesktopFlyout::CompleteClose()
    {
        m_native->visual.SetClosedVisual(m_native->activeWidth, m_native->activeHeight);
        m_native->isOpen = false;
        m_native->host.IsOpen(false);
        m_native->visual.IsOpen(false);
        (void)m_native->lifecycle.complete_close();
        m_state = DesktopFlyoutState::closed;
        m_native->host.Hide();
        // The host is hidden only after the close transition has completed.
        // Keep each island's SystemBackdropElement connected through that
        // transition, then detach and release the backdrops as the final
        // visual-tree step.
        m_native->visual.ReleaseIslandBackdrops();
        m_native->activeBounds.reset();
        m_native->customPlacementPoint.reset();
        m_native->host.RestoreActivationState();
    }

    DesktopFlyoutState DesktopFlyout::State(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_state;
    }

    bool DesktopFlyout::IsOpen(winrt::author::getter)
    {
        EnsureUiThread(*m_native);
        return m_state == DesktopFlyoutState::open;
    }

    void DesktopFlyout::Show()
    {
        EnsureUiThread(*m_native);
        m_native->customPlacementPoint.reset();
        ShowCore();
    }

    void DesktopFlyout::ShowAt(std::int32_t x, std::int32_t y)
    {
        EnsureUiThread(*m_native);
        m_native->customPlacementPoint = POINT{ x, y };
        ShowCore();
    }

    void DesktopFlyout::UpdateFlyoutLayout(bool opening)
    {
        EnsureUiThread(*m_native);

        const auto lifecycleState = m_native->lifecycle.state();
        if (opening)
        {
            if (lifecycleState != desktop_flyouts::core::flyout_lifecycle_state::opening)
            {
                return;
            }
        }
        else if (m_state != DesktopFlyoutState::open ||
            lifecycleState != desktop_flyouts::core::flyout_lifecycle_state::open ||
            m_native->refreshingContent ||
            m_native->updatingLayout ||
            m_native->host.Window() == nullptr)
        {
            return;
        }

        m_native->updatingLayout = true;

        try
        {
            HWND ownerWindow = reinterpret_cast<HWND>(m_ownerWindowHandle);
            if (ownerWindow == nullptr || !IsWindow(ownerWindow))
            {
                ownerWindow = GetForegroundWindow();
            }
            winrt::check_bool(ownerWindow != nullptr);

            const auto workArea = GetFlyoutWorkArea(
                ownerWindow,
                opening ? m_native->customPlacementPoint : std::optional<POINT>{});
            const auto scale = m_native->host.RasterizationScale();
            const auto availableWidth = std::max(
                0.0,
                static_cast<double>(workArea.right - workArea.left) / scale -
                    m_margin.Left - m_margin.Right);
            const auto availableHeight = std::max(
                0.0,
                static_cast<double>(workArea.bottom - workArea.top) / scale -
                    m_margin.Top - m_margin.Bottom);

            m_native->visual.SetResolvedSize(
                std::numeric_limits<double>::quiet_NaN(),
                std::numeric_limits<double>::quiet_NaN());
            const auto desired = m_native->visual.Measure(availableWidth, availableHeight);
            const auto autoWidth = std::max(
                1.0,
                static_cast<double>(desired.Width) - m_margin.Left - m_margin.Right);
            const auto autoHeight = std::max(
                1.0,
                static_cast<double>(desired.Height) - m_margin.Top - m_margin.Bottom + 16.0);
            const auto contentWidth = m_legacyWidthExplicit
                ? std::max(1.0, static_cast<double>(m_width) / scale - m_margin.Left - m_margin.Right)
                : ResolveFlyoutLength(m_flyoutWidth, availableWidth, autoWidth);
            const auto contentHeight = m_legacyHeightExplicit
                ? std::max(1.0, static_cast<double>(m_height) / scale - m_margin.Top - m_margin.Bottom)
                : ResolveFlyoutLength(m_flyoutHeight, availableHeight, autoHeight);
            m_native->visual.SetResolvedSize(contentWidth, contentHeight);

            const auto frameWidth = m_legacyWidthExplicit
                ? m_width
                : static_cast<std::int32_t>(std::ceil(
                    (contentWidth + m_margin.Left + m_margin.Right) * scale));
            const auto frameHeight = m_legacyHeightExplicit
                ? m_height
                : static_cast<std::int32_t>(std::ceil(
                    (contentHeight + m_margin.Top + m_margin.Bottom) * scale));

            auto request = desktop_flyouts::core::layout_request{
                { workArea.left, workArea.top, workArea.right, workArea.bottom },
                { std::max(1, frameWidth), std::max(1, frameHeight) },
                0,
                ToCorePlacement(m_placement),
                ToCoreDirection(m_popupDirection) };
            if (opening && m_native->customPlacementPoint.has_value())
            {
                request.bottom_center_anchor = desktop_flyouts::core::point{
                    m_native->customPlacementPoint->x,
                    m_native->customPlacementPoint->y };
            }
            else if (!opening && m_native->activeBounds.has_value())
            {
                request.resize_anchor_region = m_native->activeBounds;
                request.resize_direction = m_native->activeDirection;
            }

            const auto layout = desktop_flyouts::core::resolve_layout(request);
            m_native->activeBounds = layout.bounds;
            m_native->activeDirection = layout.direction;
            m_native->visual.ActiveDirection(layout.direction);
            m_native->visual.InteractionConfiguration(
                m_isSwipeToDismissEnabled,
                m_pressedScale,
                m_swipeDismissThreshold);

            const auto x = layout.bounds.left;
            const auto y = layout.bounds.top;
            const auto width = layout.bounds.width();
            const auto height = layout.bounds.height();
            m_native->activeWidth = width;
            m_native->activeHeight = height;
            m_native->host.MoveAndResize(x, y, width, height);

            if (!opening)
            {
                m_native->visual.SetRestingVisual();
            }
            else
            {
                m_native->customPlacementPoint.reset();
            }
        }
        catch (...)
        {
            m_native->updatingLayout = false;
            throw;
        }

        m_native->updatingLayout = false;
    }

    void DesktopFlyout::ShowCore()
    {
        EnsureUiThread(*m_native);

        HWND ownerWindow = reinterpret_cast<HWND>(m_ownerWindowHandle);
        if (ownerWindow == nullptr || !IsWindow(ownerWindow))
        {
            ownerWindow = GetForegroundWindow();
        }
        winrt::check_bool(ownerWindow != nullptr);

        if (!m_native->lifecycle.begin_open())
        {
            return;
        }

        m_native->activationMode = m_activationMode;
        m_native->hideOnLostFocus = m_hideOnLostFocus;
        m_native->host.ActivationMode(m_activationMode);
        m_native->host.HideOnLostFocus(m_hideOnLostFocus);
        m_native->visual.FocusConfiguration(
            m_activationMode == DesktopFlyoutActivationMode::never_activate);

        if (m_activationMode != DesktopFlyoutActivationMode::activate)
        {
            m_native->host.PreserveActivationState();
        }

        m_native->host.OwnerWindow(ownerWindow);
        m_native->host.EnsureCreated(ownerWindow, m_activationMode);

        m_native->visual.ApplySystemBackdrop(
            m_native->host.XamlSource(),
            m_isBackdropEnabled,
            m_backdropKind);
        m_native->visual.Margin(m_margin);
        RefreshContent();
        UpdateFlyoutLayout(true);
        m_native->host.Show(m_activationMode);
        BeginOpenAnimation();
    }

    void DesktopFlyout::Hide()
    {
        EnsureUiThread(*m_native);
        BeginCloseAnimation();
    }

    void DesktopFlyout::NavigateFocus()
    {
        EnsureUiThread(*m_native);
        (void)m_native->host.NavigateFocus();
    }
}
