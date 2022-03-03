using System.Diagnostics;
using Rylogic.Container;

namespace CoinFlip
{
	public class OrdersCompletedCollection : CollectionBase<long, OrderCompleted>
	{
		public OrdersCompletedCollection(Exchange exch)
			: base(exch, x => x.OrderId)
		{}

		/// <summary></summary>
		private BindingDict<long, OrderCompleted> Orders => m_data;

		/// <summary>Reset the collection</summary>
		public void Clear()
		{
			Orders.Clear();
		}

		/// <summary></summary>
		public OrderCompleted Add(OrderCompleted order)
		{
			return Orders.Add2(order);
		}

		/// <summary>Get/Set a history entry by order id. Returns null if 'order_id' is not in the collection</summary>
		public OrderCompleted? this[long order_id]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				return Orders.TryGetValue(order_id, out var pos) ? pos : null;
			}
		}
	}
}
