using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API
{
	/// <summary>Represents the type of an order.</summary>
	public enum EOrderType
	{
		/// <summary>A.k.a Q2B, Bid, Short</summary>
		Buy,

		/// <summary>A.k.a B2Q, Ask, Long</summary>
		Sell,
	}
	public enum EExchOrderType
	{
		Market,
		Limit,
		Stop,
		TrailingStop,
		Fok,
		ExchangeMarket,
		ExchangeLimit,
		ExchangeStop,
		ExchangeTrailingStop,
		ExchangeFok,
	}
	public enum EMarketTimeFrames
	{
		None,
		Minute1,
		Minute5,
		Minute15,
		Minute30,
		Hour1,
		Hour3,
		Hour6,
		Hour12,
		Day1,
		Week1,
		Week2,
		Month1,
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
			return ToUnixTime(dt.Ticks);
		}
		public static ulong ToUnixTime(long ticks)
		{
			var unix_ticks = Math.Max(0L, ticks - UnixEpochStart.Ticks);
			return (ulong)TimeSpan.FromTicks(unix_ticks).TotalSeconds;
		}

		/// <summary>Convert a Unix time to a date time offset</summary>
		public static DateTimeOffset ToDateTimeOffset(ulong unix_time_in_seconds)
		{
			return UnixEpochStart.AddSeconds(unix_time_in_seconds);
		}

		/// <summary></summary>
		public static DateTimeOffset ParseDateTime(string dt)
		{
			return DateTimeOffset.ParseExact(dt, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}

		/// <summary>Parse the buy/sell string</summary>
		public static EOrderType ToOrderType(string value)
		{
			switch (value.ToLowerInvariant()) {
			default: throw new ArgumentOutOfRangeException("value");
			case "limit_buy":  return EOrderType.Buy;
			case "buylimit":   return EOrderType.Buy;
			case "limit_sell": return EOrderType.Sell;
			case "selllimit":  return EOrderType.Sell;
			}
		}
		public static string ToString(EOrderType order_type)
		{
			switch (order_type) {
			default: throw new ArgumentException("order_type");
			case EOrderType.Buy: return "buylimit";
			case EOrderType.Sell: return "selllimit";
			}
		}

		/// <summary>Parse an exchange order type</summary>
		public static EExchOrderType? ToExchOrderType(string value)
		{
			if (value == null) return null;
			switch (value.ToUpperInvariant())
			{
			default: throw new Exception("Unknown exchange order type");
			case "MARKET":                 return EExchOrderType.Market;
			case "LIMIT":                  return EExchOrderType.Limit;
			case "STOP":                   return EExchOrderType.Stop;
			case "TRAILING STOP":          return EExchOrderType.TrailingStop;
			case "FOK":                    return EExchOrderType.Fok;
			case "EXCHANGE MARKET":        return EExchOrderType.ExchangeMarket;
			case "EXCHANGE LIMIT":         return EExchOrderType.ExchangeLimit;
			case "EXCHANGE STOP":          return EExchOrderType.ExchangeStop;
			case "EXCHANGE TRAILING STOP": return EExchOrderType.ExchangeTrailingStop;
			case "EXCHANGE FOK":           return EExchOrderType.ExchangeFok;
			}
		}

		/// <summary>Parse an order status string</summary>
		public static EOrderStatus? ToOrderStatus(string value)
		{
			if (value == null) return null;
			switch (value.ToUpperInvariant())
			{
			default: throw new Exception($"Unknown order status: {value}");
			case "ACTIVE":           return EOrderStatus.Active;
			case "EXECUTED":         return EOrderStatus.Executed;
			case "PARTIALLY FILLED": return EOrderStatus.PartiallyFilled;
			case "CANCELLED":        return EOrderStatus.Cancelled;
			}
		}

		/// <summary>Convert a time frame to the string to use in a request</summary>
		public static string ToRequestString(EMarketTimeFrames tf)
		{
			switch (tf)
			{
			default: throw new Exception($"Unknown time frame value: {tf}");
			case EMarketTimeFrames.None:     return "";
			case EMarketTimeFrames.Minute1:  return "1m";
			case EMarketTimeFrames.Minute5:  return "5m";
			case EMarketTimeFrames.Minute15: return "15m";
			case EMarketTimeFrames.Minute30: return "30m";
			case EMarketTimeFrames.Hour1:    return "1h";
			case EMarketTimeFrames.Hour3:    return "3h";
			case EMarketTimeFrames.Hour6:    return "6h";
			case EMarketTimeFrames.Hour12:   return "12h";
			case EMarketTimeFrames.Day1:     return "1D";
			case EMarketTimeFrames.Week1:    return "7D";
			case EMarketTimeFrames.Week2:    return "14D";
			case EMarketTimeFrames.Month1:   return "1M";
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

		/// <summary>Encode 'kv' into a URL parameter list, starting with '?'</summary>
		public static string UrlEncode(IEnumerable<KV> parameters)
		{
			var s = new StringBuilder();
			foreach (var kv in parameters)
			{
				var value = (kv.Value as string)?.Replace(' ','+') ?? kv.Value.ToString();
				s.Append("&").Append(kv.Key).Append("=").Append(value);
			}
			if (s.Length != 0) s[0] = '?';
			return s.ToString();
		}

		/// <summary>Encode 'kv' into a JSon string</summary>
		public static string JsonEncode(IEnumerable<KV> parameters)
		{
			var s = new JObject();
			foreach (var kv in parameters)
			{
				s.Add(new JProperty(kv.Key, kv.Value));
			}
			return s.ToString(Formatting.None);
		}
		public static string JsonEncode(params KV[] parameters)
		{
			return JsonEncode((IEnumerable<KV>)parameters);
		}

		/// <summary>Helper for generating "nonce"</summary>
		public static string Nonce
		{
			get
			{
				lock (m_nonce_lock)
				{
					var nonce = DateTimeOffset.UtcNow.Ticks;
					m_nonce = Math.Max(m_nonce + 1, nonce);
					return m_nonce.ToString(CultureInfo.InvariantCulture);
				}
			}
		}
		private static long m_nonce = (long)((DateTimeOffset.UtcNow - UnixEpochStart).TotalMilliseconds * 1000);
		private static object m_nonce_lock = new object();

		/// <summary>Returns an RAII scope that temporarily sets the current sync context to null</summary>
		public static Scope NoSyncContext()
		{
			return Scope.Create(
				() =>
				{
					var context = SynchronizationContext.Current;
					SynchronizationContext.SetSynchronizationContext(null);
					return context;
				},
				sc =>
				{
					SynchronizationContext.SetSynchronizationContext(sc);
				});
		}

		/// <summary>RAII scope lock this semaphore</summary>
		public static Scope Lock(this SemaphoreSlim ss, CancellationToken cancel)
		{
			return Scope.Create(
				() => ss.Wait(cancel),
				() => ss.Release());
		}

		/// <summary>Send a json encoded text message</summary>
		public static async Task SendAsync(this ClientWebSocket ws, string json, CancellationToken cancel)
		{
			Trace.WriteLine($"Send: {json}");
			var data = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
			await ws.SendAsync(data, WebSocketMessageType.Text, true, cancel);
		}

		/// <summary>Assert for testing the thread id</summary>
		public static bool AssertMainThread()
		{
			if (m_main_thread_id == null) m_main_thread_id = Thread.CurrentThread.ManagedThreadId;
			if (m_main_thread_id == Thread.CurrentThread.ManagedThreadId) return true;
			Debugger.Break();
			return true;
		}
		private static int? m_main_thread_id;
	}

	/// <summary>Helper for passing Key/Value pair parameters</summary>
	internal struct KV
	{
		[DebuggerStepThrough] public KV(string key, object value)
		{
			Key = key;
			Value = value;
		}
		public string Key;
		public object Value;
	}
}
