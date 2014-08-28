//***************************************************
// Event Batcher
//  Copyright (c) Rylogic Ltd 2011
//***************************************************

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using pr.extn;

namespace pr.common
{
	// Used to group bursts of events into a few events.
	// EventBatcher does the following:
	// - Trigger on the first event received, (optional, see TriggerOnFirst)
	// - Collect subsequent events,
	// - Trigger every 'Delay' interval if events have been received since the last trigger
	
	public class EventBatcher :IDisposable
	{
		private readonly object m_lock;

		/// <summary>Condition variable to signal shutdown</summary>
		private bool m_shutdown;

		/// <summary>The number of times Signal has been called since 'Action' was last raised</summary>
		private int m_count;

		public  EventBatcher()                                                     :this(TimeSpan.FromMilliseconds(10))               {}
		public  EventBatcher(EventHandler action)                                  :this(action, TimeSpan.FromMilliseconds(10))       {}
		public  EventBatcher(EventHandler action, TimeSpan delay)                  :this(() => action(null,null), delay)              {}
		public  EventBatcher(Action action)                                        :this(action, TimeSpan.FromMilliseconds(10))       {}
		public  EventBatcher(Action action, TimeSpan delay)                        :this(delay, Dispatcher.CurrentDispatcher, action) {}
		public  EventBatcher(Action action, TimeSpan delay, Dispatcher dispatcher) :this(delay, dispatcher, action)                   {}
		public  EventBatcher(TimeSpan delay)                                       :this(delay, Dispatcher.CurrentDispatcher)         {}
		public  EventBatcher(TimeSpan delay, Dispatcher dispatcher)                :this(delay, dispatcher,null)                      {}
		private EventBatcher(TimeSpan delay, Dispatcher dispatcher, Action action)
		{
			if (dispatcher == null)
				throw new ArgumentNullException("dispatcher","dispatcher can't be null");
			
			m_lock           = new object();
			m_shutdown       = false;
			m_dispatcher     = dispatcher;
			m_delay          = delay;
			m_count          = 0;
			Immediate        = false;
			TriggerOnFirst   = false;

			if (action != null)
				Action += action;
		}
		public void Dispose()
		{
			m_shutdown = true;
		}

		/// <summary>The callback called when events have been signalled</summary>
		public event Action Action;

		/// <summary>If true, 'Action' will be called on the first signal in a batch. If false, will be called after 'Delay'</summary>
		public bool TriggerOnFirst { get; set; }

		/// <summary>The time between subsequent Action invocations</summary>
		public TimeSpan Delay { get { return m_delay; } }
		private readonly TimeSpan m_delay;

		/// <summary>The thread context in which to invoke 'Action'</summary>
		public Dispatcher Dispatcher { get { return m_dispatcher; } }
		private readonly Dispatcher m_dispatcher;

		/// <summary>Toogle switch for batching on/off</summary>
		public bool Immediate
		{
			get { return m_immediate || AllImmediate; }
			set { m_immediate = value; }
		}
		private bool m_immediate;

		/// <summary>Global switch to make all event batchers immediate or not (use for debugging)</summary>
		public static bool AllImmediate { get; set; }

