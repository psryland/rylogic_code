using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using pr.util;

namespace pr.common
{
	public interface ICache<TKey,TItem>
	{
		/// <summary>Get/Set whether the cache should use thread locking</summary>
		bool ThreadSafe { get; set; }

		/// <summary>The number of items in the cache</summary>
		int Count { get; }

		/// <summary>Get/Set an upper limit on the number of cached items</summary>
		int Capacity { get; set; }

		/// <summary>Return performance data for the cache</summary>
		CacheStats Stats { get; }

		/// <summary>Returns true if an object with the given key is in the cache</summary>
		bool IsCached(TKey key);

		/// <summary>Returns an object from the cache if available, otherwise calls 'on_miss' and caches the result</summary>
		TItem Get(TKey key, Func<TKey,TItem> on_miss, Action<TKey,TItem> on_hit = null);

		/// <summary>Handles notification from the database that a row has changed</summary>
		void Invalidate(TKey key);

		/// <summary>Removed all cached items from the cache</summary>
		void Flush();
	}

	public struct CacheStats
	{
		public int Hits;
		public int Misses;
		public float Performance { get { return Hits / (float)(Hits + Misses); } }
		public void Clear() { Hits = Misses = 0; }
	}

	/// <summary>Generic, count limited, cache implementation</summary>
	public class Cache<TKey,TItem> :ICache<TKey,TItem> ,IDisposable
	{
		private class Entry
		{
			public TKey  Key  { get; set; }
			public TItem Item { get; set; }
		}

		/// <summary>A linked list of the cached items, sorted by most recently accessed</summary>
		private readonly LinkedList<Entry> m_cache = new LinkedList<Entry>();

		/// <summary>A lookup table from 'key' to node in 'm_cache'</summary>
		private readonly Dictionary<TKey,LinkedListNode<Entry>> m_lookup = new Dictionary<TKey,LinkedListNode<Entry>>();

		/// <summary>A lock object for synchronisation</summary>
		private readonly object m_lock = new object();

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			if (typeof(IDisposable).IsAssignableFrom(typeof(TItem)))
			{
				foreach (var e in m_cache)
				{
					var doomed = e.Item as IDisposable;
					if (doomed != null) doomed.Dispose();
				}
			}
			m_cache.Clear();
			m_lookup.Clear();
		}

		/// <summary>Create a synchronisation lock</summary>
		private Scope Lock
		{
			get
			{
				return ThreadSafe
					? Scope.Create(() => Monitor.Enter(m_lock), () => Monitor.Exit(m_lock))
					: Scope.Create(()=>{},()=>{});
			}
		}

		/// <summary>Get/Set whether the cache should use thread locking</summary>
		public bool ThreadSafe { get; set; }

		/// <summary>The number of items in the cache</summary>
		public int Count { get { return m_cache.Count; } }

		/// <summary>Get/Set an upper limit on the number of cached items</summary>
		public int Capacity
		{
			get { return m_capacity; }
			set
			{
				using (Lock)
				{
					m_capacity = value;
					LimitCacheSize();
				}
			}
		}
		private int m_capacity = 100;

		/// <summary>Return performance data for the cache</summary>
		public CacheStats Stats { get { return m_stats; } }
		private CacheStats m_stats = new CacheStats();

		/// <summary>Returns true if an object with the given key is in the cache</summary>
		public bool IsCached(TKey key) { return m_lookup.ContainsKey(key); }

