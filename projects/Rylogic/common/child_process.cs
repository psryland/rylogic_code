using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace pr.common
{
	/// <summary>
	/// This class can launch a process (like a bat file, perl
	/// script, etc) and return all of the StdOut and StdErr
	/// to GUI app for display in textboxes, etc.
	/// </summary>
	/// <remarks>
	/// This class (c) 2003 Michael Mayer
	/// Use it as you like (public domain licensing).
	/// Please post any bugs / fixes to the page where
	/// you downloaded this code.
	/// </remarks>
	public class ChildProcess
	{
		/// <summary>The running process</summary>
		private Process m_process;
		
		/// <summary>The command to run</summary>
		public string Executable;
		
		/// <summary>The process arguments</summary>
		public string Arguments;
		
		/// <summary>The WorkingDirectory</summary>
		public string WorkingDirectory;
		
		/// <summary>The process exit code</summary>
		public int ExitCode() { return m_process.ExitCode; }
		
		public Stream Stdout { get { return m_process.StandardOutput; }

		/// <summary>
		/// This method is generally called by DoWork()
		/// which is called by the base classs Start()
		/// </summary>
		public virtual void StartProcess()
		{
			// Start a new process for the cmd
			ProcessStartInfo info = new ProcessStartInfo
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				FileName = Executable,
				Arguments = Arguments,
				WorkingDirectory = WorkingDirectory
			};
			m_process = new Process {StartInfo = info};
			m_process.Start();
		}

		/// <summary>
		/// Launch a process, but do not return until the process has exited.
		/// That way we can kill the process if a cancel is requested.
		/// </summary>
		protected override void DoWork()
		{
			char ch;
			
			StartProcess();
			
			while (!CancelRequested)
			{
				if (m_process.StandardOutput.EndOfStream) break;
				
				if (m_process.StandardOutput.Peek() >= 0)
				{
					ch = (char)m_process.StandardOutput.Read();
					FireAsync(StdOutReceived, this, new DataReceivedEventArgs(ch));
					//Console.Write(ch);
				}
			}
			
			while (!CancelRequested)
			{
				if (m_process.StandardError.EndOfStream) break;
				
				if (m_process.StandardError.Peek() >= 0)
				{
					ch = (char)m_process.StandardError.Read();
					FireAsync(StdErrReceived, this, new DataReceivedEventArgs(ch));
					//Console.Write(ch);
				}
			}
			
			if (CancelRequested)
			{
				try
				{
					m_process.Kill();
				}
				catch
				{
				}
				
				m_process.WaitForExit(500);
				AcknowledgeCancel();
			}
		}
	}
}

	//    /// <summary>
	///// Delegate used by the events StdOutReceived and
	///// StdErrReceived...
	///// </summary>
	//public delegate void DataReceivedHandler(object sender, DataReceivedEventArgs e);
			
	///// <summary>
	///// Event Args for above delegate
	///// </summary>
	//public class DataReceivedEventArgs : EventArgs
	//{
	//    /// <summary>
	//    /// The text that was received
	//    /// </summary>
		
	//    //public string Text;
	//    public char ch;
		
	//    /// <summary>
	//    /// Constructor
	//    /// </summary>
	//    /// <param name="text">The text that was received for this event to be triggered.</param>
	//    public DataReceivedEventArgs(char new_ch)
	//    //public DataReceivedEventArgs(string text)
	//    {
	//        ch = new_ch;
	//        //Text = text;
	//    }
	//}
	
			
		///// <summary>Fired for every line of stdOut received.</summary>
		//public event DataReceivedHandler StdOutReceived;
		
		///// <summary>
		///// Fired for every line of stdErr received.
		///// </summary>
		//public event DataReceivedHandler StdErrReceived;
		
	