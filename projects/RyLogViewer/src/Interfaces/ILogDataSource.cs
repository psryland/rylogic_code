using System;

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
}
