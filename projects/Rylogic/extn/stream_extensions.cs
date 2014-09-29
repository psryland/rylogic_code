//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.IO;
using pr.util;

namespace pr.extn
{
	/// <summary>Extensions for strings</summary>
	public static class StreamExtensions
	{
		/// <summary>Copies a maximum of 'count' bytes from this stream to 'dst'. Returns the number of bytes copied</summary>
		public static long CopyTo(this Stream src, long count, Stream dst)
		{
			return src.CopyTo(count, dst, 4096);
		}

		/// <summary>Copies a maximum of 'count' bytes from this stream to 'dst' using the given buffer size. Returns the number of bytes copied</summary>
		public static long CopyTo(this Stream src, long count, Stream dst, int buffer_size)
		{
			byte[] buffer = new byte[buffer_size];
			long copied = 0;
			for (int n; (n = src.Read(buffer, 0, (int)Math.Min(count - copied, buffer.Length))) != 0; copied += n)
				dst.Write(buffer, 0, n);
			return copied;
		}

		/// <summary>Fill 'buffer' from the stream, throwing if not enough data is available</summary>
		public static byte[] Read(this Stream src, byte[] buffer)
		{
			var read = src.Read(buffer, 0, buffer.Length);
			if (read == buffer.Length) return buffer;
			var msg = src.CanSeek
				? "Incomplete read. Expected {0} bytes at stream position {1} (of {2})".Fmt(buffer.Length, src.Position - read, src.Length)
				: "Incomplete read. Expected {0} bytes from stream".Fmt(buffer.Length);
			throw new Exception(msg);
		}

		/// <summary>Read a structure from the stream</summary>
		public static T Read<T>(this Stream src) where T:struct
		{
			try
			{
				var buffer = Util.CreateByteArrayOfSize<T>();
				src.Read(buffer);
				return Util.FromBytes<T>(buffer);
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to read type {0}.\r\n{1}".Fmt(typeof(T).Name, ex.Message));
			}
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using extn;

	[TestFixture] public class TestStreamExtns
	{
		[Test] public void CopyNTo()
		{
			using (var ms0 = new MemoryStream(new byte[]{1,2,3,4,5,6,7,8}, false))
			using (var ms1 = new MemoryStream())
			{
				ms0.CopyTo(5, ms1);
				Assert.AreEqual(8L, ms0.Length);
				Assert.AreEqual(5L, ms1.Length);

				var b0 = ms0.ToArray();
				var b1 = ms1.ToArray();
				for (int i = 0; i != ms1.Length; ++i)
					Assert.AreEqual(b0[i], b1[i]);
			}
		}
	}
}
#endif
