using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.util;

namespace RyLogViewer
{
	/// <summary>Parts of the Main form related to buffering non-file stream into an output file</summary>
	public partial class Main :Form
	{
		private BufferedProcess m_buffered_process;

		/// <summary>Launch a process, piping its output into a temporary file</summary>
		private void LaunchProcess(LaunchApp launch)
		{
			try
			{
				// Close any currently open file
				// Strictly, we don't have to close because OpenLogFile closes before opening
				// however if the user reopens the same process the existing process will hold
				// a lock to the capture file preventing the new process being created.
				CloseLogFile();
				
				// Launch the process with standard output/error redirected to the temporary file
				BufferedProcess buffered_process = new BufferedProcess(launch);
				
				// Give some UI feedback when the process ends
				buffered_process.ProcessExited += (s,a)=>
					{
						Action proc_exit = () => SetTransientStatusMessage(string.Format("{0} exited", launch.Executable));
						BeginInvoke(proc_exit);
					};
			
				// Open the capture file created by buffered_process
				OpenLogFile(buffered_process.Filepath, !buffered_process.TmpFile);
				m_buffered_process = buffered_process;
			}
			catch (Exception ex)
			{
				Log.Exception(ex, "Failed to launch child process {0} {1} -> {2}", launch.Executable, launch.Arguments, launch.OutputFilepath);
				MessageBox.Show(this
					,string.Format("Failed to launch child process {0}\r\n.Error: {1}",launch.Executable,ex.Message)
					,Resources.FailedToLaunchProcess, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
	
	// The process runs from the construction of the BufferedProcess type
	// When the process ends the BufferedProcess instance hangs around until
	// the associated file is closed.

	/// <summary>Manages a process and reading its output.</summary>
	public class BufferedProcess :IDisposable
	{
		private class AsyncData
		{
			public readonly Stream Stream;
			public readonly byte[] Buffer;
			public AsyncData(Stream s, byte[] b) { Stream = s; Buffer = b; }
		}
		
		private readonly byte[] m_outbuf; // A buffer for standard output data
		private readonly byte[] m_errbuf; // A buffer for standard error data
		private readonly object m_lock;   // Sync writes to the file
		private Process m_process;        // The running process
		private FileStream m_outp;        // The file that captured output is written to
		
		/// <summary>The filepath of the file that contains the redirected output</summary>
		public readonly string Filepath;

		/// <summary>True if 'Filepath' is a temporary file that will get deleted on close</summary>
		public readonly bool TmpFile;

		/// <summary>Raised when the process exits</summary>
		public event EventHandler ProcessExited;

		public BufferedProcess(LaunchApp launch)
		{
			m_lock = new object();
			
			// File open options
			FileMode mode = launch.AppendOutputFile ? FileMode.Append : FileMode.Create;
			FileOptions opts = FileOptions.Asynchronous|FileOptions.RandomAccess;
			TmpFile = false;

			// Get a file to capture the process output in
			Filepath = launch.OutputFilepath;
			if (string.IsNullOrEmpty(Filepath))
			{
				Filepath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
				mode = FileMode.Create;
				opts |= FileOptions.DeleteOnClose;
				TmpFile = true;
			}
				
			// Open the file that will receive the captured output
			m_outp = new FileStream(Filepath, mode, FileAccess.Write, FileShare.Read, Constants.FileReadChunkSize, opts);
					
			// Start the process
			ProcessStartInfo info = new ProcessStartInfo
			{
				UseShellExecute        = false,
				RedirectStandardOutput = launch.CaptureStdout,
				RedirectStandardError  = launch.CaptureStderr,
				CreateNoWindow         =!launch.ShowWindow,
				FileName               = launch.Executable,
				Arguments              = launch.Arguments,
				WorkingDirectory       = launch.WorkingDirectory
			};
			m_process = new Process{StartInfo = info};
			m_process.Exited += (s,a) =>
			{
				Log.Info("Process {0} exited", launch.Executable);
				if (ProcessExited != null)
					ProcessExited(s,a);
			};
			m_process.Start();
			Log.Info("Process {0} started", m_process.ProcessName);
			
			// Capture stdout
			if (launch.CaptureStdout)
			{
				m_outbuf = new byte[1];
				m_process.StandardOutput.BaseStream.BeginRead(m_outbuf, 0, m_outbuf.Length, DataRecv, new AsyncData(m_process.StandardOutput.BaseStream, m_outbuf));
			}
			
			// Capture stderr
			if (launch.CaptureStderr)
			{
				m_errbuf = new byte[1];
				m_process.StandardError.BaseStream.BeginRead(m_errbuf, 0, m_errbuf.Length, DataRecv, new AsyncData(m_process.StandardError.BaseStream, m_errbuf));
			}
		}

		/// <summary>Data received on the process' standard output/error</summary>
		private void DataRecv(IAsyncResult ar)
		{
			try
			{
				AsyncData data = (AsyncData)ar.AsyncState;
				int read = data.Stream.EndRead(ar);
				lock (m_lock)
				{
					if (m_outp == null) return;
					m_outp.Write(data.Buffer, 0, read);
					m_outp.Flush();
					data.Stream.BeginRead(data.Buffer, 0, data.Buffer.Length, DataRecv, data);
				}
			}
			catch (Exception ex) { Log.Exception(ex, "Process output receive exception"); }
			//catch {}
		}
			
		/// <summary>Cleanup</summary>
		public void Dispose()
		{
			lock (m_lock)
			{
				if (m_process != null)
				{
					Log.Info("Disposing process {0}", m_process.ProcessName);
					if (!m_process.HasExited)
						if (!m_process.CloseMainWindow())
							m_process.Kill();
					m_process.Dispose();
					m_process = null;
				}
				if (m_outp != null)
				{
					Log.Info("Disposing process capture file");
					m_outp.Dispose();
					m_outp = null;
				}
			}
		}
	}
}
