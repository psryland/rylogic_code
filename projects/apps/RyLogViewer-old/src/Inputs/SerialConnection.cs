using System;
using System.Drawing;
using System.IO.Ports;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Rylogic.Utility;

namespace RyLogViewer
{
	[DataContract]
	public class SerialConn :ICloneable
	{
		// Notes: It is usually recommended to set DTR and RTS true.
		// If the connected device uses these signals, it will not transmit before
		// the signals are set

		[DataMember] public string       CommPort         = string.Empty;
		[DataMember] public int          BaudRate         = 9600;
		[DataMember] public int          DataBits         = 8;
		[DataMember] public Parity       Parity           = Parity.None;
		[DataMember] public StopBits     StopBits         = StopBits.One;
		[DataMember] public Handshake    FlowControl      = Handshake.None;
		[DataMember] public bool         DtrEnable        = true;
		[DataMember] public bool         RtsEnable        = true;
		[DataMember] public string       OutputFilepath   = string.Empty;
		[DataMember] public bool         AppendOutputFile = true;

		public SerialConn() {}
		public SerialConn(SerialConn rhs)
		{
			CommPort         = rhs.CommPort;
			BaudRate         = rhs.BaudRate;
			DataBits         = rhs.DataBits;
			Parity           = rhs.Parity;
			StopBits         = rhs.StopBits;
			FlowControl      = rhs.FlowControl;
			DtrEnable        = rhs.DtrEnable;
			RtsEnable        = rhs.RtsEnable;
			OutputFilepath   = rhs.OutputFilepath;
			AppendOutputFile = rhs.AppendOutputFile;
		}
		public override string ToString()
		{
			return CommPort;
		}
		public object Clone()
		{
			return new SerialConn(this);
		}
	}

	/// <summary>Parts of the Main form related to buffering non-file streams into an output file</summary>
	public partial class Main
	{
		private BufferedSerialConn m_buffered_serialconn;

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
				Src = null;

				// Set options so that data always shows
				PrepareForStreamedData(conn.OutputFilepath);

				// Launch the process with standard output/error redirected to the temporary file
				buffered_serialconn = new BufferedSerialConn(conn);

				// Give some UI feedback if the connection drops
				buffered_serialconn.ConnectionDropped += (s,a)=>
					{
						this.BeginInvoke(() => SetStaticStatusMessage("Connection dropped", Color.Black, Color.LightSalmon));
					};

				// Open the capture file created by buffered_process
				OpenSingleLogFile(buffered_serialconn.Filepath, !buffered_serialconn.TmpFile);
				buffered_serialconn.Start();
				SetStaticStatusMessage("Connected", Color.Black, Color.LightGreen);

				// Pass over the ref
				if (m_buffered_serialconn != null) m_buffered_serialconn.Dispose();
				m_buffered_serialconn = buffered_serialconn;
				buffered_serialconn = null;
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, $"Failed to connect {conn.CommPort}:{conn.BaudRate} -> {conn.OutputFilepath}");
				Misc.ShowMessage(this, $"Failed to connect to {conn.CommPort}:{conn.BaudRate}.", Application.ProductName, MessageBoxIcon.Error, ex);
			}
			finally
			{
				if (buffered_serialconn != null)
					buffered_serialconn.Dispose();
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
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (m_port != null)
			{
				lock (m_lock)
				{
					Log.Write(ELogLevel.Info, $"Disposing serial port connection {m_conn.CommPort}");
					m_port.Dispose();
					m_port = null;
				}
			}
		}

		/// <summary>Start asynchronously reading from the TCP client</summary>
		public void Start()
		{
			m_port.Open();
			m_port.BaseStream.ReadTimeout = Constants.FilePollingRate;
			var src = new StreamSource(m_port.BaseStream);
			src.BeginRead(m_buf, 0, m_buf.Length, DataRecv, new AsyncData(src, m_buf));
		}

		/// <summary>Returns true while the serial port is connected</summary>
		protected override bool IsConnected
		{
			get { return m_port != null && m_port.IsOpen; }
		}
	}
}