// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
#endif

namespace U5BFA.Libraries
{
    internal static class TransitionHelpers
    {
        internal static Storyboard GetWindows11BottomToTopTransitionStoryboard(DependencyObject target, double from, double to)
        {
            var storyboard = new Storyboard();

            var keyFrames = new DoubleAnimationUsingKeyFrames() { EnableDependentAnimation = true };
            keyFrames.KeyFrames.Add(new DiscreteDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)),
                Value = from,
            });

            keyFrames.KeyFrames.Add(new SplineDoubleKeyFrame()
            {
                KeySpline = new() { ControlPoint1 = new(0.1, 0.9), ControlPoint2 = new(0.4, 1.0) },
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(267)),
                Value = to,
            });

            Storyboard.SetTarget(keyFrames, target);
            Storyboard.SetTargetProperty(keyFrames, "TranslateY");

            storyboard.Children.Add(keyFrames);

            return storyboard;
        }

        internal static Storyboard GetWindows11TopToBottomTransitionStoryboard(DependencyObject target, double from, double to)
        {
            var storyboard = new Storyboard();

            var keyFrames = new DoubleAnimationUsingKeyFrames() { EnableDependentAnimation = true };
            keyFrames.KeyFrames.Add(new DiscreteDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)),
                Value = from,
            });

            keyFrames.KeyFrames.Add(new SplineDoubleKeyFrame()
            {
                KeySpline = new() { ControlPoint1 = new(0.2, 0.0), ControlPoint2 = new(0.9, 0.0) },
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
                Value = to,
            });

            Storyboard.SetTarget(keyFrames, target);
            Storyboard.SetTargetProperty(keyFrames, "TranslateY");

            storyboard.Children.Add(keyFrames);

            return storyboard;
        }

        internal static Storyboard GetWindows11RightToLeftTransitionStoryboard(DependencyObject target, double from, double to)
        {
            var storyboard = new Storyboard();

            var keyFrames = new DoubleAnimationUsingKeyFrames() { EnableDependentAnimation = true };
            keyFrames.KeyFrames.Add(new DiscreteDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)),
                Value = from,
            });

            keyFrames.KeyFrames.Add(new SplineDoubleKeyFrame()
            {
                KeySpline = new() { ControlPoint1 = new(0.1, 0.9), ControlPoint2 = new(0.4, 1.0) },
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(167)),
                Value = to,
            });

            Storyboard.SetTarget(keyFrames, target);
            Storyboard.SetTargetProperty(keyFrames, "TranslateX");

            storyboard.Children.Add(keyFrames);

            return storyboard;
        }

        internal static Storyboard GetWindows11LeftToRightTransitionStoryboard(DependencyObject target, double from, double to)
        {
            var storyboard = new Storyboard();

            var keyFrames = new DoubleAnimationUsingKeyFrames() { EnableDependentAnimation = true };
            keyFrames.KeyFrames.Add(new DiscreteDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)),
                Value = from,
            });

            keyFrames.KeyFrames.Add(new SplineDoubleKeyFrame()
            {
                KeySpline = new() { ControlPoint1 = new(0.2, 0.0), ControlPoint2 = new(0.9, 0.0) },
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(167)),
                Value = to,
            });

            Storyboard.SetTarget(keyFrames, target);
            Storyboard.SetTargetProperty(keyFrames, "TranslateX");

            storyboard.Children.Add(keyFrames);

            return storyboard;
        }

        internal static Storyboard GetPressedScaleTransitionStoryboard(
            DependencyObject target,
            double fromScaleX,
            double fromScaleY,
            double toScale,
            TimeSpan duration)
        {
            var storyboard = new Storyboard();
            AddScaleAnimation(storyboard, target, "ScaleX", fromScaleX, toScale, duration);
            AddScaleAnimation(storyboard, target, "ScaleY", fromScaleY, toScale, duration);

            return storyboard;
        }

        private static void AddScaleAnimation(
            Storyboard storyboard,
            DependencyObject target,
            string property,
            double fromScale,
            double toScale,
            TimeSpan duration)
        {
            var keyFrames = new DoubleAnimationUsingKeyFrames() { EnableDependentAnimation = true };
            keyFrames.KeyFrames.Add(new DiscreteDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
                Value = fromScale,
            });

            keyFrames.KeyFrames.Add(new SplineDoubleKeyFrame()
            {
                KeySpline = new() { ControlPoint1 = new(0.16, 0.0), ControlPoint2 = new(0.3, 1.0) },
                KeyTime = KeyTime.FromTimeSpan(duration),
                Value = toScale,
            });

            Storyboard.SetTarget(keyFrames, target);
            Storyboard.SetTargetProperty(keyFrames, property);

            storyboard.Children.Add(keyFrames);
        }
    }
}
