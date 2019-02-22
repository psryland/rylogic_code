using System.Diagnostics;

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
				Debug.Assert(Misc.AssertMarketDataWrite());

				// Ignore out of date data
				if (this[key]?.Updated > value.Updated)
					return;

				base[key] = value;
			}
		}
	}

}
