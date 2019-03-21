using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Rylogic.Utility;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class Order
	{
		// Notes:
		//  - An Order is a request to buy/sell that has been sent to an exchange and
		//    should exist somewhere in their order book. When an Order is filled it
		//    becomes a 'OrderFill'

		public Order(string fund_id, long order_id, TradePair pair, ETradeType tt, Unit<decimal> price_q2b, Unit<decimal> volume_base, Unit<decimal> remaining_base, DateTimeOffset? created, DateTimeOffset updated, bool fake = false)
		{
			FundId        = fund_id;
			OrderId       = order_id;
			UniqueKey     = Guid.NewGuid();
			Pair          = pair;
			TradeType     = tt;
			PriceQ2B      = price_q2b;
			VolumeBase    = volume_base;
			RemainingBase = remaining_base;
			Created       = created;
			Updated       = updated;
			Fake          = fake;
		}
		public Order(Order rhs)
			:this(rhs.FundId, rhs.OrderId, rhs.Pair, rhs.TradeType, rhs.PriceQ2B, rhs.VolumeBase, rhs.RemainingBase, rhs.Created, rhs.Updated, rhs.Fake)
		{}

		/// <summary>The fund associated with this position</summary>
		public string FundId { get; }

		/// <summary>Unique Id for the open position on an exchange</summary>
		public long OrderId { get; private set; }
		public long OrderIdHACK { set { OrderId = value; } }

		/// <summary>A unique key assigned to this position (local only)</summary>
		public Guid UniqueKey { get; }

		/// <summary>The pair being traded</summary>
		public TradePair Pair { get; }

		/// <summary>The exchange that this position is on</summary>
		public Exchange Exchange => Pair.Exchange;

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; }

		/// <summary>The price that the order trades at (Quote/Base)</summary>
		public Unit<decimal> PriceQ2B { get; }

		/// <summary>The volume of the trade (in base currency)</summary>
		public Unit<decimal> VolumeBase { get; }

		/// <summary>The volume of the trade (in quote currency) based on the trade price</summary>
		public Unit<decimal> VolumeQuote => VolumeBase * PriceQ2B;

		/// <summary>The remaining volume to be traded (in base currency)</summary>
		public Unit<decimal> RemainingBase { get; }

		/// <summary>When the order was created</summary>
		public DateTimeOffset? Created { get; }

		/// <summary>When this object was last updated from the server</summary>
		public DateTimeOffset Updated { get; }

		/// <summary>True if this order is not really on the exchange</summary>
		public bool Fake { get; }

		/// <summary>The coin type being sold</summary>
		public Coin CoinIn => TradeType == ETradeType.B2Q ? Pair.Base : Pair.Quote;

		/// <summary>The coin type being bought</summary>
		public Coin CoinOut => TradeType == ETradeType.B2Q ? Pair.Quote : Pair.Base;

		/// <summary>The input volume of the trade (in base or quote, depending on 'TradeType')</summary>
		public Unit<decimal> VolumeIn => TradeType.VolumeIn(VolumeBase, PriceQ2B);

		/// <summary>The remaining volume to be traded (in VolumeIn currency)</summary>
		public Unit<decimal> Remaining => TradeType.VolumeIn(RemainingBase, PriceQ2B);

		/// <summary>The output volume of the trade excluding commissions (in quote or base, depending on 'TradeType')</summary>
		public Unit<decimal> VolumeOut => TradeType.VolumeOut(VolumeBase, PriceQ2B);

		/// <summary>The output volume of the trade including commissions (in quote or base, depending on 'TradeType')</summary>
		public Unit<decimal> VolumeOutNett => VolumeOut - Commission;

		/// <summary>The commission that would be charged on this trade (in the same currency as VolumeOut)</summary>
		public Unit<decimal> Commission => Exchange.Fee * VolumeOut;

		/// <summary>String description of the trade</summary>
		public string Description => $"[Id:{OrderId}] {VolumeIn.ToString("G6", true)} → {VolumeOut.ToString("G6", true)} @ {PriceQ2B.ToString("G6", true)}";

		/// <summary>Cancel this position</summary>
		public async Task CancelOrder()
		{
			await Exchange.CancelOrder(Pair, OrderId);
		}

		/// <summary>Simulate this order being filled. Must be a Fake order</summary>
		public async Task FillFakeOrder()
		{
			if (!Fake)
				throw new Exception("Cannot fill a live order");

			// Cancel the order
			await CancelOrder();

			// Add a fake entry to the history
			var fill = Exchange.History.GetOrAdd(OrderId, TradeType, Pair);
			var tid = (long)fill.Trades.Count;
			fill.Trades[tid] = new TradeCompleted(tid, this, DateTimeOffset.Now);
			Exchange.PositionUpdateRequired = true;

			// Log message
			Model.Log.Write(ELogLevel.Info, $"Fake Order Filled: {Description}");
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
