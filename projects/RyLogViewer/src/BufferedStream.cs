using System;
using System.IO;
using Rylogic.Extn;
using Rylogic.Utility;

namespace RyLogViewer
{
	/// <summary>
	/// Base class for buffering a non-seek-able stream into a temporary file.
	/// The data source starts from the construction of this class. Captured output
	/// is written to the temporary file. When the data source ends this instance
	/// hangs around until the associated file is closed.</summary>
	public abstract class BufferedStream :IDisposable
	{
		protected const int BufBlockSize = 4096;
		protected readonly object m_lock;   // Sync writes to the file
		protected FileStream m_outp;        // The file that captured output is written to

		protected BufferedStream(string output_filepath, bool append)
		{
			m_lock = new object();

			// File open options
			var mode = append ? FileMode.Append : FileMode.Create;
			var opts = FileOptions.Asynchronous|FileOptions.RandomAccess;
			TmpFile = false;

			// Get a file to capture the process output in
			Filepath = output_filepath;
			if (string.IsNullOrEmpty(Filepath))
			{
				Filepath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
				mode = FileMode.Create;
				opts |= FileOptions.DeleteOnClose;
				TmpFile = true;
			}

			// Open the file that will receive the captured output
			m_outp = new FileStream(Filepath, mode, FileAccess.Write, FileShare.Read, BufBlockSize, opts);
		}
		public virtual void Dispose()
		{
			if (m_outp != null)
			{
				lock (m_lock)
				{
					Log.Info(this, "Disposing buffer stream capture file");
					m_outp.Dispose();
					m_outp = null;
				}
			}
		}

		/// <summary>The filepath of the file that contains the redirected output</summary>
		public readonly string Filepath;

		/// <summary>True if 'Filepath' is a temporary file that will get deleted on close</summary>
		public readonly bool TmpFile;

		/// <summary>Raised if the connection to the host is lost</summary>
		public event EventHandler ConnectionDropped;
		protected void RaiseConnectionDropped()
		{
			ConnectionDropped?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Should return true to continue reading data</summary>
		protected abstract bool IsConnected { get; }

		/// <summary>Handler for async reads from a stream</summary>
		protected virtual void DataRecv(IAsyncResult ar)
		{
			AsyncData data = (AsyncData)ar.AsyncState;
			lock (m_lock)
			{
				try
				{
					if (m_outp == null)
						return;

					data.Read = data.Source.EndRead(ar);
					if (data.Read != 0 || IsConnected)
					{
						m_outp.Write(data.Buffer, 0, data.Read);
						m_outp.Flush();
						data.Source.BeginRead(data.Buffer, 0, data.Buffer.Length, DataRecv, data);
						return;
					}
				}
				catch (Exception ex)
				{
					var type = GetType().DeclaringType;
					Log.Exception(this, ex, $"[{(type != null ? type.Name : "")}] Data receive exception");
				}
				RaiseConnectionDropped();
			}
		}

		/// <summary></summary>
		protected class AsyncData
		{
			public AsyncData(ILogDataSource s, byte[] b)
			{
				Source = s;
				Buffer = b;
				Read = 0;
			}

			/// <summary></summary>
			public readonly ILogDataSource Source;

			/// <summary></summary>
			public readonly byte[] Buffer;

			/// <summary>The valid data size in 'Buffer'</summary>
			public int Read;
		}
	}
}
