using System;
using System.Drawing;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Rylogic.Gui.WinForms;
using Rylogic.Utility;

namespace RyLogViewer
{
	[DataContract]
	public class PipeConn :ICloneable
	{
		// Notes: It is usually recommended to set DTR and RTS true.
		// If the connected device uses these signals, it will not transmit before
		// the signals are set

		[DataMember] public string       ServerName       = string.Empty;
		[DataMember] public string       PipeName         = string.Empty;
		[DataMember] public string       OutputFilepath   = string.Empty;
		[DataMember] public bool         AppendOutputFile = true;

		public string PipeAddr
		{
			get { return string.Format(@"\\{0}\pipe\{1}",ServerName,PipeName); }
			set
			{
				var parts = value.Replace(@"\\", string.Empty).Split('\\');
				if (parts.Length != 3 || parts[1] != "pipe") throw new ArgumentException("Invalid pipe name");
				ServerName = parts[0];
				PipeName = parts[2];
			}
		}

		public PipeConn() {}
		public PipeConn(PipeConn rhs)
		{
			ServerName       = rhs.ServerName;
			PipeName         = rhs.PipeName;
			OutputFilepath   = rhs.OutputFilepath;
			AppendOutputFile = rhs.AppendOutputFile;
		}
		public override string ToString()
		{
			return PipeAddr;
		}
		public object Clone()
		{
			return new PipeConn(this);
		}
	}

	/// <summary>Parts of the Main form related to buffering non-file streams into an output file</summary>
	public partial class Main
	{
		private BufferedPipeConn m_buffered_pipeconn;

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
				Src = null;

				// Set options so that data always shows
				PrepareForStreamedData(conn.OutputFilepath);

				// Create the buffered pipe connection
				buffered_pipeconn = new BufferedPipeConn(conn);

				// Give some UI feedback if the connection drops
				buffered_pipeconn.ConnectionDropped += (s,a)=>
				{
					this.BeginInvoke(() => SetStaticStatusMessage("Connection dropped", Color.Black, Color.LightSalmon));
				};

				// Open the capture file
				OpenSingleLogFile(buffered_pipeconn.Filepath, !buffered_pipeconn.TmpFile);
				buffered_pipeconn.Start(this);
				SetStaticStatusMessage("Connected", Color.Black, Color.LightGreen);

				// Pass over the ref
				if (m_buffered_pipeconn != null) m_buffered_pipeconn.Dispose();
				m_buffered_pipeconn = buffered_pipeconn;
				buffered_pipeconn = null;
			}
			catch (OperationCanceledException) {}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, $"Failed to connect {conn.PipeAddr} -> {conn.OutputFilepath}");
				Misc.ShowMessage(this, $"Failed to connect to {conn.PipeAddr}.", Application.ProductName, MessageBoxIcon.Error, ex);
			}
			finally
			{
				if (buffered_pipeconn != null)
					buffered_pipeconn.Dispose();
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
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (m_pipe != null)
			{
				lock (m_lock)
				{
					Log.Write(ELogLevel.Info, $"Disposing named pipe connection {m_conn.PipeName}");
					m_pipe.Dispose();
					m_pipe = null;
				}
			}
		}

		/// <summary>Start asynchronously reading from the TCP client</summary>
		public void Start(Main parent)
		{
			var connect = new ProgressForm("Connecting...", "Connecting to "+m_conn.PipeAddr, null, ProgressBarStyle.Marquee, (s,a,cb)=>
			{
				cb(new ProgressForm.UserState{ProgressBarVisible = false, Icon = parent.Icon});

				while (!m_pipe.IsConnected && !s.CancelPending)
					try { m_pipe.Connect(100); } catch (TimeoutException) {}

				if (!s.CancelPending)
				{
					m_pipe.ReadMode = PipeTransmissionMode.Byte;
					var src = new StreamSource(m_pipe);
					src.BeginRead(m_buf, 0, m_buf.Length, DataRecv, new AsyncData(src, m_buf));
				}
			});
			using (connect)
				if (connect.ShowDialog(parent) != DialogResult.OK)
					throw new OperationCanceledException("Connecting cancelled");
		}

		/// <summary>Returns true while the pipe is connected</summary>
		protected override bool IsConnected
		{
			get { return m_pipe != null && m_pipe.IsConnected; }
		}
	}
}