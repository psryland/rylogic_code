//***************************************************
// Random Extensions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using pr.maths;

namespace pr.extn
{
	public static class Random_
	{
		#region Booleans

		/// <summary>Return a random boolean</summary>
		public static bool Bool(this Random r)
		{
			return (r.Next() % 2) != 0;
		}

		/// <summary>An endless stream of random booleans</summary>
		public static IEnumerable<bool> Bools(this Random r)
		{
			for (;;) yield return r.Bool();
		}

		#endregion

		#region Bytes

		/// <summary>Return a random byte</summary>
		public static byte Byte(this Random r)
		{
			return (byte)r.Next(0,256);
		}

		/// <summary>An endless stream of random bytes</summary>
		public static IEnumerable<byte> Bytes(this Random r)
		{
			for (;;) yield return r.Byte();
		}

		#endregion

		#region Ints

		/// <summary>Return a random number within a range [0, int.MaxValue)</summary>
		public static int Int(this Random r)
		{
			return r.Next();
		}

		/// <summary>Returns a random integer on [min,max)</summary>
		public static int Int(this Random r, int min, int max)
		{
			return r.Next(min, max);
		}

		/// <summary>Return a random number within a range centred on 'centre' with radius 'radius'</summary>
		public static int IntC(this Random r, int centre, int radius)
		{
			return r.Int(centre - radius, centre + radius);
		}

		/// <summary>An endless stream of random numbers</summary>
		public static IEnumerable<int> Ints(this Random r)
		{
			for (;;) yield return r.Int();
		}

		/// <summary>An endless stream of random numbers within the range [min,max)</summary>
		public static IEnumerable<int> Ints(this Random r, int min, int max)
		{
			for (;;) yield return r.Int(min, max);
		}

		/// <summary>An endless stream of random numbers centred on 'centre' with radius 'radius'</summary>
		public static IEnumerable<int> IntsC(this Random r, int centre, int radius)
		{
			for (;;) yield return r.IntC(centre, radius);
		}

		#endregion

		#region Floats

		/// <summary>Returns a random number on [min,max)</summary>
		public static float Float(this Random r)
		{
			return (float)r.Double();
		}

		/// <summary>Returns a random number on [min,max)</summary>
		public static float Float(this Random r, float min, float max)
		{
			return (float)r.Double(min, max);
		}
		
		/// <summary>Return a random number within a range centred on 'centre' with radius 'radius'</summary>
		public static float FloatC(this Random r, float centre, float radius)
		{
			return (float)r.DoubleC(centre, radius);
		}

		/// <summary>An endless stream of random floats</summary>
		public static IEnumerable<float> Floats(this Random r)
		{
			for (;;) yield return r.Float();
		}

		/// <summary>An endless stream of random doubles within the range [min,max)</summary>
		public static IEnumerable<float> Floats(this Random r, float min, float max)
		{
			for (;;) yield return r.Float(min, max);
		}

		/// <summary>An endless stream of random doubles on [centre - radius, centre + radius)</summary>
		public static IEnumerable<float> FloatsC(this Random r, float centre, float radius)
		{
			for (;;) yield return r.FloatC(centre, radius);
		}

		#endregion

		#region Doubles

		/// <summary>Return a random number within a range [0.0, 1.0)</summary>
		public static double Double(this Random r)
		{
			return r.NextDouble();
		}

		/// <summary>Return a random number within a range [min, max)</summary>
		public static double Double(this Random r, double min, double max)
		{
			return min + (max - min) * r.NextDouble();
		}

		/// <summary>Return a random number within a range centred on 'centre' with radius 'radius'</summary>
		public static double DoubleC(this Random r, double centre, double radius)
		{
			return centre + radius * (2.0 * r.NextDouble() - 1.0);
		}

		/// <summary>An endless stream of random doubles</summary>
		public static IEnumerable<double> Doubles(this Random r)
		{
			for (;;) yield return r.Double();
		}

		/// <summary>An endless stream of random doubles within the range [min,max)</summary>
		public static IEnumerable<double> Doubles(this Random r, double min, double max)
		{
			for (;;) yield return r.Double(min, max);
		}

		/// <summary>An endless stream of random doubles on [centre - radius, centre + radius)</summary>
		public static IEnumerable<double> DoublesC(this Random r, double centre, double radius)
		{
			for (;;) yield return r.DoubleC(centre, radius);
		}

		#endregion

		#region Gaussian 

		/// <summary>Return a random double with Gaussian distribution</summary>
		public static double GaussianDouble(this Random rng, double mean = 0.0, double std_dev = 1.0)
		{
			// Use a 'Box-Muller' transformation to get Gaussian distribution

			// Uniformly distributed random doubles on (0,1]
			var u1 = 1.0 - rng.NextDouble();
			var u2 = 1.0 - rng.NextDouble();

			// Gaussian distribution with mean == 0, standard deviation = 1
			var std_norm = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(Maths.Tau * u2);

			// Offset and scaled
			return mean + std_dev * std_norm;
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
