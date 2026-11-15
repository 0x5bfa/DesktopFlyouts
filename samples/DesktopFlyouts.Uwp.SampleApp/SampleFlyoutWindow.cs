// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using DesktopFlyouts.Shared;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.Win32;
using Windows.Win32.Foundation;
using XamlHostingKit;
using WinRT.Interop;

namespace DesktopFlyouts
{
    // Sample-only coordination until the library provides an asynchronous host API.
    internal sealed class SampleFlyoutWindow<T> where T : Control, IDisposable
    {
        private readonly T _content;
        private readonly CoreDispatcher _dispatcher;

        private SampleFlyoutWindow(T content)
        {
            _content = content;
            _dispatcher = content.Dispatcher;
        }

        internal static Task<SampleFlyoutWindow<T>> CreateAsync(Func<T> factory)
        {
            var completion = new TaskCompletionSource<SampleFlyoutWindow<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                XamlIslandApplication.CreateWindow(_ =>
                {
                    try
                    {
                        completion.SetResult(new(factory()));
                    }
                    catch (Exception exception)
                    {
                        // Failed construction must not leave an empty window's message loop running.
                        PInvoke.DestroyWindow((HWND)WindowNative.GetWindowHandle(XamlWindow.Current));
                        completion.SetException(exception);
                    }
                });
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }

            return completion.Task;
        }

        internal async Task InvokeAsync(Action<T> action)
        {
            Exception? failure = null;
            var invoked = false;
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                invoked = true;
                try
                {
                    action(_content);
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            });

            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
            if (!invoked)
                throw new InvalidOperationException("The flyout dispatcher stopped before handling the request.");
        }

        internal Task DisposeAsync() => InvokeAsync(content => content.Dispose());
    }
}
