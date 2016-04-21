//***************************************************
// Bit operations
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pr.extn;
using pr.maths;
using pr.util;

namespace pr.maths
{
	/// <summary> Bit manipulation functions</summary>
	public static class Bit
	{
		public enum EState { Clear = 0, Set = 1, Toggle = 2 }

		/// <summary>Create a 32-bit mask from 1 &lt;&lt; n</summary>
		public static uint Bit32(int n) { return 1U << n; }

		/// <summary>Create a 64-bit mask from 1 &lt;&lt; n</summary>
		public static ulong Bit64(int n) { return 1UL << n; }

		/// <summary>Set/Clear bits in 'value'</summary>
		public static ulong SetBits(ulong value, ulong mask, bool state)
		{
			return state ? value | mask : value & ~mask;
		}
		public static long SetBits(long value, long mask, bool state)
		{
			return state ? value | mask : value & ~mask;
		}
		public static uint SetBits(uint value, uint mask, bool state)
		{
			return state ? value | mask : value & ~mask;
		}
		public static int SetBits(int value, int mask, bool state)
		{
			return state ? value | mask : value & ~mask;
		}
		public static T SetBits<T>(T value, T mask, bool state) where T :struct, IConvertible
		{
			System.Diagnostics.Debug.Assert(typeof(T).HasAttribute<FlagsAttribute>(), "Type {0} is not a flags enum".Fmt(typeof(T).Name));
			return (T)Enum.ToObject(typeof(T), SetBits(value.ToInt32(null), mask.ToInt32(null), state));
		}

		/// <summary>Returns true if 'value & mask' != 0</summary>
		public static bool AnySet(ulong value, ulong mask)
		{
			return (value & mask) != 0;
		}
		public static bool AnySet(long value, long mask)
		{
			return (value & mask) != 0;
		}
		public static bool AnySet(uint value, uint mask)
		{
			return (value & mask) != 0;
		}
		public static bool AnySet(int value, int mask)
		{
			return (value & mask) != 0;
		}
		public static bool AnySet(byte value, byte mask)
		{
			return (value & mask) != 0;
		}
		public static bool AnySet<T>(T value, T mask) where T :struct, IConvertible
		{
			System.Diagnostics.Debug.Assert(typeof(T).HasAttribute<FlagsAttribute>(), "Type {0} is not a flags enum".Fmt(typeof(T).Name));
			return AnySet(value.ToUInt64(null), mask.ToUInt64(null));
		}

		/// <summary>Returns true if 'value & mask' == mask</summary>
		public static bool AllSet(ulong value, ulong mask)
		{
			return (value & mask) == mask;
		}
		public static bool AllSet(long value, long mask)
		{
			return (value & mask) == mask;
		}
		public static bool AllSet(uint value, uint mask)
		{
			return (value & mask) == mask;
		}
		public static bool AllSet(int value, int mask)
		{
			return (value & mask) == mask;
		}
		public static bool AllSet(byte value, byte mask)
		{
			return (value & mask) == mask;
		}
		public static bool AllSet<T>(T value, T mask) where T :struct, IConvertible
		{
			System.Diagnostics.Debug.Assert(typeof(T).HasAttribute<FlagsAttribute>(), "Type {0} is not an enum with the [Flags] attribute".Fmt(typeof(T).Name));
			return AnySet(value.ToUInt64(null), mask.ToUInt64(null));
		}

		/// <summary>Iterate over the index positions of bits in a bit field</summary>
		public static IEnumerable<int> EnumBitIndices(int value)
		{
			for (var i = 0; value != 0; ++i, value >>= 1)
				if ((value&1) != 0) yield return i;
		}

		/// <summary>Iterate over the index positions of bits in a bit field</summary>
		public static IEnumerable<int> EnumBitIndices(uint value)
		{
			for (var i = 0; value != 0; ++i, value >>= 1)
				if ((value&1) != 0) yield return i;
		}

		/// <summary>Iterate over the set bits in a bit field</summary>
		public static IEnumerable<int> EnumBitMasks(int value)
		{
			for (int mask; value != 0; value -= mask)
				yield return mask = value & (value ^ (value - 1));
		}

		/// <summary>Iterate over the set bits in a bit field</summary>
		public static IEnumerable<uint> EnumBitMasks(uint value)
		{
			for (uint mask; value != 0; value -= mask)
				yield return mask = value & (value ^ (value - 1));
		}

		/// <summary>Returns a value containing the reverse of the bits in 'value'</summary>
		public static byte ReverseBits(byte value)
		{
			return (byte)(((value * 0x0802LU & 0x22110LU) | (value * 0x8020LU & 0x88440LU)) * 0x10101LU >> 16);
		}

