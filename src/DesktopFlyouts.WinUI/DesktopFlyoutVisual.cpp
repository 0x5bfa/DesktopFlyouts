#include <windows.h>
#undef GetCurrentTime

#include "DesktopFlyoutVisual.h"

#include <winrt/Microsoft.UI.Input.h>
#include <winrt/Microsoft.UI.Dispatching.h>
#include <winrt/Microsoft.UI.Xaml.Automation.h>
#include <winrt/Microsoft.UI.Xaml.Input.h>
#include <winrt/Microsoft.UI.Xaml.Media.h>
#include <winrt/Microsoft.UI.Xaml.Media.Animation.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Numerics.h>

#include <algorithm>
#include <chrono>
#include <cmath>
#include <memory>
#include <string>
#include <string_view>
#include <utility>

namespace winrt::DesktopFlyouts::detail
{
    namespace
    {
        constexpr double c_defaultFlyoutMargin = 12.0;
        constexpr double c_swipeDismissDragStartThreshold = 4.0;
        constexpr double c_swipeDismissAxisDominanceRatio = 1.2;

        template <typename T>
        T LookupResource(std::wstring_view key, T fallback)
        {
            try
            {
                auto application = Microsoft::UI::Xaml::Application::Current();
                if (!application)
                {
                    return fallback;
                }

                auto value = application.Resources().Lookup(winrt::box_value(std::wstring(key)));
                if (auto resource = value.try_as<T>())
                {
                    return resource;
                }
            }
            catch (...)
            {
            }

            return fallback;
        }

        double LookupDouble(std::wstring_view key, double fallback)
        {
            try
            {
                auto application = Microsoft::UI::Xaml::Application::Current();
                if (!application)
                {
                    return fallback;
                }

                return winrt::unbox_value<double>(
                    application.Resources().Lookup(winrt::box_value(std::wstring(key))));
            }
            catch (...)
            {
                return fallback;
            }
        }

        bool IsSystemLightTheme()
        {
            DWORD value{};
            DWORD valueSize = sizeof(value);
            const auto result = RegGetValueW(
                HKEY_CURRENT_USER,
                L"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
                L"SystemUsesLightTheme",
                RRF_RT_REG_DWORD,
                nullptr,
                &value,
                &valueSize);
            return result == ERROR_SUCCESS && value != 0;
        }

        void ApplyTheme(Microsoft::UI::Xaml::UIElement const& element)
        {
            if (auto frameworkElement = element.try_as<Microsoft::UI::Xaml::FrameworkElement>())
            {
                frameworkElement.RequestedTheme(IsSystemLightTheme()
                    ? Microsoft::UI::Xaml::ElementTheme::Light
                    : Microsoft::UI::Xaml::ElementTheme::Dark);
            }
        }

        Microsoft::UI::Xaml::Media::Brush ThemeBrush(
            std::wstring_view key,
            Windows::UI::Color fallbackColor = Windows::UI::Color{ 0, 0, 0, 0 })
        {
            return LookupResource<Microsoft::UI::Xaml::Media::Brush>(
                key,
                Microsoft::UI::Xaml::Media::SolidColorBrush(fallbackColor));
        }

        Microsoft::UI::Xaml::CornerRadius ThemeCornerRadius()
        {
            const auto radius = LookupDouble(L"OverlayCornerRadius", 12.0);
            return { radius, radius, radius, radius };
        }

        Microsoft::UI::Xaml::Controls::Grid CreateIslandSurface(
            Microsoft::UI::Xaml::UIElement const& content,
            std::uint32_t index,
            std::vector<Microsoft::UI::Xaml::Controls::SystemBackdropElement>& backdrops)
        {
            using namespace Microsoft::UI::Xaml;
            using namespace Microsoft::UI::Xaml::Controls;
            using namespace Microsoft::UI::Xaml::Media;

            auto surface = Grid{};
            surface.Name(L"DesktopFlyoutIslandSurface");
            const auto automationId = index == 0
                ? std::wstring(L"DesktopFlyoutIslandSurface")
                : std::wstring(L"DesktopFlyoutIslandSurface") + std::to_wstring(index);
            Automation::AutomationProperties::SetAutomationId(
                surface,
                automationId);
            Automation::AutomationProperties::SetName(surface, L"Desktop flyout island");
            surface.Background(nullptr);
            surface.BorderBrush(ThemeBrush(
                L"SurfaceStrokeColorDefaultBrush",
                IsSystemLightTheme()
                    ? Windows::UI::Color{ 0x26, 0x00, 0x00, 0x00 }
                    : Windows::UI::Color{ 0x33, 0xFF, 0xFF, 0xFF }));
            surface.BorderThickness({ 1, 1, 1, 1 });
            const auto cornerRadius = ThemeCornerRadius();
            surface.CornerRadius(cornerRadius);
            surface.Shadow(ThemeShadow{});
            surface.Clip(nullptr);
            ApplyTheme(surface);

            // Keep one backdrop target per island even when the material is
            // currently disabled. This mirrors the C# template, where the
            // SystemBackdropElement is always part of the island tree and
            // only its SystemBackdrop value changes.
            try
            {
                auto backdrop = SystemBackdropElement{};
                backdrop.CornerRadius({
                    std::max(0.0, cornerRadius.TopLeft - 1.0),
                    std::max(0.0, cornerRadius.TopRight - 1.0),
                    std::max(0.0, cornerRadius.BottomRight - 1.0),
                    std::max(0.0, cornerRadius.BottomLeft - 1.0) });
                backdrop.IsHitTestVisible(false);
                surface.Children().Append(backdrop);
                backdrops.push_back(backdrop);
            }
            catch (...)
            {
            }

            auto background = Border{};
            background.Background(ThemeBrush(
                L"FlyoutOverlayBackgroundBrush",
                Windows::UI::Color{ 0x09, 0xFF, 0xFF, 0xFF }));
            background.CornerRadius(cornerRadius);
            background.IsHitTestVisible(false);
            surface.Children().Append(background);

            if (content)
            {
                surface.Children().Append(content);
            }

            return surface;
        }

