using System.Collections.Generic;

namespace pr.container
{
	/// <summary>A specialised dictionary for counting occurrences of unique types</summary>
	public class Accumulator<TKey,TValue> :Dictionary<TKey,TValue>
	{
		public new TValue this[TKey key]
		{
			get { return TryGetValue(key, out var count) ? count : default(TValue); }
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
			var acc = new Accumulator<string, float>();

			acc["One"] = 1f;
			acc["Two"] = 1f;
			acc["Two"] += 1f;
			acc["Three"] += 4f;
			acc["Three"] -= 1f;
			acc["Three"] *= 1f;
			Assert.AreEqual(acc["One"], 1f);
			Assert.AreEqual(acc["Two"], 2f);
			Assert.AreEqual(acc["Three"], 3f);
			Assert.AreEqual(acc["Four"], 0f);
		}
	}
}
#endif