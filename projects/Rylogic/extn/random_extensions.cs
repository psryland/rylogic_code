//***************************************************
// Random Extensions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;

namespace pr.extn
{
	public static class RandomExtensions
	{
		#region Ints

		/// <summary>Return a random number within a range centred on 'centre' with radius 'radius'</summary>
		public static int NextCentred(this Random r, int centre, int radius)
		{
			return r.Next(centre - radius, centre + radius);
		}

		/// <summary>An endless stream of random numbers</summary>
		public static IEnumerable<int> Ints(this Random r)
		{
			for (;;) yield return r.Next();
		}

		/// <summary>An endless stream of random numbers within the range [min,max)</summary>
		public static IEnumerable<int> Ints(this Random r, int min, int max)
		{
			for (;;) yield return r.Next(min,max);
		}

		/// <summary>An endless stream of random numbers centred on 'centre' with radius 'radius'</summary>
		public static IEnumerable<int> IntsCentred(this Random r, int centre, int radius)
		{
			for (;;) yield return r.NextCentred(centre,radius);
		}

		#endregion

		#region Bytes

		/// <summary>Return a random byte</summary>
		public static byte NextByte(this Random r)
		{
			return (byte)r.Next(0,256);
		}

		/// <summary>Return a random byte</summary>
		public static IEnumerable<byte> Bytes(this Random r)
		{
			for (;;) yield return r.NextByte();
		}

		#endregion

		#region Doubles

		/// <summary>Return a random number within a range [min, max)</summary>
		public static double NextDouble(this Random r, double min, double max)
		{
			return min + (max - min) * r.NextDouble();
		}

		/// <summary>Return a random number within a range centred on 'centre' with radius 'radius'</summary>
		public static double NextDoubleCentred(this Random r, double centre, double radius)
		{
			return centre + radius * (2.0 * r.NextDouble() - 1.0);
		}

		/// <summary>An endless stream of random doubles</summary>
		public static IEnumerable<double> Doubles(this Random r)
		{
			for (;;) yield return r.NextDouble();
		}

		/// <summary>An endless stream of random numbers within the range [min,max)</summary>
		public static IEnumerable<double> Doubles(this Random r, double min, double max)
		{
			for (;;) yield return r.NextDouble(min, max);
		}

		/// <summary>An endless stream of random numbers within the range [min,max)</summary>
		public static IEnumerable<double> DoublesCentred(this Random r, double centre, double radius)
		{
			for (;;) yield return r.NextDoubleCentred(centre, radius);
		}

		#endregion
	}
}


#if PR_UNITTESTS
namespace pr.unittests
{
	using extn;
	using System.Linq;

	[TestFixture] public class TestRandomExtns
	{
		[Test] public void TestRandomNumberStream()
		{
			var r = new Random(123);
			var arr = r.Ints(0, 5).Take(5).ToArray();
			Assert.True(arr.SequenceEqual(new[]{4,4,3,4,3}));
		}
	}
}
#endif
