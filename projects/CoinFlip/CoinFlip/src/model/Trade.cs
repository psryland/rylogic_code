using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using pr.extn;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class Trade
	{
		// Notes:
		//  - VolumeIn * Price does not have to equal VolumeOut, because 'Trade' is used
		//    with the order book to calculate the best price for trading 'volume_in'.

		public Trade(ETradeType tt, TradePair pair, Unit<decimal> volume_in, Unit<decimal> volume_out, Unit<decimal> price)
		{
			// Check trade volumes and units
			if (tt == ETradeType.B2Q)
			{
				if (volume_in < 0m._(pair.Base))
					throw new Exception("Invalid trade volume in");
				if (volume_out < 0m._(pair.Quote))
					throw new Exception("Invalid trade volume out");
				if (price < 0m._(pair.RateUnits))
					throw new Exception("Invalid trade price");
			}
			if (tt == ETradeType.Q2B)
			{
				if (volume_in < 0m._(pair.Quote))
					throw new Exception("Invalid trade volume in");
				if (volume_out < 0m._(pair.Base))
					throw new Exception("Invalid trade volume out");
				if (price < 0m._(pair.RateUnitsInv))
					throw new Exception("Invalid trade price");
			}

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

		/// <summary>The volume being bought after commissions</summary>
		public Unit<decimal> VolumeNett
		{
			get { return VolumeOut * (1 - Pair.Fee); }
		}

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

		/// <summary>The effective price after fees</summary>
		public Unit<decimal> PriceNett
		{
			get { return VolumeNett / VolumeIn; }
		}
		public Unit<decimal> PriceNettInv
		{
			get { return PriceNett != 0m._(PriceNett) ? (1m / PriceNett) : (0m._(VolumeIn) / 1m._(VolumeNett)); }
		}
		public Unit<decimal> PriceNettQ2B
		{
			get { return TradeType == ETradeType.B2Q ? PriceNett : PriceNettInv; }
		}

		/// <summary>The position of this trade in the order book for the trade type</summary>
		public int OrderBookIndex
		{
			get { return Pair.OrderBookIndex(TradeType, PriceQ2B); }
		}

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
				return $"{VolumeIn.ToString("G6")} {sym0} → {VolumeOut.ToString("G6")} {sym1} @ {PriceQ2B.ToString("G6")} {Pair.RateUnits}"; }
		}

		/// <summary>Check whether the given trade is an allowed trade</summary>
		public EValidation Validate(Guid? reserved_balance = null)
		{
			var result = EValidation.Valid;

			// Check that the trade volumes
			if (VolumeIn <= 0m._(VolumeIn))
			{
				result |= EValidation.VolumeInIsInvalid;
			}
			else
			{
				var rangeI = TradeType == ETradeType.B2Q ? Pair.VolumeRangeBase  : Pair.VolumeRangeQuote;
				var rangeO = TradeType == ETradeType.B2Q ? Pair.VolumeRangeQuote : Pair.VolumeRangeBase;
				if (!rangeI.Contains(VolumeIn))
					result |= EValidation.VolumeInOutOfRange;
				if (!rangeO.Contains(VolumeOut))
					result |= EValidation.VolumeOutOutOfRange;
			}

			// Check the price limits.
			if (Price == 0m._(Price))
			{
				result |= EValidation.PriceIsInvalid;
			}
			else
			{
				var price = TradeType == ETradeType.B2Q ? Price : 1m / Price;
				if (!Pair.PriceRange.Contains(price))
					result |= EValidation.PriceOutOfRange;
			}

			// Check the balances (allowing for fees)
			var bal = TradeType == ETradeType.B2Q ? Pair.Base.Balance : Pair.Quote.Balance;
			var available = bal.Available + (reserved_balance != null ? bal.Reserved(reserved_balance.Value) : 0m._(bal.Coin));
			if (available < VolumeIn * (1.0000001m + Pair.Fee))
				result |= EValidation.InsufficientBalance;

			return result;
		}

		/// <summary>Create this trade on the Exchange that owns 'Pair'</summary>
		public Task<TradeResult> CreateOrder()
		{
			return Pair.Exchange.CreateOrder(TradeType, Pair, VolumeIn, Price);
		}

		[Flags] public enum EValidation
		{
			Valid               = 0,
			VolumeInOutOfRange  = 1 << 0,
			VolumeOutOutOfRange = 1 << 1,
			PriceOutOfRange     = 1 << 2,
			InsufficientBalance = 1 << 3,
			PriceIsInvalid      = 1 << 4,
			VolumeInIsInvalid   = 1 << 5,
		}
	}

	/// <summary>The result of a submit trade request</summary>
	[DebuggerDisplay("{OrderId} [{TradeIds}]")]
	public class TradeResult
	{
		public TradeResult()
			:this(null)
		{}
		public TradeResult(ulong? order_id)
			:this(order_id, null)
		{}
		public TradeResult(ulong? order_id, IEnumerable<ulong> trade_ids)
		{
			OrderId = order_id;
			TradeIds = trade_ids?.Cast<ulong>().ToList() ?? new List<ulong>();
		}

		/// <summary>The ID of an order that has been added to the order book of a pair</summary>
		public ulong? OrderId { get; private set; }

		/// <summary>Filled orders as a result of a submitted trade</summary>
		public List<ulong> TradeIds { get; private set; }
	}
}
