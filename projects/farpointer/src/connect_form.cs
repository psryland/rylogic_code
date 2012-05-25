using System.Collections.Generic;
using System.Windows.Forms;

namespace FarPointer
{
	public partial class ConnectForm :Form
	{
		public ConnectForm(Settings settings)
		{
			InitializeComponent();

			m_edit_name.Text = settings.ClientName;
			
			foreach (Host h in settings.Hosts)
			{
				m_combo_hostname.Items.Add(h.m_hostname);
			}
			
			m_combo_hostname.SelectedIndexChanged += delegate
			{
				if (m_combo_hostname.SelectedIndex >= 0 && m_combo_hostname.SelectedIndex < settings.Hosts.Count)
					m_edit_port.Text = settings.Hosts[m_combo_hostname.SelectedIndex].m_port.ToString();
				else
					m_edit_port.Text = Host.DefaultPort.ToString();
			};

			m_edit_port.KeyPress += delegate(object sender, KeyPressEventArgs e)
			{
				int num; // Only accept numbers
				e.Handled = !int.TryParse(e.KeyChar.ToString(), out num);
			};

			if (settings.Hosts.Count == 0)
			{
				m_combo_hostname.Items.Add("localhost");
			}

			m_combo_hostname.SelectedIndex = 0;
		}
		public string ClientName { get { return m_edit_name.Text; } }
		public string Hostname   { get { return m_combo_hostname.Text; } }
		public int    Port       { get { int port; return int.TryParse(m_edit_port.Text, out port) ? port : 0; } }
	}
}
