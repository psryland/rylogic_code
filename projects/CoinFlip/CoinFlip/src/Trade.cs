using System;
using System.Diagnostics;
using System.Threading.Tasks;
using pr.util;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class Trade
	{
		public Trade(ETradeType tt, TradePair pair, Unit<decimal> volume_in, Unit<decimal> volume_out, Unit<decimal> price)
		{
			// Check trade volumes and units
			if (tt == ETradeType.B2Q && (volume_in < 0m._(pair.Base) || volume_out < 0m._(pair.Quote)))
				throw new Exception("Invalid trade volumes");
			if (tt == ETradeType.Q2B && (volume_in < 0m._(pair.Quote) || volume_out < 0m._(pair.Base)))
				throw new Exception("Invalid trade volumes");
			if (price < 0m._(volume_out)/1m._(volume_in))
				throw new Exception("Invalid trade price");

			TradeType = tt;
			Pair      = pair;
			VolumeIn  = volume_in;
			VolumeOut = volume_out;
			Price     = price;
		}
		public Trade(Trade rhs, decimal scale = 1m)
			:this(rhs.TradeType, rhs.Pair, rhs.VolumeIn * scale, rhs.VolumeOut * scale, rhs.Price)
		{}

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; private set; }

		/// <summary>The pair being traded</summary>
		public TradePair Pair { get; private set; }

		/// <summary>The volume being sold</summary>
		public Unit<decimal> VolumeIn { get; set; }

		/// <summary>The volume being bought</summary>
		public Unit<decimal> VolumeOut { get; set; }

		/// <summary>The price of the trade (in VolumeOut/VolumeIn units)</summary>
		public Unit<decimal> Price { get; set; }
		public Unit<decimal> PriceInv { get { return 1m / Price; } }

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

		/// <summary>String description of the trade</summary>
		public string Description
		{
			get
			{
				var sym0 = TradeType == ETradeType.B2Q ? Pair.Base : Pair.Quote;
				var sym1 = TradeType == ETradeType.B2Q ? Pair.Quote : Pair.Base;
				return $"{VolumeIn} {sym0} → {VolumeOut} {sym1} @ {Price}"; }
		}

		/// <summary>Check whether the given trade is an allowed trade</summary>
		public EValidation Validate()
		{
			var result = EValidation.Valid;

			// Check that the trade volumes
			var rangeI = TradeType == ETradeType.B2Q ? Pair.VolumeRangeBase  : Pair.VolumeRangeQuote;
			var rangeO = TradeType == ETradeType.B2Q ? Pair.VolumeRangeQuote : Pair.VolumeRangeBase;
			if (!rangeI.Contains(VolumeIn))
				result |= EValidation.VolumeInOutOfRange;
			if (!rangeO.Contains(VolumeOut))
				result |= EValidation.VolumeOutOutOfRange;

			// Check the price limits.
			var price  = TradeType == ETradeType.B2Q ? Price : 1m / Price;
			if (!Pair.PriceRange.Contains(price))
				result |= EValidation.PriceOutOfRange;

			return result;
		}

		/// <summary>Create this trade on the Exchange that owns 'Pair'</summary>
		public Task<ulong> CreateOrder()
		{
			return Pair.Exchange.CreateOrder(TradeType, Pair, VolumeIn, Price);
		}

		[Flags] public enum EValidation
		{
			Valid             = 0,
			VolumeInOutOfRange  = 1 << 0,
			VolumeOutOutOfRange = 1 << 1,
			PriceOutOfRange     = 1 << 2,
		}
	}
}
