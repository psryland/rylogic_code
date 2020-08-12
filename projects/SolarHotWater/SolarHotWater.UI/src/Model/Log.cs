using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Rylogic.Utility;

namespace SolarHotWater
{
	/// <summary>Logging</summary>
	public static class Log
	{
		private static Logger m_log;
		static Log()
		{
			m_log = new Logger("Solar", new LogToFile(Filepath, LogToFile.EFlags.None));
			m_log.TimeZero -= m_log.TimeZero.TimeOfDay;
			m_log.Write(ELogLevel.Info, "<<< Started >>>", Util.__FILE__(), Util.__LINE__());
		}
		public static void Dispose()
		{
			Util.Dispose(ref m_log!);
		}

		/// <summary>Log filepath</summary>
		public static string Filepath => Util.ResolveUserDocumentsPath("Rylogic", "SolarHotWater", "Logs", "log.txt");

		/// <summary>How to interpret each log entry</summary>
		public static Regex? LogEntryPattern => new Regex(@"^(?<File>.*?)\|(?<Tag>.*?)\|(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*)\n"
			,RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		/// <summary>Log a message</summary>
		public static void Write(ELogLevel level, string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) => m_log.Write(level, msg, file, line);

		/// <summary>Log an exception with message 'msg'</summary>
		public static void Write(ELogLevel level, Exception ex, string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) => m_log.Write(level, ex, msg, file, line);
	}
}
