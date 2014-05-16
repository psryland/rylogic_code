//***************************************************
// Colour32
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace pr.gfx
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Colour32
	{
		public uint m_argb;

		public Colour32(uint argb)                          { m_argb = argb; }
		public Colour32(byte a, byte r, byte g, byte b)     :this(((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b) {}
		public Colour32(float a, float r, float g, float b) :this((byte)(0xFF * a), (byte)(0xFF * r), (byte)(0xFF * g), (byte)(0xFF * b)) {}
		public byte A                                       { get { return (byte)(m_argb >> 24); } set { m_argb = ((uint)value << 24) | (m_argb & 0x00FFFFFF); } }
		public byte R                                       { get { return (byte)(m_argb >> 16); } set { m_argb = ((uint)value << 16) | (m_argb & 0xFF00FFFF); } }
		public byte G                                       { get { return (byte)(m_argb >>  8); } set { m_argb = ((uint)value <<  8) | (m_argb & 0xFFFF00FF); } }
		public byte B                                       { get { return (byte)(m_argb >>  0); } set { m_argb = ((uint)value <<  0) | (m_argb & 0xFFFFFF00); } }
		public Color ToColor()                              { return Color.FromArgb(A,R,G,B); }
		public Colour128 ToColour128()                      { return new Colour128(A/255f, R/255f, G/255f, B/255f); }
		public override string ToString()                   { return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}",A,R,G,B); }
		public static implicit operator uint(Colour32 col)  { return col.m_argb; }

		// Operators
		public static Colour32 operator + (Colour32 lhs, Colour32 rhs)	{ return new Colour32(sat(lhs.A+rhs.A), sat(lhs.R+rhs.R), sat(lhs.G+rhs.G), sat(lhs.B+rhs.B)); }
		public static Colour32 operator - (Colour32 lhs, Colour32 rhs)	{ return new Colour32(sat(lhs.A-rhs.A), sat(lhs.R-rhs.R), sat(lhs.G-rhs.G), sat(lhs.B-rhs.B)); }

		// Constants
		public static Colour32 Zero  = new Colour32(0x00,0x00,0x00,0x00);
		public static Colour32 Black = new Colour32(0xFF,0x00,0x00,0x00);
		public static Colour32 White = new Colour32(0xFF,0xFF,0xFF,0xFF);
		public static Colour32 Red   = new Colour32(0xFF,0xFF,0x00,0x00);
		public static Colour32 Green = new Colour32(0xFF,0x00,0xFF,0x00);
		public static Colour32 Blue  = new Colour32(0xFF,0x00,0x00,0xFF);
		public static Colour32 Gray  = new Colour32(0xFF,0x80,0x80,0x80);
		
		private static byte sat(int i) { return (byte)Math.Min(0xff, Math.Max(0, i)); }
	
		/// <summary>Linearly interpolate two colours</summary>
		public static Colour32 Blend(Colour32 c0, Colour32 c1, float t)
		{
			return new Colour32(
				(byte)(c0.A*(1f - t) + c1.A*t),
				(byte)(c0.R*(1f - t) + c1.R*t),
				(byte)(c0.G*(1f - t) + c1.G*t),
				(byte)(c0.B*(1f - t) + c1.B*t));
		}
	}
}
