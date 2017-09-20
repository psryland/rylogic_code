using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cryptopia.API
{
	public class Market
	{
		/// <summary></summary>
		[JsonProperty]
		public int TradePairId { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Label { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal AskPrice { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal BidPrice { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Low { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal High { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Volume { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal LastPrice { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal LastVolume { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal BuyVolume { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal SellVolume { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Change { get; set; }
	}

	public class MarketHistory
	{
		/// <summary></summary>
		[JsonProperty]
		public int TradePairId { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Label { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Type { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Price { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Amount { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Total { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public int Timestamp { get; set; }
	}

	public class MarketsResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public List<Market> Data { get; set; }
	}

	public class MarketResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public Market Data { get; set; }
	}

	public class MarketHistoryResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public List<MarketHistory> Data { get; set; }
	}
}
