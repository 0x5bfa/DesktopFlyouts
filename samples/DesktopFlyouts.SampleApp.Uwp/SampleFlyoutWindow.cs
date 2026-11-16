// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using DesktopFlyouts.Shared;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace DesktopFlyouts;

// Creates flyout controls and dispatches their operations on the application's XAML thread.
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
        return Task.FromResult(new SampleFlyoutWindow<T>(factory()));
    }

    internal bool TryPreTranslateMessage(nint message) => _content switch
    {
        DesktopFlyout flyout => flyout.TryPreTranslateMessage(message),
        DesktopMenuFlyout menu => menu.TryPreTranslateMessage(message),
        _ => false,
    };

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
