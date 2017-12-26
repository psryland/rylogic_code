using System;
using System.Threading.Tasks;

namespace Rylogic.Utility
{
	/// <summary>An interface for supporting shutdown of types with async methods</summary>
	public interface IShutdownAsync
	{
		Task ShutdownAsync();
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Threading;
	using Extn;
	using Utility;

	[TestFixture] public class TestShutdownAsync
	{
#if false
		public class Thing :IDisposable, IShutdownAsync
		{
			public Thing(int value)
			{
				Value = value;
			}
			public void Dispose()
			{
				Debug.Assert(!Running, "Must have shutdown before here. Shutting down an async class in Dispose is not possible");
			}

			/// <summary>Shutdown async</summary>
			public Task ShutdownAsync()
			{
				Running = false;
				return Task_.WaitWhile(() => Running);
			}

			/// <summary>Main loop, running in the main thread context</summary>
			private async void MainLoop(CancellationToken shutdown)
			{
				using (Scope.Create(() => ++m_running, () => --m_running))
				{
					try
					{
						for (;!shutdown.IsCancellationRequested;)
						{
							// Simulate work (with cancel option)
							await Task.Delay(1000, shutdown);
						}
					}
					catch (OperationCanceledException){}
				}
			}
			private CancellationTokenSource m_shutdown;
			private int m_running;

			/// <summary>Run 'main loop'</summary>
			public bool Running
			{
				get { return m_running != 0; }
				set
				{
					if (Running == value) return;
					if (value)
					{
						// Create a new token for each run of the main loop
						m_shutdown = new CancellationTokenSource();
						MainLoop(m_shutdown.Token);
					}
					else
					{
						// Signal shutdown, and release our reference to the token
						m_shutdown.Cancel();
						m_shutdown = null;
					}
				}
			}

			/// <summary></summary>
			public int Value { get; set; }
		}
		public class ThingUI :Form ,IShutdownAsync
		{
			private Thing m_thing;
			public ThingUI()
			{
				m_thing = new Thing(10);
				m_thing.Running = true;
			}
			public Task ShutdownAsync()
			{
				return m_thing.ShutdownAsync();
			}
			protected async override void OnClosing(CancelEventArgs e)
			{
				// Form shutdown is a PITA when using async methods.
				// Disable the form while we wait for shutdown to be allowed
				Enabled = false;
				if (m_thing.Running)
				{
					e.Cancel = true;
					await m_thing.ShutdownAsync();
					Close();
					return;
				}
				base.OnClosing(e);
			}
		}

		[Test] public async void Test0()
		{
			using (var thing = new Thing(1))
			{
				Assert.AreEqual(thing.Value, 1);

				thing.Running = true;

				thing.Value = 2;
				Assert.AreEqual(thing.Value, 2);

				await thing.ShutdownAsync();

				thing.Value = 3;
				Assert.AreEqual(thing.Value, 3);
			}
		}
#endif
	}
}
#endif