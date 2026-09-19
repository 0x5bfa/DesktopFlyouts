#include "pch.h"
#include "MainWindow.xaml.h"

#include "MainWindow.g.cpp"
#include <winrt/Microsoft.UI.Dispatching.h>
#include <microsoft.ui.xaml.window.h>
#include <winrt/Microsoft.UI.Text.h>

#include <algorithm>
#include <chrono>
#include <cmath>

using namespace winrt;
using namespace Microsoft::UI::Xaml;
using namespace Microsoft::UI::Xaml::Controls;

namespace
{
    constexpr std::int32_t c_defaultWidth = 360;

    Windows::UI::Color Transparent()
    {
        return { 0, 0, 0, 0 };
    }

    winrt::DesktopFlyouts::DesktopFlyoutPlacementMode PlacementFromIndex(std::int32_t index) noexcept
    {
        switch (index)
        {
        case 0: return winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::top_left;
        case 1: return winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::top_center;
        case 2: return winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::top_right;
        case 3: return winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::bottom_left;
        case 4: return winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::bottom_center;
        case 5: return winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::bottom_right;
        case 6: return winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::left_center;
        case 7: return winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::right_center;
        default: return winrt::DesktopFlyouts::DesktopFlyoutPlacementMode::bottom_right;
        }
    }

    winrt::DesktopFlyouts::DesktopFlyoutPopupDirection DirectionFromIndex(std::int32_t index) noexcept
    {
        switch (index)
        {
        case 0: return winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::vertical;
        case 1: return winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::bottom_to_top;
        case 2: return winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::top_to_bottom;
        case 3: return winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::horizontal;
        case 4: return winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::left_to_right;
        case 5: return winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::right_to_left;
        default: return winrt::DesktopFlyouts::DesktopFlyoutPopupDirection::vertical;
        }
    }
}

namespace winrt::DesktopFlyoutsSample::WinUI::implementation
{
    void ResetFlyoutContent(winrt::DesktopFlyouts::DesktopFlyout const& flyout)
    {
        flyout.Hide();
        flyout.Content(nullptr);
        flyout.Islands().Clear();
        flyout.AutoCloseDelay({});
        flyout.IsTransitionAnimationEnabled(true);
        flyout.IsSwipeToDismissEnabled(false);
        flyout.PressedScale(1.0);
        flyout.SwipeDismissThreshold(80.0);
    }

    MainWindow::MainWindow()
    {
    }

    void MainWindow::InitializeComponent()
    {
        MainWindowT<MainWindow>::InitializeComponent();
        ExtendsContentIntoTitleBar(true);

        (void)Closed([this](auto const&, auto const&)
        {
            ShutdownDesktopFlyouts();
        });

        // The WinUI Window.Activated event can be raised while the window is
        // being torn down. Queue initialization once the window has entered
        // its normal dispatcher lifetime instead of retaining a raw callback
        // on that event source.
        DispatcherQueue().TryEnqueue([this]
        {
            if (m_desktopFlyoutsInitialized)
            {
                return;
            }

            m_desktopFlyoutsInitialized = true;
            InitializeDesktopFlyouts();
        });

    }

