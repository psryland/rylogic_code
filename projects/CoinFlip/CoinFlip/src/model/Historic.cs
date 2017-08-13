using System;
using System.Diagnostics;
using pr.util;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class Historic
	{
		public Historic(ulong order_id, ulong trade_id, TradePair pair, ETradeType tt, Unit<decimal> price, Unit<decimal> volume_in, Unit<decimal> volume_out, Unit<decimal> commission, DateTimeOffset created, DateTimeOffset updated)
		{
			OrderId     = order_id;
			TradeId     = trade_id;
			Pair        = pair;
			TradeType   = tt;
			Price       = price;
			VolumeIn    = volume_in;
			VolumeOut   = volume_out;
			Commission  = commission;
			Created     = created;
			UpdatedTime = updated;

			// Check units
			if (price <= 0m._(volume_out) / 1m._(volume_in))
				throw new Exception("Invalid price");
			if (commission < 0m._(volume_out))
				throw new Exception("Negative commission");
		}
		public Historic(ulong trade_id, Position pos, DateTimeOffset updated)
			:this(pos.OrderId, trade_id, pos.Pair, pos.TradeType,
				 pos.TradeType == ETradeType.B2Q ? pos.Price : (1m / pos.Price),
				 pos.TradeType == ETradeType.B2Q ? pos.VolumeBase : pos.VolumeQuote,
				 pos.TradeType == ETradeType.B2Q ? pos.VolumeQuote : pos.VolumeBase,
				 pos.TradeType == ETradeType.B2Q ? (pos.VolumeQuote * pos.Pair.Fee) : (pos.VolumeBase * pos.Pair.Fee),
				 pos.Created.Value, updated)
		{ }

		/// <summary>Unique Id for the open position on an exchange</summary>
		public ulong OrderId { get; private set; }
		public ulong OrderIdHACK { set { OrderId = value; } }

		/// <summary>Unique Id for a completed trade. 0 means not completed</summary>
		public ulong TradeId { get; private set; }

		/// <summary>The pair being traded</summary>
		public TradePair Pair { get; private set; }

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; private set; }

		/// <summary>The price of the trade (in VolumeOut/VolumeIn units)</summary>
		public Unit<decimal> Price { get; set; }
		public Unit<decimal> PriceInv
		{
			get { return Price != 0m._(Price) ? (1m / Price) : (0m._(VolumeIn) / 1m._(VolumeOut)); }
		}
		public Unit<decimal> PriceQ2B
		{
			get { return TradeType == ETradeType.B2Q ? Price : PriceInv; }
		}

		/// <summary>The input volume of the trade (in base or quote, depending on 'TradeType')</summary>
		public Unit<decimal> VolumeIn { get; private set; }

		/// <summary>The output volume of the trade excluding commissions (in quote or base, depending on 'TradeType')</summary>
		public Unit<decimal> VolumeOut { get; private set; }

		/// <summary>The volume received after commissions</summary>
		public Unit<decimal> VolumeNett { get { return VolumeOut - Commission; } }

		/// <summary>The commission that was charged on this trade (in the same currency as VolumeOut)</summary>
		public Unit<decimal> Commission { get; private set; }

		/// <summary>When the order was created</summary>
		public DateTimeOffset Created { get; private set; }

		/// <summary>When this object was last updated from the server</summary>
		public DateTimeOffset UpdatedTime { get; private set; }

		/// <summary>The coin type being sold</summary>
		public Coin CoinIn
		{
			get { return TradeType == ETradeType.B2Q ? Pair.Base : Pair.Quote; }
		}

		/// <summary>The coin type being bought</summary>
		public Coin CoinOut
		{
			get { return TradeType == ETradeType.B2Q ? Pair.Quote : Pair.Base; }
		}

		/// <summary>Description string for the trade</summary>
		public string Description
		{
			get
			{
				var sym0 = TradeType == ETradeType.B2Q ? Pair.Base : Pair.Quote;
				var sym1 = TradeType == ETradeType.B2Q ? Pair.Quote : Pair.Base;
				return $"{VolumeIn.ToString("G6")} {sym0} → {VolumeOut.ToString("G6")} {sym1} @ {PriceQ2B.ToString("G6")}";
			}
		}

		#region Equals
		public bool Equals(Historic rhs)
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
			return Equals(obj as Historic);
		}
		public override int GetHashCode()
		{
			return new { OrderId, Pair }.GetHashCode();
		}
		#endregion
	}
}
