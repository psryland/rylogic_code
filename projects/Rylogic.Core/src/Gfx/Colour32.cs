//***************************************************
// Colour32
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Rylogic.Gfx
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Colour32 :IComparable<Colour32> ,IComparable
	{
		public uint m_argb;

		public Colour32(uint argb)                          { m_argb = argb; }
		public Colour32(byte a, byte r, byte g, byte b)     :this(((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b) {}
		public Colour32(float a, float r, float g, float b) :this((byte)(0xFF * a), (byte)(0xFF * r), (byte)(0xFF * g), (byte)(0xFF * b)) {}
		public byte A                                       { get { return (byte)(m_argb >> 24); } set { m_argb = ((uint)value << 24) | (m_argb & 0x00FFFFFF); } }
		public byte R                                       { get { return (byte)(m_argb >> 16); } set { m_argb = ((uint)value << 16) | (m_argb & 0xFF00FFFF); } }
		public byte G                                       { get { return (byte)(m_argb >>  8); } set { m_argb = ((uint)value <<  8) | (m_argb & 0xFFFF00FF); } }
		public byte B                                       { get { return (byte)(m_argb >>  0); } set { m_argb = ((uint)value <<  0) | (m_argb & 0xFFFFFF00); } }
		public uint ARGB                                    { get { return m_argb; } }
		public Color ToColor()                              { return Color.FromArgb(A,R,G,B); }
		public Colour128 ToColour128()                      { return new Colour128(A/255f, R/255f, G/255f, B/255f); }
		public override string ToString()                   { return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}",A,R,G,B); }
		public static implicit operator uint(Colour32 col)  { return col.m_argb; }
		public static implicit operator Color(Colour32 col) { return Color.FromArgb(col.A,col.R,col.G,col.B); }
		public static implicit operator Colour32(Color col) { return new Colour32(unchecked((uint)col.ToArgb())); }
		public static implicit operator Colour32(uint col)  { return new Colour32(col); }

		// Operators
		public static Colour32 operator + (Colour32 lhs, Colour32 rhs)	{ return new Colour32(Sat(lhs.A+rhs.A), Sat(lhs.R+rhs.R), Sat(lhs.G+rhs.G), Sat(lhs.B+rhs.B)); }
		public static Colour32 operator - (Colour32 lhs, Colour32 rhs)	{ return new Colour32(Sat(lhs.A-rhs.A), Sat(lhs.R-rhs.R), Sat(lhs.G-rhs.G), Sat(lhs.B-rhs.B)); }

		// Constants
		public static Colour32 Zero      = new Colour32(0x00,0x00,0x00,0x00);
		public static Colour32 Black     = new Colour32(0xFF,0x00,0x00,0x00);
		public static Colour32 White     = new Colour32(0xFF,0xFF,0xFF,0xFF);
		public static Colour32 Red       = new Colour32(0xFF,0xFF,0x00,0x00);
		public static Colour32 Green     = new Colour32(0xFF,0x00,0xFF,0x00);
		public static Colour32 Blue      = new Colour32(0xFF,0x00,0x00,0xFF);
		public static Colour32 Yellow    = new Colour32(0xFF,0xFF,0xFF,0x00);
		public static Colour32 Turquoise = new Colour32(0xFF,0x00,0xFF,0xFF);
		public static Colour32 Purple    = new Colour32(0xFF,0xFF,0x00,0xFF);
		public static Colour32 Gray      = new Colour32(0xFF,0x80,0x80,0x80);

		/// <summary>Saturate 'i'</summary>
		private static byte Sat(int i)
		{
			return (byte)Math.Min(0xff, Math.Max(0, i));
		}

		/// <summary>Linearly interpolate two colours</summary>
		public Colour32 Lerp(Colour32 rhs, float t)
		{
			return Lerp(this, rhs, t);
		}
		public static Colour32 Lerp(Colour32 lhs, Colour32 rhs, float t)
		{
			return new Colour32(
				(byte)(lhs.A*(1f - t) + rhs.A*t),
				(byte)(lhs.R*(1f - t) + rhs.R*t),
				(byte)(lhs.G*(1f - t) + rhs.G*t),
				(byte)(lhs.B*(1f - t) + rhs.B*t));
		}

		/// <summary>Linearly interpolate the non-alpha channels of two colours (lhs.A is used)</summary>
		public Colour32 LerpNoAlpha(Colour32 rhs, float t)
		{
			return LerpNoAlpha(this, rhs, t);
		}
		public static Colour32 LerpNoAlpha(Colour32 lhs, Colour32 rhs, float t)
		{
			return new Colour32(
				lhs.A,
				(byte)(lhs.R*(1f - t) + rhs.R*t),
				(byte)(lhs.G*(1f - t) + rhs.G*t),
				(byte)(lhs.B*(1f - t) + rhs.B*t));
		}

		/// <summary>Lerp this colour toward black by 't'</summary>
		public Colour32 Darken(float t, bool alpha_too = false)
		{
			return alpha_too ? Lerp(Black, t) : LerpNoAlpha(Black, t);
		}

		/// <summary>Lerp this colour toward white by 't'</summary>
		public Colour32 Lighten(float t, bool alpha_too = false)
		{
			return alpha_too ? Lerp(White, t) : LerpNoAlpha(White, t);
		}

		/// <summary>Return this colour with the alpha value changed</summary>
		public Colour32 Alpha(byte alpha)
		{
			return new Colour32(((uint)alpha << 24) | (m_argb & 0x00FFFFFF));
		}
		public Colour32 Alpha(float alpha)
		{
			return Alpha((byte)(alpha * 0xFF));
		}

		#region Parse
		public static Colour32 Parse(string s, int radix = 16)
		{
			if (s == null) throw new ArgumentNullException("s", "Colour32.Parse() string argument was null");
			if (radix != 16 && radix != 10) throw new FormatException("Colour32.Parse() only supports radix = 10 or 16");
			return new Colour32(
				radix == 16 ? uint.Parse(s, NumberStyles.HexNumber) :
				radix == 10 ? uint.Parse(s, NumberStyles.Integer) :
				0U);
		}
		public static bool TryParse(string s, out Colour32 col, int radix = 16)
		{
			return
				radix == 16 ? uint.TryParse(s, NumberStyles.HexNumber, null, out col.m_argb) :
				radix == 10 ? uint.TryParse(s, NumberStyles.Integer, null, out col.m_argb) :
				uint.TryParse(s, out col.m_argb);
		}
		public static Colour32? TryParse(string s, int radix = 16)
		{
			Colour32 col;
			return TryParse(s, out col, radix) ? (Colour32?)col : null;
		}
		#endregion

		#region IComparible
		public int CompareTo(Colour32 rhs)
		{
			// Compare using RGBA so that the same colours with different alpha are adjacent
			var lhs = this;
			var l_rgb = ((lhs.m_argb << 8) & 0xFFFFFF00) | ((lhs.m_argb >> 24) & 0xFF);
			var r_rgb = ((rhs.m_argb << 8) & 0xFFFFFF00) | ((rhs.m_argb >> 24) & 0xFF);
			return l_rgb.CompareTo(r_rgb);
		}
		public int CompareTo(object rhs)
		{
			return CompareTo((Colour32)rhs);
		}
		#endregion
	}
}
