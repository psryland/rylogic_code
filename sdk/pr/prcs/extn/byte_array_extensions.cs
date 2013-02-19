//***************************************************
// Byte Array Functions
//  Copyright © Rylogic Ltd 2010
//***************************************************

using System;
using System.Text;

namespace pr.extn
{
	public static class ByteArrayExtensions
	{
		public static byte   AsUInt8        (this byte[] arr) { return arr[0]; }
		public static ushort AsUInt16       (this byte[] arr) { return BitConverter.ToUInt16(arr, 0); }
		public static uint   AsUInt32       (this byte[] arr) { return BitConverter.ToUInt32(arr, 0); }
		public static ulong  AsUInt64       (this byte[] arr) { return BitConverter.ToUInt64(arr, 0); }
		public static sbyte  AsInt8         (this byte[] arr) { return (sbyte)arr[0]; }
		public static short  AsInt16        (this byte[] arr) { return BitConverter.ToInt16(arr, 0); }
		public static int    AsInt32        (this byte[] arr) { return BitConverter.ToInt32(arr, 0); }
		public static long   AsInt64        (this byte[] arr) { return BitConverter.ToInt64(arr, 0); }
		public static float  AsFloat        (this byte[] arr) { return BitConverter.ToSingle(arr, 0); }
		public static double AsDouble       (this byte[] arr) { return BitConverter.ToDouble(arr, 0); }
		public static bool   AsBool         (this byte[] arr) { return BitConverter.ToBoolean(arr, 0); }
		public static char   AsChar         (this byte[] arr) { return BitConverter.ToChar(arr, 0); }
		public static string AsUTF8String   (this byte[] arr) { return Encoding.UTF8.GetString(arr); }
		public static string AsUnicodeString(this byte[] arr) { return Encoding.Unicode.GetString(arr); }
		
		public static T As<T>(this byte[] arr) where T:struct
		{
			if (typeof(T) == typeof(byte  )) return (T)(object)AsUInt8 (arr);
			if (typeof(T) == typeof(ushort)) return (T)(object)AsUInt16(arr);
			if (typeof(T) == typeof(uint  )) return (T)(object)AsUInt32(arr);
			if (typeof(T) == typeof(ulong )) return (T)(object)AsUInt64(arr);
			if (typeof(T) == typeof(sbyte )) return (T)(object)AsInt8  (arr);
			if (typeof(T) == typeof(short )) return (T)(object)AsInt16 (arr);
			if (typeof(T) == typeof(int   )) return (T)(object)AsInt32 (arr);
			if (typeof(T) == typeof(long  )) return (T)(object)AsInt64 (arr);
			if (typeof(T) == typeof(float )) return (T)(object)AsFloat (arr);
			if (typeof(T) == typeof(double)) return (T)(object)AsDouble(arr);
			if (typeof(T) == typeof(bool  )) return (T)(object)AsBool  (arr);
			if (typeof(T) == typeof(char  )) return (T)(object)AsChar  (arr);
			throw new NotSupportedException("Type " + typeof(T).Name + " not supported in As<T>");
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using extn;

	[TestFixture] internal partial class UnitTests
	{
		[Test] public static void TestByteArrayAs()
		{
			var b0 = new byte[]{0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08};
			var b1 = new byte[]{0x00, 0x00, 0x80, 0x3f};
			var b2 = new byte[]{0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F};
			Assert.AreEqual(0x01                  ,b0.AsUInt8 ());
			Assert.AreEqual(0x0201                ,b0.AsUInt16());
			Assert.AreEqual(0x04030201            ,b0.AsUInt32());
			Assert.AreEqual(0x0807060504030201    ,b0.AsUInt64());
			Assert.AreEqual(0x01                  ,b0.AsInt8  ());
			Assert.AreEqual(0x0201                ,b0.AsInt16 ());
			Assert.AreEqual(0x04030201            ,b0.AsInt32 ());
			Assert.AreEqual(0x0807060504030201    ,b0.AsInt64 ());
			Assert.AreEqual(1f                    ,b1.AsFloat ());
			Assert.AreEqual(1.0                   ,b2.AsDouble());
			Assert.AreEqual(true                  ,b0.AsBool  ());
			Assert.AreEqual((char)0x0201          ,b0.AsChar  ());
		}
	}
}
#endif
