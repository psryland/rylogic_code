using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
		[JsonProperty("seq")] public int Seq { get; private set; }

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
	}

	/// <summary>Data type received from the WAMP order book channel</summary>
	public class OrderBookUpdate
	{
		public enum EUpdateType
		{
			New,
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
				case "newTrade":        Type = EUpdateType.New; break;
				case "orderBookModify": Type = EUpdateType.Modify; break;
				case "orderBookRemove": Type = EUpdateType.Remove; break;
				}
			}
		}

		/// <summary>The updated volume at the given rate</summary>
		[JsonProperty("data")]
		public Order Order { get; private set; }
	}

	/// <summary>A trade offer</summary>
	[DebuggerDisplay("Price={Price} Vol={VolumeBase}")]
	public class Order
	{
		internal Order()
		{}
		internal Order(EOrderType order_type, decimal price, decimal volume)
		{
			Type = order_type;
			Price = price;
			VolumeBase = volume;
		}

		/// <summary>A unique ID assigned to the order</summary>
		[JsonProperty("orderNumber")]
		public ulong OrderId { get; private set; }

		/// <summary>A Buy or Sell order</summary>
		public EOrderType Type { get; private set; }
		[JsonProperty("type")] internal string TypeInternal
		{
			set { Type = Misc.ToOrderType(value); }
		}

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

		/// <summary></summary>
		[JsonProperty("total")]
		public decimal TotalBase { get; private set; }

		/// <summary>The order time stamp</summary>
		public DateTimeOffset Timestamp { get; private set; }
		[JsonProperty("date")] private string TimestampInternal
		{
			set { Timestamp = DateTimeOffset.ParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture); }
		}

		/// <summary>Leverage</summary>
		[JsonProperty("margin")]
		public int Margin { get; private set; }
	}
}
