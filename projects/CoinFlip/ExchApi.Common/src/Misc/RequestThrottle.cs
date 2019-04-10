using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Extn;
using Rylogic.Utility;

namespace ExchApi.Common
{
	/// <summary>Helper for gating requests</summary>
	public class RequestThrottle
	{
		// Use:
		//    Use a lock to prevent multiple threads all waiting, and then going, at the same time
		//    using (m_lock.Lock(cancel_token))
		//    {
		//        await m_request_throttle.Wait();
		//        ...
		//    }
		private Stopwatch m_request_sw;
		private long m_last_request_ms;
		private SemaphoreSlim m_sema;

		public RequestThrottle(double requests_per_second = 1.0)
		{
			m_request_sw = new Stopwatch().StartNow();
			m_last_request_ms = 0;
			m_sema = new SemaphoreSlim(1, 1);
			RequestRateLimit = requests_per_second;
		}

		/// <summary>The number of requests per second</summary>
		public double RequestRateLimit { get; set; }

		/// <summary>Lock to ensure one thread at a time</summary>
		public Scope Lock(CancellationToken cancel)
		{
			// Don't wait in here because if an exception is thrown the semaphore
			// it's released. The 'using' doesn't work until this function returns.
			return m_sema.Lock(cancel);
		}

		/// <summary>Block the thread until the next request is allowed</summary>
		public async Task Wait(CancellationToken cancel)
		{
			var request_period_ms = 1000 / RequestRateLimit;
			for (; ; )
			{
				var delta = m_request_sw.ElapsedMilliseconds - m_last_request_ms;
				if (delta < 0 || delta > request_period_ms)
					break;

				await Task.Delay((int)(request_period_ms - delta), cancel);
			}
			m_last_request_ms = m_request_sw.ElapsedMilliseconds;
		}
	}
}