    void MainWindow::InitializeDesktopFlyouts()
    {

        HWND windowHandle{};
        winrt::impl::com_ref<::IWindowNative> windowNative = try_as<::IWindowNative>();
        winrt::check_bool(static_cast<bool>(windowNative));
        winrt::check_hresult(windowNative->get_WindowHandle(&windowHandle));

        m_flyout = winrt::DesktopFlyouts::DesktopFlyout{};
        m_flyout.OwnerWindowHandle(reinterpret_cast<std::int64_t>(windowHandle));
        m_flyout.Margin({ 12, 12, 12, 12 });

        m_menuFlyout = winrt::DesktopFlyouts::DesktopMenuFlyout{};
        m_menuFlyout.OwnerWindowHandle(reinterpret_cast<std::int64_t>(windowHandle));

        auto showItem = MenuFlyoutItem{};
        showItem.Text(L"Show selected flyout");
        showItem.Click([this](auto const&, auto const&)
        {
            ShowSelectedExample();
        });
        m_menuFlyout.Items().Append(showItem);

        auto hideItem = MenuFlyoutItem{};
        hideItem.Text(L"Hide flyout");
        hideItem.Click([this](auto const&, auto const&)
        {
            m_flyout.Hide();
            StatusText().Text(L"Flyout is closed");
        });
        m_menuFlyout.Items().Append(hideItem);

        auto exitItem = MenuFlyoutItem{};
        exitItem.Text(L"Close sample");
        exitItem.Click([this](auto const&, auto const&)
        {
            // Let MenuFlyout finish its Click/Closed dispatch before the
            // window destructor tears down the XAML island host.
            DispatcherQueue().TryEnqueue([this]
            {
                Close();
            });
        });
        m_menuFlyout.Items().Append(exitItem);

        m_trayIcon = winrt::DesktopFlyouts::SystemTrayIcon{
            L"",
            L"DesktopFlyouts C++/WinRT sample",
            winrt::guid{
                0x28DE460A,
                0x8BD6,
                0x4539,
                { 0xA4, 0x06, 0x5F, 0x68, 0x55, 0x84, 0xFD, 0x4D } } };
        m_trayLeftToken = m_trayIcon.LeftClicked([this](auto const&, IInspectable const& value)
        {
            if (auto args = value.try_as<winrt::DesktopFlyouts::SystemTrayIconEventArgs>())
            {
                const auto point = args.Point();
                ConfigureSelectedExampleContent();
                m_flyout.ShowAt(static_cast<std::int32_t>(point.X), static_cast<std::int32_t>(point.Y));
                StatusText().Text(L"Flyout opened from the tray icon");
            }
        });
        m_trayRightToken = m_trayIcon.RightClicked([this](auto const&, IInspectable const& value)
        {
            if (auto args = value.try_as<winrt::DesktopFlyouts::SystemTrayIconEventArgs>())
            {
                const auto point = args.Point();
                m_menuFlyout.ShowAt(
                    static_cast<std::int32_t>(point.X),
                    static_cast<std::int32_t>(point.Y - 32));
                StatusText().Text(L"Tray menu opened");
            }
        });
        m_trayIcon.Show();
        ApplySettings();
    }

    MainWindow::~MainWindow()
    {
    }

    void MainWindow::ShutdownDesktopFlyouts()
    {
        if (m_trayIcon)
        {
            if (m_trayLeftToken.value != 0)
            {
                try
                {
                    m_trayIcon.LeftClicked(m_trayLeftToken);
                }
                catch (...)
                {
                }
                m_trayLeftToken = {};
            }
            if (m_trayRightToken.value != 0)
            {
                try
                {
                    m_trayIcon.RightClicked(m_trayRightToken);
                }
                catch (...)
                {
                }
                m_trayRightToken = {};
            }
            try
            {
                m_trayIcon.Destroy();
            }
            catch (...)
            {
            }
            m_trayIcon = nullptr;
        }

        if (m_menuFlyout)
        {
            try
            {
                m_menuFlyout.Hide();
            }
            catch (...)
            {
            }
            m_menuFlyout = nullptr;
        }

        if (m_flyout)
        {
            try
            {
                m_flyout.Hide();
            }
            catch (...)
            {
            }
            m_flyout = nullptr;
        }
    }

    Microsoft::UI::Xaml::Media::Brush MainWindow::ResourceBrush(winrt::hstring const& key)
    {
        try
        {
            auto resources = Application::Current().Resources();
            if (auto brush = resources.Lookup(box_value(key)).try_as<Microsoft::UI::Xaml::Media::Brush>())
            {
                return brush;
            }
        }
        catch (...)
        {
        }

        return Microsoft::UI::Xaml::Media::SolidColorBrush(Transparent());
    }

    Border MainWindow::CreateCard(winrt::hstring const& title, winrt::hstring const& description)
    {
        auto titleBlock = TextBlock{};
        titleBlock.Name(L"FlyoutTitle");
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetAutomationId(titleBlock, L"FlyoutTitle");
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetName(titleBlock, title);
        titleBlock.Text(title);
        titleBlock.FontSize(20.0);
        titleBlock.FontWeight(Microsoft::UI::Text::FontWeights::SemiBold());

        auto descriptionBlock = TextBlock{};
        descriptionBlock.Name(L"FlyoutDescription");
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetAutomationId(
            descriptionBlock,
            L"FlyoutDescription");
        descriptionBlock.Text(description);
        descriptionBlock.TextWrapping(TextWrapping::Wrap);
        descriptionBlock.Foreground(ResourceBrush(L"TextFillColorSecondaryBrush"));

        auto stack = StackPanel{};
        stack.Spacing(8.0);
        stack.Children().Append(titleBlock);
        stack.Children().Append(descriptionBlock);

        auto card = Border{};
        card.Name(L"DesktopFlyoutSurface");
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetAutomationId(
            card,
            L"DesktopFlyoutSurface");
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetName(card, title);
        card.Background(ResourceBrush(L"FlyoutOverlayBackgroundBrush"));
        card.BorderBrush(ResourceBrush(L"CardStrokeColorDefaultBrush"));
        card.BorderThickness({ 1, 1, 1, 1 });
        card.CornerRadius({ 12, 12, 12, 12 });
        card.Padding({ 20, 16, 20, 16 });
        card.Child(stack);
        return card;
    }

