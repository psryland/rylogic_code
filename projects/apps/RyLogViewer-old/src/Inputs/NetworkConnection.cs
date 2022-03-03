using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Rylogic.Gui.WinForms;
using Rylogic.Net;
using Rylogic.Utility;

namespace RyLogViewer
{
	[DataContract]
	public class NetConn :ICloneable
	{
		[DataMember] public ProtocolType ProtocolType     = ProtocolType.Tcp;
		[DataMember] public bool         Listener         = true;
		[DataMember] public string       Hostname         = string.Empty;
		[DataMember] public ushort       Port             = 5555;
		[DataMember] public Proxy.EType  ProxyType        = Proxy.EType.None;
		[DataMember] public string       ProxyHostname    = string.Empty;
		[DataMember] public ushort       ProxyPort        = 5555;
		[DataMember] public string       ProxyUserName    = string.Empty;
		             public string       ProxyPassword    = string.Empty; // don't store passwords
		[DataMember] public string       OutputFilepath   = string.Empty;
		[DataMember] public bool         AppendOutputFile = true;

		public NetConn() {}
		public NetConn(NetConn rhs)
		{
			ProtocolType     = rhs.ProtocolType     ;
			Listener         = rhs.Listener         ;
			Hostname         = rhs.Hostname         ;
			Port             = rhs.Port             ;
			ProxyType        = rhs.ProxyType        ;
			ProxyHostname    = rhs.ProxyHostname    ;
			ProxyPort        = rhs.ProxyPort        ;
			ProxyUserName    = rhs.ProxyUserName    ;
			ProxyPassword    = rhs.ProxyPassword    ;
			OutputFilepath   = rhs.OutputFilepath   ;
			AppendOutputFile = rhs.AppendOutputFile ;
		}
		public override string ToString()
		{
			return Hostname;
		}
		public object Clone()
		{
			return new NetConn(this);
		}
	}

	/// <summary>Parts of the Main form related to buffering non-file streams into an output file</summary>
	public partial class Main
	{
		private BufferedTcpNetConn m_buffered_tcp_netconn;
		private BufferedUdpNetConn m_buffered_udp_netconn;

		/// <summary>Open a TCP network connection and log anything read</summary>
		private void LogTcpNetConnection(NetConn conn)
		{
			BufferedTcpNetConn buffered_tcp_netconn = null;
			try
			{
				// Close any currently open file
				// Strictly, we don't have to close because OpenLogFile closes before opening
				// however if the user reopens the same connection the existing connection will
				// hold a lock on the capture file preventing the new connection being created.
				Src = null;

				// Set options so that data always shows
				PrepareForStreamedData(conn.OutputFilepath);

				// Launch the process with standard output/error redirected to the temporary file
				buffered_tcp_netconn = new BufferedTcpNetConn(conn);

				// Give some UI feedback if the connection drops
				buffered_tcp_netconn.ConnectionDropped += (s,a)=>
				{
					this.BeginInvoke(() => SetStaticStatusMessage("Connection dropped", Color.Black, Color.LightSalmon));
				};

				// Open the capture file created by buffered_process
				OpenSingleLogFile(buffered_tcp_netconn.Filepath, !buffered_tcp_netconn.TmpFile);
				buffered_tcp_netconn.Start(this);
				SetStaticStatusMessage("Connected", Color.Black, Color.LightGreen);

				// Pass over the ref
				if (m_buffered_tcp_netconn != null) m_buffered_tcp_netconn.Dispose();
				m_buffered_tcp_netconn = buffered_tcp_netconn;
				buffered_tcp_netconn = null;
			}
			catch (OperationCanceledException) {}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, $"Failed to connect {conn.Hostname}:{conn.Port} -> {conn.OutputFilepath}");
				Misc.ShowMessage(this, $"Failed to connect to {conn.Hostname}:{conn.Port}.", Application.ProductName, MessageBoxIcon.Error, ex);
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
				Src = null;

				// Set options so that data always shows
				PrepareForStreamedData(conn.OutputFilepath);

				// Launch the process with standard output/error redirected to the temporary file
				buffered_udp_netconn = new BufferedUdpNetConn(conn);

				// Give some UI feedback if the connection drops (not that there is a connection with UDP... on well..)
				buffered_udp_netconn.ConnectionDropped += (s,a)=>
				{
					this.BeginInvoke(() => SetStaticStatusMessage("Connection dropped", Color.Black, Color.LightSalmon));
				};

				// Open the capture file created by buffered_process
				OpenSingleLogFile(buffered_udp_netconn.Filepath, !buffered_udp_netconn.TmpFile);
				buffered_udp_netconn.Start();
				SetStaticStatusMessage("Connected", Color.Black, Color.LightGreen);

