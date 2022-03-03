using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rylogic.Common;

namespace RyLogViewer
{
	public class DelimitedLineFormatter : ILineFormatter
	{
		/// <summary>A name for this formatter, for displaying in the UI</summary>
		public const string Name = "Delimited Text";
		string ILineFormatter.Name => Name;

		/// <summary>Return a line instance from a buffer of log data</summary>
		public ILine CreateLine(byte[] line_buf, int start, int count, RangeI file_byte_range)
		{
			throw new NotImplementedException();

			//// Cache data for the line
			//line.Read(rng.Beg, m_line_buf, 0, read, m_encoding, m_col_delim, m_highlights, m_transforms);

			//// Save the text size
			//line.TextSize = m_gfx.MeasureString(line.RowText, m_grid.RowsDefaultCellStyle.Font);

		}
	}
}
