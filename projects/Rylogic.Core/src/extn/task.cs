using System;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Utility;

namespace Rylogic.Extn
{
	public static class Task_
	{
		/// <summary>A task to wait fill 'wait' is signalled</summary>
		public static Task WaitUntil(WaitHandle wait)
		{
			return Task.Run(() =>
			{
				wait.WaitOne();
			});
		}

		/// <summary>A task to wait till 'condition' returns false</summary>
		public static Task WaitWhile(Func<bool> condition)
		{
			return Task.Run(() =>
			{
				for (; condition(); )
					Thread.Yield();
			});
		}

		/// <summary>Returns an RAII scope that temporarily sets the current sync context to null</summary>
		public static Scope NoSyncContext()
		{
			return Scope.Create(
				() =>
				{
					var context = SynchronizationContext.Current;
					SynchronizationContext.SetSynchronizationContext(null);
					return context;
				},
				sc =>
				{
					SynchronizationContext.SetSynchronizationContext(sc);
				});
		}
		
		/// <summary>
		/// Run an async task synchronously without risk of deadlocks or reentrancy.
		/// This only works if code after the first 'await' does not require running on the same thread.</summary>
		public static void RunSync(Func<Task> task)
		{
			using (NoSyncContext())
				task().Wait();
		}
		public static T RunSync<T>(Func<Task<T>> task)
		{
			using (NoSyncContext())
				return task().Result;
		}

		/// <summary>RAII semaphore acquire</summary>
		public static async Task<Scope> LockAsync(this SemaphoreSlim semaphore)
		{
			return await Scope.CreateAsync(
				() => semaphore.WaitAsync(),
				() => semaphore.Release());
		}
	}
}
