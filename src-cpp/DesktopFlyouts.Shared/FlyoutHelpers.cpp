#include "pch.h"
#include "FlyoutHelpers.h"

namespace winrt::DesktopFlyouts::details
{
    namespace
    {
        bool TryGetTaskbarInfo(RECT& rect, TaskbarEdge& edge) noexcept
        {
            APPBARDATA data{};
            data.cbSize = sizeof(data);
            if (SHAppBarMessage(ABM_GETTASKBARPOS, &data) == 0)
            {
                return false;
            }

            rect = data.rc;
            switch (data.uEdge)
            {
            case ABE_LEFT:
                edge = TaskbarEdge::Left;
                break;
            case ABE_TOP:
                edge = TaskbarEdge::Top;
                break;
            case ABE_RIGHT:
                edge = TaskbarEdge::Right;
                break;
            default:
                edge = TaskbarEdge::Bottom;
                break;
            }

            return true;
        }
    }

    RECT GetFlyoutWorkAreaRect(std::optional<winrt::Windows::Foundation::Point> const& anchor) noexcept
    {
        if (anchor)
        {
            POINT point{ static_cast<LONG>(anchor->X), static_cast<LONG>(anchor->Y) };
            MONITORINFO monitorInfo{ sizeof(monitorInfo) };
            auto monitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
            if (monitor && GetMonitorInfoW(monitor, &monitorInfo))
            {
                return monitorInfo.rcWork;
            }
        }

        RECT rect{};
        SystemParametersInfoW(SPI_GETWORKAREA, 0, &rect, 0);
        return rect;
    }

    bool TryGetTaskbarEdgeForPoint(winrt::Windows::Foundation::Point const& point, TaskbarEdge& edge) noexcept
    {
        RECT rect{};
        if (!TryGetTaskbarInfo(rect, edge))
        {
            return false;
        }

        constexpr LONG tolerance = 8;
        InflateRect(&rect, tolerance, tolerance);
        POINT nativePoint{ static_cast<LONG>(point.X), static_cast<LONG>(point.Y) };
        return PtInRect(&rect, nativePoint) != FALSE;
    }

    bool IsTaskbarLight() noexcept
    {
        DWORD value{};
        DWORD size = sizeof(value);
        auto status = RegGetValueW(
            HKEY_CURRENT_USER,
            L"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
            L"SystemUsesLightTheme",
            RRF_RT_REG_DWORD,
            nullptr,
            &value,
            &size);
        return status == ERROR_SUCCESS && value != 0;
    }

    winrt::Microsoft::UI::Xaml::Media::Animation::Storyboard CreateTranslationStoryboard(
        winrt::Microsoft::UI::Xaml::DependencyObject const& target,
        wchar_t const* property,
        double from,
        double to,
        int milliseconds)
    {
        using namespace winrt::Microsoft::UI::Xaml::Media::Animation;

        DoubleAnimation animation;
        animation.From(from);
        animation.To(to);
        animation.Duration(winrt::Microsoft::UI::Xaml::DurationHelper::FromTimeSpan(std::chrono::milliseconds(milliseconds)));
        animation.EnableDependentAnimation(true);
        Storyboard::SetTarget(animation, target);
        Storyboard::SetTargetProperty(animation, property);

        Storyboard storyboard;
        storyboard.Children().Append(animation);
        return storyboard;
    }

    winrt::Microsoft::UI::Xaml::Media::Animation::Storyboard CreateScaleStoryboard(
        winrt::Microsoft::UI::Xaml::DependencyObject const& target,
        double fromX,
        double fromY,
        double to,
        int milliseconds)
    {
        using namespace winrt::Microsoft::UI::Xaml::Media::Animation;

        Storyboard storyboard;
        auto append = [&](wchar_t const* property, double from)
        {
            DoubleAnimation animation;
            animation.From(from);
            animation.To(to);
            animation.Duration(winrt::Microsoft::UI::Xaml::DurationHelper::FromTimeSpan(std::chrono::milliseconds(milliseconds)));
            animation.EnableDependentAnimation(true);
            Storyboard::SetTarget(animation, target);
            Storyboard::SetTargetProperty(animation, property);
            storyboard.Children().Append(animation);
        };
        append(L"ScaleX", fromX);
        append(L"ScaleY", fromY);
        return storyboard;
    }
}
