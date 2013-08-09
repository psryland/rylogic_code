using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public partial class SerialConnectionUI :Form
	{
		private readonly Settings m_settings;
		private readonly List<SerialConn> m_history;
		private readonly List<string> m_outp_history;
		private readonly ToolTip m_tt;

		/// <summary>The serial port connection properties selected</summary>
		public SerialConn Conn { get; set; }

		public SerialConnectionUI(Settings settings)
		{
			InitializeComponent();
			m_settings = settings;
			m_history  = new List<SerialConn>(m_settings.SerialConnectionHistory);
			m_outp_history = new List<string>(m_settings.OutputFilepathHistory);
			Conn       = m_history.Count != 0 ? new SerialConn(m_history[0]) : new SerialConn();
			m_tt       = new ToolTip();
			
			// Comm Port
			string[] portnames = SerialPort.GetPortNames();
			m_combo_commport.ToolTip(m_tt, "The serial communications port to connect to");
			foreach (var i in portnames) m_combo_commport.Items.Add(i);
			m_combo_commport.TextChanged += (s,a)=>
				{
					Conn.CommPort = m_combo_commport.Text;
					UpdateUI();
				};
			m_combo_commport.SelectedIndexChanged += (s,a)=>
				{
					Conn.CommPort = m_combo_commport.Text;
					UpdateUI();
				};

			// Baud rate
			int[] baudrates = new[]{110,300,600,1200,2400,4800,9600,14400,19200,38400,57600,115200,230400,460800,921600,1843200};
			m_combo_baudrate.ToolTip(m_tt, "The baud rate for the connection");
			foreach (var i in baudrates) m_combo_baudrate.Items.Add(i);
			m_combo_baudrate.KeyPress += (s,a) => a.Handled = !char.IsDigit(a.KeyChar);
			m_combo_baudrate.TextChanged += (s,a)=>
				{
					int.TryParse(m_combo_baudrate.Text, out Conn.BaudRate);
					UpdateUI();
				};
			m_combo_baudrate.SelectedIndexChanged += (s,a)=>
				{
					Conn.BaudRate = (int)m_combo_baudrate.SelectedItem;
					UpdateUI();
				};

			// Data bits
			int[] databits = new[]{7,8};
			m_combo_databits.ToolTip(m_tt, "The number of data bits");
			foreach (var i in databits) m_combo_databits.Items.Add(i);
			m_combo_databits.KeyPress += (s,a) => a.Handled = !char.IsDigit(a.KeyChar);
			m_combo_databits.TextChanged += (s,a)=>
				{
					int.TryParse(m_combo_databits.Text, out Conn.DataBits);
					UpdateUI();
				};
			m_combo_databits.SelectedIndexChanged += (s,a)=>
				{
					Conn.DataBits = (int)m_combo_databits.SelectedItem;
					UpdateUI();
				};

			// Parity
			m_combo_parity.ToolTip(m_tt,"The parity checking protocol to use");
			m_combo_parity.DataSource = Enum.GetValues(typeof(Parity));
			m_combo_parity.SelectedItem = Conn.Parity;
			m_combo_parity.SelectedIndexChanged += (s,a)=>
				{
					Conn.Parity = (Parity)m_combo_parity.SelectedItem;
					UpdateUI();
				};

			// Stop bits
			m_combo_stopbits.ToolTip(m_tt, "The number of stop bits per byte");
			m_combo_stopbits.DataSource = Enum.GetValues(typeof(StopBits));
			m_combo_stopbits.SelectedItem = Conn.StopBits;
			m_combo_stopbits.SelectedIndexChanged += (s,a)=>
				{
					Conn.StopBits = (StopBits)m_combo_stopbits.SelectedItem;
					UpdateUI();
				};

			// Flow control
			m_combo_flow_control.ToolTip(m_tt, "The handshaking protocol for communication");
			m_combo_flow_control.DataSource = Enum.GetValues(typeof(Handshake));
			m_combo_flow_control.SelectedItem = Conn.FlowControl;
			m_combo_flow_control.SelectedIndexChanged += (s,a)=>
				{
					Conn.FlowControl = (Handshake)m_combo_flow_control.SelectedItem;
					UpdateUI();
				};

			// DTR enable
			m_check_dtr_enable.ToolTip(m_tt, "Set the Data Terminal Ready signal during communication");
			m_check_dtr_enable.Checked = Conn.DtrEnable;
			m_check_dtr_enable.Click += (s,a)=>
				{
					Conn.DtrEnable = m_check_dtr_enable.Checked;
					UpdateUI();
				};

			// RTS enable
			m_check_rts_enable.ToolTip(m_tt,"Set the Request to Send signal during communication");
			m_check_rts_enable.Checked = Conn.RtsEnable;
			m_check_rts_enable.Click += (s,a)=>
				{
					Conn.RtsEnable = m_check_rts_enable.Checked;
					UpdateUI();
				};

			// Output file
			m_combo_output_filepath.ToolTip(m_tt, "The file to save captured program output in.\r\nLeave blank to not save captured output");
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
			
			// Save settings on close
			FormClosing += (s,a)=>
				{
					// If launch is selected, add the launch command line to the history
					if (DialogResult == DialogResult.OK && Conn.CommPort.Length != 0)
					{
						Misc.AddToHistoryList(m_history, Conn, true, Constants.MaxSerialConnHistoryLength);
						m_settings.SerialConnectionHistory = m_history.ToArray();
						
						Misc.AddToHistoryList(m_outp_history, Conn.OutputFilepath, true, Constants.MaxOutputFileHistoryLength);
						m_settings.OutputFilepathHistory = m_outp_history.ToArray();
					}
				};

			Disposed += (s,a) =>
				{
					m_tt.Dispose();
				};

			UpdateUI();
		}

		/// <summary>Enable/Disable bits of the UI based on current settings</summary>
		private void UpdateUI()
		{
			m_combo_commport.Text             = Conn.CommPort;
			m_combo_baudrate.Text             = Conn.BaudRate.ToString();
			m_combo_databits.Text             = Conn.DataBits.ToString();
			m_combo_parity.SelectedItem       = Conn.Parity;
			m_combo_stopbits.SelectedItem     = Conn.StopBits;
			m_combo_flow_control.SelectedItem = Conn.FlowControl;
			m_check_dtr_enable.Checked        = Conn.DtrEnable;
			m_check_rts_enable.Checked        = Conn.RtsEnable;
			m_combo_output_filepath.Text      = Conn.OutputFilepath;
			m_check_append.Checked            = Conn.AppendOutputFile;
			m_check_append.Enabled            = Conn.OutputFilepath.Length != 0;
		}
	}
}
