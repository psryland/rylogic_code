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

		/// <summary>Get or Add a history entry with order id 'key' for 'pair'</summary>
		public OrderCompleted GetOrAdd(long key, ETradeType tt, TradePair pair)
		{
			Debug.Assert(Misc.AssertMarketDataWrite());
			return this.GetOrAdd(key, x => new OrderCompleted(key, tt, pair));
		}

		/// <summary>Get/Set a history entry by order id. Returns null if 'key' is not in the collection</summary>
		public override OrderCompleted this[long key]
		{
			get
			{
				Debug.Assert(Misc.AssertMarketDataRead());
				return TryGetValue(key, out var pos) ? pos : null;
			}
			set
			{
				Debug.Assert(Misc.AssertMarketDataWrite());
				base[key] = value;
			}
		}
	}

}



