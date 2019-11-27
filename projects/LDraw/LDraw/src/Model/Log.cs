using System;
using System.IO;
using Rylogic.Utility;

namespace LDraw
{
	public class Log
	{
		private static readonly Logger m_log;
		static Log()
		{
			m_log = new Logger("LDraw", new LogToFile(Path.GetTempFileName(), LogToFile.EFlags.DeleteOnClose));
			m_log.TimeZero = m_log.TimeZero - m_log.TimeZero.TimeOfDay;
		}
		public static void Dispose()
		{
			m_log.Dispose();
		}

		/// <summary>The full filepath of the log file</summary>
		public static string Filepath => m_log.LogCB is LogToFile l2f ? l2f.Filepath : string.Empty;

		/// <summary>Reset the error log</summary>
		public static void Clear()
		{
			//todo m_log.
		}

		/// <summary>Write to the log</summary>
		public static void Write(ELogLevel level, string msg, string? file = null, int? line = null)
		{
			m_log.Write(level, msg, file, line);
		}
		public static void Write(ELogLevel level, Exception ex, string msg, string? file = null, int? line = null)
		{
			m_log.Write(level, ex, msg, file, line);
		}
	}
}
