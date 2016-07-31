using System;
using System.Diagnostics;

namespace Tradee
{
	/// <summary>A time value in multiples of time frame units</summary>
	public struct TFTime
	{
		private readonly long m_ticks;
		private readonly ETimeFrame m_time_frame;

		public TFTime(long ticks, ETimeFrame tf)
		{
			m_ticks      = ticks;
			m_time_frame = tf;
		}
		public TFTime(double tf_units, ETimeFrame tf)
			:this(Misc.TimeFrameToTicks(tf_units, tf), tf)
		{}
		public TFTime(TFTime tft, ETimeFrame tf)
			:this(tft.m_ticks, tf)
		{}

		/// <summary>The exact time value in Ticks (not a multiple of the time frame units</summary>
		public long ExactTicks
		{
			get { return m_ticks; }
		}

		/// <summary>The time in ticks rounded down to an integer time frame unit</summary>
		public long IntgTicks
		{
			get
			{
				var quanta = Misc.TimeFrameToTicks(1.0, m_time_frame);
				return (m_ticks / quanta) * quanta;
			}
		}

		/// <summary>The exact time value in time frame units (includes fractional part)</summary>
		public double ExactTF
		{
			get { return Misc.TicksToTimeFrame(m_ticks, m_time_frame); }
		}

		/// <summary>The time value rounded down to the nearest integer time frame unit</summary>
		public double IntgTF
		{
			get { return Misc.TicksToTimeFrame(IntgTicks, m_time_frame); }
		}

		/// <summary>Return this time value in a different time frame</summary>
		public TFTime To(ETimeFrame tf)
		{
			return new TFTime(this, tf);
		}

		/// <summary></summary>
		public override string ToString()
		{
			return new DateTimeOffset(ExactTicks, TimeSpan.Zero).ToString();
		}

		#region Operators
		public static bool operator == (TFTime lhs, TFTime rhs)
		{
			return lhs.m_ticks == rhs.m_ticks;
		}
		public static bool operator != (TFTime lhs, TFTime rhs)
		{
			return !(lhs == rhs);
		}
		public static bool operator <  (TFTime lhs, TFTime rhs)
		{
			return lhs.m_ticks < rhs.m_ticks;
		}
		public static bool operator >  (TFTime lhs, TFTime rhs)
		{
			return lhs.m_ticks > rhs.m_ticks;
		}
		public static bool operator <= (TFTime lhs, TFTime rhs)
		{
			return lhs.m_ticks <= rhs.m_ticks;
		}
		public static bool operator >= (TFTime lhs, TFTime rhs)
		{
			return lhs.m_ticks >= rhs.m_ticks;
		}
		public static TFTime operator + (TFTime lhs, TFTime rhs)
		{
			Debug.Assert(lhs.m_time_frame == rhs.m_time_frame);
			return new TFTime(lhs.m_ticks + rhs.m_ticks, lhs.m_time_frame);
		}
		public static TFTime operator - (TFTime lhs, TFTime rhs)
		{
			Debug.Assert(lhs.m_time_frame == rhs.m_time_frame);
			return new TFTime(lhs.m_ticks - rhs.m_ticks, lhs.m_time_frame);
		}
		#endregion

		#region Equals
		public bool Equals(TFTime rhs)
		{
			return m_ticks == rhs.m_ticks && m_time_frame == rhs.m_time_frame;
		}
		public override bool Equals(object obj)
		{
			return obj is TFTime && Equals((TFTime)obj);
		}
		public override int GetHashCode()
		{
			return new { m_ticks, m_time_frame }.GetHashCode();
		}
		#endregion
	}
}
