using System;

namespace RyLogViewer
{
	/// <summary>Interface to a component that manages reading a log data source</summary>
	public interface ICustomLogDataSource :ILogDataSource ,IDisposable
	{
		// Notes:
		// -This interface represents the contract for a complete custom
		//  data source, including any UI interaction needed for configuration.
		//  To create a custom data source, create a class that implements this
		//  interface and mark it with [Rylogic.Common.PluginAttribute(typeof(ICustomLogDataSource))].
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
}
