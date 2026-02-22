// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;

#if UWP
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
#elif WASDK
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
#endif

namespace U5BFA.Libraries
{
	[ContentProperty(Name = nameof(Islands))]
	public partial class TrayIconFlyout : Control, IDisposable
	{
		private const string PART_RootGrid = "PART_RootGrid";
		private const string PART_IslandsGrid = "PART_IslandsGrid";

#if WASDK
		private static readonly PersistentAcrylicBackdrop _persistentBackdrop = new();
#endif

		private readonly XamlIslandHostWindow? _host;
		private bool? _wasTaskbarLightLastTimeChecked;
		private bool? _wasTaskbarColorPrevalenceLastTimeChecked;
		private bool _isPopupAnimationPlaying;

		private Grid? RootGrid;
		private Grid? IslandsGrid;

#if WASDK
		internal ContentBackdropManager? BackdropManager { get; private set; }
#endif

		public bool IsOpen { get; private set; }

		public TrayIconFlyout()
		{
			DefaultStyleKey = typeof(TrayIconFlyout);

			_host = new XamlIslandHostWindow();
			_host.SetContent(this);
			_host.UpdateWindowVisibility(false);
			_host.WindowInactivated += HostWindow_Inactivated;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			RootGrid = GetTemplateChild(PART_RootGrid) as Grid
				?? throw new MissingFieldException($"Could not find {PART_RootGrid} in the given {nameof(TrayIconFlyout)}'s style.");
			IslandsGrid = GetTemplateChild(PART_IslandsGrid) as Grid
				?? throw new MissingFieldException($"Could not find {PART_IslandsGrid} in the given {nameof(TrayIconFlyout)}'s style.");

#if WASDK
			LayoutUpdated += TrayIconFlyout_LayoutUpdated;
#endif

			UpdateIslands();
		}

		public void Show()
		{
			if (_host?.DesktopWindowXamlSource is null || RootGrid is null || _isPopupAnimationPlaying)
				return;

			_isPopupAnimationPlaying = true;
			_host.Maximize();

			_ = Task.Run(async () =>
			{
#if UWP
				await RootGrid.Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, async () =>
#elif WASDK
				RootGrid.DispatcherQueue.TryEnqueue(async () =>
#endif
				{
					UpdateLayout();
					await Task.Delay(1);

					UpdateFlyoutTheme();
#if WASDK
					UpdateBackdropManager();
#endif
					UpdateFlyoutRegion();

					// Ensure to hide first
					if (RootGrid.RenderTransform is TranslateTransform translateTransform)
					{
						if (PopupDirection is Orientation.Vertical)
							translateTransform.Y = DesiredSize.Height;
						else
							translateTransform.X = DesiredSize.Width;
					}

					UpdateLayout();
					await Task.Delay(1);

					_host.UpdateWindowVisibility(true);

					if (IsTransitionAnimationEnabled)
					{
						var storyboard = PopupDirection is Orientation.Vertical
							? TransitionHelpers.GetWindows11BottomToTopTransitionStoryboard(RootGrid, (int)DesiredSize.Height, 0)
							: TransitionHelpers.GetWindows11RightToLeftTransitionStoryboard(RootGrid, (int)DesiredSize.Width, 0);
						storyboard.Begin();
						storyboard.Completed += OpenAnimationStoryboard_Completed;
					}
				});
			});
		}

		public void Hide()
		{
			if (RootGrid is null || _isPopupAnimationPlaying)
				return;

			_isPopupAnimationPlaying = true;

			if (IsTransitionAnimationEnabled)
			{
				var storyboard = PopupDirection is Orientation.Vertical
					? TransitionHelpers.GetWindows11TopToBottomTransitionStoryboard(RootGrid, 0, (int)DesiredSize.Height)
					: TransitionHelpers.GetWindows11LeftToRightTransitionStoryboard(RootGrid, 0, (int)DesiredSize.Width);
				storyboard.Begin();
				storyboard.Completed += CloseAnimationStoryboard_Completed;
			}
		}

#if UWP
		public unsafe bool TryPreTranslateMessage(MSG* msg)
		{
			return _host?.TryPreTranslateMessage(msg) ?? false;
		}
#endif

#if WASDK
		private void UpdateBackdropManager(bool coerce = false)
		{
			var isTaskbarLight = GeneralHelpers.IsTaskbarLight();
			var isTaskbarColorPrevalence = GeneralHelpers.IsTaskbarColorPrevalenceEnabled();
			bool shouldUpdateBackdrop = _wasTaskbarLightLastTimeChecked != isTaskbarLight || _wasTaskbarColorPrevalenceLastTimeChecked != isTaskbarColorPrevalence;
			_wasTaskbarLightLastTimeChecked = isTaskbarLight;
			_wasTaskbarColorPrevalenceLastTimeChecked = isTaskbarColorPrevalence;
			if (!shouldUpdateBackdrop && !coerce)
				return;

			ISystemBackdropControllerWithTargets? controller = BackdropKind is BackdropKind.Acrylic
				? (isTaskbarColorPrevalence
					? BackdropControllerHelpers.GetAccentedAcrylicController()
					: isTaskbarLight
						? BackdropControllerHelpers.GetLightAcrylicController()
						: BackdropControllerHelpers.GetDarkAcrylicController())
				: (isTaskbarColorPrevalence
					? BackdropControllerHelpers.GetAccentedMicaController()
					: isTaskbarLight
						? BackdropControllerHelpers.GetLightMicaController()
						: BackdropControllerHelpers.GetDarkMicaController());
			if (controller is null)
				return;

			BackdropManager?.Dispose();
			BackdropManager = null;
			BackdropManager = ContentBackdropManager.Create(controller, ElementCompositionPreview.GetElementVisual(IslandsGrid).Compositor, ActualTheme);

			UpdateBackdrop(true);
		}

