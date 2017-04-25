//***************************************************
// Log File Helper
//  Copyright (c) Rylogic Ltd 2011
//***************************************************
// Usage:
//  At the start of your program call:
//   Log.Register(filepath, reset:false);
// Throughout call
//   Log.Info(this, "Message");
//   etc
//
// To output to a file, Register with a valid filename
// To output to the debug console, Register with the magic string "DebugConsole"
// To output to nowhere, Register with null or an empty string

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using pr.common;
using pr.extn;

namespace pr.util
{
	public enum ELogLevel
	{
		Debug,
		Info,
		Warn,
		Error,
		Exception,
		Silent,
	}

	public interface ILogWriter
	{
		/// <summary>Write a message to the log. Filtering already applied</summary>
		void Write(string msg);
	}

	public static class Log
	{
		public const string ToDebugConsole = "DebugConsole";

		static Log()
		{
			Writer = new NullLogger();
			#if DEBUG
			Level = ELogLevel.Debug;
			#else
			Level = ELogLevel.Info;
			#endif
		}

		/// <summary>Set the log level. Messages below this level aren't logged</summary>
		public static ELogLevel Level { get; set; }

		/// <summary>Where log output is sent</summary>
		public static ILogWriter Writer { get; set; }

		/// <summary>Regex filter pattern</summary>
		public static string FilterPattern { get; set; }

		/// <summary>Register a log file for the current process id. Pass null or empty string for filepath to log to the debug window</summary>
		public static void Register(string filepath, bool reset)
		{
			try
			{
				if (filepath == ToDebugConsole)
				{
					Writer = new DebugConsoleLogger();
					return;
				}
				if (Path_.IsValidFilepath(filepath, false))
				{
					Writer = new FileLogger(filepath, reset);
					return;
				}
			}
			catch {}
			Writer = new NullLogger();
			return;
		}

		/// <summary>Write debug trace statements into the log</summary>
		public static void Debug(object sender, string str)
		{
			Write(ELogLevel.Debug, sender, str);
		}
		public static void Debug(object sender, Exception ex, string str)
		{
			Write(ELogLevel.Debug, sender, str, ex);
		}

		/// <summary>Write info to the current log</summary>
		public static void Info(object sender, string str)
		{
			Write(ELogLevel.Info, sender, str);
		}
		public static void Info(object sender, Exception ex, string str)
		{
			Write(ELogLevel.Info, sender, str, ex);
		}

		/// <summary>Write info to the current log</summary>
		public static void Warn(object sender, string str)
		{
			Write(ELogLevel.Warn, sender, str);
		}
		public static void Warn(object sender, Exception ex, string str)
		{
			Write(ELogLevel.Warn, sender, str, ex);
		}

		/// <summary>Write info to the current log</summary>
		public static void Error(object sender, string str)
		{
			Write(ELogLevel.Error, sender, str);
		}
		public static void Error(object sender, Exception ex, string str)
		{
			Write(ELogLevel.Error, sender, str, ex);
		}

		/// <summary>Write info to the current log</summary>
		public static void Exception(object sender, Exception ex, string str)
		{
			Write(ELogLevel.Exception, sender, str, ex);
		}

		/// <summary>Single method for filtering and formatting log messages</summary>
		private static void Write(ELogLevel level, object sender, string str, Exception ex = null)
		{
			// Below the log level, ignore
			if (level < Level)
				return;

			// Convert 'sender' to a tag
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

			// Filter out messages based on tag
			if (FilterPattern != null && !Regex.IsMatch(tag, FilterPattern))
				return;

			// Construct the log message and write it
			var msg = string.Format("[{0}][{1}] {2}{3}"+Environment.NewLine, level, tag, str, ex != null ? Environment.NewLine + ex.MessageFull() : string.Empty);
			Writer.Write(msg);
		}

		#region Logger Implementations
		public class NullLogger :ILogWriter
		{
			public void Write(string msg)
			{ }
		}
		public class DebugConsoleLogger :ILogWriter
		{
			private readonly object m_lock;
			public DebugConsoleLogger()
			{
				m_lock = new object();
			}
		
			/// <summary>Add a string to the log</summary>
			public void Write(string msg)
			{
				lock (m_lock)
					System.Diagnostics.Debug.Write(msg);
			}
		}
		public class FileLogger :ILogWriter
		{
			private readonly object m_lock;
			public FileLogger(string filepath, bool reset)
			{
				System.Diagnostics.Debug.Assert(Path_.IsValidFilepath(filepath, false));
				m_lock = new object();

				// Ensure the directory exists (todo: should probably go in an official logs folder)
				var dir = Path_.Directory(filepath);
				if (!Path_.DirExists(dir))
					Directory.CreateDirectory(dir);

				// Ensure the file exists/is reset
				Filepath = filepath;
				using (File.Open(Filepath, reset ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)) {}
			}

			/// <summary>The file being written</summary>
			public string Filepath { get; set; }

			/// <summary>Add a string to the log</summary>
			public void Write(string str)
			{
				lock (m_lock)
					using (var f = new StreamWriter(File.Open(Filepath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
						f.Write(str);
			}
		}
		#endregion
	}
}
