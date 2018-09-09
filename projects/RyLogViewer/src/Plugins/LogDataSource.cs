using System;
using System.Collections.Generic;
using System.IO;

namespace RyLogViewer
{
	/// <summary>Data provided when configuring a custom log data source</summary>
	public class LogDataSourceConfig
	{
		public LogDataSourceConfig(IMainUI main_ui, IEnumerable<string> output_filepath_history)
		{
			MainUI = main_ui;
			OutputFilepathHistory = output_filepath_history;
		}

		/// <summary>The form for the main window (to use as the parent for any dialogs)</summary>
		public IMainUI MainUI { get; private set; }

		/// <summary>The history of filepaths used as output files</summary>
		public IEnumerable<string> OutputFilepathHistory { get; private set; }
	}

	/// <summary>Data returned after configuring a custom log data source</summary>
	public class LogDataSourceRunData
	{
		public LogDataSourceRunData(bool do_launch = false, string output_filepath = null, bool append = false, Action<IEnumerable<ILogDataRow>> selection_changed_handler = null)
		{
			DoLaunch = do_launch;
			OutputFilepath = output_filepath;
			AppendOutputFile = append;
			HandleSelectionChanged = selection_changed_handler;
		}

		/// <summary>
		/// Launch the custom data source if true.
		/// This will be false if the 'ShowConfigUI' method detects that the user wants to cancel</summary>
		public bool DoLaunch { get; set; }

		/// <summary>The file to capture log data into (use null or empty to have a temp file used)</summary>
		public string OutputFilepath { get; set; }

		/// <summary>True if data should be appended to 'OutputFilepath'</summary>
		public bool AppendOutputFile { get; set; }

		/// <summary>
		/// An optional handler that is called whenever the selected lines in the grid are changed.
		/// The provided enumerable is the set of selected rows, lazily evaluated.</summary>
		public Action<IEnumerable<ILogDataRow>> HandleSelectionChanged;
	}

	/// <summary>Wrapper for Stream that implements the ILogDataSource interface</summary>
	public class StreamSource :ILogDataSource
	{
		// This is a helper class that wraps a stream and implements the ILogDataSource interface.
		// Note, this object does not own the stream and therefore is not IDisposable.
		private readonly Stream m_stream;
		public StreamSource(Stream stream)
		{
			m_stream = stream;
		}
		public IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return m_stream.BeginRead(buffer, offset, count, callback, state);
		}
		public int EndRead(IAsyncResult async_result)
		{
			return m_stream.EndRead(async_result);
		}
	}
}
