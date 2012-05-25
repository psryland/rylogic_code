using System;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;

namespace pr.util
{
	/// <summary>C Runtime Interop</summary>
	public static class crt
	{
		/// <summary>Memory copy</summary>
		[DllImport("msvcrt.dll")]  public static extern int memcpy(byte[] dst, byte[] src, long num);

		/// <summary>Memory compare using implicit array lengths</summary>
		public static int memcpy(byte[] lhs, byte[] rhs)
		{
			return memcpy(lhs, rhs, Math.Min(lhs.Length, rhs.Length));
		}

		/// <summary>Memory compare</summary>
		[DllImport("msvcrt.dll")]  public static extern int memcmp(byte[] b1, byte[] b2, long count);

		/// <summary>Memory compare using implicit array lengths</summary>
		public static int memcmp(byte[] lhs, byte[] rhs)
		{
			if (lhs == rhs) return 0;
			if (lhs == null) return -1;
			if (rhs == null) return +1;
			int r = memcmp(lhs, rhs, Math.Min(lhs.Length, rhs.Length));
			return (r != 0) ? r : lhs.Length - rhs.Length;
		}
		
		/// <summary>Scan a formatted string</summary>
		[DllImport("msvcrt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern int sscanf(string buffer, string format, __arglist);
	}

	[TestFixture] internal partial class UnitTests
	{
		[Test] public static void TestCRT()
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
			{// sscanf
				char c; int d; float f;
				crt.sscanf("A,2;3.125", "%c,%d;%f", __arglist(out c, out d, out f));
				Assert.AreEqual('A', c);
				Assert.AreEqual(2, d);
				Assert.AreEqual(3.125, f);

				//const string line = "DVL:";// 21378: + 1.571%  >   3.140% (   3.1%), RV: 8573, AV:    0, Dir:S, mm:  23.773, PKmA:  371, OD: 0.0, W:  0.1, AC°C:26, AS°C:22";
				//StringBuilder state = new StringBuilder(); state.Length = 4;
				//////int timestamp, rv, av, PKmA, ACtemp, AStemp;
				//////float delta_dc, total_dc, summik0, extn, OD, W;
				//////char dir;
				//crt.sscanf(line,
				//    "%s",//: %d: + %f%%  >%f%% (%f%%), RV:%d, AV:%d, Dir:%c, mm:%f, PKmA:%d, OD:%f, W:%f, AC°C:%d, AS°C:%d",
				//    __arglist(ref state));//, out timestamp, out delta_dc, out total_dc, out summik0, out rv, out av, out dir, out extn, out PKmA, out OD, out W, out ACtemp, out AStemp));
				////Assert.AreEqual("DVL:", state);
				
				////Assert.AreEqual(21378, timestamp);
				//Assert.AreEqual(1.571, delta_dc);
				//Assert.AreEqual(3.140, total_dc);
				//Assert.AreEqual(3.1, summik0);
				//Assert.AreEqual(8573, rv);
				//Assert.AreEqual(0, av);
				//Assert.AreEqual('S', dir);
				//Assert.AreEqual(23.773, extn);
				//Assert.AreEqual(371, PKmA);
				//Assert.AreEqual(0.0, OD);
				//Assert.AreEqual(0.1, W);
				//Assert.AreEqual(26, ACtemp);
				//Assert.AreEqual(22, AStemp);
			}
		}
	}
}
