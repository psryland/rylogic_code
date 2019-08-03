using System;
using System.Diagnostics;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A single trade offer in an order book</summary>
	[DebuggerDisplay("{Description,nq}")]
	public struct Offer : IComparable<Offer>
	{
		public Offer(Unit<double> price_q2b, Unit<double> amount_base)
		{
			Debug.Assert(!price_q2b.IsUnit(0));
			PriceQ2B = price_q2b;
			AmountBase = amount_base;
		}

		/// <summary>The price (to buy or sell) (in Quote/Base)</summary>
		public Unit<double> PriceQ2B { get; set; }

		/// <summary>The volume (in base currency)</summary>
		public Unit<double> AmountBase { get; set; }

		/// <summary>The volume (in quote currency)</summary>
		public Unit<double> AmountQuote => AmountBase * PriceQ2B;

		/// <summary>The 'In' side of the offer</summary>
		public Unit<double> AmountIn(ETradeType tt) => tt.AmountIn(AmountBase, PriceQ2B);

		/// <summary>The 'Out' side of the offer</summary>
		public Unit<double> AmountOut(ETradeType tt) => tt.AmountOut(AmountBase, PriceQ2B);

		/// <summary>Orders are compared by price</summary>
		public int CompareTo(Offer rhs) => PriceQ2B.CompareTo(rhs.PriceQ2B);

		/// <summary>Offer description</summary>
		public string Description => $"Price={PriceQ2B} Amount={AmountBase}";

		/// <summary>Check this order against the limits given in 'pair'</summary>
		public Exception Validate(TradePair pair)
		{
			if (PriceQ2B <= 0.0)
				return new Exception($"Offer price ({PriceQ2B.ToString(6)}) is <= 0");

			if (!pair.AmountRangeBase.Contains(AmountBase))
				return new Exception($"Offer amount ({AmountBase.ToString(6)}) is not within the valid range: [{pair.AmountRangeBase.Beg},{pair.AmountRangeBase.End}]");

			if (!pair.AmountRangeQuote.Contains(AmountQuote))
				return new Exception($"Offer amount ({AmountQuote.ToString(6)}) is not within the valid range: [{pair.AmountRangeQuote.Beg},{pair.AmountRangeQuote.End}]");

			return null;
		}
	}
}
