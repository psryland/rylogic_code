//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Utility;

namespace Rylogic.Extn
{
	/// <summary>Extensions for IEnumerable</summary>
	public static class Enumerable_
	{
		/// <summary>Create an IEnumerable from parameters</summary>
		public static IEnumerable<T> As<T>(params T[] items)
		{
			foreach (var item in items)
				yield return item;
		}
		public static IEnumerable<T> As<T>(params object?[] items)
		{
			foreach (var item in items)
				yield return (T)item!;
		}

		/// <summary>Create an IEnumerable from a single value</summary>
		[Obsolete] public static IEnumerable<T> Sequence<T>(T value) => As<T>(value);

		/// <summary>Enumerate instances of a given type</summary>
		public static IEnumerable OfType(this IEnumerable source, Type ty)
		{
			foreach (var x in source)
				if (x != null && x.GetType().Inherits(ty))
					yield return x;
		}

		/// <summary>Enumerate all instances that aren't null</summary>
		public static IEnumerable<TSource> NotNull<TSource>(this IEnumerable<TSource?> source)
			where TSource : class
		{
			foreach (var s in source)
			{
				if (s == null) continue;
				yield return s;
			}
		}
		public static IEnumerable<TSource> NotNull<TSource>(this IEnumerable<TSource?> source)
			where TSource : struct
		{
			foreach (var s in source)
			{
				if (s == null) continue;
				yield return s.Value;
			}
		}

		/// <summary>Cast a collection to statically convertible type</summary>
		public static IEnumerable<TResult> ConvertTo<TResult>(this IEnumerable source)
		{
			return source.Cast<object>().Select(x => Util.ConvertTo<TResult>(x));
		}

		/// <summary>Convert the collection into a hash set</summary>
		public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
		{
			return new HashSet<TSource>(source);
		}
		public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source, int disambiguator_from_net472)
		{
			// This is needed when Rylogic.Core (a NetStandart2.0 library) is used in a .NET4.7.2 project
			return new HashSet<TSource>(source);
		}

