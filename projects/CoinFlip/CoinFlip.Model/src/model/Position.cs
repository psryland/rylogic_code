using System;
using System.Diagnostics;
using pr.util;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class Position
	{
		public Position(ulong order_id, TradePair pair, ETradeType tt, Unit<decimal> price, Unit<decimal> volume, Unit<decimal> remaining, DateTimeOffset? created, DateTimeOffset updated, bool fake = false)
		{
			OrderId    = order_id;
			UniqueKey  = Guid.NewGuid();
			Pair       = pair;
			TradeType  = tt;
			Price      = price;
			VolumeBase = volume;
			Remaining  = remaining;
			Created    = created;
			Updated    = updated;
			Fake       = fake;
		}

		/// <summary>Unique Id for the open position on an exchange</summary>
		public ulong OrderId { get; private set; }
		public ulong OrderIdHACK { set { OrderId = value; } }

		/// <summary>A unique key assigned to this position (local only)</summary>
		public Guid UniqueKey { get; private set; }

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; private set; }

		/// <summary>The pair being traded</summary>
		public TradePair Pair { get; private set; }

		/// <summary>The exchange that this position is on</summary>
		public Exchange Exchange { get { return Pair.Exchange; } }

		/// <summary>The price that the order trades at</summary>
		public Unit<decimal> Price { get; private set; }

		/// <summary>The volume of the trade (in base currency)</summary>
		public Unit<decimal> VolumeBase { get; private set; }

		/// <summary>The volume of the trade (in quote currency) based on the trade price</summary>
		public Unit<decimal> VolumeQuote { get { return VolumeBase * Price; } }

		/// <summary>The remaining volume to be traded</summary>
		public Unit<decimal> Remaining { get; private set; }

		/// <summary>When the order was created</summary>
		public DateTimeOffset? Created { get; private set; }

		/// <summary>When this object was last updated from the server</summary>
		public DateTimeOffset Updated { get; private set; }

		/// <summary>True if this order is not really on the exchange</summary>
		public bool Fake { get; private set; }

		/// <summary>String description of the trade</summary>
		public string Description
		{
			get
			{
				var volI = TradeType == ETradeType.B2Q ? VolumeBase : VolumeQuote;
				var volO = TradeType == ETradeType.B2Q ? VolumeQuote : VolumeBase;
				var sym0 = TradeType == ETradeType.B2Q ? Pair.Base : Pair.Quote;
				var sym1 = TradeType == ETradeType.B2Q ? Pair.Quote : Pair.Base;
				return $"[Id:{OrderId}] {volI.ToString("G6")} {sym0} → {volO.ToString("G6")} {sym1} @ {Price.ToString("G6")} {Pair.RateUnits}"; }
		}

		/// <summary>Cancel this position</summary>
		public void CancelOrder()
		{
			Exchange.CancelOrder(Pair, OrderId);
		}

		/// <summary>Simulate this order being filled. Must be a Fake order</summary>
		public void FillFakeOrder()
		{
			if (!Fake)
				throw new Exception("Cannot fill a live order");

			// Cancel the order
			CancelOrder();

			// Add a fake entry to the history
			var fill = Exchange.History.GetOrAdd(OrderId, TradeType, Pair);
			var tid = (ulong)fill.Trades.Count;
			fill.Trades[tid] = new Historic(tid, this, DateTimeOffset.Now);
			Exchange.HistoryUpdateRequired = true;
		}

		#region Equals
		public bool Equals(Position rhs)
		{
			return
				rhs        != null &&
				OrderId    == rhs.OrderId &&
				Pair       == rhs.Pair &&
				TradeType  == rhs.TradeType;
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as Position);
		}
		public override int GetHashCode()
		{
			return new { OrderId, Pair }.GetHashCode();
		}
		#endregion
	}
}
