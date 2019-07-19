using System;
using System.Diagnostics;
using Rylogic.Utility;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class TradeCompleted
	{
		// Notes:
		//  - A TradeCompleted is a completed single trade that is part of an OrderCompleted
		//  - TradeCompleted doesn't really need 'pair' and 'tt' because they are duplicates
		//    of fields in the containing OrderCompleted. However, they allow convenient conversion
		//    to CoinIn/CoinOut etc.

		public TradeCompleted(OrderCompleted order_completed, long trade_id, Unit<double> price_q2b, Unit<double> amount_base, Unit<double> commission_quote, DateTimeOffset created, DateTimeOffset updated)
		{
			// Check units
			var pair = order_completed.Pair;
			if (amount_base <= 0.0._(pair.Base))
				throw new Exception("Invalid amount");
			if (price_q2b * amount_base <= 0.0._(pair.Quote))
				throw new Exception("Invalid price");
			if (commission_quote < 0.0._(pair.Quote))
				throw new Exception("Negative commission");

			Order           = order_completed;
			TradeId         = trade_id;
			PriceQ2B        = price_q2b;
			AmountBase      = amount_base;
			CommissionQuote = commission_quote;
			Created         = created;
			Updated         = updated;
		}
		public TradeCompleted(TradeCompleted rhs)
			:this(rhs.Order, rhs.TradeId, rhs.PriceQ2B, rhs.AmountBase, rhs.CommissionQuote, rhs.Created, rhs.Updated)
		{}

		/// <summary>The order that this trade complete is a member of</summary>
		public OrderCompleted Order { get; }

		/// <summary>Unique Id for the open position on an exchange</summary>
		public long OrderId => Order.OrderId;

		/// <summary>The pair being traded</summary>
		public TradePair Pair => Order.Pair;

		/// <summary>The trade type</summary>
		public ETradeType TradeType => Order.TradeType;

		/// <summary>Unique Id for a completed trade. 0 means not completed</summary>
		public long TradeId { get; }

		/// <summary>The price that the trade was filled at</summary>
		public Unit<double> PriceQ2B { get; private set; }
		private double PriceQ2BInternal
		{
			get { return PriceQ2B; }
			set { PriceQ2B = value._(Pair.RateUnits); }
		}

		/// <summary>The amount of base currency traded (excludes commission)</summary>
		public Unit<double> AmountBase { get; }

		/// <summary>The amount paid in commission (in Quote)</summary>
		public Unit<double> CommissionQuote { get; }

		/// <summary>When the trade occurred (in Ticks)</summary>
		public long Timestamp { get; private set; }
		public DateTimeOffset TimestampUTC => new DateTimeOffset(Timestamp, TimeSpan.Zero);

		/// <summary>When the order was created</summary>
		public DateTimeOffset Created { get; }

		/// <summary>When this object was last updated from the server</summary>
		public DateTimeOffset Updated { get; }

		/// <summary>The amount of quote current traded (excludes commission)</summary>
		public Unit<double> AmountQuote => AmountBase * PriceQ2B;

		/// <summary>The input amount of the trade (in base or quote, depending on 'TradeType'. Excluding commission)</summary>
		public Unit<double> AmountIn => TradeType.AmountIn(AmountBase, PriceQ2B);

		/// <summary>The output amount of the trade excluding commissions (in quote or base, depending on 'TradeType')</summary>
		public Unit<double> AmountOut => TradeType.AmountOut(AmountBase, PriceQ2B);

		/// <summary>The amount received after commissions (in AmountOut currency)</summary>
		public Unit<double> AmountNett => AmountOut - Commission;

		/// <summary>The commission that was charged on this trade (in the same currency as AmountOut)</summary>
		public Unit<double> Commission => TradeType.Commission(CommissionQuote, PriceQ2B);

		/// <summary>The coin type being sold</summary>
		public Coin CoinIn => TradeType.CoinIn(Pair);

		/// <summary>The coin type being bought</summary>
		public Coin CoinOut => TradeType.CoinOut(Pair);

		/// <summary>Description string for the trade</summary>
		public string Description => $"{AmountIn.ToString("F8", true)} → {AmountOut.ToString("F8", true)} @ {PriceQ2B.ToString("F8", true)}";

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
	}
}
