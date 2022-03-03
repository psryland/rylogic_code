using Rylogic.Common;

namespace RyLogViewer
{
	public class Line :ILine
	{
		private readonly string m_text;
		public Line(string text, RangeI file_byte_range)
		{
			m_text = text;
			FileByteRange = file_byte_range;
		}

		/// <summary>The log data byte range</summary>
		public RangeI FileByteRange { get; private set; }

		/// <summary>Return the value for the requested column. Return "" for out of range column indices</summary>
		public string Value(int column)
		{
			return column == 0 ? m_text : string.Empty;
		}
	}
}
