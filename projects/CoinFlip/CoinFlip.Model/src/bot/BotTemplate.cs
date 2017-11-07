using System.ComponentModel;
using System.Xml.Linq;
using CoinFlip;
using pr.common;
using pr.gui;
using pr.util;

namespace Bot.Template
{
	[Plugin(typeof(IBot))]
    public class Template :IBot
    {
		public Template(Model model, XElement settings_xml)
			:base("Template", model, new SettingsData(settings_xml))
		{ }
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		/// <summary>Settings for this strategy</summary>
		public new SettingsData Settings
		{
			get { return (SettingsData)base.Settings; }
		}

		/// <summary>Start the bot</summary>
		public override void OnStart()
		{
		}

		/// <summary>Stop the bot</summary>
		public override void OnStop()
		{
		}

		/// <summary>Main loop step</summary>
		public override void Step()
		{
		}

		/// <summary>Add graphics to an chart displaying 'Pair'</summary>
		public override void OnChartRendering(Instrument instrument, Settings.ChartSettings chart_settings, ChartControl.ChartRenderingEventArgs args)
		{
		}

		/// <summary>Bot settings data</summary>
		[TypeConverter(typeof(TyConv))]
		public class SettingsData :SettingsBase<SettingsData>
		{
			public SettingsData()
			{
			}
			public SettingsData(XElement node)
				:base(node)
			{}

			private class TyConv :GenericTypeConverter<SettingsData> {}
		}
	}
}
