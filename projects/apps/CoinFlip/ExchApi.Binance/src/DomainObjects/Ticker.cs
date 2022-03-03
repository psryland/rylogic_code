using System;
using System.Diagnostics;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;

namespace Binance.API.DomainObjects
{
	[DebuggerDisplay("{Pair.Id,nq}")]
	public class Ticker
	{
		public Ticker()
		{ }
		public Ticker(CurrencyPair pair, decimal price_change, decimal price_change_percent, decimal weighted_avg_price, decimal prev_close_price, decimal last_price, decimal last_qty, decimal price_q2b, decimal price_b2q, decimal open_price, decimal high_price, decimal low_price, decimal volume_base, decimal volume_quote, DateTimeOffset open_time, DateTimeOffset close_time, long first_trade_id, long last_trade_id, long trade_count)
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
		public decimal PriceChange { get; set; }

		/// <summary></summary>
		[JsonProperty("PriceChangePercent")]
		public decimal PriceChangePercent { get; set; }

		/// <summary></summary>
		[JsonProperty("weightedAvgPrice")]
		public decimal WeightedAvgPrice { get; set; }

		/// <summary></summary>
		[JsonProperty("prevClosePrice")]
		public decimal PrevClosePrice { get; set; }

		/// <summary></summary>
		[JsonProperty("lastPrice")]
		public decimal LastPrice { get; set; }

		/// <summary></summary>
		[JsonProperty("lastQty")]
		public decimal LastQty { get; set; }

		/// <summary></summary>
		[JsonProperty("bidPrice")]
		public decimal PriceB2Q { get; set; }

		/// <summary></summary>
		[JsonProperty("askPrice")]
		public decimal PriceQ2B { get; set; }

		/// <summary></summary>
		[JsonProperty("openPrice")]
		public decimal OpenPrice { get; set; }

		/// <summary></summary>
		[JsonProperty("highPrice")]
		public decimal HighPrice { get; set; }

		/// <summary></summary>
		[JsonProperty("lowPrice")]
		public decimal LowPrice { get; set; }

		/// <summary></summary>
		[JsonProperty("volume")]
		public decimal VolumeBase { get; set; }

		/// <summary></summary>
		[JsonProperty("quoteVolume")]
		public decimal VolumeQuote { get; set; }

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
