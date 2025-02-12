﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExchApi.Common
{
	public static class Misc
	{
		/// <summary></summary>
		public static readonly string AssemblyVersionString = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? throw new Exception("Assembly Version not found");

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

			var buf = new byte[str.Length / 2];
			for (int i = 0; i != buf.Length; ++i)
				buf[i] = byte.Parse(str.Substring(2 * i, 2), NumberStyles.HexNumber);
			return buf;
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
		private static long m_nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
		private static object m_nonce_lock = new();

		/// <summary>Assert for testing the thread id</summary>
		public static bool AssertMainThread()
		{
			if (m_main_thread_id == null) m_main_thread_id = Thread.CurrentThread.ManagedThreadId;
			if (m_main_thread_id == Thread.CurrentThread.ManagedThreadId) return true;
			Debugger.Break();
			return true;
		}
		private static int? m_main_thread_id;

		/// <summary>Send a json encoded text message</summary>
		public static async Task SendAsync(this ClientWebSocket ws, string json, CancellationToken cancel)
		{
			Trace.WriteLine($"Send: {json}");
			var data = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
			await ws.SendAsync(data, WebSocketMessageType.Text, true, cancel);
		}
	}

	public class Params :Dictionary<string, object>{}
}
