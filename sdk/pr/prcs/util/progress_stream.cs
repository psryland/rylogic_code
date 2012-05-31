using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
using System.Text;
using NUnit.Framework;

namespace pr.util
{
	/// <summary>A wrapper for a stream that notifies of transfer progress</summary>
	public class ProgressStream :StreamWrapper
	{
		private readonly long m_length;  // The length of the stream (if known, 0 if not known)
		private long m_bytes_read;       // The number of bytes read from the wrapped stream
		private long m_last_report;      // The number of bytes read when progress was last reported
		private long m_last_report_time; // The time of the last report
		private long m_granularity;      // Controls how frequently to report progress

		/// <summary>An event that notifies as the progress is changed.</summary>
		public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
		public class ProgressChangedEventArgs : EventArgs
		{
			/// <summary>The number of bytes read from the wrapped stream</summary>
			public long BytesRead { get; private set; }
			
			/// <summary>The total length of the wrapped stream (if known, 0 if not known)</summary>
			public long Length    { get; private set; }
			
			public ProgressChangedEventArgs(long bytesRead, long length) { BytesRead = bytesRead; Length = length; }
		}
		public void RaiseProgressChanged()
		{
			if (NotifyAllSteps)
			{
				while (m_bytes_read - m_last_report >= m_granularity)
				{
					m_last_report += m_granularity;
					m_last_report_time = Environment.TickCount;
					ProgressChangedEventArgs args = new ProgressChangedEventArgs(m_last_report, m_length);
					if (ProgressChanged != null) ProgressChanged(this, args);
				}
			}
			else
			{
				if (m_bytes_read - m_last_report >= m_granularity &&
					Environment.TickCount - m_last_report_time >= MinInterval)
				{
					m_last_report = m_bytes_read;
					m_last_report_time = Environment.TickCount;
					ProgressChangedEventArgs args = new ProgressChangedEventArgs(m_last_report, m_length);
					if (ProgressChanged != null) ProgressChanged(this, args);
				}
			}
			
			// Report the 100% case
			if (m_length == m_bytes_read && m_last_report != m_bytes_read)
			{
				m_last_report = m_bytes_read;
				ProgressChangedEventArgs args = new ProgressChangedEventArgs(m_last_report, m_length);
				if (ProgressChanged != null) ProgressChanged(this, args);
			}
		}
		
		/// <summary>The granularity of progress changed reporting for streams of unknown length</summary>
		public const long DefaultGranularity = 1024;
		
		public ProgressStream(Stream stream) :base(stream)
		{
			try { m_length = stream.CanSeek ? stream.Length : 0; } catch (NotSupportedException) { m_length = 0; }
			NotifyAllSteps = false;
			MinInterval = 0;
			m_bytes_read = 0;
			m_last_report = 0;
			m_last_report_time = Environment.TickCount - MinInterval;
			Granularity = m_length != 0 ? m_length / 100 : DefaultGranularity;
		}
		
		/// <summary>Controls how frequently ProgressChanged is fired.
		/// By default this is set to the stream.Length/100 or 1024 for streams of unknown length</summary>
		public long Granularity
		{
			get { return m_granularity; }
			set { m_granularity = Math.Max(1, value); }
		}

		/// <summary>
		/// If true, then the ProgressChanged will be called for each increment of 'Granularity' in the number of bytes read.
		/// If false, then ProgressChanged will be called when at least 'Granularity' bytes have been read, but not N times
		/// if N*Granularity bytes are read with a single Read call. Default is false.
		/// </summary>
		public bool NotifyAllSteps { get; set; }

		/// <summary>The minimum time (in ms) between notification calls</summary>
		public int MinInterval { get; set; }

		/// <summary>
		/// If the wrapped stream has a known length, returns the fraction of the wrapped stream's
		/// total length that has been read (i.e. 0.0 -> 1.0). If the wrapped stream has an unknown
		/// length, then returns the number of bytes read so far.
		/// </summary>
		public double ReadProgress
		{
			get { return (double)m_bytes_read / (m_length == 0 ? 1 : m_length); }
		}
		
		/// <summary>
		/// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <returns>
		/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
		/// </returns>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <exception cref="T:System.ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="buffer"/> is null.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative.</exception>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
		/// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
		/// <filterpriority>1</filterpriority>
		public override int Read(byte[] buffer, int offset, int count)
		{
			int result = base.Read(buffer, offset, count);
			m_bytes_read += result;
			RaiseProgressChanged();
			return result;
		}

		/// <summary>
		/// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
		/// </summary>
		/// <returns>
		/// The unsigned byte cast to an Int32, or -1 if at the end of the stream.
		/// </returns>
		/// <exception cref="T:System.NotSupportedException">The stream does not support reading.</exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
		/// <filterpriority>2</filterpriority>
		[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public override int ReadByte()
		{
			int result = base.ReadByte();
			m_bytes_read++;
			RaiseProgressChanged();
			return result;
		}

	}

	// ReSharper disable PossibleNullReferenceException, AccessToModifiedClosure
	/// <summary>Unit tests</summary>
	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestProgressStream()
		{
			const string str = "The quick brown fox jumped over the lazy dog";
			byte[] buf = new byte[256];
			int read;
			
			// Test NotifyAllSteps = false
			using (var stream = new ProgressStream(new MemoryStream(Encoding.ASCII.GetBytes(str))))
			{
				List<long> steps = new List<long>();
				
				stream.Granularity = 10;
				stream.ProgressChanged += (s,a)=>{ steps.Add(a.BytesRead); };
				
				const int chunk = 40;
				List<long> expected_steps = new List<long>{chunk};
				
				// Test reading more that 2*granularity bytes causes only 1 progress update
				read = stream.Read(buf, 0, chunk);
				Assert.AreEqual(chunk, read);
				Assert.AreEqual(expected_steps, steps);

				expected_steps.Add(44);

				// Test ReportProgress is called when 100% of the data is read
				read = stream.Read(buf, 0, buf.Length);
				Assert.AreEqual(str.Length - chunk, read);
				Assert.AreEqual(expected_steps, steps);
			}
			
			// Test NotifyAllSteps = true
			using (var stream = new ProgressStream(new MemoryStream(Encoding.ASCII.GetBytes(str))))
			{
				List<long> steps = new List<long>();
				
				stream.Granularity = 10;
				stream.NotifyAllSteps = true;
				stream.ProgressChanged += (s,a)=>{ steps.Add(a.BytesRead); };
				read = stream.Read(buf, 0, buf.Length);
				
				long expected_call_count = (str.Length/stream.Granularity) + ((str.Length%stream.Granularity) != 0 ? 1 : 0);
				List<long> expected_steps = new List<long>{10,20,30,40,44};
				
				// Test there is a progress call for each granularity step plus one for the final 100% of data read
				Assert.AreEqual(str.Length, read);
				Assert.AreEqual(expected_call_count, steps.Count);
				Assert.AreEqual(expected_steps, steps);
			}
		}
	}
	// ReSharper restore PossibleNullReferenceException, AccessToModifiedClosure
}
