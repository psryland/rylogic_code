using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;
using Util = Rylogic.Utility.Util;

namespace RyLogViewer
{
	public partial class ProgramOutputUI :Form
	{
		private readonly Settings m_settings;
		private readonly List<string> m_outp_history;
		private readonly ToolTip  m_tt;

		/// <summary>The command line to execute</summary>
		public LaunchApp Launch { get; set; }

		public ProgramOutputUI(Settings settings)
		{
			InitializeComponent();
			m_settings     = settings;
			m_outp_history = new List<string>(m_settings.OutputFilepathHistory);
			Launch         = m_settings.LogProgramOutputHistory.Length != 0 ? new LaunchApp(m_settings.LogProgramOutputHistory[0]) : new LaunchApp();
			m_tt           = new ToolTip();

			// Command line
			m_combo_launch_cmdline.ToolTip(m_tt, "Command line for the application to launch");
			m_combo_launch_cmdline.Load(m_settings.LogProgramOutputHistory);
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
					if (!((TextBox)s).Modified) return;
					Launch.Arguments = m_edit_arguments.Text;
					UpdateUI();
				};

			// Working Directory
			m_edit_working_dir.ToolTip(m_tt, "The application working directory");
			m_edit_working_dir.Text = Launch.WorkingDirectory;
			m_edit_working_dir.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					Launch.WorkingDirectory = m_edit_working_dir.Text;
					UpdateUI();
				};

			// Output file
			m_combo_output_filepath.ToolTip(m_tt, "The file to save captured program output in.\r\nLeave blank to not save captured output");
			foreach (var i in m_outp_history) m_combo_output_filepath.Items.Add(i);
			m_combo_output_filepath.Text = Launch.OutputFilepath;
			m_combo_output_filepath.TextChanged += (s,a)=>
				{
					Launch.OutputFilepath = m_combo_output_filepath.Text;
					UpdateUI();
				};

			// Browse executable
			m_btn_browse_exec.Click += (s,a)=>
				{
					var dg = new OpenFileDialog{Filter = Constants.ExecutablesFilter, CheckFileExists = true};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					Launch.Executable = dg.FileName;
					Launch.WorkingDirectory = Path.GetDirectoryName(dg.FileName);
					UpdateUI();
				};

			// Browse output file
			m_btn_browse_output.Click += (s,a)=>
				{
					var dg = new SaveFileDialog{Filter = Constants.LogFileFilter, CheckPathExists = true, OverwritePrompt = false};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					Launch.OutputFilepath = dg.FileName;
					UpdateUI();
				};

			// Todo, show window doesn't really work, remove it
			Launch.ShowWindow = false;
			//// Show window
			//m_check_show_window.ToolTip(m_tt, "If checked, the window for the application is shown.\r\n If not, then it is hidden");
			//m_check_show_window.Checked = Launch.ShowWindow;
			//m_check_show_window.Click += (s,a)=>
			//	{
			//		Launch.ShowWindow = m_check_show_window.Checked;
			//		UpdateUI();
			//	};

			// Append to existing
			m_check_append.ToolTip(m_tt, "If checked, captured output is appended to the capture file.\r\nIf not, then the capture file is overwritten");
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
			FormClosing += (s,a)=>
				{
					// If launch is selected, add the launch command line to the history
					if (DialogResult == DialogResult.OK && Launch.Executable.Length != 0)
					{
						m_settings.LogProgramOutputHistory = Util.AddToHistoryList(m_settings.LogProgramOutputHistory, Launch, true, Constants.MaxProgramHistoryLength);
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
			m_combo_launch_cmdline.Text    = Launch.Executable;
			m_edit_arguments.Text          = Launch.Arguments;
			m_edit_working_dir.Text        = Launch.WorkingDirectory;
			m_combo_output_filepath.Text   = Launch.OutputFilepath;
			m_check_show_window.Checked    = Launch.ShowWindow;
			m_check_capture_stdout.Checked = (Launch.Streams & StandardStreams.Stdout) != 0;
			m_check_capture_stderr.Checked = (Launch.Streams & StandardStreams.Stderr) != 0;
			m_check_append.Checked         = Launch.AppendOutputFile;
			m_check_append.Enabled         = Launch.OutputFilepath.Length != 0;
		}
	}
}
