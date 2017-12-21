using System.Collections.Generic;
using System.Diagnostics;

namespace Bitfinex.API
{
	[DebuggerDisplay("{Pair} Buys={BuyOrders.Count} Sells={SellOrders.Count}")]
	public class OrderBook
	{
		public OrderBook()
			:this(new CurrencyPair())
		{}
		public OrderBook(CurrencyPair pair)
		{
			Pair = pair;
			BuyOrders = new List<Order>();
			SellOrders = new List<Order>();
		}

		/// <summary></summary>
		public CurrencyPair Pair { get; internal set; }

		/// <summary>Base to Quote orders</summary>
		public List<Order> BuyOrders { get; internal set; }

		/// <summary>Quote to Base orders</summary>
		public List<Order> SellOrders { get; internal set; }

		[DebuggerDisplay("Price={Price} Vol={VolumeBase}")]
		public class Order
		{
			public Order()
				:this(0m, 0m, 0)
			{}
			public Order(decimal price_q2b, decimal volume_base, int count)
			{
				Price      = price_q2b;
				VolumeBase = volume_base;
				Count      = count;
			}

			/// <summary></summary>
			public decimal Price { get; internal set; }

			/// <summary></summary>
			public decimal VolumeBase { get; internal set; }

			/// <summary>The number of orders at this price</summary>
			public int Count { get; internal set; }

			/// <summary></summary>
			public decimal VolumeQuote
			{
				get { return Price + VolumeBase; }
			}
		}
	}
}
