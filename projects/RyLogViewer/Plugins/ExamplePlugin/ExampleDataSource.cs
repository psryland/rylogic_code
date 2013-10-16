using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using RyLogViewer;

namespace ExamplePlugin
{
	/// <summary>A data source that reads a file as hex formatted text</summary>
	[CustomDataSource]
	public class ExampleDataSource :ICustomLogDataSource
	{
		private const int TextBytesPerFileByte = 3; // Each byte is represented as 'XX '

		private FileStream m_source_file;
		private TaskCompletionSource<int> m_active_read;
		private CancellationTokenSource m_cancel;
		private string m_source_filepath;
		private readonly byte[] m_text;   // A buffer of the byte data converted into hex strings
		private int m_index;
		private int m_size;

		public ExampleDataSource()
		{
			m_text = new byte[1024];
			m_index = 0;
			m_size = 0;
			ColumnCount = 16;
		}
		public void Dispose()
		{
			CheckIsMainThread();

			// Cancel any reads in progress
			if (m_cancel != null)
				m_cancel.Cancel();

			if (m_active_read != null)
				m_active_read.Task.Wait();

			if (m_cancel != null)
				m_cancel.Dispose();

			if (m_source_file != null)
				m_source_file.Dispose();
		}

		/// <summary>The number of bytes per row</summary>
		public int ColumnCount { get; set; }

		/// <summary>A succinct name for the data source</summary>
		public string ShortName { get { return "Example Source"; } }

		/// <summary>The string to display in the Data Sources menu</summary>
		public string MenuText { get { return "File to Hex - Demo Plugin"; } }

		/// <summary>
		/// Displays a modal dialog that allows configuration of the data source.
		/// Return true if the user wants to continue on and view the custom data source</summary>
		public LogDataSourceRunData ShowConfigUI(LogDataSourceConfig config)
		{
			const string msg =
				"This plugin is a demonstration of a custom data source.\r\n" +
				"It reads an arbitrary file and outputs hexadecimal text\r\n" +
				"data which is then displayed by RyLogViewer";
			MessageBox.Show(config.MainWindow, msg, "Custom Data Source Plugin Example");
			var dlg = new OpenFileDialog{Title = "Choose a file"};
			if (dlg.ShowDialog(config.MainWindow) != DialogResult.OK)
				return new LogDataSourceRunData(false);

			m_source_filepath = dlg.FileName;

			// Return launch data that indicates continue logging the data source
			var res = new LogDataSourceRunData(true);
			res.OutputFilepath = null;
			res.AppendOutputFile = false;
			return res;
		}

		/// <summary>Begins the asynchronous process of collecting log data</summary>
		public void Start()
		{
			CheckIsMainThread();
			if (m_source_file != null) { m_source_file.Dispose(); m_source_file = null; }
			m_source_file = new FileStream(m_source_filepath, FileMode.Open, FileAccess.Read, FileShare.Read|FileShare.Write);
		}

		/// <summary>Should return true to continue reading data</summary>
		public bool IsConnected
		{
			get
			{
				CheckIsMainThread();
				return m_source_file != null;
			}
		}

		/// <summary>
		/// Begin an asynchronous read of the log data.
		/// Buffer should be filled with the byte representation of the text from the
		/// data source. (Using byte[] to be encoding independent)
		/// 'buffer' is where read log data should be stored, beginning at 'offset',
		/// and containing no more than 'count' bytes.
		/// 'callback' should be called once the read is complete (unless EndRead is called first)
		/// 'state' is a context object to pass through the async operation</summary>
		public IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			CheckIsMainThread();

			// Note, not using Task<int> here because there is no guarantee that a task
			// will run on a background thread. If it does run on the main thread then
			// the UI gets blocked...

			// Create a task completion source
			if (m_active_read != null) throw new InvalidOperationException("Only one read request at a time");
			m_active_read = new TaskCompletionSource<int>(state);

			// A cancelation token so this object can be disposed at any point
			if (m_cancel != null) { m_cancel.Dispose(); m_cancel = null; }
			m_cancel = new CancellationTokenSource();

			// Start the read task
			var dispatcher = Dispatcher.CurrentDispatcher;
			ThreadPool.QueueUserWorkItem(s =>
				{
					try
					{
						int read = 0;
						while (read != count)
						{
							var b = ReadByte();
							if (b == -1) break;
							buffer[offset++] = (byte)b;
							++read;
							m_cancel.Token.ThrowIfCancellationRequested();
						}
						m_active_read.SetResult(read);
					}
					catch (OperationCanceledException)
					{
						m_active_read.SetCanceled();
					}
					catch (Exception ex)
					{
						m_active_read.SetException(ex);
					}
					finally
					{
						// Once the task is complete, call the callback
						dispatcher.BeginInvoke(callback, m_active_read.Task);
					}
				});

			return m_active_read.Task;
		}

		/// <summary>Completes an asynchronous read returning the number of bytes read</summary>
		public int EndRead(IAsyncResult async_result)
		{
			CheckIsMainThread();

			var task = async_result as Task<int>;
			if (task == null || task != m_active_read.Task)
				throw new Exception("async_result is not that last result returned from BeginRead");

			try
			{
				m_active_read.Task.Wait();
				if (m_active_read.Task.Exception != null) throw m_active_read.Task.Exception;
				if (m_active_read.Task.IsCanceled) return 0;
				return m_active_read.Task.Result;
			}
			finally
			{
				m_active_read = null;
			}
		}

		/// <summary>Returns the next byte of converted text, or -1 at EOF</summary>
		private int ReadByte()
		{
			if (m_index == m_size)
			{
				FillBuffer();
				if (m_index == m_size)
					return -1;
			}
			return m_text[m_index++];
		}

		/// <summary>Reads from 'm_source_file' into 'm_buffer'</summary>
		private void FillBuffer()
		{
			// Shift any remaining buffered data to the front of the buffer
			var remaining = m_size - m_index;
			Array.Copy(m_text, m_index, m_text, 0, remaining);
			m_index = 0;
			m_size = remaining;

			// Find the amount of free space in the buffer
			// and decided how many rows we can read
			remaining = m_text.Length - m_size;
			var bytes_per_row = ColumnCount * TextBytesPerFileByte;

			// Fill the buffer from the file source
			var line = new byte[ColumnCount];
			var sb = new StringBuilder(bytes_per_row);
			for (var rows_to_read = remaining / bytes_per_row; rows_to_read != 0; --rows_to_read)
			{
				// Read a line's worth of bytes
				var read = m_source_file.Read(line, 0, line.Length);

				sb.Clear();
				for (int i = 0; i != read; ++i)
				{
					var b = line[i];
					var ws = i == ColumnCount - 1 ? "\n" : " ";
					sb.AppendFormat("{0:X}{1:X}{2}", (b >> 4) & 0x0F, b & 0x0F, ws);
				}
				var text = Encoding.ASCII.GetBytes(sb.ToString());
				Array.Copy(text, 0, m_text, m_size, text.Length);
				m_size += text.Length;
			}
		}

		/// <summary>Checking multithreading..</summary>
		[Conditional("DEBUG")] private void CheckIsMainThread()
		{
			if (m_thread_id == -1)
				m_thread_id = Thread.CurrentThread.ManagedThreadId;
			if (m_thread_id != Thread.CurrentThread.ManagedThreadId)
				throw new Exception("Wrong thread");
		}
		private int m_thread_id = -1; // Construction can happen in a worker thread
	}
}
