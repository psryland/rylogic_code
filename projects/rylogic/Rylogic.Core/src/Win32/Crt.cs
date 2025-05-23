﻿using System;
using System.Runtime.InteropServices;

namespace Rylogic.Interop.Win32
{
	/// <summary>C Runtime Interop</summary>
	public static class Crt
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
namespace Rylogic.UnitTests
{
	using Interop.Win32;

	[TestFixture] public class TestCrt
	{
		[Test] public void CRT()
		{
			{
				byte[] block0 = new byte[]{0,1,2,3,4,5,6,7,8,9};
				byte[] block1 = new byte[10];
				Crt.memcpy(block1, block0);
				for (int i = 0; i != block0.Length; ++i)
					Assert.Equal(block0[i], block1[i]);
			}
			{
				byte[] block0 = new byte[]{0,1,2,3,4,5,6,7,8,9};
				byte[] block1 = new byte[]{0,1,2,3,4,5,6,7,8,9};
				Assert.Equal(0, Crt.memcmp(block0, block1));
				block1[3] = 4;
				Assert.True(Crt.memcmp(block0, block1) < 0);
				Assert.True(Crt.memcmp(block1, block0) > 0);
			}
		}
	}
}
#endif
