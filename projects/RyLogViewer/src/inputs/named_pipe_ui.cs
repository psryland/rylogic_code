using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.extn;

namespace RyLogViewer
{
	public partial class NamedPipeUI :Form
	{
		private readonly Settings m_settings;
		private readonly List<PipeConn> m_history;
		private readonly List<string> m_outp_history;
		private readonly ToolTip m_tt;
		private string m_pipeaddr;
			
		/// <summary>The serial port connection properties selected</summary>
		public PipeConn Conn { get; set; }
		
		public NamedPipeUI(Settings settings)
		{
			InitializeComponent();
			m_settings = settings;
			m_history  = new List<PipeConn>(m_settings.PipeConnectionHistory);
			m_outp_history = new List<string>(m_settings.OutputFilepathHistory);
			Conn       = m_history.Count != 0 ? new PipeConn(m_history[0]) : new PipeConn();
			m_tt       = new ToolTip();
			m_pipeaddr = Conn.PipeAddr;
			
			// Pipe name
			m_combo_pipe_name.ToolTip(m_tt, "The pipe name in the form \\\\<server>\\pipe\\<pipename>\r\nPipes on the local machine use '.' for <server>");
			foreach (var i in m_history) m_combo_pipe_name.Items.Add(i);
			if (m_history.Count != 0) m_combo_pipe_name.SelectedIndex = 0;
			m_combo_pipe_name.TextChanged += (s,a)=>
				{
					m_pipeaddr = m_combo_pipe_name.Text;
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
					if (DialogResult == DialogResult.OK && m_pipeaddr.Length != 0)
					{
						// Validate the pipe address
						try { Conn.PipeAddr = m_pipeaddr; }
						catch (ArgumentException)
						{
							MessageBox.Show(this, "The pipe name field is invalid.\r\n\r\nIt must have the form: \\\\<server>\\pipe\\<pipename>\r\ne.g. \\\\.\\pipe\\MyLocalPipe", "Pipe Name Invalid", MessageBoxButtons.OK, MessageBoxIcon.Error);
							a.Cancel = true;
							return;
						}

						Misc.AddToHistoryList(m_history, Conn, true, Constants.MaxSerialConnHistoryLength);
						m_settings.PipeConnectionHistory = m_history.ToArray();
						
						Misc.AddToHistoryList(m_outp_history, Conn.OutputFilepath, true, Constants.MaxOutputFileHistoryLength);
						m_settings.OutputFilepathHistory = m_outp_history.ToArray();
					}
				};
			UpdateUI();
		}

		/// <summary>Enable/Disable bits of the UI based on current settings</summary>
		private void UpdateUI()
		{
			m_combo_output_filepath.Text = Conn.OutputFilepath;
			m_check_append.Checked       = Conn.AppendOutputFile;
			m_check_append.Enabled       = Conn.OutputFilepath.Length != 0;
		}
	}
}
