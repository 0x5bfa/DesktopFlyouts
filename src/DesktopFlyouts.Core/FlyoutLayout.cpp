#include "FlyoutLayout.h"

#include <algorithm>

namespace desktop_flyouts::core
{
    namespace
    {
        [[nodiscard]] std::int32_t clamp_value(std::int32_t value, std::int32_t minimum, std::int32_t maximum) noexcept
        {
            return std::clamp(value, minimum, std::max(minimum, maximum));
        }

        [[nodiscard]] bool is_top_placement(placement_mode placement) noexcept
        {
            return placement == placement_mode::top_center ||
                placement == placement_mode::top_left ||
                placement == placement_mode::top_right;
        }

        [[nodiscard]] bool is_bottom_placement(placement_mode placement) noexcept
        {
            return placement == placement_mode::bottom_center ||
                placement == placement_mode::bottom_left ||
                placement == placement_mode::bottom_right;
        }

        [[nodiscard]] bool is_left_placement(placement_mode placement) noexcept
        {
            return placement == placement_mode::left_center;
        }

        [[nodiscard]] bool is_right_placement(placement_mode placement) noexcept
        {
            return placement == placement_mode::right_center;
        }

        [[nodiscard]] rect clamp_to_work_area(rect bounds, rect work_area, std::int32_t margin) noexcept
        {
            const auto minimum_left = work_area.left + margin;
            const auto minimum_top = work_area.top + margin;
            const auto maximum_left = work_area.right - margin - bounds.width();
            const auto maximum_top = work_area.bottom - margin - bounds.height();

            const auto left = clamp_value(bounds.left, minimum_left, maximum_left);
            const auto top = clamp_value(bounds.top, minimum_top, maximum_top);
            return { left, top, left + bounds.width(), top + bounds.height() };
        }
    }

    popup_direction resolve_popup_direction(layout_request const& request) noexcept
    {
        if (request.direction != popup_direction::vertical && request.direction != popup_direction::horizontal)
        {
            return request.direction;
        }

        if (request.direction == popup_direction::horizontal)
        {
            if (is_left_placement(request.placement))
            {
                return popup_direction::left_to_right;
            }

            if (is_right_placement(request.placement))
            {
                return popup_direction::right_to_left;
            }

            // There is no physical side anchor for top/bottom placements. Keep
            // the horizontal fallback deterministic so callers can rely on it.
            return popup_direction::left_to_right;
        }

        if (is_top_placement(request.placement))
        {
            return popup_direction::top_to_bottom;
        }

        if (is_bottom_placement(request.placement))
        {
            return popup_direction::bottom_to_top;
        }

        // The public placement enum has no unanchored center value. If an
        // invalid value reaches this point, keep the fallback deterministic.
        return popup_direction::bottom_to_top;
    }

    layout_result resolve_layout(layout_request const& request) noexcept
    {
        const auto margin = std::max(0, request.margin);
        const auto available_width = std::max(0, request.work_area.width() - margin * 2);
        const auto available_height = std::max(0, request.work_area.height() - margin * 2);
        const auto width = std::clamp(request.desired_size.width, 0, available_width);
        const auto height = std::clamp(request.desired_size.height, 0, available_height);

        const auto minimum_left = request.work_area.left + margin;
        const auto minimum_top = request.work_area.top + margin;
        const auto maximum_left = request.work_area.right - margin - width;
        const auto maximum_top = request.work_area.bottom - margin - height;

        std::int32_t left = minimum_left;
        std::int32_t top = minimum_top;

        if (request.bottom_center_anchor.has_value())
        {
            left = request.bottom_center_anchor->x - width / 2;
            top = request.bottom_center_anchor->y - height;
        }
        else if (request.resize_anchor_region.has_value())
        {
            const auto& anchor = request.resize_anchor_region.value();
            switch (request.resize_direction)
            {
            case popup_direction::right_to_left:
                left = anchor.right - width;
                break;
            case popup_direction::left_to_right:
                left = anchor.left;
                break;
            case popup_direction::bottom_to_top:
            case popup_direction::top_to_bottom:
            case popup_direction::vertical:
            case popup_direction::horizontal:
            default:
                left = anchor.left + (anchor.width() - width) / 2;
                break;
            }

            switch (request.resize_direction)
            {
            case popup_direction::bottom_to_top:
                top = anchor.bottom - height;
                break;
            case popup_direction::top_to_bottom:
                top = anchor.top;
                break;
            case popup_direction::left_to_right:
            case popup_direction::right_to_left:
            case popup_direction::vertical:
            case popup_direction::horizontal:
            default:
                top = anchor.top + (anchor.height() - height) / 2;
                break;
            }
        }
        else
        {
            switch (request.placement)
            {
            case placement_mode::top_center:
            case placement_mode::bottom_center:
                left = request.work_area.center_x() - width / 2;
                break;
            case placement_mode::top_right:
            case placement_mode::bottom_right:
            case placement_mode::right_center:
                left = maximum_left;
                break;
            case placement_mode::top_left:
            case placement_mode::bottom_left:
            case placement_mode::left_center:
                left = minimum_left;
                break;
            }

            switch (request.placement)
            {
            case placement_mode::left_center:
            case placement_mode::right_center:
                top = request.work_area.center_y() - height / 2;
                break;
            case placement_mode::bottom_center:
            case placement_mode::bottom_left:
            case placement_mode::bottom_right:
                top = maximum_top;
                break;
            case placement_mode::top_center:
            case placement_mode::top_left:
            case placement_mode::top_right:
                top = minimum_top;
                break;
            }
        }

        const auto bounds = clamp_to_work_area({ left, top, left + width, top + height }, request.work_area, margin);
        return { bounds, resolve_popup_direction(request) };
    }
}
