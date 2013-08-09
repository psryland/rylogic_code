using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using pr.extn;

namespace RyLogViewer
{
	public class AndroidConnectDeviceUI :Form
	{
		private readonly ToolTip m_tt;
		private Label    m_lbl_connection_type;
		private Label    m_lbl_ipaddress;
		private Label    m_lbl_port;
		private ComboBox m_combo_connection_type;
		private ComboBox m_combo_ip_address;
		private TextBox  m_edit_port;
		private Button   m_btn_ok;
		private Button   m_btn_cancel;

		public AndroidConnectDeviceUI(AndroidLogcat settings)
		{
			InitializeComponent();

			m_tt = new ToolTip();

			// Connection type combo
			m_combo_connection_type.ToolTip(m_tt, "Select the method used to connect to the android device");
			m_combo_connection_type.DataSource = new[]{"USB", "TCP/IP"};
			m_combo_connection_type.SelectedIndex = (int)settings.ConnectionType;
			m_combo_connection_type.SelectedIndexChanged += (s,a) =>
				{
					settings.ConnectionType = (AndroidLogcat.EConnectionType)m_combo_connection_type.SelectedIndex;
					m_combo_ip_address.Enabled = settings.ConnectionType == AndroidLogcat.EConnectionType.Tcpip;
					m_edit_port.Enabled = settings.ConnectionType == AndroidLogcat.EConnectionType.Tcpip;
				};

			// IP Address
			m_combo_ip_address.ToolTip(m_tt, "The IP address of the android device");
			m_combo_ip_address.Load(settings.IPAddressHistory);
			m_combo_ip_address.Enabled = settings.ConnectionType == AndroidLogcat.EConnectionType.Tcpip;

			// Port
			m_edit_port.ToolTip(m_tt, "The port on which to connect to the android device, typically (5555)");
			m_edit_port.Text = settings.ConnectionPort.ToString(CultureInfo.InvariantCulture);
			m_edit_port.Enabled = settings.ConnectionType == AndroidLogcat.EConnectionType.Tcpip;
			m_edit_port.TextChanged += (s,a) =>
				{
					int port;
					if (int.TryParse(m_edit_port.Text, out port))
					{
						m_edit_port.BackColor = Misc.FieldBkColor(false);
						settings.ConnectionPort = port;
					}
					else
					{
						m_edit_port.BackColor = SystemColors.ControlLight;
					}
				};

			FormClosing += (s,a) =>
				{
					if (DialogResult == DialogResult.OK)
					{
						Misc.AddToHistoryList(ref settings.IPAddressHistory, m_combo_ip_address.Text, false, Constants.MaxHistoryDefault);
					}
				};

			Disposed += (s,a) =>
				{
					m_tt.Dispose();
				};
		}

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private IContainer components = null;

		/// <summary>Clean up any resources being used.</summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AndroidConnectDeviceUI));
			this.m_combo_connection_type = new ComboBox();
			this.m_lbl_connection_type = new System.Windows.Forms.Label();
			this.m_edit_port = new System.Windows.Forms.TextBox();
			this.m_lbl_ipaddress = new System.Windows.Forms.Label();
			this.m_lbl_port = new System.Windows.Forms.Label();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_combo_ip_address = new ComboBox();
			this.SuspendLayout();
			// 
			// m_combo_connection_type
			// 
			this.m_combo_connection_type.FormattingEnabled = true;
			this.m_combo_connection_type.Location = new System.Drawing.Point(106, 12);
			this.m_combo_connection_type.Name = "m_combo_connection_type";
			this.m_combo_connection_type.Size = new System.Drawing.Size(134, 21);
			this.m_combo_connection_type.TabIndex = 0;
			// 
			// m_lbl_connection_type
			// 
			this.m_lbl_connection_type.AutoSize = true;
			this.m_lbl_connection_type.Location = new System.Drawing.Point(12, 15);
			this.m_lbl_connection_type.Name = "m_lbl_connection_type";
			this.m_lbl_connection_type.Size = new System.Drawing.Size(91, 13);
			this.m_lbl_connection_type.TabIndex = 1;
			this.m_lbl_connection_type.Text = "Connection Type:";
			// 
			// m_edit_port
			// 
			this.m_edit_port.Location = new System.Drawing.Point(106, 65);
			this.m_edit_port.Name = "m_edit_port";
			this.m_edit_port.Size = new System.Drawing.Size(60, 20);
			this.m_edit_port.TabIndex = 3;
			// 
			// m_lbl_ipaddress
			// 
			this.m_lbl_ipaddress.AutoSize = true;
			this.m_lbl_ipaddress.Location = new System.Drawing.Point(39, 42);
			this.m_lbl_ipaddress.Name = "m_lbl_ipaddress";
			this.m_lbl_ipaddress.Size = new System.Drawing.Size(61, 13);
			this.m_lbl_ipaddress.TabIndex = 4;
			this.m_lbl_ipaddress.Text = "IP Address:";
			// 
			// m_lbl_port
			// 
			this.m_lbl_port.AutoSize = true;
			this.m_lbl_port.Location = new System.Drawing.Point(71, 68);
			this.m_lbl_port.Name = "m_lbl_port";
			this.m_lbl_port.Size = new System.Drawing.Size(29, 13);
			this.m_lbl_port.TabIndex = 5;
			this.m_lbl_port.Text = "Port:";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(84, 94);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 6;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(165, 94);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 7;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_combo_ip_address
			// 
			this.m_combo_ip_address.FormattingEnabled = true;
			this.m_combo_ip_address.Location = new System.Drawing.Point(106, 39);
			this.m_combo_ip_address.Name = "m_combo_ip_address";
			this.m_combo_ip_address.Size = new System.Drawing.Size(134, 21);
			this.m_combo_ip_address.TabIndex = 8;
			// 
			// AndroidConnectDeviceUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(252, 129);
			this.Controls.Add(this.m_combo_ip_address);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_lbl_port);
			this.Controls.Add(this.m_lbl_ipaddress);
			this.Controls.Add(this.m_edit_port);
			this.Controls.Add(this.m_lbl_connection_type);
			this.Controls.Add(this.m_combo_connection_type);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "AndroidConnectDeviceUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Connect Device";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
