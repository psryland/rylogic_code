//***************************************************
// Byte Array Functions
//  Copyright © Rylogic Ltd 2010
//***************************************************

namespace pr.extn
{
	public static class ArrayExtensions
	{
		/// <summary>Create a shallow copy of this array</summary>
		public static T[] Dup<T>(this T[] arr)
		{
			return (T[])arr.Clone();
		}

		/// <summary>Returns the index of 'what' in the array</summary>
		public static int IndexOf<T>(this T[] arr, T what)
		{
			int idx = 0;
			foreach (var i in arr)
			{
				if (!Equals(i,what)) ++idx;
				else return idx;
			}
			return -1;
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using System.Linq;
	using extn;

	[TestFixture] internal partial class UnitTests
	{
		internal static partial class TestExtensions
		{
			[Test] public static void ArrayExtns()
			{
				var a0 = new[]{1,2,3,4};
				var A0 = a0.Dup();

				Assert.AreEqual(typeof(int[]), A0.GetType());
				Assert.IsTrue(A0.SequenceEqual(a0));

				Assert.AreEqual(2, A0.IndexOf(3));
				Assert.AreEqual(-1, A0.IndexOf(5));
			}
		}
	}
}
#endif
