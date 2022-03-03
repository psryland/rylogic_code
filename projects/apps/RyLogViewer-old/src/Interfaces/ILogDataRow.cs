using System.Collections.Generic;

namespace RyLogViewer
{
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
}
