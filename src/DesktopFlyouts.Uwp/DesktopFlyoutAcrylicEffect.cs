// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Windows.UI;
using Windows.UI.Composition;

namespace DesktopFlyouts;

internal static class DesktopFlyoutAcrylicEffect
{
	internal static CompositionEffectBrush Create(
		Compositor compositor, CompositionBrush backdrop, CompositionBrush noise,
		Color tintColor, double tintOpacity, double luminosityOpacity)
	{
		using var luminosityColor = new ColorSourceEffect
		{
			Color = Color.FromArgb((byte)Math.Round(255 * luminosityOpacity), tintColor.R, tintColor.G, tintColor.B),
		};
		using var tint = new ColorSourceEffect
		{
			Color = Color.FromArgb((byte)Math.Round(255 * tintOpacity), tintColor.R, tintColor.G, tintColor.B),
		};
		// Direct2D's Color and Luminosity blend mode names are reversed in this recipe.
		using var luminosity = new BlendEffect
		{
			Mode = BlendEffectMode.Color,
			Background = new CompositionEffectSourceParameter("Backdrop"),
			Foreground = luminosityColor,
		};
		using var color = new BlendEffect
		{
			Mode = BlendEffectMode.Luminosity,
			Background = luminosity,
			Foreground = tint,
		};
		using var noiseBorder = new BorderEffect
		{
			Source = new CompositionEffectSourceParameter("Noise"),
			ExtendX = CanvasEdgeBehavior.Wrap,
			ExtendY = CanvasEdgeBehavior.Wrap,
		};
		using var noiseOpacity = new OpacityEffect { Source = noiseBorder, Opacity = 0.02F };
		using var effect = new BlendEffect
		{
			Mode = BlendEffectMode.Overlay,
			Background = color,
			Foreground = noiseOpacity,
		};
		using var factory = compositor.CreateEffectFactory(effect);
		var brush = factory.CreateBrush();
		try
		{
			brush.SetSourceParameter("Backdrop", backdrop);
			brush.SetSourceParameter("Noise", noise);
			return brush;
		}
		catch
		{
			brush.Dispose();
			throw;
		}
	}
}
