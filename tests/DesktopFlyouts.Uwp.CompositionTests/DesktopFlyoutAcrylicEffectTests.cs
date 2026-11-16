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
    public sealed class DesktopFlyoutAcrylicEffectTests
    {
        [TestMethod]
        [DataRow(32, 32, 32, 0.5, 0.96)]
        [DataRow(243, 243, 243, 0.0, 0.9)]
        [DataRow(25, 55, 85, 0.8, 0.8)]
        public async Task NativeCompositorAcceptsAcrylicGraphAndBothInputs(
            int red, int green, int blue, double tintOpacity, double luminosityOpacity)
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
                        using var backdrop = compositor.CreateHostBackdropBrush();
                        using var noise = compositor.CreateColorBrush(Colors.Gray);
                        using var brush = DesktopFlyoutAcrylicEffect.Create(
                            compositor, backdrop, noise, Color.FromArgb(255, (byte)red, (byte)green, (byte)blue),
                            tintOpacity, luminosityOpacity);

                        Assert.AreEqual(backdrop, brush.GetSourceParameter("Backdrop"));
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
