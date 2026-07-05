#pragma once
#include "pch.h"
#include "DesktopFlyout.h"
#include "DesktopFlyout.g.cpp"
#include "DesktopFlyoutIsland.h"
#include "FlyoutHelpers.h"

namespace winrt::DesktopFlyouts::implementation
{
    using namespace winrt::Microsoft::UI::Xaml;
    using namespace winrt::Microsoft::UI::Xaml::Controls;
    using namespace winrt::Microsoft::UI::Xaml::Media;
    using namespace winrt::Microsoft::UI::Xaml::Media::Animation;

    namespace
    {
        PropertyMetadata Metadata(winrt::Windows::Foundation::IInspectable const& value = nullptr)
        {
            return PropertyMetadata{ value };
        }

        PropertyMetadata LayoutMetadata(winrt::Windows::Foundation::IInspectable const& value)
        {
            return PropertyMetadata{ value };
        }
    }

    DependencyProperty DesktopFlyout::s_islandsProperty = DependencyProperty::Register(
        L"Islands", winrt::xaml_typename<winrt::Windows::Foundation::Collections::IVector<winrt::DesktopFlyouts::DesktopFlyoutIsland>>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
        Metadata());
    DependencyProperty DesktopFlyout::s_islandsSourceProperty = DependencyProperty::Register(
        L"IslandsSource", winrt::xaml_typename<winrt::Windows::Foundation::IInspectable>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
        Metadata());
    DependencyProperty DesktopFlyout::s_isBackdropEnabledProperty = DependencyProperty::Register(
        L"IsBackdropEnabled", winrt::xaml_typename<bool>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
        Metadata(winrt::box_value(true)));
    DependencyProperty DesktopFlyout::s_isOpenProperty = DependencyProperty::Register(
        L"IsOpen", winrt::xaml_typename<bool>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(), Metadata(winrt::box_value(false)));
    DependencyProperty DesktopFlyout::s_flyoutWidthProperty = DependencyProperty::Register(
        L"FlyoutWidth", winrt::xaml_typename<GridLength>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
        LayoutMetadata(winrt::box_value(GridLengthHelper::Auto())));
    DependencyProperty DesktopFlyout::s_flyoutHeightProperty = DependencyProperty::Register(
        L"FlyoutHeight", winrt::xaml_typename<GridLength>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
        LayoutMetadata(winrt::box_value(GridLengthHelper::Auto())));
    DependencyProperty DesktopFlyout::s_popupDirectionProperty = DependencyProperty::Register(
        L"PopupDirection", winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutPopupDirection>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
        LayoutMetadata(winrt::box_value(winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::Vertical)));
    DependencyProperty DesktopFlyout::s_islandsOrientationProperty = DependencyProperty::Register(
        L"IslandsOrientation", winrt::xaml_typename<Orientation>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
        LayoutMetadata(winrt::box_value(Orientation::Vertical)));
    DependencyProperty DesktopFlyout::s_placementProperty = DependencyProperty::Register(
        L"Placement", winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutPlacementMode>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
        LayoutMetadata(winrt::box_value(winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::BottomRight)));
    DependencyProperty DesktopFlyout::s_menuFlyoutProperty = DependencyProperty::Register(
        L"MenuFlyout", winrt::xaml_typename<winrt::Microsoft::UI::Xaml::Controls::MenuFlyout>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(), Metadata());
    DependencyProperty DesktopFlyout::s_isTransitionAnimationEnabledProperty = DependencyProperty::Register(
        L"IsTransitionAnimationEnabled", winrt::xaml_typename<bool>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(), Metadata(winrt::box_value(true)));
    DependencyProperty DesktopFlyout::s_pressedScaleProperty = DependencyProperty::Register(
        L"PressedScale", winrt::xaml_typename<double>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(), Metadata(winrt::box_value(1.0)));
    DependencyProperty DesktopFlyout::s_isSwipeToDismissEnabledProperty = DependencyProperty::Register(
        L"IsSwipeToDismissEnabled", winrt::xaml_typename<bool>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(), Metadata(winrt::box_value(false)));
    DependencyProperty DesktopFlyout::s_swipeDismissThresholdProperty = DependencyProperty::Register(
        L"SwipeDismissThreshold", winrt::xaml_typename<double>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(), Metadata(winrt::box_value(80.0)));
    DependencyProperty DesktopFlyout::s_hideOnLostFocusProperty = DependencyProperty::Register(
        L"HideOnLostFocus", winrt::xaml_typename<bool>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(), Metadata(winrt::box_value(true)));
    DependencyProperty DesktopFlyout::s_activationModeProperty = DependencyProperty::Register(
        L"ActivationMode", winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutActivationMode>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
        Metadata(winrt::box_value(winrt::DesktopFlyouts::DesktopFlyoutActivationMode::Activate)));
    DependencyProperty DesktopFlyout::s_autoCloseDelayProperty = DependencyProperty::Register(
        L"AutoCloseDelay", winrt::xaml_typename<winrt::Windows::Foundation::TimeSpan>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
        Metadata(winrt::box_value(winrt::Windows::Foundation::TimeSpan{})));
    DependencyProperty DesktopFlyout::s_backdropKindProperty = DependencyProperty::Register(
        L"BackdropKind", winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyoutBackdropKind>(), winrt::xaml_typename<winrt::DesktopFlyouts::DesktopFlyout>(),
        Metadata(winrt::box_value(winrt::DesktopFlyouts::DesktopFlyoutBackdropKind::DesktopAcrylic)));

    DesktopFlyout::DesktopFlyout() :
        m_host(std::make_unique<winrt::DesktopFlyouts::details::XamlIslandHostWindow>())
    {
        DefaultStyleKey(winrt::box_value(L"DesktopFlyouts.DesktopFlyout"));
        auto islands = winrt::single_threaded_observable_vector<winrt::DesktopFlyouts::DesktopFlyoutIsland>();
        m_vectorChangedToken = islands.VectorChanged([this](auto const&, auto const&) { UpdateIslands(); });
        SetValue(s_islandsProperty, islands);
        RegisterPropertyCallbacks();
        m_host->WindowInactivated = [this]()
        {
            if (HideOnLostFocus())
            {
                Hide();
            }
        };
        m_host->SystemSettingsChanged = [this]()
        {
            UpdateFlyoutTheme();
            UpdateIslandBackdrops();
        };
        m_host->SetContent(*this);
        m_host->SetVisible(false);
    }

