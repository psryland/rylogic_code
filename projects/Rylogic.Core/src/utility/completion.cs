using System;
using System.Diagnostics;
using System.Threading;

namespace Rylogic.Utility
{
	public class Completion<T> :IDisposable
	{
		private readonly object m_lock;
		private ManualResetEventSlim m_mre;

		public Completion()
		{
			m_lock = new object();
			m_mre = new ManualResetEventSlim(false);
			m_result = default!;
			m_exception = null;
			Type = EType.NotComplete;
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool _)
		{
			Util.Dispose(ref m_mre!, gc_ok: true);
		}

		/// <summary>How the job was completed</summary>
		public EType Type
		{
			get { return m_type; }
			private set
			{
				Debug.Assert(m_type == EType.NotComplete || m_type == value, "Completion type cannot change, exception from 'NotComplete'");
				m_type = value;
				m_mre.Set();
			}
		}
		private EType m_type;

		/// <summary>True if the job has ended</summary>
		public bool IsCompleted => Type != EType.NotComplete;

		/// <summary>The result of the operation</summary>
		public T Result
		{
			get
			{
				lock (m_lock)
				{
					if (!IsCompleted)
						throw new InvalidOperationException("Job has not completed");

					return (T)m_result;
				}
			}
			set
			{
				lock (m_lock)
				{
					if (IsCompleted)
						throw new InvalidOperationException("Job has already completed");

					m_result = value!;
					Type = EType.Result;
				}
			}
		}
		private object m_result;

		/// <summary>Signal that the job was cancelled</summary>
		public bool Cancelled
		{
			get { return m_cancelled; }
			set
			{
				if (value == false)
					 throw new InvalidOperationException("Cancelled can only be set to true");

				lock (m_lock)
				{
					if (IsCompleted && Type != EType.Cancelled)
						throw new InvalidOperationException("Job is already marked as completed");

					m_cancelled = true;
					Type = EType.Cancelled;
				}
			}
		}
		private bool m_cancelled;

		/// <summary>Signal that the job throw an exception</summary>
		public Exception? Exception
		{
			get => m_exception;
			set
			{
				if (value == null)
					throw new InvalidOperationException("Exception can not be set to null");

				lock (m_lock)
				{
					if (IsCompleted && Type != EType.Exception)
						throw new InvalidOperationException("Job is already marked as completed");

					m_exception = m_exception != null
						? new AggregateException(m_exception, value).Flatten()
						: value;
					Type = EType.Exception;
				}
			}
		}
		private Exception? m_exception;

		/// <summary>Wait for the job to complete</summary>
		public void Wait()
		{
			m_mre.Wait();
		}
		public void Wait(CancellationToken cancel)
		{
			m_mre.Wait(cancel);
		}
		public bool Wait(int timeout_ms)
		{
			return m_mre.Wait(timeout_ms);
		}
		public bool Wait(TimeSpan timeout)
		{
			return m_mre.Wait(timeout);
		}
		public void Wait(int timeout_ms, CancellationToken cancel)
		{
			m_mre.Wait(timeout_ms, cancel);
		}
		public void Wait(TimeSpan timeout, CancellationToken cancel)
		{
			m_mre.Wait(timeout, cancel);
		}

		/// <summary>Completion types</summary>
		public enum EType
		{
			NotComplete,
			Result,
			Cancelled,
			Exception,
		}
	}
	public class Completion :Completion<object>
	{
		/// <summary>Signal the job as complete</summary>
		public void Completed()
		{
			base.Result = null!;
		}

		/// <summary>Hide 'Result' for void results</summary>
		private new object Result
		{
			get => base.Result;
			set => base.Result = value;
		}
	}
}
