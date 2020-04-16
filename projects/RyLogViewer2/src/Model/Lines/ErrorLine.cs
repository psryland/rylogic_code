using Rylogic.Common;

namespace RyLogViewer
{
	public class ErrorLine : ILine
	{
		private readonly string m_msg;
		public ErrorLine(string error_message, RangeI range)
		{
			FileByteRange = range;
			m_msg = error_message;
		}

		/// <summary>The log data byte range</summary>
		public RangeI FileByteRange { get; private set; }

		/// <summary>Return the value for the requested column. Return "" for out of range column indices</summary>
		public string Value(int _)
		{
			return m_msg;
		}
	}
}
