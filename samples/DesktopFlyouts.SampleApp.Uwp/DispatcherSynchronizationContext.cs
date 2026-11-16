// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading;
using Windows.UI.Core;

namespace DesktopFlyouts;

internal sealed class DispatcherSynchronizationContext(CoreDispatcher dispatcher) : SynchronizationContext
{
	public override void Post(SendOrPostCallback callback, object? state)
	{
		_ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => callback(state));
	}

	public override void Send(SendOrPostCallback callback, object? state)
	{
		if (!dispatcher.HasThreadAccess)
			throw new NotSupportedException("Cross-thread synchronous dispatch is not supported.");

		callback(state);
	}

    public override SynchronizationContext CreateCopy()
    {
        return new DispatcherSynchronizationContext(dispatcher);
    }
}
