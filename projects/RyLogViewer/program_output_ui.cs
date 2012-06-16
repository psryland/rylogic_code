using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.maths;
using pr.util;

namespace RyLogViewer
{
	public partial class LogProgramOutputUI :Form
	{
		private readonly Settings m_settings;
		private readonly List<LaunchApp> m_history;
		private readonly ToolTip  m_tt;
		
		/// <summary>The command line to execute</summary>
		public LaunchApp Launch;
		
		public LogProgramOutputUI(Settings settings)
		{
			InitializeComponent();
			m_settings = settings;
			m_history  = new List<LaunchApp>(m_settings.LogProgramOutputHistory);
			Launch     = m_history.Count != 0 ? new LaunchApp(m_history[0]) : new LaunchApp();
			m_tt       = new ToolTip();
			
			// Command line
			m_combo_launch_cmdline.ToolTip(m_tt, "Command line for the application to launch");
			m_combo_launch_cmdline.DisplayMember = "Executable";
			foreach (var s in m_history) m_combo_launch_cmdline.Items.Add(s);
			if (m_history.Count != 0) m_combo_launch_cmdline.SelectedIndex = 0;
			m_combo_launch_cmdline.TextChanged += (s,a)=>
				{
					Launch.Executable = m_combo_launch_cmdline.Text;
					UpdateUI();
				};
			m_combo_launch_cmdline.SelectedIndexChanged += (s,a)=>
				{
					Launch = new LaunchApp((LaunchApp)m_combo_launch_cmdline.SelectedItem);
					UpdateUI();
				};
			
			// Arguments
			m_edit_arguments.ToolTip(m_tt, "Command line arguments for the executable");
			m_edit_arguments.Text = Launch.Arguments;
			m_edit_arguments.TextChanged += (s,a)=>
				{
					Launch.Arguments = m_edit_arguments.Text;
					UpdateUI();
				};

			// Working Directory
			m_edit_working_dir.ToolTip(m_tt, "The application working directory");
			m_edit_working_dir.Text = Launch.WorkingDirectory;
			m_edit_working_dir.TextChanged += (s,a)=>
				{
					Launch.WorkingDirectory = m_edit_working_dir.Text;
					UpdateUI();
				};

			// Output file
			m_edit_output_file.ToolTip(m_tt, "The file to capture program output in.\r\nLeave blank to not save captured output");
			m_edit_output_file.Text = Launch.OutputFilepath;
			m_edit_output_file.TextChanged += (s,a)=>
				{
					Launch.OutputFilepath = m_edit_output_file.Text;
					UpdateUI();
				};

			// Browse executable
			m_btn_browse_exec.Click += (s,a)=>
				{
					var dg = new OpenFileDialog{Filter = Resources.ExecutablesFilter, CheckFileExists = true};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					Launch.Executable = dg.FileName;
					Launch.WorkingDirectory = Path.GetDirectoryName(dg.FileName);
					UpdateUI();
				};
			
			// Browse output file
			m_btn_browse_output.Click += (s,a)=>
				{
					var dg = new SaveFileDialog{Filter = Resources.LogFileFilter, CheckPathExists = true, OverwritePrompt = false};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					Launch.OutputFilepath = dg.FileName;
					UpdateUI();
				};
			
			// Show window
			m_check_show_window.ToolTip(m_tt, "If checked, the window for the application is shown.\r\n If not, then it is hidden");
			m_check_show_window.Checked = Launch.ShowWindow;
			m_check_show_window.Click += (s,a)=>
				{
					Launch.ShowWindow = m_check_show_window.Checked;
					UpdateUI();
				};

			// Append to existing
			m_check_append.ToolTip(m_tt, "If checked, captured output is appended to the output file.\r\nIf not, then the output file is overwritten");
			m_check_append.Checked = Launch.AppendOutputFile;
			m_check_append.Click += (s,a)=>
				{
					Launch.AppendOutputFile = m_check_append.Checked;
					UpdateUI();
				};

			// Capture Stdout
			m_check_capture_stdout.ToolTip(m_tt, "Check to log standard output (STDOUT)");
			m_check_capture_stdout.Click += (s,a)=>
				{
					Launch.Streams = (StandardStreams)Bit.SetBits(
						(int)Launch.Streams,
						(int)StandardStreams.Stdout,
						m_check_capture_stdout.Checked);
				};
			
			// Capture Stderr
			m_check_capture_stderr.ToolTip(m_tt, "Check to log standard error (STDERR)");
			m_check_capture_stderr.Click += (s,a)=>
				{
					Launch.Streams = (StandardStreams)Bit.SetBits(
						(int)Launch.Streams,
						(int)StandardStreams.Stderr,
						m_check_capture_stdout.Checked);
				};

			// Save settings on close
			FormClosed += (s,a)=>
				{
					// If launch is selected, add the launch command line to the history
					if (DialogResult == DialogResult.OK && Launch.Executable.Length != 0)
					{
						m_history.RemoveAll(i => string.Compare(i.ToString(), Launch.ToString(), StringComparison.OrdinalIgnoreCase) == 0);
						m_history.Insert(0, Launch);
						if (m_history.Count > 10) m_history.RemoveRange(10, m_history.Count - 10);
						m_settings.LogProgramOutputHistory = m_history.ToArray();
					}
				};
			UpdateUI();
		}

		/// <summary>Enable/Disable bits of the UI based on current settings</summary>
		private void UpdateUI()
		{
			m_combo_launch_cmdline.Text    = Launch.Executable;
			m_edit_arguments.Text          = Launch.Arguments;
			m_edit_working_dir.Text        = Launch.WorkingDirectory;
			m_edit_output_file.Text        = Launch.OutputFilepath;
			m_check_show_window.Checked    = Launch.ShowWindow;
			m_check_append.Checked         = Launch.AppendOutputFile;
			m_check_append.Enabled         = Launch.OutputFilepath.Length != 0;
			m_check_capture_stdout.Checked = (Launch.Streams & StandardStreams.Stdout) != 0;
			m_check_capture_stderr.Checked = (Launch.Streams & StandardStreams.Stderr) != 0;
		}
	}
}
