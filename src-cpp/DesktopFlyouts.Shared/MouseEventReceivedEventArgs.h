#pragma once

#include "MouseEventReceivedEventArgs.g.h"

namespace winrt::DesktopFlyouts::implementation
{
    struct MouseEventReceivedEventArgs : MouseEventReceivedEventArgsT<MouseEventReceivedEventArgs>
    {
        explicit MouseEventReceivedEventArgs(winrt::Windows::Foundation::Point const& point);

        winrt::Windows::Foundation::Point Point() const noexcept;

    private:
        winrt::Windows::Foundation::Point m_point{};
    };
}

namespace winrt::DesktopFlyouts::factory_implementation
{
    struct MouseEventReceivedEventArgs : MouseEventReceivedEventArgsT<MouseEventReceivedEventArgs, implementation::MouseEventReceivedEventArgs>
    {
    };
}