        std::pair<double, double> GetClosedOffset(
            desktop_flyouts::core::popup_direction direction,
            std::int32_t width,
            std::int32_t height) noexcept
        {
            switch (direction)
            {
            case desktop_flyouts::core::popup_direction::bottom_to_top:
                return { 0.0, static_cast<double>(height) + c_defaultFlyoutMargin };
            case desktop_flyouts::core::popup_direction::top_to_bottom:
                return { 0.0, -(static_cast<double>(height) + c_defaultFlyoutMargin) };
            case desktop_flyouts::core::popup_direction::left_to_right:
                return { -(static_cast<double>(width) + c_defaultFlyoutMargin), 0.0 };
            case desktop_flyouts::core::popup_direction::right_to_left:
                return { static_cast<double>(width) + c_defaultFlyoutMargin, 0.0 };
            case desktop_flyouts::core::popup_direction::vertical:
            case desktop_flyouts::core::popup_direction::horizontal:
            default:
                return { 0.0, 0.0 };
            }
        }

        bool IsVerticalDirection(desktop_flyouts::core::popup_direction direction) noexcept
        {
            return direction == desktop_flyouts::core::popup_direction::bottom_to_top ||
                direction == desktop_flyouts::core::popup_direction::top_to_bottom;
        }

        Microsoft::UI::Xaml::Media::Animation::KeySpline MakeKeySpline(
            Windows::Foundation::Point controlPoint1,
            Windows::Foundation::Point controlPoint2)
        {
            auto keySpline = Microsoft::UI::Xaml::Media::Animation::KeySpline{};
            keySpline.ControlPoint1(controlPoint1);
            keySpline.ControlPoint2(controlPoint2);
            return keySpline;
        }

        void AppendTransitionAnimation(
            Microsoft::UI::Xaml::Media::Animation::Storyboard& storyboard,
            Microsoft::UI::Xaml::Media::CompositeTransform const& transform,
            bool vertical,
            bool opening,
            double from,
            double to)
        {
            using namespace Microsoft::UI::Xaml::Media::Animation;

            auto keyFrames = DoubleAnimationUsingKeyFrames{};
            keyFrames.EnableDependentAnimation(true);

            auto initialFrame = DiscreteDoubleKeyFrame{};
            initialFrame.KeyTime(KeyTimeHelper::FromTimeSpan(std::chrono::milliseconds(0)));
            initialFrame.Value(from);
            keyFrames.KeyFrames().Append(initialFrame);

            auto finalFrame = SplineDoubleKeyFrame{};
            finalFrame.KeySpline(opening
                ? MakeKeySpline({ 0.1f, 0.9f }, { 0.4f, 1.0f })
                : MakeKeySpline({ 0.2f, 0.0f }, { 0.9f, 0.0f }));
            finalFrame.KeyTime(KeyTimeHelper::FromTimeSpan(
                opening
                    ? (vertical ? std::chrono::milliseconds(267) : std::chrono::milliseconds(167))
                    : (vertical ? std::chrono::milliseconds(200) : std::chrono::milliseconds(167))));
            finalFrame.Value(to);
            keyFrames.KeyFrames().Append(finalFrame);

            Storyboard::SetTarget(keyFrames, transform);
            Storyboard::SetTargetProperty(keyFrames, vertical ? L"TranslateY" : L"TranslateX");
            storyboard.Children().Append(keyFrames);
        }

