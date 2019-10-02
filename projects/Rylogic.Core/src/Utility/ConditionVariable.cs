using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rylogic.Utility
{
	/// <summary>A condition variable (same as std::condition_variable from C++)</summary>
	public class ConditionVariable
	{
		// Use:
		// var mutex = new object();
		// var cv = new ConditionVariable();
		// lock (mutex)
		// {
		//		cv.Wait(mutex, () => !is_true());
		// }

		/// <summary>Wake the first waiting thread</summary>
		public void NotifyOne()
		{
			lock (this)
				NotifyOne(this);
		}
		public void NotifyOne(object mutex)
		{
			Monitor.Pulse(mutex);
		}

		/// <summary>Wake all waiting threads</summary>
		public void NotifyAll()
		{
			lock (this)
				NotifyAll(this);
		}
		public void NotifyAll(object mutex)
		{
			Monitor.PulseAll(mutex);
		}

		/// <summary>Block the current thread on the condition variable</summary>
		public void Wait()
		{
			lock (this)
				Wait(this);
		}
		public void Wait(object mutex)
		{
			Monitor.Wait(mutex);
		}

		/// <summary>Block the current thread until 'condition' returns true</summary>
		public void Wait(Func<bool> condition)
		{
			lock (this)
				Wait(this, condition);
		}
		public void Wait(object mutex, Func<bool> condition)
		{
			for (;!condition();)
				Monitor.Wait(mutex);
		}

		/// <summary>Block the current thread until 'condition' returns true or 'timeout'</summary>
		public bool Wait(TimeSpan timeout, Func<bool> condition)
		{
			lock (this)
				return Wait(this, timeout, condition);
		}
		public bool Wait(object mutex, TimeSpan timeout, Func<bool> condition)
		{
			for (;!condition();)
				if (!Monitor.Wait(mutex, timeout))
					return false;

			return true;
		}

		/// <summary>Async await notification</summary>
		public async Task WaitAsync()
		{
			var mutex = this;
			await Task.Run(() =>
			{
				lock (mutex)
					Wait(mutex);
			});
		}

		/// <summary>Async await until 'condition' returns true</summary>
		public async Task WaitAsync(Func<bool> condition)
		{
			var mutex = this;
			await Task.Run(() =>
			{
				lock (mutex)
					Wait(mutex, condition);
			});
		}

		/// <summary>Async await until 'condition' returns true</summary>
		public async Task WaitAsync(TimeSpan timeout, Func<bool> condition)
		{
			var mutex = this;
			await Task.Run(() =>
			{
				lock (mutex)
					Wait(mutex, timeout, condition);
			});
		}
	}

	/// <summary>A condition variable that encapsulates a shared state variable</summary>
	public class ConditionVariable<T>
	{
		// Use:
		//  var mutex = new object();
		//  var cv = new ConditionVariable<bool>();
		//  lock (mutex)
		//  {
		//      var value = cv.Wait(mutex);
		//      if (value) {...}
		//  }
		private T m_value;
		public ConditionVariable()
			:this(default!)
		{
		}
		public ConditionVariable(T value)
		{
			m_value = value;
		}

		/// <summary>Wake the first waiting thread</summary>
		public void NotifyOne(T value)
		{
			lock (this)
				NotifyOne(this, value);
		}
		public void NotifyOne(object mutex, T value)
		{
			m_value = value;
			Monitor.Pulse(mutex);
		}

		/// <summary>Wake all waiting threads</summary>
		public void NotifyAll(T value)
		{
			lock (this)
				NotifyAll(this, value);
		}
		public void NotifyAll(object mutex, T value)
		{
			m_value = value;
			Monitor.PulseAll(mutex);
		}

		/// <summary>Block the current thread on the condition variable</summary>
		public T Wait()
		{
			lock (this)
				return Wait(this);
		}
		public T Wait(object mutex)
		{
			Monitor.Wait(mutex);
			return m_value;
		}

		/// <summary>Block the current thread until 'condition' returns true</summary>
		public T Wait(Func<T, bool> condition)
		{
			lock (this)
				return Wait(this, condition);
		}
		public T Wait(object mutex, Func<T, bool> condition)
		{
			for (;!condition(m_value);)
				Monitor.Wait(mutex);

			return m_value;
		}

		/// <summary>Block the current thread until 'condition' returns true or 'timeout'</summary>
		public bool Wait(TimeSpan timeout, Func<T, bool> condition, out T value)
		{
			lock (this)
				return Wait(this, timeout, condition, out value);
		}
		public bool Wait(object mutex, TimeSpan timeout, Func<T, bool> condition, out T value)
		{
			for (;!condition(m_value);)
			{
				if (!Monitor.Wait(mutex, timeout))
				{
					value = m_value;
					return false;
				}
			}

			value = m_value;
			return true;
		}

		/// <summary>Async await notification</summary>
		public async Task<T> WaitAsync()
		{
			var mutex = this;
			return await Task.Run(() =>
			{
				lock (mutex)
					return Wait(mutex);
			});
		}

		/// <summary>Async await until 'condition' returns true</summary>
		public async Task<T> WaitAsync(Func<T, bool> condition)
		{
			var mutex = this;
			return await Task.Run(() =>
			{
				lock (mutex)
					return Wait(mutex, condition);
			});
		}

		/// <summary>Async await until 'condition' returns true. Throws 'timeout'</summary>
		public async Task<T> WaitAsync(TimeSpan timeout, Func<T, bool> condition)
		{
			var mutex = this;
			return await Task.Run(() =>
			{
				lock (mutex)
					return Wait(mutex, timeout, condition, out var value) ? value : throw new TimeoutException();
			});
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;
	using Utility;

	[TestFixture]
	public class TestCV
	{
		[Test]
		public void Test0()
		{
			var cv = new ConditionVariable();
			var data = (string?)null;
			var ready = false;
			var processed = false;

			// Main
			{
				var worker = new Thread(new ThreadStart(WorkerThread)).Run();

				data = "Example data";

				// send data to the worker thread
				ready = true;
				cv.NotifyOne();

				// wait for the worker
				cv.Wait(() => processed);

				// "Back in main(), data = " << data << '\n';
				Assert.Equal(data, "Changed data");

				worker.Join();
			}

			void WorkerThread()
			{
				// Wait until main() sends data
				cv.Wait(() => ready);

				// after the wait, we own the lock.
				data = "Changed data";

				// Send data back to main()
				processed = true;

				// Should be notifying outside of the lock but C# Monitor.Pulse is
				// not quite the same as std::condition_variable.notifiy_all()
				cv.NotifyOne();
			}
		}

		[Test]
		public void Test1()
		{
			var mutex = new object();
			var cv = new ConditionVariable<int>();
			bool ready = false;
			bool processed = false;

			// Main
			{
				var worker = new Thread(new ThreadStart(WorkerThread)).Run();

				// send data to the worker thread
				lock (mutex)
				{
					ready = true;
					cv.NotifyOne(mutex, 1);
				}

				// wait for the worker
				lock (mutex)
				{
					var value = cv.Wait(mutex, x => processed);
					Assert.Equal(value, 42);
				}

				worker.Join();
			}

			void WorkerThread()
			{
				// Wait until main() sends data
				lock (mutex)
				{
					var value = cv.Wait(mutex, x => ready);
					Assert.Equal(value, 1);

					// after the wait, we own the lock.
					cv.NotifyOne(mutex, 42);

					// Send data back to main()
					processed = true;
				}
			}
		}

		[Test]
		public async void Test2()
		{
			var cv = new ConditionVariable();
			var data = (string?)null;
			var ready = false;
			var processed = false;

			// Main
			{
				var worker = new Thread(new ThreadStart(WorkerThread)).Run();

				data = "Example data";

				// send data to the worker thread
				ready = true;
				cv.NotifyOne();

				// wait for the worker
				await cv.WaitAsync(() => processed);

				// "Back in main(), data = " << data << '\n';
				Assert.Equal(data, "Changed data");

				worker.Join();
			}

			async void WorkerThread()
			{
				// Wait until main() sends data
				await cv.WaitAsync(() => ready);

				// after the wait, we own the lock.
				data = "Changed data";

				// Send data back to main()
				processed = true;

				// Should be notifying outside of the lock but C# Monitor.Pulse is
				// not quite the same as std::condition_variable.notifiy_all()
				cv.NotifyOne();
			}
		}
	}
}
#endif