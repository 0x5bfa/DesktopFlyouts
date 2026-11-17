// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System.Collections.ObjectModel;

namespace DesktopFlyouts.SampleApp.Shared;

public sealed class SampleIslandData
{
	public string Title { get; set; } = string.Empty;
	public string Detail { get; set; } = string.Empty;
}

#if !HAS_UNO
[WinRT.GeneratedWinRTExposedType]
#endif
public sealed partial class SampleIslandDataSource : ObservableCollection<SampleIslandData>
{
	public SampleIslandDataSource()
	{
		Add(new() { Title = "Data-driven island", Detail = "The item template controls this content. The library owns its frame." });
		Add(new() { Title = "Live collection", Detail = "Adding or removing items updates the visible flyout." });
	}
}
