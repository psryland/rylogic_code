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
			:this(pair, order_id, filled, Enumerable.Empty<Fill>())
		{}
		public OrderResult(TradePair pair, long order_id, bool filled, IEnumerable<Fill> trades)
		{
			Pair = pair;
			OrderId = order_id;
			Trades = trades.ToList();
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
			public Fill(long trade_id, Unit<decimal> amount_in, Unit<decimal> amount_out, Unit<decimal>? commission, Coin? commission_coin)
			{
				TradeId = trade_id;
				AmountIn = amount_in;
				AmountOut = amount_out;
				Commission = commission;
				CommissionCoin = commission_coin;
			}

			/// <summary>The exchange assigned Id for the trade</summary>
			public long TradeId { get; }

			/// <summary>The amount that was sold in this trade</summary>
			public Unit<decimal> AmountIn { get; }

			/// <summary>The amount that was bought in this trade (excluding commission)</summary>
			public Unit<decimal> AmountOut { get; }

			/// <summary>The amount of commission charged on the trade (in CommissionCurrency)</summary>
			public Unit<decimal>? Commission { get; }

			/// <summary>The currency that the commission was charged in</summary>
			public Coin? CommissionCoin { get; }

			/// <summary>The price that the trade was filled at</summary>
			public Unit<decimal> PriceQ2B(ETradeType tt) => tt.PriceQ2B(AmountOut / AmountIn);
		}
	}
}