				// Pass over the ref
				if (m_buffered_udp_netconn != null) m_buffered_udp_netconn.Dispose();
				m_buffered_udp_netconn = buffered_udp_netconn;
				buffered_udp_netconn = null;
			}
			catch (OperationCanceledException) {}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, $"Failed to open connection {conn.Hostname}:{conn.Port} -> {conn.OutputFilepath}");
				Misc.ShowMessage(this, $"Failed to open connected to {conn.Hostname}:{conn.Port}.", Application.ProductName, MessageBoxIcon.Error, ex);
			}
			finally
			{
				if (buffered_udp_netconn != null)
					buffered_udp_netconn.Dispose();
			}
		}
	}
	/// <summary>Manages a network connection and reading its incoming data</summary>
	public class BufferedTcpNetConn :BufferedStream
	{
		private readonly NetConn m_conn;
		private readonly byte[] m_buf;
		private TcpClient m_client;

		public BufferedTcpNetConn(NetConn conn)
		:base(conn.OutputFilepath, conn.AppendOutputFile)
		{
			m_conn  = conn;
			m_buf   = new byte[BufBlockSize];
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (m_client != null)
			{
				lock (m_lock)
				{
					Log.Write(ELogLevel.Info, $"Disposing TCP client {m_conn.Hostname}");
					m_client.Close();
					m_client = null;
				}
			}
		}

		/// <summary>Start asynchronously reading from the TCP client</summary>
		public void Start(Main parent)
		{
			if (m_conn.Listener)
				StartListener(parent);
			else
				StartClient(parent);
		}

		/// <summary>Start a TCP client that connects to a server that is producing log data</summary>
		private void StartClient(Main parent)
		{
			var connect = new ProgressForm("Connecting...", $"Connecting to remote host: {m_conn.Hostname}:{m_conn.Port}", parent.Icon, ProgressBarStyle.Marquee, (s,a,cb)=>
			{
				cb(new ProgressForm.UserState{ProgressBarVisible = false, Icon = parent.Icon});

				m_client = new TcpClient();
				var proxy = m_conn.ProxyType == Proxy.EType.None ? null
					: Proxy.Create(m_conn.ProxyType, m_conn.ProxyHostname, m_conn.ProxyPort, m_conn.ProxyUserName, m_conn.ProxyPassword);

				// Connect async
				var ar = proxy != null
					? proxy.BeginConnect(m_conn.Hostname, m_conn.Port)
					: m_client.BeginConnect(m_conn.Hostname, m_conn.Port, null, null);

				for (;!s.CancelPending && !ar.AsyncWaitHandle.WaitOne(500);){}
				if (!s.CancelPending)
				{
					if (proxy != null)
						m_client = proxy.EndConnect(ar);
					else
						m_client.EndConnect(ar);

					var src = new StreamSource(m_client.GetStream());
					src.BeginRead(m_buf, 0, m_buf.Length, DataRecv, new AsyncData(src, m_buf));
				}
			});
			using (connect)
				if (connect.ShowDialog(parent) != DialogResult.OK)
					throw new OperationCanceledException("Connecting cancelled");
		}

		/// <summary>Start a TCP server that listens for incoming connections from clients</summary>
		private void StartListener(Main parent)
		{
			var connect = new ProgressForm("Listening...", $"Waiting for connections on port: {m_conn.Port}", parent.Icon, ProgressBarStyle.Marquee, (s,a,cb) =>
			{
				cb(new ProgressForm.UserState{ProgressBarVisible = false, Icon = parent.Icon});

				var listener = new TcpListener(IPAddress.Any, m_conn.Port);
				listener.Start();

				try
				{
					// Listen async
					var ar = listener.BeginAcceptTcpClient(r => {}, null);
					for (;!s.CancelPending && !ar.AsyncWaitHandle.WaitOne(500);){}
					if (!s.CancelPending)
					{
						m_client = listener.EndAcceptTcpClient(ar);
						listener.Stop();

						var src = new StreamSource(m_client.GetStream());
						src.BeginRead(m_buf, 0, m_buf.Length, DataRecv, new AsyncData(src, m_buf));
					}
				}
				finally
				{
					listener.Stop();
				}
			});

			using (connect)
				if (connect.ShowDialog(parent) != DialogResult.OK)
					throw new OperationCanceledException("Connecting cancelled");
		}

		/// <summary>Returns true if the TCP client is connected</summary>
		protected override bool IsConnected
		{
			get { return m_client != null && m_client.Connected; }
		}
	}

	/// <summary>Manages a network connection and reading its incoming data</summary>
	public class BufferedUdpNetConn :BufferedStream
	{
		private readonly NetConn m_conn;
		private readonly byte[] m_buf;
		private readonly bool m_specific_host;
		private UdpClient m_udp;

		public BufferedUdpNetConn(NetConn conn)
		:base(conn.OutputFilepath, conn.AppendOutputFile)
		{
			m_conn = conn;
			m_buf = new byte[BufBlockSize];
			m_udp = new UdpClient(m_conn.Port);
			m_specific_host = !string.IsNullOrEmpty(m_conn.Hostname);
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (m_udp != null)
			{
				lock (m_lock)
				{
					Log.Write(ELogLevel.Info, $"Disposing UDP client {m_conn.Hostname}");
					m_udp.Close();
					m_udp = null;
				}
			}
		}

		/// <summary></summary>
		private void BeginRecv()
		{
			EndPoint src = new IPEndPoint(IPAddress.Any, m_conn.Port);
			if (m_specific_host)
				m_udp.Client.BeginReceive(m_buf, 0, m_buf.Length, SocketFlags.None, DataRecv, null);
			else
				m_udp.Client.BeginReceiveFrom(m_buf, 0, m_buf.Length, SocketFlags.None, ref src, DataRecv, null);
		}

		private int EndRecv(IAsyncResult ar)
		{
			EndPoint src = new IPEndPoint(IPAddress.Any, m_conn.Port);
			return m_specific_host
				? m_udp.Client.EndReceive(ar)
				: m_udp.Client.EndReceiveFrom(ar, ref src);
		}

		/// <summary>Start asynchronously reading from the TCP client</summary>
		public void Start()
		{
			if (m_specific_host) m_udp.Connect(m_conn.Hostname, 0);
			BeginRecv();
		}

		/// <summary>Returns true if the TCP client is connected</summary>
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
					Log.Write(ELogLevel.Error, ex, $"[{(type != null ? type.Name : "")}] Data receive exception");
				}
			}
		}
	}
}