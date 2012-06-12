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
		[Conditional("PR_LOGGING")] public static void Write(string str)
		{
			lock (m_lock)
			{
				LogFile lf = m_logs[Id];
				if (lf == null) { Debug.Write(str); return; }
				using (StreamWriter f = new StreamWriter(File.Open(lf.m_fname, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
					f.Write(str);
			}
		}

		/// <summary>Write info to the current log</summary>
		[Conditional("PR_LOGGING")] public static void Info(string str, params object[] args)
		{
			Write(string.Format("[info] "+str+Environment.NewLine, args));
		}
		
		/// <summary>Write info to the current log</summary>
		[Conditional("PR_LOGGING")] public static void Warn(string str, params object[] args)
		{
			Write(string.Format("[warn] "+str+Environment.NewLine, args));
		}
		
		/// <summary>Write info to the current log</summary>
		[Conditional("PR_LOGGING")] public static void Error(string str, params object[] args)
		{
			Write(string.Format("[error] "+str+Environment.NewLine, args));
		}
		
		/// <summary>Write info to the current log</summary>
		[Conditional("PR_LOGGING")] public static void Exception(Exception ex, string str, params object[] args)
		{
			Write(string.Format("[exception] "+str+Environment.NewLine, args));
			Write(string.Format("[exception] {0}"+Environment.NewLine, ex));
		}
	}
}
