using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cryptopia.API
{
	/// <summary>Represents the type of an order.</summary>
	public enum EOrderType
	{
		/// <summary>A.k.a Q2B, Bid, Short</summary>
		Buy,

		/// <summary>A.k.a B2Q, Ask, Long</summary>
		Sell,
	}

	/// <summary>Trade Cancel Type</summary>
	public enum ECancelTradeType
	{
		/// <summary>Single open order cancel</summary>
		Trade,

		/// <summary>Cancel all open orders for trade pair</summary>
		TradePair,

		/// <summary>Cancel All open orders</summary>
		All
	}

	/// <summary>Account transaction types</summary>
	public enum ETransactionType
	{
		Deposit,
		Withdraw
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