		private void UpdateBackdrop(bool coerce = false)
		{
			foreach (var island in Islands)
				island.UpdateBackdrop(IsBackdropEnabled, coerce);
		}
#endif

		private void UpdateFlyoutTheme()
		{
			if (GeneralHelpers.IsTaskbarLight())
			{
				foreach (var island in Islands)
					island.RequestedTheme = ElementTheme.Light;
			}
			else
			{
				foreach (var island in Islands)
					island.RequestedTheme = ElementTheme.Dark;
			}
		}

		private void UpdateIslands()
		{
			if (IslandsGrid is null)
				return;

			IslandsGrid.Children.Clear();
			IslandsGrid.RowDefinitions.Clear();
			IslandsGrid.ColumnDefinitions.Clear();

			if (IslandsOrientation is Orientation.Vertical)
			{
				for (int index = 0; index < Islands.Count; index++)
				{
					if (Islands[index] is not TrayIconFlyoutIsland island)
						continue;

					IslandsGrid.RowDefinitions.Add(new() { Height = GridLength.Auto });
					Grid.SetRow(island, index);
					Grid.SetColumn(island, 0);
					island.SetOwner(this);
					IslandsGrid.Children.Add(island);
				}
			}
			else
			{
				for (int index = 0; index < Islands.Count; index++)
				{

					if (Islands[index] is not TrayIconFlyoutIsland island)
						continue;

					IslandsGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
					Grid.SetRow(island, 0);
					Grid.SetColumn(island, index);
					island.SetOwner(this);
					IslandsGrid.Children.Add(island);
				}
			}
		}

		private void UpdateFlyoutRegion()
		{
			if (_host?.DesktopWindowXamlSource is null || IslandsGrid is null)
				return;

			var flyoutWidth = DesiredSize.Width * _host.XamlIslandRasterizationScale;
			var flyoutHeight = DesiredSize.Height * _host.XamlIslandRasterizationScale;

			_host?.SetHWndRectRegion(new(
				(int)(_host.WindowSize.Width - flyoutWidth),
				(int)(_host.WindowSize.Height - flyoutHeight),
				(int)_host.WindowSize.Width,
				(int)_host.WindowSize.Height));
		}

#if WASDK
		private void TrayIconFlyout_LayoutUpdated(object? sender, object e)
		{
			//if (_host?.DesktopWindowXamlSource is null)
			//	return;

			//var flyouts = VisualTreeHelper.GetOpenPopupsForXamlRoot(_host.DesktopWindowXamlSource.Content.XamlRoot);
			//foreach (var flyout in flyouts)
			//{
			//	if (flyout.SystemBackdrop is not PersistentAcrylicBackdrop)
			//		flyout.SystemBackdrop = _persistentBackdrop;
			//}
		}
#endif

		private void OpenAnimationStoryboard_Completed(object? sender, object e)
		{
			if (sender is not Storyboard storyboard)
				return;

			storyboard.Completed -= OpenAnimationStoryboard_Completed;
			_isPopupAnimationPlaying = false;
			IsOpen = true;
		}

		private void CloseAnimationStoryboard_Completed(object? sender, object e)
		{
			if (sender is not Storyboard storyboard)
				return;

			storyboard.Completed -= CloseAnimationStoryboard_Completed;
			_isPopupAnimationPlaying = false;
			IsOpen = false;
			_host?.UpdateWindowVisibility(false);
		}

		private void HostWindow_Inactivated(object? sender, EventArgs e)
		{
			if (HideOnLostFocus) Hide();
		}

		public void Dispose()
		{
#if WASDK
			BackdropManager?.Dispose();
#endif
			_host?.WindowInactivated -= HostWindow_Inactivated;
			_host?.Dispose();
		}
	}
}
