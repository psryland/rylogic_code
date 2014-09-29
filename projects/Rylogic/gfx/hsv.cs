//***************************************************
// Colour32
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using pr.maths;

namespace pr.gfx
{
	// The Hue/Saturation/Value model was created by A. R. Smith in 1978.
	// It is based on such intuitive color characteristics as tint, shade and tone (or family, purity and intensity).
	// The coordinate system is cylindrical, and the colours are defined inside a hex cone.
	// The hue value H runs from 0 to 360°. The saturation S is the degree of strength or purity and is from 0 to 1.
	// Purity is how much white is added to the colour, so S=1 makes the purest color (no white).
	// Brightness V also ranges from 0 to 1, where 0 is the black.

	[StructLayout(LayoutKind.Sequential)]
	public struct HSV
	{
		public float A; // alpha       [0,1]
		public float H; // hue         [0,1] corresponding to the range 0 to Tau
		public float S; // saturation  [0,1] S = 0 is all white, S = 1 is pure colour
		public float V; // value       [0,1] aka brightness, 0 = black

		public HSV(float a, float h, float s, float v)
		{
			Debug.Assert(
				a >= 0f && a <= 1f &&
				h >= 0f && h <= 1f &&
				s >= 0f && s <= 1f &&
				v >= 0f && v <= 1f
				,"Expected component colour values in the range [0,1]");
			A = a;
			H = h;
			S = s;
			V = v;
		}

		/// <summary>ToString</summary>
		public override string ToString() { return string.Format("a={0} h={1} s={2} v={3}", A, H, S, V); }

		/// <summary>Indicates whether this instance and a specified object are equal.</summary>
		public override bool Equals(object obj)
		{
			return obj is HSV && Equals((HSV)obj);
		}
		public bool Equals(HSV other)
		{
			// If saturation is 0 for both, then H doesn't matter
			return !S.Equals(0)
				? A.Equals(other.A) && H.Equals(other.H) && S.Equals(other.S) && V.Equals(other.V)
				: A.Equals(other.A) && V.Equals(other.V) && other.S.Equals(0);
		}

		/// <summary>Returns the hash code for this instance.</summary>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash_code = A.GetHashCode();
				hash_code = (hash_code*397) ^ H.GetHashCode();
				hash_code = (hash_code*397) ^ S.GetHashCode();
				hash_code = (hash_code*397) ^ V.GetHashCode();
				return hash_code;
			}
		}

		/// <summary>Convert this HSV to RGB</summary>
		public Color ToColor()
		{
			return ToColor(this);
		}

		/// <summary>Create an HSV from components</summary>
		public static HSV FromAHSV(float a, float h, float s, float v)
		{
			return new HSV(a,h,s,v);
		}

		/// <summary>Set Hue and Saturation from a 2d vector</summary>
		public static HSV FromRadial(float x, float y, float v, float a)
		{
			float h,s;
			var len = (float)Math.Sqrt(x*x + y*y);
			if (Math.Abs(len - 0) < float.Epsilon)
			{
				h = 0f;
				s = 0f;
			}
			else
			{
				float nx = x / len;
				float ny = y / len;
				s = Math.Min(len, 1f);
				h = (float)((Math.Atan2(-ny, -nx) + Maths.TauBy2) / Maths.Tau);
			}
			return new HSV(a, h, s, v);
		}

		/// <summary>Return the HSV colour as a standard ARGB colour</summary>
		public static Color ToColor(float a, float h, float s, float v)
		{
			Debug.Assert(
				a >= 0f && a <= 1f &&
				h >= 0f && h <= 1f &&
				s >= 0f && s <= 1f &&
				v >= 0f && v <= 1f
				,"Expected component colour values in the range [0,1]");

			// Saturation == 0f means 'sec' is undefined
			if (Math.Abs(s) < float.Epsilon)
			{
				var i = (int)Maths.Clamp(v * 255, 0, 255);
				return Color.FromArgb(i,i,i);
			}

			// Hue is divided into 6 sectors: red -> magenta -> blue -> cyan -> green -> yellow -> red
			// Find the sector, and the fraction position within the sector
			var f = Maths.Clamp(h * 6f, 0f, 5.99999f);
			var sec = (int)Math.Floor(f);
			f -= sec;

			// Calculate values for the three axes of the colour.
			var p = v * (1 - s);
			var q = v * (1 - (s * f));
			var t = v * (1 - (s * (1 - f)));

			// Assign the fractional colours to r,g,b
			double r,g,b;
			switch (sec)
			{
			default: throw new ArgumentOutOfRangeException("h","Hue sector out of range");
			case 0: r = v; g = t; b = p; break;
			case 1: r = q; g = v; b = p; break;
			case 2: r = p; g = v; b = t; break;
			case 3: r = p; g = q; b = v; break;
			case 4: r = t; g = p; b = v; break;
			case 5: r = v; g = p; b = q; break;
			}
			return Color.FromArgb((int)(a * 255), (int)(r * 255), (int)(g * 255), (int)(b * 255));
		}

		/// <summary>Return the HSV colour as a standard ARGB colour</summary>
		public static Color ToColor(HSV hsv)
		{
			return ToColor(hsv.A, hsv.H, hsv.S, hsv.V);
		}

		/// <summary>
		/// Return an HSV colour from a standard ARGB colour.
		/// 'undef_h' is the value to use for h when it would otherwise be undefined.
		/// Use previous value in order to preserve it through the singular points.</summary>
		public static HSV FromColor(Color rgb, float undef_h = 0f)
		{
			var a = rgb.A / 255f;
			var r = rgb.R / 255f;
			var g = rgb.G / 255f;
			var b = rgb.B / 255f;

			var min = Math.Min(r, Math.Min(g, b));
			var max = Math.Max(r, Math.Max(g, b));
			var delta = max - min;

			// If r = g = b, => S = 0, H is technically undefined, V == r,g,b
			if (Math.Abs(delta) < float.Epsilon)
				return new HSV(a, undef_h, 0f, max);

			HSV hsv;
			hsv.A = a;
			hsv.S = delta / max;
			hsv.V = max;
			if      (r == max) hsv.H = 0f + (g - b) / delta; // between yellow & magenta
			else if (g == max) hsv.H = 2f + (b - r) / delta; // between cyan & yellow
			else               hsv.H = 4f + (r - g) / delta; // between magenta & cyan
			if (hsv.H < 0) hsv.H += 6f;
			hsv.H /= 6f;
			return hsv;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using extn;

	[TestFixture] public class TestHSV
	{
		private static bool Equal(Color lhs, Color rhs)
		{
			return
				lhs.A == rhs.A &&
				lhs.R == rhs.R &&
				lhs.G == rhs.G &&
				lhs.B == rhs.B;
		}

		[Test] public void Rgb2Hsv2Rgb()
		{
			Assert.True(Equal(Color.White, Color.White.ToHSV().ToColor()));
			Assert.True(Equal(Color.Black, Color.Black.ToHSV().ToColor()));
			Assert.True(Equal(Color.Red  , Color.Red  .ToHSV().ToColor()));
			Assert.True(Equal(Color.Green, Color.Green.ToHSV().ToColor()));
			Assert.True(Equal(Color.Blue , Color.Blue .ToHSV().ToColor()));
		}
	}
}
#endif