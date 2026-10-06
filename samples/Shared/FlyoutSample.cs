#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
#endif
using System.Runtime.InteropServices;

namespace DesktopFlyoutsSample;

internal sealed record FlyoutSettings(
    int Example, int Placement, int Direction, int Activation, int Backdrop,
    double Width, double Height, double SwipeThreshold, double AutoCloseSeconds,
    bool EnableBackdrop, bool HideOnLostFocus, bool Swipe, bool Transition);

// Compile against each platform's projection so the examples exercise the public C# API.
internal sealed partial class FlyoutSample : IDisposable
{
    private readonly DesktopFlyouts.DesktopFlyout _flyout;
    private readonly DesktopFlyouts.DesktopMenuFlyout _menu;
    private readonly DesktopFlyouts.SystemTrayIcon _tray;
    private readonly Action<string> _status;
    private readonly Func<FlyoutSettings> _settings;
    private bool _disposed;

    public FlyoutSample(long ownerWindow, Func<FlyoutSettings> settings, Action<string> status, Action close)
    {
        _settings = settings;
        _status = status;
        _flyout = new DesktopFlyouts.DesktopFlyout
        {
            OwnerWindowHandle = ownerWindow,
            Margin = new Thickness(12),
        };
        _menu = new DesktopFlyouts.DesktopMenuFlyout { OwnerWindowHandle = ownerWindow };
        AddMenuItem("Show selected flyout", Show);
        AddMenuItem("Hide flyout", Hide);
        AddMenuItem("Close sample", close);
#if UWP
        var trayId = new Guid("3cae5f6e-c2c6-4f35-a595-af3ea40a1776");
        const string tooltip = "DesktopFlyouts C# UWP sample";
#else
        var trayId = new Guid("28de460a-8bd6-4539-a406-5f685584fd4d");
        const string tooltip = "DesktopFlyouts C# Windows App SDK sample";
#endif
        _tray = new DesktopFlyouts.SystemTrayIcon("", tooltip, trayId);
        _tray.LeftClicked += TrayLeftClicked;
        _tray.RightClicked += TrayRightClicked;
        _tray.Show();
        ApplySettings();
    }

