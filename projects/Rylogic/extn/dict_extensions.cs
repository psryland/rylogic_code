using System;
using System.Collections.Generic;

namespace pr.extn
{
	public static class DictExtensions
	{
		/// <summary>Merge 'rhs' into this dictionary, key duplicates in 'rhs' replace items in this dictionary</summary>
		public static IDictionary<K,V> Merge<TDict,K,V>(this TDict dic, params IDictionary<K,V>[] rhs) where TDict :IDictionary<K,V>, new()
		{
			foreach (var src in rhs)
				foreach (var pair in src)
					dic[pair.Key] = pair.Value;
			return dic;
		}

		/// <summary>Value compare of a dictionary without regard to order</summary>
		public static bool EqualUnordered<TKey, TValue>(this IDictionary<TKey, TValue> lhs, IDictionary<TKey, TValue> rhs)
		{
			if (lhs == rhs) return true;
			if (lhs == null || rhs == null) return false;
			if (lhs.Count != rhs.Count) return false;

			var comparer = EqualityComparer<TValue>.Default;
			foreach (KeyValuePair<TKey, TValue> kvp in lhs)
			{
				TValue secondValue;
				if (!rhs.TryGetValue(kvp.Key, out secondValue)) return false;
				if (!comparer.Equals(kvp.Value, secondValue)) return false;
			}
			return true;
		}

		/// <summary>Get or add a new item in the dictionary</summary>
		public static TValue GetOrAdd<TKey,TValue>(this IDictionary<TKey,TValue> dic, TKey key, Func<TKey,TValue> factory)
		{
			TValue value;
			if (!dic.TryGetValue(key, out value))
				dic.Add(key, value = factory(key));
			return value;
		}

		/// <summary>Get or add a new item in the dictionary</summary>
		public static TValue GetOrAdd<TKey,TValue>(this IDictionary<TKey,TValue> dic, TKey key) where TValue :new()
		{
			return GetOrAdd(dic, key, k => new TValue());
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Linq;
	using extn;

	[TestFixture] public class TestDictExtensions
	{
		[Test] public void Merge()
		{
			var d0 = new Dictionary<int,string>();
			var d1 = new Dictionary<int,string>();
			var d2 = new Dictionary<int,string>();
			d1.Add(1,"one");
			d1.Add(2,"two");
			d2.Add(2,"too");
			d2.Add(3,"free");
			d0.Merge(d1,d2);

			Assert.AreEqual("one"  , d0[1]);
			Assert.AreEqual("too"  , d0[2]);
			Assert.AreEqual("free" , d0[3]);
		}
		[Test] public void UnorderedEqual()
		{
			var d1 = new[]{1,2,3,4}.ToDictionary(k=>k,v=>v);
			var d2 = new[]{4,3,2,1}.ToDictionary(k=>k,v=>v);
			var d3 = new[]{4,2}.ToDictionary(k=>k,v=>v);

			Assert.True(d1.EqualUnordered(d2));
			Assert.True(d2.EqualUnordered(d1));
			Assert.False(d1.EqualUnordered(d3));
			Assert.False(d3.EqualUnordered(d1));
		}
	}
}
#endif