using System;
using System.Drawing;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using pr.common;
using pr.crypt;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	/// <summary>Buy/Sell</summary>
	public enum ETradeType
	{
		/// <summary>Quote->Base, Buy, Bid, Long, High spot price</summary>
		Q2B,

		/// <summary>Base->Quote, Sell, Ask, Short. Low spot price</summary>
		B2Q,
	}

	public enum EPlaceOrderType
	{
		/// <summary>Place the order at the current price, whatever it is</summary>
		Market,

		/// <summary>Place the order when the order book for an instrument has orders available at the given price level</summary>
		Limit,

		/// <summary>Place the order when the spot price of an instrument reaches a given price level. </summary>
		Stop,
	}

	/// <summary>Bitmask of trade directions</summary>
	[Flags] public enum ETradeDirection
	{
		/// <summary></summary>
		None = 0,

		/// <summary>Quote->Base, Buy, Bid, Short</summary>
		Q2B = 1 << ETradeType.Q2B,

		/// <summary>Base->Quote, Sell, Ask, Long</summary>
		B2Q = 1 << ETradeType.B2Q,

		/// <summary>Both directions</summary>
		Both = Q2B | B2Q,
	}

	/// <summary>The connection status of the exchange</summary>
	[Flags] public enum EStatus
	{
		Offline    = 1 << 0,
		Connected  = 1 << 2,
		Stopped    = 1 << 3,
		Simulated  = 1 << 4,
		Error      = 1 << 16,
	}

	/// <summary>Trade validation</summary>
	[Flags] public enum EValidation
	{
		Valid               = 0,
		VolumeInOutOfRange  = 1 << 0,
		VolumeOutOutOfRange = 1 << 1,
		PriceOutOfRange     = 1 << 2,
		InsufficientBalance = 1 << 3,
		PriceIsInvalid      = 1 << 4,
		VolumeInIsInvalid   = 1 << 5,
		VolumeOutIsInvalid  = 1 << 6,
	}

	public static class Validation
	{
		/// <summary>Return a string description of this validation result</summary>
		public static string ToErrorDescription(this EValidation val)
		{
			var sb = new StringBuilder();
			if (val.HasFlag(EValidation.VolumeInOutOfRange))
			{
				sb.AppendLine("The volume of currency being sold is not within the valid range.");
				val ^= EValidation.VolumeInOutOfRange;
			}
			if (val.HasFlag(EValidation.VolumeOutOutOfRange))
			{
				sb.AppendLine("The volume of currency being bought is not within the valid range.");
				val ^= EValidation.VolumeOutOutOfRange;
			}
			if (val.HasFlag(EValidation.PriceOutOfRange))
			{
				sb.AppendLine("The price level to trade at is not within the valid range.");
				val ^= EValidation.PriceOutOfRange;
			}
			if (val.HasFlag(EValidation.InsufficientBalance))
			{
				sb.AppendLine("There is insufficient balance of the currency being sold.");
				val ^= EValidation.InsufficientBalance;
			}
			if (val.HasFlag(EValidation.PriceIsInvalid))
			{
				sb.AppendLine("The price level to trade at is invalid.");
				val ^= EValidation.PriceIsInvalid;
			}
			if (val.HasFlag(EValidation.VolumeInIsInvalid))
			{
				sb.AppendLine("The volume of currency being sold is invalid.");
				val ^= EValidation.VolumeInIsInvalid;
			}
			if (val.HasFlag(EValidation.VolumeOutIsInvalid))
			{
				sb.AppendLine("The volume of currency being bought is invalid.");
				val ^= EValidation.VolumeOutIsInvalid;
			}
			if (val != 0)
			{
				throw new Exception("Unknown validation flags");
			}
			return sb.ToString();
		}
	}

	/// <summary>Odds and sods</summary>
	public static class Misc
	{
		/// <summary>Regex pattern for log UIs</summary>
		public static readonly Regex LogEntryPattern = new Regex(@"^(?<Tag>.*?)\|(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*)",RegexOptions.Singleline|RegexOptions.Multiline|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		public static readonly LogUI.HLPattern[] LogHighlighting = new[]
		{
			new LogUI.HLPattern(Color_.FromArgb(0xffb5ffd2), Color.Black, EPattern.Substring, "Fishing started"),
			new LogUI.HLPattern(Color_.FromArgb(0xffb5ffd2), Color.Black, EPattern.Substring, "Fishing stopped"),
			new LogUI.HLPattern(Color_.FromArgb(0xffe7a5ff), Color.Black, EPattern.Substring, "!Profit!"),
			new LogUI.HLPattern(Color_.FromArgb(0xfffcffae), Color.Black, EPattern.Substring, "filled"),
			new LogUI.HLPattern(Color_.FromArgb(0xffff7e39), Color.Black, EPattern.Substring, "ignored"),
		};

		/// <summary>Resolve a relative path to a user directory path</summary>
		public static string ResolveUserPath(string rel_path = null)
		{
			return Util.ResolveUserDocumentsPath(Application.CompanyName, Application.ProductName, rel_path ?? string.Empty);
		}

		/// <summary>Generate a filepath for the given pair name</summary>
		public static string CacheDBFilePath(string pair_name)
		{
			var dbpath = ResolveUserPath($"PriceData\\{Path_.SanitiseFileName(pair_name)}.db");
			Path_.CreateDirs(Path_.Directory(dbpath));
			return dbpath;
		}

		/// <summary>The location of where to look for bot plugin dlls</summary>
		public static string BotDirectory
		{
			get { return Util.ResolveAppPath("bots"); }
		}
		
		/// <summary>Regex filter pattern for Bot dlls</summary>
		public static string BotRegexFilter
		{
			get { return @"Bot\.(?<name>\w+)\.dll"; }
		}

		/// <summary>User log in</summary>
		public static User LogIn(Form parent, Settings settings)
		{
			// Prompt for the user name and password, so we can load the exchange API keys
			var dlg = new LogInUI
			{
				Username = settings.LastUser,
				Icon = parent.Icon,
				StartPosition = FormStartPosition.CenterScreen,
			};
			using (dlg)
			{
				#if DEBUG
				dlg.Username = "Paul";
				dlg.Password = "UltraSecurePasswordWotIMade";
				#else
				if (dlg.ShowDialog(parent) != DialogResult.OK)
					throw new OperationCanceledException();
				#endif

				settings.LastUser = dlg.Username;
				return new User
				{
					Username = dlg.Username,
					Cred = Crypt.CredHash(dlg.Username, dlg.Password, "RylogicLimitedIsAwesome!"),
				};
			}
		}

		/// <summary>Formatting for log entries</summary>
		public static void LogFormatting(object sender, LogUI.FormattingEventArgs args)
		{
			switch (args.ColumnName)
			{
			case LogUI.ColumnNames.Timestamp:
				{
					args.Value = args.Value is TimeSpan ts ? ts.ToString(@"hh\:mm\:ss") : string.Empty;
					args.FormattingApplied = true;
					break;
				}
			}
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

		/// <summary>Return the 'out' coin for a trade on 'pair' in this trade direction</summary>
		public static Coin CoinOut(this ETradeType tt, TradePair pair)
		{
			return 
				tt == ETradeType.B2Q ? pair.Quote :
				tt == ETradeType.Q2B ? pair.Base :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the 'in' volume for a trade in this trade direction</summary>
		public static Unit<decimal> VolumeIn(this ETradeType tt, Unit<decimal> volume_base, Unit<decimal> price_q2b)
		{
			return 
				tt == ETradeType.B2Q ? volume_base :
				tt == ETradeType.Q2B ? volume_base * price_q2b :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the 'out' volume for a trade in this trade direction</summary>
		public static Unit<decimal> VolumeOut(this ETradeType tt, Unit<decimal> volume_base, Unit<decimal> price_q2b)
		{
			return 
				tt == ETradeType.B2Q ? volume_base * price_q2b :
				tt == ETradeType.Q2B ? volume_base :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return 'price' in (CoinOut/CoinIn) for this trade direction. Assumes price is in (Quote/Base)</summary>
		public static Unit<decimal> Price(this ETradeType tt, Unit<decimal> price_q2b)
		{
			return
				tt == ETradeType.B2Q ? price_q2b :
				tt == ETradeType.Q2B ? Maths.Div(1m._(), price_q2b, 0m / 1m._(price_q2b)) :
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

		/// <summary>Return the volume range to validate against for the input currency</summary>
		public static RangeF<Unit<decimal>> VolumeRangeIn(this ETradeType tt, TradePair pair)
		{
			return
				tt == ETradeType.B2Q ? pair.VolumeRangeBase :
				tt == ETradeType.Q2B ? pair.VolumeRangeQuote :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the volume range to validate against for the output currency</summary>
		public static RangeF<Unit<decimal>> VolumeRangeOut(this ETradeType tt, TradePair pair)
		{
			return
				tt == ETradeType.B2Q ? pair.VolumeRangeQuote :
				tt == ETradeType.Q2B ? pair.VolumeRangeBase :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the default volume to use for a trade on 'pair' (in CoinIn currency)</summary>
		public static Unit<decimal> DefaultTradeVolume(this ETradeType tt, TradePair pair)
		{
			var coin = tt.CoinIn(pair);
			return coin.DefaultTradeVolume;
		}

		/// <summary>Return the default volume to use for a trade on 'pair' (in Base currency)</summary>
		public static Unit<decimal> DefaultTradeVolumeBase(this ETradeType tt, TradePair pair, Unit<decimal> price_q2b)
		{
			var vol = DefaultTradeVolume(tt, pair);
			return
				tt == ETradeType.B2Q ? vol :                // 'vol' is in base 
				tt == ETradeType.Q2B ? vol / price_q2b :    // 'vol' is in quote
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the order book that provides offers for a trade in this direction</summary>
		public static OrderBook OrderBook(this ETradeType tt, TradePair pair)
		{
			return
				tt == ETradeType.B2Q ? pair.B2Q :
				tt == ETradeType.Q2B ? pair.Q2B :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Convert a trade type string to the enumeration value</summary>
		public static ETradeType TradeType(string trade_type)
		{
			switch (trade_type.ToLowerInvariant()) {
			case "q2b": case "buy": case "bid": case "short": return ETradeType.Q2B;
			case "b2q": case "sell": case "ask": case "long": return ETradeType.B2Q;
			}
			throw new Exception("Unknown trade type string");
		}

		/// <summary>Convert a Cryptopia trade type to ETradeType</summary>
		public static ETradeType TradeType(global::Cryptopia.API.EOrderType order_type)
		{
			switch (order_type) {
			default: throw new Exception("Unknown trade type string");
			case global::Cryptopia.API.EOrderType.Buy: return ETradeType.Q2B;
			case global::Cryptopia.API.EOrderType.Sell: return ETradeType.B2Q;
			}
		}

		/// <summary>Convert a Poloniex trade type to ETradeType</summary>
		public static ETradeType TradeType(global::Poloniex.API.EOrderType order_type)
		{
			switch (order_type) {
			default: throw new Exception("Unknown trade type string");
			case global::Poloniex.API.EOrderType.Buy: return ETradeType.Q2B;
			case global::Poloniex.API.EOrderType.Sell: return ETradeType.B2Q;
			}
		}

		/// <summary>Convert a Bittrex trade type to ETradeType</summary>
		public static ETradeType TradeType(global::Bittrex.API.EOrderType order_type)
		{
			switch (order_type) {
			default: throw new Exception("Unknown trade type string");
			case global::Bittrex.API.EOrderType.Buy: return ETradeType.Q2B;
			case global::Bittrex.API.EOrderType.Sell: return ETradeType.B2Q;
			}
		}

		/// <summary>Convert this trade type to the Cryptopia definition of a trade type</summary>
		public static global::Cryptopia.API.EOrderType ToCryptopiaTT(this ETradeType trade_type)
		{
			switch (trade_type) {
			default: throw new Exception("Unknown trade type");
			case ETradeType.Q2B: return global::Cryptopia.API.EOrderType.Buy;
			case ETradeType.B2Q: return global::Cryptopia.API.EOrderType.Sell;
			}
		}

		/// <summary>Convert this trade type to the Poloniex definition of a trade type</summary>
		public static global::Poloniex.API.EOrderType ToPoloniexTT(this ETradeType trade_type)
		{
			switch (trade_type) {
			default: throw new Exception("Unknown trade type");
			case ETradeType.Q2B: return global::Poloniex.API.EOrderType.Buy;
			case ETradeType.B2Q: return global::Poloniex.API.EOrderType.Sell;
			}
		}

		/// <summary>Convert this trade type to the Poloniex definition of a trade type</summary>
		public static global::Bittrex.API.EOrderType ToBittrexTT(this ETradeType trade_type)
		{
			switch (trade_type) {
			default: throw new Exception("Unknown trade type");
			case ETradeType.Q2B: return global::Bittrex.API.EOrderType.Buy;
			case ETradeType.B2Q: return global::Bittrex.API.EOrderType.Sell;
			}
		}

		/// <summary>
		/// Convert a time value into units of 'time_frame'.
		/// e.g if 'time_in_ticks' is 4.3 hours, and 'time_frame' is Hour1, the 4.3 is returned</summary>
		public static double TicksToTimeFrame(long time_in_ticks, ETimeFrame time_frame)
		{
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
		public static TimeSpan TimeFrameToTimeSpan(double units, ETimeFrame time_frame)
		{
			return TimeSpan.FromTicks(TimeFrameToTicks(units, time_frame));
		}

		/// <summary>Return 'time' rounded down to units of 'time_frame'</summary>
		public static DateTimeOffset Round(DateTimeOffset time, ETimeFrame time_frame)
		{
			var tf = TimeFrameToTicks(1.0, time_frame);
			var ticks = (time.Ticks / tf) * tf;
			return new DateTimeOffset(ticks, time.Offset);
		}

		/// <summary>Return the end time for this candle given 'time_frame'</summary>
		public static DateTimeOffset TimestampEnd(this Candle candle, ETimeFrame time_frame)
		{
			return candle.TimestampUTC + TimeFrameToTimeSpan(1.0, time_frame);
		}
	}

	/// <summary>Application Resources</summary>
	public static class Res
	{
		public static readonly Image Active      = new Bitmap(Resources.active, new Size(28,28));
		public static readonly Image Inactive    = new Bitmap(Resources.inactive, new Size(28,28));
		public static readonly Image Changing    = new Bitmap(Resources.changing, new Size(28,28));
		public static readonly Image Play        = new Bitmap(Resources.play, new Size(28, 28));
		public static readonly Image Pause       = new Bitmap(Resources.pause, new Size(28,28));
		public static readonly SoundPlayer Coins = new SoundPlayer(Resources.coins);
	}

	/// <summary>XML tags</summary>
	public static class XmlTag
	{
		public const string Settings = "Settings";
		public const string APIKey = "APIKey";
		public const string APISecret = "APISecret";
	}

	/// <summary>Z-values for chart elements</summary>
	public static class ZOrder
	{
		// Grid lines are drawn at 0
		public const float Min          = 0f;
		public const float Candles      = 0.010f;
		public const float CurrentPrice = 0.011f;
		public const float Indicators   = 0.012f;
		public const float Trades       = 0.013f;
		public const float Max          = 0.1f;
	}

	/// <summary>User details</summary>
	public struct User
	{
		public string Username;
		public byte[] Cred;
	}
}
