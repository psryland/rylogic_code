using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rylogic.Extn;

namespace CoinFlip
{
	public class OrdersCollection : CollectionBase<long, Order>
	{
		public OrdersCollection(Exchange exch)
			: base(exch)
		{
			KeyFrom = x => x.OrderId;
		}
		public OrdersCollection(OrdersCollection rhs)
			: base(rhs)
		{ }

		/// <summary>Add or update an order</summary>
		public Order AddOrUpdate(Order order)
		{
			Debug.Assert(Misc.AssertMarketDataWrite());
			if (TryGetValue(order.OrderId, out var existing))
			{
				existing.Update(order);
				ResetItem(order.OrderId); // Needed since we're updating in-place
			}
			else
			{
				Add(order.OrderId, order);
			}
			return order;
		}

		/// <summary>Get/Set a position by order id</summary>
		public override Order this[long key]
		{
			get
			{
				Debug.Assert(Misc.AssertMarketDataRead());
				return TryGetValue(key, out var pos) ? pos : null;
			}
		}
	}
}
