using System.Collections.Generic;

namespace pr.container
{
	/// <summary>A specialised dictionary for counting occurrences of unique types</summary>
	public class Accumulator<TKey> :Dictionary<TKey,int>
	{
		public new int this[TKey key]
		{
			get { return TryGetValue(key, out var count) ? count : 0; }
			set { base[key] = value; }
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using container;

	[TestFixture] public class TestAccumulator
	{
		[Test] public void Accumulator()
		{
			var acc = new Accumulator<string>();

			acc["One"] = 1;
			acc["Two"] = 1;
			acc["Two"] += 1;
			acc["Three"] += 1;
			acc["Three"] += 1;
			acc["Three"] += 1;
			Assert.AreEqual(acc["One"], 1);
			Assert.AreEqual(acc["Two"], 2);
			Assert.AreEqual(acc["Three"], 3);
			Assert.AreEqual(acc["Four"], 0);
		}
	}
}
#endif