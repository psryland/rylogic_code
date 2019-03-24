using System;
using System.Diagnostics;
using Dapper;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class TradeCompleted
	{
		// Notes:
		//  - A TradeCompleted is a completed single trade that is part of an OrderCompleted

		public TradeCompleted(long order_id, long trade_id, TradePair pair, ETradeType tt, Unit<decimal> price_q2b, Unit<decimal> amount_base, Unit<decimal> commission_quote, DateTimeOffset created, DateTimeOffset updated)
		{
			// Check units
			if (amount_base <= 0m._(pair.Base))
				throw new Exception("Invalid amount");
			if (price_q2b * amount_base <= 0m._(pair.Quote))
				throw new Exception("Invalid price");
			if (commission_quote < 0m._(pair.Quote))
				throw new Exception("Negative commission");

			OrderId         = order_id;
			TradeId         = trade_id;
			Pair            = pair;
			TradeType       = tt;
			PriceQ2B        = price_q2b;
			AmountBase      = amount_base;
			CommissionQuote = commission_quote;
			Created         = created;
			Updated         = updated;
		}
		public TradeCompleted(long trade_id, Order order, DateTimeOffset updated)
			:this(order.OrderId, trade_id, order.Pair, order.TradeType, order.PriceQ2B, order.AmountBase, order.AmountQuote * order.Exchange.Fee, order.Created.Value, updated)
		{}
		public TradeCompleted(TradeCompleted rhs)
			:this(rhs.OrderId, rhs.TradeId, rhs.Pair, rhs.TradeType, rhs.PriceQ2B, rhs.AmountBase, rhs.CommissionQuote, rhs.Created, rhs.Updated)
		{}

		/// <summary>Unique Id for the open position on an exchange</summary>
		public long OrderId { get; }

		/// <summary>Unique Id for a completed trade. 0 means not completed</summary>
		public long TradeId { get; }

		/// <summary>The pair being traded</summary>
		public TradePair Pair { get; }

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; }

		/// <summary>The price that the trade was filled at</summary>
		public Unit<decimal> PriceQ2B { get; private set; }
		private double PriceQ2BInternal
		{
			get { return (double)(decimal)PriceQ2B; }
			set { PriceQ2B = ((decimal)value)._(Pair.RateUnits); }
		}

		/// <summary>The amount of base currency traded (excludes commission)</summary>
		public Unit<decimal> AmountBase { get; }

		/// <summary>The amount paid in commission (in Quote)</summary>
		public Unit<decimal> CommissionQuote { get; }

		/// <summary>When the trade occurred (in Ticks)</summary>
		public long Timestamp { get; private set; }
		public DateTimeOffset TimestampUTC => new DateTimeOffset(Timestamp, TimeSpan.Zero);

		/// <summary>When the order was created</summary>
		public DateTimeOffset Created { get; }

		/// <summary>When this object was last updated from the server</summary>
		public DateTimeOffset Updated { get; }

		/// <summary>The amount of quote current traded (excludes commission)</summary>
		public Unit<decimal> AmountQuote => AmountBase * PriceQ2B;

		/// <summary>The input amount of the trade (in base or quote, depending on 'TradeType'. Excluding commission)</summary>
		public Unit<decimal> AmountIn => TradeType.AmountIn(AmountBase, PriceQ2B);

		/// <summary>The output amount of the trade excluding commissions (in quote or base, depending on 'TradeType')</summary>
		public Unit<decimal> AmountOut => TradeType.AmountOut(AmountBase, PriceQ2B);

		/// <summary>The amount received after commissions (in AmountOut currency)</summary>
		public Unit<decimal> AmountNett => AmountOut - Commission;

		/// <summary>The commission that was charged on this trade (in the same currency as AmountOut)</summary>
		public Unit<decimal> Commission => TradeType.Commission(CommissionQuote, PriceQ2B);

		/// <summary>The coin type being sold</summary>
		public Coin CoinIn => TradeType.CoinIn(Pair);

		/// <summary>The coin type being bought</summary>
		public Coin CoinOut => TradeType.CoinOut(Pair);

		/// <summary>Description string for the trade</summary>
		public string Description => $"{AmountIn.ToString("G6", true)} → {AmountOut.ToString("G6", true)} @ {PriceQ2B.ToString("G6", true)}";

		#region Equals
		public bool Equals(TradeCompleted rhs)
		{
			return
				rhs        != null &&
				OrderId    == rhs.OrderId &&
				TradeId    == rhs.TradeId &&
				Pair       == rhs.Pair &&
				TradeType  == rhs.TradeType;
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as TradeCompleted);
		}
		public override int GetHashCode()
		{
			return new { OrderId, Pair }.GetHashCode();
		}
		#endregion

		#region SqlMapping
		#endregion
	}
}
