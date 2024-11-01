using System;
using System.Windows.Threading;

namespace Rylogic.Extn
{
	public static class Dispatcher_
	{
		/// <summary>BeginInvokeDelayed on the current dispatcher</summary>
		public static void BeginInvokeDelayed(Action action, TimeSpan delay, DispatcherPriority priority = DispatcherPriority.Normal)
		{
			Dispatcher.CurrentDispatcher.BeginInvokeDelayed(action, delay, priority);
		}

		/// <summary>BeginInvokes 'action' after 'delay'</summary>
		public static void BeginInvokeDelayed(this Dispatcher dis, Action action, TimeSpan delay, DispatcherPriority priority = DispatcherPriority.Normal)
		{
			if (delay == TimeSpan.Zero)
			{
				dis.BeginInvoke(action);
			}
			else
			{
				new DispatcherTimer(delay, priority, (s, a) =>
				{
					var dt = (DispatcherTimer)s!;
					dt.Stop();
					action();
				}, dis).Start();
			}
		}
	}
}