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
		HistoryData,
		MarketOrderChangeResult,

		// OutMsg - messages from Tradee to client
		HelloClient,
		RequestAccountStatus,
		RequestInstrument,
		RequestInstrumentStop,
		RequestInstrumentHistory,
		RequestTradeHistory,
		PlaceMarketOrder,
		//NEW_MSG_HERE
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
	public enum EErrorCode
	{
		[Description("The operation was successful.")] NoError = 0,

		/// <summary>A generic technical error with a trade request.</summary>
		[Description("A generic error with a trade request.")] Failed,

		/// <summary>There are not enough money in the account to trade with.</summary>
		[Description("There are not enough money in the account to trade with.")] InsufficientFunds,

		/// <summary>Position does not exist.</summary>
		[Description("Position does not exist.")] EntityNotFound,

		/// <summary>The volume value is not valid.</summary>
		[Description("The volume value is not valid.")] InvalidVolume,

		/// <summary>The market is closed.</summary>
		[Description("The market is closed.")] MarketClosed,

		/// <summary>The server is disconnected.</summary>
		[Description("The server is disconnected.")] Disconnected,

		/// <summary>Operation timed out.</summary>
		[Description("Operation timed out.")] Timeout,
	}
	#endregion

	/// <summary>Marker interface for messages sent between tradee and a client</summary>
	public interface ISerialise
	{
		void Serialise(BinaryWriter s);
		void Deserialise(BinaryReader s);
	}
	public interface ITradeeMsg :ISerialise
	{}

	/// <summary>Things that 'Position' and 'PendingOrder' have in common</summary>
	public interface IOrder
	{
		/// <summary>Unique order Id.</summary>
		int Id { get; }

		/// <summary>Symbol code of the order</summary>
		string SymbolCode { get; }

		/// <summary>Specifies whether this order is to buy or sell</summary>
		ETradeType TradeType { get; }

		/// <summary>The price which activated or will activate the trade</summary>
		double EntryPrice { get; }

		/// <summary>The stop loss (in base currency)</summary>
		double StopLossAbs { get; }

		/// <summary>The take profit (in base currency)</summary>
		double TakeProfitAbs { get; }

		/// <summary>
		/// The stop loss (in base currency) relative to the entry price.
		/// 0 means no stop loss, positive means on the losing side, negative means on the winning side.</summary>
		double StopLossRel { get; }

		/// <summary>
		/// The take profit (in base currency) relative to the entry price.
		/// 0 means no take profit, positive means on the winning side, negative means on the losing side.</summary>
		double TakeProfitRel { get; }

		/// <summary>The size of this order (in lots)</summary>
		double Quantity { get; }

		/// <summary>Volume of this order.</summary>
		long Volume { get; }

		/// <summary>User assigned identifier for the order.</summary>
		string Label { get; }

		/// <summary>User comments for the order</summary>
		string Comment { get; }
	}

	/// <summary>Account status and risk exposure</summary>
	public class Account :ISerialise
	{
		public Account()
		{
			AccountId             = string.Empty;
			BrokerName            = string.Empty;
			Currency              = string.Empty;
			Balance               = 0.0;
			Equity                = 0.0;
			FreeMargin            = 0.0;
			IsLive                = false;
			Leverage              = 0;
			Margin                = 0.0;
			UnrealizedGrossProfit = 0.0;
			UnrealizedNetProfit   = 0.0;
		}
		public Account(Account rhs)
		{
			AccountId             = rhs.AccountId;
			BrokerName            = rhs.BrokerName;
			Currency              = rhs.Currency;
			Balance               = rhs.Balance;
			Equity                = rhs.Equity;
			FreeMargin            = rhs.FreeMargin;
			IsLive                = rhs.IsLive;
			Leverage              = rhs.Leverage;
			Margin                = rhs.Margin;
			UnrealizedGrossProfit = rhs.UnrealizedGrossProfit;
			UnrealizedNetProfit   = rhs.UnrealizedNetProfit;
		}

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
	public class Position :ISerialise ,IOrder
	{
		// Notes:
		//  To get a base currency price in pips:
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
	public class PendingOrder :ISerialise ,IOrder
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
	public class PriceCandle :ISerialise
	{
		public static readonly PriceCandle Default = new PriceCandle();

		public PriceCandle() {}
		public PriceCandle(long timestamp, double open, double high, double low, double close, double median, double volume)
		{
			Timestamp = timestamp;
			Open      = open;
			High      = Math.Max(high, Math.Max(open, close));
			Low       = Math.Min(low , Math.Min(open, close));
			Close     = close;
			Median    = median;
			Volume    = volume;
		}
		public PriceCandle(PriceCandle rhs)
		{
			Timestamp = rhs.Timestamp;
			Open      = rhs.Open;
			High      = rhs.High;
			Low       = rhs.Low;
			Close     = rhs.Close;
			Median    = rhs.Median;
			Volume    = rhs.Volume;
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

		/// <summary>The median price over the duration of the candle</summary>
		public double Median { get; set; }

		/// <summary>The number of ticks in this candle</summary>
		public double Volume { get; set; }

		/// <summary>Friendly print</summary>
		public override string ToString()
		{
			return string.Format("ts:{0} - ohlc:({1:G4} {2:G4} {3:G4} {4:G4}) - vol:{5}", new DateTimeOffset(Timestamp, TimeSpan.Zero).ToString(), Open, High, Low, Close, Volume);
		}

		#region Equals
		public bool Equals(PriceCandle rhs)
		{
			return
				Timestamp == rhs.Timestamp &&
				Open      == rhs.Open      &&
				High      == rhs.High      &&
				Low       == rhs.Low       &&
				Close     == rhs.Close     &&
				Median    == rhs.Median    &&
				Volume    == rhs.Volume    ;
		}
		public override bool Equals(object obj)
		{
			return obj is PriceCandle && Equals((PriceCandle)obj);
		}
		public override int GetHashCode()
		{
			return new { Timestamp, Open, High, Low, Close, Median, Volume }.GetHashCode();
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
			s.Write(Median   );
			s.Write(Volume   );
		}
		public void Deserialise(BinaryReader s)
		{
			Timestamp = s.ReadInt64();
			Open      = s.ReadDouble();
			High      = s.ReadDouble();
			Low       = s.ReadDouble();
			Close     = s.ReadDouble();
			Median    = s.ReadDouble();
			Volume    = s.ReadDouble();
		}
		#endregion
	}

	/// <summary>A batch of candle data</summary>
	public class PriceCandles :ISerialise
	{
		public PriceCandles()
		{
			Timestamp = new long  [0];
			Open      = new double[0];
			High      = new double[0];
			Low       = new double[0];
			Close     = new double[0];
			Median    = new double[0];
			Volume    = new double[0];
		}
		public PriceCandles(long[] timestamp, double[] open, double[] high, double[] low, double[] close, double[] median, double[] volume)
		{
			Debug.Assert(new[] { timestamp.Length, open.Length, high.Length, low.Length, close.Length, volume.Length }.All(x => x == timestamp.Length), "All arrays should be the same length");
			Timestamp = timestamp;
			Open      = open;
			High      = high;
			Low       = low;
			Close     = close;
			Median    = median;
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

		/// <summary>The median price</summary>
		public double[] Median { get; set; }

		/// <summary>The number of ticks in each candle</summary>
		public double[] Volume { get; set; }

		/// <summary>Enumerate each candle in the batch</summary>
		public IEnumerable<PriceCandle> AllCandles
		{
			get
			{
				for (int i = 0; i != Timestamp.Length; ++i)
					yield return new PriceCandle(Timestamp[i], Open[i], High[i], Low[i], Close[i], Median[i], Volume[i]);
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
			foreach (var x in Median   ) s.Write(x);
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
			Median    = new double[len]; for (int i = 0; i != len; ++i) Median   [i] = s.ReadDouble();
			Volume    = new double[len]; for (int i = 0; i != len; ++i) Volume   [i] = s.ReadDouble();
		}
		#endregion
	}

	/// <summary>Stats for a given instrument</summary>
	public class PriceData :ISerialise
	{
		public static readonly PriceData Default = new PriceData();

		public PriceData() { }
		public PriceData(double ask, double bid, double avr_spread, double lot_size, double pip_size, double pip_value, double volume_min, double volume_step, double volume_max)
		{
			AskPrice   = ask;
			BidPrice   = bid;
			AvrSpread  = avr_spread;
			LotSize    = lot_size;
			PipSize    = pip_size;
			PipValue   = pip_value;
			VolumeMin  = volume_min;
			VolumeStep = volume_step;
			VolumeMax  = volume_max;
		}
		public PriceData(PriceData rhs)
		{
			AskPrice   = rhs.AskPrice;
			BidPrice   = rhs.BidPrice;
			AvrSpread  = rhs.AvrSpread;
			LotSize    = rhs.LotSize;
			PipSize    = rhs.PipSize;
			PipValue   = rhs.PipValue;
			VolumeMin  = rhs.VolumeMin;
			VolumeStep = rhs.VolumeStep;
			VolumeMax  = rhs.VolumeMax;
		}

		/// <summary>The price (in base currency) if you want to buy (lowest seller price)</summary>
		public double AskPrice { get; set; }

		/// <summary>The price (in base currency) if you want to sell (highest buyer price)</summary>
		public double BidPrice { get; set; }

		/// <summary>The current difference between Ask and Bid (in base currency)</summary>
		public double Spread { get { return AskPrice - BidPrice; } }
		
		/// <summary>The spread for this instrument (in pips)</summary>
		public double SpreadPips { get { return Spread / PipSize; } }

		/// <summary>Typical spread value for the associated instrument</summary>
		public double AvrSpread { get; set; }

		/// <summary>The size of 1 lot (in base currency)</summary>
		public double LotSize { get; set; }

		/// <summary>The smallest unit of change for the symbol</summary>
		public double PipSize { get; set; }

		/// <summary>The monetary value of 1 pip</summary>
		public double PipValue { get; set; }

		/// <summary>The number of decimal places to this display the price with</summary>
		public int DecimalPlaces
		{
			get { return (int)Math.Max(0, -Math.Floor(Math.Log10(PipSize))); }
		}

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
				AvrSpread  == rhs.AvrSpread  &&
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
				AvrSpread ,
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
			s.Write(AvrSpread );
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
			AvrSpread  = s.ReadDouble();
			LotSize    = s.ReadDouble();
			PipSize    = s.ReadDouble();
			PipValue   = s.ReadDouble();
			VolumeMin  = s.ReadDouble();
			VolumeStep = s.ReadDouble();
			VolumeMax  = s.ReadDouble();
		}
		#endregion
	}

	/// <summary>A historic trade</summary>
	public class ClosedOrder :ISerialise
	{
		/// <summary>The position/trade's unique id</summary>
		public int Id { get; set; }
		// The position's unique identifier.
		//int PositionId { get; }

		/// <summary>Symbol code of the Historical Trade.</summary>
		public string SymbolCode { get; set; }

		/// <summary>The TradeType of the Opening Deal</summary>
		public ETradeType TradeType { get; set; }

		/// <summary>Account balance after the Deal was filled</summary>
		public double Balance { get; set; }

		/// <summary>Time of the Opening Deal, or the time of the first Opening deal that was closed (in ticks UTC)</summary>
		public long EntryTimeUTC { get; set; }

		/// <summary>Time of the Closing Deal (in ticks UTC)</summary>
		public long ExitTimeUTC { get; set; }

		/// <summary>The distance from the entry price (in pips)</summary>
		public double Pips { get; set; }

		/// <summary>The VWAP (Volume Weighted Average Price) of the Opening Deals that are closed</summary>
		public double EntryPrice { get; set; }

		/// <summary>The execution price of the Closing Deal</summary>
		public double ExitPrice { get; set; }

		/// <summary>The Quantity (in lots) that was closed by the Closing Deal</summary>
		public double Quantity { get; set; }

		/// <summary>The Volume that was closed by the Closing Deal</summary>
		public long Volume { get; set; }

		/// <summary>Profit and loss before swaps and commission</summary>
		public double GrossProfit { get; set; }

		/// <summary>Profit and loss including swaps and commissions</summary>
		public double NetProfit { get; set; }

		/// <summary>Commission paid</summary>
		public double Commissions { get; set; }

		/// <summary>Swap is the overnight interest rate if any, accrued on the position.</summary>
		public double Swap { get; set; }

		/// <summary>User assigned identifier for the position</summary>
		public string Label { get; set; }

		/// <summary>Comments about the trade</summary>
		public string Comment { get; set; }

		#region Serialise
		public void Serialise(BinaryWriter s)
		{
			s.Write(Id             );
			s.Write(SymbolCode     );
			s.Write((int)TradeType );
			s.Write(Balance        );
			s.Write(EntryTimeUTC   );
			s.Write(ExitTimeUTC );
			s.Write(Pips           );
			s.Write(EntryPrice     );
			s.Write(ExitPrice   );
			s.Write(Quantity       );
			s.Write(Volume         );
			s.Write(GrossProfit    );
			s.Write(NetProfit      );
			s.Write(Commissions    );
			s.Write(Swap           );
			s.Write(Label          );
			s.Write(Comment        );
		}
		public void Deserialise(BinaryReader s)
		{
			Id             = s.ReadInt32();
			SymbolCode     = s.ReadString();
			TradeType      = (ETradeType)s.ReadInt32();
			Balance        = s.ReadDouble();
			EntryTimeUTC   = s.ReadInt64();
			ExitTimeUTC = s.ReadInt64();
			Pips           = s.ReadDouble();
			EntryPrice     = s.ReadDouble();
			ExitPrice   = s.ReadDouble();
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

	/// <summary>Messages from the trade data source *in* to Tradee</summary>
	public static class InMsg
	{
		// Search for '//NEW_MSG_HERE'

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
			{
				SymbolCode = string.Empty;
				TimeFrame  = ETimeFrame.None;
				Candle     = null;
				Candles    = null;
			}
			public CandleData(string symbol_code, ETimeFrame time_frame, PriceCandle candle)
			{
				SymbolCode = symbol_code;
				TimeFrame  = time_frame;
				Candle     = candle;
			}
			public CandleData(string symbol_code, ETimeFrame time_frame, PriceCandles candles)
			{
				SymbolCode = symbol_code;
				TimeFrame  = time_frame;
				Candles    = candles;
			}

			/// <summary>The symbol that this candle is for</summary>
			public string SymbolCode { get; private set; }

			/// <summary>The time-frame that the candle represents</summary>
			public ETimeFrame TimeFrame { get; private set; }

			/// <summary>A single candle</summary>
			public PriceCandle Candle { get; private set; }

			/// <summary>A batch of candles</summary>
			public PriceCandles Candles { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(SymbolCode);
				s.Write((int)TimeFrame);
				s.Write(Candle != null ? 1 : Candles != null ? 2 : 0);
				if (Candle  != null) Candle.Serialise(s);
				if (Candles != null) Candles.Serialise(s);
			}
			public void Deserialise(BinaryReader s)
			{
				SymbolCode    = s.ReadString();
				TimeFrame = (ETimeFrame)s.ReadInt32();
				var what = s.ReadInt32();
				if (what == 1) Candle = new PriceCandle().Deserialise2(s);
				if (what == 2) Candles = new PriceCandles().Deserialise2(s);
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

		/// <summary>The trade history of the current account</summary>
		public class HistoryData :ITradeeMsg
		{
			public HistoryData()
				:this(new ClosedOrder[0])
			{}
			public HistoryData(ClosedOrder[] orders)
			{
				Orders = orders;
			}

			/// <summary>The historical trades of this account</summary>
			public ClosedOrder[] Orders { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(Orders.Length);
				foreach (var x in Orders) x.Serialise(s);
			}
			public void Deserialise(BinaryReader s)
			{
				var len = s.ReadInt32();
				Orders = new ClosedOrder[len];
				for (int i = 0; i != len; ++i)
					Orders[i] = new ClosedOrder().Deserialise2(s);
			}
			#endregion
		}

		/// <summary>A response to a request to create a new order or modify an existing order</summary>
		public class MarketOrderChangeResult :ITradeeMsg
		{
			public MarketOrderChangeResult(bool success, EErrorCode error_code)
			{
				Success = success;
				ErrorCode = error_code;
			}

			/// <summary>True if the operation was successful</summary>
			public bool Success { get; private set; }

			/// <summary>A description of the error if 'Success' is false</summary>
			public EErrorCode ErrorCode { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(Success);
				s.Write((int)ErrorCode);
			}
			public void Deserialise(BinaryReader s)
			{
				Success = s.ReadBoolean();
				ErrorCode = (EErrorCode)s.ReadInt32();
			}
			#endregion
		}
	}

	/// <summary>Messages from the tradee *out* to the trade data source</summary>
	public static class OutMsg
	{
		// Search for '//NEW_MSG_HERE'

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

		/// <summary>Request real time updates of the given instrument</summary>
		public class RequestInstrument :ITradeeMsg
		{
			public RequestInstrument()
				:this(string.Empty, ETimeFrame.None)
			{}
			public RequestInstrument(string sym, ETimeFrame tf)
			{
				SymbolCode = sym;
				TimeFrame = tf;
			}

			/// <summary>The symbol code to send data for</summary>
			public string SymbolCode { get; private set; }

			/// <summary>The time frame to send (unioned with other time frames being sent already)</summary>
			public ETimeFrame TimeFrame { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(SymbolCode);
				s.Write((int)TimeFrame);
			}
			public void Deserialise(BinaryReader s)
			{
				SymbolCode = s.ReadString();
				TimeFrame = (ETimeFrame)s.ReadInt32();
			}
			#endregion
		}

		/// <summary>Tell the trade data source that data for the given instrument is no longer needed</summary>
		public class RequestInstrumentStop :ITradeeMsg
		{
			public RequestInstrumentStop()
				:this(string.Empty, ETimeFrame.None)
			{ }
			public RequestInstrumentStop(string sym, ETimeFrame tf)
			{
				SymbolCode = sym;
				TimeFrame = tf;
			}

			/// <summary>The symbol code to stop sending data for</summary>
			public string SymbolCode { get; private set; }

			/// <summary>The time frame to stop sending. 'None' means remove all time frames</summary>
			public ETimeFrame TimeFrame { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(SymbolCode);
			}
			public void Deserialise(BinaryReader s)
			{
				SymbolCode = s.ReadString();
			}
			#endregion
		}

		/// <summary>Request a time range of history instrument data</summary>
		public class RequestInstrumentHistory :ITradeeMsg
		{
			public RequestInstrumentHistory()
				:this(string.Empty, ETimeFrame.None)
			{}
			public RequestInstrumentHistory(string sym, ETimeFrame tf)
			{
				SymbolCode = sym;
				TimeFrame = tf;
				TimeRanges = new List<long>();
				IndexRanges = new List<int>();
			}

			/// <summary>The symbol code to send data for</summary>
			public string SymbolCode { get; private set; }

			/// <summary>The time frame to send</summary>
			public ETimeFrame TimeFrame { get; private set; }

			/// <summary>Pairs of start/end timestamps (in ticks UTC) to return</summary>
			public List<long> TimeRanges { get; private set; }

			/// <summary>Pairs of start/end indices in the instrument data to return. Indices are 'now-relative', i.e. 0 = latest, -Count = oldest</summary>
			public List<int> IndexRanges { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(SymbolCode);
				s.Write((int)TimeFrame);
				s.Write(TimeRanges.Count);
				foreach (var tr in TimeRanges) s.Write(tr);
				s.Write(IndexRanges.Count);
				foreach (var idx in IndexRanges) s.Write(idx);
			}
			public void Deserialise(BinaryReader s)
			{
				SymbolCode = s.ReadString();
				TimeFrame = (ETimeFrame)s.ReadInt32();

				var tr_count = s.ReadInt32();
				TimeRanges.Clear();
				TimeRanges.Capacity = tr_count;
				for (var i = 0; i != tr_count; ++i)
					TimeRanges.Add(s.ReadInt64());

				var idx_count = s.ReadInt32();
				IndexRanges.Clear();
				IndexRanges.Capacity = idx_count;
				for (var i = 0; i != idx_count; ++i)
					IndexRanges.Add(s.ReadInt32());
			}
			#endregion
		}

		/// <summary>Request the historic trade data</summary>
		public class RequestTradeHistory :ITradeeMsg
		{
			public RequestTradeHistory()
				:this(DateTimeOffset.MinValue.Ticks, DateTimeOffset.MaxValue.Ticks)
			{}
			public RequestTradeHistory(long time_beg, long time_end)
			{
				TimeBegUTC = time_beg;
				TimeEndUTC = time_end;
			}

			/// <summary>The beginning of the time range to get</summary>
			public long TimeBegUTC { get; private set; }

			/// <summary>The end of the time range to get</summary>
			public long TimeEndUTC { get; private set; }

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(TimeBegUTC);
				s.Write(TimeEndUTC);
			}
			public void Deserialise(BinaryReader s)
			{
				TimeBegUTC = s.ReadInt64();
				TimeEndUTC = s.ReadInt64();
			}
			#endregion
		}

		/// <summary>Make an order to buy or sell an instrument</summary>
		public class PlaceMarketOrder :ITradeeMsg
		{
			// Notes:
			// - Only one of 'Position', 'PendingLimit', or 'PendingStop' should be non-null

			public PlaceMarketOrder()
			{
				Id              = Guid.Empty;
				Position        = null;
				MarketRangePips = 0;
				PendingLimit    = null;
				PendingStop     = null;
			}
			public PlaceMarketOrder(Position position, double market_range_pips)
			{
				Id              = Guid.NewGuid();
				Position        = position;
				MarketRangePips = market_range_pips;
				PendingLimit    = null;
				PendingStop     = null;
			}
			public PlaceMarketOrder(PendingOrder pending, bool limit)
			{
				Id              = Guid.NewGuid();
				Position        = null;
				MarketRangePips = 0;
				PendingLimit    = limit ? pending : null;
				PendingStop     = !limit ? pending : null;
			}

			/// <summary>A UID for uniquely identifying responses to this PlaceMarketOrder message</summary>
			public Guid Id { get; private set; }

			/// <summary>How close the current price must be to the entry price in order for the order to be placed</summary>
			public double MarketRangePips { get; private set; }

			/// <summary>
			/// If not null, place an immediate order.
			/// Required non-zero values are: TradeType, SymbolCode, EntryPrice, Volume.
			/// Optional values are: StopLossRel, TakeProfitRel, Label, Comment.</summary>
			public Position Position { get; private set; }

			/// <summary>
			/// If not null, place a pending limit order.
			/// A limit order is one where the current price is in the direction of profit for the order.
			/// A limit order would typically be used for reversal trades. i.e. expecting the price to reach the entry price then reverse.
			/// "If the price gets to here, then I expect it to reverse."</summary>
			public PendingOrder PendingLimit { get; private set; }

			/// <summary>
			/// If not null, place a pending stop order.
			/// A stop order is one where the current price is in the direction of loss for the order.
			/// A stop order would typically be used for continuation trades. i.e. expecting the price to continue in the current direction
			/// "If the price gets to here, then I expect it to carry on to here".</summary>
			public PendingOrder PendingStop  { get; private set; }

			/// <summary>Return a common interface to the order</summary>
			public IOrder Order
			{
				get
				{
					return
						Position     != null ? Position     :
						PendingLimit != null ? PendingLimit :
						PendingStop  != null ? PendingStop  :
						(IOrder)null;
				}
			}

			#region Serialise
			public void Serialise(BinaryWriter s)
			{
				s.Write(Id.ToString());
				s.Write(MarketRangePips);
				if      (Position     != null) { s.Write(1); Position.Serialise(s); }
				else if (PendingLimit != null) { s.Write(2); PendingLimit.Serialise(s); }
				else if (PendingStop  != null) { s.Write(3); PendingStop.Serialise(s); }
			}
			public void Deserialise(BinaryReader s)
			{
				Id = new Guid(s.ReadString());
				MarketRangePips = s.ReadDouble();
				var what = s.ReadInt32();
				if      (what == 1) Position = new Position().Deserialise2(s);
				else if (what == 2) PendingLimit = new PendingOrder().Deserialise2(s);
				else if (what == 3) PendingStop = new PendingOrder().Deserialise2(s);
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
			case EMsgType.HelloTradee:             return typeof(InMsg.HelloTradee);
			case EMsgType.AccountUpdate:           return typeof(InMsg.AccountUpdate);
			case EMsgType.PositionsUpdate:         return typeof(InMsg.PositionsUpdate);
			case EMsgType.PendingOrdersUpdate:     return typeof(InMsg.PendingOrdersUpdate);
			case EMsgType.CandleData:              return typeof(InMsg.CandleData);
			case EMsgType.SymbolData:              return typeof(InMsg.SymbolData);
			case EMsgType.HistoryData:             return typeof(InMsg.HistoryData);
			case EMsgType.MarketOrderChangeResult: return typeof(InMsg.MarketOrderChangeResult);

			case EMsgType.HelloClient:              return typeof(OutMsg.HelloClient);
			case EMsgType.RequestAccountStatus:     return typeof(OutMsg.RequestAccountStatus);
			case EMsgType.RequestInstrument:        return typeof(OutMsg.RequestInstrument);
			case EMsgType.RequestInstrumentStop:    return typeof(OutMsg.RequestInstrumentStop);
			case EMsgType.RequestInstrumentHistory: return typeof(OutMsg.RequestInstrumentHistory);
			case EMsgType.RequestTradeHistory:      return typeof(OutMsg.RequestTradeHistory);
			case EMsgType.PlaceMarketOrder:         return typeof(OutMsg.PlaceMarketOrder);
			//NEW_MSG_HERE
			}
		}

		/// <summary>Convert a tradee message type to an EMsgType value</summary>
		public static EMsgType ToMsgType(this ITradeeMsg msg)
		{
			if (msg is InMsg.HelloTradee               ) return EMsgType.HelloTradee;
			if (msg is InMsg.AccountUpdate             ) return EMsgType.AccountUpdate;
			if (msg is InMsg.PositionsUpdate           ) return EMsgType.PositionsUpdate;
			if (msg is InMsg.PendingOrdersUpdate       ) return EMsgType.PendingOrdersUpdate;
			if (msg is InMsg.CandleData                ) return EMsgType.CandleData;
			if (msg is InMsg.SymbolData                ) return EMsgType.SymbolData;
			if (msg is InMsg.HistoryData               ) return EMsgType.HistoryData;
			if (msg is InMsg.MarketOrderChangeResult   ) return EMsgType.MarketOrderChangeResult;

			if (msg is OutMsg.HelloClient              ) return EMsgType.HelloClient;
			if (msg is OutMsg.RequestAccountStatus     ) return EMsgType.RequestAccountStatus;
			if (msg is OutMsg.RequestInstrument        ) return EMsgType.RequestInstrument;
			if (msg is OutMsg.RequestInstrumentStop    ) return EMsgType.RequestInstrumentStop;
			if (msg is OutMsg.RequestInstrumentHistory ) return EMsgType.RequestInstrumentHistory;
			if (msg is OutMsg.RequestTradeHistory      ) return EMsgType.RequestTradeHistory;
			if (msg is OutMsg.PlaceMarketOrder         ) return EMsgType.PlaceMarketOrder;
			//NEW_MSG_HERE
			throw new Exception("Unknown tradee message type");
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

		/// <summary>Get a longer name for a symbol code</summary>
		public static KnownSymbolsMap KnownSymbols = new KnownSymbolsMap();
		public class KnownSymbolsMap :Dictionary<string, SymbolInfo>
		{
			public KnownSymbolsMap()
			{
				// Currencies
				Add("USDCAD", new SymbolInfo("USD - Canadian Dollar"   , "$ {0:N2}"));
				Add("USDCHF", new SymbolInfo("USD - Swiss Franc"       , "$ {0:N2}"));
				Add("USDCNH", new SymbolInfo("USD - Chinese Offshore"  , "$ {0:N2}"));
				Add("USDCZK", new SymbolInfo("USD - Czech Koruna"      , "$ {0:N2}"));
				Add("USDHKD", new SymbolInfo("USD - Hong Kong Dollar"  , "$ {0:N2}"));
				Add("USDJPY", new SymbolInfo("USD - Japanese Yen"      , "$ {0:N2}"));
				Add("USDMXN", new SymbolInfo("USD - Mexican Peso"      , "$ {0:N2}"));
				Add("USDNOK", new SymbolInfo("USD - Norwegian Krone"   , "$ {0:N2}"));
				Add("USDPLN", new SymbolInfo("USD - Polish Zloty"      , "$ {0:N2}"));
				Add("USDRUB", new SymbolInfo("USD - Russian Ruble"     , "$ {0:N2}"));
				Add("USDSEK", new SymbolInfo("USD - Swedish Krona"     , "$ {0:N2}"));
				Add("USDSGD", new SymbolInfo("USD - Singapore Dollar"  , "$ {0:N2}"));
				Add("USDTRY", new SymbolInfo("USD - Turkish Lira"      , "$ {0:N2}"));
				Add("USDTHB", new SymbolInfo("USD - Thai Bhat"         , "$ {0:N2}"));
				Add("USDZAR", new SymbolInfo("USD - South African Rand", "$ {0:N2}"));

				Add("EURAUD", new SymbolInfo("Euro - Australian Dollar" , "€ {0:N2}"));
				Add("EURCAD", new SymbolInfo("Euro - Canadian Dollar"   , "€ {0:N2}"));
				Add("EURCHF", new SymbolInfo("Euro - Swiss Franc"       , "€ {0:N2}"));
				Add("EURCZK", new SymbolInfo("Euro - Czech Koruna"      , "€ {0:N2}"));
				Add("EURGBP", new SymbolInfo("Euro - British Pound"     , "€ {0:N2}"));
				Add("EURJPY", new SymbolInfo("Euro - Japanese Yen"      , "€ {0:N2}"));
				Add("EURNOK", new SymbolInfo("Euro - Norwegian Krone"   , "€ {0:N2}"));
				Add("EURNZD", new SymbolInfo("Euro - New Zealand Dollar", "€ {0:N2}"));
				Add("EURPLN", new SymbolInfo("Euro - Polish Zloty"      , "€ {0:N2}"));
				Add("EURSEK", new SymbolInfo("Euro - Swedish Krona"     , "€ {0:N2}"));
				Add("EURSGD", new SymbolInfo("Euro - Singapore Dollar"  , "€ {0:N2}"));
				Add("EURTRY", new SymbolInfo("Euro - Turkish Lira"      , "€ {0:N2}"));
				Add("EURUSD", new SymbolInfo("Euro - USD"               , "€ {0:N2}"));
				Add("EURZAR", new SymbolInfo("Euro - South African Rand", "€ {0:N2}"));

				Add("GBPAUD", new SymbolInfo("British Pound - Australian Dollar" , "£ {0:N2}"));
				Add("GBPCAD", new SymbolInfo("British Pound - Canadian Dollar"   , "£ {0:N2}"));
				Add("GBPCHF", new SymbolInfo("British Pound - Swiss Franc"       , "£ {0:N2}"));
				Add("GBPJPY", new SymbolInfo("British Pound - Japanese Yen"      , "£ {0:N2}"));
				Add("GBPNOK", new SymbolInfo("British Pound - Norwegian Krone"   , "£ {0:N2}"));
				Add("GBPNZD", new SymbolInfo("British Pound - New Zealand Dollar", "£ {0:N2}"));
				Add("GBPSEK", new SymbolInfo("British Pound - Swedish Krona"     , "£ {0:N2}"));
				Add("GBPSGD", new SymbolInfo("British Pound - Singapore Dollar"  , "£ {0:N2}"));
				Add("GBPTRY", new SymbolInfo("British Pound - Turkish Lira"      , "£ {0:N2}"));
				Add("GBPUSD", new SymbolInfo("British Pound - USD"               , "£ {0:N2}"));

				Add("AUDCAD", new SymbolInfo("Australian Dollar - Australian Dollar" , "$ {0:N2} (AUD)"));
				Add("AUDCHF", new SymbolInfo("Australian Dollar - Swiss Franc"       , "$ {0:N2} (AUD)"));
				Add("AUDJPY", new SymbolInfo("Australian Dollar - Japanese Yen"      , "$ {0:N2} (AUD)"));
				Add("AUDNZD", new SymbolInfo("Australian Dollar - New Zealand Dollar", "$ {0:N2} (AUD)"));
				Add("AUDSGD", new SymbolInfo("Australian Dollar - Singapore Dollar"  , "$ {0:N2} (AUD)"));
				Add("AUDUSD", new SymbolInfo("Australian Dollar - USD"               , "$ {0:N2} (AUD)"));

				Add("NZDCAD", new SymbolInfo("New Zealand Dollar - Canadian Dollar", "$ {0:N2} (NZD)"));
				Add("NZDCHF", new SymbolInfo("New Zealand Dollar - Swiss Franc"    , "$ {0:N2} (NZD)"));
				Add("NZDJPY", new SymbolInfo("New Zealand Dollar - Japanese Yen"   , "$ {0:N2} (NZD)"));
				Add("NZDUSD", new SymbolInfo("New Zealand Dollar - USD"            , "$ {0:N2} (NZD)"));

				Add("CHFJPY", new SymbolInfo("Swiss Franc - Japanese Yen"     , "{0:N2} (CHF)"));
				Add("CHFSGD", new SymbolInfo("Swiss Franc - Singapore Dollar" , "{0:N2} (CHF)"));
				Add("CADCHF", new SymbolInfo("Canadian Dollar - Swiss Franc"  , "{0:N2} (CAD)"));
				Add("CADJPY", new SymbolInfo("Canadian Dollar - Japanese Yen" , "{0:N2} (CAD)"));
				Add("NOKJPY", new SymbolInfo("Norwegian Krone - Japanese Yen" , "{0:N2} (NOK)"));
				Add("NOKSEK", new SymbolInfo("Norwegian Krone - Swedish Krona", "{0:N2} (NOK)"));
				Add("SEKJPY", new SymbolInfo("Swedish Krona - Japanese Yen"   , "{0:N2} (SEK)"));
				Add("SGDJPY", new SymbolInfo("Singapore Dollar - Japanese Yen", "{0:N2} (SGD)"));

				// Commodities
				Add("XAUUSD", new SymbolInfo("Gold - USD"       , "{0:N0} Oz"));
				Add("XAGUSD", new SymbolInfo("Silver - USD"     , "{0:N0} Oz"));
				Add("XPDUSD", new SymbolInfo("Palladium - USD"  , "{0:N0} Oz"));
				Add("XPTUSD", new SymbolInfo("Platinum - USD"   , "{0:N0} Oz"));
				Add("XNGUSD", new SymbolInfo("Natural Gas - USD", "{0:N0} Oz"));

				// Indices
				Add("AUS200" , new SymbolInfo("Australian S&P/ASX 200 Index", "{0:N0} indices"));
				Add("EUSTX50", new SymbolInfo("Euro STOXX 50 Index"         , "{0:N0} indices"));
				Add("FRA40"  , new SymbolInfo("French CAC 40 Index"         , "{0:N0} indices"));
				Add("GER30"  , new SymbolInfo("German DAX 30 Index"         , "{0:N0} indices"));
				Add("HK50"   , new SymbolInfo("Chinese Hang Seng 50 Index"  , "{0:N0} indices"));
				Add("IT40"   , new SymbolInfo("Italian FTSE MIB Index"      , "{0:N0} indices"));
				Add("JPN225" , new SymbolInfo("Japan Nikkei 225 Index"      , "{0:N0} indices"));
				Add("SPA35"  , new SymbolInfo("Spanish IBEX 35 Index"       , "{0:N0} indices"));
				Add("UK100"  , new SymbolInfo("UK 100 Index"                , "{0:N0} indices"));
				Add("US2000" , new SymbolInfo("USA Russell 2000 Index"      , "{0:N0} indices"));
				Add("US500"  , new SymbolInfo("USA S&P 500 Index"           , "{0:N0} indices"));
				Add("NAS100" , new SymbolInfo("USA NASDAQ 100 Index"        , "{0:N0} indices"));
				Add("US30"   , new SymbolInfo("USA Dow Jones IA Index"      , "{0:N0} indices"));
				Add("CN50"   , new SymbolInfo("China A50 Index"             , "{0:N0} indices"));
				Add("USDX"   , new SymbolInfo("US Dollar Index"             , "{0:N0} indices"));
			}

			/// <summary>Get the description of a known symbol</summary>
			public new SymbolInfo this[string key]
			{
				get { SymbolInfo info; return TryGetValue(key, out info) ? info : new SymbolInfo(key, "{0}"); }
			}
		}

		/// <summary>Round a base currency price value to the nearest pip</summary>
		public static double RoundToNearestPip(double price, PriceData pd)
		{
			return Math.Round(price / pd.PipSize, MidpointRounding.AwayFromZero) * pd.PipSize;
		}

		/// <summary>Convert a price in base currency to account currency</summary>
		public static double BaseToAcctCurrency(double price, PriceData pd)
		{
			return price * pd.PipValue / pd.PipSize;
		}

		/// <summary>Convert a price in account currency to base currency</summary>
		public static double AcctToBaseCurrency(double price, PriceData pd)
		{
			return price * pd.PipSize / pd.PipValue;
		}
	}

	public class SymbolInfo
	{
		public SymbolInfo(string desc, string fmt_string)
		{
			Desc      = desc;
			FmtString = fmt_string;
		}

		/// <summary>A longer description of an instrument</summary>
		public string Desc { get; private set; }

		/// <summary>A format string to use for displaying the symbol</summary>
		public string FmtString { get; private set; }
	}
}
