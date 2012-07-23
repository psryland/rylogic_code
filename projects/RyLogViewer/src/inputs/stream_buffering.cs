using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.gui;
using pr.util;

namespace RyLogViewer
{
	/// <summary>Parts of the Main form related to buffering non-file stream into an output file</summary>
	public partial class Main :Form
	{
		private BufferedProcess    m_buffered_process;
		private BufferedTcpNetConn m_buffered_tcp_netconn;
		private BufferedUdpNetConn m_buffered_udp_netconn;
		private BufferedSerialConn m_buffered_serialconn;
		private BufferedPipeConn   m_buffered_pipeconn;

		/// <summary>Launch a process, piping its output into a temporary file</summary>
		private void LaunchProcess(LaunchApp conn)
		{
			BufferedProcess buffered_process = null;
			try
			{
				// Close any currently open file
				// Strictly, we don't have to close because OpenLogFile closes before opening
				// however if the user reopens the same process the existing process will hold
				// a lock to the capture file preventing the new process being created.
				CloseLogFile();
				
				// Launch the process with standard output/error redirected to the temporary file
				buffered_process = new BufferedProcess(conn);
				
				// Give some UI feedback when the process ends
				buffered_process.ConnectionDropped += (s,a)=>
					{
						Action proc_exit = () => SetTransientStatusMessage(string.Format("'{0}' exited", Path.GetFileName(conn.Executable)), Color.Azure, Color.Blue);
						BeginInvoke(proc_exit);
					};
			
				// Open the capture file created by buffered_process
				OpenLogFile(buffered_process.Filepath, !buffered_process.TmpFile);
				buffered_process.Start();
				
				// Pass over the ref
				m_buffered_process = buffered_process;
				buffered_process = null;
			}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Failed to launch child process {0} {1} -> {2}", conn.Executable, conn.Arguments, conn.OutputFilepath);
				Misc.ShowErrorMessage(this, ex, string.Format("Failed to launch child process {0}.",conn.Executable),Resources.FailedToLaunchProcess);
			}
			finally 
			{
				if (buffered_process != null)
					buffered_process.Dispose();
			}
		}
		
