using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public static class Misc
	{
		/// <summary>The smallest amount change</summary>
		public const double AmountEpsilon = 1e-8;

		/// <summary>The smallest price change</summary>
		public const double PriceEpsilon = 1e-8;

		/// <summary>Regex expression for finding bot plugin dlls</summary>
		public const string BotFilterRegex = @"Bot\.(?<name>\w+)\.dll";

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

		/// <summary>Assert that the current thread is the main thread</summary>
		public static bool AssertBackgroundThread()
		{
			if (Thread.CurrentThread.ManagedThreadId != Dispatcher.Thread.ManagedThreadId) return true;
			throw new Exception("Main-thread call detected");
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
		public static DispatcherOperation RunOnMainThread(Action action)
		{
			return Dispatcher.BeginInvoke(Worker);
			void Worker()
			{
				Debug.Assert(AssertMainThread());
				try { action(); }
				catch (Exception ex)
				{
					Model.Log.Write(ELogLevel.Error, ex, "Unhandled exception in main-thread function");
					throw;
				}
			}
		}

		/// <summary>Execute a delegate on a background thread</summary>
		public static Task RunOnWorkerThread(Action action, CancellationToken? cancel = null)
		{
			return Task.Factory.StartNew(Worker, cancel ?? CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
			void Worker()
			{
				Debug.Assert(AssertBackgroundThread());
				try { action(); }
				catch (Exception ex)
				{
					Model.Log.Write(ELogLevel.Error, ex, "Unhandled exception in worker-thread function");
					throw;
				}
			}
		}

		/// <summary>Resolve a relative path in the temporary directory</summary>
		public static string ResolveTempPath(params string[] rel_path)
		{
			return Util.ResolveTempPath(new[] { "Rylogic", "CoinFlip" }.Concat(rel_path));
		}

		/// <summary>Resolve a relative path to a user directory path</summary>
		public static string ResolveUserPath(params string[] rel_path)
		{
			return Util.ResolveUserDocumentsPath(new[]{ "Rylogic", "CoinFlip" }.Concat(rel_path));
		}

		/// <summary>Resolve a full path relative to the bots directory</summary>
		public static string ResolveBotPath(params string[] rel_path)
		{
			return Util.ResolveAppPath(new[] { "bots" }.Concat(rel_path));
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

		/// <summary>Convert a Binance order type to EPlaceOrderType</summary>
		public static EOrderType OrderType(this global::Binance.API.EOrderType order_type)
		{
			switch (order_type)
			{
			default: throw new Exception("Unknown order type string");
			case global::Binance.API.EOrderType.LIMIT: return EOrderType.Limit;
			case global::Binance.API.EOrderType.MARKET: return EOrderType.Market;
			case global::Binance.API.EOrderType.STOP_LOSS: return EOrderType.Stop;
			case global::Binance.API.EOrderType.STOP_LOSS_LIMIT: return EOrderType.Stop;
			case global::Binance.API.EOrderType.TAKE_PROFIT: return EOrderType.Stop;
			case global::Binance.API.EOrderType.TAKE_PROFIT_LIMIT: return EOrderType.Stop;
			case global::Binance.API.EOrderType.LIMIT_MAKER: return EOrderType.Limit;
			}
		}
		public static global::Binance.API.EOrderType ToBinanceOT(this EOrderType order_type)
		{
			switch (order_type)
			{
			default: throw new Exception("Unknown trade type");
			case EOrderType.Market: return global::Binance.API.EOrderType.MARKET;
			case EOrderType.Limit: return global::Binance.API.EOrderType.LIMIT;
			case EOrderType.Stop: return global::Binance.API.EOrderType.TAKE_PROFIT;
			}
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
		public static ETradeType TradeType(this global::Bittrex.API.EOrderSide order_type)
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

		/// <summary>Convert a time value into units of 'time_frame'. e.g if 'time_in_ticks' is 4.3 hours, and 'time_frame' is Hour1, the 4.3 is returned</summary>
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

		/// <summary>Convert 'units' in 'time_frame' units to ticks. e.g. if 'units' is 4.3 hours, then TimeSpan.FromHours(4.3).Ticks is returned</summary>
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
	}
}
