using System;

namespace RyLogViewer
{
	public class NoLinesException :Exception
	{
		private readonly int m_buf_size;
		public NoLinesException(int buf_size)
		{
			m_buf_size = buf_size;
		}
		public override string Message
		{
			get
			{
				return
					$"No lines detected within a {m_buf_size} byte block.\r\n" +
					$"\r\n" +
					$"There are several possible causes for this:\r\n" +
					$" - The log file may contain one or more lines that are longer than {m_buf_size} bytes,\r\n" +
					$" - The line ending setting may be incorrect causing new lines not to be detected correctly,\r\n" +
					$" - The text encoding setting may be incorrect causing the log data to not be read correctly,\r\n" +
					$"\r\n" +
					$"If lines longer than {m_buf_size} bytes are expected, increase the 'Maximum Line Length'\r\n" +
					$"option under settings. Otherwise, check the settings under the 'Line Ending' and\r\n" +
					$"'Encoding' menus. You may have to specify these values explicitly rather than\r\n" +
					$"using automatic detection.";
			}
		}
	}
}
