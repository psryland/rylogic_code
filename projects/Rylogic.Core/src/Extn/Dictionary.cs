using System;
using System.Collections.Generic;
using System.Linq;
using Rylogic.Container;
using Rylogic.Utility;

namespace Rylogic.Extn
{
	public static class Dictionary_
	{
		/// <summary>Try to get an element from this dictionary, returning null if not available</summary>
		public static V? TryGetValue<K,V>(this IDictionary<K,V> dic, K key) 
			where K : notnull
			where V : class 
		{
			return dic.TryGetValue(key, out var value) ? value : null;
		}

		/// <summary>Insert the given key,value pair into this dictionary, returning 'value'</summary>
		public static V Add2<K,V>(this Dictionary<K,V> dic, K key, V value)
			where K : notnull
		{
			dic.Add(key, value);
			return value;
		}

		/// <summary>Insert the given key,value pair into this dictionary, returning this for method chaining</summary>
		public static Dictionary<K,V> ChainAdd<K,V>(this Dictionary<K,V> dic, K key, V value)
			where K : notnull
		{
			dic.Add(key, value);
			return dic;
		}

		/// <summary>Merge 'rhs' into this dictionary, key duplicates in 'rhs' replace items in this dictionary</summary>
		public static IDictionary<K,V> Merge<TDict,K,V>(this TDict dic, params IDictionary<K,V>[] rhs)
			where K : notnull
			where TDict :IDictionary<K,V>, new()
		{
			foreach (var src in rhs)
				foreach (var pair in src)
					dic[pair.Key] = pair.Value;
			return dic;
		}

		/// <summary>Value compare of a dictionary without regard to order</summary>
		public static bool SequenceEqualUnordered<TKey, TValue>(this IDictionary<TKey, TValue> lhs, IDictionary<TKey, TValue> rhs)
			where TKey : notnull
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

		/// <summary>Remove entries from the dictionary that match 'pred'</summary>
		public static int RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dic, Func<KeyValuePair<TKey, TValue>, bool> pred)
			where TKey : notnull
		{
			var to_remove = dic.Where(pred).ToList();
			foreach (var kv in to_remove)
				dic.Remove(kv);

			return to_remove.Count;
		}

		/// <summary>Get or add a new item in the dictionary</summary>
		public static TValue GetOrAdd<TKey,TValue>(this IDictionary<TKey,TValue> dic, TKey key, Func<TKey,TValue> factory)
			where TKey : notnull
		{
			if (!dic.TryGetValue(key, out var value))
				dic.Add(key, value = factory(key));
			return value;
		}

		/// <summary>Get or add a new item in the dictionary</summary>
		public static TValue GetOrAdd<TKey,TValue>(this IDictionary<TKey,TValue> dic, TKey key)
			where TKey : notnull
			where TValue :new()
		{
			return GetOrAdd(dic, key, k => new TValue());
		}

		/// <summary>Get the item associated with 'key' or 'default(TValue)' if not in the dictionary</summary>
		public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey,TValue> dic, TKey key)
			where TKey : notnull
		{
			return dic.TryGetValue(key, out var value) ? value : default!;
		}

		/// <summary>Get the item associated with 'key' or 'default(TValue)' if not in the dictionary</summary>
		public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey,TValue> dic, TKey key, Func<TKey, TValue> on_default)
			where TKey : notnull
		{
			return dic.TryGetValue(key, out var value) ? value : on_default(key);
		}

		/// <summary>Create an accumulator from this collection</summary>
		public static Accumulator<TKey, TValue> ToAccumulator<TSource, TKey, TValue>(this IEnumerable<TSource> src, Func<TSource,TKey> key, Func<TSource,TValue> value)
			where TKey : notnull
		{
			var d = new Accumulator<TKey,TValue>();
			foreach (var x in src)
				d[key(x)] = Operators<TValue>.Add(d[key(x)], value(x));

			return d;
		}
	}

	/// <summary>A wrapper that allows nullable key values to be used in a dictionary</summary>
	public struct NullableKey<TKey>
	{
		// Notes:
		//  - Wraps a dictionary key value that might be null.
		//  - Null keys are considered equal

		public NullableKey(TKey key)
		{
			Key = key;
		}

		/// <summary>The key value</summary>
		public TKey Key { get; }

		#region Equals
		public override bool Equals(object? obj)
		{
			return obj is NullableKey<TKey> nk && Equals(nk.Key, Key);
		}
		public override int GetHashCode()
		{
			return Key?.GetHashCode() ?? 0;
		}
		public static bool operator ==(NullableKey<TKey> left, NullableKey<TKey> right)
		{
			return left.Equals(right);
		}
		public static bool operator !=(NullableKey<TKey> left, NullableKey<TKey> right)
		{
			return !(left == right);
		}
		#endregion

		public static implicit operator NullableKey<TKey>(TKey key) { return new NullableKey<TKey>(key); }
		public static implicit operator TKey(NullableKey<TKey> nk) { return nk.Key; }
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Linq;
	using Extn;

	[TestFixture]
	public class TestDictExtensions
	{
		[Test]
		public void Merge()
		{
			var d0 = new Dictionary<int, string>();
			var d1 = new Dictionary<int, string>();
			var d2 = new Dictionary<int, string>();
			d1.Add(1, "one");
			d1.Add(2, "two");
			d2.Add(2, "too");
			d2.Add(3, "free");
			d0.Merge(d1, d2);

			Assert.Equal("one", d0[1]);
			Assert.Equal("too", d0[2]);
			Assert.Equal("free", d0[3]);
		}
		[Test]
		public void UnorderedEqual()
		{
			var d1 = new[] { 1, 2, 3, 4 }.ToDictionary(k => k, v => v);
			var d2 = new[] { 4, 3, 2, 1 }.ToDictionary(k => k, v => v);
			var d3 = new[] { 4, 2 }.ToDictionary(k => k, v => v);

			Assert.True(d1.SequenceEqualUnordered(d2));
			Assert.True(d2.SequenceEqualUnordered(d1));
			Assert.False(d1.SequenceEqualUnordered(d3));
			Assert.False(d3.SequenceEqualUnordered(d1));
		}
		[Test]
		public void NullableKeys()
		{
			var k0 = (object?)null;
			var k1 = new object();
			var k2 = new object();

			var d = new Dictionary<NullableKey<object?>, int>();
			d.Add(k0, 0);
			d.Add(k1, 1);
			d.Add(k2, 2);
			Assert.Equal(3, d.Count);
			Assert.Equal(0, d[null]);
			Assert.Equal(0, d[k0]);
		}
	}
}
#endif