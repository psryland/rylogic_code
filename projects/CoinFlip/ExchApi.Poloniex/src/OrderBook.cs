using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;

namespace Poloniex.API
{
	/// <summary>The Buy(Bid) / Sell(Ask) orders. Typically associated with a pair</summary>
	[DebuggerDisplay("Buys={BuyOrders.Count} Sells={SellOrders.Count}")]
	public class OrderBook
	{
		public OrderBook()
		{
			BuyOrders = new List<Order>();
			SellOrders = new List<Order>();
		}

		/// <summary>The currency pair associated with this order book</summary>
		public CurrencyPair Pair { get; internal set; }

		/// <summary>Offers to buy</summary>
		public List<Order> BuyOrders { get; private set; }
		[JsonProperty("bids")] private List<string[]> BuyOrdersInternal
		{
			set { BuyOrders = ParseOrders(value, EOrderType.Buy); }
		}

		/// <summary>Offers to sell</summary>
		public List<Order> SellOrders { get; private set; }
		[JsonProperty("asks")] private List<string[]> SellOrdersInternal
		{
			set { SellOrders = ParseOrders(value, EOrderType.Sell); }
		}

		/// <summary>True if trading is frozen on the owning pair</summary>
		public bool IsFrozen { get; private set; }
		[JsonProperty("isFrozen")] private string IsFrozenInternal
		{
			set { IsFrozen = int.Parse(value) != 0; }
		}

		/// <summary>A sequence number for this update</summary>
		[JsonProperty("seq")]
		public int Seq { get; internal set; }

		/// <summary></summary>
		private static List<Order> ParseOrders(List<string[]> orders, EOrderType order_type)
		{
			var output = new List<Order>(orders.Count);
			foreach (var order in orders)
			{
				output.Add(new Order(
					order_type,
					(decimal)double.Parse(order[0], CultureInfo.InvariantCulture),
					(decimal)double.Parse(order[1], CultureInfo.InvariantCulture)));
			}
			return output;
		}

		/// <summary>A trade offer</summary>
		[DebuggerDisplay("Price={Price} Vol={VolumeBase}")]
		public class Order
		{
			internal Order()
			{}
			internal Order(EOrderType type, decimal price, decimal volume)
			{
				Type = type;
				Price = price;
				VolumeBase = volume;
			}

			/// <summary>A buy or sell order</summary>
			public EOrderType Type { get; private set; }

			/// <summary>The trade price (in quote currency)</summary>
			[JsonProperty("rate")]
			public decimal Price { get; private set; }

			/// <summary>The trade volume (in base currency)</summary>
			[JsonProperty("amount")]
			public decimal VolumeBase { get; private set; }

			/// <summary>The trade volume (in quote currency)</summary>
			public decimal VolumeQuote
			{
				get { return VolumeBase * Price; }
			}
		}
	}

	/// <summary>Data type received from the WAMP order book and trade update channel</summary>
	public class OrderBookUpdate
	{
		public enum EUpdateType
		{
			NewTrade,
			Modify,
			Remove,
		}

		/// <summary>The type of update this represents</summary>
		public EUpdateType Type { get; private set; }
		[JsonProperty("type")] internal string TypeInternal
		{
			set
			{
				switch (value) {
				default: throw new Exception(string.Format("Unknown update type: {0}", value));
				case "newTrade":        Type = EUpdateType.NewTrade; break;
				case "orderBookModify": Type = EUpdateType.Modify; break;
				case "orderBookRemove": Type = EUpdateType.Remove; break;
				}
			}
		}

		/// <summary>The updated volume at the given rate</summary>
		[JsonProperty("data")]
		public OrderBook.Order Order { get; internal set; }
	}
}
