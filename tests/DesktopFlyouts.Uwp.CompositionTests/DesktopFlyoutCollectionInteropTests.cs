// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DesktopFlyouts.SampleApp.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinRT;

namespace DesktopFlyouts.Tests;

[TestClass]
public sealed class DesktopFlyoutCollectionInteropTests
{
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void CollectionsExposeGeneratedWinRTInterfacesWithoutCreatingXaml(bool sampleSource)
	{
		Assert.IsFalse(RuntimeFeature.IsDynamicCodeSupported, "Exercise the samples' AOT-compatible marshalling path.");
		var collection = sampleSource ? (object)new SampleIslandDataSource() : CreateUnownedCollection();
		// The sample disables dynamic code support, so reflection-based CCW fallback cannot
		// supply the collection interfaces. Query the actual native collection interfaces.
		var inspectable = MarshalInspectable<object>.FromManaged(collection);
		try
		{
			AssertInterface(inspectable, new Guid("036d2c08-df29-41af-8aa2-d774be62ba6f")); // IBindableIterable
			AssertInterface(inspectable, new Guid("393de7de-6fd0-4c0d-bb71-47244a113e93")); // IBindableVector
			AssertInterface(inspectable, new Guid("28b167d5-1a31-465b-9b25-d5c3ae686c40")); // UWP INotifyCollectionChanged
		}
		finally
		{
			MarshalInspectable<object>.DisposeAbi(inspectable);
		}
	}

	private static object CreateUnownedCollection()
	{
		var type = typeof(DesktopFlyout).Assembly.GetType("DesktopFlyouts.DesktopFlyoutIslandCollection", throwOnError: true)!;
		// Only interface discovery is exercised here. A null owner avoids constructing XAML;
		// owner-dependent mutation guards are covered by the integration tests.
		return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
			binder: null, args: [null], culture: null)!;
	}

	private static void AssertInterface(nint inspectable, Guid iid)
	{
		var result = Marshal.QueryInterface(inspectable, in iid, out var pointer);
		try
		{
			Assert.AreEqual(0, result, $"Missing WinRT collection interface {iid}: 0x{result:X8}");
		}
		finally
		{
			if (pointer != 0) Marshal.Release(pointer);
		}
	}
}
