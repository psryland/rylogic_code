using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rylogic.Utility;

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
		public OrderResult(TradePair pair, long order_id, bool filled, IEnumerable<Fill> trades)
		{
			Pair = pair;
			OrderId = order_id;
			Trades = trades?.ToList() ?? new List<Fill>();
			Filled = filled;
		}

		/// <summary>The pair that was traded</summary>
		public TradePair Pair { get; }

		/// <summary>The ID of an order that has been added to the order book of a pair</summary>
		public long OrderId { get; }

		/// <summary>Filled orders as a result of a submitted trade</summary>
		public List<Fill> Trades { get; }

		/// <summary>True if the trade is filled immediately</summary>
		public bool Filled { get; }

		/// <summary>A string description of this trade result</summary>
		public string Description => $"{Pair.Name} Id={OrderId} [{Trades.Count}]";

		/// <summary>Trades that occurred immediately to fill or partially fill the order</summary>
		public class Fill
		{
			public Fill(long trade_id, Unit<double> price, Unit<double> amount, Unit<double> commission)
			{
				TradeId = trade_id;
				Price = price;
				Amount = amount;
				Commission = commission;
			}

			/// <summary>The exchange assigned Id for the trade</summary>
			public long TradeId { get; }

			/// <summary>The price that the trade was filled at</summary>
			public Unit<double> Price { get; }

			/// <summary>The amount that was traded in this trade</summary>
			public Unit<double> Amount { get; }

			/// <summary>The amount of commission charged on the trade</summary>
			public Unit<double> Commission { get; }
		}
	}
}
