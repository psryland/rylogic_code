using System;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.common;

namespace Rylobot
{
	/// <summary>Extension methods for CAlgo types</summary>
	public static class CAlgo
	{
		// Notes:
		//  Currency Pair = AAA/BBB, where AAA = base currency, BBB = quote currency
		//     e.g. AAA/BBB = 1.500 => 1.0*AAA == 1.5*BBB
		//  'PipSize' (units = quote currency) is the smallest unit of change in the quote currency compared to the base currency
		//     e.g. AAA/BBB PipSize = 0.0001 BBB
		//  'Pip' (units = quote currency times a scaling factor) is a count of the number of multiples of 'PipSize' in the Quote currency
		//     e.g +10pips in AAA/BBB = 1.5010 BBB
		//  'PipValue' (units = acct currency) is normally the value of '1' pip in base currency. However 'Symbol.PipValue' is the value in the
		//   account currency:
		//     e.g  AAA/BBB = 1.5, PipValue would normally be '0.0001 BBB * 1*AAA/1.5*BBBB' = '0.0001 / 1.5' = '0.0000666*AAA'
		//          Symbol.PipValue = '0.0000666*AAA * (NZD/AAA)' or '0.0000666*AAA / (AAA/NZD)'
		//          Note: "Pips * PipValue = Value in Acct currency". Remember to convert base or quote currencies to pips before multiplying by 'PipValue'
		//  'Volume' (units = base currency) is Lots * LotSize = the number of units traded
		//     e.g 1 MicroLot of AAA/BBB = 1000*AAA. So Volume = 1000
		//
		// For an arbitrary currency pair:
		//   Delta quote price to Pips  =>  (1.5678 - 1.5000) / Symbol.PipSize = 678pips
		//   Delta pips to account value  =>  (678pips * Symbol.PipValue) * Volume
		// 

		/// <summary>The current Ask(+1)/Bid(-1) price</summary>
		public static double CurrentPrice(this Symbol sym, int sign)
		{
			return sign > 0 ? sym.Ask : sym.Bid;
		}

		/// <summary>
		/// Return the stop loss as a signed price value relative to the entry price.
		/// Positive values mean on the losing side (e.g. buy => sign = +1, entry_price - sign*SL = lower price)
		/// Negative values mean on the winning side (e.g. buy => sign = +1, entry_price - sign*SL = higher price)
		/// 0 means no stop loss</summary>
		public static double StopLossRel(this Position pos)
		{
			switch (pos.TradeType)
			{
			default: throw new Exception("unknown trade type");
			case TradeType.Buy:  return pos.EntryPrice - (pos.StopLoss ?? pos.EntryPrice);
			case TradeType.Sell: return (pos.StopLoss ?? pos.EntryPrice) - pos.EntryPrice;
			}
		}

		/// <summary>
		/// Return the take profit as a signed price value relative to the entry price.
		/// Positive values mean on the winning side (e.g. buy => sign = +1, entry_price + sign*TP = higher price)
		/// Negative values mean on the losing side (e.g. buy => sign = +1, entry_price + sign*TP = lower price)
		/// 0 means no take profit</summary>
		public static double TakeProfitRel(this Position pos)
		{
			switch (pos.TradeType)
			{
			default: throw new Exception("unknown trade type");
			case TradeType.Buy:  return (pos.TakeProfit ?? pos.EntryPrice) - pos.EntryPrice;
			case TradeType.Sell: return pos.EntryPrice - (pos.TakeProfit ?? pos.EntryPrice);
			}
		}

		/// <summary>Return the stop loss as a signed price value relative to the entry price. 0 means no stop loss</summary>
		public static double StopLossRel(this PendingOrder pos)
		{
			switch (pos.TradeType)
			{
			default: throw new Exception("unknown trade type");
			case TradeType.Buy:  return pos.TargetPrice - (pos.StopLoss ?? pos.TargetPrice);
			case TradeType.Sell: return (pos.StopLoss ?? pos.TargetPrice) - pos.TargetPrice;
			}
		}

		/// <summary>Return the take profit as a signed price value relative to the entry price. 0 means no take profit</summary>
		public static double TakeProfitRel(this PendingOrder pos)
		{
			switch (pos.TradeType)
			{
			default: throw new Exception("unknown trade type");
			case TradeType.Buy:  return (pos.TakeProfit ?? pos.TargetPrice) - pos.TargetPrice;
			case TradeType.Sell: return pos.TargetPrice - (pos.TakeProfit ?? pos.TargetPrice);
			}
		}

		/// <summary>Convert a price in quote currency to account currency. Remember to multiple by trade volume</summary>
		public static double AcctToQuotePrice(this Symbol sym, double price)
		{
			return sym.PipsToQuotePrice(price / sym.PipValue);
		}

		/// <summary>Convert a price in quote currency to account currency. Remember to multiple by trade volume</summary>
		public static double QuotePriceToAcct(this Symbol sym, double price)
		{
			return sym.QuotePriceToPips(price) * sym.PipValue;
		}

		/// <summary>Convert a price in quote currency to pips</summary>
		public static double QuotePriceToPips(this Symbol sym, double price)
		{
			return price / sym.PipSize;
		}

		/// <summary>Convert a value in pips to a price in quote currency</summary>
		public static double PipsToQuotePrice(this Symbol sym, double pips)
		{
			return pips * sym.PipSize;
		}

