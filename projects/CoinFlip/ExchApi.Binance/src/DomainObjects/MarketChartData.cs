using System;
using Newtonsoft.Json.Linq;

namespace Binance.API.DomainObjects
{
	public class MarketChartData :IComparable<MarketChartData>
	{
		public MarketChartData(DateTimeOffset time, double open, double high, double low, double close, double volume, DateTimeOffset time_close, double quote_asset_volume, int trade_count, double taker_buy_volume_base, double taker_buy_volume_quote)
		{
			Time = time;
			Open = open;
			High = high;
			Low = low;
			Close = close;
			Volume = volume;
			TimeClose = time_close;
			QuoteAssetVolume = quote_asset_volume;
			TradeCount = trade_count;
			TakerBuyVolumeBase = taker_buy_volume_base;
			TakerBuyVolumeQuote = taker_buy_volume_quote;
		}
		public MarketChartData(JArray candle_data)
		{
			Time = DateTimeOffset.FromUnixTimeMilliseconds(candle_data[0].Value<long>());
			Open = candle_data[1].Value<double>();
			High = candle_data[2].Value<double>();
			Low = candle_data[3].Value<double>();
			Close = candle_data[4].Value<double>();
			Volume = candle_data[5].Value<double>();
			TimeClose = DateTimeOffset.FromUnixTimeMilliseconds(candle_data[6].Value<long>());
			QuoteAssetVolume = candle_data[7].Value<double>();
			TradeCount = candle_data[8].Value<int>();
			TakerBuyVolumeBase = candle_data[9].Value<double>();
			TakerBuyVolumeQuote = candle_data[10].Value<double>();
		}

		/// <summary>Time stamp</summary>
		public DateTimeOffset Time { get; }

		/// <summary></summary>
		public double Open { get; }

		/// <summary></summary>
		public double High { get; }

		/// <summary></summary>
		public double Low { get; }

		/// <summary></summary>
		public double Close { get; }

		/// <summary>Volume traded in this candle (in quote currency)</summary>
		public double Volume { get; }

		/// <summary></summary>
		public DateTimeOffset TimeClose { get; }

		/// <summary></summary>
		public double QuoteAssetVolume { get; }

		/// <summary></summary>
		public int TradeCount { get; }

		/// <summary></summary>
		public double TakerBuyVolumeBase { get; }

		/// <summary></summary>
		public double TakerBuyVolumeQuote { get; }

		/// <summary>Approximate median data, since Binance doesn't supply this info</summary>
		public double Median => (Open + High + Low + Close) / 4;

		/// <summary></summary>
		public double Reserved { get; }

		public int CompareTo(MarketChartData other)
		{
			return Time.CompareTo(other.Time);
		}
	}
}
