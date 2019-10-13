using System;
using Rylogic.Utility;

namespace EDTradeAdvisor
{
	/// <summary>Logging</summary>
	public static class Log
	{
		private static Logger m_log;
		static Log()
		{
			m_log = new Logger("EDTA", new LogToFile(Filepath, LogToFile.EFlags.None));
			m_log.TimeZero = m_log.TimeZero - m_log.TimeZero.TimeOfDay;
			m_log.Write(ELogLevel.Info, "<<< Started >>>");
		}
		public static void Dispose()
		{
			Util.Dispose(ref m_log);
		}

		/// <summary>Log filepath</summary>
		public static string Filepath => Util.ResolveUserDocumentsPath("Rylogic", "EDTradeAdvisor", "Logs", "log.txt");

		/// <summary>Log a message</summary>
		public static void Write(ELogLevel level, string msg, string file = null, int? line = null) => m_log.Write(level, msg, file, line);

		/// <summary>Log an exception with message 'msg'</summary>
		public static void Write(ELogLevel level, Exception ex, string msg, string file = null, int? line = null) => m_log.Write(level, ex, msg, file, line);
	}
}
