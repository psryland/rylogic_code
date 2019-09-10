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

		/// <summary>Add or update an order</summary>
		public Order AddOrUpdate(Order order)
		{
			Debug.Assert(Misc.AssertMainThread());
			Debug.Assert(order.AmountIn > 0 && order.AmountOut > 0 && order.Created > Misc.CryptoCurrencyEpoch); // Sanity check
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

		/// <summary>Get a position by order id</summary>
		public override Order this[long order_id]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				return TryGetValue(order_id, out var ord) ? ord : null;
			}
		}
	}
}
