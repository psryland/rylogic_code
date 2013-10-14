using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
		/// <summary>Parts of the Main form related to buffering non-file streams into an output file</summary>
	public partial class Main :Form
	{
		private BufferedProcess m_buffered_process;

		/// <summary>Launch a process, piping its output into a temporary file</summary>
		private void LaunchProcess(LaunchApp conn)
		{
			Debug.Assert(conn != null);
			BufferedProcess buffered_process = null;
			try
			{
				// Close any currently open file
				// Strictly, we don't have to close because OpenLogFile closes before opening
				// however if the user reopens the same process the existing process will hold
				// a lock to the capture file preventing the new process being created.
				CloseLogFile();

				// Set options so that data always shows
				PrepareForStreamedData();

				// Launch the process with standard output/error redirected to the temporary file
				buffered_process = new BufferedProcess(conn);

				// Give some UI feedback when the process ends
				buffered_process.ConnectionDropped += (s,a)=>
					{
						this.BeginInvoke(() => SetStaticStatusMessage(string.Format("{0} exited", Path.GetFileName(conn.Executable)), Color.Black, Color.LightSalmon));
					};

				// Open the capture file created by buffered_process
				OpenSingleLogFile(buffered_process.Filepath, !buffered_process.TmpFile);
				buffered_process.Start();
				SetStaticStatusMessage("Connected", Color.Black, Color.LightGreen);

				// Pass over the ref
				if (m_buffered_process != null) m_buffered_process.Dispose();
				m_buffered_process = buffered_process;
				buffered_process = null;
			}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Failed to launch child process {0} {1} -> {2}".Fmt(conn.Executable, conn.Arguments, conn.OutputFilepath));
				Misc.ShowMessage(this, string.Format("Failed to launch child process {0}.",conn.Executable),Resources.FailedToLaunchProcess, MessageBoxIcon.Error, ex);
			}
			finally
			{
				if (buffered_process != null)
					buffered_process.Dispose();
			}
		}
	}

	/// <summary>Manages a process and reading its output.</summary>
	public class BufferedProcess :BufferedStream
	{
		private readonly LaunchApp m_launch; // The launch description
		private readonly byte[] m_outbuf;    // A buffer for standard output data
		private readonly byte[] m_errbuf;    // A buffer for standard error data
		private Process m_process;           // The running process

		public BufferedProcess(LaunchApp launch)
		:base(launch.OutputFilepath, launch.AppendOutputFile)
		{
			m_launch = launch;
			m_outbuf = new byte[BufBlockSize];
			m_errbuf = new byte[BufBlockSize];

			// Create the process
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
				Log.Info(this, "Process {0} exited".Fmt(launch.Executable));
				RaiseConnectionDropped();
			};
		}
		public override void Dispose()
		{
			base.Dispose();
			if (m_process != null)
			{
				lock (m_lock)
				{
					Log.Info(this, "Disposing process {0}".Fmt(m_process.StartInfo.FileName));
					
					// HasExited can throw, Dispose() should be all that's needed anyway
					//if (!m_process.HasExited)
					//	if (!m_process.CloseMainWindow())
					//		m_process.Kill();

					m_process.Dispose();
					m_process = null;
				}
			}
		}

		public void Start()
		{
			m_process.Start();
			Log.Info(this, "Process {0} started".Fmt(m_process.StartInfo.FileName));

			// Attach to the window console so we can forward received data to it
			if (m_launch.ShowWindow)
				Win32.AttachConsole(m_process.Id);

			// Capture stdout
			if (m_launch.CaptureStdout)
			{
				var stdout = new StreamSource(m_process.StandardOutput.BaseStream);
				stdout.BeginRead(m_outbuf, 0, m_outbuf.Length, DataRecv, new AsyncData(stdout, m_outbuf));
			}

			// Capture stderr
			if (m_launch.CaptureStderr)
			{
				var stderr = new StreamSource(m_process.StandardError.BaseStream);
				stderr.BeginRead(m_errbuf, 0, m_errbuf.Length, DataRecv, new AsyncData(stderr, m_errbuf));
			}
		}

		/// <summary>True while the process is still running</summary>
		protected override bool IsConnected
		{
			get { return m_process != null && !m_process.HasExited; }
		}

		/// <summary>Handler for async reads from a stream</summary>
		protected override void DataRecv(IAsyncResult ar)
		{
			base.DataRecv(ar);
			if (!m_launch.ShowWindow) return;

			// If we're "showing the window" forward received data to the window
			AsyncData data = (AsyncData)ar.AsyncState;
			lock (m_lock)
			{
				Win32.AttachConsole(m_process.Id);
				char[] msg = Encoding.ASCII.GetChars(data.Buffer, 0, data.Read);
				Console.Write(msg);
			}
		}
	}
}