using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.inet;
using pr.util;

namespace RyLogViewer
{
	public partial class NetworkConnectionUI :Form
	{
		private readonly Settings m_settings;
		private readonly List<NetConn> m_history;
		private readonly List<string> m_outp_history;
		private readonly ToolTip m_tt;

		/// <summary>The network connection properties selected</summary>
		public NetConn Conn;

		public NetworkConnectionUI(Settings settings)
		{
			InitializeComponent();
			m_settings = settings;
			m_history  = new List<NetConn>(m_settings.NetworkConnectionHistory);
			m_outp_history = new List<string>(m_settings.OutputFilepathHistory);
			Conn       = m_history.Count != 0 ? new NetConn(m_history[0]) : new NetConn();
			m_tt       = new ToolTip();
			
			// Protocol type
			m_combo_protocol_type.ToolTip(m_tt, "The connection type");
			var allowed = new[]{ProtocolType.Tcp, ProtocolType.Udp};
			foreach (var i in allowed) m_combo_protocol_type.Items.Add(i);
			m_combo_protocol_type.SelectedIndex = 0;
			m_combo_protocol_type.SelectedIndexChanged += (s,a)=>
				{
					Conn.ProtocolType = (ProtocolType)m_combo_protocol_type.SelectedItem;
					UpdateUI();
				};
			
			// Hostname
			foreach (var i in m_history) m_combo_hostname.Items.Add(i);
			if (m_history.Count != 0) m_combo_hostname.SelectedIndex = 0;
			m_combo_hostname.TextChanged += (s,a)=>
				{
					Conn.Hostname = m_combo_hostname.Text;
					UpdateUI();
				};
			m_combo_hostname.SelectedIndexChanged += (s,a)=>
				{
					Conn = new NetConn((NetConn)m_combo_hostname.SelectedItem);
					UpdateUI();
				};
			
			// Port
			m_spinner_port.Value = Conn.Port;
			m_spinner_port.ValueChanged += (s,a)=>
				{
					Conn.Port = (ushort)m_spinner_port.Value;
					UpdateUI();
				};
			
			// Use proxy
			m_combo_proxy_type.ToolTip(m_tt, "Select if using a proxy server");
			m_combo_proxy_type.DataSource = Enum.GetValues(typeof(ProxyType));
			m_combo_proxy_type.SelectedIndex = (int)Conn.ProxyType;
			m_combo_proxy_type.SelectedIndexChanged +=(s,a)=>
				{
					Conn.ProxyType = (ProxyType)m_combo_proxy_type.SelectedItem;
					UpdateUI();
				};
			
			// Proxy host
			m_edit_proxy_hostname.ToolTip(m_tt, "The hostname of the proxy server");
			m_edit_proxy_hostname.Text = Conn.ProxyHostname;
			m_edit_proxy_hostname.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					Conn.ProxyHostname = m_edit_proxy_hostname.Text;
					UpdateUI();
				};

			// Proxy port
			m_spinner_proxy_port.ToolTip(m_tt, "The remote port of the proxy server");
			m_spinner_proxy_port.Value = Conn.ProxyPort;
			m_spinner_proxy_port.ValueChanged += (s,a)=>
				{
					Conn.ProxyPort = (ushort)m_spinner_proxy_port.Value;
					UpdateUI();
				};

			// Output file
			m_combo_output_filepath.ToolTip(m_tt, "The file to capture network data in.\r\nLeave blank to not save captured data");
			foreach (var i in m_outp_history) m_combo_output_filepath.Items.Add(i);
			m_combo_output_filepath.Text = Conn.OutputFilepath;
			m_combo_output_filepath.TextChanged += (s,a)=>
				{
					Conn.OutputFilepath = m_combo_output_filepath.Text;
					UpdateUI();
				};

			// Browse output file
			m_btn_browse_output.Click += (s,a)=>
				{
					var dg = new SaveFileDialog{Filter = Resources.LogFileFilter, CheckPathExists = true, OverwritePrompt = false};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					Conn.OutputFilepath = dg.FileName;
					UpdateUI();
				};
			
			// Append to existing
			m_check_append.ToolTip(m_tt, "If checked, captured data is appended to the capture file.\r\nIf not, then the capture file is overwritten");
			m_check_append.Checked = Conn.AppendOutputFile;
			m_check_append.Click += (s,a)=>
				{
					Conn.AppendOutputFile = m_check_append.Checked;
					UpdateUI();
				};
			
			Shown += (s,a)=>
				{
					UpdateUI();
				};
			
