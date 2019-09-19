using System;
using System.Diagnostics;
using CoinFlip.Settings;
using Rylogic.Utility;

namespace CoinFlip
{
	public class SpotPrices
	{
		/// <summary>The buy and sell price in units of Quote/Base</summary>
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

		/// <summary>Get/Set spot price (in Quote/Base)</summary>
		public Unit<decimal>? this[ETradeType tt]
		{
			get
			{
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

				CoinData.NotifyLivePriceChanged(Base);
				CoinData.NotifyLivePriceChanged(Quote);
			}
		}

		/// <summary>Return the current difference between the lowest Q2B offer and the highest B2Q offer (positive number)</summary>
		public Unit<decimal>? Spread
		{
			// Spread is the difference between the lowest Q2B offer and the highest B2Q offer
			get
			{
				var spread = m_spot_q2b - m_spot_b2q;
				Debug.Assert(spread == null || spread >= 0m._(RateUnits));
				return spread;
			}
		}
	}
}
