using System;
using System.Collections.Generic;
using System.Threading;

namespace Rylogic.Container
{
	public class BlockingCollectionFat<T>
	{
		// Notes:
		//  Basically the same as BlockingCollection, except using locks (can be faster)
		//  In the unit test below, it's about 4x slower than 'BlockingCollection' though.

		private readonly Queue<T> _queue;
		private readonly object _sync_add;
		private readonly object _sync_rem;
		private readonly int _bound;
		private bool _complete;

		public BlockingCollectionFat()
			: this(int.MaxValue)
		{ }
		public BlockingCollectionFat(int bounded_capacity)
		{
			_queue = new Queue<T>();
			_sync_add = new object();
			_sync_rem = new object();
			_bound = bounded_capacity;
			_complete = false;
		}

		/// <summary>Signal that no more items will be added</summary>
		public void CompleteAdding()
		{
			// Flag as complete, and wake up consumers
			lock (_sync_add)
			{
				_complete = true;

				// Wake up all consumers
				Monitor.PulseAll(_sync_add);
			}
		}

		/// <summary>Queue an item</summary>
		public void Add(T item, CancellationToken cancel = default(CancellationToken))
		{
			lock (_sync_rem)
			{
				// Wait for room to add
				using (cancel.Register(() => cancel.ThrowIfCancellationRequested()))
				{
					for (; _queue.Count == _bound;)
						Monitor.Wait(_sync_rem);
				}

				lock (_sync_add)
				{
					// Don't allow adds after '_complete'
					if (_complete)
						throw new InvalidOperationException("CompleteAdding has already been signalled for this collection");

					_queue.Enqueue(item);

					// Wake up all consumers
					Monitor.PulseAll(_sync_add);
				}
			}
		}

		/// <summary>Queue a batch of items</summary>
		public void AddRange(IEnumerable<T> item, CancellationToken cancel = default(CancellationToken))
		{
			// Get an iterator to the range
			var iter = item.GetEnumerator();
			if (!iter.MoveNext())
				return;

			lock (_sync_rem)
			{
				for (; ; )
				{
					// Wait for room to add
					using (cancel.Register(() => cancel.ThrowIfCancellationRequested()))
					{
						for (; _queue.Count == _bound;)
							Monitor.Wait(_sync_rem);
					}

					lock (_sync_add)
					{
						// Don't allow adds after '_complete'
						if (_complete)
							throw new InvalidOperationException("CompleteAdding has already been signalled for this collection");

						for (int i = _queue.Count; i != _bound; ++i)
						{
							_queue.Enqueue(iter.Current);
							if (!iter.MoveNext())
								return;
						}

						// Wake up all consumers
						Monitor.PulseAll(_sync_add);
					}
				}
			}
		}

		/// <summary>Enumerable that pulls from the queue until complete</summary>
		public IEnumerable<T> GetConsumingEnumerable(CancellationToken cancel = default(CancellationToken))
		{
			for (;;)
			{
				var item = default(T);
				lock (_sync_add)
				{
					// Wait for items to be added
					using (cancel.Register(() => cancel.ThrowIfCancellationRequested()))
					{
						for (; _queue.Count == 0 && !_complete;)
							Monitor.Wait(_sync_add);
					}
					
					if (_queue.Count == 0 && _complete)
						yield break;

					item = _queue.Dequeue();
				}

				// Wake up all bound-limited producers
				lock (_sync_rem)
					Monitor.PulseAll(_sync_rem);

				yield return item;
			}
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Collections.Concurrent;
	using System.Linq;
	using Container;

	[TestFixture]
	public class TestBlockingCollectionFat
	{
		[Test]
		public void Test1()
		{
			const int N = 10000;
			var buf = new BlockingCollectionFat<int>(100);
			var cancel = new CancellationTokenSource();
			var tmp = new ConcurrentBag<int>();
			var semi = new SemaphoreSlim(0,4);

			// Consumers
			ThreadPool.QueueUserWorkItem(_ =>
			{
				foreach (var i in buf.GetConsumingEnumerable(cancel.Token))
					tmp.Add(i);

				semi.Release();
			});
			ThreadPool.QueueUserWorkItem(_ =>
			{
				foreach (var i in buf.GetConsumingEnumerable(cancel.Token))
					tmp.Add(i);

				semi.Release();
			});

			Thread.Yield();

			// Producers
			ThreadPool.QueueUserWorkItem(_ =>
			{
				for (int i = 0; i != N; ++i)
					buf.Add(i, cancel.Token);

				semi.Release();
			});
			ThreadPool.QueueUserWorkItem(_ =>
			{
				var list = new List<int>(N);
				for (int i = 0; i != N; ++i)
					list.Add(i);

				buf.AddRange(list, cancel.Token);
				semi.Release();
			});

			semi.Wait(cancel.Token);
			semi.Wait(cancel.Token);
			buf.CompleteAdding();
			semi.Wait(cancel.Token);
			semi.Wait(cancel.Token);

			var res = tmp.ToList();
			Assert.True(res.Count == 2 * N);
			res.Sort();

			for (int i = 0, j = 0; i != N; ++i)
			{
				Assert.True(res[j++] == i);
				Assert.True(res[j++] == i);
			}
		}
	}
}
#endif
