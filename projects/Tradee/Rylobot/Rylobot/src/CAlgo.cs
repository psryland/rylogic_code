using System;
using System.Collections.Generic;
using System.Reflection;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.extn;
using pr.maths;

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

		#region Trade Type

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

		#endregion

		#region Symbol Extensions

		/// <summary>The current Ask(+1)/Bid(-1)/Mid(0) price</summary>
		public static QuoteCurrency CurrentPrice(this Symbol sym, int sign)
		{
			return
				sign > 0 ? sym.Ask :
				sign < 0 ? sym.Bid :
				(sym.Ask + sym.Bid) / 2.0;
		}

		/// <summary>Convert a price in account currency to quote currency. Remember to multiple by trade volume</summary>
		public static QuoteCurrency AcctToQuote(this Symbol sym, AcctCurrency price)
		{
			return sym.PipsToQuote((double)price / sym.PipValue);
		}

		/// <summary>Convert a price in quote currency to account currency. Remember to multiple by trade volume</summary>
		public static AcctCurrency QuoteToAcct(this Symbol sym, QuoteCurrency price)
		{
			return (double)(sym.QuoteToPips(price) * sym.PipValue);
		}

		/// <summary>Convert a price in quote currency to pips</summary>
		public static Pips QuoteToPips(this Symbol sym, QuoteCurrency price)
		{
			return (double)price / sym.PipSize;
		}

		/// <summary>Convert a value in pips to a price in quote currency</summary>
		public static QuoteCurrency PipsToQuote(this Symbol sym, Pips pips)
		{
			return (double)pips * sym.PipSize;
		}

		#endregion

		#region Position Extensions

		/// <summary>Return the value (in quote currency) of this position when the price is at 'price'</summary>
		public static QuoteCurrency ValueAt(this Position pos, double price, bool consider_sl, bool consider_tp)
		{
			return new Order(pos).ValueAt(price, consider_sl, consider_tp);
		}
		public static QuoteCurrency ValueAt(this PendingOrder pos, double price, bool consider_sl, bool consider_tp)
		{
			return new Order(pos).ValueAt(price, consider_sl, consider_tp);
		}

		public static QuoteCurrency StopLossAbs(this Position pos)
		{
			return new Order(pos).StopLoss;
		}
		public static QuoteCurrency StopLossAbs(this PendingOrder pos)
		{
			return new Order(pos).StopLoss;
		}

		public static QuoteCurrency TakeProfitAbs(this Position pos)
		{
			return new Order(pos).TakeProfit;
		}
		public static QuoteCurrency TakeProfitAbs(this PendingOrder pos)
		{
			return new Order(pos).TakeProfit;
		}

		/// <summary>
		/// Return the stop loss as a signed price value relative to the entry price.
		/// Positive values mean on the losing side (e.g. buy => sign = +1, entry_price - sign*SL = lower price)
		/// Negative values mean on the winning side (e.g. buy => sign = +1, entry_price - sign*SL = higher price)
		/// 0 means no stop loss</summary>
		public static QuoteCurrency StopLossRel(this Position pos)
		{
			return new Order(pos).StopLossRel;
		}
		public static QuoteCurrency StopLossRel(this PendingOrder pos)
		{
			return new Order(pos).StopLossRel;
		}
		public static QuoteCurrency StopLossRel(this Trade pos)
		{
			return new Order(pos, false).StopLossRel;
		}

		/// <summary>
		/// Return the take profit as a signed price value relative to the entry price.
		/// Positive values mean on the winning side (e.g. buy => sign = +1, entry_price + sign*TP = higher price)
		/// Negative values mean on the losing side (e.g. buy => sign = +1, entry_price + sign*TP = lower price)
		/// 0 means no take profit</summary>
		public static QuoteCurrency TakeProfitRel(this Position pos)
		{
			return new Order(pos).TakeProfitRel;
		}
		public static QuoteCurrency TakeProfitRel(this PendingOrder pos)
		{
			return new Order(pos).TakeProfitRel;
		}
		public static QuoteCurrency TakeProfitRel(this Trade pos)
		{
			return new Order(pos, false).TakeProfitRel;
		}

		/// <summary>Return the price level at which this position would break even</summary>
		public static QuoteCurrency BreakEven(this Position pos, AcctCurrency? bias = null)
		{
			var sign = pos.TradeType.Sign();
			var sym = Rylobot.Instance.GetSymbol(pos.SymbolCode);
			var costs = (AcctCurrency)(pos.GrossProfit - pos.NetProfit + (bias ?? 0)) / pos.Volume;

			var break_even = pos.EntryPrice + sign * sym.AcctToQuote(costs);
			return break_even;
		}

		#endregion

		#region DataSeries

		/// <summary>Return the first derivative of the data series at 'index'</summary>
		public static double FirstDerivative(this DataSeries series, NegIdx index)
		{
			// No data => no gradient
			if (series.Count <= 1)
				return double.NaN;

			// Convert from 'NegIdx' to CAlgo index
			var idx = (int)(index + series.Count - 1);

			// Allow indices before the start of the data, assuming zero slope
			if (idx < 0)
				return 0;

			// Forwards single point derivative
			if (idx == 0)
				return (series[idx+1] - series[idx]) / 1.0; // Error O(h)

			// Backwards single point derivative
			if (idx == series.Count-1)
				return (series[idx] - series[idx-1]) / 1.0; // Error O(h)

			// Centred 3 point derivative
			if (idx == 1 || idx == series.Count-2)
				return (series[idx+1] - series[idx-1]) / 2.0;  // Error O(h^2)

			// Centred 5 point derivative
			return (series[idx-2] - 8*series[idx-1] + 8*series[idx+1] - series[idx+2]) / 12.0;  // Error O(h^4)
		}
		public static double FirstDerivative(this DataSeries series)
		{
			return FirstDerivative(series, 0);
		}

		/// <summary>Return the second derivative of the data series at 'index'</summary>
		public static double SecondDerivative(this DataSeries series, NegIdx index)
		{
			// Not enough data
			if (series.Count <= 1)
				return double.NaN;
			if (series.Count <= 2)
				return 0.0;

			var idx = (int)(index + series.Count - 1);

			// Forwards double point 2nd derivative
			if (idx == 0)
				return (series[idx+2] - 2*series[idx+1] + series[idx]) / 1.0; // Error O(h)

			// Backwards double point 2nd derivative
			if (idx == series.Count-1)
				return (series[idx] - 2*series[idx-1] + series[idx-2]) / 1.0; // Error O(h)

			// Centred double point 2nd derivative
			return (series[idx+1] - 2*series[idx] + series[idx-1]) / 1.0;  // Error O(h^2)
		}
		public static double SecondDerivative(this DataSeries series)
		{
			return SecondDerivative(series, 0);
		}

		/// <summary>Integrate the series over the index range [index0,index1)</summary>
		public static double Integrate(this DataSeries series, NegIdx index0, NegIdx index1)
		{
			if (index0 > index1)
				throw new Exception("Invalid index range: [{0},{1})".Fmt(index0, index1));

			var idx0 = (int)(index0 + series.Count - 1);
			var idx1 = (int)(index1 + series.Count - 1);
			idx0 = Maths.Clamp(idx0, 0, series.Count);
			idx1 = Maths.Clamp(idx1, 0, series.Count);

			var sum = 0.0;
			for (int i = idx0; i != idx1; ++i)
				sum += series[i];

			return sum;
		}

		#endregion

		#region Time Frame Extensions

		/// <summary>Enumerate the time frames (in order of increasing time)</summary>
		public static IEnumerable<TimeFrame> Timeframes
		{
			get
			{
				yield return TimeFrame.Minute  ;
				yield return TimeFrame.Minute2 ;
				yield return TimeFrame.Minute3 ;
				yield return TimeFrame.Minute4 ;
				yield return TimeFrame.Minute5 ;
				yield return TimeFrame.Minute6 ;
				yield return TimeFrame.Minute7 ;
				yield return TimeFrame.Minute8 ;
				yield return TimeFrame.Minute9 ;
				yield return TimeFrame.Minute10;
				yield return TimeFrame.Minute15;
				yield return TimeFrame.Minute20;
				yield return TimeFrame.Minute30;
				yield return TimeFrame.Minute45;
				yield return TimeFrame.Hour    ;
				yield return TimeFrame.Hour2   ;
				yield return TimeFrame.Hour3   ;
				yield return TimeFrame.Hour4   ;
				yield return TimeFrame.Hour6   ;
				yield return TimeFrame.Hour8   ;
				yield return TimeFrame.Hour12  ;
				yield return TimeFrame.Daily   ;
				yield return TimeFrame.Day2    ;
				yield return TimeFrame.Day3    ;
				yield return TimeFrame.Weekly  ;
				yield return TimeFrame.Monthly ;
			}
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

		/// <summary>Return a higher or lower time frame (e.g. this=h1, ratio=2 => h2. this=h1, ratio=0.5 => m30))</summary>
		public static TimeFrame GetRelativeTimeFrame(this TimeFrame tf, double ratio)
		{
			// Get the number of ticks the target time frame should have
			var ideal_ticks = tf.ToTicks() * ratio;

			// Return the time frame with the nearest ticks value
			return Timeframes.MinBy(x => Math.Abs(x.ToTicks() - ideal_ticks));
		}

		/// <summary>Convert a time value in 'tf' units to ticks</summary>
		public static long TimeFrameToTicks(double time, TimeFrame tf)
		{
			return (long)(time * tf.ToTicks());
		}

		#endregion
	}
}
