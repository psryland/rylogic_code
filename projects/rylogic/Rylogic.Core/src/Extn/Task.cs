using System;
using System.Runtime.CompilerServices;
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

		/// <summary>Allows CancellationToken to be await-able.</summary>
		public static CancellationTokenAwaiter GetAwaiter(this CancellationToken token)
		{
			// Awaiting cancellation token is considered not good practise
			// because sometimes cancels are never signalled.
			return new CancellationTokenAwaiter(token);
		}

		public struct CancellationTokenAwaiter : INotifyCompletion
		{
			private readonly CancellationToken _token;
			public CancellationTokenAwaiter(CancellationToken token)
			{
				_token = token;
			}
			public bool IsCompleted => _token.IsCancellationRequested;
			public void OnCompleted(Action continuation) => _token.Register(continuation);
			public void GetResult() => _token.WaitHandle.WaitOne();
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;

	[TestFixture]
	public class TestTaskExtns
	{
		[Test]
		public async void AwaitTest()
		{
			var cts = new CancellationTokenSource();
			var token = cts.Token;
			ThreadPool.QueueUserWorkItem(_ => { Thread.Sleep(100); cts.Cancel(); });
			await token;
		}
	}
}
#endif