			// Save settings on close
			FormClosing += (s,a)=>
				{
					// If launch is selected, add the launch command line to the history
					if (DialogResult == DialogResult.OK && Conn.Hostname.Length != 0)
					{
						Misc.AddToHistoryList(m_history, Conn, true, Constants.MaxNetConnHistoryLength);
						m_settings.NetworkConnectionHistory = m_history.ToArray();

						Misc.AddToHistoryList(m_outp_history, Conn.OutputFilepath, true, Constants.MaxOutputFileHistoryLength);
						m_settings.OutputFilepathHistory = m_outp_history.ToArray();
					}
				};
		}

		/// <summary>Enable/Disable bits of the UI based on current settings</summary>
		private void UpdateUI()
		{
			string tt;

			m_combo_protocol_type.SelectedItem  = Conn.ProtocolType;
			if (Conn.ProtocolType == ProtocolType.Tcp)
			{
				tt = "The remote host to connect to and receive data from";
				m_lbl_hostname.ToolTip(m_tt, tt);
				m_combo_hostname.ToolTip(m_tt, tt);
				
				tt = "The remote port to connect to and receive data from";
				m_lbl_port.ToolTip(m_tt, tt);
				m_lbl_port.Text = "Remote Port:";
				m_spinner_port.ToolTip(m_tt, tt);
				
				// Allow proxy server description
				m_combo_proxy_type.Enabled    = true;
				m_lbl_proxy_hostname.Enabled  = Conn.ProxyType != ProxyType.None;
				m_lbl_proxy_port.Enabled      = Conn.ProxyType != ProxyType.None;
				m_lbl_proxy_username.Enabled  = Conn.ProxyType != ProxyType.None && Conn.ProxyType != ProxyType.Http;
				m_lbl_proxy_password.Enabled  = Conn.ProxyType != ProxyType.None && Conn.ProxyType != ProxyType.Http;
				m_edit_proxy_hostname.Enabled = Conn.ProxyType != ProxyType.None;
				m_spinner_proxy_port.Enabled  = Conn.ProxyType != ProxyType.None;
				m_edit_proxy_username.Enabled = Conn.ProxyType != ProxyType.None && Conn.ProxyType != ProxyType.Http;
				m_edit_proxy_password.Enabled = Conn.ProxyType != ProxyType.None && Conn.ProxyType != ProxyType.Http;
				switch (Conn.ProxyType)
				{
				case ProxyType.Http:    m_spinner_proxy_port.Value = HttpProxyClient.DefaultPort; break;
				case ProxyType.Socks4:  m_spinner_proxy_port.Value = Socks4ProxyClient.DefaultPort; break;
				case ProxyType.Socks4A: m_spinner_proxy_port.Value = Socks4ProxyClient.DefaultPort; break;
				case ProxyType.Socks5:  m_spinner_proxy_port.Value = Socks5ProxyClient.DefaultPort; break;
				}
			}
			else if (Conn.ProtocolType == ProtocolType.Udp)
			{
				tt = "The host to expect data from. Leave blank for multiple udp data sources";
				m_lbl_hostname.ToolTip(m_tt, tt);
				m_combo_hostname.ToolTip(m_tt, tt);
				
				tt = "The local port that remote clients will connect to";
				m_lbl_port.ToolTip(m_tt, tt);
				m_lbl_port.Text = "Local Port:";
				m_spinner_port.ToolTip(m_tt, tt);
				
				// Proxy doesn't make sense for UDP connections
				m_combo_proxy_type.Enabled    = false;
				m_lbl_proxy_hostname.Enabled  = false;
				m_lbl_proxy_port.Enabled      = false;
				m_lbl_proxy_username.Enabled  = false;
				m_lbl_proxy_password.Enabled  = false;
				m_edit_proxy_hostname.Enabled = false;
				m_spinner_proxy_port.Enabled  = false;
				m_edit_proxy_username.Enabled = false;
				m_edit_proxy_password.Enabled = false;
			}
			
			m_combo_hostname.Text               = Conn.Hostname;
			m_spinner_port.Value                = Conn.Port;
			m_combo_proxy_type.SelectedIndex    = (int)Conn.ProxyType;
			m_edit_proxy_hostname.Text          = Conn.ProxyHostname;
			m_spinner_proxy_port.Value          = Conn.ProxyPort;
			m_combo_output_filepath.Text        = Conn.OutputFilepath;
			m_check_append.Checked              = Conn.AppendOutputFile;
			m_check_append.Enabled              = Conn.OutputFilepath.Length != 0;
		}
	}
}
