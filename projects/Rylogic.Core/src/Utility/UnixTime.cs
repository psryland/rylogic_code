using System;
using System.Diagnostics;

namespace Rylogic.Utility
{
	/// <summary>Unix milliseconds since 1/1/1970</summary>
	[DebuggerDisplay("{DateTime}")]
	public struct UnixMSec
	{
		public UnixMSec(long msec)
		{
			Value = msec;
		}

		/// <summary>The time in Unix milliseconds</summary>
		public long Value { get; set; }

		/// <summary>The value in DateTimeOffset Ticks</summary>
		public long Ticks => DateTime.Ticks;

		/// <summary>The value as a DateTimeOffset</summary>
		public DateTimeOffset DateTime => DateTimeOffset.FromUnixTimeMilliseconds(Value);

		public static explicit operator UnixMSec(long d) { return new UnixMSec(d); }
		public static implicit operator long(UnixMSec d) { return d.Value; }
		public static implicit operator UnixMSec(UnixSec d) { return new UnixMSec(d.Value * 1000); }
		public static implicit operator DateTimeOffset(UnixMSec d) { return DateTimeOffset.FromUnixTimeMilliseconds(d.Value); }
		public static implicit operator UnixMSec(DateTimeOffset d) { return new UnixMSec(d.ToUnixTimeMilliseconds()); }
	}

	/// <summary>Unix seconds since 1/1/1970</summary>
	[DebuggerDisplay("{DateTime}")]
	public struct UnixSec
	{
		public UnixSec(long sec)
		{
			Value = sec;
		}

		/// <summary>The time in Unix seconds</summary>
		public long Value { get; set; }

		/// <summary>The value in DateTimeOffset Ticks</summary>
		public long Ticks => DateTime.Ticks;

		/// <summary>The value as a DateTimeOffset</summary>
		public DateTimeOffset DateTime => DateTimeOffset.FromUnixTimeSeconds(Value);

		public static explicit operator UnixSec(long d) { return new UnixSec(d); }
		public static implicit operator long(UnixSec d) { return d.Value; }
		public static implicit operator UnixSec(UnixMSec d) { return new UnixSec(d.Value / 1000); }
		public static implicit operator DateTimeOffset(UnixSec d) { return DateTimeOffset.FromUnixTimeSeconds(d.Value); }
		public static implicit operator UnixSec(DateTimeOffset d) { return new UnixSec(d.ToUnixTimeSeconds()); }
	}
}
