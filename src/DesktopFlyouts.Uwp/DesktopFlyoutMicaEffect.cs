// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Graphics.Canvas.Effects;
using Windows.UI;
using Windows.UI.Composition;

namespace DesktopFlyouts;

internal static class DesktopFlyoutMicaEffect
{
	internal static CompositionEffectBrush Create(Compositor compositor, CompositionBrush wallpaper, Color tintColor, double tintOpacity, double luminosityOpacity)
	{
		using var tint = new ColorSourceEffect()
		{
			Color = tintColor
		};

		using var luminosityTint = new ColorSourceEffect()
		{
			Color = tintColor
		};

		using var luminosityColor = new OpacityEffect()
		{
			Source = luminosityTint,
			Opacity = (float)luminosityOpacity
		};

		using var tintOpacityEffect = new OpacityEffect()
		{
			Source = tint,
			Opacity = (float)tintOpacity
		};

		// Direct2D's Color and Luminosity blend mode names are reversed in this recipe.
		using var luminosity = new BlendEffect()
		{
			Mode = BlendEffectMode.Color,
			Background = new CompositionEffectSourceParameter("BlurredWallpaperBackdrop"),
			Foreground = luminosityColor,
		};

		using var color = new BlendEffect()
		{
			Mode = BlendEffectMode.Luminosity,
			Background = luminosity,
			Foreground = tintOpacityEffect,
		};

		using var factory = compositor.CreateEffectFactory(color);
		var brush = factory.CreateBrush();

		try
		{
			brush.SetSourceParameter("BlurredWallpaperBackdrop", wallpaper);
			return brush;
		}
		catch
		{
			brush.Dispose();
			throw;
		}
	}
}
