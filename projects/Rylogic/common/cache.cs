using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using pr.util;

namespace pr.common
{
	public interface ICache<TKey,TItem> :IDisposable
	{
		/// <summary>Get/Set whether the cache should use thread locking</summary>
		bool ThreadSafe { get; set; }

		/// <summary>The number of items in the cache</summary>
		int Count { get; }

		/// <summary>Get/Set an upper limit on the number of cached items</summary>
		int Capacity { get; set; }

		/// <summary>Return performance data for the cache</summary>
		CacheStats Stats { get; }

		/// <summary>Returns true if an object with a key matching 'key' is currently in the cache</summary>
		bool IsCached(TKey key);

		/// <summary>Add an item to the cache. Returns true if the item was added</summary>
		bool Add(TKey key, TItem item);

		/// <summary>Add multiple items to the cache. Returns true if all items were added</summary>
		bool Add(IEnumerable<TKey> keys, IEnumerable<TItem> items);

		/// <summary>Remove an item from the cache (does not delete/dispose it). Returns true if an item is removed</summary>
		bool Remove(TKey key);

		/// <summary>
		/// Returns an object from the cache if available, otherwise calls 'on_miss'.
		/// If the Mode is StandardCache, the result of 'on_miss' is stored in the cache.
		/// If the Mode is ObjectPool, the returned object will not be in the cache, users
		/// need to call 'Add' to return the object to the cache.</summary>
		TItem Get(TKey key, Func<TKey,TItem> on_miss, Action<TKey,TItem> on_hit = null);

		/// <summary>Deletes (i.e. Disposes) and removes any object associated with 'key' from the cache</summary>
		void Invalidate(TKey key);

		/// <summary>Delete (i.e. Dispose) and remove all items from the cache</summary>
		void Flush();
	}

	public enum CacheMode
	{
		/// <summary>
		/// Standard cache, where objects returned in 'Get' remain in the 
		/// cache until flushed or removed. Used for performance when accessing
		/// a slow data source.</summary>
		StandardCache,

		/// <summary>
		/// An object pool removes items from the cache when they are returned
		/// from 'Get'. Users return the object to the cache when finished with
		/// it. Object pools are used for recycling expensive objects.</summary>
		ObjectPool
	}

	public struct CacheStats
	{
		public int Hits;
		public int Misses;
		public float Performance { get { return Hits / (float)(Hits + Misses); } }
		public void Clear() { Hits = Misses = 0; }
	}

	/// <summary>Generic, count limited, cache implementation</summary>
	public class Cache<TKey,TItem> :ICache<TKey,TItem>
	{
		// The cache is an ordered linked list and a map from keys to list nodes
		// The order of items in the linked list is the order of last accessed.

		/// <summary>The cache entry</summary>
		private class Entry
		{
			public TKey  Key  { get; set; }
			public TItem Item { get; set; }
			public override string ToString() { return string.Format("{0} -> {1}", Key, Item); }
		}

		/// <summary>A linked list of the cached items, sorted by most recently accessed</summary>
		private readonly LinkedList<Entry> m_cache;

		/// <summary>A lookup table from 'key' to node in 'm_cache'</summary>
		private readonly Dictionary<TKey,LinkedListNode<Entry>> m_lookup;

		/// <summary>A lock object for synchronisation</summary>
		private readonly object m_lock;

		/// <summary>The id of the thread the cache was created on (for debugging non-thread-safe use)</summary>
		private int m_thread_id;

		public Cache()
		{
			m_cache     = new LinkedList<Entry>();
			m_lookup    = new Dictionary<TKey,LinkedListNode<Entry>>();
			m_lock      = new object();
			m_thread_id = Thread.CurrentThread.ManagedThreadId;
			Mode        = CacheMode.StandardCache;
			ThreadSafe  = false;
			m_capacity  = 100;
			m_stats     = new CacheStats();
		}

		/// <summary>Calls Dispose() on all cached items</summary>
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

		/// <summary>Get/Set the behaviour of the Get method</summary>
		public CacheMode Mode { get; set; }

		/// <summary>Get/Set whether the cache should use thread locking</summary>
		public bool ThreadSafe { get; set; }

		/// <summary>The number of items in the cache</summary>
		public int Count
		{
			get { return m_cache.Count; }
		}

		/// <summary>Enumerate over the items in the cache</summary>
		public IEnumerable<TItem> CachedItems
		{
			get { return m_cache.Select(x => x.Item); }
		}

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
		private int m_capacity;

