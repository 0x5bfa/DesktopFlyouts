#include "CppUnitTest.h"

#include <windows.h>

#include <winrt/Windows.Foundation.h>

#include "author/SystemTrayIcon.author.h"
#include "author/SystemTrayIcon.author.impl.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace DesktopFlyoutsUwpTests
{
    TEST_CLASS(SystemTrayIconTests)
    {
    public:
        TEST_METHOD_INITIALIZE(InitializeApartment)
        {
            winrt::init_apartment(winrt::apartment_type::single_threaded);
        }

        TEST_METHOD(ClickEventsForwardThePointAndCanBeUnsubscribed)
        {
            const auto sourceIcon = LoadIconW(nullptr, IDI_APPLICATION);
            Assert::IsNotNull(sourceIcon);

            auto icon = winrt::DesktopFlyouts::author::SystemTrayIcon::CreateFromIconHandle(
                reinterpret_cast<std::int64_t>(sourceIcon),
                L"Test tooltip",
                winrt::guid{});
            auto* implementation = winrt::get_self<winrt::DesktopFlyouts::implementation::SystemTrayIcon>(icon);
            const winrt::Windows::Foundation::Point expected{ 12.0f, 34.0f };
            auto args = winrt::make<winrt::DesktopFlyouts::implementation::SystemTrayIconEventArgs>(expected);

            int leftClicks{};
            winrt::Windows::Foundation::Point actual{};
            auto leftToken = icon.LeftClicked([&](auto const&, auto const& eventArgs)
            {
                ++leftClicks;
                actual = eventArgs.Point();
            });
            implementation->RaiseLeftClicked(args);
            Assert::AreEqual(1, leftClicks);
            Assert::AreEqual(expected.X, actual.X);
            Assert::AreEqual(expected.Y, actual.Y);

            icon.LeftClicked(leftToken);
            implementation->RaiseLeftClicked(args);
            Assert::AreEqual(1, leftClicks);

            int rightClicks{};
            auto rightToken = icon.RightClicked([&](auto const&, auto const& eventArgs)
            {
                ++rightClicks;
                actual = eventArgs.Point();
            });
            implementation->RaiseRightClicked(args);
            Assert::AreEqual(1, rightClicks);
            Assert::AreEqual(expected.X, actual.X);
            Assert::AreEqual(expected.Y, actual.Y);
            icon.RightClicked(rightToken);
            implementation->RaiseRightClicked(args);
            Assert::AreEqual(1, rightClicks);

            int leftDoubleClicks{};
            auto leftDoubleToken = icon.LeftDoubleClicked([&](auto const&, auto const& eventArgs)
            {
                ++leftDoubleClicks;
                actual = eventArgs.Point();
            });
            implementation->RaiseLeftDoubleClicked(args);
            Assert::AreEqual(1, leftDoubleClicks);
            Assert::AreEqual(expected.X, actual.X);
            Assert::AreEqual(expected.Y, actual.Y);
            icon.LeftDoubleClicked(leftDoubleToken);
            implementation->RaiseLeftDoubleClicked(args);
            Assert::AreEqual(1, leftDoubleClicks);

            int rightDoubleClicks{};
            auto rightDoubleToken = icon.RightDoubleClicked([&](auto const&, auto const& eventArgs)
            {
                ++rightDoubleClicks;
                actual = eventArgs.Point();
            });
            implementation->RaiseRightDoubleClicked(args);
            Assert::AreEqual(1, rightDoubleClicks);
            Assert::AreEqual(expected.X, actual.X);
            Assert::AreEqual(expected.Y, actual.Y);
            icon.RightDoubleClicked(rightDoubleToken);
            implementation->RaiseRightDoubleClicked(args);
            Assert::AreEqual(1, rightDoubleClicks);
        }

        TEST_METHOD(CreationFromIconHandleInitializesConsumerVisibleState)
        {
            const auto sourceIcon = LoadIconW(nullptr, IDI_APPLICATION);
            Assert::IsNotNull(sourceIcon);

            const winrt::guid expectedId{ 0x01234567, 0x89ab, 0xcdef, { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef } };
            auto icon = winrt::DesktopFlyouts::author::SystemTrayIcon::CreateFromIconHandle(
                reinterpret_cast<std::int64_t>(sourceIcon),
                L"Test tooltip",
                expectedId);

            Assert::IsTrue(icon.IconPath().empty());
            Assert::IsTrue(icon.Tooltip() == L"Test tooltip");
            Assert::IsTrue(icon.Id() == expectedId);
            Assert::IsFalse(icon.IsVisible());

            icon.Tooltip(L"Updated tooltip");
            Assert::IsTrue(icon.Tooltip() == L"Updated tooltip");
            Assert::IsFalse(icon.IsVisible());
        }

        TEST_METHOD(InvalidIconHandleIsRejectedAndLeavesPropertiesUnchanged)
        {
            const auto sourceIcon = LoadIconW(nullptr, IDI_APPLICATION);
            Assert::IsNotNull(sourceIcon);

            auto icon = winrt::DesktopFlyouts::author::SystemTrayIcon::CreateFromIconHandle(
                reinterpret_cast<std::int64_t>(sourceIcon),
                L"Test tooltip",
                winrt::guid{});

            bool rejected{};
            try
            {
                icon.SetIconHandle(0);
            }
            catch (winrt::hresult_error const&)
            {
                rejected = true;
            }

            Assert::IsTrue(rejected);
            Assert::IsTrue(icon.Tooltip() == L"Test tooltip");
            Assert::IsFalse(icon.IsVisible());
        }
    };
}
