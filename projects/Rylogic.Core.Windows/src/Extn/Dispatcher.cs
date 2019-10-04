#if NET472

using System;
using System.Windows.Threading;

namespace Rylogic.Extn
{
	public static class Dispatcher_
	{
		/// <summary>Allow a lambda to be passed to Invoke</summary>
		public static void Invoke(this Dispatcher dis, Action action)
		{
			dis.Invoke(action);
		}

		/// <summary>BeginInvoke on the current dispatcher</summary>
		public static DispatcherOperation BeginInvoke(Action action)
		{
			return Dispatcher.CurrentDispatcher.BeginInvoke(action);
		}

		/// <summary>BeginInvokeDelayed on the current dispatcher</summary>
		public static void BeginInvokeDelayed(Action action, TimeSpan delay, DispatcherPriority priority = DispatcherPriority.Normal)
		{
			Dispatcher.CurrentDispatcher.BeginInvokeDelayed(action, delay, priority);
		}

		/// <summary>Allow a lambda to be passed to BeginInvoke</summary>
		public static DispatcherOperation BeginInvoke(this Dispatcher dis, Action action)
		{
			return dis.BeginInvoke(action);
		}
		public static DispatcherOperation BeginInvoke(this Dispatcher dis, Action<object> action, object arg)
		{
			return dis.BeginInvoke(action, arg);
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
					var dt = (DispatcherTimer)s;
					dt.Stop();
					action();
				}, dis).Start();
			}
		}
	}
}

#endif