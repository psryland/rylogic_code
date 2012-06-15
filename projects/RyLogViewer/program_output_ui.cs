using System;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.common;
using pr.maths;
using pr.util;

namespace RyLogViewer
{
	public partial class LogProgramOutputUI :Form
	{
		private readonly Settings m_settings;
		private readonly ToolTip  m_tt;
		
		public LogProgramOutputUI(Settings settings)
		{
			InitializeComponent();
			m_settings = settings;
			m_tt = new ToolTip();

			// Launch
			m_radio_launch.ToolTip(m_tt, "Launch an application from command line and log its output");
			m_radio_launch.Click += (s,a)=>
				{
					m_settings.LogProgramOutput_Action = ProgramOutputAction.LaunchApplication;
				};
			
			// Command line
			m_edit_launch_cmdline.ToolTip(m_tt, "Command line for the application to launch");
			m_edit_launch_cmdline.Text = m_settings.LogProgramOutput_CmdLine;
			m_edit_launch_cmdline.TextChanged += (s,a)=>
				{
					m_settings.LogProgramOutput_CmdLine = m_edit_launch_cmdline.Text;
				};
			
			// Browse button
			m_btn_browse.Click += (s,a)=>
				{
					var dg = new OpenFileDialog{Filter = Resources.ExecutablesFilter, CheckFileExists = true};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					m_settings.LogProgramOutput_CmdLine = dg.FileName;
				};
			
			// Attach
			m_radio_attach.ToolTip(m_tt, "Attach to a running process and log its output");
			m_radio_attach.Click += (s,a)=>
				{
					m_settings.LogProgramOutput_Action = ProgramOutputAction.AttachToProcess;
				};
			
			// Process combo
			m_combo_processes.ToolTip(m_tt, "Select a process to a attach to");
			
			// Capture Stdout
			m_check_capture_stdout.ToolTip(m_tt, "Check to log standard output (STDOUT)");
			m_check_capture_stdout.Click += (s,a)=>
				{
					m_settings.LogProgramOutput_Streams = (StandardStreams)Bit.SetBits(
						(int)m_settings.LogProgramOutput_Streams,
						(int)StandardStreams.Stdout,
						m_check_capture_stdout.Checked);
				};
			
			// Capture Stderr
			m_check_capture_stderr.ToolTip(m_tt, "Check to log standard error (STDERR)");
			m_check_capture_stderr.Click += (s,a)=>
				{
					m_settings.LogProgramOutput_Streams = (StandardStreams)Bit.SetBits(
						(int)m_settings.LogProgramOutput_Streams,
						(int)StandardStreams.Stderr,
						m_check_capture_stdout.Checked);
				};

			// Hook up and unhook handlers
			EventHandler<SettingsBase.SettingChangedEventArgs> update = (s,a) => UpdateUI();
			Shown += (s,a)=>
				{
					m_settings.SettingChanged += update;
				};
			FormClosed += (s,a)=>
				{
					m_settings.SettingChanged -= update;
				};
			UpdateUI();
		}

		/// <summary>Enable/Disable bits of the UI based on current settings</summary>
		private void UpdateUI()
		{
			var action                     = m_settings.LogProgramOutput_Action;
			m_radio_launch.Checked         = action == ProgramOutputAction.LaunchApplication;
			m_radio_attach.Checked         = action == ProgramOutputAction.AttachToProcess;
			m_edit_launch_cmdline.Enabled  = action == ProgramOutputAction.LaunchApplication;
			m_edit_launch_cmdline.Text     = m_settings.LogProgramOutput_CmdLine;
			m_btn_browse.Enabled           = action == ProgramOutputAction.LaunchApplication;
			m_combo_processes.Enabled      = action == ProgramOutputAction.AttachToProcess;
			m_check_capture_stdout.Checked = (m_settings.LogProgramOutput_Streams & StandardStreams.Stdout) != 0;
			m_check_capture_stderr.Checked = (m_settings.LogProgramOutput_Streams & StandardStreams.Stderr) != 0;
		}
	}
}
