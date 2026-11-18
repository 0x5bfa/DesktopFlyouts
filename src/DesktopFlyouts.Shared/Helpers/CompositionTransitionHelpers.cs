// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Numerics;

#if UWP
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
#elif WASDK
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
#endif

namespace DesktopFlyouts;

internal static class CompositionTransitionHelpers
{
	internal static CompositionAnimationOperation CreateFlyoutTranslationAnimation(
		UIElement target,
		string targetProperty,
		float from,
		float to,
		bool opening,
		bool isVertical)
	{
		var duration = opening
			? isVertical ? TimeSpan.FromMilliseconds(267) : TimeSpan.FromMilliseconds(167)
			: isVertical ? TimeSpan.FromMilliseconds(200) : TimeSpan.FromMilliseconds(167);
		var controlPoint1 = opening ? new Vector2(0.1F, 0.9F) : new Vector2(0.2F, 0.0F);
		var controlPoint2 = opening ? new Vector2(0.4F, 1.0F) : new Vector2(0.9F, 0.0F);
		return CreateTranslationAnimation(target, targetProperty, from, to, duration, controlPoint1, controlPoint2);
	}

	internal static CompositionAnimationOperation CreateTranslationAnimation(
		UIElement target,
		string targetProperty,
		float from,
		float to,
		TimeSpan duration,
		Vector2 controlPoint1,
		Vector2 controlPoint2)
	{
		var compositor = ElementCompositionPreview.GetElementVisual(target).Compositor;
		var animation = compositor.CreateScalarKeyFrameAnimation();
		var easing = compositor.CreateCubicBezierEasingFunction(controlPoint1, controlPoint2);
		animation.InsertKeyFrame(0.0F, from);
		animation.InsertKeyFrame(1.0F, to, easing);
		animation.Duration = duration;
		animation.Target = targetProperty;
		return new(target, animation, duration);
	}

	internal static CompositionAnimationOperation CreateScaleAnimation(
		UIElement target,
		Vector3 from,
		Vector3 to,
		TimeSpan duration)
	{
		var compositor = ElementCompositionPreview.GetElementVisual(target).Compositor;
		var animation = compositor.CreateVector3KeyFrameAnimation();
		var easing = compositor.CreateCubicBezierEasingFunction(new(0.16F, 0.0F), new(0.3F, 1.0F));
		animation.InsertKeyFrame(0.0F, from);
		animation.InsertKeyFrame(1.0F, to, easing);
		animation.Duration = duration;
		animation.Target = "Scale";
		return new(target, animation, duration);
	}
}

internal sealed class CompositionAnimationOperation : IDisposable
{
	private readonly UIElement _target;
	private readonly CompositionAnimation _animation;
#if HAS_UNO
	// Uno's Skia compositor does not implement CompositionScopedBatch completion.
	private readonly DispatcherTimer _completionTimer;
#else
	private readonly CompositionScopedBatch _batch;
#endif
	private bool _isStarted;
	private bool _isDisposed;

	internal CompositionAnimationOperation(UIElement target, CompositionAnimation animation, TimeSpan duration)
	{
		_target = target;
		_animation = animation;
#if HAS_UNO
		_completionTimer = new DispatcherTimer { Interval = duration };
		_completionTimer.Tick += CompletionTimer_Tick;
#else
		var compositor = ElementCompositionPreview.GetElementVisual(target).Compositor;
		_batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
		_batch.Completed += Batch_Completed;
#endif
	}

	internal event EventHandler? Completed;

	internal void Start()
	{
		if (_isDisposed || _isStarted)
		{
			return;
		}

		_isStarted = true;
		_target.StartAnimation(_animation);
#if HAS_UNO
		_completionTimer.Start();
#else
		_batch.End();
#endif
	}

#if HAS_UNO
	private void CompletionTimer_Tick(object? sender, object e)
	{
		_completionTimer.Stop();
		if (!_isDisposed)
		{
			Completed?.Invoke(this, EventArgs.Empty);
		}
	}
#else
	private void Batch_Completed(object? sender, CompositionBatchCompletedEventArgs e)
	{
		if (!_isDisposed)
		{
			Completed?.Invoke(this, EventArgs.Empty);
		}
	}
#endif

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;
#if HAS_UNO
		_completionTimer.Tick -= CompletionTimer_Tick;
		_completionTimer.Stop();
#else
		_batch.Completed -= Batch_Completed;
#endif
		_target.StopAnimation(_animation);
#if !HAS_UNO
		_batch.Dispose();
#endif
		_animation.Dispose();
	}
}
