using System;
using Newtonsoft.Json.Linq;

namespace Binance.API.DomainObjects
{
	public class MarketChartData
	{
		public MarketChartData(JArray candle_data)
		{
			Time = DateTimeOffset.FromUnixTimeMilliseconds(candle_data[0].Value<long>());
			Open = candle_data[1].Value<decimal>();
			High = candle_data[2].Value<decimal>();
			Low = candle_data[3].Value<decimal>();
			Close = candle_data[4].Value<decimal>();
			Volume = candle_data[5].Value<decimal>();
			TimeClose = DateTimeOffset.FromUnixTimeMilliseconds(candle_data[6].Value<long>());
			QuoteAssetVolume = candle_data[7].Value<decimal>();
			TradeCount = candle_data[8].Value<int>();
			TakerBuyVolumeBase = candle_data[9].Value<decimal>();
			TakerBuyVolumeQuote = candle_data[10].Value<decimal>();
		}

		/// <summary>Time stamp</summary>
		public DateTimeOffset Time { get; }

		/// <summary></summary>
		public decimal Open { get; }

		/// <summary></summary>
		public decimal High { get; }

		/// <summary></summary>
		public decimal Low{ get; }

		/// <summary></summary>
		public decimal Close { get; }

		/// <summary>Volume traded in this candle (in quote currency)</summary>
		public decimal Volume { get; }

		/// <summary></summary>
		public DateTimeOffset TimeClose { get; }

		/// <summary></summary>
		public decimal QuoteAssetVolume { get; }

		/// <summary></summary>
		public int TradeCount { get; }

		/// <summary></summary>
		public decimal TakerBuyVolumeBase { get; }

		/// <summary></summary>
		public decimal TakerBuyVolumeQuote { get; }

		/// <summary>Fake median data, since Binance doesn't supply this info</summary>
		public decimal Median => (Open + High + Low + Close) / 4;

		/// <summary></summary>
		public decimal Reserved { get; }
	}
}