        void AppendScaleAnimation(
            Microsoft::UI::Xaml::Media::Animation::Storyboard& storyboard,
            Microsoft::UI::Xaml::Media::CompositeTransform const& transform,
            wchar_t const* property,
            double from,
            double to,
            std::chrono::milliseconds duration)
        {
            using namespace Microsoft::UI::Xaml::Media::Animation;

            auto keyFrames = DoubleAnimationUsingKeyFrames{};
            keyFrames.EnableDependentAnimation(true);

            auto initialFrame = DiscreteDoubleKeyFrame{};
            initialFrame.KeyTime(KeyTimeHelper::FromTimeSpan(std::chrono::milliseconds(0)));
            initialFrame.Value(from);
            keyFrames.KeyFrames().Append(initialFrame);

            auto finalFrame = SplineDoubleKeyFrame{};
            finalFrame.KeySpline(MakeKeySpline({ 0.16f, 0.0f }, { 0.3f, 1.0f }));
            finalFrame.KeyTime(KeyTimeHelper::FromTimeSpan(duration));
            finalFrame.Value(to);
            keyFrames.KeyFrames().Append(finalFrame);

            Storyboard::SetTarget(keyFrames, transform);
            Storyboard::SetTargetProperty(keyFrames, property);
            storyboard.Children().Append(keyFrames);
        }

    }

    DesktopFlyoutVisual::~DesktopFlyoutVisual()
    {
        StopAnimation();
        StopScaleAnimation();
        StopSwipeRestoreAnimation();
        DetachInteractionHandlers();
        ReleaseIslandBackdrops();
        if (m_root)
        {
            try
            {
                m_root.Children().Clear();
            }
            catch (...)
            {
            }
        }
    }

    void DesktopFlyoutVisual::HideCallback(std::function<void()> callback)
    {
        m_hideCallback = std::move(callback);
    }

    void DesktopFlyoutVisual::SwipeDismissCallback(std::function<void()> callback)
    {
        m_swipeDismissCallback = std::move(callback);
    }

    void DesktopFlyoutVisual::SwipeDismissStartedCallback(std::function<void()> callback)
    {
        m_swipeDismissStartedCallback = std::move(callback);
    }

    void DesktopFlyoutVisual::SwipeDismissRestoredCallback(std::function<void()> callback)
    {
        m_swipeDismissRestoredCallback = std::move(callback);
    }

    void DesktopFlyoutVisual::AnimationCallback(std::function<void()> callback)
    {
        m_animationCallback = std::move(callback);
    }

    void DesktopFlyoutVisual::LayoutChangedCallback(std::function<void()> callback)
    {
        m_layoutChangedCallback = std::move(callback);
    }

    void DesktopFlyoutVisual::FocusConfiguration(bool neverActivate)
    {
        m_neverActivate = neverActivate;
    }

    void DesktopFlyoutVisual::RequestedContent(Microsoft::UI::Xaml::UIElement const& value)
    {
        m_requestedContent = value;
    }

    Microsoft::UI::Xaml::UIElement DesktopFlyoutVisual::RequestedContent() const noexcept
    {
        return m_requestedContent;
    }

    void DesktopFlyoutVisual::Margin(Microsoft::UI::Xaml::Thickness value)
    {
        EnsureRoot();
        m_root.Margin(value);
    }

    void DesktopFlyoutVisual::SetResolvedSize(double width, double height)
    {
        EnsureRoot();
        m_root.Width(width);
        m_root.Height(height);
    }

    Windows::Foundation::Size DesktopFlyoutVisual::Measure(
        double availableWidth,
        double availableHeight)
    {
        EnsureRoot();
        m_root.Measure(Windows::Foundation::Size{
            static_cast<float>(std::max(0.0, availableWidth)),
            static_cast<float>(std::max(0.0, availableHeight)) });
        return m_root.DesiredSize();
    }

