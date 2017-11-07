using System;
using System.ComponentModel;
using System.Xml.Linq;
using CoinFlip;
using pr.common;
using pr.gui;
using pr.util;

namespace Bot.ReturnToMean
{
	[Plugin(typeof(IBot))]
    public class ReturnToMean :IBot
    {
		public ReturnToMean(Model model, XElement settings_xml)
			:base("ReturnToMean", model, new SettingsData(settings_xml))
		{ }
		protected override void Dispose(bool disposing)
		{
			Pair = null;
			base.Dispose(disposing);
		}

		/// <summary>Settings for this strategy</summary>
		public new SettingsData Settings
		{
			get { return (SettingsData)base.Settings; }
		}

		/// <summary>The pair to trade</summary>
		public TradePair Pair
		{
			get { return m_pair; }
			set
			{
				if (m_pair == value) return;

				if (Active)
					throw new Exception("Don't change the pair while the bot is running");

				if (m_pair != null)
				{
				}
				m_pair = value;
				if (m_pair != null)
				{
					Settings.PairWithExchange = m_pair.NameWithExchange;
				}
				RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Pair)));
			}
		}
		private TradePair m_pair;

		/// <summary>Moving average data for 'Instrument'</summary>
		public IndicatorMA MA
		{
			get { return m_ma; }
			private set
			{
				if (m_ma == value) return;
				Util.Dispose(ref m_ma);
				m_ma = value;
			}
		}
		private IndicatorMA m_ma;

		/// <summary>Start the bot</summary>
		public override void OnStart()
		{
			// Generate the EMA
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

		/// <summary>Data needed to save a fishing instance in the settings</summary>
		[TypeConverter(typeof(TyConv))]
		public class SettingsData :SettingsBase<SettingsData>
		{
			public SettingsData()
			{
				PairWithExchange = string.Empty;
			}
			public SettingsData(XElement node)
				:base(node)
			{}

			/// <summary>The name of the pair to trade and the exchange it's on</summary>
			public string PairWithExchange
			{
				get { return get<string>(nameof(PairWithExchange)); }
				set { set(nameof(PairWithExchange), value); }
			}

			private class TyConv :GenericTypeConverter<SettingsData> {}
		}
	}
}
