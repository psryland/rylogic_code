using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cryptopia.API
{
	public class OpenOrder
	{
		/// <summary></summary>
		[JsonProperty]
		public int OrderId { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public int TradePairId { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Market { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Type { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Rate { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Amount { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Total { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Remaining { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public DateTime TimeStamp { get; set; }
	}

	public class TradeHistory
	{
		/// <summary></summary>
		[JsonProperty]
		public int OrderId { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public int TradeId { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public int TradePairId { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Market { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Type { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Rate { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Amount { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Total { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Fee { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public DateTime TimeStamp { get; set; }
	}

	public class SubmitTradeData
	{
		/// <summary>Get/Set the created order identifier. This is the ID of an order added to the order book for the pair</summary>
		[JsonProperty]
		public int? OrderId { get; set; }

		/// <summary>
		/// Get/Set the list of any filled orders. This is the IDs of completed trades.
		/// If an order can be filled (possibly partially) by orders in the order book, then these trades
		/// are performed immediately and their trade IDs are returned here</summary>
		[JsonProperty]
		public List<int> FilledOrders { get; set; }
	}

	public class OpenOrdersResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public List<OpenOrder> Data { get; set; }
	}

	public class TradeHistoryResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public List<TradeHistory> Data { get; set; }
	}

	public class SubmitTradeResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public SubmitTradeData Data { get; set; }
	}

	public class CancelTradeResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public List<int> Data { get; set; }
	}
}
