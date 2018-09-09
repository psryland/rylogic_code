using System;
using System.IO;

namespace RyLogViewer
{
	/// <summary>Interface for asynchronous access to a log data source</summary>
	public interface ILogDataSource: IDisposable
	{
		/// <summary>A filepath or description of the data source</summary>
		string Path { get; }

		/// <summary>Create a new stream with access to the log data</summary>
		Stream OpenStream();
	}
}
