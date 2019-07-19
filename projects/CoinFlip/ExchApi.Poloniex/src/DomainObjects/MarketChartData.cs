using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;
using Rylogic.Utility;

namespace Poloniex.API.DomainObjects
{
	public class MarketChartData
	{
		/// <summary>Time stamp</summary>
		[JsonProperty("date"), JsonConverter(typeof(ToUnixSec))]
		public UnixSec Time { get; private set; }

		[JsonProperty("open")]
		public double Open { get; private set; }

		[JsonProperty("close")]
		public double Close { get; private set; }

		[JsonProperty("high")]
		public double High { get; private set; }

		[JsonProperty("low")]
		public double Low { get; private set; }

		[JsonProperty("volume")]
		public double VolumeBase { get; private set; }

		[JsonProperty("quoteVolume")]
		public double VolumeQuote { get; private set; }

		[JsonProperty("weightedAverage")]
		public double WeightedAverage { get; private set; }

		/// <summary>An invalid candle returned by Poloniex</summary>
		public bool Invalid => Open == 0 && Close == 0 && High == 0 && Low == 0;
	}
}
