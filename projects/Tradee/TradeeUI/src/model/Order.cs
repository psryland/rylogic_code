using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr.maths;

namespace Tradee
{
	/// <summary>A single order to buy or sell</summary>
	public class Order
	{
		// Notes:
		// - Represents a single buy or sell order with the broker.
		// - A trade can have more than one of these.
		// - An order can be pending or active.
		// - Orders don't have associated graphics because they may be part
		//   of an more complex trade

		public Order()
		{
			State = Trade.EState.Experimental;
		}

		/// <summary>The state of this order</summary>
		public Trade.EState State
		{
			get { return m_impl_state; }
			set
			{
				if (m_impl_state == value) return;
				Debug.Assert(Bit.CountBits((int)value) == 1, "An order can only be in one state");
				m_impl_state = value;
			}
		}
		private Trade.EState m_impl_state;


	}
}