    void DesktopFlyoutVisual::RefreshContent(
        Microsoft::UI::Xaml::Hosting::DesktopWindowXamlSource const& xamlSource,
        Windows::Foundation::Collections::IObservableVector<Microsoft::UI::Xaml::UIElement> const& islands,
        author::DesktopFlyoutOrientation orientation,
        std::int32_t spacing)
    {
        if (!xamlSource)
        {
            return;
        }

        if (m_root)
        {
            ReleaseIslandBackdrops();
            m_root.Children().Clear();
        }

        m_contentUsesIslands = false;

        if (m_requestedContent)
        {
            m_content = CreateIslandSurface(
                m_requestedContent,
                0,
                m_islandBackdrops);
            m_contentUsesIslands = true;
        }
        else if (islands && islands.Size() > 0)
        {
            auto stack = Microsoft::UI::Xaml::Controls::StackPanel{};
            stack.Orientation(orientation == author::DesktopFlyoutOrientation::horizontal
                ? Microsoft::UI::Xaml::Controls::Orientation::Horizontal
                : Microsoft::UI::Xaml::Controls::Orientation::Vertical);
            stack.Spacing(static_cast<double>(spacing));

            for (std::uint32_t index = 0; index < islands.Size(); ++index)
            {
                auto island = islands.GetAt(index);
                if (island)
                {
                    stack.Children().Append(CreateIslandSurface(
                        island,
                        index,
                        m_islandBackdrops));
                }
            }

            ApplyTheme(stack);
            m_content = stack;
            m_contentUsesIslands = true;
        }
        else if (m_defaultContent)
        {
            m_content = CreateIslandSurface(
                m_defaultContent,
                0,
                m_islandBackdrops);
            m_contentUsesIslands = true;
        }
        else
        {
            auto stack = Microsoft::UI::Xaml::Controls::StackPanel{};
            stack.Spacing(8.0);

            auto title = Microsoft::UI::Xaml::Controls::TextBlock{};
            title.Name(L"FlyoutTitle");
            Microsoft::UI::Xaml::Automation::AutomationProperties::SetAutomationId(title, L"FlyoutTitle");
            Microsoft::UI::Xaml::Automation::AutomationProperties::SetName(title, L"C++/WinRT DesktopFlyout");
            title.Text(L"C++/WinRT DesktopFlyout");
            title.FontSize(20.0);
            ApplyTheme(title);

            auto description = Microsoft::UI::Xaml::Controls::TextBlock{};
            description.Name(L"FlyoutDescription");
            Microsoft::UI::Xaml::Automation::AutomationProperties::SetAutomationId(
                description,
                L"FlyoutDescription");
            Microsoft::UI::Xaml::Automation::AutomationProperties::SetName(
                description,
                L"Rendered by a WinUI 3 XAML island hosted in a native popup window.");
            description.Text(L"Rendered by a WinUI 3 XAML island hosted in a native popup window.");
            description.TextWrapping(Microsoft::UI::Xaml::TextWrapping::Wrap);
            description.Foreground(ThemeBrush(L"TextFillColorSecondaryBrush"));

            auto close = Microsoft::UI::Xaml::Controls::Button{};
            close.Name(L"CloseFlyoutButton");
            Microsoft::UI::Xaml::Automation::AutomationProperties::SetAutomationId(
                close,
                L"CloseFlyoutButton");
            Microsoft::UI::Xaml::Automation::AutomationProperties::SetName(close, L"Close flyout");
            close.Content(winrt::box_value(L"Close"));
            close.Click([this](auto const&, auto const&)
            {
                if (m_hideCallback)
                {
                    m_hideCallback();
                }
            });

            stack.Children().Append(title);
            stack.Children().Append(description);
            stack.Children().Append(close);

            auto border = Microsoft::UI::Xaml::Controls::Border{};
            border.Name(L"DesktopFlyoutSurface");
            Microsoft::UI::Xaml::Automation::AutomationProperties::SetAutomationId(
                border,
                L"DesktopFlyoutSurface");
            Microsoft::UI::Xaml::Automation::AutomationProperties::SetName(border, L"Desktop flyout");
            border.Background(nullptr);
            border.BorderBrush(nullptr);
            border.BorderThickness(Microsoft::UI::Xaml::Thickness{ 0, 0, 0, 0 });
            border.CornerRadius({ 0, 0, 0, 0 });
            border.Padding(Microsoft::UI::Xaml::Thickness{ 20, 16, 20, 16 });
            border.Child(stack);
            ApplyTheme(border);

            m_defaultContent = border;
            m_content = CreateIslandSurface(
                m_defaultContent,
                0,
                m_islandBackdrops);
            m_contentUsesIslands = true;
        }

        EnsureRoot();
        if (m_content)
        {
            m_root.Children().Append(m_content);
        }
        xamlSource.Content(m_root);
        // The target must be connected to the XAML island before creating the
        // WinUI system-backdrop controller. This is also what makes the first
        // show use the same material as subsequent shows.
        UpdateIslandBackdrops();
    }

    void DesktopFlyoutVisual::ApplySystemBackdrop(
        Microsoft::UI::Xaml::Hosting::DesktopWindowXamlSource const& xamlSource,
        bool enabled,
        author::DesktopFlyoutBackdropKind kind)
    {
        (void)xamlSource;
        m_backdropEnabled = enabled;
        m_backdropKind = kind;
        UpdateIslandBackdrops();
    }

    void DesktopFlyoutVisual::UpdateIslandBackdrops() noexcept
    {
        for (auto const& backdrop : m_islandBackdrops)
        {
            try
            {
                if (!m_backdropEnabled)
                {
                    backdrop.SystemBackdrop(nullptr);
                }
                else if (m_backdropKind == author::DesktopFlyoutBackdropKind::mica)
                {
                    backdrop.SystemBackdrop(Microsoft::UI::Xaml::Media::SystemBackdrop{
                        Microsoft::UI::Xaml::Media::MicaBackdrop{} });
                }
                else
                {
                    backdrop.SystemBackdrop(Microsoft::UI::Xaml::Media::SystemBackdrop{
                        Microsoft::UI::Xaml::Media::DesktopAcrylicBackdrop{} });
                }
            }
            catch (...)
            {
                try
                {
                    backdrop.SystemBackdrop(nullptr);
                }
                catch (...)
                {
                }
            }
        }
    }

