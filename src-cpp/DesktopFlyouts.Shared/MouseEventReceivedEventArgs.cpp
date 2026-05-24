#include "pch.h"
#include "MouseEventReceivedEventArgs.h"
#if __has_include("Libraries.MouseEventReceivedEventArgs.g.cpp")
#include "Libraries.MouseEventReceivedEventArgs.g.cpp"
#endif

namespace winrt::DesktopFlyouts::implementation
{
    MouseEventReceivedEventArgs::MouseEventReceivedEventArgs(winrt::Windows::Foundation::Point const& point) :
        m_point(point)
    {
    }

    winrt::Windows::Foundation::Point MouseEventReceivedEventArgs::Point() const noexcept
    {
        return m_point;
    }
}
