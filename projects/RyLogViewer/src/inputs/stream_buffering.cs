using System;
using System.IO;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	/// <summary>
	/// Base class for buffering a non-seekable stream into a temporary file.
	/// The data source starts from the construction of this class. Captured output
	/// is written to the temporary file. When the data source ends this instance
	/// hangs around until the associated file is closed.</summary>
	public abstract class BufferedStream :IDisposable
	{
		protected class AsyncData
		{
			public readonly ILogDataSource Source;
			public readonly byte[] Buffer;
			public int Read;               // the valid data size in 'Buffer'
			public AsyncData(ILogDataSource s, byte[] b) { Source = s; Buffer = b; Read = 0; }
		}
		protected const int BufBlockSize = 4096;
		protected readonly object m_lock;   // Sync writes to the file
		protected FileStream m_outp;        // The file that captured output is written to

		/// <summary>The filepath of the file that contains the redirected output</summary>
		public readonly string Filepath;

		/// <summary>True if 'Filepath' is a temporary file that will get deleted on close</summary>
		public readonly bool TmpFile;

		/// <summary>Raised if the connection to the host is lost</summary>
		public event EventHandler ConnectionDropped;
		protected void RaiseConnectionDropped()
		{
			if (ConnectionDropped == null) return;
			ConnectionDropped(this, EventArgs.Empty);
		}

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
					Log.Exception(this, ex, "[{0}] Data receive exception".Fmt(type != null ? type.Name : ""));
				}
				RaiseConnectionDropped();
			}
		}
	}
}