    Button MainWindow::CreateActionButton(
        winrt::hstring const& text,
        winrt::hstring const& automationId)
    {
        auto button = Button{};
        button.Content(box_value(text));
        button.Name(automationId);
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetAutomationId(button, automationId);
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetName(button, text);
        button.Click([this](auto const&, auto const&)
        {
            m_flyout.Hide();
            StatusText().Text(L"Flyout is closed");
        });
        return button;
    }

    UIElement MainWindow::CreateExampleContent(std::int32_t example)
    {
        switch (example)
        {
        case 1:
        {
            auto card = CreateCard(L"Button", L"A compact action flyout using a standard Fluent button.");
            auto stack = card.Child().as<StackPanel>();
            stack.Children().Append(CreateActionButton(L"Primary action", L"ExampleButtonAction"));
            stack.Children().Append(CreateActionButton(L"Close", L"CloseFlyoutButton"));
            return card;
        }
        case 2:
        {
            auto card = CreateCard(L"Indicator", L"Progress and state controls remain ordinary WinUI controls.");
            auto stack = card.Child().as<StackPanel>();
            auto progress = ProgressBar{};
            progress.Value(68.0);
            progress.Height(6.0);
            stack.Children().Append(progress);
            auto status = ToggleSwitch{};
            status.Header(box_value(L"Ready"));
            status.IsOn(true);
            stack.Children().Append(status);
            stack.Children().Append(CreateActionButton(L"Close", L"CloseFlyoutButton"));
            return card;
        }
        case 3:
        {
            auto card = CreateCard(L"Notification Center", L"A stacked notification surface with dismissible actions.");
            auto stack = card.Child().as<StackPanel>();
            auto notification = Border{};
            notification.Background(ResourceBrush(L"ControlFillColorSecondaryBrush"));
            notification.CornerRadius({ 8, 8, 8, 8 });
            notification.Padding({ 12, 10, 12, 10 });
            auto text = TextBlock{};
            text.Text(L"DesktopFlyouts notification");
            text.TextWrapping(TextWrapping::Wrap);
            notification.Child(text);
            stack.Children().Append(notification);
            stack.Children().Append(CreateActionButton(L"Dismiss", L"NotificationCloseButton"));
            stack.Children().Append(CreateActionButton(L"Close", L"CloseFlyoutButton"));
            return card;
        }
        case 4:
        {
            auto card = CreateCard(L"Start Menu", L"A two-column launcher scenario with search and pinned actions.");
            auto stack = card.Child().as<StackPanel>();
            auto search = TextBox{};
            search.PlaceholderText(L"Search apps");
            search.Name(L"StartMenuSearchBox");
            Microsoft::UI::Xaml::Automation::AutomationProperties::SetAutomationId(search, L"StartMenuSearchBox");
            stack.Children().Append(search);
            auto apps = ListView{};
            apps.Items().Append(box_value(L"Files"));
            apps.Items().Append(box_value(L"Settings"));
            apps.Items().Append(box_value(L"Terminal"));
            apps.MaxHeight(120.0);
            stack.Children().Append(apps);
            stack.Children().Append(CreateActionButton(L"Close", L"CloseFlyoutButton"));
            return card;
        }
        case 5:
        {
            auto card = CreateCard(L"Sticky small", L"A small always-available utility flyout.");
            card.Padding({ 14, 12, 14, 12 });
            auto stack = card.Child().as<StackPanel>();
            stack.Children().Append(CreateActionButton(L"Pin", L"StickyPinButton"));
            stack.Children().Append(CreateActionButton(L"Close", L"CloseFlyoutButton"));
            return card;
        }
        case 6:
        {
            auto card = CreateCard(L"Widget", L"A wider dashboard-like flyout with independent cards.");
            auto stack = card.Child().as<StackPanel>();
            auto metrics = Grid{};
            metrics.ColumnDefinitions().Append(ColumnDefinition{});
            metrics.ColumnDefinitions().Append(ColumnDefinition{});
            auto left = Border{};
            left.Background(ResourceBrush(L"ControlFillColorSecondaryBrush"));
            left.Padding({ 12, 12, 12, 12 });
            auto leftStack = StackPanel{};
            auto leftText = TextBlock{};
            leftText.Text(L"CPU 42%");
            leftStack.Children().Append(leftText);
            auto leftBar = ProgressBar{};
            leftBar.Value(42.0);
            leftStack.Children().Append(leftBar);
            left.Child(leftStack);
            Grid::SetColumn(left, 0);
            metrics.Children().Append(left);
            auto right = Border{};
            right.Background(ResourceBrush(L"ControlFillColorSecondaryBrush"));
            right.Padding({ 12, 12, 12, 12 });
            auto rightText = TextBlock{};
            rightText.Text(L"Memory 68%");
            right.Child(rightText);
            Grid::SetColumn(right, 1);
            metrics.Children().Append(right);
            stack.Children().Append(metrics);
            stack.Children().Append(CreateActionButton(L"Close", L"CloseFlyoutButton"));
            return card;
        }
        case 7:
        {
            auto card = CreateCard(L"Severity", L"An alert-style scenario for warning and error presentation.");
            auto stack = card.Child().as<StackPanel>();
            auto severity = Border{};
            severity.Background(ResourceBrush(L"SystemFillColorCautionBackgroundBrush"));
            severity.Padding({ 12, 10, 12, 10 });
            auto severityText = TextBlock{};
            severityText.Text(L"Warning: review the current settings.");
            severityText.TextWrapping(TextWrapping::Wrap);
            severity.Child(severityText);
            stack.Children().Append(severity);
            stack.Children().Append(CreateActionButton(L"Close", L"CloseFlyoutButton"));
            return card;
        }
        case 0:
        default:
        {
            auto card = CreateCard(
                L"Default",
                L"The default Windows 11 DesktopFlyout visual, hosted by a native C++/WinRT popup.");
            auto stack = card.Child().as<StackPanel>();
            stack.Children().Append(CreateActionButton(L"Close", L"CloseFlyoutButton"));
            return card;
        }
        }
    }

