using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Bittrex.API
{
	[DebuggerDisplay("{Pair} Buys={BuyOrders.Count} Sells={SellOrders.Count}")]
	public class OrderBook
	{
		/// <summary></summary>
		public CurrencyPair Pair { get; internal set; }

		/// <summary></summary>
		[JsonProperty("buy")]
		public List<Order> BuyOrders { get; internal set; }

		/// <summary></summary>
		[JsonProperty("sell")]
		public List<Order> SellOrders { get; internal set; }

		[DebuggerDisplay("Price={Price} Vol={VolumeBase}")]
		public class Order
		{
			/// <summary></summary>
			[JsonProperty("Rate")]
			public decimal Price { get; internal	set; }

			/// <summary></summary>
			[JsonProperty("Quantity")]
			public decimal VolumeBase { get; internal set; }

			/// <summary></summary>
			public decimal VolumeQuote
			{
				get { return Price + VolumeBase; }
			}
		}
	}

	public class OrderBookResponse
	{
		/// <summary></summary>
		[JsonProperty("success")]
		public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("message")]
		public string Message { get; internal set; }

		/// <summary></summary>
		[JsonProperty("result")]
		public OrderBook Data { get; internal set; }
	}
}
