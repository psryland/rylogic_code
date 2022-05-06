using System.Windows.Controls;
using Rylogic.Common;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>General options for the dock container</summary>
	public class OptionsData :SettingsSet<OptionsData>
	{
		public OptionsData()
		{
			AllowUserDocking = true;
			AllowUserCloseTabs = true;
			TabStripLocation = Dock.Bottom;
			DoubleClickTitleBarToDock = true;
			DoubleClickTabsToFloat = true;
			AlwaysShowTabs = false;
			ShowTitleBars = true;
			DragThresholdInPixels = 5;
		}

		/// <summary>Get/Set whether the user is allowed to drag and drop panes</summary>
		public bool AllowUserDocking
		{
			get => get<bool>(nameof(AllowUserDocking));
			set => set(nameof(AllowUserDocking), value);
		}

		/// <summary>Get/Set whether the user can close tabs</summary>
		public bool AllowUserCloseTabs
		{
			get => get<bool>(nameof(AllowUserCloseTabs));
			set => set(nameof(AllowUserCloseTabs), value);
		}

		/// <summary>The default location for tab strips</summary>
		public Dock TabStripLocation
		{
			get => get<Dock>(nameof(TabStripLocation));
			set => set(nameof(TabStripLocation), value);
		}

		/// <summary>Get/Set whether double clicking on the title bar of a floating window returns its contents to the dock container</summary>
		public bool DoubleClickTitleBarToDock
		{
			get => get<bool>(nameof(DoubleClickTitleBarToDock));
			set => set(nameof(DoubleClickTitleBarToDock), value);
		}

		/// <summary>Get/Set whether double clicking on a tab in the tab strip causes the content to move to/from a floating window</summary>
		public bool DoubleClickTabsToFloat
		{
			get => get<bool>(nameof(DoubleClickTabsToFloat));
			set => set(nameof(DoubleClickTabsToFloat), value);
		}

		/// <summary>True to always show the tabs, even if there's only one</summary>
		public bool AlwaysShowTabs
		{
			get => get<bool>(nameof(AlwaysShowTabs));
			set => set(nameof(AlwaysShowTabs), value);
		}

		/// <summary>Show/Hide all title bars</summary>
		public bool ShowTitleBars
		{
			get => get<bool>(nameof(ShowTitleBars));
			set => set(nameof(ShowTitleBars), value);
		}

		/// <summary>The minimum distance the mouse must move before a drag operation starts</summary>
		public double DragThresholdInPixels
		{
			get => get<double>(nameof(DragThresholdInPixels));
			set => set(nameof(DragThresholdInPixels), value);
		}
	}

}