		/// <summary>Return performance data for the cache</summary>
		public CacheStats Stats
		{
			get { return m_stats; }
		}
		private CacheStats m_stats;

		/// <summary>Returns true if an object with a key matching 'key' is currently in the cache</summary>
		public bool IsCached(TKey key)
		{
			using (Lock)
				return m_lookup.ContainsKey(key);
		}

		/// <summary>Add an item to the cache. Returns true if the item was added</summary>
		public bool Add(TKey key, TItem item)
		{
			if (Capacity == 0)
				return false;

			using (Lock)
			{
				// Create a new node and add to the cache
				var node = m_cache.AddFirst(new Entry{Key = key, Item = item});
				m_lookup[key] = node;
				LimitCacheSize();
				return true;
			}
		}

		/// <summary>Add multiple items to the cache. Returns true if all items were added</summary>
		public bool Add(IEnumerable<TKey> keys, IEnumerable<TItem> items)
		{
			if (Capacity == 0)
				return false;

			using (Lock)
			{
				var k = keys.GetIterator<TKey>();
				var i = items.GetIterator<TItem>();
				for (; !k.AtEnd && !i.AtEnd; k.MoveNext(), i.MoveNext())
				{
					// Create a new node and add to the cache
					var node = m_cache.AddFirst(new Entry{Key = k.Current, Item = i.Current});
					m_lookup[k.Current] = node;
				}
				LimitCacheSize();
				return true;
			}
		}

		/// <summary>Remove an item from the cache (does not delete/dispose it). Returns true if an item is removed</summary>
		public bool Remove(TKey key)
		{
			// Use the lookup map to find the node in the cache
			LinkedListNode<Entry> node;
			using (Lock) m_lookup.TryGetValue(key, out node);
			if (node == null) return false;

			using (Lock)
				DeleteCachedItem(node, false);

			return true;
		}

		/// <summary>
		/// Returns an object from the cache if available, otherwise calls 'on_miss'.
		/// If the Mode is StandardCache, the result of 'on_miss' is stored in the cache.
		/// If the Mode is ObjectPool, the returned object will not be in the cache, users
		/// need to call 'Add' to return the object to the cache.</summary>
		public TItem Get(TKey key, Func<TKey,TItem> on_miss, Action<TKey,TItem> on_hit = null)
		{
			// Special case 0 capacity caches
			if (Capacity == 0)
				return on_miss(key);

			TItem item;

			// Use the lookup map to find the node in the cache
			LinkedListNode<Entry> node = null;
			using (Lock) m_lookup.TryGetValue(key, out node);
			if (node != null)
			{
				// Found!
				Interlocked.Increment(ref m_stats.Hits);

				// For standard caches, move the item to the head of the cache list.
				// For object pools, remove the item from the cache before returning it.
				using (Lock)
				{
					switch (Mode)
					{
					default: throw new Exception("Unknown cache mode");
					case CacheMode.StandardCache:
						m_cache.Remove(node);
						m_cache.AddFirst(node);
						break;
					case CacheMode.ObjectPool:
						DeleteCachedItem(node, false);
						break;
					}
				}

				// Allow callers to do something on a cache hit
				if (on_hit != null)
					on_hit(key, node.Value.Item);

				// Get the item to return
				item = node.Value.Item;
			}
			else
			{
				// Not found
				Interlocked.Increment(ref m_stats.Misses);

				// Cache miss, read it
				item = on_miss(key);
				if (Equals(item, default(TItem)))
					return item;

				// For standard caches, store the new item in the cache before returning it.
				// For object pools, do nothing, the user will put it in the cache when finished with it.
				switch (Mode)
				{
				default: throw new Exception("Unknown cache mode");
				case CacheMode.StandardCache:
					using (Lock)
					{
						// Check again if 'key' is not in the cache, it might have been
						// added by another thread while we weren't holding the lock.
						if (!m_lookup.TryGetValue(key, out node))
						{
							// Create a new node and add to the cache
							node = m_cache.AddFirst(new Entry { Key = key, Item = item });
							m_lookup[key] = node;
							LimitCacheSize();
						}

						// Get the item to return
						item = node.Value.Item;
					}
					break;
				case CacheMode.ObjectPool:
					break; // Return 'item'
				}
			}

			// Return the cached object.
			// For standard caches this item should really be immutable
			// For object pools, it doesn't matter because it's not in our cache now.
			return item;
		}