    DesktopFlyout::~DesktopFlyout()
    {
        UnregisterPropertyCallbacks();
        Close();
    }

    DependencyProperty DesktopFlyout::IslandsProperty() { return s_islandsProperty; }
    DependencyProperty DesktopFlyout::IslandsSourceProperty() { return s_islandsSourceProperty; }
    DependencyProperty DesktopFlyout::IsBackdropEnabledProperty() { return s_isBackdropEnabledProperty; }
    DependencyProperty DesktopFlyout::IsOpenProperty() { return s_isOpenProperty; }
    DependencyProperty DesktopFlyout::FlyoutWidthProperty() { return s_flyoutWidthProperty; }
    DependencyProperty DesktopFlyout::FlyoutHeightProperty() { return s_flyoutHeightProperty; }
    DependencyProperty DesktopFlyout::PopupDirectionProperty() { return s_popupDirectionProperty; }
    DependencyProperty DesktopFlyout::IslandsOrientationProperty() { return s_islandsOrientationProperty; }
    DependencyProperty DesktopFlyout::PlacementProperty() { return s_placementProperty; }
    DependencyProperty DesktopFlyout::MenuFlyoutProperty() { return s_menuFlyoutProperty; }
    DependencyProperty DesktopFlyout::IsTransitionAnimationEnabledProperty() { return s_isTransitionAnimationEnabledProperty; }
    DependencyProperty DesktopFlyout::PressedScaleProperty() { return s_pressedScaleProperty; }
    DependencyProperty DesktopFlyout::IsSwipeToDismissEnabledProperty() { return s_isSwipeToDismissEnabledProperty; }
    DependencyProperty DesktopFlyout::SwipeDismissThresholdProperty() { return s_swipeDismissThresholdProperty; }
    DependencyProperty DesktopFlyout::HideOnLostFocusProperty() { return s_hideOnLostFocusProperty; }
    DependencyProperty DesktopFlyout::ActivationModeProperty() { return s_activationModeProperty; }
    DependencyProperty DesktopFlyout::AutoCloseDelayProperty() { return s_autoCloseDelayProperty; }
    DependencyProperty DesktopFlyout::BackdropKindProperty() { return s_backdropKindProperty; }

    winrt::Windows::Foundation::Collections::IVector<winrt::DesktopFlyouts::DesktopFlyoutIsland> DesktopFlyout::Islands()
    {
        return GetValue(s_islandsProperty).as<winrt::Windows::Foundation::Collections::IVector<winrt::DesktopFlyouts::DesktopFlyoutIsland>>();
    }
    winrt::Windows::Foundation::IInspectable DesktopFlyout::IslandsSource() { return GetValue(s_islandsSourceProperty); }
    void DesktopFlyout::IslandsSource(winrt::Windows::Foundation::IInspectable const& value) { SetValue(s_islandsSourceProperty, value); }
    bool DesktopFlyout::IsBackdropEnabled() { return winrt::unbox_value<bool>(GetValue(s_isBackdropEnabledProperty)); }
    void DesktopFlyout::IsBackdropEnabled(bool value) { SetValue(s_isBackdropEnabledProperty, winrt::box_value(value)); }
    bool DesktopFlyout::IsOpen() { return winrt::unbox_value<bool>(GetValue(s_isOpenProperty)); }
    GridLength DesktopFlyout::FlyoutWidth() { return winrt::unbox_value<GridLength>(GetValue(s_flyoutWidthProperty)); }
    void DesktopFlyout::FlyoutWidth(GridLength const& value) { SetValue(s_flyoutWidthProperty, winrt::box_value(value)); }
    GridLength DesktopFlyout::FlyoutHeight() { return winrt::unbox_value<GridLength>(GetValue(s_flyoutHeightProperty)); }
    void DesktopFlyout::FlyoutHeight(GridLength const& value) { SetValue(s_flyoutHeightProperty, winrt::box_value(value)); }
    winrt::DesktopFlyouts::DesktopFlyoutPopupDirection DesktopFlyout::PopupDirection() { return winrt::unbox_value<winrt::DesktopFlyouts::DesktopFlyoutPopupDirection>(GetValue(s_popupDirectionProperty)); }
    void DesktopFlyout::PopupDirection(winrt::DesktopFlyouts::DesktopFlyoutPopupDirection value) { SetValue(s_popupDirectionProperty, winrt::box_value(value)); }
    Orientation DesktopFlyout::IslandsOrientation() { return winrt::unbox_value<Orientation>(GetValue(s_islandsOrientationProperty)); }
    void DesktopFlyout::IslandsOrientation(Orientation value) { SetValue(s_islandsOrientationProperty, winrt::box_value(value)); }
    winrt::DesktopFlyouts::DesktopFlyoutPlacementMode DesktopFlyout::Placement() { return winrt::unbox_value<winrt::DesktopFlyouts::DesktopFlyoutPlacementMode>(GetValue(s_placementProperty)); }
    void DesktopFlyout::Placement(winrt::DesktopFlyouts::DesktopFlyoutPlacementMode value) { SetValue(s_placementProperty, winrt::box_value(value)); }
    MenuFlyout DesktopFlyout::MenuFlyout() { return GetValue(s_menuFlyoutProperty).try_as<winrt::Microsoft::UI::Xaml::Controls::MenuFlyout>(); }
    void DesktopFlyout::MenuFlyout(winrt::Microsoft::UI::Xaml::Controls::MenuFlyout const& value) { SetValue(s_menuFlyoutProperty, value); }
    bool DesktopFlyout::IsTransitionAnimationEnabled() { return winrt::unbox_value<bool>(GetValue(s_isTransitionAnimationEnabledProperty)); }
    void DesktopFlyout::IsTransitionAnimationEnabled(bool value) { SetValue(s_isTransitionAnimationEnabledProperty, winrt::box_value(value)); }
    double DesktopFlyout::PressedScale() { return winrt::unbox_value<double>(GetValue(s_pressedScaleProperty)); }
    void DesktopFlyout::PressedScale(double value) { SetValue(s_pressedScaleProperty, winrt::box_value(value)); }
    bool DesktopFlyout::IsSwipeToDismissEnabled() { return winrt::unbox_value<bool>(GetValue(s_isSwipeToDismissEnabledProperty)); }
    void DesktopFlyout::IsSwipeToDismissEnabled(bool value) { SetValue(s_isSwipeToDismissEnabledProperty, winrt::box_value(value)); }
    double DesktopFlyout::SwipeDismissThreshold() { return winrt::unbox_value<double>(GetValue(s_swipeDismissThresholdProperty)); }
    void DesktopFlyout::SwipeDismissThreshold(double value) { SetValue(s_swipeDismissThresholdProperty, winrt::box_value(value)); }
    bool DesktopFlyout::HideOnLostFocus() { return winrt::unbox_value<bool>(GetValue(s_hideOnLostFocusProperty)); }
    void DesktopFlyout::HideOnLostFocus(bool value) { SetValue(s_hideOnLostFocusProperty, winrt::box_value(value)); }
    winrt::DesktopFlyouts::DesktopFlyoutActivationMode DesktopFlyout::ActivationMode() { return winrt::unbox_value<winrt::DesktopFlyouts::DesktopFlyoutActivationMode>(GetValue(s_activationModeProperty)); }
    void DesktopFlyout::ActivationMode(winrt::DesktopFlyouts::DesktopFlyoutActivationMode value) { SetValue(s_activationModeProperty, winrt::box_value(value)); }
    winrt::Windows::Foundation::TimeSpan DesktopFlyout::AutoCloseDelay() { return winrt::unbox_value<winrt::Windows::Foundation::TimeSpan>(GetValue(s_autoCloseDelayProperty)); }
    void DesktopFlyout::AutoCloseDelay(winrt::Windows::Foundation::TimeSpan const& value) { SetValue(s_autoCloseDelayProperty, winrt::box_value(value)); }
    winrt::DesktopFlyouts::DesktopFlyoutBackdropKind DesktopFlyout::BackdropKind() { return winrt::unbox_value<winrt::DesktopFlyouts::DesktopFlyoutBackdropKind>(GetValue(s_backdropKindProperty)); }
    void DesktopFlyout::BackdropKind(winrt::DesktopFlyouts::DesktopFlyoutBackdropKind value) { SetValue(s_backdropKindProperty, winrt::box_value(value)); }

