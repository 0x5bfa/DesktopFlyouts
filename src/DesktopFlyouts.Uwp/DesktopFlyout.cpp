#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#undef GetCurrentTime

#include "author/DesktopFlyout.author.h"
#include "author/DesktopFlyout.author.impl.h"
#include "DesktopFlyoutHost.h"
#include "FlyoutLayout.h"
#include "winrt/DesktopFlyouts.h"

#include <winrt/Windows.UI.Xaml.Controls.h>
#include <winrt/Windows.UI.Xaml.Interop.h>

#include <algorithm>
#include <cmath>
#include <limits>
#include <optional>
#include <vector>

namespace winrt::DesktopFlyouts::author
{
    struct DesktopFlyoutNativeState
    {
        detail::DesktopFlyoutHost host;
        winrt::DesktopFlyouts::DesktopFlyoutIslandsPanel panel{ nullptr };
        std::optional<POINT> customPlacementPoint;
        desktop_flyouts::core::popup_direction activeDirection{
            desktop_flyouts::core::popup_direction::bottom_to_top };
        bool isOpen{};
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
        using source = winrt::DesktopFlyouts::author::DesktopFlyoutPlacementMode;
        using target = desktop_flyouts::core::placement_mode;
        switch (placement)
        {
        case source::top_center: return target::top_center;
        case source::top_left: return target::top_left;
        case source::top_right: return target::top_right;
        case source::bottom_center: return target::bottom_center;
        case source::bottom_left: return target::bottom_left;
        case source::bottom_right: return target::bottom_right;
        case source::left_center: return target::left_center;
        case source::right_center: return target::right_center;
        default: return target::bottom_right;
        }
    }

    desktop_flyouts::core::popup_direction ToCoreDirection(
        winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection direction) noexcept
    {
        using source = winrt::DesktopFlyouts::author::DesktopFlyoutPopupDirection;
        using target = desktop_flyouts::core::popup_direction;
        switch (direction)
        {
        case source::bottom_to_top: return target::bottom_to_top;
        case source::top_to_bottom: return target::top_to_bottom;
        case source::vertical: return target::vertical;
        case source::left_to_right: return target::left_to_right;
        case source::right_to_left: return target::right_to_left;
        case source::horizontal: return target::horizontal;
        default: return target::vertical;
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
        winrt::Windows::UI::Xaml::GridLength length,
        double available,
        double fallback) noexcept
    {
        using namespace winrt::Windows::UI::Xaml;
        if (length.GridUnitType == GridUnitType::Star)
        {
            return available;
        }
        if (length.GridUnitType == GridUnitType::Pixel && std::isfinite(length.Value))
        {
            return std::clamp(length.Value, 0.0, available);
        }
        return fallback;
    }
}

namespace winrt::DesktopFlyouts::author
{
    DesktopFlyoutIslandTemplateSettings::DesktopFlyoutIslandTemplateSettings() = default;

    Windows::UI::Xaml::DependencyProperty
        DesktopFlyoutIslandTemplateSettings::BackdropCornerRadiusProperty(winrt::author::getter)
    {
        static auto property = Windows::UI::Xaml::DependencyProperty::Register(
            L"BackdropCornerRadius",
            winrt::xaml_typename<Windows::UI::Xaml::CornerRadius>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings>(),
            Windows::UI::Xaml::PropertyMetadata{ winrt::box_value(Windows::UI::Xaml::CornerRadius{}) });
        return property;
    }

    Windows::UI::Xaml::CornerRadius
        DesktopFlyoutIslandTemplateSettings::BackdropCornerRadius(winrt::author::getter)
    {
        return winrt::unbox_value<Windows::UI::Xaml::CornerRadius>(
            self(this)->GetValue(BackdropCornerRadiusProperty()));
    }

    winrt::author::setter DesktopFlyoutIslandTemplateSettings::BackdropCornerRadius(
        Windows::UI::Xaml::CornerRadius value)
    {
        self(this)->SetValue(BackdropCornerRadiusProperty(), winrt::box_value(value));
        return {};
    }

