using System;

namespace pr.extn
{
	public static class DateTimeExtensions
	{
		/// <summary>Returns a new DateTimeOffset object clamped to within the given range</summary>
		public static DateTime Clamp(this DateTime time, DateTime min, DateTime max)
		{
			if (time < min) return min;
			if (time > max) return max;
			return time;
		}
		/// <summary>Returns a new DateTimeOffset object clamped to within the given range</summary>
		public static DateTimeOffset Clamp(this DateTimeOffset time, DateTimeOffset min, DateTimeOffset max)
		{
			if (time < min) return min;
			if (time > max) return max;
			return time;
		}
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using extn;

	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestDateTimeExtensions
		{
			[Test] public static void TestClamp()
			{
				var min = new DateTimeOffset(2000,12,1,0,0,0,TimeSpan.Zero);
				var max = new DateTimeOffset(2000,12,31,0,0,0,TimeSpan.Zero);
				DateTimeOffset dt,DT;

				dt = new DateTimeOffset(2000,11,29,0,0,0,TimeSpan.Zero);
				DT = dt.Clamp(min,max);
				Assert.IsTrue(!Equals(dt,DT) && dt < min);
				Assert.IsTrue(DT >= min && DT <= max);

				dt = new DateTimeOffset(2000,12,20,0,0,0,TimeSpan.Zero);
				DT = dt.Clamp(min,max);
				Assert.IsTrue(Equals(dt,DT));
				Assert.IsTrue(DT >= min && DT <= max);

				dt = new DateTimeOffset(2000,12,31,23,59,59,TimeSpan.Zero);
				DT = dt.Clamp(min,max);
				Assert.IsTrue(!dt.Equals(DT) && dt > max);
				Assert.IsTrue(DT >= min && DT <= max);
			}
		}
	}
}
#endif