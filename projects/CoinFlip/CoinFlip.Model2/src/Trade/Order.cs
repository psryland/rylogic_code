using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Rylogic.Maths;
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

		public Order(string fund_id, long order_id, TradePair pair, ETradeType tt, Unit<decimal> price_q2b, Unit<decimal> amount_base, Unit<decimal> remaining_base, DateTimeOffset? created, DateTimeOffset updated, bool fake = false)
		{
			FundId        = fund_id;
			OrderId       = order_id;
			UniqueKey     = Guid.NewGuid();
			Pair          = pair;
			TradeType     = tt;
			PriceQ2B      = price_q2b;
			AmountBase    = amount_base;
			RemainingBase = remaining_base;
			Created       = created;
			Updated       = updated;
			Fake          = fake;
		}
		public Order(Order rhs)
			:this(rhs.FundId, rhs.OrderId, rhs.Pair, rhs.TradeType, rhs.PriceQ2B, rhs.AmountBase, rhs.RemainingBase, rhs.Created, rhs.Updated, rhs.Fake)
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
		public string TradeTypeDesc =>
			TradeType == ETradeType.Q2B ? $"{Pair.Quote}→{Pair.Base} ({TradeType})" :
			TradeType == ETradeType.B2Q ? $"{Pair.Base}→{Pair.Quote} ({TradeType})" :
			"---";

		/// <summary>The price that the order trades at (Quote/Base)</summary>
		public Unit<decimal> PriceQ2B { get; }

		/// <summary>The amount to trade (in base currency)</summary>
		public Unit<decimal> AmountBase { get; }

		/// <summary>The amount to trade (in quote currency) based on the trade price</summary>
		public Unit<decimal> AmountQuote => AmountBase * PriceQ2B;

		/// <summary>The remaining amount to be traded (in base currency)</summary>
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

		/// <summary>The input amount to trade (in base or quote, depending on 'TradeType')</summary>
		public Unit<decimal> AmountIn => TradeType.AmountIn(AmountBase, PriceQ2B);

		/// <summary>The remaining amount to be traded (in AmountIn currency)</summary>
		public Unit<decimal> Remaining => TradeType.AmountIn(RemainingBase, PriceQ2B);
		public decimal RemainingPC => 100m * Math_.Div((decimal)RemainingBase, (decimal)AmountBase, 0m);

		/// <summary>The output amount of the trade excluding commissions (in quote or base, depending on 'TradeType')</summary>
		public Unit<decimal> AmountOut => TradeType.AmountOut(AmountBase, PriceQ2B);

		/// <summary>The output amount of the trade including commissions (in quote or base, depending on 'TradeType')</summary>
		public Unit<decimal> AmountOutNett => AmountOut - Commission;

		/// <summary>The commission that would be charged on this trade (in the same currency as AmountOut)</summary>
		public Unit<decimal> Commission => Exchange.Fee * AmountOut;

		/// <summary>Return the current live price of the pair associated with this order</summary>
		public Unit<decimal>? LivePriceQ2B => Pair.SpotPrice(TradeType);

		/// <summary>The price distance between the order price and the current spot price</summary>
		public Unit<decimal>? DistanceQ2B
		{
			get
			{
				var spot_b2q = Pair.SpotPrice(ETradeType.B2Q);
				var spot_q2b = Pair.SpotPrice(ETradeType.Q2B);
				var dist =
					TradeType == ETradeType.B2Q && spot_b2q != null ? (PriceQ2B - spot_b2q.Value) :
					TradeType == ETradeType.Q2B && spot_q2b != null ? (spot_q2b.Value - PriceQ2B) :
					(Unit<decimal>?)0m;

				return dist;
			}
		}
		public string DistanceQ2BDesc
		{
			get
			{
				var dist = DistanceQ2B;
				return dist != null ? $"{dist:G8} ({Pair.OrderBookIndex(TradeType, PriceQ2B)})" : "---";
			}
		}

		/// <summary>String description of the trade</summary>
		public string Description => $"[Id:{OrderId}] {AmountIn.ToString("G6", true)} → {AmountOut.ToString("G6", true)} @ {PriceQ2B.ToString("G6", true)}";

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
