// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;

namespace DesktopFlyouts;

#if !HAS_UNO
[WinRT.GeneratedWinRTExposedType]
#endif
internal sealed partial class DesktopFlyoutIslandCollection(DesktopFlyout owner) : ObservableCollection<object>
{
	private void CheckWritable()
	{
		if (owner.IslandsSource is not null)
			throw new InvalidOperationException("Islands cannot be modified while IslandsSource is set.");
	}
	protected override void InsertItem(int index, object item) { CheckWritable(); base.InsertItem(index, item); }
	protected override void SetItem(int index, object item) { CheckWritable(); base.SetItem(index, item); }
	protected override void RemoveItem(int index) { CheckWritable(); base.RemoveItem(index); }
	protected override void ClearItems() { CheckWritable(); base.ClearItems(); }
	protected override void MoveItem(int oldIndex, int newIndex) { CheckWritable(); base.MoveItem(oldIndex, newIndex); }
}