    void DesktopFlyout::OnApplyTemplate()
    {
        DetachRootPointerHandlers();
        m_rootGrid = GetTemplateChild(L"PART_RootGrid").try_as<Grid>();
        m_islandsGrid = GetTemplateChild(L"PART_IslandsGrid").try_as<Grid>();
        if (!m_rootGrid || !m_islandsGrid)
        {
            throw winrt::hresult_error(E_FAIL, L"DesktopFlyout template must define PART_RootGrid and PART_IslandsGrid.");
        }
        if (!m_rootGrid.RenderTransform().try_as<CompositeTransform>())
        {
            m_rootGrid.RenderTransform(CompositeTransform{});
        }
        AttachRootPointerHandlers();
        UpdateIslands();
    }

    void DesktopFlyout::Show()
    {
        if (m_closed || m_transitioning || !m_host || !m_host->HasSource())
        {
            m_customPlacement.reset();
            return;
        }

        ApplyTemplate();
        if (!m_rootGrid)
        {
            return;
        }

        StopAutoCloseTimer();
        m_transitioning = true;
        auto activate = ActivationMode() == winrt::DesktopFlyouts::DesktopFlyoutActivationMode::Activate;
        if (!activate)
        {
            m_host->PreserveActivationState();
        }
        m_host->SetActivationMode(ActivationMode());
        auto workArea = winrt::DesktopFlyouts::details::GetFlyoutWorkAreaRect(m_customPlacement);
        m_host->Maximize(workArea, activate);

        ResetResolvedFlyoutSize();
        UpdateLayout();
        auto strongThis = get_strong();
        if (!m_rootGrid.DispatcherQueue().TryEnqueue([strongThis, activate]()
        {
            strongThis->ContinueOpenAfterInitialLayout(activate);
        }))
        {
            CancelPendingOpen(!activate);
        }
    }

    void DesktopFlyout::Show(winrt::Windows::Foundation::Point const& bottomCenterPoint)
    {
        if (!m_transitioning)
        {
            m_customPlacement = bottomCenterPoint;
            Show();
        }
    }

    void DesktopFlyout::Hide()
    {
        if (!m_closed && !m_transitioning && IsOpen())
        {
            StopAutoCloseTimer();
            StopSwipeDismissRestoreStoryboard();
            ResetSwipeDismissTracking();
            ResetPressedScale();
            StartCloseTransition();
        }
    }

    void DesktopFlyout::NavigateFocus(winrt::Microsoft::UI::Xaml::Hosting::XamlSourceFocusNavigationReason reason)
    {
        if (m_host)
        {
            m_host->NavigateFocus(reason);
        }
    }

    void DesktopFlyout::Close()
    {
        if (m_closed)
        {
            return;
        }
        m_closed = true;
        StopAutoCloseTimer();
        StopSwipeDismissRestoreStoryboard();
        StopPressedScaleStoryboard();
        DetachRootPointerHandlers();
        ResetSwipeDismissTracking();
        if (m_storyboard)
        {
            m_storyboard.Stop();
            m_storyboard = nullptr;
        }
        if (m_vectorChangedToken.value)
        {
            if (auto islands = GetValue(s_islandsProperty).try_as<winrt::Windows::Foundation::Collections::IObservableVector<winrt::DesktopFlyouts::DesktopFlyoutIsland>>())
            {
                islands.VectorChanged(m_vectorChangedToken);
            }
        }
        ClearIslandSubscriptions();
        if (m_host)
        {
            m_host->Close();
            m_host.reset();
        }
        SetValue(s_isOpenProperty, winrt::box_value(false));
    }

    void DesktopFlyout::OnIslandSizeChanged()
    {
        UpdateIslands();
        UpdateOpenFlyoutLayout();
    }

    SystemBackdrop DesktopFlyout::CreateIslandSystemBackdrop()
    {
        if (!IsBackdropEnabled())
        {
            return nullptr;
        }
        if (BackdropKind() == winrt::DesktopFlyouts::DesktopFlyoutBackdropKind::Mica)
        {
            return MicaBackdrop{};
        }
        return DesktopAcrylicBackdrop{};
    }

