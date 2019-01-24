using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>General options for the dock container</summary>
	public class OptionsData
	{
		public OptionsData()
		{
			AllowUserDocking = true;
			DoubleClickTitleBarToDock = true;
			DoubleClickTabsToFloat = true;
			AlwaysShowTabs = false;
			ShowTitleBars = true;
			DragThresholdInPixels = 5;
		}
		public OptionsData(XElement node)
		{
			AllowUserDocking = node.Element(nameof(AllowUserDocking)).As<bool>();
			DoubleClickTitleBarToDock = node.Element(nameof(DoubleClickTitleBarToDock)).As<bool>();
			DoubleClickTabsToFloat = node.Element(nameof(DoubleClickTabsToFloat)).As<bool>();
			AlwaysShowTabs = node.Element(nameof(AlwaysShowTabs)).As<bool>();
			ShowTitleBars = node.Element(nameof(ShowTitleBars)).As<bool>();
			DragThresholdInPixels = node.Element(nameof(DragThresholdInPixels)).As<double>();
		}
		public XElement ToXml(XElement node)
		{
			node.Add2(nameof(AllowUserDocking), AllowUserDocking, false);
			node.Add2(nameof(DoubleClickTitleBarToDock), DoubleClickTitleBarToDock, false);
			node.Add2(nameof(DoubleClickTabsToFloat), DoubleClickTabsToFloat, false);
			node.Add2(nameof(AlwaysShowTabs), AlwaysShowTabs, false);
			node.Add2(nameof(ShowTitleBars), ShowTitleBars, false);
			node.Add2(nameof(DragThresholdInPixels), DragThresholdInPixels, false);
			return node;
		}

		/// <summary>Get/Set whether the user is allowed to drag and drop panes</summary>
		public bool AllowUserDocking { get; set; }

		/// <summary>Get/Set whether double clicking on the title bar of a floating window returns its contents to the dock container</summary>
		public bool DoubleClickTitleBarToDock { get; set; }

		/// <summary>Get/Set whether double clicking on a tab in the tab strip causes the content to move to/from a floating window</summary>
		public bool DoubleClickTabsToFloat { get; set; }

		/// <summary>True to always show the tabs, even if there's only one</summary>
		public bool AlwaysShowTabs { get; set; }

		/// <summary>Show/Hide all title bars</summary>
		public bool ShowTitleBars { get; set; }

		/// <summary>The minimum distance the mouse must move before a drag operation starts</summary>
		public double DragThresholdInPixels { get; set; }
	}

}
