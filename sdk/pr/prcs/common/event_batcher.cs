//***************************************************
// Event Batcher
//  Copyright © Rylogic Ltd 2011
//***************************************************

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using Timer = System.Timers.Timer;

namespace pr.common
{
	public class EventBatcher
	{
		private readonly Timer m_timer;

		/// <summary>True when the event batcher has been signalled</summary>
		public bool Signalled { get { return Interlocked.CompareExchange(ref m_signal, 1, 1) == 1; } }
		private int m_signal;

		/// <summary>The callback called when this event has been signalled</summary>
		public event Action Action;

		public EventBatcher() :this(100) {}
		public EventBatcher(int delay_ms) :this(delay_ms, Dispatcher.CurrentDispatcher) {}
		public EventBatcher(int delay_ms, Dispatcher dispatcher)
		{
			if (dispatcher == null) throw new ArgumentNullException("dispatcher","dispatcher can't be null");
			m_signal = 0;
			m_timer = new Timer{AutoReset = false, Enabled = false, Interval = delay_ms};
			m_timer.Elapsed += (s,e)=>
				{
					try
					{
						if (Action == null) return;
						dispatcher.BeginInvoke(Action, null);
					}
					catch (InvalidOperationException)
					{
						Debug.Assert(false, "Don't signal the event batch before the synchronizing object has a handle (if it's a control or form)");
					}
					finally
					{
						Interlocked.Exchange(ref m_signal, 0);
					}
				};
		}

		/// <summary>Signal the event. Signal can be called multiple times during the processing of a windows message.
		/// The event will be delayed to a later windows message and will only be called once.
		/// Note: this can be called from any thread, the resulting event will be marshalled to the synchronising object's
		/// thread, or run on the thread pool if no synchronising object is given</summary>
		public void Signal()
		{
			if (Interlocked.CompareExchange(ref m_signal, 1, 0) == 0)
				m_timer.Enabled = true;
		}
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using common;

	[TestFixture] internal static partial class UnitTests
	{
		internal static partial class TestPathEx
		{
			[Test] public static void TestEventBatch()
			{
				var count = new int[1];
				var thread_id = Thread.CurrentThread.ManagedThreadId;

				var eb1 = new EventBatcher(100);
				eb1.Action += () =>
					{
						Assert.AreEqual(thread_id, Thread.CurrentThread.ManagedThreadId);
						Assert.AreEqual(1, count[0]);
					};

				var eb2 = new EventBatcher(100);
				eb2.Action += () =>
					{
						Assert.AreEqual(thread_id, Thread.CurrentThread.ManagedThreadId);
						count[0]++;
						eb1.Signal();
					};

				ThreadPool.QueueUserWorkItem(x =>
					{
						for (var i = 0; i != 10; ++i)
							eb2.Signal();
					});

				for (var i = 0; i != 10; ++i)
					eb2.Signal();
			}
		}
	}
}

#endif