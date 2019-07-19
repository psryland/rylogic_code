using System;
using System.ComponentModel;
using System.Windows;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn.Windows;
using Rylogic.Utility;

namespace CoinFlip.Settings
{
	/// <summary>Settings associated with a connection to Rex via RexLink</summary>
	[Serializable]
	[TypeConverter(typeof(TyConv))]
	public class UISettings : SettingsSet<UISettings>
	{
		public UISettings()
		{
			UILayout = null;
			//ToolStripLayout = new ToolStripLocations();
			WindowPosition = Rect_.Zero;
			WindowMaximised = false;
		}

		/// <summary>The dock panel layout</summary>
		public XElement UILayout
		{
			get { return get<XElement>(nameof(UILayout)); }
			set { set(nameof(UILayout), value); }
		}

		///// <summary>The layout of main window tool bars</summary>
		//public ToolStripLocations ToolStripLayout
		//{
		//	get { return get<ToolStripLocations>(nameof(ToolStripLayout)); }
		//	set { set(nameof(ToolStripLayout), value); }
		//}

		/// <summary>The last position on screen</summary>
		public Rect WindowPosition
		{
			get { return get<Rect>(nameof(WindowPosition)); }
			set { set(nameof(WindowPosition), value); }
		}
		public bool WindowMaximised
		{
			get { return get<bool>(nameof(WindowMaximised)); }
			set { set(nameof(WindowMaximised), value); }
		}

		private class TyConv : GenericTypeConverter<UISettings> { }
	}
}