    void DesktopFlyoutVisual::ReleaseIslandBackdrops() noexcept
    {
        auto pendingBackdrops = std::make_shared<
            std::vector<Microsoft::UI::Xaml::Controls::SystemBackdropElement>>(
                std::move(m_islandBackdrops));
        m_islandBackdrops.clear();
        m_content = nullptr;

        if (m_root)
        {
            try
            {
                m_root.Children().Clear();
            }
            catch (...)
            {
            }
        }

        try
        {
            if (auto dispatcher = m_root ? m_root.DispatcherQueue() : nullptr)
            {
                if (dispatcher.TryEnqueue([pendingBackdrops]
                {
                    for (auto const& backdrop : *pendingBackdrops)
                    {
                        try
                        {
                            backdrop.SystemBackdrop(nullptr);
                        }
                        catch (...)
                        {
                        }
                    }
                }))
                {
                    return;
                }
            }
        }
        catch (...)
        {
        }

        // During dispatcher shutdown the detached element is no longer part of
        // the XAML tree. Let the held WinRT references release naturally
        // instead of mutating SystemBackdrop on an unloading XAML object.
    }

    void DesktopFlyoutVisual::InteractionConfiguration(
        bool enabled,
        double pressedScale,
        double threshold)
    {
        m_interactionEnabled = enabled;
        m_pressedScale = pressedScale;
        m_swipeDismissThreshold = threshold;
    }

    void DesktopFlyoutVisual::ActiveDirection(desktop_flyouts::core::popup_direction direction) noexcept
    {
        m_activeDirection = direction;
    }

    void DesktopFlyoutVisual::IsOpen(bool value) noexcept
    {
        m_isOpen = value;
    }

    void DesktopFlyoutVisual::EnsureRoot()
    {
        if (m_root)
        {
            return;
        }

        m_root = Microsoft::UI::Xaml::Controls::Grid{};
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetAutomationId(
            m_root,
            L"DesktopFlyoutRoot");
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetName(
            m_root,
            L"Desktop flyout root");
        m_root.Background(nullptr);

        m_transform = Microsoft::UI::Xaml::Media::CompositeTransform{};
        m_root.RenderTransform(m_transform);
        m_root.RenderTransformOrigin(Windows::Foundation::Point{ 0.5, 0.5 });
        m_sizeChangedToken = m_root.SizeChanged([this](auto const&, auto const&)
        {
            if (m_layoutChangedCallback)
            {
                m_layoutChangedCallback();
            }
        });
        AttachInteractionHandlers();
    }

    void DesktopFlyoutVisual::AttachInteractionHandlers()
    {
        if (!m_root || m_pointerPressedToken.value != 0)
        {
            return;
        }

        m_gettingFocusToken = m_root.GettingFocus([this](auto const& sender, auto const& args)
        {
            OnRootGettingFocus(sender, args);
        });

        m_pointerPressedToken = m_root.PointerPressed([this](auto const&, auto const& args)
        {
            if (!m_isOpen || m_animation || m_swipeRestoreAnimation)
            {
                return;
            }

            const auto isPressedScaleEnabled = std::abs(m_pressedScale - 1.0) > 0.001;
            const auto isSwipeDismissEnabled = m_interactionEnabled && GetSwipeMaxDistance(m_activeWidth, m_activeHeight) > 0.0;
            if (!isPressedScaleEnabled && !isSwipeDismissEnabled)
            {
                return;
            }

            auto currentPoint = args.GetCurrentPoint(m_root);
            m_pointerId = currentPoint.PointerId();
            m_pointerStart = currentPoint.Position();
            m_swipeTracking = isSwipeDismissEnabled;
            m_swipeDragging = false;
            (void)m_root.CapturePointer(args.Pointer());
            m_pointerCaptured = true;

            if (isPressedScaleEnabled && m_transform)
            {
                AnimateScale(std::clamp(m_pressedScale, 0.1, 2.0), std::chrono::milliseconds(110));
            }
        });

        m_pointerMovedToken = m_root.PointerMoved([this](auto const&, auto const& args)
        {
            if (!m_pointerCaptured || !m_transform)
            {
                return;
            }

            auto currentPoint = args.GetCurrentPoint(m_root);
            if (currentPoint.PointerId() != m_pointerId)
            {
                return;
            }

            if (!m_swipeTracking)
            {
                return;
            }

            const auto position = currentPoint.Position();
            const auto deltaX = static_cast<double>(position.X - m_pointerStart.X);
            const auto deltaY = static_cast<double>(position.Y - m_pointerStart.Y);
            double translateX{};
            double translateY{};
            if (!TryGetSwipeTranslation(deltaX, deltaY, translateX, translateY))
            {
                return;
            }

            if (!m_swipeDragging)
            {
                m_swipeDragging = true;
                StopScaleAnimation();
                ResetPressedScale();
                if (m_swipeDismissStartedCallback)
                {
                    m_swipeDismissStartedCallback();
                }
            }

            m_transform.TranslateX(translateX);
            m_transform.TranslateY(translateY);
            args.Handled(true);
        });

        m_pointerReleasedToken = m_root.PointerReleased([this](auto const&, auto const& args)
        {
            if (!m_pointerCaptured || !m_transform)
            {
                return;
            }

            auto currentPoint = args.GetCurrentPoint(m_root);
            if (currentPoint.PointerId() != m_pointerId)
            {
                return;
            }

            const auto shouldDismiss = m_swipeTracking &&
                m_swipeDragging &&
                GetSwipeDistance() >= std::clamp(
                    std::isfinite(m_swipeDismissThreshold) ? m_swipeDismissThreshold : 80.0,
                    1.0,
                    std::max(1.0, GetSwipeMaxDistance(m_activeWidth, m_activeHeight)));
            const auto shouldRestore = m_swipeTracking && m_swipeDragging && !shouldDismiss;

            m_root.ReleasePointerCapture(args.Pointer());
            m_pointerCaptured = false;
            m_swipeTracking = false;
            m_swipeDragging = false;
            args.Handled(true);

            if (shouldDismiss)
            {
                ResetPressedScale();
                if (m_swipeDismissCallback)
                {
                    m_swipeDismissCallback();
                }
                return;
            }

            if (shouldRestore)
            {
                AnimateSwipeRestore();
            }
            else
            {
                RestorePressedScale();
            }
        });

        m_pointerCanceledToken = m_root.PointerCanceled([this](auto const&, auto const&)
        {
            m_pointerCaptured = false;
            const auto shouldRestore = m_swipeTracking && m_swipeDragging;
            m_swipeTracking = false;
            m_swipeDragging = false;
            if (shouldRestore)
            {
                AnimateSwipeRestore();
            }
            else
            {
                RestorePressedScale();
            }
        });

        m_pointerCaptureLostToken = m_root.PointerCaptureLost([this](auto const&, auto const&)
        {
            m_pointerCaptured = false;
            const auto shouldRestore = m_swipeTracking && m_swipeDragging;
            m_swipeTracking = false;
            m_swipeDragging = false;
            if (shouldRestore)
            {
                AnimateSwipeRestore();
            }
            else
            {
                RestorePressedScale();
            }
        });
    }

