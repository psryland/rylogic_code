using System.Collections.Generic;
using System.Net.Sockets;
using System.Windows.Forms;
using RyLogViewer.Properties;
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
			
			// Hostname
			m_combo_hostname.ToolTip(m_tt, "The hostname to connect to");
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
			m_spinner_port.ToolTip(m_tt, "The remote port to connect to");
			m_spinner_port.Value = Conn.Port;
			m_spinner_port.ValueChanged += (s,a)=>
				{
					Conn.Port = (ushort)m_spinner_port.Value;
					UpdateUI();
				};
			
			// Protocol type
			m_combo_protocol_type.ToolTip(m_tt, "The connection type");
			var allowed = new[]{ProtocolType.IPv4, ProtocolType.IPv6, ProtocolType.Udp};
			foreach (var i in allowed) m_combo_protocol_type.Items.Add(i);
			m_combo_protocol_type.SelectedIndex = 0;
			m_combo_protocol_type.SelectedIndexChanged += (s,a)=>
				{
					Conn.ProtocolType = (ProtocolType)m_combo_protocol_type.SelectedItem;
					UpdateUI();
				};
			
			// Use proxy
			m_check_use_proxy.ToolTip(m_tt, "Check to specify a proxy server");
			m_check_use_proxy.Checked = Conn.UseProxy;
			m_check_use_proxy.Click += (s,a)=>
				{
					Conn.UseProxy = m_check_use_proxy.Checked;
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
			UpdateUI();
		}

		/// <summary>Enable/Disable bits of the UI based on current settings</summary>
		private void UpdateUI()
		{
			m_combo_hostname.Text               = Conn.Hostname;
			m_spinner_port.Value                = Conn.Port;
			m_combo_protocol_type.SelectedItem  = Conn.ProtocolType;
			m_check_use_proxy.Checked           = Conn.UseProxy;
			m_edit_proxy_hostname.Text          = Conn.ProxyHostname;
			m_spinner_proxy_port.Value          = Conn.ProxyPort;
			m_combo_output_filepath.Text        = Conn.OutputFilepath;
			m_check_append.Checked              = Conn.AppendOutputFile;
			m_check_append.Enabled              = Conn.OutputFilepath.Length != 0;
		}
	}
}
