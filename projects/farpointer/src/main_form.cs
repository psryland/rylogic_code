using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using pr.stream;
using pr.util;

namespace FarPointer
{
	public partial class MainForm :Form
	{
		private const int MaxHostHistory = 10;
		private const string Connect = "Connect";
		private const string Disconnect = "Disconnect";
		private readonly Settings          m_settings = Settings.Load();
		private readonly List<PointerForm> m_pointer = new List<PointerForm>();
		private readonly TcpListener       m_inbound;    // Listens for connections from pointer clients
		private TcpClient                  m_server;     // Our connection to a remote server
		private bool m_hide_not_close;

		// Constructor
		public MainForm()
		{
			InitializeComponent();

			m_inbound = new TcpListener(IPAddress.Loopback, Host.DefaultPort);
			m_inbound.Start(10);
			m_inbound.BeginAcceptTcpClient(r=>{BeginInvoke(new AsyncCallback(AcceptConnectionCallback), r);}, m_inbound);
			m_server = null;
			m_hide_not_close = true;
			m_btn_connect.Text = Connect;
			m_btn_connect.Click       += ConnectPointer;
			m_traymenu_options.Click  += ShowOptions;
			m_traymenu_exit.Click     += delegate { Shutdown(); };
			m_tray_notify.DoubleClick += delegate { Show(); WindowState = FormWindowState.Normal; };

			ResizeBegin += delegate { if (FormWindowState.Minimized == WindowState) Hide(); };
			FormClosing += delegate(object sender, FormClosingEventArgs e)
			{
				e.Cancel = m_hide_not_close;
				WindowState = FormWindowState.Minimized;
				Hide();
			};
		}

		// Close the connection and shutdown
		private void Shutdown()
		{
			UnInstallHooks();
			lock (m_pointer) foreach(var p in m_pointer) {p.Close();}
			m_inbound.Stop();
			if (m_server != null)
			{
				m_server.GetStream().Close();
				m_server.Close();
			}
			m_hide_not_close = false;
			Close(); 
		}

		// Install hook functions to catch local mouse/keyboard events
		private void InstallHooks()
		{
			UnInstallHooks();

			MouseKeyboardHook.MouseDown  += OnMouseDown;
			MouseKeyboardHook.MouseUp    += OnMouseUp;
			MouseKeyboardHook.MouseMove  += OnMouseMove;
			MouseKeyboardHook.MouseClick += OnMouseClick;
			MouseKeyboardHook.MouseWheel += OnMouseWheel;
			MouseKeyboardHook.KeyDown    += OnKeyDown;
			MouseKeyboardHook.KeyUp      += OnKeyUp;
			MouseKeyboardHook.KeyPress   += OnKeyPress;
		}

		// Remove the installed hooks
		private void UnInstallHooks()
		{
			MouseKeyboardHook.MouseDown  -= OnMouseDown;
			MouseKeyboardHook.MouseUp    -= OnMouseUp;
			MouseKeyboardHook.MouseMove  -= OnMouseMove;
			MouseKeyboardHook.MouseClick -= OnMouseClick;
			MouseKeyboardHook.MouseWheel -= OnMouseWheel;
			MouseKeyboardHook.KeyDown    -= OnKeyDown;
			MouseKeyboardHook.KeyUp      -= OnKeyUp;
			MouseKeyboardHook.KeyPress   -= OnKeyPress;
		}