    DesktopFlyoutIsland::DesktopFlyoutIsland()
        : m_templateSettings(
            winrt::make<winrt::DesktopFlyouts::implementation::DesktopFlyoutIslandTemplateSettings>())
    {
        self(this)->DefaultStyleKey(winrt::box_value(L"DesktopFlyouts.DesktopFlyoutIsland"));
        m_cornerRadiusToken = self(this)->RegisterPropertyChangedCallback(
            Windows::UI::Xaml::Controls::Control::CornerRadiusProperty(),
            [this](auto const&, auto const&)
            {
                const auto radius = self(this)->CornerRadius();
                const auto inner = [](double value) noexcept { return std::max(0.0, value - 1.0); };
                m_templateSettings.BackdropCornerRadius({
                    inner(radius.TopLeft),
                    inner(radius.TopRight),
                    inner(radius.BottomRight),
                    inner(radius.BottomLeft) });
            });
    }

    DesktopFlyoutIsland::~DesktopFlyoutIsland()
    {
        if (m_cornerRadiusToken != 0)
        {
            try
            {
                self(this)->UnregisterPropertyChangedCallback(
                    Windows::UI::Xaml::Controls::Control::CornerRadiusProperty(),
                    m_cornerRadiusToken);
            }
            catch (...)
            {
            }
        }
    }

    Windows::UI::Xaml::DependencyProperty DesktopFlyoutIsland::IslandWidthProperty(winrt::author::getter)
    {
        static auto property = Windows::UI::Xaml::DependencyProperty::Register(
            L"IslandWidth",
            winrt::xaml_typename<Windows::UI::Xaml::GridLength>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIsland>(),
            Windows::UI::Xaml::PropertyMetadata{
                winrt::box_value(Windows::UI::Xaml::GridLength{ 1.0, Windows::UI::Xaml::GridUnitType::Auto }) });
        return property;
    }

    Windows::UI::Xaml::DependencyProperty DesktopFlyoutIsland::IslandHeightProperty(winrt::author::getter)
    {
        static auto property = Windows::UI::Xaml::DependencyProperty::Register(
            L"IslandHeight",
            winrt::xaml_typename<Windows::UI::Xaml::GridLength>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIsland>(),
            Windows::UI::Xaml::PropertyMetadata{
                winrt::box_value(Windows::UI::Xaml::GridLength{ 1.0, Windows::UI::Xaml::GridUnitType::Auto }) });
        return property;
    }

    Windows::UI::Xaml::GridLength DesktopFlyoutIsland::IslandWidth(winrt::author::getter)
    {
        return winrt::unbox_value<Windows::UI::Xaml::GridLength>(self(this)->GetValue(IslandWidthProperty()));
    }

    winrt::author::setter DesktopFlyoutIsland::IslandWidth(Windows::UI::Xaml::GridLength value)
    {
        self(this)->SetValue(IslandWidthProperty(), winrt::box_value(value));
        return {};
    }

    Windows::UI::Xaml::GridLength DesktopFlyoutIsland::IslandHeight(winrt::author::getter)
    {
        return winrt::unbox_value<Windows::UI::Xaml::GridLength>(self(this)->GetValue(IslandHeightProperty()));
    }

    winrt::author::setter DesktopFlyoutIsland::IslandHeight(Windows::UI::Xaml::GridLength value)
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

    Windows::UI::Xaml::DependencyProperty DesktopFlyoutIslandsPanel::OrientationProperty(winrt::author::getter)
    {
        static auto property = Windows::UI::Xaml::DependencyProperty::Register(
            L"Orientation",
            winrt::xaml_typename<Windows::UI::Xaml::Controls::Orientation>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIslandsPanel>(),
            Windows::UI::Xaml::PropertyMetadata{
                winrt::box_value(Windows::UI::Xaml::Controls::Orientation::Vertical) });
        return property;
    }

    Windows::UI::Xaml::DependencyProperty DesktopFlyoutIslandsPanel::SpacingProperty(winrt::author::getter)
    {
        static auto property = Windows::UI::Xaml::DependencyProperty::Register(
            L"Spacing",
            winrt::xaml_typename<double>(),
            winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutIslandsPanel>(),
            Windows::UI::Xaml::PropertyMetadata{ winrt::box_value(0.0) });
        return property;
    }

    Windows::UI::Xaml::Controls::Orientation DesktopFlyoutIslandsPanel::Orientation(winrt::author::getter)
    {
        return winrt::unbox_value<Windows::UI::Xaml::Controls::Orientation>(
            self(this)->GetValue(OrientationProperty()));
    }

    winrt::author::setter DesktopFlyoutIslandsPanel::Orientation(
        Windows::UI::Xaml::Controls::Orientation value)
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
            Windows::UI::Xaml::UIElement element{ nullptr };
            Windows::UI::Xaml::GridLength length{};
        };