		/// <summary>Return the gradient at 'index'</summary>
		public static double Gradient(this IndicatorDataSeries series, int index)
		{
			if (index == 0) return 0.0;
			return series[index] - series[index-1];
		}

		/// <summary>Return the opposite trade type to this type</summary>
		public static TradeType Opposite(this TradeType tt)
		{
			return tt == TradeType.Buy ? TradeType.Sell : TradeType.Buy;
		}

		/// <summary>Return +1 for buy, -1 for sell</summary>
		public static int Sign(this TradeType tt)
		{
			return tt == TradeType.Buy ? +1 : -1;
		}

		/// <summary>Convert a time-frame value to ticks</summary>
		public static long ToTicks(this TimeFrame tf)
		{
			if (tf == TimeFrame.Minute  ) return TimeSpan.FromMinutes(1).Ticks;
			if (tf == TimeFrame.Minute2 ) return TimeSpan.FromMinutes(2).Ticks;
			if (tf == TimeFrame.Minute3 ) return TimeSpan.FromMinutes(3).Ticks;
			if (tf == TimeFrame.Minute4 ) return TimeSpan.FromMinutes(4).Ticks;
			if (tf == TimeFrame.Minute5 ) return TimeSpan.FromMinutes(5).Ticks;
			if (tf == TimeFrame.Minute6 ) return TimeSpan.FromMinutes(6).Ticks;
			if (tf == TimeFrame.Minute7 ) return TimeSpan.FromMinutes(7).Ticks;
			if (tf == TimeFrame.Minute8 ) return TimeSpan.FromMinutes(8).Ticks;
			if (tf == TimeFrame.Minute9 ) return TimeSpan.FromMinutes(9).Ticks;
			if (tf == TimeFrame.Minute10) return TimeSpan.FromMinutes(10).Ticks;
			if (tf == TimeFrame.Minute15) return TimeSpan.FromMinutes(15).Ticks;
			if (tf == TimeFrame.Minute20) return TimeSpan.FromMinutes(20).Ticks;
			if (tf == TimeFrame.Minute30) return TimeSpan.FromMinutes(30).Ticks;
			if (tf == TimeFrame.Minute45) return TimeSpan.FromMinutes(45).Ticks;
			if (tf == TimeFrame.Hour    ) return TimeSpan.FromHours(1).Ticks;
			if (tf == TimeFrame.Hour2   ) return TimeSpan.FromHours(2).Ticks;
			if (tf == TimeFrame.Hour3   ) return TimeSpan.FromHours(3).Ticks;
			if (tf == TimeFrame.Hour4   ) return TimeSpan.FromHours(4).Ticks;
			if (tf == TimeFrame.Hour6   ) return TimeSpan.FromHours(6).Ticks;
			if (tf == TimeFrame.Hour8   ) return TimeSpan.FromHours(8).Ticks;
			if (tf == TimeFrame.Hour12  ) return TimeSpan.FromHours(12).Ticks;
			if (tf == TimeFrame.Daily   ) return TimeSpan.FromDays(1).Ticks;
			if (tf == TimeFrame.Day2    ) return TimeSpan.FromDays(2).Ticks;
			if (tf == TimeFrame.Day3    ) return TimeSpan.FromDays(3).Ticks;
			if (tf == TimeFrame.Weekly  ) return TimeSpan.FromDays(7).Ticks;
			if (tf == TimeFrame.Monthly ) return TimeSpan.FromDays(30).Ticks;
			throw new Exception("Unknown time frame");
		}

		/// <summary>Convert a time value in 'tf' units to ticks</summary>
		public static long TimeFrameToTicks(double time, TimeFrame tf)
		{
			return (long)(time * tf.ToTicks());
		}

		/// <summary>Return the first derivative of the data series at 'index' (CAlgo index)</summary>
		public static double FirstDerivative(this DataSeries series, int index)
		{
			if (series.Count <= 1)
				return double.NaN;

			// Forwards single point derivative
			if (index == 0)
				return (series[index+1] - series[index]) / 1.0; // Error O(h)

			// Backwards single point derivative
			if (index == series.Count-1)
				return (series[index] - series[index-1]) / 1.0; // Error O(h)

			// Centred 3 point derivative
			if (index == 1 || index == series.Count-2)
				return (series[index+1] - series[index-1]) / 2.0;  // Error O(h^2)

			// Centred 5 point derivative
			return (series[index-2] - 8*series[index-1] + 8*series[index+1] - series[index+2]) / 12.0;  // Error O(h^4)
		}

		/// <summary>Return the second derivative of the data series at 'index'</summary>
		public static double SecondDerivative(this DataSeries series, int index)
		{
			if (series.Count <= 1)
				return double.NaN;
			if (series.Count <= 2)
				return 0.0;

			// Forwards double point 2nd derivative
			if (index == 0)
				return (series[index+2] - 2*series[index+1] + series[index]) / 1.0; // Error O(h)

			// Backwards double point 2nd derivative
			if (index == series.Count-1)
				return (series[index] - 2*series[index-1] + series[index-2]) / 1.0; // Error O(h)

			// Centred double point 2nd derivative
			return (series[index+1] - 2*series[index] + series[index-1]) / 1.0;  // Error O(h^2)
		}
	}
}
