using System;
using System.ComponentModel;
using System.Drawing;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gui;
using pr.util;

namespace LDraw
{
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			RecentFiles = string.Empty;

			Scene                  = new ChartControl.RdrOptions();
			Scene.AntiAliasing     = false;
			Scene.ChartBkColour    = Color.FromArgb(0x00,0x00,0x00);
			Scene.XAxis.GridColour = Color.FromArgb(0x20,0x20,0x20);
			Scene.YAxis.GridColour = Color.FromArgb(0x20,0x20,0x20);

			UI = new UISettings();

			AutoSaveOnChanges = true;
		}
		public Settings(string filepath)
			:base(filepath)
		{ }

		/// <summary>Recently loaded files</summary>
		public string RecentFiles
		{
			get { return get(x => x.RecentFiles); }
			set { set(x => x.RecentFiles, value); }
		}

		/// <summary>Style options for charts</summary>
		public ChartControl.RdrOptions Scene
		{
			get { return get(x => x.Scene); }
			set { set(x => x.Scene, value); }
		}

		/// <summary>UI settings</summary>
		public UISettings UI
		{
			get { return get(x => x.UI); }
			set
			{
				if (value == null) throw new ArgumentNullException("Setting '{0}' cannot be null".Fmt(nameof(UI)));
				set(x => x.UI, value);
			}
		}

		/// <summary>Settings version</summary>
		protected override string Version
		{
			get { return "v1.0"; }
		}
	}

	/// <summary>Settings associated with a connection to Rex via RexLink</summary>
	[TypeConverter(typeof(TyConv))]
	public class UISettings :SettingsSet<UISettings>
	{
		public UISettings()
		{
			UILayout             = null;
			WindowPosition       = Rectangle.Empty;
			WindowMaximised      = false;
		}

		/// <summary>The dock panel layout</summary>
		public XElement UILayout
		{
			get { return get(x => x.UILayout); }
			set { set(x => x.UILayout, value); }
		}

		/// <summary>The last position on screen</summary>
		public Rectangle WindowPosition
		{
			get { return get(x => x.WindowPosition); }
			set { set(x => x.WindowPosition, value); }
		}
		public bool WindowMaximised
		{
			get { return get(x => x.WindowMaximised); }
			set { set(x => x.WindowMaximised, value); }
		}

		private class TyConv :GenericTypeConverter<UISettings> {}
	}
}
