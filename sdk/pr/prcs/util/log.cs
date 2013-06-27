//***************************************************
// Log File Helper
//  Copyright © Rylogic Ltd 2011
//***************************************************
using System;
using System.IO;

namespace pr.util
{

	public interface ILog
	{
		/// <summary>Register a log file for the current process id. Pass null or empty string for filepath to log to the debug window</summary>
		void Register(string filepath, bool reset);

		/// <summary>Write debug trace statements into the log</summary>
		void Debug(object sender, string str);

		/// <summary>Write info to the current log</summary>
		void Info(object sender, string str);

		/// <summary>Write info to the current log</summary>
		void Warn(object sender, string str);

		/// <summary>Write info to the current log</summary>
		void Error(object sender, string str);

		/// <summary>Write info to the current log</summary>
		void Exception(object sender, Exception ex, string str);
	}

	public static class Log
	{
		public enum ELogLevel { Debug, Info, Warn, Error, Exception, Silent }

		public class NullLogger :ILog
		{
			/// <summary>Set the log level</summary>
			public ELogLevel Level { get; set; }

			/// <summary>Register a log file for the current process id. Pass null or empty string for filepath to log to the debug window</summary>
			public void Register(string filepath, bool reset){}

			/// <summary>Write debug trace statements into the log</summary>
			public void Debug(object sender, string str){}

			/// <summary>Write info to the current log</summary>
			public void Info(object sender, string str){}

			/// <summary>Write info to the current log</summary>
			public void Warn(object sender, string str){}

			/// <summary>Write info to the current log</summary>
			public void Error(object sender, string str){}

			/// <summary>Write info to the current log</summary>
			public void Exception(object sender, Exception ex, string str){}
		}

		public class Logger :ILog
		{
			private class LogFile
			{
				public string Filepath;
				public LogFile(string fname, bool reset)
				{
					Filepath = string.IsNullOrEmpty(fname) ? null : fname;
					if (Filepath == null) return;
					using (File.Open(Filepath, reset ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)) {} // ensure the file exists/is reset
				}
				public void WriteLog(string str)
				{
					if (Filepath == null) { System.Diagnostics.Debug.Write(str); return; }
					using (StreamWriter f = new StreamWriter(File.Open(Filepath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
						f.Write(str);
				}
			}

			/// <summary>Sync object</summary>
			private static readonly object m_lock = new object();

			/// <summary>The log file representation</summary>
			private static LogFile m_log = new LogFile(null, false);

			/// <summary>Set the log level</summary>
			public ELogLevel Level { get; set; }

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
			private void Write(string str)
			{
				lock (m_lock) m_log.WriteLog(str);
			}

			/// <summary>Creates a tag based on the namespace and type of the sender</summary>
			private string GetTag(object sender)
			{
				if (sender == null) return "static";
				Type sender_type = sender.GetType();
				return string.Format("{0}.{1}", sender_type.Namespace, sender_type.Name);
			}

			/// <summary>Write debug trace statements into the log</summary>
			public void Debug(object sender, string str)
			{
				Write(string.Format("[debug][{0}] {1}"+Environment.NewLine, GetTag(sender), str));
			}

			/// <summary>Write info to the current log</summary>
			public void Info(object sender, string str)
			{
				Write(string.Format("[info][{0}] {1}"+Environment.NewLine, GetTag(sender), str));
			}

			/// <summary>Write info to the current log</summary>
			public void Warn(object sender, string str)
			{
				Write(string.Format("[warn][{0}] {1}"+Environment.NewLine, GetTag(sender), str));
			}

			/// <summary>Write info to the current log</summary>
			public void Error(object sender, string str)
			{
				Write(string.Format("[error][{0}] {1}"+Environment.NewLine, GetTag(sender), str));
			}

			/// <summary>Write info to the current log</summary>
			public void Exception(object sender, Exception ex, string str)
			{
				Write(string.Format("[exception][{0}] {1}"+Environment.NewLine, GetTag(sender), str));
				Write(string.Format("[exception] {0}"+Environment.NewLine, ex));
			}
		}

		// Clients should set this to NullLogger to turn off logging
		public static ILog Implementation { get; set; }

		/// <summary>Set the log level</summary>
		public static ELogLevel Level { get; set; }

		static Log()
		{
			Implementation = new Logger();
			#if DEBUG
			Level = ELogLevel.Debug;
			#else
			Level = ELogLevel.Info;
			#endif
		}

		/// <summary>Register a log file for the current process id. Pass null or empty string for filepath to log to the debug window</summary>
		public static void Register(string filepath, bool reset)
		{
			Implementation.Register(filepath, reset);
		}

		/// <summary>Write debug trace statements into the log</summary>
		public static void Debug(object sender, string str)
		{
			if (Level > ELogLevel.Debug) return;
			Implementation.Debug(sender, str);
		}

		/// <summary>Write info to the current log</summary>
		public static void Info(object sender, string str)
		{
			if (Level > ELogLevel.Info) return;
			Implementation.Info(sender, str);
		}

		/// <summary>Write info to the current log</summary>
		public static void Warn(object sender, string str)
		{
			if (Level > ELogLevel.Warn) return;
			Implementation.Warn(sender, str);
		}

		/// <summary>Write info to the current log</summary>
		public static void Error(object sender, string str)
		{
			if (Level > ELogLevel.Error) return;
			Implementation.Error(sender, str);
		}

		/// <summary>Write info to the current log</summary>
		public static void Exception(object sender, Exception ex, string str)
		{
			if (Level > ELogLevel.Exception) return;
			Implementation.Exception(sender, ex, str);
		}
	}
}
