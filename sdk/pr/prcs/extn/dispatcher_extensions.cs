using System;
using System.Windows.Threading;

namespace pr.extn
{
	public static class DispatcherExtensions
	{
		/// <summary>Allow a lambda to be passed to Invoke</summary>
		public static void Invoke(this Dispatcher dis, Action action, params object[] args)
		{
			dis.Invoke(action, args);
		}

		/// <summary>Allow a lambda to be passed to BeginInvoke</summary>
		public static void BeginInvoke(this Dispatcher dis, Action action, params object[] args)
		{
			dis.BeginInvoke(action, args);
		}

		/// <summary>BeginInvokes 'action' after 'delay'</summary>
		public static void BeginInvokeDelayed(this Dispatcher dis, Action action, TimeSpan delay, DispatcherPriority priority = DispatcherPriority.Normal)
		{
			new DispatcherTimer(delay, priority, (s,a) =>
				{
					var dt = s.As<DispatcherTimer>();
					dt.Stop();
					action();
				}, dis).Start();
		}
	}
}