    void DesktopFlyout::RegisterPropertyCallbacks()
    {
        m_islandsSourceToken = RegisterPropertyChangedCallback(s_islandsSourceProperty, [this](auto const&, auto const&)
        {
            SynchronizeIslandsSource();
            UpdateIslands();
            UpdateOpenFlyoutLayout();
        });
        auto layoutChanged = [this](DependencyObject const&, DependencyProperty const&)
        {
            UpdateIslands();
            UpdateOpenFlyoutLayout();
        };
        m_flyoutWidthToken = RegisterPropertyChangedCallback(s_flyoutWidthProperty, layoutChanged);
        m_flyoutHeightToken = RegisterPropertyChangedCallback(s_flyoutHeightProperty, layoutChanged);
        m_popupDirectionToken = RegisterPropertyChangedCallback(s_popupDirectionProperty, layoutChanged);
        m_islandsOrientationToken = RegisterPropertyChangedCallback(s_islandsOrientationProperty, layoutChanged);
        m_placementToken = RegisterPropertyChangedCallback(s_placementProperty, layoutChanged);
        m_isBackdropEnabledToken = RegisterPropertyChangedCallback(s_isBackdropEnabledProperty, [this](auto const&, auto const&)
        {
            UpdateIslandBackdrops();
        });
        m_backdropKindToken = RegisterPropertyChangedCallback(s_backdropKindProperty, [this](auto const&, auto const&)
        {
            UpdateIslandBackdrops();
        });
        m_activationModeToken = RegisterPropertyChangedCallback(s_activationModeProperty, [this](auto const&, auto const&)
        {
            if (m_host)
            {
                m_host->SetActivationMode(ActivationMode());
            }
        });
        m_autoCloseDelayToken = RegisterPropertyChangedCallback(s_autoCloseDelayProperty, [this](auto const&, auto const&)
        {
            RestartAutoCloseTimer();
        });
    }

    void DesktopFlyout::UnregisterPropertyCallbacks()
    {
        auto unregister = [this](DependencyProperty const& property, int64_t& token)
        {
            if (token)
            {
                UnregisterPropertyChangedCallback(property, token);
                token = 0;
            }
        };
        unregister(s_islandsSourceProperty, m_islandsSourceToken);
        unregister(s_flyoutWidthProperty, m_flyoutWidthToken);
        unregister(s_flyoutHeightProperty, m_flyoutHeightToken);
        unregister(s_popupDirectionProperty, m_popupDirectionToken);
        unregister(s_islandsOrientationProperty, m_islandsOrientationToken);
        unregister(s_placementProperty, m_placementToken);
        unregister(s_isBackdropEnabledProperty, m_isBackdropEnabledToken);
        unregister(s_backdropKindProperty, m_backdropKindToken);
        unregister(s_activationModeProperty, m_activationModeToken);
        unregister(s_autoCloseDelayProperty, m_autoCloseDelayToken);
    }

    void DesktopFlyout::SynchronizeIslandsSource()
    {
        auto islands = Islands();
        auto source = IslandsSource().try_as<winrt::Windows::Foundation::Collections::IIterable<winrt::DesktopFlyouts::DesktopFlyoutIsland>>();
        if (!source)
        {
            return;
        }
        if (auto sourceVector = source.try_as<winrt::Windows::Foundation::Collections::IVector<winrt::DesktopFlyouts::DesktopFlyoutIsland>>();
            sourceVector && sourceVector == islands)
        {
            return;
        }

        islands.Clear();
        for (auto const& island : source)
        {
            islands.Append(island);
        }
    }

    void DesktopFlyout::ClearIslandSubscriptions()
    {
        for (auto const& subscription : m_islandSubscriptions)
        {
            if (subscription.WidthToken)
            {
                subscription.Island.UnregisterPropertyChangedCallback(
                    winrt::DesktopFlyouts::DesktopFlyoutIsland::IslandWidthProperty(), subscription.WidthToken);
            }
            if (subscription.HeightToken)
            {
                subscription.Island.UnregisterPropertyChangedCallback(
                    winrt::DesktopFlyouts::DesktopFlyoutIsland::IslandHeightProperty(), subscription.HeightToken);
            }
        }
        m_islandSubscriptions.clear();
    }

    void DesktopFlyout::UpdateIslands()
    {
        if (!m_islandsGrid)
        {
            return;
        }
        ClearIslandSubscriptions();
        m_islandsGrid.Children().Clear();
        m_islandsGrid.RowDefinitions().Clear();
        m_islandsGrid.ColumnDefinitions().Clear();
        uint32_t index = 0;
        for (auto const& island : Islands())
        {
            if (IslandsOrientation() == Orientation::Vertical)
            {
                RowDefinition row;
                row.Height(island.IslandHeight());
                m_islandsGrid.RowDefinitions().Append(row);
                Grid::SetRow(island, static_cast<int32_t>(index));
            }
            else
            {
                ColumnDefinition column;
                column.Width(island.IslandWidth());
                m_islandsGrid.ColumnDefinitions().Append(column);
                Grid::SetColumn(island, static_cast<int32_t>(index));
            }
            IslandSubscription subscription{ island };
            subscription.WidthToken = island.RegisterPropertyChangedCallback(
                winrt::DesktopFlyouts::DesktopFlyoutIsland::IslandWidthProperty(),
                [this](auto const&, auto const&) { OnIslandSizeChanged(); });
            subscription.HeightToken = island.RegisterPropertyChangedCallback(
                winrt::DesktopFlyouts::DesktopFlyoutIsland::IslandHeightProperty(),
                [this](auto const&, auto const&) { OnIslandSizeChanged(); });
            m_islandSubscriptions.push_back(subscription);
            m_islandsGrid.Children().Append(island);
            ++index;
        }
    }

    void DesktopFlyout::UpdateIslandBackdrops()
    {
        for (auto const& island : Islands())
        {
            island.TemplateSettings().SetValue(
                winrt::DesktopFlyouts::DesktopFlyoutIslandTemplateSettings::SystemBackdropProperty(),
                CreateIslandSystemBackdrop());
        }
    }

    void DesktopFlyout::UpdateFlyoutTheme()
    {
        auto theme = winrt::DesktopFlyouts::details::IsTaskbarLight() ? ElementTheme::Light : ElementTheme::Dark;
        for (auto const& island : Islands())
        {
            island.RequestedTheme(theme);
        }
    }

    void DesktopFlyout::UpdateOpenFlyoutLayout()
    {
        if (!IsOpen() || m_transitioning || !m_rootGrid)
        {
            return;
        }
        ResetResolvedFlyoutSize();
        UpdateLayout();
        ApplyResolvedFlyoutSize();
        UpdateLayout();
        m_activeDirection = UpdateFlyoutRegion();
        SetOpenTransform();
    }

