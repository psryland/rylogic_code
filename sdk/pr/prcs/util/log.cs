//***************************************************
// Log File Helper
//  Copyright © Rylogic Ltd 2011
//***************************************************
#define PR_LOGGING
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace pr.util
{
	public static class Log
	{
		public class LogFile
		{
			public string m_fname;
			public LogFile(string fname) { m_fname = fname; }
		}
		
		/// <summary>Sync object</summary>
		private static readonly object m_lock = new object();

		/// <summary>A map from process id to filename</summary>
		private static readonly Dictionary<int, LogFile> m_logs = new Dictionary<int,LogFile>();

		/// <summary>Return the current process id</summary>
		private static int Id { get { return Process.GetCurrentProcess().Id; } }
		
		/// <summary>Creates a tag based on the namespcase and type of the sender</summary>
		private static string GetTag(object sender)
		{
			if (sender == null) return "static";
			Type sender_type = sender.GetType();
			return string.Format("{0}.{1}", sender_type.Namespace, sender_type.Name);
		}

		/// <summary>Register a log file for the current process id. Pass null for filepath to log to the debug window</summary>
		[Conditional("PR_LOGGING")] public static void Register(string filepath, bool reset)
		{
			lock (m_lock)
			{
				try
				{
					if (filepath == null)
					{
						m_logs.Add(Id, null);
					}
					else
					{
						using (File.Open(filepath, reset ? FileMode.Create : FileMode.OpenOrCreate)) {}
						m_logs.Add(Id, new LogFile(filepath));
					}
					return;
				}
				catch {}
				m_logs.Add(Id, null);
			}
		}
		
		/// <summary>Add a string to the log</summary>
		[Conditional("PR_LOGGING")] private static void Write(string str)
		{
			LogFile lf; lock (m_lock) lf = m_logs[Id];
			if (lf == null) { Debug.Write(str); return; }
			using (StreamWriter f = new StreamWriter(File.Open(lf.m_fname, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
				f.Write(str);
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
