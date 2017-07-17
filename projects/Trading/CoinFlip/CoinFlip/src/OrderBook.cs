using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	/// <summary>Depth of market. The available trades ordered by price. (Increasing for Ask, Decreasing for Bid).</summary>
	[DebuggerDisplay("{Base.Symbol,nq}/{Quote.Symbol,nq} Orders={Count}")]
	public class OrderBook :IEnumerable<Order>
	{
		public OrderBook(Coin base_, Coin quote)
		{
			Orders = new BindingListEx<Order>();
			Base = base_;
			Quote = quote;
		}

		/// <summary>Base currency</summary>
		public Coin Base { get; private set; }

		/// <summary>Quote currency</summary>
		public Coin Quote { get; private set; }

		/// <summary>The buy/sell offers</summary>
		public BindingListEx<Order> Orders { get; private set; }

		/// <summary>The number of orders</summary>
		public int Count
		{
			get { return Orders.Count; }
		}

		/// <summary>Remove all orders</summary>
		public void Clear()
		{
			Orders.Clear();
		}

		/// <summary>Add an order to the depth of market</summary>
		public void Add(Order order, bool validate = true)
		{
			Debug.Assert(!validate || order.VolumeBase != 0m._(Base));
			Debug.Assert(!validate || order.VolumeBase * order.Price != 0m._(Quote));
			Orders.Add(order);
		}

		/// <summary>Array access</summary>
		public Order this[int index]
		{
			get { return Orders[index]; }
		}

		/// <summary>Remove orders up to 'price' or 'volume' (simulating them being filled)</summary>
		public void RemoveOrders(Unit<decimal> volume, Unit<decimal> price)
		{
			var count = 0;
			foreach (var order in Orders)
			{
				if (order.Price > price) break;
				if (order.VolumeBase > volume) break;
				volume -= order.VolumeBase;
				++count;
			}

			// Remove the orders that have been filled and reduce that 
			Orders.RemoveRange(0, count);
			if (volume != 0m._(volume) && Orders.Count != 0 && Orders[0].Price <= price)
				Orders[0] = new Order(Orders[0].Price, Orders[0].VolumeBase - volume);
		}

		/// <summary>Enumerable Orders</summary>
		public IEnumerator<Order> GetEnumerator()
		{
			return Orders.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	/// <summary>A single trade offer</summary>
	[DebuggerDisplay("Price={Price} Vol={VolumeBase}")]
	public struct Order :IComparable<Order>
	{
		public Order(Unit<decimal> price, Unit<decimal> volume)
		{
			Price = price;
			VolumeBase = volume;
		}

		/// <summary>The price (to buy or sell) (in quote currency)</summary>
		public Unit<decimal> Price { get; set; }

		/// <summary>The volume (in base currency)</summary>
		public Unit<decimal> VolumeBase { get; set; }

		/// <summary>The volume (in quote currency)</summary>
		public Unit<decimal> VolumeQuote
		{
			get { return VolumeBase * Price; }
		}

		/// <summary>Orders are compared by price</summary>
		public int CompareTo(Order rhs)
		{
			return Price.CompareTo(rhs.Price);
		}
	}
}
