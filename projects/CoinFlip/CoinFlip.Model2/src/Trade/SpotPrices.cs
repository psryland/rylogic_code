using System;
using System.Diagnostics;
using Rylogic.Utility;

namespace CoinFlip
{
	public class SpotPrices
	{
		private Unit<decimal>? m_spot_q2b;
		private Unit<decimal>? m_spot_b2q;

		public SpotPrices(Coin base_, Coin quote)
		{
			Base = base_;
			Quote = quote;
		}

		/// <summary>The Base currency</summary>
		public Coin Base { get; }

		/// <summary>The quote currency</summary>
		public Coin Quote { get; }

		/// <summary>The units that the spot prices are in</summary>
		public string RateUnits => $"{Quote}/{Base}";

		/// <summary>Get/Set spot price</summary>
		public Unit<decimal>? this[ETradeType tt]
		{
			get
			{
				///// <summary>Return the spot price (Quote/Base) for the given trade type. Requires order book data</summary>
				//public Unit<decimal>? SpotPrice(ETradeType tt)
				//{
				//	// Note: SpotPrice is for the person wanting to buy or sell. If they want to buy
				//	// then they want the cheapest of the sell offers. If they want to sell, they want
				//	// the highest of the buy offers.
				//	switch (tt)
				//	{
				//	default: throw new Exception($"Unknown trade type: {tt}");
				//	case ETradeType.Q2B: return Q2B.Offers.Count != 0 ? Q2B.Offers[0].Price : (Unit<decimal>?)null;
				//	case ETradeType.B2Q: return B2Q.Offers.Count != 0 ? B2Q.Offers[0].Price : (Unit<decimal>?)null;
				//	}
				//}

				return
					tt == ETradeType.Q2B ? m_spot_q2b :
					tt == ETradeType.B2Q ? m_spot_b2q :
					throw new Exception("Unknown trade type");
			}
			set
			{
				if (value < 0m._(RateUnits))
					throw new Exception("Invalid spot price");

				switch (tt)
				{
				default: throw new Exception("Unknown trade type");
				case ETradeType.Q2B: m_spot_q2b = value; break;
				case ETradeType.B2Q: m_spot_b2q = value; break;
				}
				Base.Meta.NotifyLivePriceChanged();
				Quote.Meta.NotifyLivePriceChanged();
			}
		}

		/// <summary>Return the current difference between buy/sell prices</summary>
		public Unit<decimal>? Spread
		{
			// Remember: Q2B spot price = B2Q[0].Price and visa versa
			// Spread is the difference between the buy and sell price,
			// which is always a loss (i.e. negative).
			get
			{
				var spread = -(m_spot_q2b - m_spot_b2q);
				Debug.Assert(spread == null || spread < 0m._(RateUnits));
				return spread;
			}
		}
	}
}
