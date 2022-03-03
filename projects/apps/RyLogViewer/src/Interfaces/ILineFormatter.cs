using Rylogic.Common;

namespace RyLogViewer
{
	public interface ILineFormatter
	{
		/// <summary>A name for this formatter, for displaying in the UI</summary>
		string Name { get; }

		/// <summary>Return a line instance from a buffer of log data</summary>
		ILine CreateLine(byte[] line_buf, int start, int count, RangeI file_byte_range);
	}
}
