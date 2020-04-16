using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A collection of hold instances</summary>
	public class FundHoldContainer :IEnumerable<FundHold>
	{
		private readonly List<FundHold> m_holds;
		private readonly Dispatcher m_dispatcher;
		public FundHoldContainer(Fund fund)
		{
			Fund = fund;
			m_holds = new List<FundHold>();
			m_dispatcher = Dispatcher.CurrentDispatcher;
		}

		/// <summary>The associated fund</summary>
		public Fund Fund { get; }

		/// <summary>The sum of holds in this container</summary>
		public Unit<decimal> Total
		{
			get { lock (m_holds) return m_holds.Sum(x => x.Amount); }
		}

		/// <summary>The sum of holds held locally only</summary>
		public Unit<decimal> Local
		{
			get { lock (m_holds) return m_holds.Where(x => x.Local).Sum(x => x.Amount); }
		}

		/// <summary>The sum of holds that the exchange is aware oforder</summary>
		public Unit<decimal> ExchHeld
		{
			get { lock (m_holds) return m_holds.Where(x => !x.Local).Sum(x => x.Amount); }
		}

		/// <summary>Create a new hold instance</summary>
		public FundHold Create(Unit<decimal> amount, Guid? id = null, long? order_id = null, bool local = true)
		{
			lock (m_holds)
			{
				var hold = m_holds.Add2(new FundHold(id ?? Guid.NewGuid(), order_id, amount, local));
				NotifyHoldsChanged();
				return hold;
			}
		}

		/// <summary>Look for the fund hold associated with 'id' or 'order_id'</summary>
		public bool TryGet(long order_id, out FundHold hold)
		{
			lock (m_holds)
			{
				hold = m_holds.FirstOrDefault(x => x.OrderId == order_id);
				return hold != null;
			}
		}
		public bool TryGet(Guid id, out FundHold hold)
		{
			lock (m_holds)
			{
				hold = m_holds.FirstOrDefault(x => x.Id == id);
				return hold != null;
			}
		}

		/// <summary>Remove fund holds by id. Returns true if something was removed</summary>
		public void Remove(Guid? id = null, long? order_id = null)
		{
			bool removed;
			lock (m_holds)
				removed = m_holds.RemoveAll(x => x.Id == id || x.OrderId == order_id) != 0;
			if (removed)
				NotifyHoldsChanged();
		}
		public void Remove(FundHold hold)
		{
			bool removed;
			lock (m_holds)
				removed = m_holds.Remove(hold);
			if (removed)
				NotifyHoldsChanged();
		}

		/// <summary>Access a fund hold by id</summary>
		public FundHold this[Guid id]
		{
			get { lock (m_holds) return m_holds.FirstOrDefault(x => x.Id == id); }
		}

		/// <summary>Access a fund hold by order id</summary>
		public FundHold this[long order_id]
		{
			get { lock (m_holds) return m_holds.FirstOrDefault(x => x.OrderId == order_id); }
		}

		/// <summary>Raised when the holds collection changes</summary>
		public event EventHandler HoldsChanged;
		public void NotifyHoldsChanged()
		{
			m_dispatcher.BeginInvoke(new Action(() =>
			{
				HoldsChanged?.Invoke(this, EventArgs.Empty);
			}));
		}

		/// <summary>Enumerable all holds</summary>
		public IEnumerator<FundHold> GetEnumerator()
		{
			lock (m_holds)
				return new List<FundHold>(m_holds).GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
