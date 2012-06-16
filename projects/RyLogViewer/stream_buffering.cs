using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.gui;
using pr.util;

namespace RyLogViewer
{
	/// <summary>Parts of the Main form related to buffering non-file stream into an output file</summary>
	public partial class Main :Form
	{
		private BufferedProcess m_buffered_process;
		private BufferedNetConn m_buffered_netconn;

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
				m_buffered_process.Start();
			}
			catch (Exception ex)
			{
				Log.Exception(ex, "Failed to launch child process {0} {1} -> {2}", launch.Executable, launch.Arguments, launch.OutputFilepath);
				MessageBox.Show(this
					,string.Format("Failed to launch child process {0}\r\n.Error: {1}",launch.Executable,ex.Message)
					,Resources.FailedToLaunchProcess, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		
		/// <summary>Open a network connection and log anything read</summary>
		private void LogNetworkConnection(NetConn conn)
		{
			try
			{
				// Close any currently open file
				// Strictly, we don't have to close because OpenLogFile closes before opening
				// however if the user reopens the same connection the existing connection will
				// hold a lock on the capture file preventing the new connection being created.
				CloseLogFile();
				
				// Launch the process with standard output/error redirected to the temporary file
				BufferedNetConn buffered_netconn = new BufferedNetConn(conn);
				
				// Give some UI feedback if the connection drops
				buffered_netconn.ConnectionDropped += (s,a)=>
					{
						Action proc_exit = () => SetTransientStatusMessage("Connection dropped");
						BeginInvoke(proc_exit);
					};
			
				// Open the capture file created by buffered_process
				OpenLogFile(buffered_netconn.Filepath, !buffered_netconn.TmpFile);
				m_buffered_netconn = buffered_netconn;
				m_buffered_netconn.Start(this);
			}
			catch (Exception ex)
			{
				Log.Exception(ex, "Failed to connect {0}:{1} -> {2}", conn.Hostname, conn.Port, conn.OutputFilepath);
				MessageBox.Show(this
					,string.Format("Failed to connect to {0}:{1}\r\n.Error: {2}",conn.Hostname,conn.Port,ex.Message)
					,Resources.FailedToLaunchProcess, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
	
	/// <summary>
	/// Base class for buffering a non-seekable stream into a temporary file.
	/// The data source starts from the construction of this class. Captured output
	/// is written to the temporary file. When the data source ends this instance
	/// hangs around until the associated file is closed.</summary>
	public abstract class BufferedStream :IDisposable
	{
		protected class AsyncData
		{
			public readonly Stream Stream;
			public readonly byte[] Buffer;
			public AsyncData(Stream s, byte[] b) { Stream = s; Buffer = b; }
		}

		protected readonly object m_lock;   // Sync writes to the file
		protected FileStream m_outp;        // The file that captured output is written to
		
		/// <summary>The filepath of the file that contains the redirected output</summary>
		public readonly string Filepath;

		/// <summary>True if 'Filepath' is a temporary file that will get deleted on close</summary>
		public readonly bool TmpFile;

		protected BufferedStream(string output_filepath, bool append)
		{
			m_lock = new object();
			
			// File open options
			FileMode mode = append ? FileMode.Append : FileMode.Create;
			FileOptions opts = FileOptions.Asynchronous|FileOptions.RandomAccess;
			TmpFile = false;
			
			// Get a file to capture the process output in
			Filepath = output_filepath;
			if (string.IsNullOrEmpty(Filepath))
			{
				Filepath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
				mode = FileMode.Create;
				opts |= FileOptions.DeleteOnClose;
				TmpFile = true;
			}
			
			// Open the file that will receive the captured output
			m_outp = new FileStream(Filepath, mode, FileAccess.Write, FileShare.Read, Constants.FileReadChunkSize, opts);
		}
		
		/// <summary>Handler for async reads from a stream</summary>
		protected virtual void DataRecv(IAsyncResult ar)
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
		
		/// <summary>Cleanup</summary>
		public virtual void Dispose()
		{
			if (m_outp != null)
			{
				lock (m_lock)
				{
					Log.Info("Disposing buffer stream capture file");
					m_outp.Dispose();
					m_outp = null;
				}
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
		
		/// <summary>Raised when the process exits</summary>
		public event EventHandler ProcessExited;

		public BufferedProcess(LaunchApp launch)
		:base(launch.OutputFilepath, launch.AppendOutputFile)
		{
			m_launch = launch;
			m_outbuf = new byte[1];
			m_errbuf = new byte[1];
			
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
				Log.Info("Process {0} exited", launch.Executable);
				if (ProcessExited != null)
					ProcessExited(s,a);
			};
		}
		
		public void Start()
		{
			m_process.Start();
			Log.Info("Process {0} started", m_process.ProcessName);
			
			// Capture stdout
			if (m_launch.CaptureStdout)
				m_process.StandardOutput.BaseStream.BeginRead(m_outbuf, 0, m_outbuf.Length, DataRecv, new AsyncData(m_process.StandardOutput.BaseStream, m_outbuf));
			
			// Capture stderr
			if (m_launch.CaptureStderr)
				m_process.StandardError.BaseStream.BeginRead(m_errbuf, 0, m_errbuf.Length, DataRecv, new AsyncData(m_process.StandardError.BaseStream, m_errbuf));
		}

		/// <summary>Handler for async reads from a stream</summary>
		protected override void DataRecv(IAsyncResult ar)
		{
			try { base.DataRecv(ar); }
			catch (Exception ex) { Log.Exception(ex, "Process output receive exception"); }
			//catch {}
		}

		/// <summary>Cleanup</summary>
		public override void Dispose()
		{
			base.Dispose();
			if (m_process != null)
			{
				lock (m_lock)
				{
					Log.Info("Disposing process {0}", m_process.ProcessName);
					if (!m_process.HasExited)
						if (!m_process.CloseMainWindow())
							m_process.Kill();
					
					m_process.Dispose();
					m_process = null;
				}
			}
		}
	}

	/// <summary>Manages a network connection and reading its incoming data</summary>
	public class BufferedNetConn :BufferedStream
	{
		private readonly NetConn m_conn;
		private readonly byte[] m_buf;
		private TcpClient m_client;
		
		/// <summary>Raised if the connection to the host is lost</summary>
		public event EventHandler ConnectionDropped;

		public BufferedNetConn(NetConn conn)
		:base(conn.OutputFilepath, conn.AppendOutputFile)
		{
			m_conn = conn;
			m_buf = new byte[1];
			m_client = new TcpClient();
		}

		/// <summary>Start asynchronously reading from the tcp client</summary>
		public void Start(IWin32Window parent)
		{
			ProgressForm connect = new ProgressForm("Connecting..."
				,string.Format("Connecting to remote host: {0}:{1}", m_conn.Hostname, m_conn.Port)
				,(s,a)=>
				{
					BackgroundWorker bgw = (BackgroundWorker)s;
					bgw.ReportProgress(0, new ProgressForm.UserState{ProgressBarVisible = false});
					
					//new Socket(AddressFamily.i
					//new UdpClient().Client.ProtocolType = ;
					m_client.Connect(m_conn.Hostname, m_conn.Port);
					
					NetworkStream stream = m_client.GetStream();
					stream.ReadTimeout = Constants.FilePollingRate;
					stream.BeginRead(m_buf, 0, m_buf.Length, DataRecv, new AsyncData(stream, m_buf));
				}, null);
			
			if (connect.ShowDialog(parent) != DialogResult.OK)
				throw new OperationCanceledException("Connecting cancelled");
		}

		/// <summary>Handler for async reads from a stream</summary>
		protected override void DataRecv(IAsyncResult ar)
		{
			try { base.DataRecv(ar); }
			catch (Exception ex) { Log.Exception(ex, "Network connect receive exception"); }
			if (ConnectionDropped != null && (m_client == null || !m_client.Connected))
				ConnectionDropped(this, EventArgs.Empty);
		}

		/// <summary>Cleanup</summary>
		public override void Dispose()
		{
			base.Dispose();
			if (m_client != null)
			{
				lock (m_lock)
				{
					Log.Info("Disposing tcp client {0}", m_client.Client.RemoteEndPoint);
					m_client.GetStream().Dispose();
					m_client.Close();
					m_client = null;
				}
			}
		}
	}
}