		/// <summary>Returns an object from the cache if available, otherwise calls 'on_miss' and caches the result</summary>
		public TItem Get(TKey key, Func<TKey,TItem> on_miss, Action<TKey,TItem> on_hit = null)
		{
			// Use the lookup map to find the node in the cache
			LinkedListNode<Entry> node;
			if (m_lookup.TryGetValue(key, out node))
			{
				Interlocked.Increment(ref m_stats.Hits);

				// If found, move it to the head of the cache list
				using (Lock)
				{
					m_cache.Remove(node);
					m_cache.AddFirst(node);
				}

				// Allow callers to do something on a cache hit
				if (on_hit != null)
					on_hit(key, node.Value.Item);
			}
			else
			{
				Interlocked.Increment(ref m_stats.Misses);

				// Cache miss, read it
				var item = on_miss(key);
				if (Equals(item, default(TItem)))
					return default(TItem);

				using (Lock)
				{
					// Check again if 'key' is not in the cache, it might have been added by another thread
					if (!m_lookup.TryGetValue(key, out node))
					{
						// Create a new node and add to the cache
						node = m_cache.AddFirst(new Entry{Key = key, Item = item});
						m_lookup[key] = node;
						LimitCacheSize();
					}
				}
			}
			return node.Value.Item; // Danger, this should really be immutable
		}

		/// <summary>Handles notification from the database that a row has changed</summary>
		public void Invalidate(TKey key)
		{
			using (Lock)
			{
				// See if we have this row cached and if so, remove it
				LinkedListNode<Entry> node;
				if (m_lookup.TryGetValue(key, out node))
					DeleteCachedItem(node);
			}
		}

		/// <summary>Removed all cached items from the cache</summary>
		public void Flush()
		{
			using (Lock)
			{
				int cap = Capacity;
				Capacity = 0;
				Capacity = cap;
			}
		}

		/// <summary>Reduces the size of the cache to 'MaxCachedItemsCount' items</summary>
		private void LimitCacheSize()
		{
			using (Lock)
			{
				while (m_cache.Count > Capacity)
					DeleteCachedItem(m_cache.Last);
			}
		}

		/// <summary>Delete a cached item</summary>
		private void DeleteCachedItem(LinkedListNode<Entry> node)
		{
			using (Lock)
			{
				m_lookup.Remove(node.Value.Key);
				node.List.Remove(node);

				var disposable = node.Value.Item as IDisposable;
				if (disposable != null) disposable.Dispose();
			}
		}
	}

	/// <summary>A dummy cache that does no caching</summary>
	public class PassThruCache<TKey,TItem> :ICache<TKey,TItem>
	{
		/// <summary>Get/Set whether the cache should use thread locking</summary>
		public bool ThreadSafe { get; set; }

		/// <summary>The number of items in the cache</summary>
		public int Count { get { return 0; } }

		/// <summary>Gets/Sets the maximum number of objects to store in the cache</summary>
		public int Capacity { get { return 0; } set {} }

		/// <summary>Return performance data for the cache</summary>
		public CacheStats Stats { get { return m_stats; } }
		private CacheStats m_stats;

		/// <summary>Returns true if an object with a primary key matching 'key' is currently cached</summary>
		public bool IsCached(TKey key) { return false; }

		/// <summary>Returns an object from the cache if available, otherwise calls 'on_miss' and caches the result</summary>
		public TItem Get(TKey key, Func<TKey,TItem> on_miss, Action<TKey,TItem> on_hit = null)
		{
			Interlocked.Increment(ref m_stats.Misses);
			return on_miss(key);
		}

		/// <summary>Remove a cached item with a given key</summary>
		public void Invalidate(TKey key) {}

		/// <summary>Removed all cached items from the cache</summary>
		public void Flush() {}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using System.Threading.Tasks;
	using common;

	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestCache
		{
			[Test] public static void TestThreadSafety()
			{
				var cache = new Cache<int,string>{ThreadSafe = true};
				var tasks = new List<Task>();
				for (var i = 0; i != 1000; ++i)
				{
					var index = i;
					var task = new Task(() => cache.Get(index % 10, k => k.ToString(CultureInfo.InvariantCulture)));
					tasks.Add(task);
					task.Start();
				}

				Task.WaitAll(tasks.ToArray());

				for (var i = 0; i != 10; ++i)
				{
					Assert.IsTrue(cache.IsCached(i));
				}
			}
		}
	}
}
#endif