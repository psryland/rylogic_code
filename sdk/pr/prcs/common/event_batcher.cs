//***************************************************
// Event Batcher
//  Copyright © Rylogic Ltd 2011
//***************************************************

using System;
using System.Threading;
using System.Windows.Threading;
using pr.extn;

namespace pr.common
{
	public class EventBatcher
	{
		private readonly Dispatcher m_dispatcher;
		private readonly TimeSpan m_delay;
		private int m_issue;

		/// <summary>The callback called when this event has been signalled</summary>
		public event Action Action;

		public EventBatcher() :this(TimeSpan.FromMilliseconds(100)) {}
		public EventBatcher(Action action) :this() { Action += action; }
		public EventBatcher(Action action, TimeSpan delay) :this(delay) { Action += action; }
		public EventBatcher(Action action, TimeSpan delay, Dispatcher dispatcher) :this(delay, dispatcher) { Action += action; }
		public EventBatcher(TimeSpan delay) :this(delay, Dispatcher.CurrentDispatcher) {}
		public EventBatcher(TimeSpan delay, Dispatcher dispatcher)
		{
			if (dispatcher == null)
				throw new ArgumentNullException("dispatcher","dispatcher can't be null");

			m_dispatcher = dispatcher;
			m_delay = delay;
			m_issue = 0;
		}

		/// <summary>
		/// Signal the event. Signal can be called multiple times during the processing of a windows message.
		/// The event will be delayed to a later windows message and will only be called once.
		/// Note: this can be called from any thread, the resulting event will be marshalled to the Dispatcher
		/// provided in the constructor of the event batcher</summary>
		public void Signal(object sender = null, EventArgs args = null)
		{
			var issue = Interlocked.Increment(ref m_issue);
			m_dispatcher.BeginInvokeDelayed(() =>
				{
					if (Interlocked.CompareExchange(ref m_issue, issue, issue) != issue) return;
					if (Action == null) return;
					Action();
				}, m_delay);
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

				var eb1 = new EventBatcher();
				eb1.Action += () =>
					{
						Assert.AreEqual(thread_id, Thread.CurrentThread.ManagedThreadId);
						Assert.AreEqual(1, count[0]);
					};

				var eb2 = new EventBatcher();
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