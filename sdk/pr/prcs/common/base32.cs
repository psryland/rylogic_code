using System;
using System.Text.RegularExpressions;

namespace pr.common
{
	// In Base32, all data is grouped into 40bit groups.
	// Special processing is performed if fewer than 40 bits are available
	// at the end of the data being encoded.  A full encoding quantum is
	// always completed at the end of a body.  When fewer than 40 input bits
	// are available in an input group, bits with value zero are added (on
	// the right) to form an integral number of 5-bit groups.  Padding at
	// the end of the data is performed using the "=" character.  Since all
	// base 32 input is an integral number of octets, only the following
	// cases can arise:
	//
	// (1) The final quantum of encoding input is an integral multiple of 40
	//  bits; here, the final unit of encoded output will be an integral
	//  multiple of 8 characters with no "=" padding.
	//
	// (2) The final quantum of encoding input is exactly 8 bits; here, the
	//  final unit of encoded output will be two characters followed by
	//  six "=" padding characters.
	//
	// (3) The final quantum of encoding input is exactly 16 bits; here, the
	//  final unit of encoded output will be four characters followed by
	//  four "=" padding characters.
	//
	// (4) The final quantum of encoding input is exactly 24 bits; here, the
	//  final unit of encoded output will be five characters followed by
	//  three "=" padding characters.
	//
	// (5) The final quantum of encoding input is exactly 32 bits; here, the
	//  final unit of encoded output will be seven characters followed by
	//  one "=" padding character.
	// 
	//                    1          2          3
	//         01234567 89012345 67890123 45678901 23456789
	//        +--------+--------+--------+--------+--------+
	//        |< 1 >< 2| >< 3 ><|.4 >< 5.|>< 6 ><.|7 >< 8 >|
	//        +--------+--------+--------+--------+--------+
	//                                                <===> 8th character
	//                                          <====> 7th character
	//                                     <===> 6th character
	//                               <====> 5th character
	//                         <====> 4th character
	//                    <===> 3rd character
	//              <====> 2nd character
	//         <===> 1st character
	//
	// The RFC 4648 Base 32 alphabet
	//   0 -> A     9 -> J    18 -> S    27 -> 3
	//   1 -> B    10 -> K    19 -> T    28 -> 4
	//   2 -> C    11 -> L    20 -> U    29 -> 5
	//   3 -> D    12 -> M    21 -> V    30 -> 6
	//   4 -> E    13 -> N    22 -> W    31 -> 7
	//   5 -> F    14 -> O    23 -> X
	//   6 -> G    15 -> P    24 -> Y
	//   7 -> H    16 -> Q    25 -> Z
	//   8 -> I    17 -> R    26 -> 2    pad -> =
	
	/// <summary>Base32 encoder/decoder conforming to http://tools.ietf.org/html/rfc4648#section-6 </summary>
	public static class Base32Encoding
	{
		/// <summary>Removes any invalid characters from 'code32'. Useful for removing hyphens, spaces, etc</summary>
		public static string Sanitise(string code32)
		{
			return Regex.Replace(code32, @"[^2-7,a-z,A-Z]", "");
		}

		/// <summary>Return the length of the base32 encoded string for a byte array with length 'byte_array_length'</summary>
		public static int EncodedLength(int byte_array_length)
		{
			// Encoded data are always a multiple of 8 characters
			return ((byte_array_length + 4) / 5) * 8;
		}

		/// <summary>
		/// Returns the maximum length of the byte array needed to hold a decoded string with length 'string_length'
		/// Note: if the string contains the padding character '=',
		/// then the returned size will be a few bytes larger than necessary.</summary>
		public static int DecodedLength(int string_length)
		{
			return string_length * 5 / 8;
		}

		/// <summary>Returns the length of the byte array needed to hold the decoded 'enc_string'</summary>
		public static int DecodedLength(string enc_string)
		{
			return DecodedLength(enc_string.Trim('=').Length);
		}