    private void AddMenuItem(string text, Action action)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => action();
        _menu.Items.Add(item);
    }

    public void ApplySettings()
    {
        var settings = _settings();
        _flyout.FlyoutWidth = Length(settings.Width);
        _flyout.FlyoutHeight = Length(settings.Height);
        _flyout.AutoCloseDelay = TimeSpan.FromSeconds(settings.AutoCloseSeconds);
        _flyout.IsBackdropEnabled = settings.EnableBackdrop;
        _flyout.HideOnLostFocus = settings.HideOnLostFocus;
        _flyout.IsSwipeToDismissEnabled = settings.Swipe;
        _flyout.IsTransitionAnimationEnabled = settings.Transition;
        _flyout.PressedScale = settings.Swipe ? 0.96 : 1;
        _flyout.SwipeDismissThreshold = settings.SwipeThreshold;
        _flyout.BackdropKind = settings.Backdrop == 0
            ? DesktopFlyouts.DesktopFlyoutBackdropKind.Mica
            : DesktopFlyouts.DesktopFlyoutBackdropKind.DesktopAcrylic;
        _flyout.ActivationMode = (DesktopFlyouts.DesktopFlyoutActivationMode)settings.Activation;
        _flyout.Placement = settings.Placement switch
        {
            0 => DesktopFlyouts.DesktopFlyoutPlacementMode.TopLeft,
            1 => DesktopFlyouts.DesktopFlyoutPlacementMode.TopCenter,
            2 => DesktopFlyouts.DesktopFlyoutPlacementMode.TopRight,
            3 => DesktopFlyouts.DesktopFlyoutPlacementMode.BottomLeft,
            4 => DesktopFlyouts.DesktopFlyoutPlacementMode.BottomCenter,
            6 => DesktopFlyouts.DesktopFlyoutPlacementMode.LeftCenter,
            7 => DesktopFlyouts.DesktopFlyoutPlacementMode.RightCenter,
            _ => DesktopFlyouts.DesktopFlyoutPlacementMode.BottomRight,
        };
        _flyout.PopupDirection = settings.Direction switch
        {
            1 => DesktopFlyouts.DesktopFlyoutPopupDirection.BottomToTop,
            2 => DesktopFlyouts.DesktopFlyoutPopupDirection.TopToBottom,
            3 => DesktopFlyouts.DesktopFlyoutPopupDirection.Horizontal,
            4 => DesktopFlyouts.DesktopFlyoutPopupDirection.LeftToRight,
            5 => DesktopFlyouts.DesktopFlyoutPopupDirection.RightToLeft,
            _ => DesktopFlyouts.DesktopFlyoutPopupDirection.Vertical,
        };
    }

    private static GridLength Length(double value) => value <= 0 ? GridLength.Auto : new GridLength(value);

    private void ResetContent()
    {
        _flyout.Hide();
        _flyout.Content = null;
        _flyout.Islands.Clear();
        ApplySettings();
    }

    private void ConfigureContent()
    {
        ResetContent();
        var example = _settings().Example;
        if (example is 4 or 5 or 6)
        {
            _flyout.FlyoutWidth = new GridLength(example switch { 4 => 520, 5 => 280, _ => 720 });
            _flyout.FlyoutHeight = new GridLength(example switch { 4 => 560, 5 => 220, _ => 360 });
        }
        _flyout.Content = CreateCard(example);
    }

    public void Show()
    {
        ConfigureContent();
        _flyout.Show();
        _status("Selected flyout requested");
    }

    public void Hide()
    {
        _flyout.Hide();
        _status("Flyout is closed");
    }

    public void ShowIslands()
    {
        ResetContent();
        _flyout.FlyoutWidth = new GridLength(420);
        _flyout.FlyoutHeight = new GridLength(300);
        _flyout.IslandsOrientation = Orientation.Vertical;
        _flyout.IslandSpacing = 8;
        _flyout.PressedScale = 0.96;
        _flyout.IsSwipeToDismissEnabled = true;
        _flyout.SwipeDismissThreshold = 60;
        _flyout.IsTransitionAnimationEnabled = false;
        _flyout.Islands.Add(new DesktopFlyouts.DesktopFlyoutIsland { Content = CreateIslandContent(false) });
        _flyout.Islands.Add(new DesktopFlyouts.DesktopFlyoutIsland { Content = CreateIslandContent(true) });
        _flyout.Show();
        _status("Flyout islands requested");
    }

    public void ShowMenu()
    {
        if (!GetCursorPos(out var point))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
        }
        _menu.ShowAt(point.X, point.Y);
        _status("Menu flyout is open");
    }

    public void ToggleTray()
    {
        if (_tray.IsVisible) _tray.Hide();
        else _tray.Show();
        _status(_tray.IsVisible ? "Tray icon shown" : "Tray icon hidden");
    }

    public void NavigateFocus()
    {
        _flyout.NavigateFocus();
        _status("Focus navigation requested");
    }

#if UWP
    public bool TryPreTranslateMessage(nint message)
        => _flyout.TryPreTranslateMessage(message) || _menu.TryPreTranslateMessage(message);
