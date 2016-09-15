using System;
using System.ComponentModel;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.util;

namespace Rylobot
{
	/// <summary>Rylobot settings</summary>
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			General    = new GeneralSettings();
			UI         = new UISettings();
		}
		public Settings(string filepath)
			:base(filepath)
		{ }

		/// <summary>Settings version</summary>
		protected override string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>General application settings</summary>
		public GeneralSettings General
		{
			get { return get(x => x.General); }
			set
			{
				if (value == null) throw new ArgumentNullException("Setting '{0}' cannot be null".Fmt(R<Settings>.Name(x => x.General)));
				set(x => x.General, value);
			}
		}

		/// <summary>UI settings</summary>
		public UISettings UI
		{
			get { return get(x => x.UI); }
			set
			{
				if (value == null) throw new ArgumentNullException("Setting '{0}' cannot be null".Fmt(R<Settings>.Name(x => x.UI)));
				set(x => x.UI, value);
			}
		}
	}

	/// <summary>Settings associated with a connection to Rex via RexLink</summary>
	[TypeConverter(typeof(TyConv))]
	public class GeneralSettings :SettingsSet<GeneralSettings>
	{
		public GeneralSettings()
		{
			InstrumentSettingsDir = Util.ResolveAppDataPath("Rylogic", "Rylobot", ".\\Instruments");
		}

		/// <summary>The directory for account database files</summary>
		public string InstrumentSettingsDir
		{
			get { return get(x => x.InstrumentSettingsDir); }
			set { set(x => x.InstrumentSettingsDir, value); }
		}

		private class TyConv :GenericTypeConverter<GeneralSettings> {}
	}

		/// <summary>Settings associated with a connection to Rex via RexLink</summary>
	[TypeConverter(typeof(TyConv))]
	public class UISettings :SettingsSet<UISettings>
	{
		public UISettings()
		{
			UILayout = null;
		}

		/// <summary>The dock panel layout</summary>
		public XElement UILayout
		{
			get { return get(x => x.UILayout); }
			set { set(x => x.UILayout, value); }
		}

		private class TyConv :GenericTypeConverter<UISettings> {}
	}
}