    void DesktopFlyout::ResetResolvedFlyoutSize()
    {
        if (!m_rootGrid)
        {
            return;
        }
        m_rootGrid.Width(std::numeric_limits<double>::quiet_NaN());
        m_rootGrid.Height(std::numeric_limits<double>::quiet_NaN());
    }

    void DesktopFlyout::ApplyResolvedFlyoutSize()
    {
        if (!m_rootGrid || !m_host)
        {
            return;
        }
        auto workArea = winrt::DesktopFlyouts::details::GetFlyoutWorkAreaRect(m_customPlacement);
        auto scale = std::max(0.01, m_host->RasterizationScale());
        auto margin = Margin();
        auto availableWidth = std::max(0.0, (workArea.right - workArea.left) / scale - margin.Left - margin.Right);
        auto availableHeight = std::max(0.0, (workArea.bottom - workArea.top) / scale - margin.Top - margin.Bottom);
        m_rootGrid.Width(ResolveLength(FlyoutWidth(), availableWidth, HasStarIslandWidth()));
        m_rootGrid.Height(ResolveLength(FlyoutHeight(), availableHeight, HasStarIslandHeight()));
    }

    bool DesktopFlyout::HasStarIslandWidth()
    {
        for (auto const& island : Islands())
        {
            if (GridLengthHelper::GetIsStar(island.IslandWidth()))
            {
                return true;
            }
        }
        return false;
    }

    bool DesktopFlyout::HasStarIslandHeight()
    {
        for (auto const& island : Islands())
        {
            if (GridLengthHelper::GetIsStar(island.IslandHeight()))
            {
                return true;
            }
        }
        return false;
    }

    void DesktopFlyout::ContinueOpenAfterInitialLayout(bool activate)
    {
        if (!CanContinueOpening())
        {
            return;
        }

        ApplyResolvedFlyoutSize();
        UpdateLayout();
        auto strongThis = get_strong();
        if (!m_rootGrid.DispatcherQueue().TryEnqueue([strongThis, activate]()
        {
            strongThis->ContinueOpenAfterResolvedLayout(activate);
        }))
        {
            CancelPendingOpen(!activate);
        }
    }

    void DesktopFlyout::ContinueOpenAfterResolvedLayout(bool activate)
    {
        if (!CanContinueOpening())
        {
            return;
        }

        UpdateFlyoutTheme();
        UpdateIslandBackdrops();
        m_activeDirection = UpdateFlyoutRegion();
        SetClosedTransform(m_activeDirection);
        UpdateLayout();
        auto strongThis = get_strong();
        if (!m_rootGrid.DispatcherQueue().TryEnqueue([strongThis, activate]()
        {
            strongThis->PresentOpenFlyout(activate);
        }))
        {
            CancelPendingOpen(!activate);
        }
    }

    void DesktopFlyout::PresentOpenFlyout(bool activate)
    {
        if (!CanContinueOpening())
        {
            return;
        }

        m_host->SetVisible(true, activate);
        if (!activate)
        {
            m_host->RestoreActivationState();
        }
        StartOpenTransition();
    }

    bool DesktopFlyout::CanContinueOpening()
    {
        return !m_closed && m_transitioning && m_host && m_host->HasSource() && m_rootGrid;
    }

    void DesktopFlyout::CancelPendingOpen(bool restoreActivation)
    {
        if (restoreActivation && m_host)
        {
            m_host->RestoreActivationState();
        }
        m_transitioning = false;
        m_customPlacement.reset();
    }

    winrt::DesktopFlyouts::DesktopFlyoutPopupDirection DesktopFlyout::UpdateFlyoutRegion()
    {
        auto work = winrt::DesktopFlyouts::details::GetFlyoutWorkAreaRect(m_customPlacement);
        auto scale = m_host ? m_host->RasterizationScale() : 1.0;
        auto margin = Margin();
        auto width = std::max(1, static_cast<int>(std::ceil((m_rootGrid.ActualWidth() + margin.Left + margin.Right) * scale)));
        auto height = std::max(1, static_cast<int>(std::ceil((m_rootGrid.ActualHeight() + margin.Top + margin.Bottom) * scale)));
        auto workWidth = work.right - work.left;
        auto workHeight = work.bottom - work.top;
        width = std::min(width, static_cast<int>(workWidth));
        height = std::min(height, static_cast<int>(workHeight));

        double left = work.right - width;
        double top = work.bottom - height;
        if (m_customPlacement)
        {
            winrt::DesktopFlyouts::details::TaskbarEdge edge;
            if (winrt::DesktopFlyouts::details::TryGetTaskbarEdgeForPoint(*m_customPlacement, edge))
            {
                switch (edge)
                {
                case winrt::DesktopFlyouts::details::TaskbarEdge::Left:
                    left = work.left;
                    top = m_customPlacement->Y - height / 2.0;
                    break;
                case winrt::DesktopFlyouts::details::TaskbarEdge::Top:
                    left = m_customPlacement->X - width / 2.0;
                    top = work.top;
                    break;
                case winrt::DesktopFlyouts::details::TaskbarEdge::Right:
                    left = work.right - width;
                    top = m_customPlacement->Y - height / 2.0;
                    break;
                default:
                    left = m_customPlacement->X - width / 2.0;
                    top = work.bottom - height;
                    break;
                }
            }
            else
            {
                left = m_customPlacement->X - width / 2.0;
                top = m_customPlacement->Y - height;
            }
        }
        else
        {
            switch (Placement())
            {
            case winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::TopCenter:
                left = work.left + (workWidth - width) / 2.0;
                top = work.top;
                break;
            case winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::TopLeft:
                left = work.left;
                top = work.top;
                break;
            case winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::TopRight:
                top = work.top;
                break;
            case winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::BottomCenter:
                left = work.left + (workWidth - width) / 2.0;
                break;
            case winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::BottomLeft:
                left = work.left;
                break;
            case winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::LeftCenter:
                left = work.left;
                top = work.top + (workHeight - height) / 2.0;
                break;
            case winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::RightCenter:
                top = work.top + (workHeight - height) / 2.0;
                break;
            default:
                break;
            }
        }
        left = Clamp(left, work.left, work.right - width);
        top = Clamp(top, work.top, work.bottom - height);
        winrt::Windows::Graphics::RectInt32 region{ static_cast<int>(std::round(left)), static_cast<int>(std::round(top)), width, height };
        m_host->MoveAndResize(region, ActivationMode() == winrt::DesktopFlyouts::DesktopFlyoutActivationMode::Activate);
        m_host->SetRectRegion({ 0, 0, width, height });
        m_customPlacement.reset();

        auto requested = PopupDirection();
        if (requested == winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::Vertical)
        {
            return region.Y + region.Height / 2.0 >= work.top + workHeight / 2.0
                ? winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::BottomToTop
                : winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::TopToBottom;
        }
        if (requested == winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::Horizontal)
        {
            return region.X + region.Width / 2.0 >= work.left + workWidth / 2.0
                ? winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::RightToLeft
                : winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::LeftToRight;
        }
        return requested;
    }

