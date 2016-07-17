using System;
using System.Collections.Generic;

namespace Tradee
{
	[Serializable] public class HelloMsg
	{
		public string Msg { get; set; }
	}

	/// <summary>Message for passing a single candle</summary>
	[Serializable] public class Candle
	{
		public static readonly Candle Default = new Candle(string.Empty, 0, 0, 0, 0, 0, 0);
		public Candle(string symbol, long timestamp, double open, double high, double low, double close, double volume)
		{
			Symbol    = symbol;
			Timestamp = timestamp;
			Open      = open;
			High      = high;
			Low       = low;
			Close     = close;
			Volume    = volume;
		}

		/// <summary>The symbol that the data is for</summary>
		public string Symbol { get; set; }

		/// <summary>The timestamp values of the given data</summary>
		public long Timestamp { get; set; }

		/// <summary>The open price</summary>
		public double Open  { get; set; }

		/// <summary>The price high</summary>
		public double High  { get; set; }

		/// <summary>The price low</summary>
		public double Low   { get; set; }

		/// <summary>The price close</summary>
		public double Close { get; set; }

		/// <summary>The number of ticks in this candle</summary>
		public double Volume { get; set; }

		/// <summary>Friendly print</summary>
		public override string ToString()
		{
			return string.Format("(V{0}) - O({1}) H({2}) L({3}) C({4}) - TS({5})", Volume, Open, High, Low, Close, new DateTime(Timestamp, DateTimeKind.Utc).ToString());
		}
	}

	/// <summary>Message for passing a batch of candle data</summary>
	[Serializable] public class Candles
	{
		public Candles(string symbol, long[] timestamp, double[] open, double[] high, double[] low, double[] close, double[] volume)
		{
			Symbol    = symbol;
			Timestamp = timestamp;
			Open      = open;
			High      = high;
			Low       = low;
			Close     = close;
			Volume    = volume;
		}

		/// <summary>The symbol that the data is for</summary>
		public string Symbol { get; set; }

		/// <summary>The timestamp values of the given data</summary>
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

		/// <summary>Enumerate</summary>
		public IEnumerable<Candle> AllCandles
		{
			get
			{
				for (int i = 0; i != Timestamp.Length; ++i)
					yield return new Candle(Symbol, Timestamp[i], Open[i], High[i], Low[i], Close[i], Volume[i]);
			}
		}
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
