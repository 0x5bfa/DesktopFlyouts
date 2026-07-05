#pragma once

namespace winrt::DesktopFlyouts::details
{
    enum class TaskbarEdge
    {
        Left,
        Top,
        Right,
        Bottom,
    };

    RECT GetFlyoutWorkAreaRect(std::optional<winrt::Windows::Foundation::Point> const& anchor = std::nullopt) noexcept;
    bool TryGetTaskbarEdgeForPoint(winrt::Windows::Foundation::Point const& point, TaskbarEdge& edge) noexcept;
    bool IsTaskbarLight() noexcept;

    winrt::Microsoft::UI::Xaml::Media::Animation::Storyboard CreateTranslationStoryboard(
        winrt::Microsoft::UI::Xaml::DependencyObject const& target,
        wchar_t const* property,
        double from,
        double to,
        int milliseconds);

    winrt::Microsoft::UI::Xaml::Media::Animation::Storyboard CreateScaleStoryboard(
        winrt::Microsoft::UI::Xaml::DependencyObject const& target,
        double fromX,
        double fromY,
        double to,
        int milliseconds);
}