		/// <summary>Returns a value containing the reverse of the bits in 'value'</summary>
		public static uint ReverseBits(uint value)
		{
			value = ((value >> 1) & 0x55555555) | ((value & 0x55555555) << 1);	// swap odd and even bits
			value = ((value >> 2) & 0x33333333) | ((value & 0x33333333) << 2);	// swap consecutive pairs
			value = ((value >> 4) & 0x0F0F0F0F) | ((value & 0x0F0F0F0F) << 4);	// swap nibbles ...
			value = ((value >> 8) & 0x00FF00FF) | ((value & 0x00FF00FF) << 8);	// swap bytes
			value = ( value >> 16             ) | ( value               << 16);	// swap 2-byte long pairs
			return value;
		}

		/// <summary>Return the index of the single bit set in 'n'</summary>
		public static int Index(uint n)
		{
			if (!IsPowerOfTwo(n)) throw new ArgumentException("BitIndex only works for powers of two");
			return HighBitIndex(n);
		}

		/// <summary>Returns a bit mask containing only the lowest bit of 'n'</summary>
		public static uint LowBit(uint n)
		{
			return n - ((n - 1) & n);
		}

		/// <summary>
		/// Returns the bit position of the highest bit
		/// Also, is the floor of the log base 2 for a 32 bit integer</summary>
		public static int HighBitIndex(uint value)
		{
			var pos = 0;
			var
			shift = ((value & 0xFFFF0000) != 0 ? 1 << 4 : 0); value >>= shift; pos |= shift;
			shift = ((value &     0xFF00) != 0 ? 1 << 3 : 0); value >>= shift; pos |= shift;
			shift = ((value &       0xF0) != 0 ? 1 << 2 : 0); value >>= shift; pos |= shift;
			shift = ((value &        0xC) != 0 ? 1 << 1 : 0); value >>= shift; pos |= shift;
			shift = ((value &        0x2) != 0 ? 1 << 0 : 0);                  pos |= shift;
			return pos;
		}

		/// <summary>Returns the bit position of the lowest bit</summary>
		public static int LowBitIndex(uint n)
		{
			return HighBitIndex(LowBit(n));
		}

		/// <summary>Return a bit mask contain only the highest bit of 'n'</summary> Must be a faster way?
		public static uint HighBit(uint n)
		{
			return (uint)(1 << HighBitIndex(n));
		}

		/// <summary>Returns true if 'n' is a exact power of two</summary>
		public static bool IsPowerOfTwo(uint n)
		{
			return ((n - 1) & n) == 0;
		}

		/// <summary>Count the bits set in 'value'</summary>
		public static int CountBits(uint value)
		{
			int count = 0;
			while (value != 0)
			{
				++count;
				value &= (value - 1);
			}
			return count;
		}
		public static int CountBits(int value)
		{
			return CountBits(unchecked((uint)value));
		}

		/// <summary>Return the bit position of the 'n'th set bit, starting from the LSB.
		/// 'n' is a zero based index, E.g.
		/// BitIndex(0b10010010110, 0) == 1
		/// BitIndex(0b10010010110, 1) == 2
		/// BitIndex(0b10010010110, 2) == 4
		/// BitIndex(0b10010010110, 3) == 7
		/// BitIndex(0b10010010110, 4) == 10
		/// BitIndex(0b10010010110, 5) == -1
		/// Returns -1 if less than 'index' bits are set</summary>
		public static int BitIndex(uint value, int n)
		{
			for (int pos = 0, b = 1; pos != 32; ++pos, b <<= 1)
				if ((value & b) != 0 && --n == -1) return pos;
			return -1;
		}

		//// http://infolab.stanford.edu/~manku/bitcount/bitcount.html
		//// Constant time bit count works for 32-bit numbers only.
		//// Fix last line for 64-bit numbers
		//public uint CountBits(uint n)
		//{
		//    uint tmp;
		//    tmp = n - ((n >> 1) & 033333333333)
		//            - ((n >> 2) & 011111111111);
		//    return ((tmp + (tmp >> 3)) & 030707070707) % 63;
		//}

		/// <summary>
		/// Interleaves the lower 16 bits of x and y, so the bits of x
		/// are in the even positions and bits from y in the odd.
		/// Returns the resulting 32-bit Morton Number.
		/// </summary>
		public static uint InterleaveBits(uint x, uint y)
		{
			var B = new uint[]{0x55555555, 0x33333333, 0x0F0F0F0F, 0x00FF00FF};
			var S = new []{1, 2, 4, 8};
			x = (x | (x << S[3])) & B[3];
			x = (x | (x << S[2])) & B[2];
			x = (x | (x << S[1])) & B[1];
			x = (x | (x << S[0])) & B[0];
			y = (y | (y << S[3])) & B[3];
			y = (y | (y << S[2])) & B[2];
			y = (y | (y << S[1])) & B[1];
			y = (y | (y << S[0])) & B[0];
			return x | (y << 1);
		}

