//***************************************************
// VectorFunctions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;

namespace pr.maths
{
	public class Rand :Random
	{
		/// <summary>The range on which to return random numbers [Min,Max)</summary>
		public double Min, Max;

		/// <summary>Return a random number on [Min,Max)</summary>
		public double Double() { return Double(Min,Max); }

		/// <summary>Return a random number on [min,max)</summary>
		public double Double(double min, double max) { return NextDouble() * (max - min) + min; }
		
		/// <summary>Returns a random number on [Min,Max)</summary>
		public float Float() { return Float((float)Min,(float)Max); } 
		
		/// <summary>Returns a random number on [min,max)</summary>
		public float Float(float min, float max) { return (float)Double(min, max); } 
		
		/// <summary>Returns a random number on [avr-d,avr+d)</summary>
		public float FloatC(float avr, float d) { return Float(avr - d, avr + d); }

		/// <summary>Returns a random integer on [(int)Min, (int)Max)</summary>
		public int Int() { return Int((int)Min, (int)Max); }

		/// <summary>Returns a random integer on [(int)min, (int)max)</summary>
		public int Int(int min, int max) { return (int)Double(min, max); }
		
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