        const auto orientation = Orientation();
        const bool vertical = orientation == Windows::UI::Xaml::Controls::Orientation::Vertical;
        const auto spacing = std::isfinite(Spacing()) ? std::max(0.0, Spacing()) : 0.0;
        std::vector<LayoutItem> items;

        for (auto const& child : self(this)->Children())
        {
            auto island = child.try_as<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
            if (!island)
            {
                if (auto presenter = child.try_as<Windows::UI::Xaml::Controls::ContentPresenter>())
                {
                    island = presenter.Content().try_as<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
                }
            }
            if (island && island.Visibility() != Windows::UI::Xaml::Visibility::Visible)
            {
                child.Measure({ 0.0f, 0.0f });
                continue;
            }
            items.push_back({
                child,
                island
                    ? (vertical ? island.IslandHeight() : island.IslandWidth())
                    : Windows::UI::Xaml::GridLength{ 1.0, Windows::UI::Xaml::GridUnitType::Auto } });
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
            if (item.length.GridUnitType == Windows::UI::Xaml::GridUnitType::Star)
            {
                starWeight += std::max(0.0, item.length.Value);
                continue;
            }
            const double requestedLength = item.length.GridUnitType == Windows::UI::Xaml::GridUnitType::Pixel
                ? std::max(0.0, item.length.Value)
                : std::numeric_limits<double>::infinity();
            item.element.Measure(vertical
                ? Windows::Foundation::Size{ crossAvailable, static_cast<float>(requestedLength) }
                : Windows::Foundation::Size{ static_cast<float>(requestedLength), crossAvailable });
            const auto desired = item.element.DesiredSize();
            if (item.length.GridUnitType == Windows::UI::Xaml::GridUnitType::Pixel)
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
            if (item.length.GridUnitType != Windows::UI::Xaml::GridUnitType::Star)
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
            Windows::UI::Xaml::UIElement element{ nullptr };
            Windows::UI::Xaml::GridLength length{};
        };

        const auto orientation = Orientation();
        const bool vertical = orientation == Windows::UI::Xaml::Controls::Orientation::Vertical;
        const auto spacing = std::isfinite(Spacing()) ? std::max(0.0, Spacing()) : 0.0;
        std::vector<LayoutItem> items;
        for (auto const& child : self(this)->Children())
        {
            auto island = child.try_as<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
            if (!island)
            {
                if (auto presenter = child.try_as<Windows::UI::Xaml::Controls::ContentPresenter>())
                {
                    island = presenter.Content().try_as<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
                }
            }
            if (island && island.Visibility() != Windows::UI::Xaml::Visibility::Visible)
            {
                child.Arrange({ 0.0f, 0.0f, 0.0f, 0.0f });
                continue;
            }
            items.push_back({
                child,
                island
                    ? (vertical ? island.IslandHeight() : island.IslandWidth())
                    : Windows::UI::Xaml::GridLength{ 1.0, Windows::UI::Xaml::GridUnitType::Auto } });
        }

