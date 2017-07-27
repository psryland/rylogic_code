using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace pr.extn
{
	public static class Thread_
	{
		/// <summary>Start this thread and return the thread object</summary>
		public static Thread Run(this Thread thrd)
		{
			thrd.Start();
			return thrd;
		}

		/// <summary>True if this event is set. Non-blocking unless 'timeout' is given</summary>
		public static bool IsSignalled(this ManualResetEvent evt, TimeSpan? timeout = null)
		{
			return timeout != null
				? evt.WaitOne(timeout.Value)
				: evt.WaitOne(0);
		}
	}
}
