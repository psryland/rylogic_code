using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;

namespace RyLogViewer
{
	/// <summary>A cached line from the log file</summary>
	[DebuggerDisplay("[{LineStartAddr}] {RowText}")]
	public class Line :ILogDataRow
	{
		public Line()
		{
			LineStartAddr = -1;
			Column = new List<Col>();
		}

		/// <summary>The file offset for the start of this cached line</summary>
		public long LineStartAddr { get; set; }

		/// <summary>The string for the whole row</summary>
		public string RowText { get; set; }

		/// <summary>The dimensions of the RowText</summary>
		public SizeF TextSize { get; set; }

		/// <summary>The columns for this row of log data</summary>
		IEnumerable<ILogDataElement> ILogDataRow.Columns => Column;

		/// <summary>The column values for this line</summary>
		public List<Col> Column { get; private set; }

		/// <summary>Indexer access to the columns of the line</summary>
		public Col this[int col_index]
		{
			get { return col_index >= 0 && col_index < Column.Count ? Column[col_index] : new Col("",null); }
		}

		/// <summary>Populate this line from a buffer</summary>
		public void Read(long addr, byte[] buf, int start, int length, Encoding encoding, byte[] col_delim, List<Highlight> highlights, IEnumerable<Transform> transforms)
		{
			LineStartAddr = addr;

			// Convert the buffer to text
			RowText = encoding.GetString(buf, start, length);
			//RowText = RowText.TrimEnd(new[]{'\r','\n'}); - don't do this, its the logs fault if it has weird newlines at the end of each row

			// Apply any transforms
			foreach (var tx in transforms)
				RowText = tx.Txfm(RowText);

			Column.Clear();

			// Split the line into columns
			if (col_delim.Length == 0) // Single column
			{
				Column.Add(new Col(RowText, highlights));
			}
			else // Multiple columns
			{
				int e, s = start;
				do
				{
					e = Misc.FindNextDelim(buf, s, length, col_delim, false); // Returns one passed the delimiter
					var col_text = encoding.GetString(buf, s, e - s - (e!=length?col_delim.Length:0));
					Column.Add(new Col(col_text, highlights));
					s = e;
				}
				while (e != length);
			}
		}

		/// <summary>A column value within a line</summary>
		public class Col :ILogDataElement
		{
			public Col(string text, IEnumerable<Highlight> hl)
			{
				Text = text;
				HL = hl != null
					? hl.Reverse().Where(h => h.IsMatch(text)).ToList()
					: new List<Highlight>();
			}

			/// <summary>The text for the column</summary>
			public string Text { get; private set; }

			/// <summary>The highlights to use for this value. Stored in order of rendering, i.e. the last one will be the last rendered</summary>
			public readonly List<Highlight> HL;

			public override string ToString()
			{
				return Text;
			}
		}
	}
}
