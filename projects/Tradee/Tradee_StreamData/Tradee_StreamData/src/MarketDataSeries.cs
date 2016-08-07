using System;
using cAlgo.API.Internals;

namespace Tradee
{
	public class MarketSeriesData
	{
		public MarketSeriesData(MarketSeries series)
		{
			Series                   = series;
			LastTransmitUTC          = DateTime.MinValue;
			LastTransmittedPriceData = PriceData.Default;
			LastTransmittedCandle    = Candle.Default;
			SendHistoricData         = true;
		}


		/// <summary>The price data</summary>
		public MarketSeries Series { get; private set; }

		/// <summary>Timestamp of the last time this data was posted to Tradee</summary>
		public DateTime LastTransmitUTC { get; set; }

		/// <summary>Timestamp of when this data was last updated to the latest</summary>
		public DateTime LastUpdateUTC { get; set; }

		/// <summary>The price data last received from the server</summary>
		public PriceData LastTransmittedPriceData { get; set; }

		/// <summary>The latest candle received from the server</summary>
		public Candle LastTransmittedCandle { get; set; }

		/// <summary>True if historic data should be sent</summary>
		public bool SendHistoricData { get; set; }
	}
}
