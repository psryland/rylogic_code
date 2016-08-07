using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tradee
{
	/// <summary>Base class for changes made to an order</summary>
	public class TradeEvent
	{
		public TradeEvent()
		{ }
	}

	/// <summary>Trade created event</summary>
	public class TradeEvent_Created :TradeEvent
	{
		public TradeEvent_Created()
		{}
	}

	/// <summary>Records a change to the entry price for a trade</summary>
	public class TradeEvent_SetEntryPrice :TradeEvent
	{
		public TradeEvent_SetEntryPrice(double entry_price)
		{
			EntryPrice = entry_price;
		}

		/// <summary>The entry price level</summary>
		public double EntryPrice { get; private set; }
	}

	/// <summary>Records a change to valid period of time that a trade can be a pending order</summary>
	public class TradeEvent_SetTimeRange :TradeEvent
	{
		public TradeEvent_SetTimeRange(long time_beg, long time_dur)
		{
			TimeBeg = time_beg;
			TimeDur = time_dur;
		}

		/// <summary>The time (in ticks) that a trade starts being a potential pending order</summary>
		public long TimeBeg { get; private set; }

		/// <summary>The duration (in ticks) that a trade remains a potential pending order</summary>
		public long TimeDur { get; private set; }
	}
}
