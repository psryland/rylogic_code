using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	public static class Misc
	{
		/// <summary>The smallest amount change</summary>
		public const decimal AmountEpsilon = 1e-8m;

		/// <summary>The smallest price change</summary>
		public const decimal PriceEpsilon = 1e-8m;

		/// <summary>The dawn of time (from a crypto point of view)</summary>
		public static readonly DateTimeOffset CryptoCurrencyEpoch = new DateTimeOffset(2009, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);

		/// <summary>Get the main thread dispatcher</summary>
		private static readonly Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;

		/// <summary>Assert that the current thread is the main thread</summary>
		public static bool AssertMainThread()
		{
			if (Thread.CurrentThread.ManagedThreadId == Dispatcher.Thread.ManagedThreadId) return true;
			throw new Exception("Cross-thread call detected");
		}

		/// <summary>Assert that it is safe to read the market data</summary>
		public static bool AssertMarketDataRead()
		{
			return AssertMainThread();
		}

		/// <summary>Assert that it is safe to write to the market data</summary>
		public static bool AssertMarketDataWrite()
		{
			return AssertMainThread();
		}

		/// <summary>Execute a delegate on the main thread</summary>
		public static void RunOnMainThread(Action action)
		{
			Dispatcher.BeginInvoke(action);
		}

		/// <summary>Resolve a relative path to a user directory path</summary>
		public static string ResolveUserPath(params string[] rel_path)
		{
			return Util.ResolveUserDocumentsPath(new[]{ "Rylogic", "CoinFlip" }.Concat(rel_path));
		}

		/// <summary>Generate a filepath for the given pair name</summary>
		public static string CandleDBFilePath(string pair_name)
		{
			var dbpath = ResolveUserPath($"PriceData\\{Path_.SanitiseFileName(pair_name)}.db");
			Path_.CreateDirs(Path_.Directory(dbpath));
			return dbpath;
		}

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
		public static Unit<decimal> Price(this ETradeType tt, Unit<decimal> price_q2b)
		{
			return
				tt == ETradeType.B2Q ? price_q2b :
				tt == ETradeType.Q2B ? Math_.Div(1m._(), price_q2b, 0m / 1m._(price_q2b)) :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return 'price' in (Quote/Base) for this trade direction. Assumes price is in (CoinOut/CoinIn)</summary>
		public static Unit<decimal> PriceQ2B(this ETradeType tt, Unit<decimal> price)
		{
			return
				tt == ETradeType.B2Q ? price :
				tt == ETradeType.Q2B ? (1m / price) :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the 'base' amount for a trade in this trade direction</summary>
		public static Unit<decimal> AmountBase(this ETradeType tt, Unit<decimal> price_q2b, Unit<decimal>? amount_in = null, Unit<decimal>? amount_out = null)
		{
			return
				tt == ETradeType.B2Q ? (amount_in != null ? amount_in.Value : amount_out != null ? amount_out.Value / price_q2b : throw new Exception("One of 'amount_in' or 'amount_out' must be given")) :
				tt == ETradeType.Q2B ? (amount_in != null ? amount_in.Value / price_q2b : amount_out != null ? amount_out.Value : throw new Exception("One of 'amount_in' or 'amount_out' must be given")) :
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

		/// <summary>Return the commission in 'out' amount for a trade in this trade direction</summary>
		public static Unit<decimal> Commission(this ETradeType tt, Unit<decimal> commission_quote, Unit<decimal> price_q2b)
		{
			return
				tt == ETradeType.B2Q ? (commission_quote) :
				tt == ETradeType.Q2B ? (commission_quote / price_q2b) :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Convert a trade type string to the enumeration value</summary>
		public static ETradeType TradeType(string trade_type)
		{
			switch (trade_type.ToLowerInvariant())
			{
			case "q2b": case "buy": case "bid": case "short": return ETradeType.Q2B;
			case "b2q": case "sell": case "ask": case "long": return ETradeType.B2Q;
			}
			throw new Exception("Unknown trade type string");
		}

		/// <summary>Convert a Binance trade type to ETradeType</summary>
		public static ETradeType TradeType(this global::Binance.API.EOrderSide order_side)
		{
			switch (order_side)
			{
			default: throw new Exception("Unknown trade type string");
			case global::Binance.API.EOrderSide.BUY: return ETradeType.Q2B;
			case global::Binance.API.EOrderSide.SELL: return ETradeType.B2Q;
			}
		}
		public static global::Binance.API.EOrderSide ToBinanceTT(this ETradeType trade_type)
		{
			switch (trade_type)
			{
			default: throw new Exception("Unknown trade type");
			case ETradeType.Q2B: return global::Binance.API.EOrderSide.BUY;
			case ETradeType.B2Q: return global::Binance.API.EOrderSide.SELL;
			}
		}

		/// <summary>Convert a Poloniex trade type to ETradeType</summary>
		public static ETradeType TradeType(this global::Poloniex.API.EOrderSide order_type)
		{
			switch (order_type)
			{
			default: throw new Exception("Unknown trade type string");
			case global::Poloniex.API.EOrderSide.Buy: return ETradeType.Q2B;
			case global::Poloniex.API.EOrderSide.Sell: return ETradeType.B2Q;
			}
		}
		public static global::Poloniex.API.EOrderSide ToPoloniexTT(this ETradeType trade_type)
		{
			switch (trade_type)
			{
			default: throw new Exception("Unknown trade type");
			case ETradeType.Q2B: return global::Poloniex.API.EOrderSide.Buy;
			case ETradeType.B2Q: return global::Poloniex.API.EOrderSide.Sell;
			}
		}

		/// <summary>Convert a Bittrex trade type to ETradeType</summary>
		public static ETradeType TradeType(global::Bittrex.API.EOrderSide order_type)
		{
			switch (order_type)
			{
			default: throw new Exception("Unknown trade type string");
			case global::Bittrex.API.EOrderSide.Buy: return ETradeType.Q2B;
			case global::Bittrex.API.EOrderSide.Sell: return ETradeType.B2Q;
			}
		}
		public static global::Bittrex.API.EOrderSide ToBittrexTT(this ETradeType trade_type)
		{
			switch (trade_type)
			{
			default: throw new Exception("Unknown trade type");
			case ETradeType.Q2B: return global::Bittrex.API.EOrderSide.Buy;
			case ETradeType.B2Q: return global::Bittrex.API.EOrderSide.Sell;
			}
		}

		///// <summary>Convert a Cryptopia trade type to ETradeType</summary>
		//public static ETradeType TradeType(global::Cryptopia.API.EOrderType order_type)
		//{
		//	switch (order_type)
		//	{
		//	default: throw new Exception("Unknown trade type string");
		//	case global::Cryptopia.API.EOrderType.Buy: return ETradeType.Q2B;
		//	case global::Cryptopia.API.EOrderType.Sell: return ETradeType.B2Q;
		//	}
		//}

		///// <summary>Convert a Bitfinex trade type to ETradeType</summary>
		//public static ETradeType TradeType(global::Bitfinex.API.EOrderType order_type)
		//{
		//	switch (order_type)
		//	{
		//	default: throw new Exception("Unknown trade type string");
		//	case global::Bitfinex.API.EOrderType.Buy: return ETradeType.Q2B;
		//	case global::Bitfinex.API.EOrderType.Sell: return ETradeType.B2Q;
		//	}
		//}

		///// <summary>Convert this trade type to the Cryptopia definition of a trade type</summary>
		//public static global::Cryptopia.API.EOrderType ToCryptopiaTT(this ETradeType trade_type)
		//{
		//	switch (trade_type)
		//	{
		//	default: throw new Exception("Unknown trade type");
		//	case ETradeType.Q2B: return global::Cryptopia.API.EOrderType.Buy;
		//	case ETradeType.B2Q: return global::Cryptopia.API.EOrderType.Sell;
		//	}
		//}


		/// <summary>
		/// Convert a time value into units of 'time_frame'.
		/// e.g if 'time_in_ticks' is 4.3 hours, and 'time_frame' is Hour1, the 4.3 is returned</summary>
		public static double TicksToTimeFrame(long time_in_ticks, ETimeFrame time_frame)
		{
			switch (time_frame)
			{
			default: throw new Exception("Unknown time frame");
			case ETimeFrame.Tick1: return new TimeSpan(time_in_ticks).TotalSeconds;
			case ETimeFrame.Min1: return new TimeSpan(time_in_ticks).TotalMinutes;
			case ETimeFrame.Min2: return new TimeSpan(time_in_ticks).TotalMinutes / 2.0;
			case ETimeFrame.Min3: return new TimeSpan(time_in_ticks).TotalMinutes / 3.0;
			case ETimeFrame.Min4: return new TimeSpan(time_in_ticks).TotalMinutes / 4.0;
			case ETimeFrame.Min5: return new TimeSpan(time_in_ticks).TotalMinutes / 5.0;
			case ETimeFrame.Min6: return new TimeSpan(time_in_ticks).TotalMinutes / 6.0;
			case ETimeFrame.Min7: return new TimeSpan(time_in_ticks).TotalMinutes / 7.0;
			case ETimeFrame.Min8: return new TimeSpan(time_in_ticks).TotalMinutes / 8.0;
			case ETimeFrame.Min9: return new TimeSpan(time_in_ticks).TotalMinutes / 9.0;
			case ETimeFrame.Min10: return new TimeSpan(time_in_ticks).TotalMinutes / 10.0;
			case ETimeFrame.Min15: return new TimeSpan(time_in_ticks).TotalMinutes / 15.0;
			case ETimeFrame.Min20: return new TimeSpan(time_in_ticks).TotalMinutes / 20.0;
			case ETimeFrame.Min30: return new TimeSpan(time_in_ticks).TotalMinutes / 30.0;
			case ETimeFrame.Min45: return new TimeSpan(time_in_ticks).TotalMinutes / 45.0;
			case ETimeFrame.Hour1: return new TimeSpan(time_in_ticks).TotalHours;
			case ETimeFrame.Hour2: return new TimeSpan(time_in_ticks).TotalHours / 2.0;
			case ETimeFrame.Hour3: return new TimeSpan(time_in_ticks).TotalHours / 3.0;
			case ETimeFrame.Hour4: return new TimeSpan(time_in_ticks).TotalHours / 4.0;
			case ETimeFrame.Hour6: return new TimeSpan(time_in_ticks).TotalHours / 6.0;
			case ETimeFrame.Hour8: return new TimeSpan(time_in_ticks).TotalHours / 8.0;
			case ETimeFrame.Hour12: return new TimeSpan(time_in_ticks).TotalHours / 12.0;
			case ETimeFrame.Day1: return new TimeSpan(time_in_ticks).TotalDays;
			case ETimeFrame.Day2: return new TimeSpan(time_in_ticks).TotalDays / 2.0;
			case ETimeFrame.Day3: return new TimeSpan(time_in_ticks).TotalDays / 3.0;
			case ETimeFrame.Week1: return new TimeSpan(time_in_ticks).TotalDays / 7.0;
			case ETimeFrame.Week2: return new TimeSpan(time_in_ticks).TotalDays / 14.0;
			case ETimeFrame.Month1: return new TimeSpan(time_in_ticks).TotalDays / 30.0;
			}
		}
		public static double TimeSpanToTimeFrame(TimeSpan ts, ETimeFrame time_frame)
		{
			return TicksToTimeFrame(ts.Ticks, time_frame);
		}

		/// <summary>
		/// Convert 'units' in 'time_frame' units to ticks.
		/// e.g. if 'units' is 4.3 hours, then TimeSpan.FromHours(4.3).Ticks is returned</summary>
		public static long TimeFrameToTicks(double units, ETimeFrame time_frame)
		{
			// Use 1 second for all tick time-frames
			switch (time_frame)
			{
			default: throw new Exception("Unknown time frame");
			case ETimeFrame.Tick1: return TimeSpan.FromSeconds(units).Ticks;
			case ETimeFrame.Min1: return TimeSpan.FromMinutes(units).Ticks;
			case ETimeFrame.Min2: return TimeSpan.FromMinutes(units * 2.0).Ticks;
			case ETimeFrame.Min3: return TimeSpan.FromMinutes(units * 3.0).Ticks;
			case ETimeFrame.Min4: return TimeSpan.FromMinutes(units * 4.0).Ticks;
			case ETimeFrame.Min5: return TimeSpan.FromMinutes(units * 5.0).Ticks;
			case ETimeFrame.Min6: return TimeSpan.FromMinutes(units * 6.0).Ticks;
			case ETimeFrame.Min7: return TimeSpan.FromMinutes(units * 7.0).Ticks;
			case ETimeFrame.Min8: return TimeSpan.FromMinutes(units * 8.0).Ticks;
			case ETimeFrame.Min9: return TimeSpan.FromMinutes(units * 9.0).Ticks;
			case ETimeFrame.Min10: return TimeSpan.FromMinutes(units * 10.0).Ticks;
			case ETimeFrame.Min15: return TimeSpan.FromMinutes(units * 15.0).Ticks;
			case ETimeFrame.Min20: return TimeSpan.FromMinutes(units * 20.0).Ticks;
			case ETimeFrame.Min30: return TimeSpan.FromMinutes(units * 30.0).Ticks;
			case ETimeFrame.Min45: return TimeSpan.FromMinutes(units * 45.0).Ticks;
			case ETimeFrame.Hour1: return TimeSpan.FromHours(units).Ticks;
			case ETimeFrame.Hour2: return TimeSpan.FromHours(units * 2.0).Ticks;
			case ETimeFrame.Hour3: return TimeSpan.FromHours(units * 3.0).Ticks;
			case ETimeFrame.Hour4: return TimeSpan.FromHours(units * 4.0).Ticks;
			case ETimeFrame.Hour6: return TimeSpan.FromHours(units * 6.0).Ticks;
			case ETimeFrame.Hour8: return TimeSpan.FromHours(units * 8.0).Ticks;
			case ETimeFrame.Hour12: return TimeSpan.FromHours(units * 12.0).Ticks;
			case ETimeFrame.Day1: return TimeSpan.FromDays(units).Ticks;
			case ETimeFrame.Day2: return TimeSpan.FromDays(units * 2.0).Ticks;
			case ETimeFrame.Day3: return TimeSpan.FromDays(units * 3.0).Ticks;
			case ETimeFrame.Week1: return TimeSpan.FromDays(units * 7.0).Ticks;
			case ETimeFrame.Week2: return TimeSpan.FromDays(units * 14.0).Ticks;
			case ETimeFrame.Month1: return TimeSpan.FromDays(units * 30.0).Ticks;
			}
		}
		public static TimeSpan TimeFrameToTimeSpan(double units, ETimeFrame time_frame)
		{
			return TimeSpan.FromTicks(TimeFrameToTicks(units, time_frame));
		}

		/// <summary>Return the time frame value from 'available' that is nearest to 'wanted'</summary>
		public static ETimeFrame Nearest(ETimeFrame wanted, IEnumerable<ETimeFrame> available)
		{
			var nearest = int.MaxValue;
			var closest = ETimeFrame.None;
			foreach (var tf in available)
			{
				var dist = Math.Abs((int)tf - (int)wanted);
				if (dist > nearest) continue;
				nearest = dist;
				closest = tf;
			}
			return closest;
		}

		/// <summary>Return 'time' rounded down to units of 'time_frame'</summary>
		public static DateTimeOffset RoundDownTo(this DateTimeOffset time, ETimeFrame time_frame)
		{
			var tf = TimeFrameToTicks(1.0, time_frame);
			var ticks = (time.Ticks / tf) * tf;
			return new DateTimeOffset(ticks, time.Offset);
		}

		/// <summary>Return 'time' rounded up to units of 'time_frame'</summary>
		public static DateTimeOffset RoundUpTo(this DateTimeOffset time, ETimeFrame time_frame)
		{
			var tf = TimeFrameToTicks(1.0, time_frame);
			var ticks = ((time.Ticks + tf - 1) / tf) * tf;
			return new DateTimeOffset(ticks, time.Offset);
		}

		/// <summary>Return a timestamp string suitable for a chart X tick value</summary>
		public static string ShortTimeString(DateTimeOffset dt_curr, DateTimeOffset dt_prev, bool first)
		{
			// First tick on the x axis
			if (first)
				return dt_curr.ToString("HH:mm'\r\n'dd-MMM-yy");

			// Show more of the time stamp depending on how it differs from the previous time stamp
			if (dt_curr.Year != dt_prev.Year)
				return dt_curr.ToString("HH:mm'\r\n'dd-MMM-yy");
			if (dt_curr.Month != dt_prev.Month)
				return dt_curr.ToString("HH:mm'\r\n'dd-MMM");
			if (dt_curr.Day != dt_prev.Day)
				return dt_curr.ToString("HH:mm'\r\n'ddd dd");

			return dt_curr.ToString("HH:mm");
		}

		/// <summary>Return the first active chart, or if none are active, make the first one active</summary>
		public static IChartView FindActiveChart(this IList<IChartView> charts)
		{
			if (charts.Count == 0)
				return null;

			// Find a chart that is visible
			var active = charts.FirstOrDefault(x => x.IsActiveContentInPane);
			if (active != null)
				return active;

			// Make the first chart visible
			charts[0].EnsureActiveContent();
			return charts[0];
		}
	}
}
