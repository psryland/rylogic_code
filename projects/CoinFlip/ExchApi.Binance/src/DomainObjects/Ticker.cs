using System;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;

namespace Binance.API.DomainObjects
{
	public class Ticker
	{
		public Ticker()
		{ }
		public Ticker(CurrencyPair pair, double price_change, double price_change_percent, double weighted_avg_price, double prev_close_price, double last_price, double last_qty, double price_q2b, double price_b2q, double open_price, double high_price, double low_price, double volume_base, double volume_quote, DateTimeOffset open_time, DateTimeOffset close_time, long first_trade_id, long last_trade_id, long trade_count)
		{
			Pair = pair;
			PriceChange = price_change;
			PriceChangePercent = price_change_percent;
			WeightedAvgPrice = weighted_avg_price;
			PrevClosePrice = prev_close_price;
			LastPrice = last_price;
			LastQty = last_qty;
			PriceQ2B = price_q2b;
			PriceB2Q = price_b2q;
			OpenPrice = open_price;
			HighPrice = high_price;
			LowPrice = low_price;
			VolumeBase = volume_base;
			VolumeQuote = volume_quote;
			OpenTime = open_time;
			CloseTime = close_time;
			FirstTradeId = first_trade_id;
			LastTradeId = last_trade_id;
			TradeCount = trade_count;
		}
		public Ticker(Ticker rhs)
		{
			Pair = rhs.Pair;
			PriceChange = rhs.PriceChange;
			PriceChangePercent = rhs.PriceChangePercent;
			WeightedAvgPrice = rhs.WeightedAvgPrice;
			PrevClosePrice = rhs.PrevClosePrice;
			LastPrice = rhs.LastPrice;
			LastQty = rhs.LastQty;
			PriceQ2B = rhs.PriceQ2B;
			PriceB2Q = rhs.PriceB2Q;
			OpenPrice = rhs.OpenPrice;
			HighPrice = rhs.HighPrice;
			LowPrice = rhs.LowPrice;
			VolumeBase = rhs.VolumeBase;
			VolumeQuote = rhs.VolumeQuote;
			OpenTime = rhs.OpenTime;
			CloseTime = rhs.CloseTime;
			FirstTradeId = rhs.FirstTradeId;
			LastTradeId = rhs.LastTradeId;
			TradeCount = rhs.TradeCount;
		}

		/// <summary>Currency pair</summary>
		public CurrencyPair Pair { get; set; }
		[JsonProperty("symbol")] private string PairInternal { set => Pair = CurrencyPair.Parse(value); }

		/// <summary></summary>
		[JsonProperty("PriceChange")]
		public double PriceChange { get; set; }

		/// <summary></summary>
		[JsonProperty("PriceChangePercent")]
		public double PriceChangePercent { get; set; }

		/// <summary></summary>
		[JsonProperty("weightedAvgPrice")]
		public double WeightedAvgPrice { get; set; }

		/// <summary></summary>
		[JsonProperty("prevClosePrice")]
		public double PrevClosePrice { get; set; }

		/// <summary></summary>
		[JsonProperty("lastPrice")]
		public double LastPrice { get; set; }

		/// <summary></summary>
		[JsonProperty("lastQty")]
		public double LastQty { get; set; }

		/// <summary></summary>
		[JsonProperty("bidPrice")]
		public double PriceB2Q { get; set; }

		/// <summary></summary>
		[JsonProperty("askPrice")]
		public double PriceQ2B { get; set; }

		/// <summary></summary>
		[JsonProperty("openPrice")]
		public double OpenPrice { get; set; }

		/// <summary></summary>
		[JsonProperty("highPrice")]
		public double HighPrice { get; set; }

		/// <summary></summary>
		[JsonProperty("lowPrice")]
		public double LowPrice { get; set; }

		/// <summary></summary>
		[JsonProperty("volume")]
		public double VolumeBase { get; set; }

		/// <summary></summary>
		[JsonProperty("quoteVolume")]
		public double VolumeQuote { get; set; }

		/// <summary></summary>
		[JsonProperty("openTime"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
		public DateTimeOffset OpenTime { get; set; }

		/// <summary></summary>
		[JsonProperty("closeTime"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
		public DateTimeOffset CloseTime { get; set; }

		/// <summary></summary>
		[JsonProperty("firstId")]
		public long FirstTradeId { get; set; }

		/// <summary></summary>
		[JsonProperty("lastId")]
		public long LastTradeId { get; set; }

		/// <summary></summary>
		[JsonProperty("count")]
		public long TradeCount { get; set; }
	}
}
