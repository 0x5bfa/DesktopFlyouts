# Focus and activation

`DesktopFlyout.ActivationMode` controls how the flyout interacts with the foreground window.

## Modes

| Mode | Behavior |
| --- | --- |
| `Activate` | Activates the flyout when it opens. This is the default. |
| `NoActivateOnOpen` | Opens without keeping activation on the flyout. The previous foreground window is restored after opening. |
| `NeverActivate` | Prevents the flyout and its XAML island windows from becoming active or focused. |

## Choosing a mode

Use `Activate` when the user should interact with controls inside the flyout by keyboard immediately.

```xml
<desktop:DesktopFlyout ActivationMode="Activate" />
```

Use `NoActivateOnOpen` for tray surfaces that should appear without stealing foreground activation, but may still be clicked later.

```xml
<desktop:DesktopFlyout ActivationMode="NoActivateOnOpen" />
```

Use `NeverActivate` for passive UI that should not receive focus.

```xml
<desktop:DesktopFlyout ActivationMode="NeverActivate" />
```

When `NeverActivate` is used, `NavigateFocus` cannot move focus into the flyout and the control suppresses focusable states inside the hosted content.

## Hide on lost focus

`HideOnLostFocus` defaults to `True`. It closes the flyout when the host window is deactivated.

```xml
<desktop:DesktopFlyout HideOnLostFocus="True" />
```

Set it to `False` for sticky flyouts.

```xml
<desktop:DesktopFlyout HideOnLostFocus="False" />
```

## UWP message loop integration

Start UWP desktop hosts with `XamlIslandApplication.Start`. XamlHostingKit owns the CoreWindow
message loop and performs XAML message translation, so application code must not run a second
native message loop. `TryPreTranslateMessage` remains available for source compatibility and
returns `False` with the XamlHostingKit host.