    void DesktopFlyout::StartOpenTransition()
    {
        m_transitioning = true;
        if (!IsTransitionAnimationEnabled())
        {
            CompleteOpen();
            return;
        }
        auto transform = RootTransform();
        auto vertical = m_activeDirection == winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::BottomToTop ||
            m_activeDirection == winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::TopToBottom;
        auto property = vertical ? L"TranslateY" : L"TranslateX";
        auto from = vertical ? static_cast<double>(ClosedYOffset(m_activeDirection)) : static_cast<double>(ClosedXOffset(m_activeDirection));
        m_storyboard = winrt::DesktopFlyouts::details::CreateTranslationStoryboard(transform, property, from, 0.0, 200);
        m_storyboardToken = m_storyboard.Completed([this](auto const&, auto const&) { CompleteOpen(); });
        m_storyboard.Begin();
    }

    void DesktopFlyout::StartCloseTransition()
    {
        m_transitioning = true;
        if (!IsTransitionAnimationEnabled())
        {
            CompleteClose();
            return;
        }
        auto transform = RootTransform();
        auto vertical = m_activeDirection == winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::BottomToTop ||
            m_activeDirection == winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::TopToBottom;
        auto property = vertical ? L"TranslateY" : L"TranslateX";
        auto from = vertical ? transform.TranslateY() : transform.TranslateX();
        auto to = vertical ? static_cast<double>(ClosedYOffset(m_activeDirection)) : static_cast<double>(ClosedXOffset(m_activeDirection));
        m_storyboard = winrt::DesktopFlyouts::details::CreateTranslationStoryboard(transform, property, from, to, 180);
        m_storyboardToken = m_storyboard.Completed([this](auto const&, auto const&) { CompleteClose(); });
        m_storyboard.Begin();
    }

    void DesktopFlyout::CompleteOpen()
    {
        if (m_storyboard)
        {
            m_storyboard.Completed(m_storyboardToken);
            m_storyboard.Stop();
            m_storyboard = nullptr;
        }
        SetOpenTransform();
        m_transitioning = false;
        SetValue(s_isOpenProperty, winrt::box_value(true));
        RestartAutoCloseTimer();
    }

    void DesktopFlyout::CompleteClose()
    {
        if (m_storyboard)
        {
            m_storyboard.Completed(m_storyboardToken);
            m_storyboard.Stop();
            m_storyboard = nullptr;
        }
        SetClosedTransform(m_activeDirection);
        ResetPressedScale();
        ResetSwipeDismissTracking();
        m_transitioning = false;
        SetValue(s_isOpenProperty, winrt::box_value(false));
        if (m_host)
        {
            m_host->SetVisible(false);
        }
    }

    void DesktopFlyout::SetOpenTransform()
    {
        if (auto transform = RootTransform())
        {
            transform.TranslateX(0.0);
            transform.TranslateY(0.0);
        }
    }

    void DesktopFlyout::SetClosedTransform(winrt::DesktopFlyouts::DesktopFlyoutPopupDirection value)
    {
        if (auto transform = RootTransform())
        {
            transform.TranslateX(ClosedXOffset(value));
            transform.TranslateY(ClosedYOffset(value));
        }
    }

    CompositeTransform DesktopFlyout::RootTransform()
    {
        if (!m_rootGrid)
        {
            return nullptr;
        }
        if (auto transform = m_rootGrid.RenderTransform().try_as<CompositeTransform>())
        {
            return transform;
        }
        CompositeTransform transform;
        transform.ScaleX(1.0);
        transform.ScaleY(1.0);
        m_rootGrid.RenderTransform(transform);
        return transform;
    }

    int DesktopFlyout::ClosedXOffset(winrt::DesktopFlyouts::DesktopFlyoutPopupDirection value)
    {
        if (!m_rootGrid)
        {
            return 0;
        }
        auto margin = Margin();
        if (value == winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::LeftToRight)
        {
            return -static_cast<int>(std::ceil(m_rootGrid.ActualWidth() + margin.Left));
        }
        if (value == winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::RightToLeft)
        {
            return static_cast<int>(std::ceil(m_rootGrid.ActualWidth() + margin.Right));
        }
        return 0;
    }

    int DesktopFlyout::ClosedYOffset(winrt::DesktopFlyouts::DesktopFlyoutPopupDirection value)
    {
        if (!m_rootGrid)
        {
            return 0;
        }
        auto margin = Margin();
        if (value == winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::TopToBottom)
        {
            return -static_cast<int>(std::ceil(m_rootGrid.ActualHeight() + margin.Top));
        }
        if (value == winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::BottomToTop)
        {
            return static_cast<int>(std::ceil(m_rootGrid.ActualHeight() + margin.Bottom));
        }
        return 0;
    }

    void DesktopFlyout::AttachRootPointerHandlers()
    {
        if (!m_rootGrid || m_pointerHandlersAttached)
        {
            return;
        }
        m_pointerPressedToken = m_rootGrid.PointerPressed({ this, &DesktopFlyout::OnRootPointerPressed });
        m_pointerMovedToken = m_rootGrid.PointerMoved({ this, &DesktopFlyout::OnRootPointerMoved });
        m_pointerReleasedToken = m_rootGrid.PointerReleased({ this, &DesktopFlyout::OnRootPointerReleased });
        m_pointerCanceledToken = m_rootGrid.PointerCanceled({ this, &DesktopFlyout::OnRootPointerCanceled });
        m_pointerCaptureLostToken = m_rootGrid.PointerCaptureLost({ this, &DesktopFlyout::OnRootPointerCaptureLost });
        m_pointerExitedToken = m_rootGrid.PointerExited({ this, &DesktopFlyout::OnRootPointerExited });
        m_pointerHandlersAttached = true;
    }

