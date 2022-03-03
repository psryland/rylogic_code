using System.Diagnostics;
using Rylogic.Container;

namespace CoinFlip
{
	public class OrdersCollection :CollectionBase<long, Order>
	{
		public OrdersCollection(Exchange exch)
			:base(exch, x => x.OrderId)
		{}

		/// <summary></summary>
		private BindingDict<long, Order> Orders => m_data;

		/// <summary>Reset the collection</summary>
		public void Clear()
		{
			Orders.Clear();
		}

		/// <summary>Add or update an order</summary>
		public Order AddOrUpdate(Order order)
		{
			Debug.Assert(Misc.AssertMainThread());
			Debug.Assert(order.AmountIn > 0 && order.AmountOut > 0 && order.Created > Misc.CryptoCurrencyEpoch); // Sanity check
			if (Orders.TryGetValue(order.OrderId, out var existing))
			{
				existing.Update(order);
				Orders.ResetItem(order.OrderId); // Needed since we're updating in-place
			}
			else
			{
				Orders.Add(order.OrderId, order);
			}
			return order;
		}

		/// <summary>Remove an order</summary>
		public bool Remove(long order_id)
		{
			return Orders.Remove(order_id);
		}

		/// <summary>Get a position by order id</summary>
		public Order? this[long order_id]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				return Orders.TryGetValue(order_id, out var ord) ? ord : null;
			}
		}
	}
}
