using System;
using System.Text;

namespace Rylogic.Common
{
	// Implements the Base64 Content-Transfer-Encoding standard described in RFC1113 (http://www.faqs.org/rfcs/rfc1113.html).
	//
	// This is the coding scheme used by MIME to allow binary data to be transferred by SMTP mail.
	// Groups of 3 bytes from a binary stream are coded as groups of 4 bytes in a text stream.
	// The input stream is 'padded' with zeros to create an input that is an even multiple of 3.
	// A special character ('=') is used to denote padding so that the stream can be decoded back to its exact size.
	//
	// Example encoding:
	//  The stream 'ABCD' is 32 bits long.  It is mapped as follows:
	//
	// ABCD
	//   A (65)     B (66)     C (67)     D (68)   (None) (None)
	//  01000001   01000010   01000011   01000100
	//  16 (Q)  20 (U)  9 (J)   3 (D)    17 (R) 0 (A)  NA (=) NA (=)
	//  010000  010100  001001  000011   010001 000000 000000 000000
	// QUJDRA==
	//
	// Decoding is the process in reverse.  A 'decode' lookup table has been created to avoid string scans.

	// Note: you should probably use Convert.ToBase64String()...
	public static class Base64Encoding
	{
		private static readonly byte[] m_enc = Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"); // Translation Table as described in RFC1113
		private static readonly byte[] m_dec = {
			62, 0, 0, 0,63,52,53,54,55,56,57,58,59,60,61, 0, 0, 0, 0, 0,
			 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15,16,17,
			18,19,20,21,22,23,24,25, 0, 0, 0, 0, 0, 0,26,27,28,29,30,31,
			32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51
			};
		
		private static byte Enc(int index) { return m_enc[index]; }
		private static byte Dec(int index) { return m_dec[index - 43]; }

		// Note:
		//        size != DecodeSize(EncodeSize(size))
		//  because encoding pads the data to a multiple of 4 bytes

		/// <summary>Return the length of a buffer required to encode data of length 'src_length' into</summary>
		public static int EncodeSize(int src_length)
		{
			return ((src_length + 2) / 3) * 4;
		}

		/// <summary>Return the length of a buffer required to decode base64 data of length 'src_length' into</summary>
		public static int DecodeSize(int src_length)
		{
			return ((src_length + 3) / 4) * 3;
		}

		// Encode 'src' to base64 into 'dst'
		// The length of 'dst' must be >= 'EncodeSize(src_length)'
		public static void Encode(byte[] src, byte[] dst)                                       { int dst_length; Encode(src, dst, out dst_length); }
		public static void Encode(byte[] src, byte[] dst, out int dst_length)                   {                 Encode(src, 0, src.Length, dst, 0, out dst_length); }
		public static void Encode(byte[] src, int soffset, int scount, byte[] dst, int doffset) { int dst_length; Encode(src, soffset, scount, dst, doffset, out dst_length); }
		public static void Encode(byte[] src, int soffset, int scount, byte[] dst, int doffset, out int dst_length)
		{
			if (dst.Length - doffset < EncodeSize(scount)) throw new ArgumentException("destination buffer too small");

			int i = soffset, o = doffset, d;
			const byte z = 0;
			for (; (scount - i) >= 3; i += 3) // encoding works in blocks of 3
			{
				dst[o++] = Enc(((src[i+0]       ) >> 2));
				dst[o++] = Enc(((src[i+0] & 0x03) << 4)|((src[i+1] & 0xf0) >> 4));
				dst[o++] = Enc(((src[i+1] & 0x0f) << 2)|((src[i+2] & 0xc0) >> 6));
				dst[o++] = Enc(((src[i+2] & 0x3f)     ));
			}
			if ((d = scount - i) != 0)
			{
				byte s0 = src[i+0], s1 = (d>1)?src[i+1]:z, s2 = (d>2)?src[i+2]:z;
				dst[o++] = Enc(((s0       ) >> 2));
				dst[o++] = Enc(((s0 & 0x03) << 4)|((s1 & 0xf0) >> 4));
				dst[o++] = d >= 2 ? Enc(((s1 & 0x0f) << 2)|((s2 & 0xc0) >> 6)) : (byte)'=';
				dst[o++] = (byte)'=';
			}
			dst_length = o - doffset;
		}

