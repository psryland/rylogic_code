using System;
using System.Diagnostics;
using pr.util;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class Position
	{
		public Position(ulong order_id, ulong trade_id, TradePair pair, ETradeType tt, Unit<decimal> price, Unit<decimal> volume, Unit<decimal> remaining, DateTimeOffset? timestamp)
		{
			OrderId    = order_id;
			TradeId    = trade_id;
			Pair       = pair;
			TradeType  = tt;
			Price      = price;
			VolumeBase = volume;
			Remaining  = remaining;
			TimeStamp  = timestamp;
		}

		/// <summary>Unique Id for the open position on an exchange</summary>
		public ulong OrderId { get; private set; }

		/// <summary>Unique Id for a completed trade. 0 means not completed</summary>
		public ulong TradeId { get; private set; }

		/// <summary>The pair being traded</summary>
		public TradePair Pair { get; private set; }

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; private set; }

		/// <summary>The price that the order trades at</summary>
		public Unit<decimal> Price { get; private set; }

		/// <summary>The volume of the trade (in base currency)</summary>
		public Unit<decimal> VolumeBase { get; private set; }

		/// <summary>The volume of the trade (in quote currency) based on the trade price</summary>
		public Unit<decimal> VolumeQuote { get { return VolumeBase * Price; } }

		/// <summary>The remaining volume to be traded</summary>
		public Unit<decimal> Remaining { get; private set; }

		/// <summary>When the order was created</summary>
		public DateTimeOffset? TimeStamp { get; private set; }

		/// <summary>Description string for the trade</summary>
		public string Description
		{
			get { return $"[Id:{OrderId}] {Pair.Name} {TradeType} Vol={VolumeBase} @ {Price}"; }
		}

		#region Equals
		public bool Equals(Position rhs)
		{
			return
				rhs != null &&
				OrderId == rhs.OrderId &&
				Pair == rhs.Pair &&
				TradeType == rhs.TradeType &&
				Price == rhs.Price &&
				VolumeBase == rhs.VolumeBase;
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
