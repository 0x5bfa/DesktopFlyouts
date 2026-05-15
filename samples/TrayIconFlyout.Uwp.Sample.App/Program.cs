// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System.Threading;
using Windows.System;
using Windows.UI.Xaml;

namespace U5BFA.Libraries
{
	public static class Program
	{
		static void Main(string[] args)
		{
			Application.Start((p) =>
			{
				var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
				SynchronizationContext.SetSynchronizationContext(context);
				new App();
			});
		}
	}
}
