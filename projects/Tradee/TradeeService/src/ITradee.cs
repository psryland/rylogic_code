using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Tradee
{
	#region Enumerations
	[Serializable] public enum ETradeType
	{
		None,
		Long,
		Short,
	}
	[Serializable] public enum ETimeFrame
	{
		None,
		[Description("T1 ")]     Tick1  ,
		[Description("M1 ")]     Min1   ,
		[Description("M2 ")]     Min2   ,
		[Description("M3 ")]     Min3   ,
		[Description("M4 ")]     Min4   ,
		[Description("M5 ")]     Min5   ,
		[Description("M6 ")]     Min6   ,
		[Description("M7 ")]     Min7   ,
		[Description("M8 ")]     Min8   ,
		[Description("M9 ")]     Min9   ,
		[Description("M10")]     Min10  ,
		[Description("M15")]     Min15  ,
		[Description("M20")]     Min20  ,
		[Description("M30")]     Min30  ,
		[Description("M45")]     Min45  ,
		[Description("H1 ")]     Hour1  ,
		[Description("H2 ")]     Hour2  ,
		[Description("H3 ")]     Hour3  ,
		[Description("H4 ")]     Hour4  ,
		[Description("H6 ")]     Hour6  ,
		[Description("H8 ")]     Hour8  ,
		[Description("H12")]     Hour12 ,
		[Description("D1 ")]     Day1   ,
		[Description("D2 ")]     Day2   ,
		[Description("D3 ")]     Day3   ,
		[Description("Weekly")]  Weekly ,
		[Description("Monthly")] Monthly,
	}
	[Serializable] public enum ECandleType
	{
		Other,
		Hammer,
		InvHammer,
		SpinningTop,
		Engulging,
	}
	[Serializable] public enum ETradePairs
	{
		None,

		[Description("USD - Canadian Dollar"   )] USDCAD,
		[Description("USD - Swiss Franc"       )] USDCHF,
		[Description("USD - Chinese Offshore"  )] USDCNH,
		[Description("USD - Czech Koruna"      )] USDCZK,
		[Description("USD - Hong Kong Dollar"  )] USDHKD,
		[Description("USD - Japanese Yen"      )] USDJPY,
		[Description("USD - Mexican Peso"      )] USDMXN,
		[Description("USD - Norwegian Krone"   )] USDNOK,
		[Description("USD - Polish Zloty"      )] USDPLN,
		[Description("USD - Russian Ruble"     )] USDRUB,
		[Description("USD - Swedish Krona"     )] USDSEK,
		[Description("USD - Singapore Dollar"  )] USDSGD,
		[Description("USD - Turkish Lira"      )] USDTRY,
		[Description("USD - Thai Bhat"         )] USDTHB,
		[Description("USD - South African Rand")] USDZAR,

		[Description("Euro - Australian Dollar" )] EURAUD,
		[Description("Euro - Canadian Dollar"   )] EURCAD,
		[Description("Euro - Swiss Franc"       )] EURCHF,
		[Description("Euro - Czech Koruna"      )] EURCZK,
		[Description("Euro - British Pound"     )] EURGBP,
		[Description("Euro - Japanese Yen"      )] EURJPY,
		[Description("Euro - Norwegian Krone"   )] EURNOK,
		[Description("Euro - New Zealand Dollar")] EURNZD,
		[Description("Euro - Polish Zloty"      )] EURPLN,
		[Description("Euro - Swedish Krona"     )] EURSEK,
		[Description("Euro - Singapore Dollar"  )] EURSGD,
		[Description("Euro - Turkish Lira"      )] EURTRY,
		[Description("Euro - USD"               )] EURUSD,
		[Description("Euro - South African Rand")] EURZAR,

		[Description("British Pound - Australian Dollar" )] GBPAUD,
		[Description("British Pound - Canadian Dollar"   )] GBPCAD,
		[Description("British Pound - Swiss Franc"       )] GBPCHF,
		[Description("British Pound - Japanese Yen"      )] GBPJPY,
		[Description("British Pound - Norwegian Krone"   )] GBPNOK,
		[Description("British Pound - New Zealand Dollar")] GBPNZD,
		[Description("British Pound - Swedish Krona"     )] GBPSEK,
		[Description("British Pound - Singapore Dollar"  )] GBPSGD,
		[Description("British Pound - Turkish Lira"      )] GBPTRY,
		[Description("British Pound - USD"               )] GBPUSD,

		[Description("Australian Dollar - Australian Dollar" )] AUDCAD,
		[Description("Australian Dollar - Swiss Franc"       )] AUDCHF,
		[Description("Australian Dollar - Japanese Yen"      )] AUDJPY,
		[Description("Australian Dollar - New Zealand Dollar")] AUDNZD,
		[Description("Australian Dollar - Singapore Dollar"  )] AUDSGD,
		[Description("Australian Dollar - USD"               )] AUDUSD,

		[Description("New Zealand Dollar - Canadian Dollar")] NZDCAD,
		[Description("New Zealand Dollar - Swiss Franc"    )] NZDCHF,
		[Description("New Zealand Dollar - Japanese Yen"   )] NZDJPY,
		[Description("New Zealand Dollar - USD"            )] NZDUSD,

		[Description("Swiss Franc - Japanese Yen"    )] CHFJPY,
		[Description("Swiss Franc - Singapore Dollar")] CHFSGD,

		[Description("Canadian Dollar - Swiss Franc" )] CADCHF,
		[Description("Canadian Dollar - Japanese Yen")] CADJPY,

		[Description("Norwegian Krone - Japanese Yen" )] NOKJPY,
		[Description("Norwegian Krone - Swedish Krona")] NOKSEK,

		[Description("Swedish Krona - Japanese Yen")] SEKJPY,

		[Description("Singapore Dollar - Japanese Yen")] SGDJPY,

		[Description("Gold - USD"       )] XAUUSD,
		[Description("Silver - USD"     )] XAGUSD,
		[Description("Palladium - USD"  )] XPDUSD,
		[Description("Platinum - USD"   )] XPTUSD,
		[Description("Natural Gas - USD")] XNGUSD,
	};
	#endregion

	/// <summary>The core data of a single candle</summary>
	[Serializable] public class Candle
	{
		public static readonly Candle Default = new Candle(0, 0, 0, 0, 0, 0);
		public Candle()
		{}
		public Candle(long timestamp, double open, double high, double low, double close, double volume)
		{
			Timestamp = timestamp;
			Open      = open;
			High      = high;
			Low       = low;
			Close     = close;
			Volume    = volume;
		}

		/// <summary>The timestamp (in ticks) of when this candle opened</summary>
		public long Timestamp { get; set; }

		/// <summary>The open price</summary>
		public double Open { get; set; }

		/// <summary>The price high</summary>
		public double High { get; set; }

		/// <summary>The price low</summary>
		public double Low  { get; set; }

		/// <summary>The price close</summary>
		public double Close { get; set; }

		/// <summary>The number of ticks in this candle</summary>
		public double Volume { get; set; }

		/// <summary>Classify the candle type</summary>
		public ECandleType Type
		{
			get
			{
				return ECandleType.Other;
			}
		}

		/// <summary>True if this is a bullish candle</summary>
		public bool Bullish
		{
			get { return Open > Close; }
		}

		/// <summary>True if this is a bullish candle</summary>
		public bool Bearish
		{
			get { return Open < Close; }
		}

		/// <summary>Replace the data in this candle with 'rhs' (Must have a matching timestamp though)</summary>
		public void Update(Candle rhs)
		{
			Debug.Assert(Timestamp == rhs.Timestamp);
			Open   = rhs.Open  ;
			High   = rhs.High  ;
			Low    = rhs.Low   ;
			Close  = rhs.Close ;
			Volume = rhs.Volume;
		}

		/// <summary>The time stamp of this candle in local time zone time</summary>
		public DateTime LocalTimestamp
		{
			get { return TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(Timestamp, DateTimeKind.Utc)); }
		}

		/// <summary>Friendly print</summary>
		public override string ToString()
		{
			return string.Format("ts:{0} - ohlc:({1:G4} {2:G4} {3:G4} {4:G4}) - vol:{5}", new DateTime(Timestamp, DateTimeKind.Utc).ToString(), Open, High, Low, Close, Volume);
		}

		#region Equals
		public bool Equals(Candle rhs)
		{
			return
				Timestamp == rhs.Timestamp &&
				Open      == rhs.Open      &&
				High      == rhs.High      &&
				Low       == rhs.Low       &&
				Close     == rhs.Close     &&
				Volume    == rhs.Volume;
		}
		public override bool Equals(object obj)
		{
			return obj is Candle && Equals((Candle)obj);
		}
		public override int GetHashCode()
		{
			return new { Timestamp, Open, High, Low, Close, Volume }.GetHashCode();
		}
		#endregion
	}

	/// <summary>A batch of candle data</summary>
	[Serializable] public class Candles
	{
		public Candles(long[] timestamp, double[] open, double[] high, double[] low, double[] close, double[] volume)
		{
			Debug.Assert(new[] { timestamp.Length, open.Length, high.Length, low.Length, close.Length, volume.Length }.All(x => x == timestamp.Length), "All arrays should be the same length");
			Timestamp = timestamp;
			Open      = open;
			High      = high;
			Low       = low;
			Close     = close;
			Volume    = volume;
		}

		/// <summary>The timestamp values of the given data (in ticks)</summary>
		public long[] Timestamp { get; set; }

		/// <summary>The open price</summary>
		public double[] Open  { get; set; }

		/// <summary>The price high</summary>
		public double[] High  { get; set; }

		/// <summary>The price low</summary>
		public double[] Low   { get; set; }

		/// <summary>The price close</summary>
		public double[] Close { get; set; }

		/// <summary>The number of ticks in each candle</summary>
		public double[] Volume { get; set; }

		/// <summary>Enumerate each candle in the batch</summary>
		public IEnumerable<Candle> AllCandles
		{
			get
			{
				for (int i = 0; i != Timestamp.Length; ++i)
					yield return new Candle(Timestamp[i], Open[i], High[i], Low[i], Close[i], Volume[i]);
			}
		}
	}

	/// <summary>Stats for a given instrument</summary>
	[Serializable] public class PriceData
	{
		public static readonly PriceData Default = new PriceData(0, 0, 0, 0, 0, 0, 0, 0);
		public PriceData(double ask, double bid, double lot_size, double pip_size, double pip_value, double volume_min, double volume_step, double volume_max)
		{
			AskPrice   = ask;
			BidPrice   = bid;
			LotSize    = lot_size;
			PipSize    = pip_size;
			PipValue   = pip_value;
			VolumeMin  = volume_min;
			VolumeStep = volume_step;
			VolumeMax  = volume_max;
		}

		/// <summary>The price if you want to buy (lowest seller price)</summary>
		public double AskPrice { get; set; }

		/// <summary>The price if you want to sell (highest buyer price)</summary>
		public double BidPrice { get; set; }

		/// <summary>The difference between Ask and Bid</summary>
		public double Spread { get { return AskPrice - BidPrice; } }

		/// <summary>The size of 1 lot in units of base currency</summary>
		public double LotSize { get; set; }

		/// <summary>The smallest unit of change for the symbol</summary>
		public double PipSize { get; set; }

		/// <summary>The monetary value of 1 pip</summary>
		public double PipValue { get; set; }

		/// <summary>The minimum tradable amount</summary>
		public double VolumeMin { get; set; }

		/// <summary>The minimum tradable amount increment</summary>		
		public double VolumeStep { get; set; }

		/// <summary>The maximum tradable amount</summary>
		public double VolumeMax { get; set; }

		#region Equals
		public bool Equals(PriceData rhs)
		{
			return
				AskPrice   == rhs.AskPrice   &&
				BidPrice   == rhs.BidPrice   &&
				LotSize    == rhs.LotSize    &&
				PipSize    == rhs.PipSize    &&
				PipValue   == rhs.PipValue   &&
				VolumeMin  == rhs.VolumeMin  &&
				VolumeStep == rhs.VolumeStep &&
				VolumeMax  == rhs.VolumeMax;
		}
		public override bool Equals(object obj)
		{
			return obj is PriceData && Equals((PriceData)obj);
		}
		public override int GetHashCode()
		{
			return new
			{
				AskPrice  ,
				BidPrice  ,
				LotSize   ,
				PipSize   ,
				PipValue  ,
				VolumeMin ,
				VolumeStep,
				VolumeMax ,
			}.GetHashCode();
		}
		#endregion
	}

	/// <summary>Messages from the trade data source to Tradee</summary>
	public static class InMsg
	{
		[Serializable] public class TestMsg
		{
			public string Text { get; set; }
		}
		
		/// <summary>Message for passing a single candle or a batch of candles</summary>
		[Serializable] public class CandleData
		{
			public CandleData(string symbol, ETimeFrame time_frame, Candle candle)
			{
				Symbol    = symbol;
				TimeFrame = time_frame;
				Candle    = candle;
			}
			public CandleData(string symbol, ETimeFrame time_frame, Candles candles)
			{
				Symbol    = symbol;
				TimeFrame = time_frame;
				Candles   = candles;
			}

			/// <summary>The symbol that this candle is for</summary>
			public string Symbol { get; set; }

			/// <summary>The time-frame that the candle represents</summary>
			public ETimeFrame TimeFrame { get; set; }

			/// <summary>A single candle</summary>
			public Candle Candle { get; set; }

			/// <summary>A batch of candles</summary>
			public Candles Candles { get; set; }
		}

		/// <summary>Message for passing the current status of a symbol</summary>
		[Serializable] public class SymbolData
		{
			public SymbolData(string sym, PriceData data)
			{
				Symbol = sym;
				Data   = data;
			}

			/// <summary>The symbol that this candle is for</summary>
			public string Symbol { get; set; }

			/// <summary>Price and Order data about the symbol</summary>
			public PriceData Data { get; set; }
		}

		/// <summary>Message for passing account status and risk position</summary>
		[Serializable] public class AccountStatus
		{
			/// <summary>The broker name of the current account</summary>
			public string BrokerName { get; set; }

			/// <summary>The number of the current account, e.g. 123456.</summary>
			public int Number { get; set; }

			/// <summary>The base currency of the account</summary>
			public string Currency { get; set; }

			/// <summary>The balance of the current account</summary>
			public double Balance { get; set; }

			/// <summary>The equity of the current account (balance plus unrealized profit and loss).</summary>
			public double Equity { get; set; }

			/// <summary>The free margin of the current account.</summary>
			public double FreeMargin { get; set; }

			/// <summary>True if the Account is Live, False if it is a Demo</summary>
			public bool IsLive { get; set; }

			/// <summary>The account leverage</summary>
			public int Leverage { get; set; }

			/// <summary>Represents the margin of the current account.</summary>
			public double Margin { get; set; }

			/// <summary>
			/// Represents the margin level of the current account.
			/// Margin level (in %) is calculated using this formula: Equity / Margin * 100.</summary>
			public double? MarginLevel { get; set; }

			/// <summary>Unrealised gross profit</summary>
			public double UnrealizedGrossProfit { get; set; }

			/// <summary>Unrealised net profit</summary>
			public double UnrealizedNetProfit { get; set; }

			/// <summary>The risk due to open positions</summary>
			public double PositionRisk { get; set; }

			/// <summary>The risk due to pending orders</summary>
			public double OrderRisk { get; set; }
		}
	}

	/// <summary>Messages from the tradee to the trade data source</summary>
	public static class OutMsg
	{
		[Serializable] public class TestMsg
		{
			public string Text { get; set; }
		}

		/// <summary>Request the market data for the given instrument at a given time frame</summary>
		[Serializable] public class ChangeInstrument
		{
			public ChangeInstrument(string sym, ETimeFrame tf)
			{
				Symbol = sym;
				TimeFrame = tf;
			}

			/// <summary>The symbol code for the requested instrument</summary>
			public string Symbol { get; private set; }

			/// <summary>The time frame of data to send</summary>
			public ETimeFrame TimeFrame { get; private set; }
		}
	}

	/// <summary>Global functions</summary>
	public static class Misc
	{
		/// <summary>
		/// Convert a time value into units of a time frame.
		/// e.g if 'time_in_ticks' is 4.3 hours, and 'time_frame' is Hour1, the 4.3 is returned</summary>
		public static double TicksToTimeFrame(long time_in_ticks, ETimeFrame time_frame)
		{
			// Use 1 second for all tick time-frames
			switch (time_frame)
			{
			default: throw new Exception("Unknown time frame");
			case ETimeFrame.Tick1  : return new TimeSpan(time_in_ticks).TotalSeconds;
			case ETimeFrame.Min1   : return new TimeSpan(time_in_ticks).TotalMinutes;
			case ETimeFrame.Min2   : return new TimeSpan(time_in_ticks).TotalMinutes / 2.0;
			case ETimeFrame.Min3   : return new TimeSpan(time_in_ticks).TotalMinutes / 3.0;
			case ETimeFrame.Min4   : return new TimeSpan(time_in_ticks).TotalMinutes / 4.0;
			case ETimeFrame.Min5   : return new TimeSpan(time_in_ticks).TotalMinutes / 5.0;
			case ETimeFrame.Min6   : return new TimeSpan(time_in_ticks).TotalMinutes / 6.0;
			case ETimeFrame.Min7   : return new TimeSpan(time_in_ticks).TotalMinutes / 7.0;
			case ETimeFrame.Min8   : return new TimeSpan(time_in_ticks).TotalMinutes / 8.0;
			case ETimeFrame.Min9   : return new TimeSpan(time_in_ticks).TotalMinutes / 9.0;
			case ETimeFrame.Min10  : return new TimeSpan(time_in_ticks).TotalMinutes / 10.0;
			case ETimeFrame.Min15  : return new TimeSpan(time_in_ticks).TotalMinutes / 15.0;
			case ETimeFrame.Min20  : return new TimeSpan(time_in_ticks).TotalMinutes / 20.0;
			case ETimeFrame.Min30  : return new TimeSpan(time_in_ticks).TotalMinutes / 30.0;
			case ETimeFrame.Min45  : return new TimeSpan(time_in_ticks).TotalMinutes / 45.0;
			case ETimeFrame.Hour1  : return new TimeSpan(time_in_ticks).TotalHours;
			case ETimeFrame.Hour2  : return new TimeSpan(time_in_ticks).TotalHours / 2.0;
			case ETimeFrame.Hour3  : return new TimeSpan(time_in_ticks).TotalHours / 3.0;
			case ETimeFrame.Hour4  : return new TimeSpan(time_in_ticks).TotalHours / 4.0;
			case ETimeFrame.Hour6  : return new TimeSpan(time_in_ticks).TotalHours / 6.0;
			case ETimeFrame.Hour8  : return new TimeSpan(time_in_ticks).TotalHours / 8.0;
			case ETimeFrame.Hour12 : return new TimeSpan(time_in_ticks).TotalHours / 12.0;
			case ETimeFrame.Day1   : return new TimeSpan(time_in_ticks).TotalDays;
			case ETimeFrame.Day2   : return new TimeSpan(time_in_ticks).TotalDays / 2.0;
			case ETimeFrame.Day3   : return new TimeSpan(time_in_ticks).TotalDays / 3.0;
			case ETimeFrame.Weekly : return new TimeSpan(time_in_ticks).TotalDays / 7.0;
			case ETimeFrame.Monthly: return new TimeSpan(time_in_ticks).TotalDays / 30.0;
			}
		}

		/// <summary>
		/// Convert time-frame units to ticks.
		/// e.g. if 'units' is 4.3 hours, then TimeZone.FromHours(4.3).Ticks is returned</summary>
		public static long TimeFrameToTicks(double units, ETimeFrame time_frame)
		{
			// Use 1 second for all tick time-frames
			switch (time_frame)
			{
			default: throw new Exception("Unknown time frame");
			case ETimeFrame.Tick1  : return TimeSpan.FromSeconds(units).Ticks;
			case ETimeFrame.Min1   : return TimeSpan.FromMinutes(units).Ticks;
			case ETimeFrame.Min2   : return TimeSpan.FromMinutes(units * 2.0).Ticks;
			case ETimeFrame.Min3   : return TimeSpan.FromMinutes(units * 3.0).Ticks;
			case ETimeFrame.Min4   : return TimeSpan.FromMinutes(units * 4.0).Ticks;
			case ETimeFrame.Min5   : return TimeSpan.FromMinutes(units * 5.0).Ticks;
			case ETimeFrame.Min6   : return TimeSpan.FromMinutes(units * 6.0).Ticks;
			case ETimeFrame.Min7   : return TimeSpan.FromMinutes(units * 7.0).Ticks;
			case ETimeFrame.Min8   : return TimeSpan.FromMinutes(units * 8.0).Ticks;
			case ETimeFrame.Min9   : return TimeSpan.FromMinutes(units * 9.0).Ticks;
			case ETimeFrame.Min10  : return TimeSpan.FromMinutes(units * 10.0).Ticks;
			case ETimeFrame.Min15  : return TimeSpan.FromMinutes(units * 15.0).Ticks;
			case ETimeFrame.Min20  : return TimeSpan.FromMinutes(units * 20.0).Ticks;
			case ETimeFrame.Min30  : return TimeSpan.FromMinutes(units * 30.0).Ticks;
			case ETimeFrame.Min45  : return TimeSpan.FromMinutes(units * 45.0).Ticks;
			case ETimeFrame.Hour1  : return TimeSpan.FromHours(units).Ticks;
			case ETimeFrame.Hour2  : return TimeSpan.FromHours(units * 2.0).Ticks;
			case ETimeFrame.Hour3  : return TimeSpan.FromHours(units * 3.0).Ticks;
			case ETimeFrame.Hour4  : return TimeSpan.FromHours(units * 4.0).Ticks;
			case ETimeFrame.Hour6  : return TimeSpan.FromHours(units * 6.0).Ticks;
			case ETimeFrame.Hour8  : return TimeSpan.FromHours(units * 8.0).Ticks;
			case ETimeFrame.Hour12 : return TimeSpan.FromHours(units * 12.0).Ticks;
			case ETimeFrame.Day1   : return TimeSpan.FromDays(units).Ticks;
			case ETimeFrame.Day2   : return TimeSpan.FromDays(units * 2.0).Ticks;
			case ETimeFrame.Day3   : return TimeSpan.FromDays(units * 3.0).Ticks;
			case ETimeFrame.Weekly : return TimeSpan.FromDays(units * 7.0).Ticks;
			case ETimeFrame.Monthly: return TimeSpan.FromDays(units * 30.0).Ticks;
			}
		}
	}
}
