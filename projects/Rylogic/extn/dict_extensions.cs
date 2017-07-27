using System;
using System.Collections.Generic;

namespace pr.extn
{
	public static class Dictionary_
	{
		/// <summary>Try to get an element from this dictionary, returning null if not available</summary>
		public static V TryGetValue<K,V>(this IDictionary<K,V> dic, K key) where V : class
		{
			return dic.TryGetValue(key, out var value) ? value : null;
		}

		/// <summary>Insert the given key,value pair into this dictionary, returning this for method chaining</summary>
		public static Dictionary<K,V> Add2<K,V>(this Dictionary<K,V> dic, K key, V value)
		{
			dic.Add(key, value);
			return dic;
		}

		/// <summary>Merge 'rhs' into this dictionary, key duplicates in 'rhs' replace items in this dictionary</summary>
		public static IDictionary<K,V> Merge<TDict,K,V>(this TDict dic, params IDictionary<K,V>[] rhs) where TDict :IDictionary<K,V>, new()
		{
			foreach (var src in rhs)
				foreach (var pair in src)
					dic[pair.Key] = pair.Value;
			return dic;
		}

		/// <summary>Value compare of a dictionary without regard to order</summary>
		public static bool SequenceEqualUnordered<TKey, TValue>(this IDictionary<TKey, TValue> lhs, IDictionary<TKey, TValue> rhs)
		{
			if (lhs == rhs) return true;
			if (lhs == null || rhs == null) return false;
			if (lhs.Count != rhs.Count) return false;

			var comparer = EqualityComparer<TValue>.Default;
			foreach (KeyValuePair<TKey, TValue> kvp in lhs)
			{
				if (!rhs.TryGetValue(kvp.Key, out var secondValue)) return false;
				if (!comparer.Equals(kvp.Value, secondValue)) return false;
			}
			return true;
		}

		/// <summary>Get or add a new item in the dictionary</summary>
		public static TValue GetOrAdd<TKey,TValue>(this IDictionary<TKey,TValue> dic, TKey key, Func<TKey,TValue> factory)
		{
			if (!dic.TryGetValue(key, out var value))
				dic.Add(key, value = factory(key));
			return value;
		}

		/// <summary>Get or add a new item in the dictionary</summary>
		public static TValue GetOrAdd<TKey,TValue>(this IDictionary<TKey,TValue> dic, TKey key) where TValue :new()
		{
			return GetOrAdd(dic, key, k => new TValue());
		}

		/// <summary>Get the item associated with 'key' or 'default(TValue)' if not in the dictionary</summary>
		public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey,TValue> dic, TKey key)
		{
			return dic.TryGetValue(key, out var value) ? value : default(TValue);
		}

		/// <summary>Get the item associated with 'key' or 'default(TValue)' if not in the dictionary</summary>
		public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey,TValue> dic, TKey key, Func<TKey, TValue> on_default)
		{
			return dic.TryGetValue(key, out var value) ? value : on_default(key);
		}
	}

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

			Assert.True(d1.SequenceEqualUnordered(d2));
			Assert.True(d2.SequenceEqualUnordered(d1));
			Assert.False(d1.SequenceEqualUnordered(d3));
			Assert.False(d3.SequenceEqualUnordered(d1));
		}
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