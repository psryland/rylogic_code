//***************************************************
// Event Batcher
//  Copyright (c) Rylogic Ltd 2011
//***************************************************

using System;
using System.Threading;
using System.Windows.Threading;
using Rylogic.Extn;

namespace Rylogic.Common
{
	/// <summary>Batch events by time</summary>
	public class EventBatcher :IDisposable
	{
		// Notes:
		// Used to group bursts of events into a few events.
		// EventBatcher does the following:
		// - Trigger on the first event received, (optional, see TriggerOnFirst)
		// - Collect subsequent events,
		// - Trigger every 'Delay' interval if events have been received since the last trigger

		/// <summary>Synchronisation</summary>
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
			m_lock           = new object();
			m_shutdown       = false;
			Dispatcher       = dispatcher ?? throw new ArgumentNullException("dispatcher","dispatcher can't be null");
			Delay            = delay;
			m_count          = 0;
			Immediate        = false;
			TriggerOnFirst   = false;
			Priority         = DispatcherPriority.Normal;

			if (action != null)
				Action += action;
		}
		public void Dispose()
		{
			m_shutdown = true;
			Action = null;
		}

		/// <summary>The callback called when events have been signalled</summary>
		public event Action Action;

		/// <summary>If true, 'Action' will be called on the first signal in a batch. If false, will be called after 'Delay'</summary>
		public bool TriggerOnFirst { get; set; }

		/// <summary>The time between subsequent Action invocations</summary>
		public TimeSpan Delay { get; set; }

		/// <summary>Priority of the action</summary>
		public DispatcherPriority Priority { get; set; }

		/// <summary>The thread context in which to invoke 'Action'</summary>
		public Dispatcher Dispatcher { get; private set; }

		/// <summary>Toggle switch for batching on/off</summary>
		public bool Immediate
		{
			get { return m_immediate || AllImmediate; }
			set { m_immediate = value; }
		}
		private bool m_immediate;

		/// <summary>Global switch to make all event batcher's immediate or not (use for debugging)</summary>
		public static bool AllImmediate { get; set; }

		/// <summary>
		/// Signal the event. Signal can be called multiple times.
		/// Note: this can be called from any thread, the resulting event will be marshalled
		/// to the Dispatcher provided in the constructor of the event batcher</summary>
		public void Signal(object sender = null, EventArgs args = null)
		{
			// Silently handle Signal calls on this object after it's shutdown
			// They can be coming from any threat.
			if (Action == null || m_shutdown)
				return;

			// If immediate mode is enabled, call Action now
			if (Immediate)
			{
				Dispatcher.Invoke(Action);
				return;
			}

			// Increment the signal count
			// If this is the first of a batch, start a dispatch timer
			if (Interlocked.Increment(ref m_count) == 1)
			{
				if (TriggerOnFirst)
				{
					// Call the action on the first signal
					// Using BeginInvoke because we don't want to block the calling thread.
					Dispatcher.BeginInvoke(new Action(() =>
					{
						if (Action == null || m_shutdown) return;
						Action();
					}));
				}

				// Add a delayed call to Action. In the meantime, repeat calls will just increment 'm_count'
				Dispatcher.BeginInvokeDelayed(new Action(() =>
				{
					// After the delay period, see how many more times we've been signalled.
					// If still only once, don't call Action again
					var count = Interlocked.Exchange(ref m_count, 0);

					// If there's still an outstanding signal count, raise the action
					if (count > 1 || (count == 1 && !TriggerOnFirst))
					{
						if (Action == null || m_shutdown) return;
						Action();
					}
				}), Delay, Priority);
			}
		}

		/// <summary>
		/// Signal the event synchronously.
		/// Note: this can be called from any thread, the resulting event will be marshalled
		/// to the Dispatcher provided in the constructor of the event batcher</summary>
		public void SignalImmediate(object sender = null, EventArgs args = null)
		{
			if (Action == null || m_shutdown) return;
			Dispatcher.Invoke(Action);
		}
	}

	/// <summary>Batch events per message pump loop</summary>
	public struct Trigger
	{
		// Notes:
		// - Much simpler than an EventBatcher
		// How to use:
		//  private Trigger m_update;
		//	public void TriggerUpdate()
		//	{
		//		if (m_update.Pending) return; // remember ' || !IsHandleCreated' if using Control.BeginInvoke
		//		m_update.Signal();
		//
		//		BeginInvoke(DoUpdate);
		//		void DoUpdate()
		//		{
		//			m_update.Actioned();
		//			...
		//			if (m_update.Pending)
		//				return; // abort, another trigger has been set
		//		}
		//	}
		// or:
		//	public void TriggerUpdate()
		//	{
		//		if (m_update.Pending) return;
		//		m_update.Signal();
		//		ThreadPool.QueueUserWorkItem(_ =>
		//		{
		//			m_update.Actioned();
		//			...
		//			if (m_update.Pending)
		//				return; // abort, another trigger has been set
		//	
		//			BeginInvoke(MergeResults);
		//			void MergeResults()
		//			{
		//				if (m_update.Pending) return;
		//				...
		//			}
		//		});
		//	}

		public bool Pending
		{
			get { return m_issue != m_in_progress; }
		}
		public void Signal()
		{
			++m_issue;
		}
		public void Actioned()
		{
			m_in_progress = m_issue;
		}

		private volatile int m_issue;
		private volatile int m_in_progress;
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Common;

	[TestFixture] public class TestEventBatcher
	{
		[Test] public void TestEventBatch()
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

			Assert.Equal(true, mre_eb1.WaitOne(0));
			Assert.Equal(true, mre_eb2.WaitOne(0));
			Assert.Equal(1, count[0]); // !TriggerOnFirst
			Assert.Equal(2, count[1]); //  TriggerOnFirst
		}
	}
}

#endif