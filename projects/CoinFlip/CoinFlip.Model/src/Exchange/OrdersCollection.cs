using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

		/// <summary>Get/Set a position by order id</summary>
		public override Order this[long key]
		{
			get
			{
				Debug.Assert(Misc.AssertMarketDataRead());
				return TryGetValue(key, out var pos) ? pos : null;
			}
			set
			{
				// If the order already exists, update rather than replace.
				Debug.Assert(Misc.AssertMarketDataWrite());
				if (ContainsKey(key))
				{
					base[key].Update(value);
					ResetItem(key); // Needed since we're updating in-place
				}
				else
				{
					base[key] = value;
				}
			}
		}
	}
}
