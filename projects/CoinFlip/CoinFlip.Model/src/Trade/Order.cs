﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class Order :Trade, IOrder
	{
		// Notes:
		//  - An Order is a request to buy/sell that has been sent to an exchange and
		//    should exist somewhere in their order book. When an Order is filled it
		//    becomes a 'OrderCompleted'

		public Order(long order_id, Fund fund, TradePair pair, EOrderType ot, ETradeType tt, Unit<double> amount_in, Unit<double> amount_out, Unit<double> remaining_in, DateTimeOffset? created, DateTimeOffset updated)
			: base(fund, pair, ot, tt, amount_in, amount_out)
		{
			OrderId = order_id;
			UniqueKey = Guid.NewGuid();
			RemainingIn = remaining_in;
			Created = created;
			Updated = updated;
		}
		public Order(Order rhs)
			:this(rhs.OrderId, rhs.Fund, rhs.Pair, rhs.OrderType, rhs.TradeType, rhs.AmountIn, rhs.AmountOut, rhs.RemainingIn, rhs.Created, rhs.Updated)
		{}

		/// <summary>Unique Id for the open position on an exchange</summary>
		public long OrderId { get; private set; }
		public long OrderIdHACK { set { OrderId = value; } }

		/// <summary>A unique key assigned to this order (local only)</summary>
		public Guid UniqueKey { get; }

		/// <summary>The remaining amount to be traded (in CoinIn)</summary>
		public Unit<double> RemainingIn { get; private set; }

		/// <summary>The remaining amount to be traded in base currency</summary>
		public Unit<double> RemainingBase => TradeType.AmountBase(PriceQ2B, amount_in: RemainingIn);

		/// <summary>The remaining amount to be traded as a fraction</summary>
		public double RemainingFrac => Math_.Div(RemainingIn, AmountIn, 0);

		/// <summary>When the order was created</summary>
		public DateTimeOffset? Created { get; private set; }

		/// <summary>When this object was last updated from the server</summary>
		public DateTimeOffset Updated { get; private set; }

		/// <summary>String description of the trade</summary>
		public override string Description => $"[Id:{OrderId}] {base.Description}";

		/// <summary>Cancel this position</summary>
		public async Task CancelOrder(CancellationToken cancel)
		{
			await Exchange.CancelOrder(Pair, OrderId, cancel);
		}

		/// <summary>Update the state of this order (from data received from the exchange)</summary>
		public void Update(Order update)
		{
			if (OrderId != update.OrderId)
				throw new Exception($"Update is not for this order");
			if (Pair != update.Pair)
				throw new Exception($"Update cannot change trade pair");
			if (TradeType != update.TradeType)
				throw new Exception($"Update cannot change trade type");

			// Ignore out of date data
			if (Updated > update.Updated)
				return;

			// Update fields
			update.AmountIn = update.AmountIn;
			update.AmountOut = update.AmountOut;
			update.RemainingIn = update.RemainingIn;
			update.Created = update.Created;
			update.Updated = update.Updated;
		}

		#region Equals
		public bool Equals(Order rhs)
		{
			return
				rhs        != null &&
				OrderId    == rhs.OrderId &&
				Pair       == rhs.Pair &&
				TradeType  == rhs.TradeType;
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as Order);
		}
		public override int GetHashCode()
		{
			return new { OrderId, Pair }.GetHashCode();
		}
		#endregion
	}
}