    void DesktopFlyoutVisual::DetachInteractionHandlers() noexcept
    {
        if (!m_root)
        {
            return;
        }

        const auto remove = [](auto const& removeHandler, winrt::event_token& token) noexcept
        {
            if (token.value == 0)
            {
                return;
            }

            try
            {
                removeHandler(token);
            }
            catch (...)
            {
            }
            token = {};
        };

        remove([this](auto const& token) { m_root.SizeChanged(token); }, m_sizeChangedToken);
        remove([this](auto const& token) { m_root.GettingFocus(token); }, m_gettingFocusToken);
        remove([this](auto const& token) { m_root.PointerPressed(token); }, m_pointerPressedToken);
        remove([this](auto const& token) { m_root.PointerMoved(token); }, m_pointerMovedToken);
        remove([this](auto const& token) { m_root.PointerReleased(token); }, m_pointerReleasedToken);
        remove([this](auto const& token) { m_root.PointerCanceled(token); }, m_pointerCanceledToken);
        remove([this](auto const& token) { m_root.PointerCaptureLost(token); }, m_pointerCaptureLostToken);
    }

    void DesktopFlyoutVisual::ResetPointerVisual()
    {
        if (!m_transform)
        {
            return;
        }

        m_transform.ScaleX(1.0);
        m_transform.ScaleY(1.0);
        m_transform.TranslateX(0.0);
        m_transform.TranslateY(0.0);
    }

    void DesktopFlyoutVisual::RestorePressedScale()
    {
        if (std::abs(m_transform ? m_transform.ScaleX() - 1.0 : 0.0) < 0.001 &&
            std::abs(m_transform ? m_transform.ScaleY() - 1.0 : 0.0) < 0.001)
        {
            return;
        }

        AnimateScale(1.0, std::chrono::milliseconds(240));
    }

    void DesktopFlyoutVisual::ResetPressedScale()
    {
        StopScaleAnimation();
        if (!m_transform)
        {
            return;
        }

        m_transform.ScaleX(1.0);
        m_transform.ScaleY(1.0);
    }

    void DesktopFlyoutVisual::AnimateScale(double scale, std::chrono::milliseconds duration)
    {
        if (!m_transform)
        {
            return;
        }

        StopScaleAnimation();
        m_scaleTarget = scale;

        auto storyboard = Microsoft::UI::Xaml::Media::Animation::Storyboard{};
        AppendScaleAnimation(
            storyboard,
            m_transform,
            L"ScaleX",
            m_transform.ScaleX(),
            scale,
            duration);
        AppendScaleAnimation(
            storyboard,
            m_transform,
            L"ScaleY",
            m_transform.ScaleY(),
            scale,
            duration);

        m_scaleAnimation = storyboard;
        m_scaleCompletedToken = m_scaleAnimation.Completed([this](auto const&, auto const&)
        {
            CompleteScaleAnimation();
        });
        m_scaleAnimation.Begin();
    }

