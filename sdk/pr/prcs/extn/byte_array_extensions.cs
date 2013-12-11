//***************************************************
// Byte Array Functions
//  Copyright © Rylogic Ltd 2010
//***************************************************

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using pr.common;

namespace pr.extn
{
	public static class ByteArrayExtensions
	{
		public static sbyte  AsInt8         (this byte[] arr, int start_index = 0) { return (sbyte)arr[start_index]; }
		public static byte   AsUInt8        (this byte[] arr, int start_index = 0) { return arr[start_index]; }
		public static ushort AsUInt16       (this byte[] arr, int start_index = 0) { return BitConverter.ToUInt16     (arr, start_index); }
		public static uint   AsUInt32       (this byte[] arr, int start_index = 0) { return BitConverter.ToUInt32     (arr, start_index); }
		public static ulong  AsUInt64       (this byte[] arr, int start_index = 0) { return BitConverter.ToUInt64     (arr, start_index); }
		public static short  AsInt16        (this byte[] arr, int start_index = 0) { return BitConverter.ToInt16      (arr, start_index); }
		public static int    AsInt32        (this byte[] arr, int start_index = 0) { return BitConverter.ToInt32      (arr, start_index); }
		public static long   AsInt64        (this byte[] arr, int start_index = 0) { return BitConverter.ToInt64      (arr, start_index); }
		public static float  AsFloat        (this byte[] arr, int start_index = 0) { return BitConverter.ToSingle     (arr, start_index); }
		public static double AsDouble       (this byte[] arr, int start_index = 0) { return BitConverter.ToDouble     (arr, start_index); }
		public static bool   AsBool         (this byte[] arr, int start_index = 0) { return BitConverter.ToBoolean    (arr, start_index); }
		public static char   AsChar         (this byte[] arr, int start_index = 0) { return BitConverter.ToChar       (arr, start_index); }
		public static string AsUTF8String   (this byte[] arr, int start_index = 0) { return Encoding.UTF8.GetString   (arr, start_index, arr.Length - start_index); }
		public static string AsUnicodeString(this byte[] arr, int start_index = 0) { return Encoding.Unicode.GetString(arr, start_index, arr.Length - start_index); }

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

			Debug.Assert(arr.Length >= Marshal.SizeOf(typeof(T)), "As<T>: Insufficient data. Expected {0}, got {1}".Fmt(Marshal.SizeOf(typeof(T)), arr.Length));
			using (var handle = GCHandleEx.Alloc(arr, GCHandleType.Pinned))
				return (T)Marshal.PtrToStructure(handle.State.AddrOfPinnedObject(), typeof(T));
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
		internal static partial class TestExtensions
		{
			[StructLayout(LayoutKind.Sequential,Pack = 1,CharSet = CharSet.Ansi)]
			internal struct Thing
			{
				public int m_int;
				public char m_char;
				public byte m_byte;
			}
			[Test] public static void ByteArrayAs()
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
			[Test] public static void ByteArrayAsStruct()
			{
				var b0 = new byte[]{0x01,0x02,0x03,0x04,0x41,0xAB};
				var thing = b0.As<Thing>();

				Assert.AreEqual(0x04030201 , thing.m_int );
				Assert.AreEqual('A'        , thing.m_char);
				Assert.AreEqual(0xab       , thing.m_byte);
			}
		}
	}
}
#endif
