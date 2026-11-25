// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.DirectX;
using Windows.System.Power;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace DesktopFlyouts;

// Each island owns its material brush rather than targeting Window.Current with BackdropMaterial.
// Acrylic samples the host backdrop; Mica samples only the desktop wallpaper.
internal sealed partial class DesktopFlyoutBackdropBrush : XamlCompositionBrushBase
{
	private readonly bool _canUseHostBackdrop;
	private readonly CoreDispatcher _dispatcher;
	private readonly UISettings _uiSettings = new();
	private readonly AccessibilitySettings _accessibilitySettings = new();
	private CompositionCapabilities? _capabilities;
	private CompositionBackdropBrush? _backdrop;
	private CompositionSurfaceBrush? _noiseBrush;
	private CompositionDrawingSurface? _noiseSurface;
	private CompositionGraphicsDevice? _noiseGraphicsDevice;
	private volatile bool _connected;

	internal bool UseLightTheme { get; }
	internal bool UseAccent { get; }
	internal DesktopFlyoutBackdropKind Kind { get; }

	internal DesktopFlyoutBackdropBrush(DesktopFlyoutBackdropKind kind, bool useLightTheme, bool useAccent, bool canUseHostBackdrop)
	{
		Kind = kind;
		UseLightTheme = useLightTheme;
		UseAccent = useAccent;
		_canUseHostBackdrop = canUseHostBackdrop;
		_dispatcher = Dispatcher;
		UpdateFallbackColor(GetPalette().FallbackColor);
	}

	protected override void OnConnected()
	{
		if (_connected)
		{
			return;
		}

		_connected = true;
		_capabilities = CompositionCapabilities.GetForCurrentView();
		_capabilities.Changed += Capabilities_Changed;
		_uiSettings.AdvancedEffectsEnabledChanged += UISettings_AdvancedEffectsEnabledChanged;
		_uiSettings.ColorValuesChanged += UISettings_ColorValuesChanged;
		_accessibilitySettings.HighContrastChanged += AccessibilitySettings_HighContrastChanged;
		PowerManager.EnergySaverStatusChanged += PowerManager_EnergySaverStatusChanged;
		UpdateBrush();
	}

	protected override void OnDisconnected()
	{
		_connected = false;
		_capabilities?.Changed -= Capabilities_Changed;
		_capabilities = null;
		_uiSettings.AdvancedEffectsEnabledChanged -= UISettings_AdvancedEffectsEnabledChanged;
		_uiSettings.ColorValuesChanged -= UISettings_ColorValuesChanged;
		_accessibilitySettings.HighContrastChanged -= AccessibilitySettings_HighContrastChanged;
		PowerManager.EnergySaverStatusChanged -= PowerManager_EnergySaverStatusChanged;
		ClearCompositionResources();
	}

	private DesktopFlyoutBackdropPalette GetPalette()
	{
		var accent = UseAccent ? _uiSettings.GetColorValue(UIColorType.AccentDark2) : (Color?)null;
		return DesktopFlyoutBackdropPalette.ForTheme(UseLightTheme, accent);
	}

	private void UpdateFallbackColor(Color fallbackColor)
	{
		FallbackColor = _accessibilitySettings.HighContrast
			? _uiSettings.GetColorValue(UIColorType.Background)
			: fallbackColor;
	}

	[SupportedOSPlatformGuard("windows10.0.22000.0")]
	private static bool IsBlurredWallpaperBackdropSupported =>
		ApiInformation.IsMethodPresent("Windows.UI.Composition.Compositor", "TryCreateBlurredWallpaperBackdropBrush");

