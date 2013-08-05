using System;
using System.Collections.Generic;
using pr.extn;

namespace pr.common
{
	public interface ICache<TKey,TItem>
	{
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
		public int Hits { get; set; }
		public int Misses { get; set; }
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

		/// <summary>The number of items in the cache</summary>
		public int Count { get { return m_cache.Count; } }

		/// <summary>Get/Set an upper limit on the number of cached items</summary>
		public int Capacity
		{
			get { return m_capacity; }
			set { m_capacity = value; LimitCacheSize(); }
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
				m_stats.Hits++;

				// If found, move it to the head of the cache list
				m_cache.Remove(node);
				m_cache.AddFirst(node);
				
				// Allow callers to do something on a cache hit
				if (on_hit != null)
					on_hit(key, node.Value.Item);
			}
			else
			{
				m_stats.Misses++;
				
				// Cache miss, read it
				var item = on_miss(key);
				if (Equals(item, default(TItem)))
					return default(TItem);
					
				// Create a new node and add to the cache
				node = m_cache.AddFirst(new Entry{Key = key, Item = item});
				m_lookup[key] = node;
				LimitCacheSize();
			}
			return node.Value.Item; // Danger, this should really be immutable
		}

		/// <summary>Handles notification from the database that a row has changed</summary>
		public void Invalidate(TKey key)
		{
			// See if we have this row cached and if so, remove it
			LinkedListNode<Entry> node;
			if (m_lookup.TryGetValue(key, out node))
				DeleteCachedItem(node);
		}

		/// <summary>Removed all cached items from the cache</summary>
		public void Flush()
		{
			int cap = Capacity;
			Capacity = 0;
			Capacity = cap;
		}

		/// <summary>Reduces the size of the cache to 'MaxCachedItemsCount' items</summary>
		private void LimitCacheSize()
		{
			while (m_cache.Count > Capacity)
				DeleteCachedItem(m_cache.Last);
		}

		/// <summary>Delete a cached item</summary>
		private void DeleteCachedItem(LinkedListNode<Entry> node)
		{
			m_lookup.Remove(node.Value.Key);
			node.List.Remove(node);
			
			var disposable = node.Value.Item as IDisposable;
			if (disposable != null) disposable.Dispose();
		}
	}

	/// <summary>A dummy cache that does no caching</summary>
	public class PassThruCache<TKey,TItem> :ICache<TKey,TItem>
	{
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
			m_stats.Misses++;
			return on_miss(key);
		}

		/// <summary>Remove a cached item with a given key</summary>
		public void Invalidate(TKey key) {}

		/// <summary>Removed all cached items from the cache</summary>
		public void Flush() {}
	}
}