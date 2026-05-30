#pragma once
#include "pch.h"
#include "DesktopMenuFlyout.h"
#include "DesktopMenuFlyout.g.cpp"
#include "FlyoutHelpers.h"

namespace winrt::DesktopFlyouts::implementation
{
    using namespace winrt::Microsoft::UI::Xaml;
    using namespace winrt::Microsoft::UI::Xaml::Controls;

    DependencyProperty DesktopMenuFlyout::s_isOpenProperty = DependencyProperty::Register(
        L"IsOpen",
        winrt::xaml_typename<bool>(),
        winrt::xaml_typename<winrt::DesktopFlyouts::DesktopMenuFlyout>(),
        PropertyMetadata{ winrt::box_value(false) });

    DesktopMenuFlyout::DesktopMenuFlyout() :
        m_host(std::make_unique<winrt::DesktopFlyouts::details::XamlIslandHostWindow>()),
        m_menu(MenuFlyout{})
    {
        DefaultStyleKey(winrt::box_value(L"DesktopFlyouts.DesktopMenuFlyout"));

        m_closedToken = m_menu.Closed([this](auto const&, auto const&)
        {
            if (m_host)
            {
                m_host->SetVisible(false);
            }
            SetValue(s_isOpenProperty, winrt::box_value(false));
        });
        m_host->SystemSettingsChanged = [this]() { UpdateTheme(); };
        m_host->SetContent(*this);
        m_host->SetVisible(false);
    }

    void DesktopMenuFlyout::OnApplyTemplate()
    {
        m_target = GetTemplateChild(L"PART_MenuFlyoutTargetControl").try_as<Border>();
        if (!m_target)
        {
            throw winrt::hresult_error(E_FAIL, L"DesktopMenuFlyout template must define PART_MenuFlyoutTargetControl.");
        }
    }

    DesktopMenuFlyout::~DesktopMenuFlyout()
    {
        Close();
    }

    DependencyProperty DesktopMenuFlyout::IsOpenProperty()
    {
        return s_isOpenProperty;
    }

    bool DesktopMenuFlyout::IsOpen()
    {
        return winrt::unbox_value<bool>(GetValue(s_isOpenProperty));
    }

    void DesktopMenuFlyout::Show(winrt::Windows::Foundation::Point const& point)
    {
        if (m_closed || !m_host)
        {
            return;
        }
        if (Items().Size() == 0)
        {
            m_menu.Items().Clear();
            return;
        }
        ApplyTemplate();
        if (!m_target)
        {
            return;
        }

        RebuildMenu();
        UpdateTheme();
        m_host->MoveAndResize({ static_cast<int32_t>(std::round(point.X)), static_cast<int32_t>(std::round(point.Y)), 1, 1 });
        m_host->SetRectRegion({ 0, 0, 1, 1 });
        m_host->SetVisible(true);
        m_menu.ShowAt(m_target);
        SetValue(s_isOpenProperty, winrt::box_value(true));
    }

    void DesktopMenuFlyout::Hide()
    {
        if (m_closed)
        {
            return;
        }
        m_menu.Hide();
        if (m_host)
        {
            m_host->SetVisible(false);
        }
        SetValue(s_isOpenProperty, winrt::box_value(false));
    }

    void DesktopMenuFlyout::Close()
    {
        if (m_closed)
        {
            return;
        }
        m_closed = true;
        if (m_menu)
        {
            m_menu.Closed(m_closedToken);
            m_menu.Hide();
            m_menu.Items().Clear();
            m_menu = nullptr;
        }
        if (m_host)
        {
            m_host->Close();
            m_host.reset();
        }
        SetValue(s_isOpenProperty, winrt::box_value(false));
    }

    void DesktopMenuFlyout::RebuildMenu()
    {
        m_menu.Items().Clear();
        for (auto const& item : Items())
        {
            m_menu.Items().Append(item.as<MenuFlyoutItemBase>());
        }
    }

    void DesktopMenuFlyout::UpdateTheme()
    {
        RequestedTheme(winrt::DesktopFlyouts::details::IsTaskbarLight() ? ElementTheme::Light : ElementTheme::Dark);
    }
}
