using System;
using System.Diagnostics;
using System.Threading;
using Rylogic.Utility;

namespace Rylogic.Extn
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

		/// <summary>Helper to construct and start a stop watch</summary>
		public static Stopwatch StartNow(this Stopwatch sw)
		{
			sw.Start();
			return sw;
		}

		/// <summary>Scoped start/stop of this stopwatch</summary>
		public static Scope Time(this Stopwatch sw)
		{
			return Scope.Create(
				() => sw.Start(),
				() => sw.Stop());
		}

		/// <summary>RAII scope lock this semaphore</summary>
		public static Scope Lock(this SemaphoreSlim ss)
		{
			return Scope.Create(
				() => ss.Wait(),
				() => ss.Release());
		}
		public static Scope Lock(this SemaphoreSlim ss, CancellationToken cancel)
		{
			return Scope.Create(
				() => ss.Wait(cancel),
				() => ss.Release());
		}
		public static Scope Lock(this SemaphoreSlim ss, int timeout_ms)
		{
			return Scope.Create(
				() => ss.Wait(timeout_ms),
				() => ss.Release());
		}
		public static Scope Lock(this SemaphoreSlim ss, TimeSpan timeout)
		{
			return Scope.Create(
				() => ss.Wait(timeout),
				() => ss.Release());
		}
		public static Scope Lock(this SemaphoreSlim ss, int timeout_ms, CancellationToken cancel)
		{
			return Scope.Create(
				() => ss.Wait(timeout_ms, cancel),
				() => ss.Release());
		}
		public static Scope Lock(this SemaphoreSlim ss, TimeSpan timeout, CancellationToken cancel)
		{
			return Scope.Create(
				() => ss.Wait(timeout, cancel),
				() => ss.Release());
		}
	}
}