		/// <summary>Deletes (i.e. Disposes) and removes any object associated with 'key' from the cache</summary>
		public void Invalidate(TKey key)
		{
			if (Capacity == 0)
				return;

			using (Lock)
			{
				LinkedListNode<Entry> node;
				if (m_lookup.TryGetValue(key, out node))
					DeleteCachedItem(node, true);
			}
		}

		/// <summary>Delete (i.e. Dispose) and remove all items from the cache</summary>
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
					DeleteCachedItem(m_cache.Last, true);
			}
		}

		/// <summary>Delete a cached item. This method should only be called from within a 'Lock'</summary>
		private void DeleteCachedItem(LinkedListNode<Entry> node, bool dispose)
		{
			// Remove the node from the list/lookup
			m_lookup.Remove(node.Value.Key);
			node.List.Remove(node);

			// Dispose the item if disposable
			if (dispose)
			{
				var disposable = node.Value.Item as IDisposable;
				if (disposable != null) disposable.Dispose();
			}
		}

		/// <summary>Create a synchronisation lock</summary>
		private Scope Lock
		{
			get
			{
				Debug.Assert(ThreadSafe || m_thread_id == Thread.CurrentThread.ManagedThreadId, "Cross thread use of non-thread safe cache");
				return ThreadSafe
					? Scope.Create(() => Monitor.Enter(m_lock), () => Monitor.Exit(m_lock))
					: Scope.Create(()=>{},()=>{});
			}
		}
	}

	/// <summary>A dummy cache that does no caching</summary>
	public class PassThruCache<TKey,TItem> :ICache<TKey,TItem>
	{
		public PassThruCache()
		{ }
		public virtual void Dispose()
		{ }

		/// <summary>Get/Set whether the cache should use thread locking</summary>
		public bool ThreadSafe { get; set; }

		/// <summary>The number of items in the cache</summary>
		public int Count { get { return 0; } }

		/// <summary>Gets/Sets the maximum number of objects to store in the cache</summary>
		public int Capacity { get { return 0; } set {} }

		/// <summary>Return performance data for the cache</summary>
		public CacheStats Stats { get { return m_stats; } }
		private CacheStats m_stats;

		/// <summary>Returns true if an object with a key matching 'key' is currently in the cache</summary>
		public bool IsCached(TKey key) { return false; }

		/// <summary>Preload the cache with an item</summary>
		public bool Add(TKey key, TItem item) { return false; }

		/// <summary>Preload the cache with a range of items.</summary>
		public bool Add(IEnumerable<TKey> keys, IEnumerable<TItem> items) { return false; }

		/// <summary>Remove an item from the cache (does not delete/dispose it). Returns true if an item is removed</summary>
		public bool Remove(TKey key) { return false; }

		/// <summary>
		/// Returns an object from the cache if available, otherwise calls 'on_miss'.
		/// If the Mode is StandardCache, the result of 'on_miss' is stored in the cache.
		/// If the Mode is ObjectPool, the returned object will not be in the cache, users
		/// need to call 'Add' to return the object to the cache.</summary>
		public TItem Get(TKey key, Func<TKey,TItem> on_miss, Action<TKey,TItem> on_hit = null)
		{
			Interlocked.Increment(ref m_stats.Misses);
			return on_miss(key);
		}

		/// <summary>Deletes (i.e. Disposes) and removes any object associated with 'key' from the cache</summary>
		public void Invalidate(TKey key) {}

		/// <summary>Delete (i.e. Dispose) and remove all items from the cache</summary>
		public void Flush() {}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Linq;
	using System.Threading.Tasks;
	using common;

	[TestFixture] public class TestCache
	{
		[Test] public void ObjectPoolBehaviour()
		{
			var cache = new Cache<int, object>{Mode = CacheMode.ObjectPool};
			cache.Add(1, (object)1);
			cache.Add(2, (object)2);
			cache.Add(3, (object)3);

			var item = cache.Get(2, k => (object)k);
			Assert.True((int)item == 2);
			Assert.True(cache.IsCached(2) == false);
			cache.Add(2, item);

			item = cache.Get(4, k => (object)k);
			Assert.True((int)item == 4);
			Assert.True(cache.IsCached(4) == false);
		}
		[Test] public void TestCachePreloading()
		{
			var cache = new Cache<int, string>();
			cache.Add(Enumerable.Range(0, 100), Enumerable.Range(0, 100).Select(x => x.ToString()));
			for (var i = 0; i != 100; ++i)
				Assert.True(cache.IsCached(i));
		}
		[Test] public void TestThreadSafety()
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
				Assert.True(cache.IsCached(i));
			}
		}
	}
}
#endif