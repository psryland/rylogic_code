//***************************************************
// Log File Helper
//  Copyright (c) Rylogic Ltd 2011
//***************************************************
using System;
using System.IO;
using System.Text.RegularExpressions;
using pr.extn;

namespace pr.util
{
	public enum ELogLevel { Debug, Info, Warn, Error, Exception, Silent }

	public interface ILogWriter
	{
		/// <summary>Register a log file for the current process id. Pass null or empty string for filepath to log to the debug window</summary>
		void Register(string filepath, bool reset);

		/// <summary>Write a message to the log. Filtering already applied</summary>
		void Write(ELogLevel level, string tag, string str);
	}

	public static class Log
	{
		public class NullLogger :ILogWriter
		{
			/// <summary>Register a log file for the current process id. Pass null or empty string for filepath to log to the debug window</summary>
			public void Register(string filepath, bool reset){}

			/// <summary>Write debug trace statements into the log</summary>
			public void Write(ELogLevel level, string tag , string str){}
		}

		public class Logger :ILogWriter
		{
			private class LogFile
			{
				public LogFile(string fname, bool reset)
				{
					Filepath = string.IsNullOrEmpty(fname) ? null : fname;
					if (Filepath == null) return;
					using (File.Open(Filepath, reset ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)) {} // ensure the file exists/is reset
				}
				public void WriteLog(string str)
				{
					if (Filepath != null)
					{
						using (StreamWriter f = new StreamWriter(File.Open(Filepath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
							f.Write(str);
					}
					else
					{
						System.Diagnostics.Debug.Write(str);
					}
				}
				public string Filepath
				{
					get { return m_filepath; }
					set { m_filepath = value; }
				}
				private string m_filepath;
			}

			/// <summary>Sync object</summary>
			private static readonly object m_lock = new object();

			/// <summary>The log file representation</summary>
			private static LogFile m_log = new LogFile(null, false);

			/// <summary>Register a log file for the current process id. Pass null or empty string for filepath to log to the debug window</summary>
			public void Register(string filepath, bool reset)
			{
				lock (m_lock)
				{
					// Make empty string equivalent to null
					filepath = string.IsNullOrEmpty(filepath) ? null : filepath;
				
					// Ensure the directory exists (todo: should probably go in an official logs folder)
					if (filepath != null) try
					{
						var dir = Path.GetDirectoryName(filepath);
						if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
							Directory.CreateDirectory(dir);
					}
					catch { filepath = null; } // Can't create the directory, log to stdout
				
					try
					{
						if (m_log == null)
							m_log = new LogFile(filepath, reset);
						else
							m_log.Filepath = filepath;
					}
					catch {} // Can't update the m_log, oh well...
				}
			}
		
			/// <summary>Add a string to the log</summary>
			public void Write(ELogLevel level, string tag, string str)
			{
				lock (m_lock) m_log.WriteLog(string.Format("[{0}][{1}] {2}"+Environment.NewLine, level, tag, str));
			}
		}

		/// <summary>Where log output is sent</summary>
		public static ILogWriter Writer { get; set; }

		/// <summary>Set the log level</summary>
		public static ELogLevel Level { get; set; }

		/// <summary>Regex filter pattern</summary>
		public static string FilterPattern { get; set; }

		static Log()
		{
			Writer = new Logger();
			#if DEBUG
			Level = ELogLevel.Debug;
			#else
			Level = ELogLevel.Info;
			#endif
		}

		/// <summary>Single method for filtering and formatting log messages</summary>
		private static void Write(ELogLevel level, object sender, string str, Exception ex = null)
		{
			if (level < Level)
				return;

			string tag;
			if (sender == null)
			{
				tag = "";
			}
			else if (sender is string)
			{
				tag = (string)sender;
			}
			else
			{
				Type sender_type = sender.GetType();
				tag = string.Format("{0}.{1}", sender_type.Namespace, sender_type.Name);
			}

			if (FilterPattern != null && !Regex.IsMatch(tag, FilterPattern))
				return;

			var msg = str;
			if (ex != null) msg += Environment.NewLine + ex.MessageFull();
			Writer.Write(level, tag, msg);
		}

		/// <summary>Register a log file for the current process id. Pass null or empty string for filepath to log to the debug window</summary>
		public static void Register(string filepath, bool reset) { Writer.Register(filepath, reset); }

		/// <summary>Write debug trace statements into the log</summary>
		public static void Debug(object sender, string str) { Write(ELogLevel.Debug, sender, str); }
		public static void Debug(object sender, Exception ex, string str) { Write(ELogLevel.Debug, sender, str, ex); }

		/// <summary>Write info to the current log</summary>
		public static void Info(object sender, string str) { Write(ELogLevel.Info, sender, str); }
		public static void Info(object sender, Exception ex, string str) { Write(ELogLevel.Info, sender, str, ex); }

		/// <summary>Write info to the current log</summary>
		public static void Warn(object sender, string str) { Write(ELogLevel.Warn, sender, str); }
		public static void Warn(object sender, Exception ex, string str) { Write(ELogLevel.Warn, sender, str, ex); }

		/// <summary>Write info to the current log</summary>
		public static void Error(object sender, string str) { Write(ELogLevel.Error, sender, str); }
		public static void Error(object sender, Exception ex, string str) { Write(ELogLevel.Error, sender, str, ex); }

		/// <summary>Write info to the current log</summary>
		public static void Exception(object sender, Exception ex, string str) { Write(ELogLevel.Exception, sender, str, ex); }
	}
}