    void DesktopFlyout::DetachRootPointerHandlers()
    {
        if (!m_rootGrid || !m_pointerHandlersAttached)
        {
            return;
        }
        m_rootGrid.PointerPressed(m_pointerPressedToken);
        m_rootGrid.PointerMoved(m_pointerMovedToken);
        m_rootGrid.PointerReleased(m_pointerReleasedToken);
        m_rootGrid.PointerCanceled(m_pointerCanceledToken);
        m_rootGrid.PointerCaptureLost(m_pointerCaptureLostToken);
        m_rootGrid.PointerExited(m_pointerExitedToken);
        m_pointerHandlersAttached = false;
    }

    void DesktopFlyout::OnRootPointerPressed(
        winrt::Windows::Foundation::IInspectable const&,
        winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const& args)
    {
        if (!m_rootGrid || m_transitioning)
        {
            return;
        }
        auto pressedScale = GetResolvedPressedScale();
        auto useScale = std::abs(pressedScale - 1.0) > 0.001;
        auto useSwipe = CanStartSwipeDismiss();
        if (!useScale && !useSwipe)
        {
            return;
        }

        StopSwipeDismissRestoreStoryboard();
        m_rootGrid.CapturePointer(args.Pointer());
        if (useSwipe)
        {
            StartSwipeDismiss(args);
        }
        if (useScale)
        {
            m_isPressAnimationActive = true;
            AnimatePressedScale(pressedScale, 110);
        }
    }

    void DesktopFlyout::OnRootPointerMoved(
        winrt::Windows::Foundation::IInspectable const&,
        winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const& args)
    {
        if (UpdateSwipeDismiss(args))
        {
            args.Handled(true);
        }
    }

    void DesktopFlyout::OnRootPointerReleased(
        winrt::Windows::Foundation::IInspectable const&,
        winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const& args)
    {
        auto dismissed = CompleteSwipeDismiss(args);
        if (m_rootGrid)
        {
            m_rootGrid.ReleasePointerCapture(args.Pointer());
        }
        if (!dismissed)
        {
            RestorePressedScale();
        }
    }

    void DesktopFlyout::OnRootPointerCanceled(
        winrt::Windows::Foundation::IInspectable const&,
        winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const&)
    {
        CancelSwipeDismiss();
        RestorePressedScale();
    }

    void DesktopFlyout::OnRootPointerCaptureLost(
        winrt::Windows::Foundation::IInspectable const&,
        winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const&)
    {
        CancelSwipeDismiss();
        RestorePressedScale();
    }

    void DesktopFlyout::OnRootPointerExited(
        winrt::Windows::Foundation::IInspectable const&,
        winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const&)
    {
        if (!m_isSwipeDismissDragging)
        {
            RestorePressedScale();
        }
    }

    bool DesktopFlyout::CanStartSwipeDismiss()
    {
        return IsSwipeToDismissEnabled() && IsOpen() && m_rootGrid && GetSwipeDismissMaxDistance() > 0.0;
    }

    void DesktopFlyout::StartSwipeDismiss(winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const& args)
    {
        m_swipeDismissPointerId = args.Pointer().PointerId();
        m_swipeDismissStartPoint = args.GetCurrentPoint(m_rootGrid).Position();
        m_isSwipeDismissTracking = true;
        m_isSwipeDismissDragging = false;
    }

    bool DesktopFlyout::UpdateSwipeDismiss(winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const& args)
    {
        if (!m_isSwipeDismissTracking || !m_rootGrid || args.Pointer().PointerId() != m_swipeDismissPointerId)
        {
            return false;
        }

        auto position = args.GetCurrentPoint(m_rootGrid).Position();
        double translateX{};
        double translateY{};
        if (!TryGetSwipeDismissTranslation(
                position.X - m_swipeDismissStartPoint.X,
                position.Y - m_swipeDismissStartPoint.Y,
                translateX,
                translateY))
        {
            return false;
        }
        if (!m_isSwipeDismissDragging)
        {
            m_isSwipeDismissDragging = true;
            StopAutoCloseTimer();
            RestorePressedScale();
        }
        if (auto transform = RootTransform())
        {
            transform.TranslateX(translateX);
            transform.TranslateY(translateY);
            return true;
        }
        return false;
    }

    bool DesktopFlyout::CompleteSwipeDismiss(winrt::Microsoft::UI::Xaml::Input::PointerRoutedEventArgs const& args)
    {
        if (!m_isSwipeDismissTracking || args.Pointer().PointerId() != m_swipeDismissPointerId)
        {
            return false;
        }
        auto dismiss = m_isSwipeDismissDragging && GetSwipeDismissDistance() >= GetResolvedSwipeDismissThreshold();
        auto restore = m_isSwipeDismissDragging && !dismiss;
        ResetSwipeDismissTracking();
        if (dismiss)
        {
            ResetPressedScale();
            Hide();
            return true;
        }
        if (restore)
        {
            AnimateSwipeDismissRestore();
        }
        return false;
    }

    void DesktopFlyout::CancelSwipeDismiss()
    {
        auto restore = m_isSwipeDismissDragging;
        ResetSwipeDismissTracking();
        if (restore)
        {
            AnimateSwipeDismissRestore();
        }
    }

    void DesktopFlyout::ResetSwipeDismissTracking()
    {
        m_isSwipeDismissTracking = false;
        m_isSwipeDismissDragging = false;
        m_swipeDismissPointerId = 0;
    }

    bool DesktopFlyout::TryGetSwipeDismissTranslation(double deltaX, double deltaY, double& translateX, double& translateY)
    {
        constexpr double dragStartThreshold = 4.0;
        constexpr double axisDominanceRatio = 1.2;
        translateX = 0.0;
        translateY = 0.0;

        auto closedX = ClosedXOffset(m_activeDirection);
        if (closedX != 0)
        {
            auto sign = closedX < 0 ? -1.0 : 1.0;
            auto distance = std::max(0.0, sign * deltaX);
            if (!m_isSwipeDismissDragging &&
                (distance < dragStartThreshold || distance < std::abs(deltaY) * axisDominanceRatio))
            {
                return false;
            }
            translateX = sign * std::min(distance, std::abs(static_cast<double>(closedX)));
            return true;
        }

        auto closedY = ClosedYOffset(m_activeDirection);
        if (closedY == 0)
        {
            return false;
        }
        auto sign = closedY < 0 ? -1.0 : 1.0;
        auto distance = std::max(0.0, sign * deltaY);
        if (!m_isSwipeDismissDragging &&
            (distance < dragStartThreshold || distance < std::abs(deltaX) * axisDominanceRatio))
        {
            return false;
        }
        translateY = sign * std::min(distance, std::abs(static_cast<double>(closedY)));
        return true;
    }

