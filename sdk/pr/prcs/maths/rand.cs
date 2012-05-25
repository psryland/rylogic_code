//***************************************************
// VectorFunctions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;

namespace pr.maths
{
	public class Rand :Random
	{
		public double Min;
		public double Max;

		public double Double()                       { return NextDouble() * (Max - Min) + Min; }
		public double Double(double min, double max) { return NextDouble() * (max - min) + min; }
		public float  Float()                        { return (float)Double(); } 
		public float  Float(float min, float max)    { return (float)Double(min, max); } 
		public int    Int()                          { return (int)Double(); }
		
		public Rand()
		{
			Min = 0.0f;
			Max = 1.0f;
		}
		public Rand(double min, double max)
		{
			Min = min;
			Max = max;
		}
	}
}