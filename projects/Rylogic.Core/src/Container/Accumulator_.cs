using System.Collections.Generic;

namespace Rylogic.Container
{
	/// <summary>A specialised dictionary for counting occurrences of unique types</summary>
	public class Accumulator<TKey,TValue> :Dictionary<TKey,TValue>
	{
		public new TValue this[TKey key]
		{
			get { return TryGetValue(key, out var count) ? count : default!; }
			set { base[key] = value; }
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Container;

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
			Assert.Equal(acc["One"], 1f);
			Assert.Equal(acc["Two"], 2f);
			Assert.Equal(acc["Three"], 3f);
			Assert.Equal(acc["Four"], 0f);

			var acc2 = new Accumulator<string, int>();
			acc2["One"]++;
			acc2["One"]++;
			acc2["One"]++;
			Assert.Equal(acc2["One"], 3);
		}
	}
}
#endif