		// Decode base64 data 'src' into 'dst'
		// The length of 'dst' must be >= 'Base64_DecodeSize(src_length)'
		public static void Decode(byte[] src, byte[] dst)                                       { int dst_length; Decode(src, dst, out dst_length); }
		public static void Decode(byte[] src, byte[] dst, out int dst_length)                   {                 Decode(src, 0, src.Length, dst, 0, out dst_length); }
		public static void Decode(byte[] src, int soffset, int scount, byte[] dst, int doffset) { int dst_length; Decode(src, soffset, scount, dst, doffset, out dst_length); }
		public static void Decode(byte[] src, int soffset, int scount, byte[] dst, int doffset, out int dst_length)
		{
			if (dst.Length - doffset < DecodeSize(scount)) throw new ArgumentException("destination buffer too small");
			Func<byte, bool> isbase64 = c => {return (c != '=') && ((c == 43) || (c >= 47 && c < 58) || (c >= 65 && c < 91) || (c >= 97 && c < 123));}; // isalnum, '+', or '/'

			int i,o,j=0;
			for (i = 0, o = 0; (src.Length - i) >= 4; i += 4) // decoding works in blocks of 4
			{
				for (j = 0; j != 4 && isbase64(src[i+j]); ++j) {}
				if (j != 4) { break; }
				dst[o++] = (byte)(((Dec(src[i+0])      ) << 2) + ((Dec(src[i+1]) & 0x30) >> 4));
				dst[o++] = (byte)(((Dec(src[i+1]) & 0xf) << 4) + ((Dec(src[i+2]) & 0x3c) >> 2));
				dst[o++] = (byte)(((Dec(src[i+2]) & 0x3) << 6) + ((Dec(src[i+3])       )     ));
			}
			if (j > 1) dst[o++] = (byte)(((Dec(src[i+0])      ) << 2) + ((Dec(src[i+1]) & 0x30) >> 4));
			if (j > 2) dst[o++] = (byte)(((Dec(src[i+1]) & 0xf) << 4) + ((Dec(src[i+2]) & 0x3c) >> 2));
			if (j > 3) dst[o++] = (byte)(((Dec(src[i+2]) & 0x3) << 6) + ((Dec(src[i+3])       )     ));
			dst_length = o;
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Common;

	[TestFixture] public class TestBase64
	{
		[Test] public void Base64()
		{
			// All bytes from 0 to ff
			byte[] sbuf = new byte[256]; for (int i = 0; i != 256; ++i) sbuf[i] = (byte)i;
			byte[] dbuf = Encoding.ASCII.GetBytes("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIj"+
								"JCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+P0BBQkNERUZH"+
								"SElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2RlZmdoaWpr"+
								"bG1ub3BxcnN0dXZ3eHl6e3x9fn+AgYKDhIWGh4iJiouMjY6P"+
								"kJGSk5SVlpeYmZqbnJ2en6ChoqOkpaanqKmqq6ytrq+wsbKz"+
								"tLW2t7i5uru8vb6/wMHCw8TFxsfIycrLzM3Oz9DR0tPU1dbX"+
								"2Nna29zd3t/g4eLj5OXm5+jp6uvs7e7v8PHy8/T19vf4+fr7"+
								"/P3+/w==");

			int len = Base64Encoding.EncodeSize(sbuf.Length);
			Assert.AreEqual(dbuf.Length, len);

			int dst_length;
			byte[] dst = new byte[len];
			Base64Encoding.Encode(sbuf, dst, out dst_length);
			Assert.AreEqual(dbuf.Length, dst_length);
			for (int i = 0; i != dbuf.Length; ++i)
				Assert.AreEqual(dbuf[i], dst[i]);
		
			len = Base64Encoding.DecodeSize(dbuf.Length);
			Assert.True(len >= sbuf.Length);

			int src_length;
			byte[] src = new byte[len];
			Base64Encoding.Decode(dst, src, out src_length);
			Assert.AreEqual(sbuf.Length, src_length);
			for (int i = 0; i != sbuf.Length; ++i)
				Assert.AreEqual(sbuf[i], src[i]);

			// Random binary data
			Random r = new Random();
			for (int i = 0; i != sbuf.Length; ++i)
				sbuf[i] = (byte)(r.Next(0xFF));

			Base64Encoding.Encode(sbuf, dst, out dst_length);
			Base64Encoding.Decode(dst,  src, out src_length);
			Assert.AreEqual(sbuf.Length, src_length);
			for (int i = 0; i != sbuf.Length; ++i)
				Assert.AreEqual(sbuf[i], src[i]);
		}
	}
}
#endif
