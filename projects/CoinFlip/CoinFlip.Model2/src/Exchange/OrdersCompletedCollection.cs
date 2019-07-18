using System;
using System.Diagnostics;
using Rylogic.Extn;

namespace CoinFlip
{
	public class OrdersCompletedCollection : CollectionBase<long, OrderCompleted>
	{
		public OrdersCompletedCollection(Exchange exch)
			: base(exch)
		{
			KeyFrom = x => x.OrderId;
		}
		public OrdersCompletedCollection(OrdersCompletedCollection rhs)
			: base(rhs)
		{ }

		/// <summary>Get or Add a history entry with order id 'order_id' for 'pair'</summary>
		public OrderCompleted GetOrAdd(long order_id, Func<long, OrderCompleted> factory)
		{
			Debug.Assert(Misc.AssertMarketDataWrite());
			var order = Dictionary_.GetOrAdd(this, order_id, factory);
			return order;
		}

		/// <summary>Get/Set a history entry by order id. Returns null if 'order_id' is not in the collection</summary>
		public override OrderCompleted this[long order_id]
		{
			get
			{
				Debug.Assert(Misc.AssertMarketDataRead());
				return TryGetValue(order_id, out var pos) ? pos : null;
			}
			set
			{
				Debug.Assert(Misc.AssertMarketDataWrite());
				base[order_id] = value;
			}
		}
	}
}
