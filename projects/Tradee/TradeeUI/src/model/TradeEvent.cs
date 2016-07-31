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
		// Notes:
		// This event is typically the first event.
		// The created trade is not yet Actioned, it's just a potential trade

		public TradeEvent_Created()
		{ }
	}
}
