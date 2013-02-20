//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.IO;

namespace pr.extn
{
	/// <summary>Extensions for strings</summary>
	public static class StreamExtensions
	{
		/// <summary>Copies a maximum of 'count' bytes from this stream to 'dst'. Returns the number of bytes copied</summary>
		public static long CopyNTo(this Stream src, Stream dst, long count)
		{
			return src.CopyNTo(dst, count, 4096);
		}

		/// <summary>Copies a maximum of 'count' bytes from this stream to 'dst' using the given buffer size. Returns the number of bytes copied</summary>
		public static long CopyNTo(this Stream src, Stream dst, long count, int buffer_size)
		{
			byte[] buffer = new byte[buffer_size];
			long copied = 0;
			for (int n; (n = src.Read(buffer, 0, (int)Math.Min(count - copied, buffer.Length))) != 0; copied += n)
				dst.Write(buffer, 0, n);
			return copied;
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using extn;
	
	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestStreamExtn_CopyNTo()
		{
			using (var ms0 = new MemoryStream(new byte[]{1,2,3,4,5,6,7,8}, false))
			using (var ms1 = new MemoryStream())
			{
				ms0.CopyNTo(ms1, 5);
				Assert.AreEqual(8, ms0.Length);
				Assert.AreEqual(5, ms1.Length);
				
				var b0 = ms0.ToArray();
				var b1 = ms1.ToArray();
				for (int i = 0; i != ms1.Length; ++i)
					Assert.AreEqual(b0[i], b1[i]);
			}
		}
	}
}
#endif
