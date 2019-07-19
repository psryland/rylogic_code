using System;
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

		public Order(string fund_id, long order_id, ETradeType tt, TradePair pair, Unit<double> price_q2b, Unit<double> amount_base, Unit<double> remaining_base, DateTimeOffset? created, DateTimeOffset updated, bool fake = false)
			:base(fund_id, tt, pair, price_q2b, amount_base)
		{
			OrderId = order_id;
			UniqueKey = Guid.NewGuid();
			RemainingBase = remaining_base;
			Created = created;
			Updated = updated;
			Fake = fake;
		}
		public Order(Trade trade, long order_id, Unit<double> remaining_base, DateTimeOffset? created, DateTimeOffset updated, bool fake = false)
			: this(trade.FundId, order_id, trade.TradeType, trade.Pair, trade.PriceQ2B, trade.AmountBase, remaining_base, created, updated, fake)
		{ }
		public Order(Order rhs)
			:this(rhs, rhs.OrderId, rhs.RemainingBase, rhs.Created, rhs.Updated, rhs.Fake)
		{}

		/// <summary>Unique Id for the open position on an exchange</summary>
		public long OrderId { get; private set; }
		public long OrderIdHACK { set { OrderId = value; } }

		/// <summary>A unique key assigned to this order (local only)</summary>
		public Guid UniqueKey { get; }

		/// <summary>The remaining amount to be traded (in base currency)</summary>
		public Unit<double> RemainingBase { get; private set; }

		/// <summary>The remaining amount to be traded (in AmountIn currency)</summary>
		public Unit<double> Remaining => TradeType.AmountIn(RemainingBase, PriceQ2B);
		public double RemainingFrac => Math_.Div(RemainingBase, AmountBase, 0);

		/// <summary>When the order was created</summary>
		public DateTimeOffset? Created { get; private set; }

		/// <summary>When this object was last updated from the server</summary>
		public DateTimeOffset Updated { get; private set; }

		/// <summary>True if this order is not really on the exchange</summary>
		public bool Fake { get; }

		/// <summary>String description of the trade</summary>
		public override string Description => $"[Id:{OrderId}] {base.Description}";

		/// <summary>Cancel this position</summary>
		public async Task CancelOrder(CancellationToken cancel)
		{
			await Exchange.CancelOrder(Pair, OrderId, cancel);
		}

		/// <summary>Simulate this order being filled. Must be a Fake order</summary>
		public async Task FillFakeOrder(CancellationToken cancel)
		{
			if (!Fake)
				throw new Exception("Cannot fill a live order");

			// Cancel the order
			await CancelOrder(cancel);

			// Add a fake entry to the history
			var order_completed = Exchange.History.GetOrAdd(OrderId, x => new OrderCompleted(x, FundId, TradeType, Pair));

			// Add the 'filled' trade to the completed order
			var tid = (long)order_completed.Trades.Count;
			order_completed.Trades[tid] = new TradeCompleted(order_completed, tid, PriceQ2B, AmountBase, CommissionQuote, DateTimeOffset.Now, DateTimeOffset.Now);

			// Signal a history updated required
			Exchange.OrdersUpdateRequired = true;

			// Log message
			Model.Log.Write(ELogLevel.Info, $"Fake Order Filled: {Description}");
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
			update.PriceQ2B = update.PriceQ2B;
			update.AmountBase = update.AmountBase;
			update.RemainingBase = update.RemainingBase;
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
