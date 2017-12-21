using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cryptopia.API
{
	public class MarketOrder
	{
		/// <summary></summary>
		[JsonProperty]
		public int TradePairId { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Label { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Price { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Volume { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Total { get; set; }
	}

	public class MarketOrders
	{
		/// <summary></summary>
		[JsonProperty]
		public List<MarketOrder> Buy { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public List<MarketOrder> Sell { get; set; }
	}

	public class MarketOrdersResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public MarketOrders Data { get; set; }
	}

	public class MarketOrderGroups
	{
		/// <summary></summary>
		[JsonProperty]
		public int TradePairId { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Market { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public List<MarketOrder> Buy { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public List<MarketOrder> Sell { get; set; }
	}

	public class MarketOrderGroupsResponse
	{
		public MarketOrderGroupsResponse()
		{
			Success = true;
			Error = string.Empty;
			Data = new List<MarketOrderGroups>();
		}

		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public List<MarketOrderGroups> Data { get; set; }
	}
}