		/// <summary>Open a tcp network connection and log anything read</summary>
		private void LogTcpNetConnection(NetConn conn)
		{
			BufferedTcpNetConn buffered_tcp_netconn = null;
			try
			{
				// Close any currently open file
				// Strictly, we don't have to close because OpenLogFile closes before opening
				// however if the user reopens the same connection the existing connection will
				// hold a lock on the capture file preventing the new connection being created.
				CloseLogFile();
				
				// Launch the process with standard output/error redirected to the temporary file
				buffered_tcp_netconn = new BufferedTcpNetConn(conn);
				
				// Give some UI feedback if the connection drops
				buffered_tcp_netconn.ConnectionDropped += (s,a)=>
					{
						Action proc_exit = () => SetTransientStatusMessage("Connection dropped", Color.Azure, Color.Blue);
						BeginInvoke(proc_exit);
					};
			
				// Open the capture file created by buffered_process
				OpenLogFile(buffered_tcp_netconn.Filepath, !buffered_tcp_netconn.TmpFile);
				buffered_tcp_netconn.Start(this);
				
				// Pass over the ref
				m_buffered_tcp_netconn = buffered_tcp_netconn;
				buffered_tcp_netconn = null;
			}
			catch (OperationCanceledException) {}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Failed to connect {0}:{1} -> {2}", conn.Hostname, conn.Port, conn.OutputFilepath);
				Misc.ShowErrorMessage(this, ex, string.Format("Failed to connect to {0}:{1}.",conn.Hostname,conn.Port),Resources.FailedToLaunchProcess);
			}
			finally 
			{
				if (buffered_tcp_netconn != null)
					buffered_tcp_netconn.Dispose();
			}
		}
		
		/// <summary>Open a network connection and log anything read</summary>
		private void LogUdpNetConnection(NetConn conn)
		{
			BufferedUdpNetConn buffered_udp_netconn = null;
			try
			{
				// Close any currently open file
				// Strictly, we don't have to close because OpenLogFile closes before opening
				// however if the user reopens the same connection the existing connection will
				// hold a lock on the capture file preventing the new connection being created.
				CloseLogFile();
				
				// Launch the process with standard output/error redirected to the temporary file
				buffered_udp_netconn = new BufferedUdpNetConn(conn);
				
				// Give some UI feedback if the connection drops (not that there is a connection with Udp... on well..)
				buffered_udp_netconn.ConnectionDropped += (s,a)=>
					{
						Action proc_exit = () => SetTransientStatusMessage("Connection dropped", Color.Azure, Color.Blue);
						BeginInvoke(proc_exit);
					};
			
				// Open the capture file created by buffered_process
				OpenLogFile(buffered_udp_netconn.Filepath, !buffered_udp_netconn.TmpFile);
				buffered_udp_netconn.Start();
				
				// Pass over the ref
				m_buffered_udp_netconn = buffered_udp_netconn;
				buffered_udp_netconn = null;
			}
			catch (OperationCanceledException) {}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Failed to open connection {0}:{1} -> {2}", conn.Hostname, conn.Port, conn.OutputFilepath);
				Misc.ShowErrorMessage(this, ex, string.Format("Failed to open connected to {0}:{1}.",conn.Hostname,conn.Port),Resources.FailedToLaunchProcess);
			}
			finally 
			{
				if (buffered_udp_netconn != null)
					buffered_udp_netconn.Dispose();
			}
		}
		
		/// <summary>Open a serial port connection and log anything read</summary>
		private void LogSerialConnection(SerialConn conn)
		{
			BufferedSerialConn buffered_serialconn = null;
			try
			{
				// Close any currently open file
				// Strictly, we don't have to close because OpenLogFile closes before opening
				// however if the user reopens the same connection the existing connection will
				// hold a lock on the capture file preventing the new connection being created.
				CloseLogFile();
				
				// Launch the process with standard output/error redirected to the temporary file
				buffered_serialconn = new BufferedSerialConn(conn);
				
				// Give some UI feedback if the connection drops
				buffered_serialconn.ConnectionDropped += (s,a)=>
					{
						Action proc_exit = () => SetTransientStatusMessage("Connection dropped", Color.Azure, Color.Blue);
						BeginInvoke(proc_exit);
					};
			
				// Open the capture file created by buffered_process
				OpenLogFile(buffered_serialconn.Filepath, !buffered_serialconn.TmpFile);
				buffered_serialconn.Start();

				// Pass over the ref
				m_buffered_serialconn = buffered_serialconn;
				buffered_serialconn = null;
			}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Failed to connect {0}:{1} -> {2}", conn.CommPort, conn.BaudRate, conn.OutputFilepath);
				Misc.ShowErrorMessage(this, ex, string.Format("Failed to connect to {0}:{1}.",conn.CommPort,conn.BaudRate),Resources.FailedToLaunchProcess);
			}
			finally
			{
				if (buffered_serialconn != null)
					buffered_serialconn.Dispose();
			}
		}
		
		/// <summary>Open a named pipe connection and log anything read</summary>
		private void LogNamedPipeConnection(PipeConn conn)
		{
			BufferedPipeConn buffered_pipeconn = null;
			try
			{
				// Close any currently open file
				// Strictly, we don't have to close because OpenLogFile closes before opening
				// however if the user reopens the same connection the existing connection will
				// hold a lock on the capture file preventing the new connection being created.
				CloseLogFile();
				
				// Create the buffered pipe connection
				buffered_pipeconn = new BufferedPipeConn(conn);
				
				// Give some UI feedback if the connection drops
				buffered_pipeconn.ConnectionDropped += (s,a)=>
					{
						Action proc_exit = () => SetTransientStatusMessage("Connection dropped", Color.Azure, Color.Blue);
						BeginInvoke(proc_exit);
					};
			
				// Open the capture file
				OpenLogFile(buffered_pipeconn.Filepath, !buffered_pipeconn.TmpFile);
				buffered_pipeconn.Start(this);
				
				// Pass over the ref
				m_buffered_pipeconn = buffered_pipeconn;
				buffered_pipeconn = null;
			}
			catch (OperationCanceledException) {}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Failed to connect {0} -> {1}", conn.PipeAddr, conn.OutputFilepath);
				Misc.ShowErrorMessage(this, ex, string.Format("Failed to connect to {0}.",conn.PipeAddr) ,Resources.FailedToLaunchProcess);
			}
			finally
			{
				if (buffered_pipeconn != null)
					buffered_pipeconn.Dispose();
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
			public int Read;               // the valid data size in 'Buffer'
			public AsyncData(Stream s, byte[] b) { Stream = s; Buffer = b; Read = 0; }
		}
		protected const int BufBlockSize = 4096;
		protected readonly object m_lock;   // Sync writes to the file
		protected FileStream m_outp;        // The file that captured output is written to
		
		/// <summary>The filepath of the file that contains the redirected output</summary>
		public readonly string Filepath;

		/// <summary>True if 'Filepath' is a temporary file that will get deleted on close</summary>
		public readonly bool TmpFile;

		/// <summary>Raised if the connection to the host is lost</summary>
		public event EventHandler ConnectionDropped;
		protected void RaiseConnectionDropped()
		{
			if (ConnectionDropped != null)
				ConnectionDropped(this, EventArgs.Empty);
		}

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
			m_outp = new FileStream(Filepath, mode, FileAccess.Write, FileShare.Read, BufBlockSize, opts);
		}
		
		/// <summary>Should return true to continue reading data</summary>
		protected abstract bool IsConnected { get; }

		/// <summary>Handler for async reads from a stream</summary>
		protected virtual void DataRecv(IAsyncResult ar)
		{
			AsyncData data = (AsyncData)ar.AsyncState;
			lock (m_lock)
			{
				try
				{
					data.Read = data.Stream.EndRead(ar);
					if (m_outp == null) return;
					if (data.Read != 0 || IsConnected)
					{
						m_outp.Write(data.Buffer, 0, data.Read);
						m_outp.Flush();
						data.Stream.BeginRead(data.Buffer, 0, data.Buffer.Length, DataRecv, data);
						return;
					}
				}
				catch (Exception ex)
				{
					var type = GetType().DeclaringType;
					Log.Exception(this, ex, "[{0}] Data receive exception", type == null ? "" : type.Name);
				}
				RaiseConnectionDropped();
			}
		}
		
		/// <summary>Cleanup</summary>
		public virtual void Dispose()
		{
			if (m_outp != null)
			{
				lock (m_lock)
				{
					Log.Info(this, "Disposing buffer stream capture file");
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
				Log.Info(this, "Process {0} exited", launch.Executable);
				RaiseConnectionDropped();
			};
		}
		
		public void Start()
		{
			m_process.Start();
			Log.Info(this, "Process {0} started", m_process.ProcessName);
			
			// Attach to the window console so we can forward received data to it
			if (m_launch.ShowWindow)
				Win32.AttachConsole(m_process.Id);
			
			// Capture stdout
			if (m_launch.CaptureStdout)
				m_process.StandardOutput.BaseStream.BeginRead(m_outbuf, 0, m_outbuf.Length, DataRecv, new AsyncData(m_process.StandardOutput.BaseStream, m_outbuf));
			
			// Capture stderr
			if (m_launch.CaptureStderr)
				m_process.StandardError.BaseStream.BeginRead(m_errbuf, 0, m_errbuf.Length, DataRecv, new AsyncData(m_process.StandardError.BaseStream, m_errbuf));
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
			
			// If we're "showing the window" forward recieved data to the window
			AsyncData data = (AsyncData)ar.AsyncState;
			lock (m_lock)
			{
				Win32.AttachConsole(m_process.Id);
				char[] msg = Encoding.ASCII.GetChars(data.Buffer, 0, data.Read);
				Console.Write(msg);
			}
		}
		
		/// <summary>Cleanup</summary>
		public override void Dispose()
		{
			base.Dispose();
			if (m_process != null)
			{
				lock (m_lock)
				{
					Log.Info(this, "Disposing process {0}", m_process.ProcessName);
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
	public class BufferedTcpNetConn :BufferedStream
	{
		private readonly NetConn m_conn;
		private readonly byte[] m_buf;
		private TcpClient m_tcp;

		public BufferedTcpNetConn(NetConn conn)
		:base(conn.OutputFilepath, conn.AppendOutputFile)
		{
			m_conn = conn;
			m_buf = new byte[BufBlockSize];
			m_tcp = new TcpClient();
		}

		/// <summary>Start asynchronously reading from the tcp client</summary>
		public void Start(Main parent)
		{
			ProgressForm connect = new ProgressForm("Connecting..."
				,string.Format("Connecting to remote host: {0}:{1}", m_conn.Hostname, m_conn.Port)
				,(s,a)=>
				{
					BackgroundWorker bgw = (BackgroundWorker)s;
					bgw.ReportProgress(0, new ProgressForm.UserState{ProgressBarVisible = false, Icon = parent.Icon});
					
					// Connect async
					var ar = m_tcp.BeginConnect(m_conn.Hostname, m_conn.Port, null, null);
					for (;!bgw.CancellationPending && !ar.AsyncWaitHandle.WaitOne(500);){}
					a.Cancel = bgw.CancellationPending;
					if (!a.Cancel)
					{
						m_tcp.EndConnect(ar);
						NetworkStream stream = m_tcp.GetStream();
						stream.BeginRead(m_buf, 0, m_buf.Length, DataRecv, new AsyncData(stream, m_buf));
					}
				});
			
			if (connect.ShowDialog(parent) != DialogResult.OK)
				throw new OperationCanceledException("Connecting cancelled");
		}

		/// <summary>Returns true if the tcp client is connected</summary>
		protected override bool IsConnected
		{
			get { return m_tcp != null && m_tcp.Connected; }
		}

		/// <summary>Cleanup</summary>
		public override void Dispose()
		{
			base.Dispose();
			if (m_tcp != null)
			{
				lock (m_lock)
				{
					Log.Info(this, "Disposing tcp client {0}", m_conn.Hostname);
					m_tcp.Close();
					m_tcp = null;
				}
			}
		}
	}

	/// <summary>Manages a network connection and reading its incoming data</summary>
	public class BufferedUdpNetConn :BufferedStream
	{
		private readonly NetConn m_conn;
		private readonly byte[] m_buf;
		private UdpClient m_udp;
		private bool m_specific_host;
		
		public BufferedUdpNetConn(NetConn conn)
		:base(conn.OutputFilepath, conn.AppendOutputFile)
		{
			m_conn = conn;
			m_buf = new byte[BufBlockSize];
			m_udp = new UdpClient(m_conn.Port);
			m_specific_host = !string.IsNullOrEmpty(m_conn.Hostname);
		}

		/// <summary></summary>
		private IAsyncResult BeginRecv()
		{
			EndPoint src = new IPEndPoint(IPAddress.Any, m_conn.Port);
			return m_specific_host
				? m_udp.Client.BeginReceive(m_buf, 0, m_buf.Length, SocketFlags.None, DataRecv, null)
				: m_udp.Client.BeginReceiveFrom(m_buf, 0, m_buf.Length, SocketFlags.None, ref src, DataRecv, null);
		}

		private int EndRecv(IAsyncResult ar)
		{
			EndPoint src = new IPEndPoint(IPAddress.Any, m_conn.Port);
			return m_specific_host
				? m_udp.Client.EndReceive(ar)
				: m_udp.Client.EndReceiveFrom(ar, ref src);
		}

		/// <summary>Start asynchronously reading from the tcp client</summary>
		public void Start()
		{
			if (m_specific_host) m_udp.Connect(m_conn.Hostname, 0);
			BeginRecv();
		}

		/// <summary>Returns true if the tcp client is connected</summary>
		protected override bool IsConnected
		{
			get { return m_udp != null; }
		}

		/// <summary>Handler for async reads from a stream</summary>
		protected override void DataRecv(IAsyncResult ar)
		{
			lock (m_lock)
			{
				try
				{
					if (m_udp == null)
						return;
					
					int read = EndRecv(ar);
					if (m_outp == null) return;
					m_outp.Write(m_buf, 0, read);
					m_outp.Flush();
					BeginRecv();
				}
				catch (Exception ex)
				{
					var type = GetType().DeclaringType;
					Log.Exception(this, ex, "[{0}] Data receive exception", type == null ? "" : type.Name);
				}
			}
		}

		/// <summary>Cleanup</summary>
		public override void Dispose()
		{
			base.Dispose();
			if (m_udp != null)
			{
				lock (m_lock)
				{
					Log.Info(this, "Disposing ucp client {0}", m_conn.Hostname);
					m_udp.Close();
					m_udp = null;
				}
			}
		}
	}
	
	/// <summary>Manages a serial port connection and reading its incoming data</summary>
	public class BufferedSerialConn :BufferedStream
	{
		private readonly SerialConn m_conn;
		private readonly byte[] m_buf;
		private SerialPort m_port;

		public BufferedSerialConn(SerialConn conn)
		:base(conn.OutputFilepath, conn.AppendOutputFile)
		{
			m_conn = conn;
			m_buf = new byte[BufBlockSize];
			m_port = new SerialPort(m_conn.CommPort, m_conn.BaudRate, m_conn.Parity, m_conn.DataBits, m_conn.StopBits)
				{
					Handshake = m_conn.FlowControl,
					DtrEnable = m_conn.DtrEnable,
					RtsEnable = m_conn.RtsEnable
				};
		}

		/// <summary>Start asynchronously reading from the tcp client</summary>
		public void Start()
		{
			m_port.Open();
			m_port.BaseStream.ReadTimeout = Constants.FilePollingRate;
			m_port.BaseStream.BeginRead(m_buf, 0, m_buf.Length, DataRecv, new AsyncData(m_port.BaseStream, m_buf));
		}

		/// <summary>Returns true while the serial port is connected</summary>
		protected override bool IsConnected
		{
			get { return m_port != null && m_port.IsOpen; }
		}

		/// <summary>Cleanup</summary>
		public override void Dispose()
		{
			base.Dispose();
			if (m_port != null)
			{
				lock (m_lock)
				{
					Log.Info(this, "Disposing serial port connection {0}", m_conn.CommPort);
					m_port.Dispose();
					m_port = null;
				}
			}
		}
	}

	/// <summary>Manages a named pipe connection and reading its incoming data</summary>
	public class BufferedPipeConn :BufferedStream
	{
		private readonly PipeConn m_conn;
		private readonly byte[] m_buf;
		private NamedPipeClientStream m_pipe;

		public BufferedPipeConn(PipeConn conn)
		:base(conn.OutputFilepath, conn.AppendOutputFile)
		{
			m_conn = conn;
			m_buf = new byte[BufBlockSize];
			m_pipe = new NamedPipeClientStream(m_conn.ServerName, m_conn.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
		}

		/// <summary>Start asynchronously reading from the tcp client</summary>
		public void Start(Main parent)
		{
			ProgressForm connect = new ProgressForm("Connecting...", "Connecting to "+m_conn.PipeAddr, (s,a)=>
				{
					BackgroundWorker bgw = (BackgroundWorker)s;
					bgw.ReportProgress(0, new ProgressForm.UserState{ProgressBarVisible = false, Icon = parent.Icon});
					
					while (!m_pipe.IsConnected && !bgw.CancellationPending)
						try { m_pipe.Connect(100); } catch (TimeoutException) {}
					
					a.Cancel = bgw.CancellationPending;
					if (!a.Cancel)
					{
						m_pipe.ReadMode = PipeTransmissionMode.Byte;
						m_pipe.BeginRead(m_buf, 0, m_buf.Length, DataRecv, new AsyncData(m_pipe, m_buf));
					}
				});
			if (connect.ShowDialog(parent) != DialogResult.OK)
				throw new OperationCanceledException("Connecting cancelled");
		}

		/// <summary>Returns true while the pipe is connected</summary>
		protected override bool IsConnected
		{
			get { return m_pipe != null && m_pipe.IsConnected; }
		}

		/// <summary>Cleanup</summary>
		public override void Dispose()
		{
			base.Dispose();
			if (m_pipe != null)
			{
				lock (m_lock)
				{
					Log.Info(this, "Disposing named pipe connection {0}", m_conn.PipeName);
					m_pipe.Dispose();
					m_pipe = null;
				}
			}
		}
	}
}
