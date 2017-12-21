//***************************************************
// Colour128
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Drawing;

namespace Rylogic.Gfx
{
	public struct Colour128
	{
		public float A;
		public float R;
		public float G;
		public float B;

		public Colour128(float a, float r, float g, float b) { A=a; R=r; G=g; B=b; }
		public uint ToUint()				{ return (uint)((sat(A*255) << 24) | (sat(R*255) << 16) | (sat(G*255) << 8) | sat(B*255)); }
		public Color ToColor()				{ return Color.FromArgb(sat(A*255),sat(R*255),sat(G*255),sat(B*255)); }
		public Colour32 ToColour32()		{ return new Colour32((byte)(A*0xff), (byte)(R*0xff), (byte)(G*0xff), (byte)(B*0xff)); }
		public override string ToString()	{ return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}",A,R,G,B); }

		public Colour128 Saturate()			{ return new Colour128(satf(A), satf(R), satf(G), satf(B)); }

		// Operators
		public static Colour128 operator + (Colour128 lhs, Colour128 rhs)	{ return new Colour128(lhs.A+rhs.A, lhs.R+rhs.R, lhs.G+rhs.G, lhs.B+rhs.B); }
		public static Colour128 operator - (Colour128 lhs, Colour128 rhs)	{ return new Colour128(lhs.A-rhs.A, lhs.R-rhs.R, lhs.G-rhs.G, lhs.B-rhs.B); }

		private static float satf(float x)	{ return Math.Max(0f, Math.Min(1f, x)); }
		private static byte  sat(float x)	{ return (byte)Math.Max(0f, Math.Min(255f, x)); }
	}
}
