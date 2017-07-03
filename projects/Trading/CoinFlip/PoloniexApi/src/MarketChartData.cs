using System;
using Newtonsoft.Json;

namespace Poloniex.API
{
	public class MarketChartData
	{
		/// <summary>Time stamp</summary>
		public DateTimeOffset Time { get; private set; }
		[JsonProperty("date")] private ulong TimeInternal
		{
			set { Time = Misc.ToDateTimeOffset(value); }
		}

		[JsonProperty("open")]
		public decimal Open { get; private set; }

		[JsonProperty("close")]
		public decimal Close { get; private set; }

		[JsonProperty("high")]
		public decimal High { get; private set; }

		[JsonProperty("low")]
		public decimal Low { get; private set; }

		[JsonProperty("volume")]
		public decimal VolumeBase { get; private set; }

		[JsonProperty("quoteVolume")]
		public decimal VolumeQuote { get; private set; }

		[JsonProperty("weightedAverage")]
		public decimal WeightedAverage { get; private set; }
	}
}
