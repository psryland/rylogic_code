using Rylogic.Common;

namespace RyLogViewer
{
	public interface ILine
	{
		// Notes:
		//  - This interface represents a single log entry, typically a single line from a log file
		//  - The Line implementation is responsible for determining the number of columns on the line

		/// <summary>The log data byte range</summary>
		RangeI FileByteRange { get; }

		/// <summary>Return the value for the requested column. Return "" for out of range column indices</summary>
		string Value(int column);
	}
}
