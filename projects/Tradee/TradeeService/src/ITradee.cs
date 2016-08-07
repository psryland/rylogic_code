using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Tradee
{
	#region Enumerations
	public enum EMsgType
	{
		// InMsg - messages from client to Tradee
		HelloTradee,
		AccountUpdate,
		PositionsUpdate,
		PendingOrdersUpdate,
		CandleData,
		SymbolData,

		// OutMsg - messages from Tradee to client
		HelloClient,
		RequestAccountStatus,
		RequestInstrument,
	}
	[Flags] public enum ETradeType
	{
		None  = 0,
		Long  = 1 << 0,
		Short = 1 << 1,
	}
	public enum ETimeFrame
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
	public enum ECandleType
	{
		Other,
		Hammer,
		InvHammer,
		SpinningTop,
		Engulging,
	}
	public enum ETradePairs
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

	/// <summary>Marker interface for messages sent between tradee and a client</summary>
	public interface ITradeeMsg :ISerialise
	{}
	public interface ISerialise
	{
		void Serialise(BinaryWriter s);
		void Deserialise(BinaryReader s);
	}

	/// <summary>Account status and risk exposure</summary>
	public class Account :ISerialise
	{
		/// <summary>The ID of the current account, e.g. 123456.</summary>
		public string AccountId { get; set; }

		/// <summary>The broker name of the current account</summary>
		public string BrokerName { get; set; }

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
	//	public double? MarginLevel { get; set; }

		/// <summary>Unrealised gross profit</summary>
		public double UnrealizedGrossProfit { get; set; }

		/// <summary>Unrealised net profit</summary>
		public double UnrealizedNetProfit { get; set; }

		#region Serialise
		public void Serialise(BinaryWriter s)
		{
			s.Write(AccountId);
			s.Write(BrokerName);
			s.Write(Currency);
			s.Write(Balance);
			s.Write(Equity);
			s.Write(FreeMargin);
			s.Write(IsLive);
			s.Write(Leverage);
			s.Write(Margin);
			s.Write(UnrealizedGrossProfit);
			s.Write(UnrealizedNetProfit);
		}
		public void Deserialise(BinaryReader s)
		{
			AccountId             = s.ReadString();
			BrokerName            = s.ReadString();
			Currency              = s.ReadString();
			Balance               = s.ReadDouble();
			Equity                = s.ReadDouble();
			FreeMargin            = s.ReadDouble();
			IsLive                = s.ReadBoolean();
			Leverage              = s.ReadInt32();
			Margin                = s.ReadDouble();
			UnrealizedGrossProfit = s.ReadDouble();
			UnrealizedNetProfit   = s.ReadDouble();
		}
		#endregion
	}

	/// <summary>An active trade</summary>
	public class Position :ISerialise
	{
		// Notes:
		//  To get a price in pips:
		//     pips = price / PriceData.PipSize

		/// <summary>Unique id for the trade</summary>
		public int Id { get; set; }

		/// <summary>The symbol code for the instrument being traded</summary>
		public string SymbolCode { get; set; }

		/// <summary>The type of trade</summary>
		public ETradeType TradeType { get; set; }

		/// <summary>The position entry time in Ticks UTC</summary>
		public long EntryTime { get; set; }

		/// <summary>The position entry price</summary>
		public double EntryPrice { get; set; }

		/// <summary>The distance from the entry price (in pips)</summary>
		public double Pips { get; set; }

		/// <summary>The stop loss (in base currency)</summary>
		public double StopLossAbs { get; set; }

		/// <summary>The take profit (in base currency)</summary>
		public double TakeProfitAbs { get; set; }

		/// <summary>
		/// The stop loss (in base currency) relative to the entry price.
		/// 0 means no stop loss, positive means on the losing side, negative means on the winning side.</summary>
		public double StopLossRel
		{
			get
			{
				switch (TradeType) {
				default: throw new Exception("Unknown trade type");
				case ETradeType.Long:  return EntryPrice - StopLossAbs;
				case ETradeType.Short: return StopLossAbs - EntryPrice;
				}
			}
			set
			{
				switch (TradeType) {
				default: throw new Exception("Unknown trade type");
				case ETradeType.Long:  StopLossAbs = EntryPrice - value; break;
				case ETradeType.Short: StopLossAbs = EntryPrice + value; break;
				}
			}
		}

		/// <summary>
		/// The take profit (in base currency) relative to the entry price.
		/// 0 means no take profit, positive means on the winning side, negative means on the losing side.</summary>
		public double TakeProfitRel
		{
			get
			{
				switch (TradeType) {
				default: throw new Exception("Unknown trade type");
				case ETradeType.Long:  return TakeProfitAbs - EntryPrice;
				case ETradeType.Short: return EntryPrice - TakeProfitAbs;
				}
			}
			set
			{
				switch (TradeType) {
				default: throw new Exception("Unknown trade type");
				case ETradeType.Long:  TakeProfitAbs = EntryPrice + value; break;
				case ETradeType.Short: TakeProfitAbs = EntryPrice - value; break;
				}
			}
		}

		/// <summary>The size of the trade (in lots)</summary>
		public double Quantity { get; set; }

		/// <summary>The amount traded by the position</summary>
		public long Volume { get; set; }

		/// <summary>Gross profit accrued by the order associated with a position</summary>
		public double GrossProfit { get; set; }

		/// <summary>The Net profit of the position/summary>
		public double NetProfit { get; set; }

		/// <summary>Commission Amount of the request to trade one way(Buy/Sell) associated with this position.</summary>
		public double Commissions { get; set; }

		/// <summary>Swap is the overnight interest rate if any, accrued on the position.</summary>
		public double Swap { get; set; }

		/// <summary>User assigned identifier for the position</summary>
		public string Label { get; set; }

		/// <summary>Notes for the trade</summary>
		public string Comment { get; set; }

		#region Serialise
		public void Serialise(BinaryWriter s)
		{
			s.Write(Id            );
			s.Write(SymbolCode    );
			s.Write((int)TradeType);
			s.Write(EntryTime     );
			s.Write(EntryPrice    );
			s.Write(Pips          );
			s.Write(StopLossAbs   );
			s.Write(TakeProfitAbs );
			s.Write(Quantity      );
			s.Write(Volume        );
			s.Write(GrossProfit   );
			s.Write(NetProfit     );
			s.Write(Commissions   );
			s.Write(Swap          );
			s.Write(Label         );
			s.Write(Comment       );
		}
		public void Deserialise(BinaryReader s)
		{
			Id             = s.ReadInt32();
			SymbolCode     = s.ReadString();
			TradeType      = (ETradeType)s.ReadInt32();
			EntryTime      = s.ReadInt64();
			EntryPrice     = s.ReadDouble();
			Pips           = s.ReadDouble();
			StopLossAbs    = s.ReadDouble();
			TakeProfitAbs  = s.ReadDouble();
			Quantity       = s.ReadDouble();
			Volume         = s.ReadInt64();
			GrossProfit    = s.ReadDouble();
			NetProfit      = s.ReadDouble();
			Commissions    = s.ReadDouble();
			Swap           = s.ReadDouble();
			Label          = s.ReadString();
			Comment        = s.ReadString();
		}
		#endregion
	}

	/// <summary>An instruction to make a trade</summary>
	public class PendingOrder :ISerialise
	{
		/// <summary>Unique order Id.</summary>
		public int Id { get; set; }

		/// <summary>Symbol code of the order</summary>
		public string SymbolCode { get; set; }

		/// <summary>Specifies whether this order is to buy or sell</summary>
		public ETradeType TradeType { get; set; }

		/// <summary>The order Expiration time in Ticks since 1/1/0001 UTC</summary>
		public long ExpirationTime { get; set; }

		/// <summary>The price in which to activate the trade</summary>
		public double EntryPrice { get; set; }

		/// <summary>The stop loss (in base currency)</summary>
		public double StopLossAbs { get; set; }

		/// <summary>The take profit (in base currency)</summary>
		public double TakeProfitAbs { get; set; }

		/// <summary>
		/// The stop loss (in base currency) relative to the entry price.
		/// 0 means no stop loss, positive means on the losing side, negative means on the winning side.</summary>
		public double StopLossRel
		{
			get
			{
				switch (TradeType) {
				default: throw new Exception("Unknown trade type");
				case ETradeType.Long:  return EntryPrice - StopLossAbs;
				case ETradeType.Short: return StopLossAbs - EntryPrice;
				}
			}
			set
			{
				switch (TradeType) {
				default: throw new Exception("Unknown trade type");
				case ETradeType.Long:  StopLossAbs = EntryPrice - value; break;
				case ETradeType.Short: StopLossAbs = EntryPrice + value; break;
				}
			}
		}

		/// <summary>
		/// The take profit (in base currency) relative to the entry price.
		/// 0 means no take profit, positive means on the winning side, negative means on the losing side.</summary>
		public double TakeProfitRel
		{
			get
			{
				switch (TradeType) {
				default: throw new Exception("Unknown trade type");
				case ETradeType.Long:  return TakeProfitAbs - EntryPrice;
				case ETradeType.Short: return EntryPrice - TakeProfitAbs;
				}
			}
			set
			{
				switch (TradeType) {
				default: throw new Exception("Unknown trade type");
				case ETradeType.Long:  TakeProfitAbs = EntryPrice + value; break;
				case ETradeType.Short: TakeProfitAbs = EntryPrice - value; break;
				}
			}
		}

		/// <summary>The size of this order (in lots)</summary>
		public double Quantity { get; set; }

		/// <summary>Volume of this order.</summary>
		public long Volume { get; set; }

		/// <summary>User assigned identifier for the order.</summary>
		public string Label { get; set; }

		/// <summary>User comments for the order</summary>
		public string Comment { get; set; }

		#region Serialise
		public void Serialise(BinaryWriter s)
		{
			s.Write(Id            );
			s.Write(SymbolCode    );
			s.Write((int)TradeType);
			s.Write(ExpirationTime);
			s.Write(EntryPrice    );
			s.Write(StopLossAbs   );
			s.Write(TakeProfitAbs );
			s.Write(Quantity      );
			s.Write(Volume        );
			s.Write(Label         );
			s.Write(Comment       );
		}
		public void Deserialise(BinaryReader s)
		{
			Id             = s.ReadInt32();
			SymbolCode     = s.ReadString();
			TradeType      = (ETradeType)s.ReadInt32();
			ExpirationTime = s.ReadInt64();
			EntryPrice     = s.ReadDouble();
			StopLossAbs    = s.ReadDouble();
			TakeProfitAbs  = s.ReadDouble();
			Quantity       = s.ReadDouble();
			Volume         = s.ReadInt64();
			Label          = s.ReadString();
			Comment        = s.ReadString();
		}
		#endregion
	}

	/// <summary>The core data of a single candle</summary>
	public class Candle :ISerialise
	{
		public static readonly Candle Default = new Candle(0, 0, 0, 0, 0, 0);
		public Candle() {}
		public Candle(long timestamp, double open, double high, double low, double close, double volume)
		{
			Timestamp = timestamp;
			Open      = open;
			High      = Math.Max(high, Math.Max(open, close));
			Low       = Math.Min(low , Math.Min(open, close));
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

		/// <summary>The time stamp of this candle (in UTC)</summary>
		public DateTimeOffset TimestampUTC
		{
			get { return new DateTimeOffset(Timestamp, TimeSpan.Zero); }
		}

		/// <summary>The time stamp of this candle (in local time)</summary>
		public DateTime TimestampLocal
		{
			get { return TimeZone.CurrentTimeZone.ToLocalTime(TimestampUTC.DateTime); }
		}

		/// <summary>Friendly print</summary>
		public override string ToString()
		{
			return string.Format("ts:{0} - ohlc:({1:G4} {2:G4} {3:G4} {4:G4}) - vol:{5}", new DateTimeOffset(Timestamp, TimeSpan.Zero).ToString(), Open, High, Low, Close, Volume);
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

		#region Serialise
		public void Serialise(BinaryWriter s)
		{
			s.Write(Timestamp);
			s.Write(Open     );
			s.Write(High     );
			s.Write(Low      );
			s.Write(Close    );
			s.Write(Volume   );
		}
		public void Deserialise(BinaryReader s)
		{
			Timestamp = s.ReadInt64();
			Open      = s.ReadDouble();
			High      = s.ReadDouble();
			Low       = s.ReadDouble();
			Close     = s.ReadDouble();
			Volume    = s.ReadDouble();
		}
		#endregion
	}

	/// <summary>A batch of candle data</summary>
	public class Candles :ISerialise
	{
		public Candles()
		{
			Timestamp = new long  [0];
			Open      = new double[0];
			High      = new double[0];
			Low       = new double[0];
			Close     = new double[0];
			Volume    = new double[0];
		}
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

		#region Serialise
		public void Serialise(BinaryWriter s)
		{
			s.Write(Timestamp.Length);
			foreach (var x in Timestamp) s.Write(x);
			foreach (var x in Open     ) s.Write(x);
			foreach (var x in High     ) s.Write(x);
			foreach (var x in Low      ) s.Write(x);
			foreach (var x in Close    ) s.Write(x);
			foreach (var x in Volume   ) s.Write(x);
		}
		public void Deserialise(BinaryReader s)
		{
			var len = s.ReadInt32();
			Timestamp = new long  [len]; for (int i = 0; i != len; ++i) Timestamp[i] = s.ReadInt64();
			Open      = new double[len]; for (int i = 0; i != len; ++i) Open     [i] = s.ReadDouble();
			High      = new double[len]; for (int i = 0; i != len; ++i) High     [i] = s.ReadDouble();
			Low       = new double[len]; for (int i = 0; i != len; ++i) Low      [i] = s.ReadDouble();
			Close     = new double[len]; for (int i = 0; i != len; ++i) Close    [i] = s.ReadDouble();
			Volume    = new double[len]; for (int i = 0; i != len; ++i) Volume   [i] = s.ReadDouble();
		}
		#endregion
	}

	/// <summary>Stats for a given instrument</summary>
	public class PriceData :ISerialise
	{
		public static readonly PriceData Default = new PriceData(0, 0, 0, 0, 0, 0, 0, 0);
		public PriceData() { }
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

		/// <summary>The price (in base currency) if you want to buy (lowest seller price)</summary>
		public double AskPrice { get; set; }

		/// <summary>The price (in base currency) if you want to sell (highest buyer price)</summary>
		public double BidPrice { get; set; }

		/// <summary>The difference between Ask and Bid (in base currency)</summary>
		public double Spread { get { return AskPrice - BidPrice; } }

		/// <summary>The size of 1 lot (in base currency)</summary>
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

		#region Serialise
		public void Serialise(BinaryWriter s)
		{
			s.Write(AskPrice  );
			s.Write(BidPrice  );
			s.Write(LotSize   );
			s.Write(PipSize   );
			s.Write(PipValue  );
			s.Write(VolumeMin );
			s.Write(VolumeStep);
			s.Write(VolumeMax );
		}
		public void Deserialise(BinaryReader s)
		{
			AskPrice   = s.ReadDouble();
			BidPrice   = s.ReadDouble();
			LotSize    = s.ReadDouble();
			PipSize    = s.ReadDouble();
			PipValue   = s.ReadDouble();
			VolumeMin  = s.ReadDouble();
			VolumeStep = s.ReadDouble();
			VolumeMax  = s.ReadDouble();
		}
		#endregion
	}

	/// <summary>Messages from the trade data source to Tradee</summary>
	public static class InMsg
	{
		public class HelloTradee :ITradeeMsg
		{
			public HelloTradee()
				:this(string.Empty)
			{}
			public HelloTradee(string msg)
			{
				Text = msg;
			}

			/// <summary>Text in the hello message</summary>
			public string Text { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(Text);
			}
			public void Deserialise(BinaryReader s)
			{
				Text = s.ReadString();
			}
			#endregion
		}

		/// <summary>Account status update</summary>
		public class AccountUpdate :ITradeeMsg
		{
			public AccountUpdate()
				:this(new Account())
			{}
			public AccountUpdate(Account acct)
			{
				Acct = acct;
			}

			/// <summary>Account data</summary>
			public Account Acct { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				Acct.Serialise(s);
			}
			public void Deserialise(BinaryReader s)
			{
				Acct.Deserialise(s);
			}
			#endregion
		}

		/// <summary>The currently active trading positions</summary>
		public class PositionsUpdate :ITradeeMsg
		{
			public PositionsUpdate()
				:this(new Position[0])
			{}
			public PositionsUpdate(Position[] positions)
			{
				Positions = positions;
			}

			/// <summary>The collection of positions</summary>
			public Position[] Positions { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(Positions.Length);
				foreach (var pos in Positions)
					pos.Serialise(s);
			}
			public void Deserialise(BinaryReader s)
			{
				Positions = new Position[s.ReadInt32()];
				for (var i = 0; i != Positions.Length; ++i)
					Positions[i] = new Position().Deserialise2(s);
			}
			#endregion
		}

		/// <summary>The currently active trading positions</summary>
		public class PendingOrdersUpdate :ITradeeMsg
		{
			public PendingOrdersUpdate()
				:this(new PendingOrder[0])
			{}
			public PendingOrdersUpdate(PendingOrder[] orders)
			{
				PendingOrders = orders;
			}

			/// <summary>The collection of positions</summary>
			public PendingOrder[] PendingOrders { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(PendingOrders.Length);
				foreach (var order in PendingOrders)
					order.Serialise(s);
			}
			public void Deserialise(BinaryReader s)
			{
				PendingOrders = new PendingOrder[s.ReadInt32()];
				for (var i = 0; i != PendingOrders.Length; ++i)
					PendingOrders[i] = new PendingOrder().Deserialise2(s);
			}
			#endregion
		}
		
		/// <summary>Message for passing a single candle or a batch of candles</summary>
		public class CandleData :ITradeeMsg
		{
			public CandleData()
				:this(string.Empty, ETimeFrame.None, new Candle())
			{}
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
			public string Symbol { get; private set; }

			/// <summary>The time-frame that the candle represents</summary>
			public ETimeFrame TimeFrame { get; private set; }

			/// <summary>A single candle</summary>
			public Candle Candle { get; private set; }

			/// <summary>A batch of candles</summary>
			public Candles Candles { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(Symbol);
				s.Write((int)TimeFrame);
				s.Write(Candle != null ? 1 : 2);
				if (Candle  != null) Candle.Serialise(s);
				if (Candles != null) Candles.Serialise(s);
			}
			public void Deserialise(BinaryReader s)
			{
				Symbol    = s.ReadString();
				TimeFrame = (ETimeFrame)s.ReadInt32();
				var count = s.ReadInt32();
				if (count == 1) { Candle = new Candle().Deserialise2(s); }
				if (count == 2) { Candles = new Candles().Deserialise2(s); }
			}
			#endregion
		}

		/// <summary>Message for passing the current status of a symbol</summary>
		public class SymbolData :ITradeeMsg
		{
			public SymbolData()
				:this(string.Empty, new PriceData())
			{}
			public SymbolData(string sym, PriceData data)
			{
				Symbol = sym;
				Data   = data;
			}

			/// <summary>The symbol that this candle is for</summary>
			public string Symbol { get; private set; }

			/// <summary>Price and Order data about the symbol</summary>
			public PriceData Data { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(Symbol);
				Data.Serialise(s);
			}
			public void Deserialise(BinaryReader s)
			{
				Symbol = s.ReadString();
				Data.Deserialise(s);
			}
			#endregion
		}
	}

	/// <summary>Messages from the tradee to the trade data source</summary>
	public static class OutMsg
	{
		public class HelloClient :ITradeeMsg
		{
			public HelloClient()
				:this(string.Empty)
			{}
			public HelloClient(string msg)
			{
				Text = msg;
			}

			/// <summary>Text in the hello message</summary>
			public string Text { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(Text);
			}
			public void Deserialise(BinaryReader s)
			{
				Text = s.ReadString();
			}
			#endregion
		}

		/// <summary>Request the data source to send account information</summary>
		public class RequestAccountStatus :ITradeeMsg
		{
			public RequestAccountStatus()
				:this(string.Empty)
			{}
			public RequestAccountStatus(string account_id)
			{
				AccountId = account_id;
			}

			/// <summary>The ID of the account to return status for. If null, then trade has no current account info</summary>
			public string AccountId { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(AccountId);
			}
			public void Deserialise(BinaryReader s)
			{
				AccountId = s.ReadString();
			}
			#endregion
		}

		/// <summary>Request instrument data for the given symbol code</summary>
		public class RequestInstrument :ITradeeMsg
		{
			// A request instrument message means, "Make sure you're sending this info, and send me an update now".
			public RequestInstrument()
				:this(string.Empty, new ETimeFrame[0])
			{}
			public RequestInstrument(string sym,  ETimeFrame[] tf)
			{
				SymbolCode = sym;
				TimeFrames = tf ?? new ETimeFrame[0];
			}

			/// <summary>The symbol code to send data for</summary>
			public string SymbolCode { get; private set; }

			/// <summary>The time frames of data to send</summary>
			public ETimeFrame[] TimeFrames { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(SymbolCode);
				s.Write(TimeFrames.Length);
				foreach (var tf in TimeFrames)
					s.Write((int)tf);
			}
			public void Deserialise(BinaryReader s)
			{
				SymbolCode = s.ReadString();
				TimeFrames = new ETimeFrame[s.ReadInt32()];
				for (var i = 0; i != TimeFrames.Length; ++i)
					TimeFrames[i] = (ETimeFrame)s.ReadInt32();
			}
			#endregion
		}
	}

	/// <summary>Global functions</summary>
	public static class Misc
	{
		/// <summary>Deserialise and return this instance</summary>
		public static T Deserialise2<T>(this T obj, BinaryReader s) where T:ISerialise
		{
			obj.Deserialise(s);
			return obj;
		}

		/// <summary>Convert the message type enum to a Type</summary>
		public static Type ToTradeeMsgType(this EMsgType mt)
		{
			switch (mt)
			{
			default: throw new Exception("Unknown message type");
			case EMsgType.HelloTradee:         return typeof(InMsg.HelloTradee);
			case EMsgType.AccountUpdate:       return typeof(InMsg.AccountUpdate);
			case EMsgType.PositionsUpdate:     return typeof(InMsg.PositionsUpdate);
			case EMsgType.PendingOrdersUpdate: return typeof(InMsg.PendingOrdersUpdate);
			case EMsgType.CandleData:          return typeof(InMsg.CandleData);
			case EMsgType.SymbolData:          return typeof(InMsg.SymbolData);

			case EMsgType.HelloClient:          return typeof(OutMsg.HelloClient);
			case EMsgType.RequestAccountStatus: return typeof(OutMsg.RequestAccountStatus);
			case EMsgType.RequestInstrument:    return typeof(OutMsg.RequestInstrument);
			}
		}

		/// <summary>Convert a tradee message type to an EMsgType value</summary>
		public static EMsgType ToMsgType(this ITradeeMsg msg)
		{
			if (msg is InMsg.HelloTradee          ) return EMsgType.HelloTradee;
			if (msg is InMsg.AccountUpdate        ) return EMsgType.AccountUpdate;
			if (msg is InMsg.PositionsUpdate      ) return EMsgType.PositionsUpdate;
			if (msg is InMsg.PendingOrdersUpdate  ) return EMsgType.PendingOrdersUpdate;
			if (msg is InMsg.CandleData           ) return EMsgType.CandleData;
			if (msg is InMsg.SymbolData           ) return EMsgType.SymbolData;
			if (msg is OutMsg.HelloClient         ) return EMsgType.HelloClient;
			if (msg is OutMsg.RequestAccountStatus) return EMsgType.RequestAccountStatus;
			if (msg is OutMsg.RequestInstrument   ) return EMsgType.RequestInstrument;
			throw new Exception("Unknown tradee message type")                    ;
		}

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
		public static double TimeSpanToTimeFrame(TimeSpan ts, ETimeFrame time_frame)
		{
			return TicksToTimeFrame(ts.Ticks, time_frame);
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
		public static TimeSpan TimeFrameToTimeSpan(double units, ETimeFrame time_frame)
		{
			return TimeSpan.FromTicks(TimeFrameToTicks(units, time_frame));
		}
	}
}
