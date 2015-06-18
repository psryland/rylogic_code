using System;
using System.Runtime.InteropServices;
using pr.util;

namespace pr.util
{
	/// <summary>C Runtime Interop</summary>
	public static class crt
	{
		/// <summary>Memory copy</summary>
		[DllImport("msvcrt.dll", CallingConvention=CallingConvention.Cdecl)]  public static extern int memcpy(byte[] dst, byte[] src, long num);

		/// <summary>Memory compare using implicit array lengths</summary>
		public static int memcpy(byte[] lhs, byte[] rhs)
		{
			return memcpy(lhs, rhs, Math.Min(lhs.Length, rhs.Length));
		}

		/// <summary>Memory compare</summary>
		[DllImport("msvcrt.dll", CallingConvention=CallingConvention.Cdecl)]  public static extern int memcmp(byte[] b1, byte[] b2, long count);

		/// <summary>Memory compare using implicit array lengths</summary>
		public static int memcmp(byte[] lhs, byte[] rhs)
		{
			if (lhs == rhs) return 0;
			if (lhs == null) return -1;
			if (rhs == null) return +1;
			int r = memcmp(lhs, rhs, Math.Min(lhs.Length, rhs.Length));
			return (r != 0) ? r : lhs.Length - rhs.Length;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	[TestFixture] public class TestCrt
	{
		[Test] public void CRT()
		{
			{// memcpy
				byte[] block0 = new byte[]{0,1,2,3,4,5,6,7,8,9};
				byte[] block1 = new byte[10];
				crt.memcpy(block1, block0);
				for (int i = 0; i != block0.Length; ++i)
					Assert.AreEqual(block0[i], block1[i]);
			}
			{// memcmp
				byte[] block0 = new byte[]{0,1,2,3,4,5,6,7,8,9};
				byte[] block1 = new byte[]{0,1,2,3,4,5,6,7,8,9};
				Assert.AreEqual(0, crt.memcmp(block0, block1));
				block1[3] = 4;
				Assert.True(crt.memcmp(block0, block1) < 0);
				Assert.True(crt.memcmp(block1, block0) > 0);
			}
		}
	}
}
#endif
