//***************************************************
// Event Batcher
//  Copyright © Rylogic Ltd 2011
//***************************************************

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace pr.util
{
	public class EventBatcher
	{
		private readonly Timer m_timer;
		private volatile bool m_signalled;
		private bool m_allow_enable;

		/// <summary>The callback called when this event has been signalled</summary>
		public event Action Action;

		public EventBatcher() :this(100, null) {}
		public EventBatcher(int delay_ms, ISynchronizeInvoke synchronising_object)
		{
			m_signalled = false;
			m_allow_enable = true;
			m_timer = new Timer{AutoReset = false, Enabled = false, Interval = delay_ms, SynchronizingObject = synchronising_object};
			m_timer.Elapsed += (s,e)=>
			{
				try
				{
					// Prevent the timer being started while we're running the callback
					lock (m_timer) { m_allow_enable = false; m_signalled = false; }

					if (Action == null)
						return;

					if (synchronising_object == null || !synchronising_object.InvokeRequired)
					{
						Action();
					}
					else
					{
						synchronising_object.Invoke(Action, null);
					}
				}
				catch (InvalidOperationException)
				{
					Debug.Assert(false, "Don't signal the event batch before the synchronizing object has a handle (if it's a control or form)");
				}
				finally
				{
					lock (m_timer) { m_allow_enable = true; m_timer.Enabled = m_signalled; }
				}
			};
		}

		/// <summary>Signal the event. Signal can be called multiple times during the processing of a windows message.
		/// The event will be delayed to a later windows message and will only be called once.
		/// Note: this can be called from any thread, the resulting event will be marshalled to the synchronising object's
		/// thread, or run on the threadpool if no synchronising object is given</summary>
		public void Signal()
		{
			lock (m_timer) { m_signalled = true; m_timer.Enabled = m_allow_enable && m_signalled; }
		}
	}
}