		/// <summary>Convert a string (e.g. "10010110101") into a uint</summary>
		public static uint Parse(string bits)
		{
			if (bits == null) throw new ArgumentNullException("bits", "Bits.Parse() string argument was null");
			uint n = 0;
			foreach (char c in bits)
			{
				if      (c == '0') n = (n << 1);
				else if (c == '1') n = (n << 1) | 1U;
				else throw new FormatException("Bits.Parse() Argument must be a string containing only '0's and '1's");
			}
			return n;
		}

		/// <summary>Try to convert a string (e.g. "10010110101") into a uint</summary>
		public static bool TryParse(string bits, out uint value)
		{
			value = 0;
			if (bits == null) return false;
			foreach (char c in bits)
			{
				if      (c == '0') value = (value << 1);
				else if (c == '1') value = (value << 1) | 1U;
				else return false;
			}
			return true;
		}

		/// <summary>Convert the bit field 'bits' into a string with at least 'min_digits' characters</summary>
		public static string ToString(uint bits, int min_digits)
		{
			var i = 0;
			var str = new StringBuilder(32);
			for (; i != 32 && min_digits != 32 && (bits & 0x80000000) == 0; ++i, ++min_digits, bits <<= 1) {}
			for (; i != 32; ++i, bits <<= 1) { str.Append((bits & 0x80000000) != 0 ? '1' : '0'); }
			return str.ToString();
		}

		/// <summary>Convert the bit field 'bits' into a string</summary>
		public static string ToString(uint bits)
		{
			return ToString(bits, 0);
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	[TestFixture] public class TestBit
	{
		public enum NonFlags
		{
			One = 1,
			Two = 2,
		}
		[Flags] public enum Flags
		{
			One   = 1 << 0,
			Two   = 1 << 1,
			Three = 1 << 2,
			Four  = 1 << 3,
			Five  = 1 << 4,
		}

		[Test] public void HasFlag()
		{
			const NonFlags nf = NonFlags.One;
			const Flags f     = Flags.One;

			Assert.Throws(typeof(ArgumentException), () => Assert.True(f.HasFlag(nf)));
		    Assert.True(f.HasFlag(Flags.One));
		    Assert.False(f.HasFlag(Flags.One|Flags.Two));
		}
		[Test] public void BitParse()
		{
			const string bitstr0 = "100100010100110010110";
			const string bitstr1 = "011011101011001101001";

			var bits0 = Bit.Parse(bitstr0);
			var bits1 = Bit.Parse(bitstr1);
			Assert.AreEqual(bitstr0 ,Bit.ToString(bits0));
			Assert.AreEqual(bitstr1 ,Bit.ToString(bits1, bitstr1.Length));
		}
		[Test] public void BitAnySet()
		{
			const string bitstr0 = "100100010100110010110";
			const string bitstr1 = "011011101011001101001";
			var bits0 = Bit.Parse(bitstr0);
			var bits1 = Bit.Parse(bitstr1);
			Assert.False(Bit.AnySet(bits0,bits1));
		}
		[Test] public void BitIndex()
		{
			const string bitstr0 = "100100010100110010110";
			var bits0 = Bit.Parse(bitstr0);
			Assert.AreEqual(8  ,Bit.BitIndex(bits0, 4));
			Assert.AreEqual(13 ,Bit.BitIndex(bits0, 6));
			Assert.AreEqual(-1 ,Bit.BitIndex(bits0, 11));
		}
		[Test] public void EnumBitIndices()
		{
			const string bitstr0 = "100100010100110010110";
			var bits0 = Bit.Parse(bitstr0);
			var bitidxs = new[]{1,2,4,7,8,11,13,17,20};
			var idxs = Bit.EnumBitIndices(bits0).ToArray();
			Assert.True(bitidxs.SequenceEqual(idxs));
		}
		[Test] public void EnumBitMasks()
		{
			const string bitstr0 = "100100010100110010110";
			var bits0 = Bit.Parse(bitstr0);
			var bitmasks = new[]
			{
				Bit.Parse("000000000000000000010"),
				Bit.Parse("000000000000000000100"),
				Bit.Parse("000000000000000010000"),
				Bit.Parse("000000000000010000000"),
				Bit.Parse("000000000000100000000"),
				Bit.Parse("000000000100000000000"),
				Bit.Parse("000000010000000000000"),
				Bit.Parse("000100000000000000000"),
				Bit.Parse("100000000000000000000")
			};
			var masks = Bit.EnumBitMasks(bits0).ToArray();
			Assert.True(bitmasks.SequenceEqual(masks));
		}
	}
}
#endif
