//***************************************************
// Log File Helper
//  Copyright © Rylogic Ltd 2011
//***************************************************

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

		/// <summary>A map from process id to filename</summary>
		private static Dictionary<int, LogFile> m_logs = new Dictionary<int,LogFile>();

		/// <summary>Return the current process id</summary>
		private static int Id { get { return Process.GetCurrentProcess().Id; } }

		/// <summary>Open a log file</summary>
		[Conditional("PR_LOGGING")]
		public static void Open(string filename, FileMode mode)
		{
			m_logs[Id] = new LogFile(filename);
			using (File.Open(filename, mode))
			{}
		}

		/// <summary>Add a string to the log</summary>
		[Conditional("PR_LOGGING")]
		public static void Write(string str)
		{
			LogFile lf = m_logs[Id];
			lock (lf)
			{
				using (StreamWriter f = new StreamWriter(File.Open(lf.m_fname, FileMode.Append)))
					f.Write(str);
			}
		}
	}
}
