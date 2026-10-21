#include "FlyoutInteraction.h"

#include <algorithm>

namespace desktop_flyouts::core
{
    bool should_dismiss_by_swipe(
        popup_direction direction,
        double delta_x,
        double delta_y,
        double threshold,
        double axis_dominance_ratio) noexcept
    {
        if (!std::isfinite(delta_x) ||
            !std::isfinite(delta_y) ||
            !std::isfinite(threshold) ||
            !std::isfinite(axis_dominance_ratio) ||
            threshold <= 0.0 ||
            axis_dominance_ratio < 1.0)
        {
            return false;
        }

        const auto horizontal_distance = std::abs(delta_x);
        const auto vertical_distance = std::abs(delta_y);

        switch (direction)
        {
        case popup_direction::bottom_to_top:
            return delta_y >= threshold && vertical_distance >= horizontal_distance * axis_dominance_ratio;
        case popup_direction::top_to_bottom:
            return delta_y <= -threshold && vertical_distance >= horizontal_distance * axis_dominance_ratio;
        case popup_direction::left_to_right:
            return delta_x <= -threshold && horizontal_distance >= vertical_distance * axis_dominance_ratio;
        case popup_direction::right_to_left:
            return delta_x >= threshold && horizontal_distance >= vertical_distance * axis_dominance_ratio;
        case popup_direction::vertical:
        case popup_direction::horizontal:
        default:
            return false;
        }
    }
}
