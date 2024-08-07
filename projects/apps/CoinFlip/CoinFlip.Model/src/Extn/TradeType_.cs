﻿using System;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	public static class TradeType_
	{
		/// <summary>Return the opposite trade type</summary>
		public static ETradeType Opposite(this ETradeType tt)
		{
			return
				tt == ETradeType.Q2B ? ETradeType.B2Q :
				tt == ETradeType.B2Q ? ETradeType.Q2B :
				throw new Exception($"Unknown trade type: {tt}");
		}

		/// <summary>Returns +1 for Q2B, -1 for B2Q</summary>
		public static int Sign(this ETradeType tt)
		{
			return
				tt == ETradeType.Q2B ? +1 :
				tt == ETradeType.B2Q ? -1 :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the unit type for a trade of this type on 'pair'</summary>
		public static string RateUnits(this ETradeType tt, TradePair pair)
		{
			return
				tt == ETradeType.B2Q ? pair.RateUnits :
				tt == ETradeType.Q2B ? pair.RateUnitsInv :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the 'in' coin for a trade on 'pair' in this trade direction</summary>
		public static Coin CoinIn(this ETradeType tt, TradePair pair)
		{
			return
				tt == ETradeType.B2Q ? pair.Base :
				tt == ETradeType.Q2B ? pair.Quote :
				throw new Exception("Unknown trade type");
		}
		public static string CoinIn(this ETradeType tt, CoinPair pair)
		{
			return
				tt == ETradeType.B2Q ? pair.Base :
				tt == ETradeType.Q2B ? pair.Quote :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the 'out' coin for a trade on 'pair' in this trade direction</summary>
		public static Coin CoinOut(this ETradeType tt, TradePair pair)
		{
			return
				tt == ETradeType.B2Q ? pair.Quote :
				tt == ETradeType.Q2B ? pair.Base :
				throw new Exception("Unknown trade type");
		}
		public static string CoinOut(this ETradeType tt, CoinPair pair)
		{
			return
				tt == ETradeType.B2Q ? pair.Quote :
				tt == ETradeType.Q2B ? pair.Base :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return 'price' in (CoinOut/CoinIn) for this trade direction. Assumes price is in (Quote/Base)</summary>
		public static Unit<decimal> Price(this ETradeType tt, Unit<decimal> price_q2b)
		{
			return
				price_q2b == 0m ? 0m/1m._(price_q2b) :
				tt == ETradeType.B2Q ? price_q2b :
				tt == ETradeType.Q2B ? (1m / price_q2b) :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return 'price' in (Quote/Base) for this trade direction. Assumes price is in (CoinOut/CoinIn)</summary>
		public static Unit<decimal> PriceQ2B(this ETradeType tt, Unit<decimal> price)
		{
			return
				price == 0m ? 0m/1m._(price) :
				tt == ETradeType.B2Q ? price :
				tt == ETradeType.Q2B ? (1m / price) :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return 'price' in (Quote/Base) for this trade direction. Assumes price is in (CoinOut/CoinIn)</summary>
		public static Unit<decimal> PriceQ2B(this ETradeType tt, Unit<decimal> amount_out, Unit<decimal> amount_in)
		{
			return tt.PriceQ2B(amount_out / amount_in);
		}

		/// <summary>Return the 'base' amount for a trade in this trade direction</summary>
		public static Unit<decimal> AmountBase(this ETradeType tt, Unit<decimal> price_q2b, Unit<decimal>? amount_in = null, Unit<decimal>? amount_out = null)
		{
			var price = (Unit<decimal>?)price_q2b;
			return
				tt == ETradeType.B2Q ? (amount_in ?? (amount_out / price) ?? throw new Exception("One of 'amount_in' or 'amount_out' must be given")) :
				tt == ETradeType.Q2B ? (amount_out ?? (amount_in / price) ?? throw new Exception("One of 'amount_in' or 'amount_out' must be given")) :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the 'quote' amount for a trade in this trade direction</summary>
		public static Unit<decimal> AmountQuote(this ETradeType tt, Unit<decimal> price_q2b, Unit<decimal>? amount_in = null, Unit<decimal>? amount_out = null)
		{
			var price = (Unit<decimal>?)price_q2b;
			return
				tt == ETradeType.B2Q ? (amount_out ?? (amount_in * price) ?? throw new Exception("One of 'amount_in' or 'amount_out' must be given")) :
				tt == ETradeType.Q2B ? (amount_in ?? (amount_out * price) ?? throw new Exception("One of 'amount_in' or 'amount_out' must be given")) :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the 'in' amount for a trade in this trade direction</summary>
		public static Unit<decimal> AmountIn(this ETradeType tt, Unit<decimal> amount_base, Unit<decimal> price_q2b)
		{
			return
				tt == ETradeType.B2Q ? amount_base :
				tt == ETradeType.Q2B ? amount_base * price_q2b :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the 'out' amount for a trade in this trade direction</summary>
		public static Unit<decimal> AmountOut(this ETradeType tt, Unit<decimal> amount_base, Unit<decimal> price_q2b)
		{
			return
				tt == ETradeType.B2Q ? amount_base * price_q2b :
				tt == ETradeType.Q2B ? amount_base :
				throw new Exception("Unknown trade type");
		}
	}
}
