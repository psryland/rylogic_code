using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoinFlip
{
	public class TradeCompletedCollection :IEnumerable<TradeCompleted>
	{
		private readonly OrderCompleted m_order;
		private readonly Dictionary<long, TradeCompleted> m_trades;
		public TradeCompletedCollection(OrderCompleted order)
		{
			m_order = order;
			m_trades = new Dictionary<long, TradeCompleted>();
		}

		/// <summary>The number of trades</summary>
		public int Count => m_trades.Count;

		/// <summary>Add or update and order fill</summary>
		public TradeCompleted AddOrUpdate(TradeCompleted fill)
		{
			Debug.Assert(Misc.AssertMainThread());
			Debug.Assert(fill.OrderId == m_order.OrderId);
			Debug.Assert(fill.Created > Misc.CryptoCurrencyEpoch && fill.AmountIn > 0 && fill.AmountOut > 0);

			// This is no update, just replace
			m_trades[fill.TradeId] = fill;
			m_order.NotifyPropertyChanged(nameof(OrderCompleted.Trades));
			return fill;
		}

		/// <summary>Get a trade by trade id</summary>
		public TradeCompleted this[long key]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				return m_trades.TryGetValue(key, out var pos) ? pos : null;
			}
		}

		#region IEnumerable
		public IEnumerator<TradeCompleted> GetEnumerator()
		{
			return m_trades.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
