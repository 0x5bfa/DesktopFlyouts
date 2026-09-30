#include "CppUnitTest.h"

#include <windows.h>

#include <winrt/Windows.Foundation.h>

#include "author/SystemTrayIcon.author.h"
#include "author/SystemTrayIcon.author.impl.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace DesktopFlyoutsWinUITests
{
    TEST_CLASS(SystemTrayIconTests)
    {
    public:
        TEST_METHOD(EventArgsPreserveTheClickPoint)
        {
            winrt::init_apartment(winrt::apartment_type::single_threaded);

            const winrt::Windows::Foundation::Point expected{ 12.0f, 34.0f };
            auto args = winrt::make<winrt::DesktopFlyouts::implementation::SystemTrayIconEventArgs>(expected);
            const auto actual = args.Point();

            Assert::AreEqual(expected.X, actual.X);
            Assert::AreEqual(expected.Y, actual.Y);
        }

        TEST_METHOD(CreationFromIconHandlePreservesPublicProperties)
        {
            winrt::init_apartment(winrt::apartment_type::single_threaded);

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
        }
    };
}
