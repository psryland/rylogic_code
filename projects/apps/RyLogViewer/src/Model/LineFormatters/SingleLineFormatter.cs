using System.Text;
using Rylogic.Common;

namespace RyLogViewer
{
	public class SingleLineFormatter : ILineFormatter
	{
		private readonly Encoding m_encoding;
		public SingleLineFormatter(Encoding encoding)
		{
			m_encoding = encoding;
		}

		/// <summary>A name for this formatter, for displaying in the UI</summary>
		public const string Name = "Single Lines";
		string ILineFormatter.Name => Name;

		/// <summary>Return a line instance from a buffer of log data</summary>
		public ILine CreateLine(byte[] line_buf, int start, int count, RangeI file_byte_range)
		{
			// Convert the buffer to text
			// Note: don't trim 'text', it's the logs fault if it has weird newlines at the end of each row.
			var text = m_encoding.GetString(line_buf, start, count);

			//todo // Apply any transforms
			//todo foreach (var tx in transforms)
			//todo 	RowText = tx.Txfm(RowText);

			var line = new Line(text, file_byte_range);
			return line;
		}
	}
}