		/// <summary>Convert the collection into a hash set</summary>
		public static HashSet<TKey> ToHashSet<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector)
		{
			return new HashSet<TKey>(source.Select(selector));
		}

		/// <summary>Apply 'action' to each item in the collection</summary>
		public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
		{
			foreach (var item in source)
				action(item);
		}
		public static void ForEach<TSource>(this IEnumerable source, Action<TSource> action)
		{
			foreach (var item in source)
				action((TSource)item!);
		}

		/// <summary>Apply 'action' to each item in the collection. Includes an indexing variable</summary>
		public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource, int> action)
		{
			int i = 0;
			foreach (var item in source)
				action(item, i++);
		}
		public static void ForEach<TSource>(this IEnumerable source, Action<TSource, int> action)
		{
			int i = 0;
			foreach (var item in source)
				action((TSource)item!, i++);
		}

		/// <summary>Apply 'action' to each item in the collection</summary>
		public static TRet ForEach<TSource,TRet>(this IEnumerable<TSource> source, TRet initial, Func<TSource, TRet, TRet> action)
		{
			foreach (var item in source)
				initial = action(item, initial);
			
			return initial;
		}
		public static TRet ForEach<TSource,TRet>(this IEnumerable source, TRet initial, Func<TSource, TRet, TRet> action)
		{
			foreach (var item in source)
				initial = action((TSource)item!, initial);

			return initial;
		}

		/// <summary>Compare sub-ranges within collections for value equality</summary>
		public static bool SequenceEqual<TSource>(this IEnumerable<TSource> lhs, IEnumerable<TSource> rhs, int len, IEqualityComparer<TSource>? comparer = null)
		{
			return SequenceEqual(lhs, rhs, 0, 0, len, comparer);
		}

		/// <summary>Compare sub-ranges within collections for value equality</summary>
		public static bool SequenceEqual<TSource>(this IEnumerable<TSource> lhs, IEnumerable<TSource> rhs, int ofs0, int ofs1, int len, IEqualityComparer<TSource>? comparer = null)
		{
			comparer ??= EqualityComparer<TSource>.Default;
			return Enumerable.SequenceEqual(lhs.Skip(ofs0).Take(len), rhs.Skip(ofs1).Take(len), comparer);
		}

		/// <summary>Compare two collections for value equality ignoring order</summary>
		public static bool SequenceEqualUnordered<TSource>(this IEnumerable<TSource> lhs, IEnumerable<TSource> rhs)
		{
			if (lhs == rhs) return true;
			if (lhs == null || rhs == null) return false;

			// A map from elements in 'lhs' to their counts
			var lookup = new Accumulator<NullableKey<TSource>, int>();
			foreach (var x in lhs)
				lookup[x] += 1;

			// Match each element in 'rhs' to the elements in 'lhs'
			foreach (var x in rhs)
			{
				if (!lookup.TryGetValue(x, out var count)) return false;
				if (count > 1) lookup[x] = count - 1;
				else lookup.Remove(x);
			}
			return lookup.Count == 0;
		}

		/// <summary>True if this collection is ordered, given by 'order'</summary>
		public static bool SequenceOrdered<TSource>(this IEnumerable<TSource> source, ESequenceOrder order, IComparer<TSource>? comparer = null)
		{
			return source.IsOrdered(order, comparer);
		}

		/// <summary>Enumerate this range in reverse. Note: Same as "IEnumerable.Reverse". "IList.Reverse" however, does an in-place reverse</summary>
		public static IEnumerable<TSource> Reversed<TSource>(this IEnumerable<TSource> source)
		{
			return source.Reverse();
		}

		/// <summary>Returns elements from this collection that aren't also in 'rhs'. Note: The MS version of this function doesn't work</summary>
		public static IEnumerable<TSource> ExceptBy<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> rhs, IEqualityComparer<TSource>? comparer = null) where TSource : notnull
		{
			comparer = comparer ?? Eql<TSource>.Default;
			var exclude = rhs.ToHashSet();
			return source.Where(x => !exclude.Contains(x, comparer));
		}

		/// <summary>Returns elements from this collection that aren't also in 'rhs'. Note: The MS version of this function doesn't work</summary>
		public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer, HashSet<TSource> exclude)
		{
			comparer = comparer ?? Eql<TSource>.Default;
			return source.Where(x => !exclude.Contains(x, comparer));
		}
		public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer, params TSource[] rhs)
		{
			return Except(source, comparer, rhs.ToHashSet());
		}
		public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> source, params TSource[] rhs)
		{
			return Except(source, null, rhs);
		}
		public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> source, HashSet<TSource> rhs)
		{
			return Except(source, null, rhs);
		}

		/// <summary>Return the index of the first occurrence of 'pred(x) == true' or -1</summary>
		public static int IndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> pred)
		{
			switch (source)
			{
				case IList<TSource> list:
				{
					return list.IndexOf(pred);
				}
				default:
				{
					var idx = 0;
					foreach (var x in source)
					{
						if (!pred(x)) { ++idx; continue; }
						return idx;
					}
					return -1;
				}
			}
		}
		public static int IndexOf(this IEnumerable source, object obj)
		{
			switch (source)
			{
				case IList list:
				{
					return list.IndexOf(obj);
				}
				default:
				{
					var idx = 0;
					foreach (var x in source)
					{
						if (!Equals(obj, x)) { ++idx; continue; }
						return idx;
					}
					return -1;
				}
			}
		}

		/// <summary>Return the index of the last occurrence of 'pred(x) == true' or -1</summary>
		public static int LastIndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> pred)
		{
			int i = -1, idx = -1;
			foreach (var x in source)
			{
				++i;
				if (pred(x))
					idx = i;
			}
			return idx;
		}

		/// <summary>Returns the indices of 'element' within this collection</summary>
		public static IEnumerable<int> IndicesOf<TSource>(this IEnumerable<TSource> source, TSource element, IEqualityComparer<TSource>? comparer = null)
		{
			comparer = comparer ?? Eql<TSource>.Default;
			var i = 0;
			foreach (var s in source)
			{
				if (comparer.Equals(s, element)) yield return i;
				++i;
			}
		}

		/// <summary>Return the indices of the selected elements within the enumerable</summary>
		public static IEnumerable<int> IndicesOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> selector)
		{
			var i = 0;
			foreach (var s in source)
			{
				if (selector(s)) yield return i;
				++i;
			}
		}

		/// <summary>Returns true if adjacent items in the collection satisfy 'pred(x[i], x[i+1])'</summary>
		public static bool IsOrdered<TSource>(this IEnumerable<TSource> source, Func<TSource, int> pred)
		{
			return IsOrdered(source, ESequenceOrder.Increasing, Cmp<TSource>.From(pred));
		}
		public static bool IsOrdered<TSource>(this IEnumerable<TSource> source, Func<TSource,TSource,bool> pred)
		{
			var iter = source.GetIterator();
			if (iter.AtEnd) return true;
			for (var prev = iter.CurrentThenNext(); !iter.AtEnd && pred(prev, iter.Current); prev = iter.CurrentThenNext()) {}
			return iter.AtEnd;
		}
		public static bool IsOrdered<TSource>(this IEnumerable<TSource> source, ESequenceOrder order = ESequenceOrder.Increasing, IComparer<TSource>? comparer = null)
		{
			comparer = comparer ?? Cmp<TSource>.Default;
			switch (order)
			{
			default: throw new Exception($"Unknown ordering type: {order}");
			case ESequenceOrder.Increasing:         return source.IsOrdered((l, r) => comparer.Compare(l, r) <= 0);
			case ESequenceOrder.StrictlyIncreasing: return source.IsOrdered((l, r) => comparer.Compare(l, r) <  0);
			case ESequenceOrder.Decreasing:         return source.IsOrdered((l, r) => comparer.Compare(l, r) >= 0);
			case ESequenceOrder.StrictlyDecreasing: return source.IsOrdered((l, r) => comparer.Compare(l, r) >  0);
			}
		}

		/// <summary>Returns true if all elements this collection are equal (or return equal results from 'selector')</summary>
		public static bool AllSame<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer = null)
		{
			comparer ??= Eql<TSource>.Default;

			if (!source.Any()) return true;
			var first = source.First();
			return source.Skip(1).All(x => comparer.Equals(first, x));
		}
		public static bool AllSame<TSource, TRet>(this IEnumerable<TSource> source, Func<TSource,TRet> selector, IEqualityComparer<TRet>? comparer = null)
		{
			comparer ??= Eql<TRet>.Default;

			if (!source.Any()) return true;
			var first = selector(source.First());
			return source.Skip(1).All(x => comparer.Equals(first, selector(x)));
		}

		/// <summary>Returns the maximum element based on 'selector', with comparisons of the selector type made by 'comparer'</summary>
		public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey>? comparer = null)
		{
			if (source   == null) throw new ArgumentNullException("source");
			if (selector == null) throw new ArgumentNullException("selector");

			comparer = comparer ?? Cmp<TKey>.Default;
			using (var src_iter = source.GetEnumerator())
			{
				if (!src_iter.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");

				var max = src_iter.Current;
				var max_key = selector(max);
				while (src_iter.MoveNext())
				{
					var item = src_iter.Current;
					var key = selector(item);
					if (comparer.Compare(key, max_key) <= 0) continue;
					max_key = key;
					max = item;
				}
				return max;
			}
		}
		public static TSource MaxByOrDefault<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey>? comparer = null)
		{
			if (!source.Any()) return default!;
			return source.MaxBy(selector, comparer);
		}

		/// <summary>Returns the minimum element based on 'selector', with comparisons of the selector type made by 'comparer'</summary>
		public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey>? comparer = null)
		{
			if (source   == null) throw new ArgumentNullException("source");
			if (selector == null) throw new ArgumentNullException("selector");

			comparer ??= Cmp<TKey>.Default;
			using (var src_iter = source.GetEnumerator())
			{
				if (!src_iter.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");

				var max = src_iter.Current;
				var max_key = selector(max);
				while (src_iter.MoveNext())
				{
					var item = src_iter.Current;
					var key = selector(item);
					if (comparer.Compare(key, max_key) >= 0) continue;
					max_key = key;
					max = item;
				}
				return max;
			}
		}
		public static TSource MinByOrDefault<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey>? comparer = null)
		{
			if (!source.Any()) return default!;
			return source.MinBy(selector, comparer);
		}

		/// <summary>Returns the sum of the values in the collection</summary>
		public static TValue Sum<TSource,TValue>(this IEnumerable<TSource> source, Func<TSource, TValue> selector)
		{
			var sum = default(TValue)!;
			foreach (var x in source)
				sum = Operators<TValue>.Add(sum, selector(x));
			return sum;
		}

		/// <summary>Returns one of the items that occur most frequently within a sequence</summary>
		public static Freq<TSource> MaxFrequency<TSource>(this IEnumerable<TSource> source)
		{
			var most_freq = new Freq<TSource>();
			var lookup = new Accumulator<NullableKey<TSource>, int>();
			foreach (var item in source)
			{
				var count = lookup[item] += 1;
				if (count <= most_freq.Frequency) continue;
				most_freq.Item = item;
				most_freq.Frequency = count;
			}
			return most_freq;
		}
		public class Freq<TItem>
		{
			/// <summary>The item being counted</summary>
			public TItem Item = default!;

			/// <summary>The count</summary>
			public int Frequency = 0;
		}

		/// <summary>Returns the index of the maximum element based on 'selector', with comparisons of the selector type made by 'comparer'</summary>
		public static int IndexOfMaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey>? comparer = null)
		{
			if (source   == null) throw new ArgumentNullException("source");
			if (selector == null) throw new ArgumentNullException("selector");

			comparer = comparer ?? Cmp<TKey>.Default;
			using (var src_iter = source.GetEnumerator())
			{
				if (!src_iter.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");

				int index = 0;
				int index_of_max = 0;
				var max_key = selector(src_iter.Current);
				while (src_iter.MoveNext())
				{
					++index;
					var item = src_iter.Current;
					var key = selector(item);
					if (comparer.Compare(key, max_key) <= 0) continue;
					max_key = key;
					index_of_max = index;
				}
				return index_of_max;
			}
		}

		/// <summary>Returns the index of the minimum element based on 'selector', with comparisons of the selector type made by 'comparer'</summary>
		public static int IndexOfMinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey>? comparer = null)
		{
			if (source   == null) throw new ArgumentNullException("source");
			if (selector == null) throw new ArgumentNullException("selector");

			comparer = comparer ?? Cmp<TKey>.Default;
			using (var src_iter = source.GetEnumerator())
			{
				if (!src_iter.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");

				int index = 0;
				int index_of_min = 0;
				var max_key = selector(src_iter.Current);
				while (src_iter.MoveNext())
				{
					++index;
					var key = selector(src_iter.Current);
					if (comparer.Compare(key, max_key) >= 0) continue;
					max_key = key;
					index_of_min = index;
				}
				return index_of_min;
			}
		}

		/// <summary>Count the number of elements in a range</summary>
		public static int Count(this IEnumerable source)
		{
			int i = 0;
			foreach (var x in source) ++i;
			return i;
		}

		/// <summary>Counts the number of items in a sequence up to a maximum of 'max_count'</summary>
		public static int CountAtMost<TSource>(this IEnumerable<TSource> source, int max_count)
		{
			return source.Take(max_count).Count();
		}

		/// <summary>Counts the number of items in a sequence until 'pred' returns false</summary>
		public static int CountWhile<TSource>(this IEnumerable<TSource> source, Func<TSource,bool> pred)
		{
			int count = 0;
			foreach (var item in source)
			{
				if (pred(item)) ++count;
				else break;
			}
			return count;
		}

		/// <summary>Concatenate a single element to the end of the sequence</summary>
		[Obsolete("Use Append")] public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> source, TSource one_more)
		{
			foreach (var s in source) yield return s;
			yield return one_more;
		}

		/// <summary>Returns this collection as pairs</summary>
		public static IEnumerable<Tuple<TSource,TSource>> InPairs<TSource>(this IEnumerable<TSource> source)
		{
			var en = source.GetEnumerator();
			while (en.MoveNext())
			{
				var item1 = en.Current;
				var item2 = en.MoveNext() ? en.Current : default!;
				yield return Tuple.Create(item1,item2);
			}
		}

		/// <summary>Zip two or more collections together by cycling through 'this', then each of the given collections</summary>
		public static IEnumerable<TSource> Zip<TSource>(this IEnumerable<TSource> source, params IEnumerable<TSource>[] others)
		{
			var iters = others.Select(x => x.GetIterator()).Prepend(source.GetIterator()).ToArray();
			for (bool all_done = false; !all_done; )
			{
				all_done = true;
				foreach (var iter in iters)
				{
					if (iter.AtEnd) continue;
					yield return iter.Current;
					all_done &= !iter.MoveNext();
				}
			}
		}

		/// <summary>Zip two collections together in order defined by 'comparer'</summary>
		public static IEnumerable<T> Zip<T>(this IEnumerable<T> lhs, IEnumerable<T> rhs, IComparer<T>? comparer = null)
		{
			comparer = comparer ?? Cmp<T>.Default;

			var i = lhs.GetIterator();
			var j = rhs.GetIterator();
			for (;!i.AtEnd && !j.AtEnd;)
			{
				var cmp = comparer.Compare(i.Current,j.Current);
				if (cmp <= 0)
				{
					// lhs <= rhs, advance 'lhs'
					yield return i.Current;
					i.MoveNext();
				}
				else
				{
					// lhs > rhs, advance 'rhs'
					yield return j.Current;
					j.MoveNext();
				}
			}
			for (; !i.AtEnd; i.MoveNext())
			{
				yield return i.Current;
			}
			for (; !j.AtEnd; j.MoveNext())
			{
				yield return j.Current;
			}
		}

		/// <summary>Zip two collections together in order defined by 'comparer' and accumulated using 'combine'</summary>
		public static IEnumerable<T> ZipDistinct<T>(this IEnumerable<T> lhs, IEnumerable<T> rhs, T initial_value, Func<T,T,T> combine, IComparer<T>? comparer = null)
		{
			comparer = comparer ?? Cmp<T>.Default;

			var i = Zip(lhs, rhs, comparer).GetIterator();
			for (;!i.AtEnd;)
			{
				var accum = initial_value;

				// Accumulate the values that compare as equal
				var first = i.Current;
				for (; !i.AtEnd && comparer.Compare(first, i.Current) == 0; i.MoveNext())
					accum = combine(accum, i.Current);

				// Return each distinct element
				yield return accum;
			}
		}

		/// <summary>Zip two collections together in order defined by 'comparer' and accumulated using 'combine'</summary>
		public static IEnumerable<T> ZipAccumulate<T>(this IEnumerable<T> lhs, IEnumerable<T> rhs, T initial_value, Func<T,T,T> combine, IComparer<T>? comparer = null)
		{
			comparer = comparer ?? Cmp<T>.Default;
			var accum = initial_value;

			var i = Zip(lhs, rhs, comparer).GetIterator();
			for (;!i.AtEnd;)
			{
				// Accumulate the values that compare as equal
				var first = i.Current;
				for (; !i.AtEnd && comparer.Compare(first, i.Current) == 0; i.MoveNext())
					accum = combine(accum, i.Current);

				// Return the accumulated value after each distinct element
				yield return accum;
			}
		}

		/// <summary>Compare elements with 'other' returning pairs where the elements are not equal</summary>
		public static IEnumerable<Tuple<TSource,TSource>> Differences<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> other, IEqualityComparer<TSource>? comparer = null)
		{
			comparer = comparer ?? Eql<TSource>.Default;

			var i0 = source.GetIterator();
			var i1 = other.GetIterator();
			for (; !i0.AtEnd && !i1.AtEnd; i0.MoveNext(), i1.MoveNext())
			{
				if (comparer.Equals(i0.Current, i1.Current)) continue;
				yield return Tuple.Create(i0.Current, i1.Current);
			}
			for (; !i0.AtEnd; i0.MoveNext())
				yield return Tuple.Create(i0.Current, default(TSource)!);
			for (; !i1.AtEnd; i1.MoveNext())
				yield return Tuple.Create(default(TSource)!, i1.Current);
		}

		/// <summary>Return all of the range except the last 'count' items</summary>
		public static IEnumerable<TSource> TakeAllBut<TSource>(this IEnumerable<TSource> source, int count)
		{
			var queue = new Queue<TSource>();
			foreach (var x in source)
			{
				if (queue.Count == count) yield return queue.Dequeue();
				queue.Enqueue(x);
			}
		}

		/// <summary>Return 'count' items from 'source'. If not enough, pad to 'count' using 'pad'</summary>
		public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count, TSource pad)
		{
			foreach (var x in source)
			{
				yield return x;
				if (--count == 0) break;
			}
			for (; count-- != 0;)
			{
				yield return pad;
			}
		}

		/// <summary>Normalise the values in this array to 'to'</summary>
		public static IEnumerable<float> Normalise(this IEnumerable<float> source, float to = 1f)
		{
			var max = source.Max();
			return source.Select(x => x * to / max);
		}

		/// <summary>Normalise the values in this array to 'to'</summary>
		public static IEnumerable<double> Normalise(this IEnumerable<double> source, double to = 1.0)
		{
			var max = source.Max();
			return source.Select(x => x * to / max);
		}

		/// <summary>Enumerate with an included index</summary>
		public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
		{
			int index = 0;
			return source.Select(x => (x, index++));
		}

		/// <summary>Batches items in a collection in groups >= 'batch_size'.</summary>
		public static IEnumerable<IList<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int batch_size, Func<TSource, bool>? filter = null, bool auto_clear = true)
		{
			var batch = new List<TSource>();
			filter = filter ?? (x => true);

			// Read from 'source' and group into 'batch'
			foreach (var item in source)
			{
				if (!filter(item))
					continue;

				batch.Add(item);
				if (batch.Count >= batch_size)
				{
					yield return batch;
					if (auto_clear) batch.Clear();
				}
			}

			// Return the left overs
			if (batch.Count != 0)
				yield return batch;
		}

		/// <summary>Group into sets where adjacent items satisfy 'selector'</summary>
		public static IEnumerable<IGrouping<TKey, TSource>> GroupByAdjacent<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : notnull
		{
			var iter = source.GetEnumerator();
			if (!iter.MoveNext())
				yield break;

			for (var more = true; more;)
			{
				var grp = new GroupByAdjacentCollection<TKey, TSource>(selector(iter.Current)) { iter.Current };
				for (; (more = iter.MoveNext()) && grp.Key.Equals(selector(iter.Current));)
					grp.Add(iter.Current);

				yield return grp;
			}
		}
		private class GroupByAdjacentCollection<TKey, TElement> :List<TElement>, IGrouping<TKey, TElement>
		{
			public GroupByAdjacentCollection(TKey key) { Key = key; }
			public TKey Key { get; private set; }
		}
	}

	public enum ESequenceOrder
	{
		Increasing,
		StrictlyIncreasing,
		Decreasing,
		StrictlyDecreasing,
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;

	[TestFixture]
	public class TestEnumerableExtns
	{
		[Test]
		public void WithIndex()
		{
			var ints = new[] { 1, 2, 3, 4, 5 };
			foreach (var (i, idx) in ints.WithIndex())
				Assert.Equal(i, idx + 1);
		}
		[Test]
		public void InPairs()
		{
			var ints = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			var sums = new List<int>();
			foreach (var pair in ints.InPairs())
				sums.Add(pair.Item1 + pair.Item2);

			Assert.Equal(5, sums.Count);
			Assert.Equal(3, sums[0]);
			Assert.Equal(7, sums[1]);
			Assert.Equal(11, sums[2]);
			Assert.Equal(15, sums[3]);
			Assert.Equal(9, sums[4]);
		}
		[Test]
		public void Zip()
		{
			var a0 = new[] { 1, 2, 4, 6, 10 };
			var a1 = new[] { 1, 3, 4, 7, 8 };
			var r0 = new[] { 1, 1, 2, 3, 4, 4, 6, 7, 8, 10 };
			var r1 = a0.Zip(a1, Cmp<int>.From((l, r) => l < r));
			Assert.True(r0.SequenceEqual(r1));

			var r2 = new[] { 2, 5, 8, 13, 8, 10 };
			var r3 = Enumerable_.ZipDistinct(a0, a1, 0, (l, r) => l + r, Cmp<int>.From((l, r) => (l / 2).CompareTo(r / 2)));
			Assert.True(r2.SequenceEqual(r3));

			var r4 = new[] { 2, 7, 15, 28, 36, 46 };
			var r5 = Enumerable_.ZipAccumulate(a0, a1, 0, (l, r) => l + r, Cmp<int>.From((l, r) => (l / 2).CompareTo(r / 2)));
			Assert.True(r2.SequenceEqual(r3));
		}
		[Test]
		public void Differences()
		{
			var a0 = new[] { 1, 2, 3, 4, 5 };
			var a1 = new[] { 1, 3, 3, 4, 6, 7 };
			var r0 = new[] { 2, 3, 5, 6, 0, 7 };
			var r1 = a0.Differences(a1).SelectMany(x => new[] { x.Item1, x.Item2 }).ToArray();

			Assert.True(r0.SequenceEqual(r1));
		}
		[Test]
		public void MinMaxBy()
		{
			var a0 = new[] { 4, 2, 7, 1, 8, 3, 4, 6, 8, 9 };

			Assert.Equal(1, a0.MinBy(x => x));
			Assert.Equal(2, a0.MinBy(x => (x % 2) == 0 ? x : 1000));
			Assert.Equal(9, a0.MinBy(x => 10 - x));

			Assert.Equal(9, a0.MaxBy(x => x));
			Assert.Equal(8, a0.MaxBy(x => (x % 2) == 0 ? x : -1000));
			Assert.Equal(1, a0.MaxBy(x => 10 - x));

			Assert.Equal(3, a0.IndexOfMinBy(x => x));
			Assert.Equal(1, a0.IndexOfMinBy(x => (x % 2) == 0 ? x : 1000));
			Assert.Equal(9, a0.IndexOfMinBy(x => 10 - x));

			Assert.Equal(9, a0.IndexOfMaxBy(x => x));
			Assert.Equal(4, a0.IndexOfMaxBy(x => (x % 2) == 0 ? x : -1000));
			Assert.Equal(3, a0.IndexOfMaxBy(x => 10 - x));
		}
		[Test]
		public void ExceptBy()
		{
			var i0 = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			var i1 = new[] { 1, 3, 5, 7, 9 };

			var cmp = Eql<int>.From((l, r) => l + 1 == r);

			var res = i0.ExceptBy(i1, cmp).ToArray();
			Assert.True(res.SequenceEqual(new[] { 1, 3, 5, 7, 9 }));

			// Notice how the built in one doesn't actually work...
			var wrong = i0.Except(i1, cmp).ToArray();
			Assert.False(wrong.SequenceEqual(new[] { 1, 3, 5, 7, 9 }));
		}
		[Test]
		public void ConvertTo()
		{
			var i0 = new int[] { 1, 2, 3, 4, 5, 6, 7 };
			var res = i0.ConvertTo<byte>().ToArray();
			Assert.True(res.SequenceEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7 }));
		}
		[Test]
		public void SequenceEqual()
		{
			var s0 = new[] { 1, 1, 2, 2, 3, 4, 6, 8, 10 };
			var s1 = new[] { 1, 1, 2, 2, 3, 4, 5, 7, 9 };
			var s2 = new[] { 2, 2, 3, 4, 5, 7, 9, 1, 1 };
			var s3 = new[] { 2, 2, 3, 4, 5, 7, 9, 1, 1, 1 };

			Assert.True(s0.SequenceEqual(s1, 6));
			Assert.True(!s0.SequenceEqual(s1));
			Assert.True(s1.SequenceEqualUnordered(s2));
			Assert.True(!s1.SequenceEqualUnordered(s3));
			Assert.True(!s3.SequenceEqualUnordered(s1));
		}
		[Test]
		public void IsOrdered()
		{
			var s0 = new[] { 1, 3, 2, 5, 3, 2, 1, 8, 10 };
			Assert.False(s0.IsOrdered(ESequenceOrder.Increasing));
			Assert.False(s0.IsOrdered(ESequenceOrder.StrictlyIncreasing));
			Assert.False(s0.IsOrdered(ESequenceOrder.Decreasing));
			Assert.False(s0.IsOrdered(ESequenceOrder.StrictlyDecreasing));

			var s1 = new[] { 1, 1, 2, 2, 3, 4, 6, 8, 10 };
			Assert.True (s1.IsOrdered(ESequenceOrder.Increasing));
			Assert.False(s1.IsOrdered(ESequenceOrder.StrictlyIncreasing));
			Assert.False(s1.IsOrdered(ESequenceOrder.Decreasing));
			Assert.False(s1.IsOrdered(ESequenceOrder.StrictlyDecreasing));

			var s2 = new[] { 1, 2, 4, 5, 8, 9, 10, 12, 13 };
			Assert.True (s2.IsOrdered(ESequenceOrder.Increasing));
			Assert.True (s2.IsOrdered(ESequenceOrder.StrictlyIncreasing));
			Assert.False(s2.IsOrdered(ESequenceOrder.Decreasing));
			Assert.False(s2.IsOrdered(ESequenceOrder.StrictlyDecreasing));

			var s3 = new[] { 10, 9, 9, 7, 6, 6, 6, 4, 2 };
			Assert.False(s3.IsOrdered(ESequenceOrder.Increasing));
			Assert.False(s3.IsOrdered(ESequenceOrder.StrictlyIncreasing));
			Assert.True (s3.IsOrdered(ESequenceOrder.Decreasing));
			Assert.False(s3.IsOrdered(ESequenceOrder.StrictlyDecreasing));

			var s4 = new[] { 10, 9, 8, 7, 6, 4, 3, 1, 0 };
			Assert.False(s4.IsOrdered(ESequenceOrder.Increasing));
			Assert.False(s4.IsOrdered(ESequenceOrder.StrictlyIncreasing));
			Assert.True (s4.IsOrdered(ESequenceOrder.Decreasing));
			Assert.True (s4.IsOrdered(ESequenceOrder.StrictlyDecreasing));
		}
		[Test]
		public void Batch()
		{
			var ints = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			{
				var b = ints.Batch(4).GetEnumerator();

				Assert.True(b.MoveNext());
				Assert.Equal(new[] { 1, 2, 3, 4 }, b.Current);

				Assert.True(b.MoveNext());
				Assert.Equal(new[] { 5, 6, 7, 8 }, b.Current);

				Assert.True(b.MoveNext());
				Assert.Equal(new[] { 9 }, b.Current);

				Assert.False(b.MoveNext());
			}
			{
				var b = ints.Batch(5, i => i != 3, auto_clear: false).GetEnumerator();

				Assert.True(b.MoveNext());
				Assert.Equal(new[] { 1, 2, 4, 5, 6 }, b.Current);

				Assert.True(b.MoveNext());
				Assert.Equal(new[] { 1, 2, 4, 5, 6, 7 }, b.Current);

				b.Current.Clear();

				Assert.True(b.MoveNext());
				Assert.Equal(new[] { 8, 9 }, b.Current);

				Assert.False(b.MoveNext());
			}
		}
		[Test]
		public void GroupByAdjacent()
		{
			var ints = new[] { 1, 1, 2, 3, 3, 3, 4, 3, 3, 1, 1, 2 };
			var iter = ints.GroupByAdjacent(x => x).GetEnumerator();

			Assert.True(iter.MoveNext());
			Assert.Equal(new[] { 1, 1 }, iter.Current);

			Assert.True(iter.MoveNext());
			Assert.Equal(new[] { 2 }, iter.Current);

			Assert.True(iter.MoveNext());
			Assert.Equal(new[] { 3, 3, 3 }, iter.Current);

			Assert.True(iter.MoveNext());
			Assert.Equal(new[] { 4 }, iter.Current);

			Assert.True(iter.MoveNext());
			Assert.Equal(new[] { 3, 3 }, iter.Current);

			Assert.True(iter.MoveNext());
			Assert.Equal(new[] { 1, 1 }, iter.Current);

			Assert.True(iter.MoveNext());
			Assert.Equal(new[] { 2 }, iter.Current);
		}
	}
}
#endif
