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
		public OrderCompleted GetOrAdd(string fund_id, long order_id, ETradeType tt, TradePair pair)
		{
			Debug.Assert(Misc.AssertMarketDataWrite());
			return this.GetOrAdd(order_id, x => new OrderCompleted(order_id, fund_id, tt, pair));
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
