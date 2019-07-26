using System;
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
		public static Unit<double> Price(this ETradeType tt, Unit<double> price_q2b)
		{
			return
				tt == ETradeType.B2Q ? price_q2b :
				tt == ETradeType.Q2B ? Math_.Div(1.0._(), price_q2b, 0.0 / 1.0._(price_q2b)) :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return 'price' in (Quote/Base) for this trade direction. Assumes price is in (CoinOut/CoinIn)</summary>
		public static Unit<double> PriceQ2B(this ETradeType tt, Unit<double> price)
		{
			return
				tt == ETradeType.B2Q ? price :
				tt == ETradeType.Q2B ? (1.0 / price) :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the 'base' amount for a trade in this trade direction</summary>
		public static Unit<double> AmountBase(this ETradeType tt, Unit<double> price_q2b, Unit<double>? amount_in = null, Unit<double>? amount_out = null)
		{
			var price = (Unit<double>?)price_q2b;
			return
				tt == ETradeType.B2Q ? (amount_in ?? (amount_out / price) ?? throw new Exception("One of 'amount_in' or 'amount_out' must be given")) :
				tt == ETradeType.Q2B ? ((amount_in / price) ?? amount_out ?? throw new Exception("One of 'amount_in' or 'amount_out' must be given")) :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the 'in' amount for a trade in this trade direction</summary>
		public static Unit<double> AmountIn(this ETradeType tt, Unit<double> amount_base, Unit<double> price_q2b)
		{
			return
				tt == ETradeType.B2Q ? amount_base :
				tt == ETradeType.Q2B ? amount_base * price_q2b :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the 'out' amount for a trade in this trade direction</summary>
		public static Unit<double> AmountOut(this ETradeType tt, Unit<double> amount_base, Unit<double> price_q2b)
		{
			return
				tt == ETradeType.B2Q ? amount_base * price_q2b :
				tt == ETradeType.Q2B ? amount_base :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the commission in 'out' amount for a trade in this trade direction</summary>
		public static Unit<double> Commission(this ETradeType tt, Unit<double> commission_quote, Unit<double> price_q2b)
		{
			return
				tt == ETradeType.B2Q ? (commission_quote) :
				tt == ETradeType.Q2B ? (commission_quote / price_q2b) :
				throw new Exception("Unknown trade type");
		}
	}
}
