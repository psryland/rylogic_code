using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.util;
using Tradee;

namespace cAlgo
{
	/// <summary>This bot streams market data to 'Tradee'</summary>
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class Tradee_StreamMarketData : Robot
	{
		#region Robot Implementation
		protected override void OnStart()
		{
			// Connect to 'Tradee'
			Tradee = new TradeeProxy();

			// Send historical data to Tradee
			var open_time = new List<long>  (MarketSeries.OpenTime.Count);
			var open      = new List<double>(MarketSeries.Open.Count);
			var high      = new List<double>(MarketSeries.High.Count);
			var low       = new List<double>(MarketSeries.Low.Count);
			var close     = new List<double>(MarketSeries.Close.Count);
			var volume    = new List<double>(MarketSeries.TickVolume.Count);
			for (int i = MarketSeries.OpenTime.Count; i-- != 0; )
			{
				open_time.Add(MarketSeries.OpenTime  .Last(i).Ticks);
				open     .Add(MarketSeries.Open      .Last(i));
				high     .Add(MarketSeries.High      .Last(i));
				low      .Add(MarketSeries.Low       .Last(i));
				close    .Add(MarketSeries.Close     .Last(i));
				volume   .Add(MarketSeries.TickVolume.Last(i));
			}

			// Post to tradee
			var data = new Candles(
				Symbol.Code,
				open_time.ToArray(),
				open.ToArray(),
				high.ToArray(),
				low.ToArray(),
				close.ToArray(),
				volume.ToArray());
			Tradee.Post(data);
		}
		protected override void OnTick()
		{
			// Post the market data to Tradee
			var data = new Candle(
				Symbol.Code,
				MarketSeries.OpenTime.LastValue.Ticks,
				MarketSeries.Open.LastValue,
				MarketSeries.High.LastValue,
				MarketSeries.Low.LastValue,
				MarketSeries.Close.LastValue,
				MarketSeries.TickVolume.LastValue);
			Tradee.Post(data);
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
