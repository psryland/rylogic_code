//***************************************************
// Log File Helper
//  Copyright © Rylogic Ltd 2011
//***************************************************
#define PR_LOGGING
using System;
using System.Diagnostics;
using System.IO;

namespace pr.util
{
	public static class Log
	{
		public class LogFile
		{
			/// <summary>The log filepath</summary>
			public string Filepath;
			
			public LogFile(string fname, bool reset)
			{
				Filepath = fname;
				if (Filepath == null) return;
				using (File.Open(Filepath, reset ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)) {} // ensure the file exists/is reset
			}
			public void WriteLog(string str)
			{
				if (Filepath == null) { Debug.Write(str); return; }
				using (StreamWriter f = new StreamWriter(File.Open(Filepath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
					f.Write(str);
			}
		}
		
		/// <summary>Sync object</summary>
		private static readonly object m_lock = new object();

		/// <summary>The log file representation</summary>
		private static LogFile m_log = new LogFile(null, false);

		/// <summary>Register a log file for the current process id. Pass null for filepath to log to the debug window</summary>
		[Conditional("PR_LOGGING")] public static void Register(string filepath, bool reset)
		{
			lock (m_lock)
			{
				// Ensure the directory exists (todo: should probably go in an official logs folder)
				string dir = Path.GetDirectoryName(filepath);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				
				if (m_log == null)
					m_log = new LogFile(filepath, reset);
				else
					m_log.Filepath = filepath;
			}
		}
		
		/// <summary>Add a string to the log</summary>
		private static void Write(string str)
		{
			lock (m_lock) m_log.WriteLog(str);
		}

		/// <summary>Creates a tag based on the namespcase and type of the sender</summary>
		private static string GetTag(object sender)
		{
			if (sender == null) return "static";
			Type sender_type = sender.GetType();
			return string.Format("{0}.{1}", sender_type.Namespace, sender_type.Name);
		}

		/// <summary>Write info to the current log</summary>
		[Conditional("PR_LOGGING")] public static void Info(object sender, string str, params object[] args)
		{
			Write(string.Format("[info][{0}] {1}"+Environment.NewLine, GetTag(sender), string.Format(str, args)));
		}
		
		/// <summary>Write info to the current log</summary>
		[Conditional("PR_LOGGING")] public static void Warn(object sender, string str, params object[] args)
		{
			Write(string.Format("[warn][{0}] {1}"+Environment.NewLine, GetTag(sender), string.Format(str, args)));
		}
		
		/// <summary>Write info to the current log</summary>
		[Conditional("PR_LOGGING")] public static void Error(object sender, string str, params object[] args)
		{
			Write(string.Format("[error][{0}] {1}"+Environment.NewLine, GetTag(sender), string.Format(str, args)));
		}
		
		/// <summary>Write info to the current log</summary>
		[Conditional("PR_LOGGING")] public static void Exception(object sender, Exception ex, string str, params object[] args)
		{
			Write(string.Format("[exception][{0}] {1}"+Environment.NewLine, GetTag(sender), string.Format(str, args)));
			Write(string.Format("[exception] {0}"+Environment.NewLine, ex));
		}
	}
}
