#include "CppUnitTest.h"

#include "FlyoutInteraction.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace desktop_flyouts::core;

namespace DesktopFlyoutsCoreTests
{
    TEST_CLASS(FlyoutInteractionTests)
    {
    public:
        TEST_METHOD(LifecycleRequiresOrderedTransitions)
        {
            flyout_state_machine state;

            Assert::IsTrue(state.begin_open());
            Assert::IsFalse(state.begin_open());
            Assert::AreEqual(
                static_cast<std::int32_t>(flyout_lifecycle_state::opening),
                static_cast<std::int32_t>(state.state()));

            Assert::IsTrue(state.complete_open());
            Assert::IsFalse(state.complete_open());
            Assert::IsTrue(state.begin_close());
            Assert::IsTrue(state.complete_close());
            Assert::AreEqual(
                static_cast<std::int32_t>(flyout_lifecycle_state::closed),
                static_cast<std::int32_t>(state.state()));
        }

        TEST_METHOD(CloseCanCancelAnOpeningTransition)
        {
            flyout_state_machine state;

            Assert::IsTrue(state.begin_open());
            Assert::IsTrue(state.begin_close());
            Assert::IsTrue(state.complete_close());
            Assert::AreEqual(
                static_cast<std::int32_t>(flyout_lifecycle_state::closed),
                static_cast<std::int32_t>(state.state()));
        }

        TEST_METHOD(BottomToTopSwipeRequiresPositiveVerticalDominantDelta)
        {
            Assert::IsTrue(should_dismiss_by_swipe(popup_direction::bottom_to_top, 2.0, 90.0, 80.0));
            Assert::IsFalse(should_dismiss_by_swipe(popup_direction::bottom_to_top, 80.0, 90.0, 80.0));
            Assert::IsFalse(should_dismiss_by_swipe(popup_direction::bottom_to_top, 0.0, -90.0, 80.0));
        }

        TEST_METHOD(HorizontalSwipeUsesTheOppositeOpeningDirection)
        {
            Assert::IsTrue(should_dismiss_by_swipe(popup_direction::left_to_right, -100.0, 0.0, 80.0));
            Assert::IsTrue(should_dismiss_by_swipe(popup_direction::right_to_left, 100.0, 0.0, 80.0));
            Assert::IsFalse(should_dismiss_by_swipe(popup_direction::right_to_left, 79.0, 0.0, 80.0));
        }

        TEST_METHOD(InvalidSwipeInputsAreRejected)
        {
            Assert::IsFalse(should_dismiss_by_swipe(popup_direction::bottom_to_top, 0.0, 100.0, 0.0));
            Assert::IsFalse(should_dismiss_by_swipe(popup_direction::bottom_to_top, NAN, 100.0, 80.0));
            Assert::IsFalse(should_dismiss_by_swipe(popup_direction::bottom_to_top, 0.0, 100.0, 80.0, 0.5));
        }
    };
}