    double DesktopFlyout::GetSwipeDismissDistance()
    {
        auto transform = RootTransform();
        if (!transform)
        {
            return 0.0;
        }
        auto closedX = ClosedXOffset(m_activeDirection);
        if (closedX != 0)
        {
            return std::max(0.0, (closedX < 0 ? -1.0 : 1.0) * transform.TranslateX());
        }
        auto closedY = ClosedYOffset(m_activeDirection);
        return closedY == 0
            ? 0.0
            : std::max(0.0, (closedY < 0 ? -1.0 : 1.0) * transform.TranslateY());
    }

    double DesktopFlyout::GetSwipeDismissMaxDistance()
    {
        auto closedX = ClosedXOffset(m_activeDirection);
        return closedX != 0
            ? std::abs(static_cast<double>(closedX))
            : std::abs(static_cast<double>(ClosedYOffset(m_activeDirection)));
    }

    double DesktopFlyout::GetResolvedSwipeDismissThreshold()
    {
        auto maximum = std::max(1.0, GetSwipeDismissMaxDistance());
        auto threshold = SwipeDismissThreshold();
        if (!std::isfinite(threshold))
        {
            return std::min(80.0, maximum);
        }
        return Clamp(threshold, 1.0, maximum);
    }

    void DesktopFlyout::AnimateSwipeDismissRestore()
    {
        StopSwipeDismissRestoreStoryboard();
        auto transform = RootTransform();
        if (!transform)
        {
            return;
        }
        if (!IsTransitionAnimationEnabled())
        {
            SetOpenTransform();
            RestartAutoCloseTimer();
            return;
        }
        auto vertical = ClosedYOffset(m_activeDirection) != 0;
        auto property = vertical ? L"TranslateY" : L"TranslateX";
        auto from = vertical ? transform.TranslateY() : transform.TranslateX();
        m_swipeRestoreStoryboard = winrt::DesktopFlyouts::details::CreateTranslationStoryboard(transform, property, from, 0.0, 180);
        m_swipeRestoreStoryboardToken = m_swipeRestoreStoryboard.Completed([this](auto const&, auto const&)
        {
            StopSwipeDismissRestoreStoryboard();
            SetOpenTransform();
            RestartAutoCloseTimer();
        });
        m_swipeRestoreStoryboard.Begin();
    }

    void DesktopFlyout::StopSwipeDismissRestoreStoryboard()
    {
        if (m_swipeRestoreStoryboard)
        {
            m_swipeRestoreStoryboard.Completed(m_swipeRestoreStoryboardToken);
            m_swipeRestoreStoryboard.Stop();
            m_swipeRestoreStoryboard = nullptr;
        }
    }

    void DesktopFlyout::RestorePressedScale()
    {
        if (!m_isPressAnimationActive)
        {
            return;
        }
        m_isPressAnimationActive = false;
        AnimatePressedScale(1.0, 240);
    }

    void DesktopFlyout::ResetPressedScale()
    {
        m_isPressAnimationActive = false;
        StopPressedScaleStoryboard();
        if (auto transform = RootTransform())
        {
            transform.ScaleX(1.0);
            transform.ScaleY(1.0);
        }
    }

    void DesktopFlyout::AnimatePressedScale(double scale, int milliseconds)
    {
        auto transform = RootTransform();
        if (!transform)
        {
            return;
        }
        StopPressedScaleStoryboard();
        m_pressScaleStoryboard = winrt::DesktopFlyouts::details::CreateScaleStoryboard(
            transform, transform.ScaleX(), transform.ScaleY(), scale, milliseconds);
        m_pressScaleStoryboardToken = m_pressScaleStoryboard.Completed([this, scale](auto const&, auto const&)
        {
            StopPressedScaleStoryboard();
            if (auto transform = RootTransform())
            {
                transform.ScaleX(scale);
                transform.ScaleY(scale);
            }
        });
        m_pressScaleStoryboard.Begin();
    }

    void DesktopFlyout::StopPressedScaleStoryboard()
    {
        if (m_pressScaleStoryboard)
        {
            m_pressScaleStoryboard.Completed(m_pressScaleStoryboardToken);
            m_pressScaleStoryboard.Stop();
            m_pressScaleStoryboard = nullptr;
        }
    }

    double DesktopFlyout::GetResolvedPressedScale()
    {
        auto scale = PressedScale();
        return std::isfinite(scale) ? Clamp(scale, 0.1, 2.0) : 1.0;
    }

    void DesktopFlyout::RestartAutoCloseTimer()
    {
        StopAutoCloseTimer();
        auto delay = AutoCloseDelay();
        if (!IsOpen() || delay.count() <= 0)
        {
            return;
        }
        m_autoCloseTimer = DispatcherTimer{};
        m_autoCloseTimer.Interval(delay);
        m_autoCloseToken = m_autoCloseTimer.Tick([this](auto const&, auto const&)
        {
            StopAutoCloseTimer();
            Hide();
        });
        m_autoCloseTimer.Start();
    }

    void DesktopFlyout::StopAutoCloseTimer()
    {
        if (m_autoCloseTimer)
        {
            m_autoCloseTimer.Stop();
            m_autoCloseTimer.Tick(m_autoCloseToken);
            m_autoCloseTimer = nullptr;
        }
    }

    double DesktopFlyout::ResolveLength(GridLength const& length, double available, bool stretchWhenAuto)
    {
        if (GridLengthHelper::GetIsAuto(length))
        {
            return stretchWhenAuto ? available : std::numeric_limits<double>::quiet_NaN();
        }
        if (GridLengthHelper::GetIsStar(length))
        {
            return available;
        }
        return Clamp(length.Value, 0.0, available);
    }

    double DesktopFlyout::Clamp(double value, double minimum, double maximum)
    {
        return std::min(std::max(value, minimum), std::max(minimum, maximum));
    }
}
