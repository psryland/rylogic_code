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
		private int m_actioned_issue;

		/// <summary>The callback called when this event has been signalled</summary>
		public event Action Action;

		/// <summary>
		/// The maximum number of times Signal can be called before 'Action' is called.
		/// Used to prevent an endless stream of Signals resulting in Action never being called</summary>
		public int MaxSignalsBeforeAction { get; set; }

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
			m_actioned_issue = 0;
			MaxSignalsBeforeAction = int.MaxValue;
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
					if (Action == null) return;
					if (unchecked(issue - m_actioned_issue) < MaxSignalsBeforeAction &&
						Interlocked.CompareExchange(ref m_issue, issue, issue) != issue)
						return;
					Action();
					m_actioned_issue = issue;
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
				var dis = Dispatcher.CurrentDispatcher;
				var thread_id = Thread.CurrentThread.ManagedThreadId;
				var mre_eb1 = new ManualResetEvent(false);
				var mre_eb2 = new ManualResetEvent(false);

				var eb1 = new EventBatcher(() =>
					{
						Assert.AreEqual(thread_id, Thread.CurrentThread.ManagedThreadId);
						Assert.AreEqual(1, count[0]);
						mre_eb1.Set();
					});

				var eb2 = new EventBatcher(() =>
					{
						Assert.AreEqual(thread_id, Thread.CurrentThread.ManagedThreadId);
						count[0]++;
						eb1.Signal();
						mre_eb2.Set();
					});

				ThreadPool.QueueUserWorkItem(x =>
					{
						for (var i = 0; i != 10; ++i)
							eb2.Signal();

						mre_eb1.WaitOne();
						mre_eb2.WaitOne();
						dis.BeginInvokeShutdown(DispatcherPriority.Normal);
					});

				// The unit test framework runs the test in a worker thread.
				// Dispatcher.CurrentDispatcher causes a new dispatcher to be
				// created but it isn't running. Calling Run starts a message
				// loop for this thread
				Dispatcher.Run();

				// Don't Signal() from this thread, as it hides cross-thread behaviour

				Assert.AreEqual(true, mre_eb1.WaitOne(0));
				Assert.AreEqual(true, mre_eb2.WaitOne(0));
				Assert.AreEqual(1, count[0]);
			}
		}
	}
}

#endif