    void DesktopFlyoutVisual::StopScaleAnimation() noexcept
    {
        if (!m_scaleAnimation)
        {
            return;
        }

        if (m_scaleCompletedToken.value != 0)
        {
            try
            {
                m_scaleAnimation.Completed(m_scaleCompletedToken);
            }
            catch (...)
            {
            }
            m_scaleCompletedToken = {};
        }

        try
        {
            m_scaleAnimation.Stop();
        }
        catch (...)
        {
        }

        m_scaleAnimation = nullptr;
    }

    void DesktopFlyoutVisual::CompleteScaleAnimation()
    {
        if (m_scaleCompletedToken.value != 0)
        {
            m_scaleAnimation.Completed(m_scaleCompletedToken);
            m_scaleCompletedToken = {};
        }

        if (m_transform)
        {
            m_transform.ScaleX(m_scaleTarget);
            m_transform.ScaleY(m_scaleTarget);
        }

        m_scaleAnimation = nullptr;
    }

    void DesktopFlyoutVisual::StopSwipeRestoreAnimation() noexcept
    {
        if (!m_swipeRestoreAnimation)
        {
            return;
        }

        if (m_swipeRestoreCompletedToken.value != 0)
        {
            try
            {
                m_swipeRestoreAnimation.Completed(m_swipeRestoreCompletedToken);
            }
            catch (...)
            {
            }
            m_swipeRestoreCompletedToken = {};
        }

        try
        {
            m_swipeRestoreAnimation.Stop();
        }
        catch (...)
        {
        }

        m_swipeRestoreAnimation = nullptr;
    }

    void DesktopFlyoutVisual::AnimateSwipeRestore()
    {
        if (!m_transform)
        {
            return;
        }

        StopSwipeRestoreAnimation();
        const auto vertical = IsVerticalDirection(m_activeDirection);
        auto storyboard = Microsoft::UI::Xaml::Media::Animation::Storyboard{};
        AppendTransitionAnimation(
            storyboard,
            m_transform,
            vertical,
            true,
            vertical ? m_transform.TranslateY() : m_transform.TranslateX(),
            0.0);
        m_swipeRestoreAnimation = storyboard;
        m_swipeRestoreCompletedToken = m_swipeRestoreAnimation.Completed([this](auto const&, auto const&)
        {
            CompleteSwipeRestore();
        });
        m_swipeRestoreAnimation.Begin();
    }

    void DesktopFlyoutVisual::CompleteSwipeRestore()
    {
        if (m_swipeRestoreCompletedToken.value != 0)
        {
            m_swipeRestoreAnimation.Completed(m_swipeRestoreCompletedToken);
            m_swipeRestoreCompletedToken = {};
        }

        m_swipeRestoreAnimation = nullptr;
        SetRestingVisual();
        if (m_swipeDismissRestoredCallback)
        {
            m_swipeDismissRestoredCallback();
        }
    }

    bool DesktopFlyoutVisual::TryGetSwipeTranslation(
        double deltaX,
        double deltaY,
        double& translateX,
        double& translateY)
    {
        translateX = 0.0;
        translateY = 0.0;

        const auto [closedX, closedY] = GetClosedOffset(m_activeDirection, m_activeWidth, m_activeHeight);
        if (closedX != 0.0)
        {
            const auto primaryDistance = std::max(0.0, std::copysign(deltaX, closedX));
            if (!CanStartSwipeDrag(primaryDistance, std::abs(deltaY)))
            {
                return false;
            }

            translateX = std::copysign(std::min(primaryDistance, std::abs(closedX)), closedX);
            return true;
        }

        if (closedY == 0.0)
        {
            return false;
        }

        const auto primaryDistance = std::max(0.0, std::copysign(deltaY, closedY));
        if (!CanStartSwipeDrag(primaryDistance, std::abs(deltaX)))
        {
            return false;
        }

        translateY = std::copysign(std::min(primaryDistance, std::abs(closedY)), closedY);
        return true;
    }

    bool DesktopFlyoutVisual::CanStartSwipeDrag(double primaryDistance, double secondaryDistance) const noexcept
    {
        if (m_swipeDragging)
        {
            return true;
        }

        if (primaryDistance < c_swipeDismissDragStartThreshold)
        {
            return false;
        }

        return primaryDistance >= secondaryDistance * c_swipeDismissAxisDominanceRatio;
    }

    double DesktopFlyoutVisual::GetSwipeDistance() const noexcept
    {
        if (!m_transform)
        {
            return 0.0;
        }

        const auto [closedX, closedY] = GetClosedOffset(m_activeDirection, m_activeWidth, m_activeHeight);
        if (closedX != 0.0)
        {
            return std::max(0.0, std::copysign(m_transform.TranslateX(), closedX));
        }

        return closedY == 0.0
            ? 0.0
            : std::max(0.0, std::copysign(m_transform.TranslateY(), closedY));
    }