        const auto primaryFinal = vertical ? static_cast<double>(finalSize.Height) : finalSize.Width;
        const auto crossFinal = vertical ? static_cast<double>(finalSize.Width) : finalSize.Height;
        const auto spacingTotal = items.empty() ? 0.0 : spacing * static_cast<double>(items.size() - 1);
        double fixedLength{};
        double autoLength{};
        double starWeight{};
        for (auto const& item : items)
        {
            if (item.length.GridUnitType == Windows::UI::Xaml::GridUnitType::Star)
            {
                starWeight += std::max(0.0, item.length.Value);
            }
            else if (item.length.GridUnitType == Windows::UI::Xaml::GridUnitType::Pixel)
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
            if (item.length.GridUnitType == Windows::UI::Xaml::GridUnitType::Star)
            {
                length = starWeight > 0.0
                    ? remaining * (std::max(0.0, item.length.Value) / starWeight)
                    : 0.0;
            }
            else if (item.length.GridUnitType == Windows::UI::Xaml::GridUnitType::Pixel)
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

    DesktopFlyout::DesktopFlyout()
        : m_native(std::make_unique<DesktopFlyoutNativeState>())
    {
        m_islands = winrt::single_threaded_observable_vector<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
        self(this)->DefaultStyleKey(winrt::box_value(L"DesktopFlyouts.DesktopFlyout"));
        self(this)->Margin({ 12.0, 12.0, 12.0, 12.0 });
        m_islandsChangedToken = m_islands.VectorChanged([this](auto const&, auto const&)
        {
            RefreshContent();
        });
        m_native->host.HideCallback([this]
        {
            Hide();
        });
        m_native->host.ActivationMode(m_activationMode);
        m_native->host.HideOnLostFocus(m_hideOnLostFocus);
    }

    DesktopFlyout::~DesktopFlyout()
    {
        if (m_islands && m_islandsChangedToken.value != 0)
        {
            try { m_islands.VectorChanged(m_islandsChangedToken); } catch (...) { }
            m_islandsChangedToken = {};
        }
        if (m_native)
        {
            try { m_native->host.HideCallback({}); } catch (...) { }
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

    Windows::UI::Xaml::GridLength DesktopFlyout::FlyoutWidth(winrt::author::getter)
    {
        return m_flyoutWidth;
    }

    winrt::author::setter DesktopFlyout::FlyoutWidth(Windows::UI::Xaml::GridLength value)
    {
        m_flyoutWidth = value;
        UpdateFlyoutLayout();
        return {};
    }

    Windows::UI::Xaml::GridLength DesktopFlyout::FlyoutHeight(winrt::author::getter)
    {
        return m_flyoutHeight;
    }

    winrt::author::setter DesktopFlyout::FlyoutHeight(Windows::UI::Xaml::GridLength value)
    {
        m_flyoutHeight = value;
        UpdateFlyoutLayout();
        return {};
    }

    DesktopFlyoutPlacementMode DesktopFlyout::Placement(winrt::author::getter) { return m_placement; }
    winrt::author::setter DesktopFlyout::Placement(DesktopFlyoutPlacementMode value)
    {
        m_placement = value;
        UpdateFlyoutLayout();
        return {};
    }
    DesktopFlyoutPopupDirection DesktopFlyout::PopupDirection(winrt::author::getter) { return m_popupDirection; }
    winrt::author::setter DesktopFlyout::PopupDirection(DesktopFlyoutPopupDirection value)
    {
        m_popupDirection = value;
        UpdateFlyoutLayout();
        return {};
    }
    DesktopFlyoutActivationMode DesktopFlyout::ActivationMode(winrt::author::getter) { return m_activationMode; }
    winrt::author::setter DesktopFlyout::ActivationMode(DesktopFlyoutActivationMode value)
    {
        m_activationMode = value;
        m_native->host.ActivationMode(value);
        return {};
    }
    bool DesktopFlyout::HideOnLostFocus(winrt::author::getter) { return m_hideOnLostFocus; }
    winrt::author::setter DesktopFlyout::HideOnLostFocus(bool value)
    {
        m_hideOnLostFocus = value;
        m_native->host.HideOnLostFocus(value);
        return {};
    }

    Windows::UI::Xaml::UIElement DesktopFlyout::Content(winrt::author::getter) { return m_content; }
    winrt::author::setter DesktopFlyout::Content(Windows::UI::Xaml::UIElement const& value)
    {
        m_content = value;
        RefreshContent();
        return {};
    }
    Windows::UI::Xaml::Controls::MenuFlyout DesktopFlyout::MenuFlyout(winrt::author::getter) { return m_menuFlyout; }
    winrt::author::setter DesktopFlyout::MenuFlyout(Windows::UI::Xaml::Controls::MenuFlyout const& value)
    {
        m_menuFlyout = value;
        return {};
    }

    Windows::Foundation::Collections::IObservableVector<winrt::DesktopFlyouts::DesktopFlyoutIsland>
        DesktopFlyout::Islands(winrt::author::getter)
    {
        if (!m_islands)
        {
            m_islands = winrt::single_threaded_observable_vector<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
        }
        return m_islands;
    }

    Windows::Foundation::IInspectable DesktopFlyout::IslandsSource(winrt::author::getter)
    {
        return m_islandsSource;
    }

    winrt::author::setter DesktopFlyout::IslandsSource(Windows::Foundation::IInspectable const& value)
    {
        m_islandsSource = value;
        auto iterable = value.try_as<
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
            RefreshContent();
        }
        return {};
    }

    Windows::UI::Xaml::Controls::Orientation DesktopFlyout::IslandsOrientation(winrt::author::getter)
    {
        return m_islandsOrientation;
    }
    winrt::author::setter DesktopFlyout::IslandsOrientation(Windows::UI::Xaml::Controls::Orientation value)
    {
        m_islandsOrientation = value;
        RefreshContent();
        return {};
    }
    std::int32_t DesktopFlyout::IslandSpacing(winrt::author::getter) { return m_islandSpacing; }
    winrt::author::setter DesktopFlyout::IslandSpacing(std::int32_t value)
    {
        m_islandSpacing = std::clamp(value, 0, 120);
        RefreshContent();
        return {};
    }

    bool DesktopFlyout::IsBackdropEnabled(winrt::author::getter) { return m_isBackdropEnabled; }
    winrt::author::setter DesktopFlyout::IsBackdropEnabled(bool value)
    {
        m_isBackdropEnabled = value;
        return {};
    }
    DesktopFlyoutBackdropKind DesktopFlyout::BackdropKind(winrt::author::getter) { return m_backdropKind; }
    winrt::author::setter DesktopFlyout::BackdropKind(DesktopFlyoutBackdropKind value)
    {
        m_backdropKind = value;
        return {};
    }
    bool DesktopFlyout::IsTransitionAnimationEnabled(winrt::author::getter) { return m_isTransitionAnimationEnabled; }
    winrt::author::setter DesktopFlyout::IsTransitionAnimationEnabled(bool value)
    {
        m_isTransitionAnimationEnabled = value;
        return {};
    }
    double DesktopFlyout::PressedScale(winrt::author::getter) { return m_pressedScale; }
    winrt::author::setter DesktopFlyout::PressedScale(double value)
    {
        m_pressedScale = std::isfinite(value) ? std::clamp(value, 0.1, 2.0) : 1.0;
        return {};
    }
    bool DesktopFlyout::IsSwipeToDismissEnabled(winrt::author::getter) { return m_isSwipeToDismissEnabled; }
    winrt::author::setter DesktopFlyout::IsSwipeToDismissEnabled(bool value)
    {
        m_isSwipeToDismissEnabled = value;
        return {};
    }
    double DesktopFlyout::SwipeDismissThreshold(winrt::author::getter) { return m_swipeDismissThreshold; }
    winrt::author::setter DesktopFlyout::SwipeDismissThreshold(double value)
    {
        m_swipeDismissThreshold = std::isfinite(value) ? std::clamp(value, 1.0, 2000.0) : 80.0;
        return {};
    }
    Windows::Foundation::TimeSpan DesktopFlyout::AutoCloseDelay(winrt::author::getter) { return m_autoCloseDelay; }
    winrt::author::setter DesktopFlyout::AutoCloseDelay(Windows::Foundation::TimeSpan value)
    {
        m_autoCloseDelay = value;
        if (m_native->isOpen)
        {
            m_native->host.ConfigureAutoCloseTimer(value);
        }
        return {};
    }

    DesktopFlyoutState DesktopFlyout::State(winrt::author::getter)
    {
        return m_native->isOpen ? DesktopFlyoutState::open : DesktopFlyoutState::closed;
    }
    bool DesktopFlyout::IsOpen(winrt::author::getter) { return m_native->isOpen; }

    void DesktopFlyout::RefreshContent()
    {
        EnsureUiThread(*m_native);
        if (m_native->refreshingContent)
        {
            return;
        }
        m_native->refreshingContent = true;
        if (!m_native->panel)
        {
            m_native->panel = winrt::DesktopFlyouts::DesktopFlyoutIslandsPanel{};
        }
        m_native->panel.Orientation(m_islandsOrientation);
        m_native->panel.Spacing(static_cast<double>(m_islandSpacing));
        auto children = m_native->panel.Children();
        children.Clear();
        if (m_content)
        {
            children.Append(m_content);
        }
        else
        {
            for (auto const& island : m_islands)
            {
                if (island)
                {
                    children.Append(island);
                }
            }
        }
        if (m_native->host.XamlSource())
        {
            m_native->host.Content(m_native->panel);
        }
        m_native->refreshingContent = false;
        UpdateFlyoutLayout();
    }

    void DesktopFlyout::UpdateFlyoutLayout()
    {
        EnsureUiThread(*m_native);
        if (m_native->updatingLayout || m_native->host.Window() == nullptr || !m_native->panel)
        {
            return;
        }
        m_native->updatingLayout = true;
        const auto scale = m_native->host.RasterizationScale();
        const auto owner = reinterpret_cast<HWND>(m_ownerWindowHandle);
        const auto workArea = GetFlyoutWorkArea(owner, m_native->customPlacementPoint);
        const auto workWidthDip = std::max(1.0, (workArea.right - workArea.left) / scale);
        const auto workHeightDip = std::max(1.0, (workArea.bottom - workArea.top) / scale);
        m_native->panel.Measure({ static_cast<float>(workWidthDip), static_cast<float>(workHeightDip) });
        const auto desired = m_native->panel.DesiredSize();
        const auto widthDip = ResolveFlyoutLength(m_flyoutWidth, workWidthDip, std::max(1.0f, desired.Width));
        const auto heightDip = ResolveFlyoutLength(m_flyoutHeight, workHeightDip, std::max(1.0f, desired.Height));
        const auto width = std::max(1, static_cast<int>(std::ceil(widthDip * scale)));
        const auto height = std::max(1, static_cast<int>(std::ceil(heightDip * scale)));
        const auto margin = self(this)->Margin();
        const auto marginDip = std::max({ 0.0, margin.Left, margin.Top, margin.Right, margin.Bottom });

        desktop_flyouts::core::layout_request request{};
        request.work_area = { workArea.left, workArea.top, workArea.right, workArea.bottom };
        request.desired_size = { width, height };
        request.margin = static_cast<std::int32_t>(std::ceil(marginDip * scale));
        request.placement = ToCorePlacement(m_placement);
        request.direction = ToCoreDirection(m_popupDirection);
        if (m_native->customPlacementPoint)
        {
            request.bottom_center_anchor = desktop_flyouts::core::point{
                m_native->customPlacementPoint->x,
                m_native->customPlacementPoint->y };
        }
        const auto result = desktop_flyouts::core::resolve_layout(request);
        m_native->activeDirection = result.direction;
        m_native->host.MoveAndResize(
            result.bounds.left,
            result.bounds.top,
            result.bounds.width(),
            result.bounds.height());
        m_native->panel.Arrange({
            0.0f,
            0.0f,
            static_cast<float>(result.bounds.width() / scale),
            static_cast<float>(result.bounds.height() / scale) });
        m_native->updatingLayout = false;
    }

    void DesktopFlyout::Show()
    {
        m_native->customPlacementPoint.reset();
        ShowCore();
    }

    void DesktopFlyout::Show(Windows::Foundation::Point bottomCenterPoint)
    {
        m_native->customPlacementPoint = POINT{
            static_cast<LONG>(std::lround(bottomCenterPoint.X)),
            static_cast<LONG>(std::lround(bottomCenterPoint.Y)) };
        ShowCore();
    }

    void DesktopFlyout::ShowAt(std::int32_t x, std::int32_t y)
    {
        m_native->customPlacementPoint = POINT{ x, y };
        ShowCore();
    }

    void DesktopFlyout::ShowCore()
    {
        EnsureUiThread(*m_native);
        auto owner = reinterpret_cast<HWND>(m_ownerWindowHandle);
        if (owner == nullptr || !IsWindow(owner))
        {
            owner = GetForegroundWindow();
        }
        winrt::check_bool(owner != nullptr);
        if (m_activationMode != DesktopFlyoutActivationMode::activate)
        {
            m_native->host.PreserveActivationState();
        }
        m_native->host.EnsureCreated(owner, m_activationMode);
        RefreshContent();
        m_native->host.Content(m_native->panel);
        UpdateFlyoutLayout();
        m_native->host.Show(m_activationMode);
        m_native->isOpen = true;
        m_native->host.IsOpen(true);
        m_native->host.ConfigureAutoCloseTimer(m_autoCloseDelay);
        if (m_activationMode != DesktopFlyoutActivationMode::activate)
        {
            m_native->host.RestoreActivationState();
        }
    }

    void DesktopFlyout::NavigateFocus()
    {
        NavigateFocus(Windows::UI::Xaml::Hosting::XamlSourceFocusNavigationReason::Programmatic);
    }

    void DesktopFlyout::NavigateFocus(Windows::UI::Xaml::Hosting::XamlSourceFocusNavigationReason reason)
    {
        EnsureUiThread(*m_native);
        m_native->host.NavigateFocus(reason);
    }

    bool DesktopFlyout::TryPreTranslateMessage(std::int64_t message)
    {
        EnsureUiThread(*m_native);
        return m_native->host.TryPreTranslateMessage(reinterpret_cast<MSG const*>(message));
    }

    void DesktopFlyout::Hide()
    {
        EnsureUiThread(*m_native);
        m_native->host.StopAutoCloseTimer();
        m_native->host.Hide();
        m_native->isOpen = false;
        m_native->host.IsOpen(false);
    }
}
