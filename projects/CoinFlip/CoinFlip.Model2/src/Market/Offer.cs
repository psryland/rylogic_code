using System;
using System.Diagnostics;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A single trade offer in an order book</summary>
	[DebuggerDisplay("Price={Price} Vol={VolumeBase}")]
	public struct Offer : IComparable<Offer>
	{
		public Offer(Unit<decimal> price, Unit<decimal> volume)
		{
			Price = price;
			VolumeBase = volume;
		}

		/// <summary>The price (to buy or sell) (in Quote/Base)</summary>
		public Unit<decimal> Price { get; set; }

		/// <summary>The volume (in base currency)</summary>
		public Unit<decimal> VolumeBase { get; set; }

		/// <summary>The volume (in quote currency)</summary>
		public Unit<decimal> VolumeQuote => VolumeBase * Price;

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

			if (!pair.VolumeRangeBase.Contains(VolumeBase))
				return false;

			if (!pair.VolumeRangeQuote.Contains(VolumeQuote))
				return false;

			return true;
		}
	}
}