    void MainWindow::ApplySettings()
    {
        const auto widthValue = FlyoutWidthNumberBox().Value();
        const auto width = std::isfinite(widthValue)
            ? static_cast<std::int32_t>(std::clamp(widthValue, 0.0, 1400.0))
            : c_defaultWidth;
        const auto heightValue = FlyoutHeightNumberBox().Value();
        const auto height = std::isfinite(heightValue)
            ? static_cast<std::int32_t>(std::clamp(heightValue, 0.0, 1000.0))
            : 0;

        const auto thresholdValue = SwipeDismissThresholdNumberBox().Value();
        const auto threshold = std::isfinite(thresholdValue)
            ? std::clamp(thresholdValue, 1.0, 2000.0)
            : 80.0;
        const auto autoCloseValue = AutoCloseDelayNumberBox().Value();
        const auto autoCloseMilliseconds = std::isfinite(autoCloseValue) && autoCloseValue > 0.0
            ? static_cast<std::int64_t>(std::llround(std::clamp(autoCloseValue, 0.0, 60.0) * 1000.0))
            : 0;

        m_flyout.FlyoutWidth(width <= 0
            ? Microsoft::UI::Xaml::GridLength{ 1.0, Microsoft::UI::Xaml::GridUnitType::Auto }
            : Microsoft::UI::Xaml::GridLength{
                static_cast<double>(width),
                Microsoft::UI::Xaml::GridUnitType::Pixel });
        m_flyout.FlyoutHeight(height == 0
            ? Microsoft::UI::Xaml::GridLength{ 1.0, Microsoft::UI::Xaml::GridUnitType::Auto }
            : Microsoft::UI::Xaml::GridLength{
                static_cast<double>(height),
                Microsoft::UI::Xaml::GridUnitType::Pixel });
        m_flyout.AutoCloseDelay(std::chrono::milliseconds(autoCloseMilliseconds));
        m_flyout.IsBackdropEnabled(BackdropCheckBox().IsChecked().GetBoolean());
        m_flyout.HideOnLostFocus(HideOnLostFocusCheckBox().IsChecked().GetBoolean());
        m_flyout.IsSwipeToDismissEnabled(SwipeCheckBox().IsChecked().GetBoolean());
        m_flyout.IsTransitionAnimationEnabled(TransitionCheckBox().IsChecked().GetBoolean());
        m_flyout.PressedScale(SwipeCheckBox().IsChecked().GetBoolean() ? 0.96 : 1.0);
        m_flyout.SwipeDismissThreshold(threshold);
        m_flyout.BackdropKind(BackdropKindComboBox().SelectedIndex() == 0
            ? winrt::DesktopFlyouts::DesktopFlyoutBackdropKind::mica
            : winrt::DesktopFlyouts::DesktopFlyoutBackdropKind::desktop_acrylic);
        m_flyout.ActivationMode(static_cast<winrt::DesktopFlyouts::DesktopFlyoutActivationMode>(
            std::clamp(ActivationModeComboBox().SelectedIndex(), 0, 2)));
        m_flyout.Placement(PlacementFromIndex(
            std::clamp(FlyoutPlacementComboBox().SelectedIndex(), 0, 7)));
        m_flyout.PopupDirection(DirectionFromIndex(
            std::clamp(PopupDirectionComboBox().SelectedIndex(), 0, 5)));
    }

