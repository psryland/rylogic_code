using System;
using System.Diagnostics;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A single trade offer in an order book</summary>
	[DebuggerDisplay("Price={Price} Amount={AmountBase}")]
	public struct Offer : IComparable<Offer>
	{
		public Offer(Unit<decimal> price, Unit<decimal> volume)
		{
			Price = price;
			AmountBase = volume;
		}

		/// <summary>The price (to buy or sell) (in Quote/Base)</summary>
		public Unit<decimal> Price { get; set; }

		/// <summary>The volume (in base currency)</summary>
		public Unit<decimal> AmountBase { get; set; }

		/// <summary>The volume (in quote currency)</summary>
		public Unit<decimal> AmountQuote => AmountBase * Price;

		/// <summary>Orders are compared by price</summary>
		public int CompareTo(Offer rhs)
		{
			return Price.CompareTo(rhs.Price);
		}

		/// <summary>Check this order against the limits given in 'pair'</summary>
		public bool Validate(TradePair pair)
		{
			if (Price <= 0m)
				return false;

			if (!pair.AmountRangeBase.Contains(AmountBase))
				return false;

			if (!pair.AmountRangeQuote.Contains(AmountQuote))
				return false;

			return true;
		}
	}
}