		// Mouse notifications
		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (m_server == null) return;
			using (BinaryWriter bw = new BinaryWriter(new UncloseableStream(m_server.GetStream())))
			{
				bw.Write((byte)EMsg.MouseDown);
				WriteMouseData(bw, e);
			}
		}
		private void OnMouseUp(object sender, MouseEventArgs e)
		{
			if (m_server == null) return;
			using (BinaryWriter bw = new BinaryWriter(new UncloseableStream(m_server.GetStream())))
			{
				bw.Write((byte)EMsg.MouseUp);
				WriteMouseData(bw, e);
			}
		}
		private void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (m_server == null) return;
			using (BinaryWriter bw = new BinaryWriter(new UncloseableStream(m_server.GetStream())))
			{
				bw.Write((byte)EMsg.MouseMove);
				WriteMouseData(bw, e);
			}
		}
		private void OnMouseClick(object sender, MouseEventArgs e)
		{
			if (m_server == null) return;
			using (BinaryWriter bw = new BinaryWriter(new UncloseableStream(m_server.GetStream())))
			{
				bw.Write((byte)EMsg.MouseClick);
				WriteMouseData(bw, e);
			}
		}
		private void OnMouseWheel(object sender, MouseEventArgs e)
		{
			if (m_server == null) return;
			using (BinaryWriter bw = new BinaryWriter(new UncloseableStream(m_server.GetStream())))
			{
				bw.Write((byte)EMsg.MouseWheel);
				WriteMouseData(bw, e);
			}
		}
		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (m_server == null) return;
			using (BinaryWriter bw = new BinaryWriter(new UncloseableStream(m_server.GetStream())))
			{
				bw.Write((byte)EMsg.KeyDown);
				bw.Write((int)e.KeyCode);
				bw.Write((int)e.Modifiers);
			}
		}
		private void OnKeyUp(object sender, KeyEventArgs e)
		{
			if (m_server == null) return;
			using (BinaryWriter bw = new BinaryWriter(new UncloseableStream(m_server.GetStream())))
			{
				bw.Write((byte)EMsg.KeyUp);
				bw.Write((int)e.KeyCode);
				bw.Write((int)e.Modifiers);
			}
		}
		private void OnKeyPress(object sender, KeyPressEventArgs e)
		{
			if (m_server == null) return;
			using (BinaryWriter bw = new BinaryWriter(new UncloseableStream(m_server.GetStream())))
			{
				bw.Write((byte)EMsg.KeyPress);
				bw.Write((int)e.KeyChar);
			}
		}
		private void WriteMouseData(BinaryWriter bw, MouseEventArgs e)
		{
			bw.Write(e.X);
			bw.Write(e.Y);
			bw.Write((int)e.Button);
			bw.Write(e.Delta);
			bw.Write(e.Clicks);
		}


		// Connect to an instance of this app and establish a pointer connection
		private void ConnectPointer(object sender, EventArgs e)
		{
			if (m_btn_connect.Text == Connect)
			{
				ConnectForm dlg = new ConnectForm(m_settings);
				if (dlg.ShowDialog(this) != DialogResult.OK) return;
				SaveRecentHost(dlg.ClientName, dlg.Hostname, dlg.Port);

				try
				{
					m_server = new TcpClient(dlg.Hostname, dlg.Port);
					using (BinaryWriter bw = new BinaryWriter(new UncloseableStream(m_server.GetStream())))
					{
						bw.Write((byte)EMsg.Name);
						bw.Write(dlg.ClientName);
					}

					m_btn_connect.Text = Disconnect;
					InstallHooks();
				}
				catch (SocketException ex)
				{
					MessageBox.Show(this, "Connection could not be established\nReason: " + ex.Message, "Connection Failure", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			else // assume Disconnect
			{
				m_server.GetStream().Close();
				m_server.Close();
				m_server = null;
				m_btn_connect.Text = Connect;
			}
		}

		// Callback for when incoming connections are made
		private void AcceptConnectionCallback(IAsyncResult ar)
		{
			try
			{
				// Get the socket that handles the client request.
				TcpListener listener = (TcpListener)ar.AsyncState;
				TcpClient client = listener.EndAcceptTcpClient(ar);
				
				// Create a pointer using the socket
				PointerForm p = new PointerForm(client, m_pointer);
				p.NameUpdated += UpdateUI;
				p.Disconnected += UpdateUI;
				p.AcceptClientData();

				// Begin waiting for another connection
				listener.BeginAcceptTcpClient(r=>{if (Created) BeginInvoke(new AsyncCallback(AcceptConnectionCallback), r);}, listener);
			}
			catch (SocketException) {}
			// This exception happens when 'Stop()' is called
			// Apparently this is the "correct" way to handle is situation...
			catch (ObjectDisposedException) {}
		}

		// Show the options dialog
		private void ShowOptions(object sender, EventArgs e)
		{
			//OptionsForm dlg = new OptionsForm();
			//dlg.m_edit_listen_port.Text = m_inbound.ToString();
			//if (dlg.ShowDialog(this) != DialogResult.OK) return;
			//int.TryParse(dlg.m_edit_listen_port.Text, out m_port);
		}

		// Add a host to the host list
		private void SaveRecentHost(string name, string hostname, int port)
		{
			m_settings.ClientName = name;

			// Remove any duplicate hosts
			foreach (var h in m_settings.Hosts)
			{
				if (string.Compare(h.m_hostname, hostname, true) != 0 || h.m_port != port) continue;
				m_settings.Hosts.Remove(h);
				break;
			}

			// Add the new host
			m_settings.Hosts.Add(new Host(hostname, port));
			
			// Limit the length of the list to 10
			if (m_settings.Hosts.Count > MaxHostHistory)
				m_settings.Hosts.RemoveRange(MaxHostHistory, m_settings.Hosts.Count - MaxHostHistory);

			m_settings.Save();
		}

		// Populate the list of connected pointers
		private void UpdateUI()
		{
			m_list_connected.Items.Clear();
			foreach (PointerForm p in m_pointer)
				m_list_connected.Items.Add(p.ControllerNameAndAddress);
		}
	}
}

