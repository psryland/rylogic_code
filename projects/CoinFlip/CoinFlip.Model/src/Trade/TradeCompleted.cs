﻿using System;
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

		public TradeCompleted(OrderCompleted order_completed, long trade_id, Unit<double> amount_in, Unit<double> amount_out, Unit<double> commission, Coin commission_coin, DateTimeOffset created, DateTimeOffset updated)
		{
			// Check units
			var pair = order_completed.Pair;
			var tt = order_completed.TradeType;
			if (amount_in <= 0.0._(tt.CoinIn(pair)))
				throw new Exception("Invalid 'in' amount");
			if (amount_out <= 0.0._(tt.CoinOut(pair)))
				throw new Exception("Invalid 'out' amount");
			if (commission < 0.0._(commission_coin))
				throw new Exception("Negative commission");

			Order          = order_completed;
			TradeId        = trade_id;
			AmountIn       = amount_in;
			AmountOut      = amount_out;
			Commission     = commission;
			CommissionCoin = commission_coin;
			Created        = created;
			Updated        = updated;
		}
		public TradeCompleted(TradeCompleted rhs)
			:this(rhs.Order, rhs.TradeId, rhs.AmountIn, rhs.AmountOut, rhs.Commission, rhs.CommissionCoin, rhs.Created, rhs.Updated)
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

		/// <summary>The amount of currency sold (excludes commission)</summary>
		public Unit<double> AmountIn { get; }

		/// <summary>The amount of currency bought (excludes commission)</summary>
		public Unit<double> AmountOut { get; }

		/// <summary>The amount of base currency traded (excludes commission)</summary>
		public Unit<double> AmountBase => TradeType.AmountBase(PriceQ2B, AmountIn, AmountOut);

		/// <summary>The amount of base currency traded (excludes commission)</summary>
		public Unit<double> AmountQuote => TradeType.AmountQuote(PriceQ2B, AmountIn, AmountOut);

		/// <summary>The price that the trade was filled at (in CoinOut/CoinIn)</summary>
		public Unit<double> Price => AmountOut / AmountIn;

		/// <summary>The price that the trade was filled at</summary>
		public Unit<double> PriceQ2B => TradeType.PriceQ2B(Price);

		/// <summary>The commission that was charged on this trade (in CoinOut)</summary>
		public Unit<double> Commission { get; }

		/// <summary>The currency that the commission was charged in</summary>
		public Coin CommissionCoin { get; }

		/// <summary>When the trade occurred (in Ticks)</summary>
		public long Timestamp { get; private set; }
		public DateTimeOffset TimestampUTC => new DateTimeOffset(Timestamp, TimeSpan.Zero);

		/// <summary>When the order was created</summary>
		public DateTimeOffset Created { get; }

		/// <summary>When this object was last updated from the server</summary>
		public DateTimeOffset Updated { get; }

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