#endif

    private void TrayLeftClicked(object? sender, DesktopFlyouts.MouseEventReceivedEventArgs args)
    {
        ConfigureContent();
        _flyout.Show(args.Point);
        _status("Flyout opened from the tray icon");
    }

    private void TrayRightClicked(object? sender, DesktopFlyouts.MouseEventReceivedEventArgs args)
    {
        _menu.ShowAt((int)args.Point.X, (int)args.Point.Y - 32);
        _status("Tray menu opened");
    }

    private UIElement CreateIslandContent(bool second)
    {
        var card = (Border)XamlReader.Load($$"""
            <Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    Padding="12,10" Background="{ThemeResource SystemControlBackgroundChromeMediumLowBrush}">
                <StackPanel Spacing="8">
                    <TextBlock Text="Native island {{(second ? "two" : "one")}}"
                               AutomationProperties.AutomationId="{{(second ? "IslandTwoText" : "IslandOneText")}}" />
                    {{(second ? """<Button Content="Close" x:Name="CloseButton" AutomationProperties.AutomationId="IslandsCloseButton" />""" : "")}}
                </StackPanel>
            </Border>
            """);
        if (second) ((Button)card.FindName("CloseButton")).Click += (_, _) => Hide();
        return card;
    }

    private UIElement CreateCard(int example)
    {
        var (heading, description, content) = example switch
        {
            1 => ("Button", "A compact action flyout using standard buttons.",
                """<Button Content="Primary action" x:Name="PrimaryAction" AutomationProperties.AutomationId="ExampleButtonAction" />"""),
            2 => ("Indicator", "Progress and state controls remain ordinary XAML controls.",
                """<ProgressBar Value="68" /><ToggleSwitch Header="Ready" IsOn="True" AutomationProperties.AutomationId="IndicatorToggle" />"""),
            3 => ("Notification Center", "A stacked notification surface with dismissible actions.",
                """<TextBlock Text="DesktopFlyouts notification" TextWrapping="Wrap" /><Button Content="Dismiss" x:Name="PrimaryAction" AutomationProperties.AutomationId="NotificationCloseButton" />"""),
            4 => ("Start Menu", "A launcher scenario with search and pinned actions.",
                """<TextBox Header="Search apps" PlaceholderText="Search apps" AutomationProperties.AutomationId="StartMenuSearchBox" /><ListView MaxHeight="120" AutomationProperties.AutomationId="PinnedApps"><x:String>Files</x:String><x:String>Settings</x:String><x:String>Terminal</x:String></ListView>"""),
            5 => ("Sticky small", "A small always-available utility flyout.",
                """<Button Content="Pin" x:Name="PrimaryAction" AutomationProperties.AutomationId="StickyPinButton" />"""),
            6 => ("Widget", "A wider dashboard-like flyout with independent cards.",
                """<Grid><Grid.ColumnDefinitions><ColumnDefinition /><ColumnDefinition /></Grid.ColumnDefinitions><StackPanel Spacing="8"><TextBlock Text="CPU 42%" /><ProgressBar Value="42" /></StackPanel><TextBlock Grid.Column="1" Text="Memory 68%" /></Grid>"""),
            7 => ("Severity", "An alert-style scenario for warning and error presentation.",
                """<TextBlock Text="Warning: review the current settings." TextWrapping="Wrap" />"""),
            _ => ("Default", "A C# XAML surface hosted by the native DesktopFlyout component.", ""),
        };
        var card = (Border)XamlReader.Load($$"""
            <Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    Padding="20,16" CornerRadius="12"
                    Background="{ThemeResource SystemControlBackgroundChromeMediumLowBrush}"
                    AutomationProperties.AutomationId="DesktopFlyoutSurface">
                <StackPanel Spacing="8">
                    <TextBlock Text="{{heading}}" FontSize="20" FontWeight="SemiBold" AutomationProperties.AutomationId="FlyoutTitle" />
                    <TextBlock Text="{{description}}" TextWrapping="Wrap" AutomationProperties.AutomationId="FlyoutDescription" />
                    {{content}}
                    <Button Content="Close" x:Name="CloseButton" AutomationProperties.AutomationId="CloseFlyoutButton" />
                </StackPanel>
            </Border>
            """);
        ((Button)card.FindName("CloseButton")).Click += (_, _) => Hide();
        if (card.FindName("PrimaryAction") is Button action) action.Click += (_, _) => Hide();
        return card;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _tray.LeftClicked -= TrayLeftClicked;
        _tray.RightClicked -= TrayRightClicked;
        _tray.Dispose();
        _menu.Dispose();
        _flyout.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CursorPoint { public int X; public int Y; }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out CursorPoint point);
}
