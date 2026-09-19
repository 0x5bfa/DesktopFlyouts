#pragma once

#include <cmath>
#include <cstdint>

#include "FlyoutLayout.h"

namespace desktop_flyouts::core
{
    enum class flyout_lifecycle_state : std::int32_t
    {
        closed,
        opening,
        open,
        closing,
    };

    class flyout_state_machine final
    {
    public:
        [[nodiscard]] flyout_lifecycle_state state() const noexcept
        {
            return m_state;
        }

        [[nodiscard]] bool begin_open() noexcept
        {
            if (m_state != flyout_lifecycle_state::closed)
            {
                return false;
            }

            m_state = flyout_lifecycle_state::opening;
            return true;
        }

        [[nodiscard]] bool complete_open() noexcept
        {
            if (m_state != flyout_lifecycle_state::opening)
            {
                return false;
            }

            m_state = flyout_lifecycle_state::open;
            return true;
        }

        [[nodiscard]] bool begin_close() noexcept
        {
            if (m_state != flyout_lifecycle_state::opening &&
                m_state != flyout_lifecycle_state::open)
            {
                return false;
            }

            m_state = flyout_lifecycle_state::closing;
            return true;
        }

        [[nodiscard]] bool complete_close() noexcept
        {
            if (m_state != flyout_lifecycle_state::closing)
            {
                return false;
            }

            m_state = flyout_lifecycle_state::closed;
            return true;
        }

        void force_closed() noexcept
        {
            m_state = flyout_lifecycle_state::closed;
        }

    private:
        flyout_lifecycle_state m_state{ flyout_lifecycle_state::closed };
    };

    [[nodiscard]] bool should_dismiss_by_swipe(
        popup_direction direction,
        double delta_x,
        double delta_y,
        double threshold,
        double axis_dominance_ratio = 1.2) noexcept;
}
