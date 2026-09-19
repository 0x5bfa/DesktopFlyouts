#include "CppUnitTest.h"

#include "FlyoutLayout.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace desktop_flyouts::core;

namespace DesktopFlyoutsCoreTests
{
    TEST_CLASS(FlyoutLayoutTests)
    {
    public:
        TEST_METHOD(BottomRightPlacementStaysInsideWorkArea)
        {
            const auto result = resolve_layout({ { 0, 0, 1920, 1080 }, { 480, 320 }, 12, placement_mode::bottom_right, popup_direction::vertical });

            Assert::AreEqual(1428, result.bounds.left);
            Assert::AreEqual(748, result.bounds.top);
            Assert::AreEqual(1908, result.bounds.right);
            Assert::AreEqual(1068, result.bounds.bottom);
            Assert::AreEqual(static_cast<std::int32_t>(popup_direction::bottom_to_top), static_cast<std::int32_t>(result.direction));
        }

        TEST_METHOD(TopPlacementResolvesToTopToBottom)
        {
            const auto result = resolve_layout({ { 100, 100, 1100, 900 }, { 300, 200 }, 16, placement_mode::top_center, popup_direction::vertical });

            Assert::AreEqual(450, result.bounds.left);
            Assert::AreEqual(116, result.bounds.top);
            Assert::AreEqual(static_cast<std::int32_t>(popup_direction::top_to_bottom), static_cast<std::int32_t>(result.direction));
        }

        TEST_METHOD(DesiredSizeIsClampedToAvailableArea)
        {
            const auto result = resolve_layout({ { 10, 20, 210, 140 }, { 500, 500 }, 20, placement_mode::bottom_right, popup_direction::vertical });

            Assert::AreEqual(30, result.bounds.left);
            Assert::AreEqual(40, result.bounds.top);
            Assert::AreEqual(190, result.bounds.right);
            Assert::AreEqual(120, result.bounds.bottom);
        }

        TEST_METHOD(ExplicitDirectionIsPreserved)
        {
            const auto request = layout_request{ { 0, 0, 1000, 800 }, { 100, 100 }, 0, placement_mode::bottom_right, popup_direction::left_to_right };
            Assert::AreEqual(static_cast<std::int32_t>(popup_direction::left_to_right), static_cast<std::int32_t>(resolve_popup_direction(request)));
        }

        TEST_METHOD(HorizontalFallbackForTopPlacementIsDeterministic)
        {
            const auto request = layout_request{ { 0, 0, 1920, 1080 }, { 320, 240 }, 16, placement_mode::top_center, popup_direction::horizontal };
            Assert::AreEqual(static_cast<std::int32_t>(popup_direction::left_to_right), static_cast<std::int32_t>(resolve_popup_direction(request)));
        }

        TEST_METHOD(BottomCenterAnchorClampsToWorkArea)
        {
            const auto request = layout_request{
                { 100, 100, 1100, 900 },
                { 300, 200 },
                16,
                placement_mode::bottom_center,
                popup_direction::vertical,
                point{ 105, 120 } };
            const auto result = resolve_layout(request);

            Assert::AreEqual(116, result.bounds.left);
            Assert::AreEqual(116, result.bounds.top);
            Assert::AreEqual(416, result.bounds.right);
            Assert::AreEqual(316, result.bounds.bottom);
        }
    };
}
