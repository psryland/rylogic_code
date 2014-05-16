//***************************************************
// Random Extensions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;

namespace pr.extn
{
	public static class RandomExtensions
	{
		/// <summary>Return a random number within a range [min, max)</summary>
		public static int NextRange(this Random r, int min, int max)
		{
			return r.Next(min, max);
		}

		/// <summary>Return a random number within a range centred on 'centre' with radius 'radius'</summary>
		public static int NextCentred(this Random r, int centre, int radius)
		{
			return r.Next(centre - radius, centre + radius);
		}

		/// <summary>Return a random number within a range [min, max)</summary>
		public static double NextDoubleRange(this Random r, double min, double max)
		{
			return min + (max - min) * r.NextDouble();
		}

		/// <summary>Return a random number within a range centred on 'centre' with radius 'radius'</summary>
		public static double NextDoubleCentred(this Random r, double centre, double radius)
		{
			return centre + radius * (2.0 * r.NextDouble() - 1.0);
		}
	}
}
