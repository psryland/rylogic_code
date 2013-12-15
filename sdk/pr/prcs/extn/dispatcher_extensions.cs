using System;
using System.Windows.Threading;

namespace pr.extn
{
	public static class DispatcherExtensions
	{
		/// <summary>BeginInvokes 'action' after 'delay'</summary>
		public static void BeginInvokeDelayed(this Dispatcher dis, Action action, TimeSpan delay, DispatcherPriority priority = DispatcherPriority.Normal)
		{
			var dt = new DispatcherTimer(priority){Interval = delay};
			dt.Tick += (s,a) =>
				{
					dt.Stop();
					dis.BeginInvoke(action);
				};
			dt.Start();
		}
	}
}
