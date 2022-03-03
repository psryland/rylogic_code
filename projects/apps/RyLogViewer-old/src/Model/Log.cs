using System;
using System.Text.RegularExpressions;
using Rylogic.Utility;

namespace RyLogViewer
{
	public sealed class Log :IDisposable
	{
		// Notes:
		//  - Singleton log class
		//  - Wrapper around Rylogic.Utility.Logger instance

		private static Logger m_instance;
		public Log(string filepath, LogToFile.EFlags flags)
		{
			var writer = filepath != null
				? (ILogWriter)new LogToFile(filepath, flags)
				: (ILogWriter)new LogToDebug();

			// Initialise a log file
			m_instance = new Logger("ZRM", writer);
			m_instance.Serialise = evt => $"{Log_.EntryDelimiter}{evt.Level}|{evt.Timestamp}|{evt.Message}|\n";
			m_instance.LogEntryPattern = new Regex($@"^(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*?)\|\n", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		}
		public void Dispose()
		{
			Util.Dispose(ref m_instance);
		}

		/// <summary>Add a log entry</summary>
		public static void Write(ELogLevel level, string msg, string file = null, int? line = null)
		{
			m_instance?.Write(level, msg, file, line);
		}
		public static void Write(ELogLevel level, Exception ex, string msg, string file = null, int? line = null)
		{
			m_instance?.Write(level, ex, msg, file, line);
		}
	}
}
