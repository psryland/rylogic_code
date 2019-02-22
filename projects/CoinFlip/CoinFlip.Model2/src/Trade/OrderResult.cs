using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CoinFlip
{
	/// <summary>The result of submitting an order request to an exchange</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class OrderResult
	{
		public OrderResult(TradePair pair, bool filled)
			:this(pair, 0, filled)
		{}
		public OrderResult(TradePair pair, long order_id, bool filled)
			:this(pair, order_id, filled, null)
		{}
		public OrderResult(TradePair pair, long order_id, bool filled, IEnumerable<long> trade_ids)
		{
			Pair = pair;
			OrderId = order_id;
			TradeIds = trade_ids?.Cast<long>().ToList() ?? new List<long>();
			Filled = filled;
		}

		/// <summary>The pair that was traded</summary>
		public TradePair Pair { get; }

		/// <summary>The ID of an order that has been added to the order book of a pair</summary>
		public long OrderId { get; }

		/// <summary>Filled orders as a result of a submitted trade</summary>
		public List<long> TradeIds { get; }

		/// <summary>True if the trade is filled immediately</summary>
		public bool Filled { get; }

		/// <summary>A string description of this trade result</summary>
		public string Description => $"{Pair.Name} Id={OrderId} [{string.Join(",", TradeIds)}]";
	}
}
