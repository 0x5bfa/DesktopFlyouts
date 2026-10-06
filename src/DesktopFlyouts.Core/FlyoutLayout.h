#pragma once

#include <cstdint>
#include <optional>

namespace desktop_flyouts::core
{
    enum class placement_mode : std::int32_t
    {
        top_center,
        top_left,
        top_right,
        bottom_center,
        bottom_left,
        bottom_right,
        left_center,
        right_center,
    };

    enum class popup_direction : std::int32_t
    {
        bottom_to_top,
        top_to_bottom,
        vertical,
        left_to_right,
        right_to_left,
        horizontal,
    };

    struct point
    {
        std::int32_t x{};
        std::int32_t y{};
    };

    struct extent
    {
        std::int32_t width{};
        std::int32_t height{};
    };

    struct rect
    {
        std::int32_t left{};
        std::int32_t top{};
        std::int32_t right{};
        std::int32_t bottom{};

        [[nodiscard]] constexpr std::int32_t width() const noexcept
        {
            return right - left;
        }

        [[nodiscard]] constexpr std::int32_t height() const noexcept
        {
            return bottom - top;
        }

        [[nodiscard]] constexpr std::int32_t center_x() const noexcept
        {
            return left + width() / 2;
        }

        [[nodiscard]] constexpr std::int32_t center_y() const noexcept
        {
            return top + height() / 2;
        }
    };

    struct layout_request
    {
        rect work_area{};
        extent desired_size{};
        std::int32_t margin{};
        placement_mode placement{ placement_mode::bottom_right };
        popup_direction direction{ popup_direction::vertical };
        std::optional<point> bottom_center_anchor{};
        std::optional<rect> resize_anchor_region{};
        popup_direction resize_direction{ popup_direction::bottom_to_top };
    };

    struct layout_result
    {
        rect bounds{};
        popup_direction direction{ popup_direction::bottom_to_top };
    };

    [[nodiscard]] popup_direction resolve_popup_direction(layout_request const& request) noexcept;

    [[nodiscard]] layout_result resolve_layout(layout_request const& request) noexcept;
}
