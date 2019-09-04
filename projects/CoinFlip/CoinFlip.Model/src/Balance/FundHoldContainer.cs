using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A collection of hold instances</summary>
	public class FundHoldContainer :IEnumerable<FundHold>
	{
		private readonly List<FundHold> m_holds;
		public FundHoldContainer(Fund fund)
		{
			Fund = fund;
			m_holds = new List<FundHold>();
		}

		/// <summary>The associated fund</summary>
		public Fund Fund { get; }

		/// <summary>The sum of holds in this container</summary>
		public Unit<double> Total => m_holds.Sum(x => x.Amount);

		/// <summary>The sum of holds held locally only</summary>
		public Unit<double> Local => m_holds.Where(x => x.Local).Sum(x => x.Amount);

		/// <summary>The sum of holds that the exchange is aware oforder</summary>
		public Unit<double> ExchHeld => m_holds.Where(x => !x.Local).Sum(x => x.Amount);

		/// <summary>Create a new hold instance</summary>
		public FundHold Create(Unit<double> amount, Guid? id = null, long? order_id = null, bool local = true)
		{
			var hold = m_holds.Add2(new FundHold(id ?? Guid.NewGuid(), order_id, amount, local));
			NotifyHoldsChanged();
			return hold;
		}

		/// <summary>Look for the fund hold associated with 'id' or 'order_id'</summary>
		public bool TryGet(long order_id, out FundHold hold)
		{
			hold = m_holds.FirstOrDefault(x => x.OrderId == order_id);
			return hold != null;
		}
		public bool TryGet(Guid id, out FundHold hold)
		{
			hold = m_holds.FirstOrDefault(x => x.Id == id);
			return hold != null;
		}

		/// <summary>Remove fund holds by id. Returns true if something was removed</summary>
		public void Remove(Guid? id = null, long? order_id = null)
		{
			if (m_holds.RemoveAll(x => x.Id == id || x.OrderId == order_id) != 0)
				NotifyHoldsChanged();
		}
		public void Remove(FundHold hold)
		{
			if (m_holds.Remove(hold))
				NotifyHoldsChanged();
		}

		/// <summary>Access a fund hold by id</summary>
		public FundHold this[Guid id]
		{
			get => m_holds.FirstOrDefault(x => x.Id == id);
		}

		/// <summary>Access a fund hold by order id</summary>
		public FundHold this[long order_id]
		{
			get => m_holds.FirstOrDefault(x => x.OrderId == order_id);
		}

		///// <summary>Remove holds whose 'StillNeeded' callback returns false</summary>
		//public void CheckHolds()
		//{
		//	if (m_holds.RemoveAll(x => x.StillNeeded != null && !x.StillNeeded()) != 0)
		//		NotifyHoldsChanged();
		//}

		/// <summary>Raised when the holds collection changes</summary>
		public event EventHandler HoldsChanged;
		public void NotifyHoldsChanged()
		{
			HoldsChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Enumerable all holds</summary>
		public IEnumerator<FundHold> GetEnumerator()
		{
			return m_holds.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
