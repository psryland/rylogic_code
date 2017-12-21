using System;
using System.Diagnostics;
using pr.db;
using pr.util;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class Historic
	{
		public Historic(ulong order_id, ulong trade_id, TradePair pair, ETradeType tt, Unit<decimal> price_q2b, Unit<decimal> volume_base, Unit<decimal> commission_quote, DateTimeOffset created, DateTimeOffset updated)
		{
			// Check units
			if (volume_base <= 0m._(pair.Base))
				throw new Exception("Invalid volume");
			if (price_q2b * volume_base <= 0m._(pair.Quote))
				throw new Exception("Invalid price");
			if (commission_quote < 0m._(pair.Quote))
				throw new Exception("Negative commission");

			OrderId         = order_id;
			TradeId         = trade_id;
			Pair            = pair;
			TradeType       = tt;
			PriceQ2B        = price_q2b;
			VolumeBase      = volume_base;
			CommissionQuote = commission_quote;
			Created         = created;
			Updated         = updated;
		}
		public Historic(ulong trade_id, Order pos, DateTimeOffset updated)
			:this(pos.OrderId, trade_id, pos.Pair, pos.TradeType, pos.PriceQ2B, pos.VolumeBase, pos.VolumeQuote * pos.Exchange.Fee, pos.Created.Value, updated)
		{}
		public Historic(Historic rhs)
			:this(rhs.OrderId, rhs.TradeId, rhs.Pair, rhs.TradeType, rhs.PriceQ2B, rhs.VolumeBase, rhs.CommissionQuote, rhs.Created, rhs.Updated)
		{}

		/// <summary>Unique Id for the open position on an exchange</summary>
		public ulong OrderId { get; private set; }

		/// <summary>Unique Id for a completed trade. 0 means not completed</summary>
		public ulong TradeId { get; private set; }

		/// <summary>The pair being traded</summary>
		public TradePair Pair { get; private set; }

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; private set; }

		/// <summary>The price that the trade was filled at</summary>
		public Unit<decimal> PriceQ2B { get; private set; }

		/// <summary>The volume of base currency traded (excludes commission)</summary>
		public Unit<decimal> VolumeBase { get; private set; }

		/// <summary>The volume of quote current traded (excludes commission)</summary>
		public Unit<decimal> VolumeQuote { get { return VolumeBase * PriceQ2B; } }

		/// <summary>The input volume of the trade (in base or quote, depending on 'TradeType'. Excluding commission)</summary>
		//public Unit<decimal> VolumeIn { get; private set; }
		public Unit<decimal> VolumeIn { get { return TradeType.VolumeIn(VolumeBase, PriceQ2B); } }

		/// <summary>The output volume of the trade excluding commissions (in quote or base, depending on 'TradeType')</summary>
		//public Unit<decimal> VolumeOut { get; private set; }
		public Unit<decimal> VolumeOut { get { return TradeType.VolumeOut(VolumeBase, PriceQ2B); } }

		/// <summary>The volume received after commissions (in VolumeOut currency)</summary>
		public Unit<decimal> VolumeNett { get { return VolumeOut - Commission; } }

		/// <summary>The commission that was charged on this trade (in the same currency as VolumeOut)</summary>
		public Unit<decimal> Commission { get { return TradeType.Commission(CommissionQuote, PriceQ2B); } }

		/// <summary>The volume paid in commission (in Quote)</summary>
		public Unit<decimal> CommissionQuote { get; private set; }

		/// <summary>When the order was created</summary>
		public DateTimeOffset Created { get; private set; }

		/// <summary>When this object was last updated from the server</summary>
		public DateTimeOffset Updated { get; private set; }

		/// <summary>The coin type being sold</summary>
		public Coin CoinIn
		{
			get { return TradeType.CoinIn(Pair); }
		}

		/// <summary>The coin type being bought</summary>
		public Coin CoinOut
		{
			get { return TradeType.CoinOut(Pair); }
		}

		/// <summary>Description string for the trade</summary>
		public string Description
		{
			get { return $"{VolumeIn.ToString("G6",true)} → {VolumeOut.ToString("G6",true)} @ {PriceQ2B.ToString("G6", true)}"; }
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