    double DesktopFlyoutVisual::GetSwipeMaxDistance(std::int32_t width, std::int32_t height) const noexcept
    {
        const auto [closedX, closedY] = GetClosedOffset(m_activeDirection, width, height);
        return closedX != 0.0 ? std::abs(closedX) : std::abs(closedY);
    }

    bool DesktopFlyoutVisual::IsFlyoutElement(Microsoft::UI::Xaml::DependencyObject const& element) const
    {
        if (!m_root)
        {
            return false;
        }

        if (element == m_root)
        {
            return true;
        }

        auto current = element;
        while (current)
        {
            if (current == m_root)
            {
                return true;
            }

            current = Microsoft::UI::Xaml::Media::VisualTreeHelper::GetParent(current);
        }

        return false;
    }

    void DesktopFlyoutVisual::OnRootGettingFocus(
        Microsoft::UI::Xaml::UIElement const&,
        Microsoft::UI::Xaml::Input::GettingFocusEventArgs const& args)
    {
        if (!m_neverActivate || !args.NewFocusedElement())
        {
            return;
        }

        if (IsFlyoutElement(args.NewFocusedElement()))
        {
            args.Cancel(true);
            args.Handled(true);
        }
    }

    void DesktopFlyoutVisual::SetRestingVisual()
    {
        if (!m_root || !m_transform)
        {
            return;
        }

        m_transform.ScaleX(1.0);
        m_transform.ScaleY(1.0);
        m_transform.TranslateX(0.0);
        m_transform.TranslateY(0.0);
    }

    void DesktopFlyoutVisual::SetClosedVisual(std::int32_t width, std::int32_t height)
    {
        m_activeWidth = width;
        m_activeHeight = height;
        if (!m_root || !m_transform)
        {
            return;
        }

        const auto [closedX, closedY] = GetClosedOffset(m_activeDirection, width, height);
        m_transform.ScaleX(1.0);
        m_transform.ScaleY(1.0);
        m_transform.TranslateX(closedX);
        m_transform.TranslateY(closedY);
    }

    bool DesktopFlyoutVisual::BeginOpenAnimation(bool enabled, std::int32_t width, std::int32_t height)
    {
        m_activeWidth = width;
        m_activeHeight = height;
        StopSwipeRestoreAnimation();
        StopScaleAnimation();
        if (!m_root || !m_transform || !enabled)
        {
            SetRestingVisual();
            return true;
        }

        StopAnimation();
        m_animationClosing = false;
        BeginAnimation(false, width, height);
        return false;
    }

    bool DesktopFlyoutVisual::BeginCloseAnimation(bool enabled, std::int32_t width, std::int32_t height)
    {
        m_activeWidth = width;
        m_activeHeight = height;
        StopSwipeRestoreAnimation();
        StopScaleAnimation();
        if (!m_root || !m_transform || !enabled)
        {
            SetClosedVisual(width, height);
            return true;
        }

        StopAnimation();
        m_animationClosing = true;
        BeginAnimation(true, width, height);
        return false;
    }

    void DesktopFlyoutVisual::StopAnimation() noexcept
    {
        if (!m_animation)
        {
            return;
        }

        if (m_animationCompletedToken.value != 0)
        {
            try
            {
                m_animation.Completed(m_animationCompletedToken);
            }
            catch (...)
            {
            }
            m_animationCompletedToken = {};
        }

        try
        {
            m_animation.Stop();
        }
        catch (...)
        {
        }
        m_animation = nullptr;
    }

    void DesktopFlyoutVisual::BeginAnimation(
        bool closing,
        std::int32_t width,
        std::int32_t height)
    {
        const auto [closedX, closedY] = GetClosedOffset(m_activeDirection, width, height);
        const auto vertical = IsVerticalDirection(m_activeDirection);
        const auto from = vertical
            ? (closing ? m_transform.TranslateY() : closedY)
            : (closing ? m_transform.TranslateX() : closedX);
        const auto to = closing
            ? (vertical ? closedY : closedX)
            : 0.0;

        if (!closing)
        {
            m_transform.TranslateX(closedX);
            m_transform.TranslateY(closedY);
        }

        auto storyboard = Microsoft::UI::Xaml::Media::Animation::Storyboard{};
        AppendTransitionAnimation(storyboard, m_transform, vertical, !closing, from, to);
        m_animation = storyboard;
        m_animationCompletedToken = m_animation.Completed([this](auto const&, auto const&)
        {
            AnimationCompleted();
        });
        m_animation.Begin();
    }

    void DesktopFlyoutVisual::AnimationCompleted()
    {
        if (m_animationCompletedToken.value != 0)
        {
            m_animation.Completed(m_animationCompletedToken);
            m_animationCompletedToken = {};
        }
        m_animation = nullptr;
        if (m_animationCallback)
        {
            m_animationCallback();
        }
    }

    bool DesktopFlyoutVisual::AnimationClosing() const noexcept
    {
        return m_animationClosing;
    }
}