		/// <summary>
		/// Signal the event. Signal can be called multiple times.
		/// Note: this can be called from any thread, the resulting event will be marshalled to the Dispatcher
		/// provided in the constructor of the event batcher</summary>
		public void Signal(object sender = null, EventArgs args = null)
		{
			// If immediate mode is enabled, call Action now
			if (Immediate)
			{
				if (Action == null || m_shutdown) return;
				m_dispatcher.Invoke(Action);
				return;
			}

			// Increment the signal count
			// If this is the first of a batch, start a dispatch timer
			if (Interlocked.Increment(ref m_count) == 1)
			{
				if (TriggerOnFirst)
				{
					if (Action == null || m_shutdown) return;
					m_dispatcher.BeginInvoke(Action);
				}
				
				m_dispatcher.BeginInvokeDelayed(() =>
					{
						var count = Interlocked.Exchange(ref m_count, 0);

						// If there's still an outstanding signal count, raise the action
						if (count > 1 || (count == 1 && !TriggerOnFirst))
						{
							if (Action == null || m_shutdown) return;
							Action();
						}
					}, Delay);
			}
		}
	}
/*
	public class EventBatcher :IDisposable
	{
		private readonly Dispatcher m_dispatcher;
		private readonly TimeSpan m_delay;
		private int m_issue;
		private int m_actioned_issue;
		private bool m_disposed;

		/// <summary>The callback called when this event has been signalled</summary>
		public event Action Action;

		/// <summary>
		/// The maximum number of times Signal can be called before 'Action' is called.
		/// Used to prevent an endless stream of Signals resulting in Action never being called</summary>
		public int MaxSignalsBeforeAction { get; set; }

		public  EventBatcher()                                                     :this(TimeSpan.FromMilliseconds(10))               {}
		public  EventBatcher(EventHandler action)                                  :this(action, TimeSpan.FromMilliseconds(10))       {}
		public  EventBatcher(EventHandler action, TimeSpan delay)                  :this(() => action(null,null), delay)              {}
		public  EventBatcher(Action action)                                        :this(action, TimeSpan.FromMilliseconds(10))       {}
		public  EventBatcher(Action action, TimeSpan delay)                        :this(delay, Dispatcher.CurrentDispatcher, action) {}
		public  EventBatcher(Action action, TimeSpan delay, Dispatcher dispatcher) :this(delay, dispatcher, action)                   {}
		public  EventBatcher(TimeSpan delay)                                       :this(delay, Dispatcher.CurrentDispatcher)         {}
		public  EventBatcher(TimeSpan delay, Dispatcher dispatcher)                :this(delay, dispatcher,null)                      {}
		private EventBatcher(TimeSpan delay, Dispatcher dispatcher, Action action)
		{
			if (dispatcher == null)
				throw new ArgumentNullException("dispatcher","dispatcher can't be null");

			m_dispatcher           = dispatcher;
			m_delay                = delay;
			m_issue                = 0;
			m_actioned_issue       = 0;
			m_disposed             = false;
			MaxSignalsBeforeAction = int.MaxValue;
			Immediate              = false;

			if (action != null)
				Action += action;
		}
		public void Dispose()
		{
			m_disposed = true;
		}

		/// <summary>Toogle switch for batching on/off</summary>
		public bool Immediate { get; set; }

		/// <summary>
		/// Signal the event. Signal can be called multiple times during the processing of a windows message.
		/// The event will be delayed to a later windows message and will only be called once.
		/// Note: this can be called from any thread, the resulting event will be marshalled to the Dispatcher
		/// provided in the constructor of the event batcher</summary>
		public void Signal(object sender = null, EventArgs args = null)
		{
			if (Immediate)
			{
				Action();
			}
			else
			{
				var issue = Interlocked.Increment(ref m_issue);
				m_dispatcher.BeginInvokeDelayed(() =>
					{
						if (Action == null || m_disposed) return;
						if (unchecked(issue - m_actioned_issue) < MaxSignalsBeforeAction &&
							Interlocked.CompareExchange(ref m_issue, issue, issue) != issue)
							return;
						Action();
						m_actioned_issue = issue;
					}, m_delay);
			}
		}
	}

 */
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using common;

	[TestFixture] public static partial class UnitTests
	{
		internal static partial class TestPathEx
		{
			[Test] public static void TestEventBatch()
			{
				var count = new int[2];
				var dis = Dispatcher.CurrentDispatcher;
				var thread_id = Thread.CurrentThread.ManagedThreadId;
				var mre_eb1 = new ManualResetEvent(false);
				var mre_eb2 = new ManualResetEvent(false);

				// Not trigger on first, expect one call after the delay period
				var eb1 = new EventBatcher(() =>
					{
						if (thread_id != Thread.CurrentThread.ManagedThreadId)
							throw new Exception("Event Batch should be called in the thread context that the batcher was created in");
						
						++count[0];
						mre_eb1.Set();
					}){TriggerOnFirst = false};

				// Trigger on first, expect one call at the start, and one after the delay period
				var eb2 = new EventBatcher(() =>
					{
						if (thread_id != Thread.CurrentThread.ManagedThreadId)
							throw new Exception("Event Batch should be called in the thread context that the batcher was created in");

						++count[1];
						eb1.Signal();
						mre_eb2.Set();
					}){TriggerOnFirst = true};

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

				// Don't Signal() from this thread, need to test cross-thread support

				Assert.AreEqual(true, mre_eb1.WaitOne(0));
				Assert.AreEqual(true, mre_eb2.WaitOne(0));
				Assert.AreEqual(1, count[0]); // !TriggerOnFirst
				Assert.AreEqual(2, count[1]); //  TriggerOnFirst
			}
		}
	}
}

#endif