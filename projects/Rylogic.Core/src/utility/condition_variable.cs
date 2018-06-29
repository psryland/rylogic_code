using System;
using System.Threading;

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
			:this(default(T))
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
			var mutex = new object();
			var cv = new ConditionVariable();
			var data = (string)null;
			bool ready = false;
			bool processed = false;

			Action worker_thread = () =>
			{
				lock (mutex)
				{
					// Wait until main() sends data
					cv.Wait(mutex, () => ready);

					// after the wait, we own the lock.
					data = "Changed data";

					// Send data back to main()
					processed = true;

					// Should be notifying outside of the lock but C# Monitor.Pulse is
					// not quite the same as std::condition_variable.notifiy_all()
					cv.NotifyOne(mutex);
				}
			};

			// Main
			{
				var worker = new Thread(new ThreadStart(worker_thread)).Run();

				data = "Example data";

				// send data to the worker thread
				lock (mutex)
				{
					ready = true;
					cv.NotifyOne(mutex);
				}

				// wait for the worker
				lock (mutex)
				{
					cv.Wait(mutex, () => processed);
				}

				// "Back in main(), data = " << data << '\n';
				Assert.Equal(data, "Changed data");

				worker.Join();
			}
		}

		[Test]
		public void Test1()
		{
			var mutex = new object();
			var cv = new ConditionVariable<int>();
			bool ready = false;
			bool processed = false;

			Action worker_thread = () =>
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
			};

			// Main
			{
				var worker = new Thread(new ThreadStart(worker_thread)).Run();

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
		}
	}
}
#endif