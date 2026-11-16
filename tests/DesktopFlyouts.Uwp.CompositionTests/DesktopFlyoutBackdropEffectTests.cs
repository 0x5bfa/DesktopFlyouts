// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.System;
using Windows.UI;
using Windows.UI.Composition;

namespace DesktopFlyouts.Tests
{
    [TestClass]
    public sealed class DesktopFlyoutBackdropEffectTests
    {
        [TestMethod]
        [DataRow(false, 32, 32, 32, 0.5, 0.96)]
        [DataRow(false, 243, 243, 243, 0.0, 0.9)]
        [DataRow(false, 25, 55, 85, 0.8, 0.8)]
        [DataRow(true, 32, 32, 32, 0.5, 0.96)]
        [DataRow(true, 243, 243, 243, 0.0, 0.9)]
        [DataRow(true, 25, 55, 85, 0.8, 0.8)]
        public async Task NativeCompositorAcceptsMaterialGraphAndInputs(
            bool useMica, int red, int green, int blue, double tintOpacity, double luminosityOpacity)
        {
            // A dedicated Composition dispatcher is sufficient; no XAML application or HWND is created.
            var controller = DispatcherQueueController.CreateOnDedicatedThread();
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                Assert.IsTrue(controller.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        using var compositor = new Compositor();
                        // A color brush substitutes only the wallpaper pixels: no desktop capture is needed
                        // to validate the native Mica graph and its source connection.
                        using CompositionBrush backdrop = useMica
                            ? compositor.CreateColorBrush(Colors.Navy)
                            : compositor.CreateHostBackdropBrush();
                        using var noise = compositor.CreateColorBrush(Colors.Gray);
                        var tintColor = Color.FromArgb(255, (byte)red, (byte)green, (byte)blue);
                        using var brush = useMica
                            ? DesktopFlyoutMicaEffect.Create(compositor, backdrop, tintColor, tintOpacity, luminosityOpacity)
                            : DesktopFlyoutAcrylicEffect.Create(compositor, backdrop, noise, tintColor, tintOpacity, luminosityOpacity);

                        Assert.AreEqual(backdrop, brush.GetSourceParameter(useMica ? "BlurredWallpaperBackdrop" : "Backdrop"));
                        if (!useMica)
                            Assert.AreEqual(noise, brush.GetSourceParameter("Noise"));
                        completion.SetResult();
                    }
                    catch (Exception exception)
                    {
                        completion.SetException(exception);
                    }
                }));
                await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
            }
            finally
            {
                await controller.ShutdownQueueAsync();
            }
        }
    }
}
