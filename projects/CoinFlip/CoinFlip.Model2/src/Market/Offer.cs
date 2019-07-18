using System;
using System.Diagnostics;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A single trade offer in an order book</summary>
	[DebuggerDisplay("Price={Price} Amount={AmountBase}")]
	public struct Offer : IComparable<Offer>
	{
		// Notes:
		//  - Market depth is the most expensive part of the simulation.
		//    For performance, store price and amount as double's

		public Offer(double price, double amount_base)
		{
			Price = price;
			AmountBase = amount_base;
		}

		/// <summary>The price (to buy or sell) (in Quote/Base)</summary>
		public double Price { get; set; }

		/// <summary>The volume (in base currency)</summary>
		public double AmountBase { get; set; }

		/// <summary>The volume (in quote currency)</summary>
		public double AmountQuote => AmountBase * Price;

		/// <summary>Orders are compared by price</summary>
		public int CompareTo(Offer rhs)
		{
			return Price.CompareTo(rhs.Price);
		}

		/// <summary>Check this order against the limits given in 'pair'</summary>
		public Exception Validate(TradePair pair)
		{
			if (Price <= 0)
				return new Exception($"Offer price ({Price.ToString(6)}) is <= 0");

			if (!pair.AmountRangeBase.Contains(((decimal)AmountBase)._(pair.Base)))
				return new Exception($"Offer amount ({AmountBase.ToString(6)}) is not within the valid range: [{pair.AmountRangeBase.Beg},{pair.AmountRangeBase.End}]");

			if (!pair.AmountRangeQuote.Contains(((decimal)AmountQuote)._(pair.Quote)))
				return new Exception($"Offer amount ({AmountQuote.ToString(6)}) is not within the valid range: [{pair.AmountRangeQuote.Beg},{pair.AmountRangeQuote.End}]");

			return null;
		}
	}
}
