#include <windows.h>
#undef GetCurrentTime

#include "author/DesktopFlyout.author.h"
#include "author/DesktopFlyout.author.impl.h"
#include "DesktopFlyoutHost.h"
#include "DesktopFlyoutVisual.h"
#include "FlyoutLayout.h"
#include "winrt/DesktopFlyouts.h"

#include <winrt/Microsoft.UI.Xaml.Hosting.h>
#include <winrt/Windows.UI.Xaml.Interop.h>

#include <algorithm>
#include <cmath>
#include <limits>
#include <optional>
#include <utility>
#include <vector>

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
        DesktopFlyoutActivationMode activationMode{ DesktopFlyoutActivationMode::Activate };
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
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::TopCenter:
            return desktop_flyouts::core::placement_mode::top_center;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::TopLeft:
            return desktop_flyouts::core::placement_mode::top_left;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::TopRight:
            return desktop_flyouts::core::placement_mode::top_right;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::BottomCenter:
            return desktop_flyouts::core::placement_mode::bottom_center;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::BottomLeft:
            return desktop_flyouts::core::placement_mode::bottom_left;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::BottomRight:
            return desktop_flyouts::core::placement_mode::bottom_right;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::LeftCenter:
            return desktop_flyouts::core::placement_mode::left_center;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode::RightCenter:
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
        case winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection::BottomToTop:
            return desktop_flyouts::core::popup_direction::bottom_to_top;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection::TopToBottom:
            return desktop_flyouts::core::popup_direction::top_to_bottom;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection::Vertical:
            return desktop_flyouts::core::popup_direction::vertical;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection::LeftToRight:
            return desktop_flyouts::core::popup_direction::left_to_right;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection::RightToLeft:
            return desktop_flyouts::core::popup_direction::right_to_left;
        case winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection::Horizontal:
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
    DesktopFlyoutIslandTemplateSettings::DesktopFlyoutIslandTemplateSettings() = default;

    Microsoft::UI::Xaml::DependencyProperty
        DesktopFlyoutIslandTemplateSettings::BackdropCornerRadiusProperty(winrt::author::getter)
    {
        static auto property = Microsoft::UI::Xaml::DependencyProperty::Register(
            L"BackdropCornerRadius",
            winrt::xaml_typename<Microsoft::UI::Xaml::CornerRadius>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings>(),
            Microsoft::UI::Xaml::PropertyMetadata{
                winrt::box_value(Microsoft::UI::Xaml::CornerRadius{}) });
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty
        DesktopFlyoutIslandTemplateSettings::SystemBackdropProperty(winrt::author::getter)
    {
        static auto property = Microsoft::UI::Xaml::DependencyProperty::Register(
            L"SystemBackdrop",
            winrt::xaml_typename<Microsoft::UI::Xaml::Media::SystemBackdrop>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings>(),
            Microsoft::UI::Xaml::PropertyMetadata{ nullptr });
        return property;
    }

    Microsoft::UI::Xaml::CornerRadius
        DesktopFlyoutIslandTemplateSettings::BackdropCornerRadius(winrt::author::getter)
    {
        return winrt::unbox_value<Microsoft::UI::Xaml::CornerRadius>(
            self(this)->GetValue(BackdropCornerRadiusProperty()));
    }

    winrt::author::setter DesktopFlyoutIslandTemplateSettings::BackdropCornerRadius(
        Microsoft::UI::Xaml::CornerRadius value)
    {
        self(this)->SetValue(BackdropCornerRadiusProperty(), winrt::box_value(value));
        return {};
    }

    Microsoft::UI::Xaml::Media::SystemBackdrop
        DesktopFlyoutIslandTemplateSettings::SystemBackdrop(winrt::author::getter)
    {
        return self(this)->GetValue(SystemBackdropProperty())
            .try_as<Microsoft::UI::Xaml::Media::SystemBackdrop>();
    }

    winrt::author::setter DesktopFlyoutIslandTemplateSettings::SystemBackdrop(
        Microsoft::UI::Xaml::Media::SystemBackdrop const& value)
    {
        self(this)->SetValue(SystemBackdropProperty(), value);
        return {};
    }

    DesktopFlyoutIsland::DesktopFlyoutIsland()
        : m_templateSettings(
            winrt::make<winrt::DesktopFlyouts::implementation::DesktopFlyoutIslandTemplateSettings>())
    {
        self(this)->DefaultStyleKey(winrt::box_value(L"DesktopFlyouts.DesktopFlyoutIsland"));
        m_cornerRadiusToken = self(this)->RegisterPropertyChangedCallback(
            Microsoft::UI::Xaml::Controls::Control::CornerRadiusProperty(),
            [this](auto const&, auto const&)
            {
                const auto radius = self(this)->CornerRadius();
                const auto inner = [](double value) noexcept
                {
                    return std::max(0.0, value - 1.0);
                };
                m_templateSettings.BackdropCornerRadius({
                    inner(radius.TopLeft),
                    inner(radius.TopRight),
                    inner(radius.BottomRight),
                    inner(radius.BottomLeft) });
            });

        const auto radius = self(this)->CornerRadius();
        const auto inner = [](double value) noexcept
        {
            return std::max(0.0, value - 1.0);
        };
        m_templateSettings.BackdropCornerRadius({
            inner(radius.TopLeft),
            inner(radius.TopRight),
            inner(radius.BottomRight),
            inner(radius.BottomLeft) });
    }

    DesktopFlyoutIsland::~DesktopFlyoutIsland()
    {
        if (m_cornerRadiusToken != 0)
        {
            try
            {
                self(this)->UnregisterPropertyChangedCallback(
                    Microsoft::UI::Xaml::Controls::Control::CornerRadiusProperty(),
                    m_cornerRadiusToken);
            }
            catch (...)
            {
            }
        }
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyoutIsland::IslandWidthProperty(winrt::author::getter)
    {
        static auto property = Microsoft::UI::Xaml::DependencyProperty::Register(
            L"IslandWidth",
            winrt::xaml_typename<Microsoft::UI::Xaml::GridLength>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIsland>(),
            Microsoft::UI::Xaml::PropertyMetadata{
                winrt::box_value(Microsoft::UI::Xaml::GridLength{
                    1.0, Microsoft::UI::Xaml::GridUnitType::Auto }) });
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyoutIsland::IslandHeightProperty(winrt::author::getter)
    {
        static auto property = Microsoft::UI::Xaml::DependencyProperty::Register(
            L"IslandHeight",
            winrt::xaml_typename<Microsoft::UI::Xaml::GridLength>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIsland>(),
            Microsoft::UI::Xaml::PropertyMetadata{
                winrt::box_value(Microsoft::UI::Xaml::GridLength{
                    1.0, Microsoft::UI::Xaml::GridUnitType::Auto }) });
        return property;
    }

    Microsoft::UI::Xaml::GridLength DesktopFlyoutIsland::IslandWidth(winrt::author::getter)
    {
        return winrt::unbox_value<Microsoft::UI::Xaml::GridLength>(
            self(this)->GetValue(IslandWidthProperty()));
    }

    winrt::author::setter DesktopFlyoutIsland::IslandWidth(Microsoft::UI::Xaml::GridLength value)
    {
        self(this)->SetValue(IslandWidthProperty(), winrt::box_value(value));
        return {};
    }

    Microsoft::UI::Xaml::GridLength DesktopFlyoutIsland::IslandHeight(winrt::author::getter)
    {
        return winrt::unbox_value<Microsoft::UI::Xaml::GridLength>(
            self(this)->GetValue(IslandHeightProperty()));
    }

    winrt::author::setter DesktopFlyoutIsland::IslandHeight(Microsoft::UI::Xaml::GridLength value)
    {
        self(this)->SetValue(IslandHeightProperty(), winrt::box_value(value));
        return {};
    }

    winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings
        DesktopFlyoutIsland::TemplateSettings(winrt::author::getter)
    {
        return m_templateSettings;
    }

    DesktopFlyoutIslandsPanel::DesktopFlyoutIslandsPanel() = default;

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyoutIslandsPanel::OrientationProperty(
        winrt::author::getter)
    {
        static auto property = Microsoft::UI::Xaml::DependencyProperty::Register(
            L"Orientation",
            winrt::xaml_typename<Microsoft::UI::Xaml::Controls::Orientation>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIslandsPanel>(),
            Microsoft::UI::Xaml::PropertyMetadata{
                winrt::box_value(Microsoft::UI::Xaml::Controls::Orientation::Vertical) });
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyoutIslandsPanel::SpacingProperty(
        winrt::author::getter)
    {
        static auto property = Microsoft::UI::Xaml::DependencyProperty::Register(
            L"Spacing",
            winrt::xaml_typename<double>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIslandsPanel>(),
            Microsoft::UI::Xaml::PropertyMetadata{ winrt::box_value(0.0) });
        return property;
    }

    Microsoft::UI::Xaml::Controls::Orientation DesktopFlyoutIslandsPanel::Orientation(
        winrt::author::getter)
    {
        return winrt::unbox_value<Microsoft::UI::Xaml::Controls::Orientation>(
            self(this)->GetValue(OrientationProperty()));
    }

    winrt::author::setter DesktopFlyoutIslandsPanel::Orientation(
        Microsoft::UI::Xaml::Controls::Orientation value)
    {
        self(this)->SetValue(OrientationProperty(), winrt::box_value(value));
        self(this)->InvalidateMeasure();
        return {};
    }

    double DesktopFlyoutIslandsPanel::Spacing(winrt::author::getter)
    {
        return winrt::unbox_value<double>(self(this)->GetValue(SpacingProperty()));
    }

    winrt::author::setter DesktopFlyoutIslandsPanel::Spacing(double value)
    {
        self(this)->SetValue(SpacingProperty(), winrt::box_value(value));
        self(this)->InvalidateMeasure();
        return {};
    }

    Windows::Foundation::Size DesktopFlyoutIslandsPanel::MeasureOverride(
        Windows::Foundation::Size availableSize,
        winrt::author::override)
    {
        struct LayoutItem
        {
            Microsoft::UI::Xaml::UIElement element{ nullptr };
            Microsoft::UI::Xaml::GridLength length{};
        };

        const auto orientation = Orientation();
        const bool vertical = orientation == Microsoft::UI::Xaml::Controls::Orientation::Vertical;
        const auto spacing = std::isfinite(Spacing()) ? std::max(0.0, Spacing()) : 0.0;
        std::vector<LayoutItem> items;

        for (auto const& child : self(this)->Children())
        {
            auto island = child.try_as<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
            if (!island)
            {
                if (auto presenter = child.try_as<Microsoft::UI::Xaml::Controls::ContentPresenter>())
                {
                    island = presenter.Content().try_as<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
                }
            }

            if (island && island.Visibility() != Microsoft::UI::Xaml::Visibility::Visible)
            {
                child.Measure({ 0.0f, 0.0f });
                continue;
            }

            items.push_back({
                child,
                island
                    ? (vertical ? island.IslandHeight() : island.IslandWidth())
                    : Microsoft::UI::Xaml::GridLength{ 1.0, Microsoft::UI::Xaml::GridUnitType::Auto } });
        }

        const auto primaryAvailable = vertical ? availableSize.Height : availableSize.Width;
        const auto crossAvailable = vertical ? availableSize.Width : availableSize.Height;
        const bool finitePrimary = std::isfinite(primaryAvailable);
        const auto spacingTotal = items.empty() ? 0.0 : spacing * static_cast<double>(items.size() - 1);
        double fixedLength{};
        double autoLength{};
        double starLength{};
        double starWeight{};
        double desiredCross{};

        for (auto const& item : items)
        {
            if (item.length.GridUnitType == Microsoft::UI::Xaml::GridUnitType::Star)
            {
                starWeight += std::max(0.0, item.length.Value);
                continue;
            }

            const double requestedLength = item.length.GridUnitType == Microsoft::UI::Xaml::GridUnitType::Pixel
                ? std::max(0.0, item.length.Value)
                : std::numeric_limits<double>::infinity();
            item.element.Measure(vertical
                ? Windows::Foundation::Size{ crossAvailable, static_cast<float>(requestedLength) }
                : Windows::Foundation::Size{ static_cast<float>(requestedLength), crossAvailable });
            const auto desired = item.element.DesiredSize();
            if (item.length.GridUnitType == Microsoft::UI::Xaml::GridUnitType::Pixel)
            {
                fixedLength += requestedLength;
            }
            else
            {
                autoLength += vertical ? desired.Height : desired.Width;
            }
            desiredCross = std::max<double>(desiredCross, vertical ? desired.Width : desired.Height);
        }

        const auto remaining = finitePrimary
            ? std::max(0.0, static_cast<double>(primaryAvailable) - fixedLength - autoLength - spacingTotal)
            : std::numeric_limits<double>::infinity();

        for (auto const& item : items)
        {
            if (item.length.GridUnitType != Microsoft::UI::Xaml::GridUnitType::Star)
            {
                continue;
            }

            const auto allocated = finitePrimary && starWeight > 0.0
                ? remaining * (std::max(0.0, item.length.Value) / starWeight)
                : std::numeric_limits<double>::infinity();
            item.element.Measure(vertical
                ? Windows::Foundation::Size{ crossAvailable, static_cast<float>(allocated) }
                : Windows::Foundation::Size{ static_cast<float>(allocated), crossAvailable });
            const auto desired = item.element.DesiredSize();
            starLength += finitePrimary ? allocated : (vertical ? desired.Height : desired.Width);
            desiredCross = std::max<double>(desiredCross, vertical ? desired.Width : desired.Height);
        }

        const auto desiredPrimary = fixedLength + autoLength + starLength + spacingTotal;
        return vertical
            ? Windows::Foundation::Size{ static_cast<float>(desiredCross), static_cast<float>(desiredPrimary) }
            : Windows::Foundation::Size{ static_cast<float>(desiredPrimary), static_cast<float>(desiredCross) };
    }

    Windows::Foundation::Size DesktopFlyoutIslandsPanel::ArrangeOverride(
        Windows::Foundation::Size finalSize,
        winrt::author::override)
    {
        struct LayoutItem
        {
            Microsoft::UI::Xaml::UIElement element{ nullptr };
            Microsoft::UI::Xaml::GridLength length{};
        };

        const auto orientation = Orientation();
        const bool vertical = orientation == Microsoft::UI::Xaml::Controls::Orientation::Vertical;
        const auto spacing = std::isfinite(Spacing()) ? std::max(0.0, Spacing()) : 0.0;
        std::vector<LayoutItem> items;

        for (auto const& child : self(this)->Children())
        {
            auto island = child.try_as<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
            if (!island)
            {
                if (auto presenter = child.try_as<Microsoft::UI::Xaml::Controls::ContentPresenter>())
                {
                    island = presenter.Content().try_as<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
                }
            }

            if (island && island.Visibility() != Microsoft::UI::Xaml::Visibility::Visible)
            {
                child.Arrange({ 0.0f, 0.0f, 0.0f, 0.0f });
                continue;
            }

            items.push_back({
                child,
                island
                    ? (vertical ? island.IslandHeight() : island.IslandWidth())
                    : Microsoft::UI::Xaml::GridLength{ 1.0, Microsoft::UI::Xaml::GridUnitType::Auto } });
        }

        const auto primaryFinal = vertical ? static_cast<double>(finalSize.Height) : finalSize.Width;
        const auto crossFinal = vertical ? static_cast<double>(finalSize.Width) : finalSize.Height;
        const auto spacingTotal = items.empty() ? 0.0 : spacing * static_cast<double>(items.size() - 1);
        double fixedLength{};
        double autoLength{};
        double starWeight{};

        for (auto const& item : items)
        {
            if (item.length.GridUnitType == Microsoft::UI::Xaml::GridUnitType::Star)
            {
                starWeight += std::max(0.0, item.length.Value);
            }
            else if (item.length.GridUnitType == Microsoft::UI::Xaml::GridUnitType::Pixel)
            {
                fixedLength += std::max(0.0, item.length.Value);
            }
            else
            {
                const auto desired = item.element.DesiredSize();
                autoLength += vertical ? desired.Height : desired.Width;
            }
        }

        const auto remaining = std::max(0.0, primaryFinal - fixedLength - autoLength - spacingTotal);
        double offset{};
        for (auto const& item : items)
        {
            double length{};
            if (item.length.GridUnitType == Microsoft::UI::Xaml::GridUnitType::Star)
            {
                length = starWeight > 0.0
                    ? remaining * (std::max(0.0, item.length.Value) / starWeight)
                    : 0.0;
            }
            else if (item.length.GridUnitType == Microsoft::UI::Xaml::GridUnitType::Pixel)
            {
                length = std::max(0.0, item.length.Value);
            }
            else
            {
                const auto desired = item.element.DesiredSize();
                length = vertical ? desired.Height : desired.Width;
            }

            item.element.Arrange(vertical
                ? Windows::Foundation::Rect{
                    0.0f, static_cast<float>(offset), static_cast<float>(crossFinal), static_cast<float>(length) }
                : Windows::Foundation::Rect{
                    static_cast<float>(offset), 0.0f, static_cast<float>(length), static_cast<float>(crossFinal) });
            offset += length + spacing;
        }

        return finalSize;
    }

    template<typename T>
    Microsoft::UI::Xaml::DependencyProperty RegisterDesktopFlyoutProperty(
        wchar_t const* name,
        T defaultValue)
    {
        return Microsoft::UI::Xaml::DependencyProperty::Register(
            name,
            winrt::xaml_typename<T>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
            Microsoft::UI::Xaml::PropertyMetadata{ winrt::box_value(defaultValue) });
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::FlyoutWidthProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(
            L"FlyoutWidth",
            Microsoft::UI::Xaml::GridLength{ 1.0, Microsoft::UI::Xaml::GridUnitType::Auto });
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::FlyoutHeightProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(
            L"FlyoutHeight",
            Microsoft::UI::Xaml::GridLength{ 1.0, Microsoft::UI::Xaml::GridUnitType::Auto });
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::PlacementProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(L"Placement", DesktopFlyoutPlacementMode::BottomRight);
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::PopupDirectionProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(L"PopupDirection", DesktopFlyoutPopupDirection::Vertical);
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::ActivationModeProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(L"ActivationMode", DesktopFlyoutActivationMode::Activate);
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::HideOnLostFocusProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(L"HideOnLostFocus", true);
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::MenuFlyoutProperty(winrt::author::getter)
    {
        static auto property = Microsoft::UI::Xaml::DependencyProperty::Register(
            L"MenuFlyout",
            winrt::xaml_typename<Microsoft::UI::Xaml::Controls::MenuFlyout>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
            Microsoft::UI::Xaml::PropertyMetadata{ nullptr });
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::IslandsSourceProperty(winrt::author::getter)
    {
        static auto property = Microsoft::UI::Xaml::DependencyProperty::Register(
            L"IslandsSource",
            winrt::xaml_typename<Windows::Foundation::IInspectable>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
            Microsoft::UI::Xaml::PropertyMetadata{ nullptr });
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::IslandsOrientationProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(
            L"IslandsOrientation",
            Microsoft::UI::Xaml::Controls::Orientation::Vertical);
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::IsBackdropEnabledProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(L"IsBackdropEnabled", true);
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::BackdropKindProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(L"BackdropKind", DesktopFlyoutBackdropKind::DesktopAcrylic);
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::IsTransitionAnimationEnabledProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(L"IsTransitionAnimationEnabled", true);
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::PressedScaleProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(L"PressedScale", 1.0);
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::IsSwipeToDismissEnabledProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(L"IsSwipeToDismissEnabled", false);
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::SwipeDismissThresholdProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(L"SwipeDismissThreshold", 80.0);
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::AutoCloseDelayProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(L"AutoCloseDelay", Windows::Foundation::TimeSpan{});
        return property;
    }

    Microsoft::UI::Xaml::DependencyProperty DesktopFlyout::IsOpenProperty(winrt::author::getter)
    {
        static auto property = RegisterDesktopFlyoutProperty(L"IsOpen", false);
        return property;
    }

    DesktopFlyout::DesktopFlyout()
        : m_native(std::make_unique<DesktopFlyoutNativeState>())
    {
        m_islands = winrt::single_threaded_observable_vector<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
        RegisterDependencyPropertyCallbacks();
        self(this)->Margin(m_margin);
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

    void DesktopFlyout::RegisterDependencyPropertyChangedCallback(
        Microsoft::UI::Xaml::DependencyProperty const& property,
        std::function<void()> callback)
    {
        const auto token = self(this)->RegisterPropertyChangedCallback(
            property,
            [callback = std::move(callback)](auto const&, auto const&)
            {
                callback();
            });
        m_propertyCallbacks.emplace_back(property, token);
    }

    void DesktopFlyout::RegisterDependencyPropertyCallbacks()
    {
        RegisterDependencyPropertyChangedCallback(FlyoutWidthProperty(), [this]
        {
            m_flyoutWidth = winrt::unbox_value<Microsoft::UI::Xaml::GridLength>(self(this)->GetValue(FlyoutWidthProperty()));
            UpdateFlyoutLayout(false);
        });
        RegisterDependencyPropertyChangedCallback(FlyoutHeightProperty(), [this]
        {
            m_flyoutHeight = winrt::unbox_value<Microsoft::UI::Xaml::GridLength>(self(this)->GetValue(FlyoutHeightProperty()));
            UpdateFlyoutLayout(false);
        });
        RegisterDependencyPropertyChangedCallback(PlacementProperty(), [this]
        {
            m_placement = winrt::unbox_value<DesktopFlyoutPlacementMode>(self(this)->GetValue(PlacementProperty()));
            UpdateFlyoutLayout(false);
        });
        RegisterDependencyPropertyChangedCallback(PopupDirectionProperty(), [this]
        {
            m_popupDirection = winrt::unbox_value<DesktopFlyoutPopupDirection>(self(this)->GetValue(PopupDirectionProperty()));
            UpdateFlyoutLayout(false);
        });
        RegisterDependencyPropertyChangedCallback(ActivationModeProperty(), [this]
        {
            m_activationMode = winrt::unbox_value<DesktopFlyoutActivationMode>(self(this)->GetValue(ActivationModeProperty()));
            if (m_native)
            {
                m_native->activationMode = m_activationMode;
                m_native->host.ActivationMode(m_activationMode);
                m_native->visual.FocusConfiguration(m_activationMode == DesktopFlyoutActivationMode::NeverActivate);
            }
        });
        RegisterDependencyPropertyChangedCallback(HideOnLostFocusProperty(), [this]
        {
            m_hideOnLostFocus = winrt::unbox_value<bool>(self(this)->GetValue(HideOnLostFocusProperty()));
            if (m_native)
            {
                m_native->hideOnLostFocus = m_hideOnLostFocus;
                m_native->host.HideOnLostFocus(m_hideOnLostFocus);
            }
        });
        RegisterDependencyPropertyChangedCallback(MenuFlyoutProperty(), [this]
        {
            m_menuFlyout = self(this)->GetValue(MenuFlyoutProperty()).try_as<Microsoft::UI::Xaml::Controls::MenuFlyout>();
        });
        RegisterDependencyPropertyChangedCallback(IslandsSourceProperty(), [this]
        {
            m_islandsSource = self(this)->GetValue(IslandsSourceProperty());
            auto iterable = m_islandsSource.try_as<
                Windows::Foundation::Collections::IIterable<winrt::DesktopFlyouts::DesktopFlyoutIsland>>();
            if (iterable)
            {
                m_islands.Clear();
                for (auto const& island : iterable)
                {
                    if (island)
                    {
                        m_islands.Append(island);
                    }
                }
                if (m_native)
                {
                    RefreshContent();
                }
            }
        });
        RegisterDependencyPropertyChangedCallback(IslandsOrientationProperty(), [this]
        {
            m_islandsOrientation = winrt::unbox_value<Microsoft::UI::Xaml::Controls::Orientation>(self(this)->GetValue(IslandsOrientationProperty()));
            if (m_native)
            {
                RefreshContent();
            }
        });
        RegisterDependencyPropertyChangedCallback(IsBackdropEnabledProperty(), [this]
        {
            m_isBackdropEnabled = winrt::unbox_value<bool>(self(this)->GetValue(IsBackdropEnabledProperty()));
            if (m_native)
            {
                m_native->visual.ApplySystemBackdrop(m_native->host.XamlSource(), m_isBackdropEnabled, m_backdropKind);
            }
        });
        RegisterDependencyPropertyChangedCallback(BackdropKindProperty(), [this]
        {
            m_backdropKind = winrt::unbox_value<DesktopFlyoutBackdropKind>(self(this)->GetValue(BackdropKindProperty()));
            if (m_native)
            {
                m_native->visual.ApplySystemBackdrop(m_native->host.XamlSource(), m_isBackdropEnabled, m_backdropKind);
            }
        });
        RegisterDependencyPropertyChangedCallback(IsTransitionAnimationEnabledProperty(), [this]
        {
            m_isTransitionAnimationEnabled = winrt::unbox_value<bool>(self(this)->GetValue(IsTransitionAnimationEnabledProperty()));
        });
        RegisterDependencyPropertyChangedCallback(PressedScaleProperty(), [this]
        {
            const auto value = winrt::unbox_value<double>(self(this)->GetValue(PressedScaleProperty()));
            m_pressedScale = std::isfinite(value) ? std::clamp(value, 0.1, 2.0) : 1.0;
            if (m_native)
            {
                m_native->visual.InteractionConfiguration(m_isSwipeToDismissEnabled, m_pressedScale, m_swipeDismissThreshold);
            }
        });
        RegisterDependencyPropertyChangedCallback(IsSwipeToDismissEnabledProperty(), [this]
        {
            m_isSwipeToDismissEnabled = winrt::unbox_value<bool>(self(this)->GetValue(IsSwipeToDismissEnabledProperty()));
            if (m_native)
            {
                m_native->visual.InteractionConfiguration(m_isSwipeToDismissEnabled, m_pressedScale, m_swipeDismissThreshold);
            }
        });
        RegisterDependencyPropertyChangedCallback(SwipeDismissThresholdProperty(), [this]
        {
            const auto value = winrt::unbox_value<double>(self(this)->GetValue(SwipeDismissThresholdProperty()));
            m_swipeDismissThreshold = std::isfinite(value) ? std::clamp(value, 1.0, 2000.0) : 80.0;
            if (m_native)
            {
                m_native->visual.InteractionConfiguration(m_isSwipeToDismissEnabled, m_pressedScale, m_swipeDismissThreshold);
            }
        });
        RegisterDependencyPropertyChangedCallback(AutoCloseDelayProperty(), [this]
        {
            m_autoCloseDelay = winrt::unbox_value<Windows::Foundation::TimeSpan>(self(this)->GetValue(AutoCloseDelayProperty()));
            if (m_native && m_native->isOpen)
            {
                m_native->host.ConfigureAutoCloseTimer(m_autoCloseDelay);
            }
        });
        RegisterDependencyPropertyChangedCallback(Microsoft::UI::Xaml::FrameworkElement::DataContextProperty(), [this]
        {
            if (m_native)
            {
                m_native->visual.DataContext(self(this)->DataContext());
            }
        });
    }

    DesktopFlyout::~DesktopFlyout()
    {
        try
        {
            Close();
        }
        catch (...)
        {
        }
    }

    void DesktopFlyout::Close(winrt::author::override)
    {
        if (m_isClosed)
        {
            return;
        }
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        SetIsOpen(false);
        m_isClosed = true;

        for (auto const& [property, token] : m_propertyCallbacks)
        {
            try
            {
                self(this)->UnregisterPropertyChangedCallback(property, token);
            }
            catch (...)
            {
            }
        }
        m_propertyCallbacks.clear();

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
            m_native.reset();
        }
    }

    std::int64_t DesktopFlyout::OwnerWindowHandle(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return m_ownerWindowHandle;
    }

    winrt::author::setter DesktopFlyout::OwnerWindowHandle(std::int64_t value)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        m_ownerWindowHandle = value;
        if (m_native)
        {
            m_native->host.OwnerWindow(reinterpret_cast<HWND>(value));
        }
        return {};
    }

    Microsoft::UI::Xaml::GridLength DesktopFlyout::FlyoutWidth(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<Microsoft::UI::Xaml::GridLength>(self(this)->GetValue(FlyoutWidthProperty()));
    }

    winrt::author::setter DesktopFlyout::FlyoutWidth(Microsoft::UI::Xaml::GridLength value)
    {
        self(this)->SetValue(FlyoutWidthProperty(), winrt::box_value(value));
        return {};
    }

    Microsoft::UI::Xaml::GridLength DesktopFlyout::FlyoutHeight(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<Microsoft::UI::Xaml::GridLength>(self(this)->GetValue(FlyoutHeightProperty()));
    }

    winrt::author::setter DesktopFlyout::FlyoutHeight(Microsoft::UI::Xaml::GridLength value)
    {
        self(this)->SetValue(FlyoutHeightProperty(), winrt::box_value(value));
        return {};
    }

    DesktopFlyoutPlacementMode DesktopFlyout::Placement(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<DesktopFlyoutPlacementMode>(self(this)->GetValue(PlacementProperty()));
    }

    winrt::author::setter DesktopFlyout::Placement(DesktopFlyoutPlacementMode value)
    {
        self(this)->SetValue(PlacementProperty(), winrt::box_value(value));
        return {};
    }

    DesktopFlyoutPopupDirection DesktopFlyout::PopupDirection(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<DesktopFlyoutPopupDirection>(self(this)->GetValue(PopupDirectionProperty()));
    }

    winrt::author::setter DesktopFlyout::PopupDirection(DesktopFlyoutPopupDirection value)
    {
        self(this)->SetValue(PopupDirectionProperty(), winrt::box_value(value));
        return {};
    }

    DesktopFlyoutActivationMode DesktopFlyout::ActivationMode(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<DesktopFlyoutActivationMode>(self(this)->GetValue(ActivationModeProperty()));
    }

    winrt::author::setter DesktopFlyout::ActivationMode(DesktopFlyoutActivationMode value)
    {
        self(this)->SetValue(ActivationModeProperty(), winrt::box_value(value));
        return {};
    }

    bool DesktopFlyout::HideOnLostFocus(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<bool>(self(this)->GetValue(HideOnLostFocusProperty()));
    }

    winrt::author::setter DesktopFlyout::HideOnLostFocus(bool value)
    {
        self(this)->SetValue(HideOnLostFocusProperty(), winrt::box_value(value));
        return {};
    }

    Microsoft::UI::Xaml::UIElement DesktopFlyout::Content(winrt::author::getter)
    {
        if (!m_native)
        {
            return nullptr;
        }
        EnsureUiThread(*m_native);
        return m_native->visual.RequestedContent();
    }

    winrt::author::setter DesktopFlyout::Content(Microsoft::UI::Xaml::UIElement const& value)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
            m_native->visual.RequestedContent(value);
            RefreshContent();
        }
        return {};
    }

    Microsoft::UI::Xaml::Controls::MenuFlyout DesktopFlyout::MenuFlyout(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return self(this)->GetValue(MenuFlyoutProperty()).try_as<Microsoft::UI::Xaml::Controls::MenuFlyout>();
    }

    winrt::author::setter DesktopFlyout::MenuFlyout(
        Microsoft::UI::Xaml::Controls::MenuFlyout const& value)
    {
        self(this)->SetValue(MenuFlyoutProperty(), value);
        return {};
    }

    Windows::Foundation::Collections::IObservableVector<winrt::DesktopFlyouts::DesktopFlyoutIsland>
        DesktopFlyout::Islands(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        if (!m_islands)
        {
            m_islands = winrt::single_threaded_observable_vector<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
        }
        return m_islands;
    }

    Windows::Foundation::IInspectable DesktopFlyout::IslandsSource(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return self(this)->GetValue(IslandsSourceProperty());
    }

    winrt::author::setter DesktopFlyout::IslandsSource(Windows::Foundation::IInspectable const& value)
    {
        self(this)->SetValue(IslandsSourceProperty(), value);
        return {};
    }

    Microsoft::UI::Xaml::Controls::Orientation DesktopFlyout::IslandsOrientation(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<Microsoft::UI::Xaml::Controls::Orientation>(self(this)->GetValue(IslandsOrientationProperty()));
    }

    winrt::author::setter DesktopFlyout::IslandsOrientation(
        Microsoft::UI::Xaml::Controls::Orientation value)
    {
        self(this)->SetValue(IslandsOrientationProperty(), winrt::box_value(value));
        return {};
    }

    std::int32_t DesktopFlyout::IslandSpacing(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return m_islandSpacing;
    }

    winrt::author::setter DesktopFlyout::IslandSpacing(std::int32_t value)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        m_islandSpacing = std::clamp(value, 0, 120);
        if (m_native)
        {
            RefreshContent();
        }
        return {};
    }

    bool DesktopFlyout::IsBackdropEnabled(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<bool>(self(this)->GetValue(IsBackdropEnabledProperty()));
    }

    winrt::author::setter DesktopFlyout::IsBackdropEnabled(bool value)
    {
        self(this)->SetValue(IsBackdropEnabledProperty(), winrt::box_value(value));
        return {};
    }

    DesktopFlyoutBackdropKind DesktopFlyout::BackdropKind(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<DesktopFlyoutBackdropKind>(self(this)->GetValue(BackdropKindProperty()));
    }

    winrt::author::setter DesktopFlyout::BackdropKind(DesktopFlyoutBackdropKind value)
    {
        self(this)->SetValue(BackdropKindProperty(), winrt::box_value(value));
        return {};
    }

    bool DesktopFlyout::IsTransitionAnimationEnabled(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<bool>(self(this)->GetValue(IsTransitionAnimationEnabledProperty()));
    }

    winrt::author::setter DesktopFlyout::IsTransitionAnimationEnabled(bool value)
    {
        self(this)->SetValue(IsTransitionAnimationEnabledProperty(), winrt::box_value(value));
        return {};
    }

    double DesktopFlyout::PressedScale(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<double>(self(this)->GetValue(PressedScaleProperty()));
    }

    winrt::author::setter DesktopFlyout::PressedScale(double value)
    {
        self(this)->SetValue(PressedScaleProperty(), winrt::box_value(value));
        return {};
    }

    bool DesktopFlyout::IsSwipeToDismissEnabled(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<bool>(self(this)->GetValue(IsSwipeToDismissEnabledProperty()));
    }

    winrt::author::setter DesktopFlyout::IsSwipeToDismissEnabled(bool value)
    {
        self(this)->SetValue(IsSwipeToDismissEnabledProperty(), winrt::box_value(value));
        return {};
    }

    double DesktopFlyout::SwipeDismissThreshold(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<double>(self(this)->GetValue(SwipeDismissThresholdProperty()));
    }

    winrt::author::setter DesktopFlyout::SwipeDismissThreshold(double value)
    {
        self(this)->SetValue(SwipeDismissThresholdProperty(), winrt::box_value(value));
        return {};
    }

    Windows::Foundation::TimeSpan DesktopFlyout::AutoCloseDelay(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<Windows::Foundation::TimeSpan>(self(this)->GetValue(AutoCloseDelayProperty()));
    }

    winrt::author::setter DesktopFlyout::AutoCloseDelay(Windows::Foundation::TimeSpan value)
    {
        self(this)->SetValue(AutoCloseDelayProperty(), winrt::box_value(value));
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
        auto margin = self(this)->Margin();
        m_margin = {
            std::max(0.0, std::isfinite(margin.Left) ? margin.Left : 0.0),
            std::max(0.0, std::isfinite(margin.Top) ? margin.Top : 0.0),
            std::max(0.0, std::isfinite(margin.Right) ? margin.Right : 0.0),
            std::max(0.0, std::isfinite(margin.Bottom) ? margin.Bottom : 0.0) };
        m_native->visual.InteractionConfiguration(
            m_isSwipeToDismissEnabled,
            m_pressedScale,
            m_swipeDismissThreshold);
        m_native->visual.Margin(m_margin);
        m_native->visual.RefreshContent(
            m_native->host.XamlSource(),
            m_islands,
            m_islandsOrientation,
            m_islandSpacing,
            self(this)->DataContext());
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
        SetIsOpen(false);
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
        SetIsOpen(true);
        m_native->host.IsOpen(true);
        m_native->visual.IsOpen(true);
        m_state = DesktopFlyoutState::open;
        m_native->host.ConfigureAutoCloseTimer(m_autoCloseDelay);
        if (m_activationMode == DesktopFlyoutActivationMode::Activate)
        {
            (void)m_native->host.NavigateFocus(
                Microsoft::UI::Xaml::Hosting::XamlSourceFocusNavigationReason::Programmatic);
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
        SetIsOpen(false);
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
        if (!m_native)
        {
            return DesktopFlyoutState::closed;
        }
        EnsureUiThread(*m_native);
        return m_state;
    }

    bool DesktopFlyout::IsOpen(winrt::author::getter)
    {
        if (m_native)
        {
            EnsureUiThread(*m_native);
        }
        return winrt::unbox_value<bool>(self(this)->GetValue(IsOpenProperty()));
    }

    void DesktopFlyout::SetIsOpen(bool value)
    {
        self(this)->SetValue(IsOpenProperty(), winrt::box_value(value));
    }

    void DesktopFlyout::Show()
    {
        if (!m_native || m_isClosed)
        {
            return;
        }
        EnsureUiThread(*m_native);
        m_native->customPlacementPoint.reset();
        ShowCore();
    }

    void DesktopFlyout::ShowAt(std::int32_t x, std::int32_t y)
    {
        if (!m_native || m_isClosed)
        {
            return;
        }
        EnsureUiThread(*m_native);
        m_native->customPlacementPoint = POINT{ x, y };
        ShowCore();
    }

    void DesktopFlyout::Show(Windows::Foundation::Point bottomCenterPoint)
    {
        if (!m_native || m_isClosed)
        {
            return;
        }
        EnsureUiThread(*m_native);
        m_native->customPlacementPoint = POINT{
            static_cast<LONG>(std::lround(bottomCenterPoint.X)),
            static_cast<LONG>(std::lround(bottomCenterPoint.Y)) };
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
            const auto contentWidth = ResolveFlyoutLength(m_flyoutWidth, availableWidth, autoWidth);
            const auto contentHeight = ResolveFlyoutLength(m_flyoutHeight, availableHeight, autoHeight);
            m_native->visual.SetResolvedSize(contentWidth, contentHeight);

            const auto frameWidth = static_cast<std::int32_t>(std::ceil(
                (contentWidth + m_margin.Left + m_margin.Right) * scale));
            const auto frameHeight = static_cast<std::int32_t>(std::ceil(
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
            m_activationMode == DesktopFlyoutActivationMode::NeverActivate);

        if (m_activationMode != DesktopFlyoutActivationMode::Activate)
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
        if (!m_native || m_isClosed)
        {
            return;
        }
        EnsureUiThread(*m_native);
        BeginCloseAnimation();
    }

    void DesktopFlyout::NavigateFocus()
    {
        if (!m_native || m_isClosed)
        {
            return;
        }
        NavigateFocus(Microsoft::UI::Xaml::Hosting::XamlSourceFocusNavigationReason::Programmatic);
    }

    void DesktopFlyout::NavigateFocus(
        Microsoft::UI::Xaml::Hosting::XamlSourceFocusNavigationReason reason)
    {
        if (!m_native || m_isClosed)
        {
            return;
        }
        EnsureUiThread(*m_native);
        (void)m_native->host.NavigateFocus(reason);
    }

    bool DesktopFlyout::TryPreTranslateMessage(std::int64_t message)
    {
        if (!m_native || m_isClosed)
        {
            return false;
        }
        EnsureUiThread(*m_native);
        return m_native->host.TryPreTranslateMessage(reinterpret_cast<MSG const*>(message));
    }
}