    void MainWindow::ConfigureFlyoutForExample()
    {
        ApplySettings();
        const auto example = FlyoutExampleComboBox().SelectedIndex();
        if (example == 5)
        {
            m_flyout.FlyoutWidth({ 280.0, Microsoft::UI::Xaml::GridUnitType::Pixel });
            m_flyout.FlyoutHeight({ 160.0, Microsoft::UI::Xaml::GridUnitType::Pixel });
        }
        else if (example == 6)
        {
            m_flyout.FlyoutWidth({ 720.0, Microsoft::UI::Xaml::GridUnitType::Pixel });
            m_flyout.FlyoutHeight({ 360.0, Microsoft::UI::Xaml::GridUnitType::Pixel });
        }
        else if (example == 4)
        {
            m_flyout.FlyoutWidth({ 520.0, Microsoft::UI::Xaml::GridUnitType::Pixel });
            m_flyout.FlyoutHeight({ 560.0, Microsoft::UI::Xaml::GridUnitType::Pixel });
        }
    }

    void MainWindow::ConfigureSelectedExampleContent()
    {
        ResetFlyoutContent(m_flyout);
        ConfigureFlyoutForExample();
        m_flyout.Content(CreateExampleContent(FlyoutExampleComboBox().SelectedIndex()));
    }

    void MainWindow::ShowSelectedExample()
    {
        ConfigureSelectedExampleContent();
        m_flyout.Show();
        StatusText().Text(L"Selected flyout requested");
    }

    void MainWindow::ShowFlyout_Click(IInspectable const&, RoutedEventArgs const&)
    {
        ShowSelectedExample();
    }

    void MainWindow::HideFlyout_Click(IInspectable const&, RoutedEventArgs const&)
    {
        m_flyout.Hide();
        StatusText().Text(L"Flyout is closed");
    }

    void MainWindow::ShowIslands_Click(IInspectable const&, RoutedEventArgs const&)
    {
        ResetFlyoutContent(m_flyout);
        ApplySettings();
        m_flyout.FlyoutWidth({ 420.0, Microsoft::UI::Xaml::GridUnitType::Pixel });
        m_flyout.FlyoutHeight({ 300.0, Microsoft::UI::Xaml::GridUnitType::Pixel });
        m_flyout.IslandsOrientation(winrt::DesktopFlyouts::DesktopFlyoutOrientation::vertical);
        m_flyout.IslandSpacing(8);
        m_flyout.PressedScale(0.96);
        m_flyout.IsSwipeToDismissEnabled(true);
        m_flyout.SwipeDismissThreshold(60.0);

        auto firstText = TextBlock{};
        firstText.Text(L"Native island one");
        firstText.FontSize(18.0);
        firstText.Name(L"IslandOneText");
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetAutomationId(firstText, L"IslandOneText");
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetName(firstText, L"Native island one");
        auto first = Border{};
        first.Padding({ 12, 10, 12, 10 });
        first.Background(ResourceBrush(L"ControlFillColorSecondaryBrush"));
        first.Child(firstText);

        auto secondText = TextBlock{};
        secondText.Text(L"Native island two");
        secondText.Name(L"IslandTwoText");
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetAutomationId(secondText, L"IslandTwoText");
        Microsoft::UI::Xaml::Automation::AutomationProperties::SetName(secondText, L"Native island two");

        auto secondStack = StackPanel{};
        secondStack.Spacing(8.0);
        secondStack.Children().Append(secondText);
        secondStack.Children().Append(CreateActionButton(L"Close", L"IslandsCloseButton"));
        auto second = Border{};
        second.Padding({ 12, 10, 12, 10 });
        second.Background(ResourceBrush(L"ControlFillColorSecondaryBrush"));
        second.Child(secondStack);

        m_flyout.Islands().Append(first);
        m_flyout.Islands().Append(second);
        m_flyout.IsTransitionAnimationEnabled(false);
        m_flyout.Show();
        StatusText().Text(L"Flyout islands requested");
    }

