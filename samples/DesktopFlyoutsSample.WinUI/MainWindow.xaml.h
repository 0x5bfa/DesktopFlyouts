#pragma once

#include "MainWindow.g.h"

#include <winrt/DesktopFlyouts.h>

namespace winrt::DesktopFlyoutsSample::WinUI::implementation
{
    struct MainWindow : MainWindowT<MainWindow>
    {
        MainWindow();
        ~MainWindow();
        void InitializeComponent();
        void InitializeDesktopFlyouts();

        void ShowFlyout_Click(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::RoutedEventArgs const& args);
        void HideFlyout_Click(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::RoutedEventArgs const& args);
        void ShowIslands_Click(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::RoutedEventArgs const& args);
        void ShowAutoClose_Click(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::RoutedEventArgs const& args);
        void ShowMenu_Click(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::RoutedEventArgs const& args);
        void ShowTray_Click(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::RoutedEventArgs const& args);
        void NavigateFocus_Click(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::RoutedEventArgs const& args);
        void FlyoutExample_SelectionChanged(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::Controls::SelectionChangedEventArgs const& args);
        void FlyoutPlacement_SelectionChanged(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::Controls::SelectionChangedEventArgs const& args);
        void PopupDirection_SelectionChanged(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::Controls::SelectionChangedEventArgs const& args);
        void ActivationMode_SelectionChanged(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::Controls::SelectionChangedEventArgs const& args);
        void BackdropKind_SelectionChanged(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::Controls::SelectionChangedEventArgs const& args);
        void FlyoutSize_ValueChanged(
            Microsoft::UI::Xaml::Controls::NumberBox const& sender,
            Microsoft::UI::Xaml::Controls::NumberBoxValueChangedEventArgs const& args);
        void Settings_Click(
            Windows::Foundation::IInspectable const& sender,
            Microsoft::UI::Xaml::RoutedEventArgs const& args);

        private:
        void ConfigureSelectedExampleContent();
        void ShowSelectedExample();
        void ShutdownDesktopFlyouts();
        void ApplySettings();
        void ConfigureFlyoutForExample();
        Microsoft::UI::Xaml::UIElement CreateExampleContent(std::int32_t example);
        Microsoft::UI::Xaml::Controls::Border CreateCard(
            winrt::hstring const& title,
            winrt::hstring const& description);
        Microsoft::UI::Xaml::Controls::Button CreateActionButton(
            winrt::hstring const& text,
            winrt::hstring const& automationId);
        Microsoft::UI::Xaml::Media::Brush ResourceBrush(winrt::hstring const& key);

        winrt::DesktopFlyouts::DesktopFlyout m_flyout{ nullptr };
        winrt::DesktopFlyouts::DesktopMenuFlyout m_menuFlyout{ nullptr };
        winrt::DesktopFlyouts::SystemTrayIcon m_trayIcon{ nullptr };
        winrt::event_token m_trayLeftToken{};
        winrt::event_token m_trayRightToken{};
        bool m_desktopFlyoutsInitialized{};
    };
}

namespace winrt::DesktopFlyoutsSample::WinUI::factory_implementation
{
    struct MainWindow : MainWindowT<MainWindow, implementation::MainWindow>
    {
    };
}
