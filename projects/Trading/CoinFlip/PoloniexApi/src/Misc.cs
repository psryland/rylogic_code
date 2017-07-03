using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Poloniex.API
{
	/// <summary>Represents the type of an order.</summary>
	public enum EOrderType
	{
		Buy,
		Sell
	}

	/// <summary>Represents a time frame of a market.</summary>
	public enum MarketPeriod
	{
		/// <summary>A time interval of 5 minutes.</summary>
		Minutes5 = 300,

		/// <summary>A time interval of 15 minutes.</summary>
		Minutes15 = 900,

		/// <summary>A time interval of 30 minutes.</summary>
		Minutes30 = 1800,

		/// <summary>A time interval of 2 hours.</summary>
		Hours2 = 7200,

		/// <summary>A time interval of 4 hours.</summary>
		Hours4 = 14400,

		/// <summary>A time interval of a day.</summary>
		Day = 86400
	}

	/// <summary>Global functions</summary>
	internal static class Misc
	{
		/// <summary></summary>
		public static readonly DateTimeOffset UnixEpochStart = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);

		/// <summary></summary>
		public static readonly string AssemblyVersionString = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

		/// <summary>Convert a timestamp to Unix time</summary>
		public static ulong ToUnixTime(DateTimeOffset dt)
		{
			return (ulong)Math.Floor(dt.Subtract(UnixEpochStart).TotalSeconds);
		}

		/// <summary>Convert a Unix time to a date time offset</summary>
		public static DateTimeOffset ToDateTimeOffset(ulong unix_time)
		{
			return UnixEpochStart.AddSeconds(unix_time);
		}

		/// <summary></summary>
		public static DateTimeOffset ParseDateTime(string dt)
		{
			return DateTimeOffset.ParseExact(dt, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}

		/// <summary>Parse the buy/sell string</summary>
		public static EOrderType ToOrderType(string value)
		{
			switch (value) {
			default: throw new ArgumentOutOfRangeException("value");
			case "buy": return EOrderType.Buy;
			case "bid": return EOrderType.Buy;
			case "sell": return EOrderType.Sell;
			case "ask": return EOrderType.Sell;
			}
		}
		public static string ToString(EOrderType order_type)
		{
			switch (order_type) {
			default: throw new ArgumentException("order_type");
			case EOrderType.Buy: return "buy";
			case EOrderType.Sell: return "sell";
			}
		}

		/// <summary>Convert a byte array to a Hex character string</summary>
		public static string ToStringHex(byte[] value)
		{
			var output = new StringBuilder();
			foreach (var b in value)
				output.Append(b.ToString("x2", CultureInfo.InvariantCulture));

			return output.ToString();
		}

		/// <summary>Convert a string of bytes (as hex character pairs) to a buffer of bytes</summary>
		public static byte[] FromStringHex(string str)
		{
			Debug.Assert((str.Length % 2) == 0);

			var buf = new byte[str.Length/2];
			for (int i = 0; i != buf.Length; ++i)
				buf[i] = byte.Parse(str.Substring(2*i,2), NumberStyles.HexNumber);
			return buf;
		}
	}
}