    void MainWindow::ShowAutoClose_Click(IInspectable const&, RoutedEventArgs const&)
    {
        AutoCloseDelayNumberBox().Value(0.7);
        TransitionCheckBox().IsChecked(false);
        ShowSelectedExample();
        StatusText().Text(L"Flyout auto-closes in 700 ms");
    }

    void MainWindow::ShowMenu_Click(IInspectable const&, RoutedEventArgs const&)
    {
        POINT point{};
        GetCursorPos(&point);
        m_menuFlyout.ShowAt(point.x, point.y);
        StatusText().Text(L"Menu flyout is open");
    }

    void MainWindow::ShowTray_Click(IInspectable const&, RoutedEventArgs const&)
    {
        if (m_trayIcon.IsVisible())
        {
            m_trayIcon.Hide();
            StatusText().Text(L"Tray icon hidden");
        }
        else
        {
            m_trayIcon.Show();
            StatusText().Text(L"Tray icon shown");
        }
    }

    void MainWindow::NavigateFocus_Click(IInspectable const&, RoutedEventArgs const&)
    {
        m_flyout.NavigateFocus();
        StatusText().Text(L"Focus navigation requested");
    }

    void MainWindow::FlyoutExample_SelectionChanged(IInspectable const&, SelectionChangedEventArgs const&)
    {
        if (m_flyout)
        {
            ConfigureFlyoutForExample();
        }
    }

    void MainWindow::FlyoutPlacement_SelectionChanged(IInspectable const&, SelectionChangedEventArgs const&)
    {
        if (m_flyout)
        {
            m_flyout.Placement(PlacementFromIndex(
                std::clamp(FlyoutPlacementComboBox().SelectedIndex(), 0, 7)));
        }
    }

    void MainWindow::PopupDirection_SelectionChanged(IInspectable const&, SelectionChangedEventArgs const&)
    {
        if (m_flyout)
        {
            m_flyout.PopupDirection(DirectionFromIndex(
                std::clamp(PopupDirectionComboBox().SelectedIndex(), 0, 5)));
        }
    }

    void MainWindow::ActivationMode_SelectionChanged(IInspectable const&, SelectionChangedEventArgs const&)
    {
        if (m_flyout)
        {
            m_flyout.ActivationMode(static_cast<winrt::DesktopFlyouts::DesktopFlyoutActivationMode>(
                std::clamp(ActivationModeComboBox().SelectedIndex(), 0, 2)));
        }
    }

    void MainWindow::BackdropKind_SelectionChanged(IInspectable const&, SelectionChangedEventArgs const&)
    {
        if (m_flyout)
        {
            m_flyout.BackdropKind(BackdropKindComboBox().SelectedIndex() == 0
                ? winrt::DesktopFlyouts::DesktopFlyoutBackdropKind::mica
                : winrt::DesktopFlyouts::DesktopFlyoutBackdropKind::desktop_acrylic);
        }
    }

    void MainWindow::FlyoutSize_ValueChanged(NumberBox const&, NumberBoxValueChangedEventArgs const&)
    {
        if (m_flyout)
        {
            ApplySettings();
        }
    }

    void MainWindow::Settings_Click(IInspectable const&, RoutedEventArgs const&)
    {
        if (m_flyout)
        {
            ApplySettings();
        }
    }
}