		/// <summary>Convert a base32 encoded string to a byte array</summary>
		public static byte[] ToBytes(string input)
		{
			if (input == null)
				throw new ArgumentNullException("input");
			
			// Remove padding characters
			input = input.TrimEnd('=');
			
			// Determine the length of the data once decoded
			int byte_count = DecodedLength(input.Length);
			byte[] data = new byte[byte_count];
			
			// Convert a character to its decoded value
			Func<char,int> char_to_value = ch =>
				{
					if (ch <  91 && ch > 64) return ch - 65; //65-90 == uppercase letters
					if (ch <  56 && ch > 49) return ch - 24; //50-55 == numbers 2-7
					if (ch < 123 && ch > 96) return ch - 97; //97-122 == lowercase letters
					throw new ArgumentException("Character is not a Base32 character.", "ch");
				};
			
			int i = 0;
			byte current_byte = 0;
			byte remaining_bits = 8;
			foreach (char c in input)
			{
				int value = char_to_value(c);
				if (remaining_bits > 5)
				{
					int mask = value << (remaining_bits - 5);
					current_byte = (byte)(current_byte | mask);
					remaining_bits -= 5;
				}
				else
				{
					int mask = value >> (5 - remaining_bits);
					current_byte = (byte)(current_byte | mask);
					data[i++] = current_byte;
					current_byte = (byte)(value << (3 + remaining_bits));
					remaining_bits += 3;
				}
			}
			
			// If we didn't end with a full byte
			if (i != byte_count)
				data[i] = current_byte;
			
			return data;
		}

		/// <summary>Convert a byte array to a base32 encoded string</summary>
		public static string ToString(byte[] input, int start, int length)
		{
			if (input == null) throw new ArgumentNullException("input");
			if (start < 0 || start >= input.Length) throw new ArgumentException("'start' index out of range");
			if (length < 0 || start + length > input.Length) throw new ArgumentException("'length' out of range");
			
			// Determine the length of the data when encoded
			int char_count = EncodedLength(length);
			char[] data = new char[char_count];
			
			// Convert a byte to its character
			Func<byte, char> value_to_char = b =>
				{
					if (b < 26) return (char)(b + 65);
					if (b < 32) return (char)(b + 24);
					throw new ArgumentException("Byte is not a value Base32 value.", "b");
				};

			int i = 0;
			byte next_char = 0;
			byte remaining_bits = 5;
			for (int j = start, jend = start + length; j != jend; ++j)
			{
				byte b = input[j];
				next_char = (byte)(next_char | (b >> (8 - remaining_bits)));
				data[i++] = value_to_char(next_char);
				if (remaining_bits < 4)
				{
					next_char = (byte)((b >> (3 - remaining_bits)) & 31);
					data[i++] = value_to_char(next_char);
					remaining_bits += 5;
				}
				remaining_bits -= 3;
				next_char = (byte)((b << remaining_bits) & 31);
			}
			
			//if we didn't end with a full char
			if (i != char_count)
			{
				data[i++] = value_to_char(next_char);
				while (i != char_count) data[i++] = '='; //padding
			}
			
			return new string(data);
		}
		public static string ToString(byte[] input)
		{
			return ToString(input, 0, input.Length);
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using common;
	using util;

	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestBase32()
		{
			{
				var data = new byte[1]{0xff};
				var enc = Base32Encoding.ToString(data);
				var dec = Base32Encoding.ToBytes(enc);
				Assert.AreEqual(enc.Length, Base32Encoding.EncodedLength(data.Length));
				Assert.AreEqual(dec.Length, Base32Encoding.DecodedLength(enc));
				Assert.AreEqual(0, Util.Compare(data, 0, data.Length, dec, 0, dec.Length));
			}
			{
				var data = new byte[256];
				for (int i = 0; i != data.Length; ++i) data[i] = (byte)i;
				var enc = Base32Encoding.ToString(data);
				var dec = Base32Encoding.ToBytes(enc);
				Assert.AreEqual(enc.Length, Base32Encoding.EncodedLength(data.Length));
				Assert.AreEqual(dec.Length, Base32Encoding.DecodedLength(enc));
				Assert.AreEqual(0, Util.Compare(data, 0, data.Length, dec, 0, dec.Length));
			}
			var rand = new Random(42);
			for (int i = 0; i != 100; ++i)
			{
				var data = new byte[rand.Next(16000)];
				rand.NextBytes(data);
				var enc = Base32Encoding.ToString(data);
				var dec = Base32Encoding.ToBytes(enc);
				Assert.AreEqual(enc.Length, Base32Encoding.EncodedLength(data.Length));
				Assert.AreEqual(dec.Length, Base32Encoding.DecodedLength(enc));
				Assert.AreEqual(0, Util.Compare(data, 0, data.Length, dec, 0, dec.Length));
			}
		}
	}
}
#endif
