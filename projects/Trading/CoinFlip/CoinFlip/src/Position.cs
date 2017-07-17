using System;
using System.Diagnostics;
using pr.util;

namespace CoinFlip
{
	[DebuggerDisplay("{TradeType} {Pair} Vol={Volume} Rate={Rate}")]
	public class Position
	{
		public Position(ulong order_id, TradePair pair, ETradeType tt, Unit<decimal> rate, Unit<decimal> volume, Unit<decimal> remaining, DateTimeOffset? timestamp)
		{
			OrderId   = order_id;
			Pair      = pair;
			TradeType = tt;
			Rate      = rate;
			VolumeBase    = volume;
			Remaining = remaining;
			TimeStamp = timestamp;
		}

		/// <summary>Unique ID for the position</summary>
		public ulong OrderId { get; private set; }

		/// <summary>The pair being traded</summary>
		public TradePair Pair { get; private set; }

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; set; }

		/// <summary>The rate that the order trades at</summary>
		public Unit<decimal> Rate { get; set; }

		/// <summary>The volume of the trade (in base currency)</summary>
		public Unit<decimal> VolumeBase { get; set; }

		/// <summary>The volume of the trade (in quote currency) based on the trade price</summary>
		public Unit<decimal> VolumeQuote { get { return VolumeBase * Rate; } }

		/// <summary>The remaining volume to be traded</summary>
		public Unit<decimal> Remaining { get; set; }

		/// <summary>When the order was created</summary>
		public DateTimeOffset? TimeStamp { get; set; }
	}

}
