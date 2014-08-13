using System;
using System.Collections.Generic;
using System.IO;

namespace RyLogViewer
{
	/// <summary>Interface for asynchronous access to a log data source</summary>
	public interface ILogDataSource
	{
		// Notes:
		//  These methods provide an interface the same as the BeginRead/EndRead
		//  methods on System.IO.Stream and should be implemented with a behaviour
		//  that mirrors that of System.IO.Stream.

		/// <summary>
		/// Begin an asynchronous read of the log data.
		/// Buffer should be filled with the byte representation of the text from the
		/// data source. (A byte[] is used since the text data can be of any of the
		/// supported text encoding formats)
		/// 'buffer' is where log data should be stored, beginning at 'offset',
		/// and containing no more than 'count' bytes.
		/// 'callback' should be called once the read is complete (unless EndRead is called first)
		/// 'state' is a context object that is passed to the callback</summary>
		IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state);

		/// <summary>Completes an asynchronous read returning the number of bytes read</summary>
		int EndRead(IAsyncResult async_result);
	}

	/// <summary>Interface to a component that manages reading a log data source</summary>
	public interface ICustomLogDataSource :ILogDataSource ,IDisposable
	{
		// Notes:
		// -This interface represents the contract for a complete custom
		//  data source, including any UI interaction needed for configuration.
		//  To create a custom data source, create a class that implements this
		//  interface and mark it with [pr.common.PluginAttribute(typeof(ICustomLogDataSource))].
		// -RyLogViewer loads plugins using a background thread, however, Start(),
		//  BeginRead(), EndRead(), and Dispose() are all called from the main thread.

		/// <summary>
		/// A succinct name for the data source, used in the status bar and as the
		/// name for the plugin in message box messages.</summary>
		string ShortName { get; }

		/// <summary>The string to display in the Data Sources menu.</summary>
		string MenuText { get; }

		/// <summary>
		/// Displays a modal dialog that allows configuration of the data source.
		/// Use this method to display a modal dialog that collects any data needed
		/// by the custom data source (files to load, formats, etc).
		/// Asynchronous data collection should not be started in this method however,
		/// the 'Start()' method is used for this.</summary>
		LogDataSourceRunData ShowConfigUI(LogDataSourceConfig config);

		/// <summary>
		/// Begin the asynchronous process of collecting log data.
		/// Start() will always be called before the first call to BeginRead().</summary>
		void Start();

		/// <summary>
		/// Used by RyLogViewer to poll the status of the custom data source.
		/// Return true to indicate that log data is still available.
		/// Return false to indicate that the custom data source is exhausted.</summary>
		bool IsConnected { get; }
	}

	/// <summary>Interface for a line of log data</summary>
	public interface ILogDataRow
	{
		/// <summary>The string for the whole row</summary>
		string RowText { get; }

		/// <summary>The file offset for the start of this data row</summary>
		long LineStartAddr { get; }

		/// <summary>The columns for this row of log data</summary>
		IEnumerable<ILogDataElement> Columns { get; }
	}

	/// <summary>Interface for a single column within a row of log data</summary>
	public interface ILogDataElement
	{
		/// <summary>The text of the data element</summary>
		string Text { get; }
	}

	/// <summary>Data provided when configuring a custom log data source</summary>
	public class LogDataSourceConfig
	{
		/// <summary>The form for the main window (to use as the parent for any dialogs)</summary>
		public IMainUI MainUI { get; private set; }

		/// <summary>The history of filepaths used as output files</summary>
		public IEnumerable<string> OutputFilepathHistory { get; private set; }

		public LogDataSourceConfig(IMainUI main_ui, IEnumerable<string> output_filepath_history)
		{
			MainUI = main_ui;
			OutputFilepathHistory = output_filepath_history;
		}
	}

	/// <summary>Data returned after configuring a custom log data source</summary>
	public class LogDataSourceRunData
	{
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

		public LogDataSourceRunData(bool do_launch = false, string output_filepath = null, bool append = false, Action<IEnumerable<ILogDataRow>> selection_changed_handler = null)
		{
			DoLaunch               = do_launch;
			OutputFilepath         = output_filepath;
			AppendOutputFile       = append;
			HandleSelectionChanged = selection_changed_handler;
		}
	}

	/// <summary>Wrapper for Stream to implement the ILogDataSource interface</summary>
	public class StreamSource :ILogDataSource
	{
		// This is a helper class that wraps a stream and implements
		// the ILogDataSource interface. Note, this object does not
		// own the stream and therefore is not IDisposable.
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
