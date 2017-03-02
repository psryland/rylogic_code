using System;
using cAlgo.API.Internals;

namespace Tradee
{
	public class MarketSeriesData
	{
		public MarketSeriesData(MarketSeries series)
		{
			Series                   = series;
			LastTransmitUTC          = DateTimeOffset.MinValue;
			LastUpdateUTC            = DateTimeOffset.MinValue;
			LastTransmittedPriceData = PriceData.Default;
			LastTransmittedCandle    = PriceCandle.Default;
		}

		/// <summary>The price data</summary>
		public MarketSeries Series { get; private set; }

		/// <summary>Timestamp of the last time this data was posted to Tradee</summary>
		public DateTimeOffset LastTransmitUTC { get; set; }

		/// <summary>Timestamp of when this data was last updated to the latest</summary>
		public DateTimeOffset LastUpdateUTC { get; set; }

		/// <summary>The price data last received from the server</summary>
		public PriceData LastTransmittedPriceData { get; set; }

		/// <summary>The latest candle received from the server</summary>
		public PriceCandle LastTransmittedCandle { get; set; }
	}
}
