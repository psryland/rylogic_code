using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;
using pr.util;

namespace FarPointer
{
	public partial class PointerForm :Form
	{
		private readonly List<PointerForm> m_list; // The socket providing the position data
		private readonly TcpClient m_src;          // The socket providing the position data
		private readonly NetworkStream m_strm;     // The input stream
		private readonly byte[]    m_buf;          // Buffer for inbound position data
		private readonly string    m_address;      // Address of the remote controller
		private string             m_name;         // The name of the person driving this pointer
		private int                m_read;         // The number of bytes read into 'm_buf'
	
		// A name id for this pointer
		public string ControllerName             { get { return m_name; } }
		public string ControllerNameAndAddress   { get { return m_name + " - " + m_address; } }
		public override string ToString()        { return ControllerNameAndAddress; }

		public delegate void NameUpdatedEvent();
		public event NameUpdatedEvent NameUpdated;

		public delegate void DisconnectedEvent();
		public event DisconnectedEvent Disconnected;

		// Constructor
		public PointerForm(TcpClient src, List<PointerForm> list)
		{
			InitializeComponent();

			m_list = list;
			m_src = src;
			m_strm = m_src.GetStream();
			m_buf = new byte[256];
			m_address = src.Client.RemoteEndPoint.ToString();
			m_name = "unknown";
			m_read = 0;

			m_list.Add(this);
			Show();
		}

		// Start receiving input from the client
		public void AcceptClientData()
		{
			// Setup an async callback for receiving data
			m_strm.BeginRead(m_buf, m_read, m_buf.Length - m_read, r=>{if (Created) BeginInvoke(new AsyncCallback(ReadFarPointerData), r);}, m_strm);
		}

		// Called when the client controlling this pointer disconnects
		private void ClientDisconnected()
		{
			m_list.Remove(this);
			if (Disconnected != null) Disconnected();
			Close();
		}

		// Called when data is available on the socket
		private void ReadFarPointerData(IAsyncResult ar)
		{
			try
			{
				NetworkStream strm = (NetworkStream)ar.AsyncState;
				int read = strm.EndRead(ar);
				if (read == 0) { ClientDisconnected(); return; }
				int used = 0;
				try
				{
					m_read += read;
					using (MemoryStream ms = new MemoryStream(m_buf, 0, m_read))
					using (BinaryReader br = new BinaryReader(ms))
					{
						for (used = 0; used != m_read; used = (int)ms.Position)
						{
							Debug.Assert(used < m_read);
							switch ((EMsg)br.ReadByte())
							{
							default: break;
							case EMsg.None: break;
							case EMsg.Name:
								{
									m_name = br.ReadString();
									m_lbl_name.Text = m_name;
									if (NameUpdated != null) NameUpdated();
								}break;
							case EMsg.MouseDown:
								{
									MouseEventArgs e = ReadMouseData(br);
									Location = new Point(e.X, e.Y);
									Window win = new Window(Win32.WindowFromPoint(Win32.POINT.FromPoint(Location)));
									if      (e.Button == MouseButtons.Left  ) win.SendMessage(Win32.WM_LBUTTONDOWN, Win32.MK_LBUTTON, Win32.MakeLParam(Location));
									else if (e.Button == MouseButtons.Right ) win.SendMessage(Win32.WM_RBUTTONDOWN, Win32.MK_RBUTTON, Win32.MakeLParam(Location));
									else if (e.Button == MouseButtons.Middle) win.SendMessage(Win32.WM_MBUTTONDOWN, Win32.MK_MBUTTON, Win32.MakeLParam(Location));
								}break;
							case EMsg.MouseUp:
								{
									MouseEventArgs e = ReadMouseData(br);
									Location = new Point(e.X, e.Y);
									Window win = new Window(Win32.WindowFromPoint(Win32.POINT.FromPoint(Location)));
									if      (e.Button == MouseButtons.Left  ) win.SendMessage(Win32.WM_LBUTTONUP, Win32.MK_LBUTTON, Win32.MakeLParam(Location));
									else if (e.Button == MouseButtons.Right ) win.SendMessage(Win32.WM_RBUTTONUP, Win32.MK_RBUTTON, Win32.MakeLParam(Location));
									else if (e.Button == MouseButtons.Middle) win.SendMessage(Win32.WM_MBUTTONUP, Win32.MK_MBUTTON, Win32.MakeLParam(Location));
								}break;
							case EMsg.MouseMove:
								{
									MouseEventArgs e = ReadMouseData(br);
									Location = new Point(e.X, e.Y);
									Window win = new Window(Win32.WindowFromPoint(Win32.POINT.FromPoint(Location)));
									win.SendMessage(Win32.WM_MOUSEMOVE, 0, Win32.MakeLParam(Location));
								}break;
							case EMsg.MouseClick:
								{
									//MouseEventArgs e = ReadMouseData(br);
									//Location = new Point(e.X, e.Y);
									//Window win = new Window(Win32.WindowFromPoint(Win32.POINT.FromPoint(Location)));
									//win.SendMessage(Win32.WM_LMOUSECLICK, 0, Win32.MakeLParam(Location));
								}break;
							case EMsg.MouseWheel:
								{
									MouseEventArgs e = ReadMouseData(br);
									Location = new Point(e.X, e.Y);
									Window win = new Window(Win32.WindowFromPoint(Win32.POINT.FromPoint(Location)));
									win.SendMessage(Win32.WM_MOUSEWHEEL, 0, Win32.MakeLParam(Location));
								}break;
							case EMsg.KeyDown:
								{
									//m_wow.SendMessage(Win32.WM_KEYDOWN, key, 0x00080001);
									//m_wow.SendMessage(Win32.WM_KEYUP, key, 0x00080001);
								}break;
							case EMsg.KeyUp:
								break;
							case EMsg.KeyPress:
								break;
							}
						}
					}
				}
				catch (EndOfStreamException) {}

				// Copy any remaining unread data to the start of the buffer
				Buffer.BlockCopy(m_buf, used, m_buf, 0, m_read - used);
				m_read -= used;

				// Wait for more data
				AcceptClientData();
			}
			catch (ObjectDisposedException) {}
		}

		private MouseEventArgs ReadMouseData(BinaryReader br)
		{
			int x = br.ReadInt32();
			int y = br.ReadInt32();
			MouseButtons btn = (MouseButtons)br.ReadInt32();
			int dlta = br.ReadInt32();
			int clks = br.ReadInt32();
			return new MouseEventArgs(btn, clks, x, y, dlta);
		}
	}
}