using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace RyLogViewer
{
	/// <summary>Interface for asynchronous access to a log data source</summary>
	public interface ILogDataSource
	{
		/// <summary>
		/// Begin an asynchronous read of the log data.
		/// Buffer should be filled with the byte representation of the text from the
		/// data source. (Using byte[] to be encoding independent)
		/// 'buffer' is where read log data should be stored, beginning at 'offset',
		/// and containing no more than 'count' bytes.
		/// 'callback' should be called once the read is complete (unless EndRead is called first)
		/// 'state' is a context object to pass through the async operation</summary>
		IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state);

		/// <summary>Completes an asynchronous read returning the number of bytes read</summary>
		int EndRead(IAsyncResult async_result);
	}

	/// <summary>Interface to a component that manages reading a log data source</summary>
	public interface ICustomLogDataSource :ILogDataSource ,IDisposable
	{
		// Notes:
		//  Plugin classes that implement this interface can get constructed
		//  in a background thread. Start, BeginRead, EndRead, Dispose should
		//  all be called from the same thread however.

		/// <summary>A succinct name for the data source</summary>
		string ShortName { get; }

		/// <summary>The string to display in the Data Sources menu</summary>
		string MenuText { get; }

		/// <summary>Displays a modal dialog that allows configuration of the data source</summary>
		LogDataSourceRunData ShowConfigUI(LogDataSourceConfig config);

		/// <summary>Begins the asynchronous process of collecting log data</summary>
		void Start();

		/// <summary>Should return true to continue reading data</summary>
		bool IsConnected { get; }
	}

	/// <summary>Data provided when configuring a custom log data source</summary>
	public class LogDataSourceConfig
	{
		/// <summary>The form for the main window (to use as the parent for any dialogs)</summary>
		public Form MainWindow { get; private set; }

		/// <summary>The history of filepaths used as output files</summary>
		public List<string> OutputFilepathHistory { get; private set; }

		public LogDataSourceConfig(Form main, IEnumerable<string> output_filepath_history)
		{
			MainWindow = main;
			OutputFilepathHistory = new List<string>(output_filepath_history);
		}
	}

	/// <summary>Data returned after configuring a custom log data source</summary>
	public class LogDataSourceRunData
	{
		/// <summary>Continue and launch the custom data source</summary>
		public bool DoLaunch { get; set; }

		/// <summary>The file to capture log data into (use null/empty to have a temp file used)</summary>
		public string OutputFilepath { get; set; }

		/// <summary>True if data should be appended to 'OutputFilepath'</summary>
		public bool AppendOutputFile { get; set; }

		public LogDataSourceRunData(bool do_launch = false, string output_filepath = null, bool append = false)
		{
			DoLaunch         = do_launch;
			OutputFilepath   = output_filepath;
			AppendOutputFile = append;
		}
	}

	/// <summary>Wrapper for Stream to implement the ILogDataSource interface</summary>
	public class StreamSource :ILogDataSource
	{
		private readonly Stream m_stream;
		public StreamSource(Stream stream) { m_stream = stream; }

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
			return m_stream.BeginRead(buffer, offset, count, callback, state);
		}

		/// <summary>Completes an asynchronous read returning the number of bytes read</summary>
		public int EndRead(IAsyncResult async_result)
		{
			return m_stream.EndRead(async_result);
		}
	}

	/// <summary>An attribute for marking classes intended as ILogDataSource implementations</summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class CustomDataSourceAttribute :Attribute
	{}
}
