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

		/// <summary>Remove orders that are older than 'timestamp' and not in 'order_ids'</summary>
		public void RemoveOrdersNotIn(HashSet<long> order_ids, DateTimeOffset timestamp)
		{
			// Remove any positions that are no longer valid.
			foreach (var pos in Values.Where(x => !order_ids.Contains(x.OrderId)).ToArray())
			{
				if (pos.Created >= timestamp) continue;
				if (Model.AllowTrades == false && pos.OrderId < 100) continue; // Hack for fake positions
				Remove(pos.OrderId);
			}
		}
	}
}
