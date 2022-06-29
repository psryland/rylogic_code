using System;
using System.Text.RegularExpressions;
using Rylogic.Utility;

namespace Rylogic.TextAligner
{
	public sealed class Log : IDisposable
	{
		// Notes:
		//  - Singleton log class
		//  - Wrapper around Rylogic.Utility.Logger instance

		private static Logger? m_instance;
		public Log()
		{
			// Initialise a log file
			m_instance = new Logger("FirmwareLoader", new LogToFile(Filepath, LogToFile.EFlags.None));
			m_instance.Serialise = evt => $"{Log_.EntryDelimiter}{evt.Level}|{evt.Timestamp}|{evt.Message}|\n";
			m_instance.LogEntryPattern = new Regex($@"^(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*?)\|\n", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		}
		public void Dispose()
		{
			Util.Dispose(ref m_instance!);
		}

		/// <summary>Location of the log file</summary>
		public static string Filepath => Util.ResolveAppDataPath("Rylogic", "VSExtension", "log.txt");

		/// <summary>Regex pattern used to identify log entries</summary>
		public static Regex? EntryPattern => m_instance?.LogEntryPattern;

		/// <summary>Add a log entry</summary>
		public static void Write(ELogLevel level, string msg, string? file = null, int? line = null)
		{
			m_instance?.Write(level, msg, file, line);
		}
		public static void Write(ELogLevel level, Exception ex, string msg, string? file = null, int? line = null)
		{
			m_instance?.Write(level, ex, msg, file, line);
		}
	}
}