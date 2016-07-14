using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using pr.util;
using Tradee;

namespace cAlgo
{
	/// <summary>This bot streams market data to 'Tradee'</summary>
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
	public class Tradee_StreamMarketData : Robot
	{
		/// <summary>The symbols to stream</summary>
		[Parameter("Symbols", DefaultValue = "USDJPY;NZDUSD;GBPUSD")]
		public string Symbols { get; set; }
		private MarketSeries[] m_series;

		#region Robot Implementation
		protected override void OnStart()
		{
			// Connect to 'Tradee'
			Tradee = new TradeeProxy();

			// Get the symbols to stream
			m_series = Symbols.Split(';').Select(x => MarketData.GetSeries(x, TimeFrame)).ToArray();
			if (m_series.Length == 0)
			{
				Print("No data series given. 'Symbols' parameter should be a semi-colon separated list of symbols");
				Stop();
			}
		}
		protected override void OnTick()
		{
			Tradee.Post(new HelloMsg());
		}
		protected override void OnStop()
		{
			Tradee = null;
		}
		#endregion

		/// <summary>The connection to the Tradee application</summary>
		private TradeeProxy Tradee
		{
			get { return m_tradee; }
			set
			{
				if (m_tradee == value) return;
				if (m_tradee != null) Util.Dispose(ref m_tradee);
				m_tradee = value;
			}
		}
		private TradeeProxy m_tradee;
	}
}