	private void UpdateBrush()
	{
		if (!_connected)
		{
			return;
		}

		var palette = GetPalette();
		UpdateFallbackColor(palette.FallbackColor);
		ClearCompositionResources();
		if ((Kind is not DesktopFlyoutBackdropKind.Mica && !_canUseHostBackdrop)
			|| _accessibilitySettings.HighContrast || !_uiSettings.AdvancedEffectsEnabled
			|| PowerManager.EnergySaverStatus is EnergySaverStatus.On
			|| _capabilities?.AreEffectsSupported() != true || !_capabilities.AreEffectsFast())
		{
			return;
		}

		try
		{
			var compositor = Window.Current.Compositor;
			if (Kind is DesktopFlyoutBackdropKind.Mica)
			{
				if (!IsBlurredWallpaperBackdropSupported)
				{
					return;
				}

				_backdrop = compositor.TryCreateBlurredWallpaperBackdropBrush();
				if (_backdrop is null)
				{
					return;
				}

				CompositionBrush = DesktopFlyoutMicaEffect.Create(
					compositor, _backdrop, palette.TintColor, palette.TintOpacity, palette.LuminosityOpacity);
			}
			else
			{
				_backdrop = compositor.CreateHostBackdropBrush();
				CreateNoiseBrush(compositor);
				CompositionBrush = DesktopFlyoutAcrylicEffect.Create(
					compositor, _backdrop, _noiseBrush!, palette.TintColor, palette.TintOpacity, palette.LuminosityOpacity);
			}
		}
		catch (COMException exception)
		{
			Debug.WriteLine($"Could not create the UWP {Kind} effect: 0x{exception.HResult:X8}.");
			ClearCompositionResources();
		}
	}

	private void CreateNoiseBrush(Compositor compositor)
	{
		_noiseGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(compositor, CanvasDevice.GetSharedDevice());
		_noiseGraphicsDevice.RenderingDeviceReplaced += NoiseGraphicsDevice_RenderingDeviceReplaced;
		_noiseSurface = _noiseGraphicsDevice.CreateDrawingSurface(
			new Size(64, 64), DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
		using (var drawing = CanvasComposition.CreateDrawingSession(_noiseSurface))
		{
			uint random = 0x5BFA;
			for (var y = 0; y < 64; y++)
			{
				for (var x = 0; x < 64; x++)
				{
					random ^= random << 13;
					random ^= random >> 17;
					random ^= random << 5;
					var value = (byte)random;
					drawing.FillRectangle(x, y, 1, 1, Color.FromArgb(255, value, value, value));
				}
			}
		}
		_noiseBrush = compositor.CreateSurfaceBrush(_noiseSurface);
		_noiseBrush.Stretch = CompositionStretch.None;
	}

	private void ClearCompositionResources()
	{
		var brush = CompositionBrush;
		CompositionBrush = null;
		brush?.Dispose();
		_backdrop?.Dispose();
		_backdrop = null;
		_noiseBrush?.Dispose();
		_noiseBrush = null;
		_noiseSurface?.Dispose();
		_noiseSurface = null;
		if (_noiseGraphicsDevice is not null)
		{
			_noiseGraphicsDevice.RenderingDeviceReplaced -= NoiseGraphicsDevice_RenderingDeviceReplaced;
			_noiseGraphicsDevice.Dispose();
		}
		_noiseGraphicsDevice = null;
	}

	private void QueueRefresh()
	{
		if (_connected)
		{
			_ = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateBrush);
		}
	}

	private void Capabilities_Changed(CompositionCapabilities sender, object args)
	{
		QueueRefresh();
	}

	private void UISettings_AdvancedEffectsEnabledChanged(UISettings sender, object args)
	{
		QueueRefresh();
	}

	private void UISettings_ColorValuesChanged(UISettings sender, object args)
	{
		QueueRefresh();
	}

	private void AccessibilitySettings_HighContrastChanged(AccessibilitySettings sender, object args)
	{
		QueueRefresh();
	}

	private void PowerManager_EnergySaverStatusChanged(object? sender, object args)
	{
		QueueRefresh();
	}

	private void NoiseGraphicsDevice_RenderingDeviceReplaced(CompositionGraphicsDevice sender, RenderingDeviceReplacedEventArgs args)
	{
		QueueRefresh();
	}
}
