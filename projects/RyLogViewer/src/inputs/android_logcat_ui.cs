using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public partial class AndroidLogcatUI :Form
	{
		private readonly ToolTip m_tt;
		private readonly Settings m_settings;
		private readonly BindingList<string> m_device_list;
		private readonly BindingList<AndroidLogcat.FilterSpec> m_filterspecs;

		/// <summary>The command line to execute</summary>
		public readonly LaunchApp Launch;

		public AndroidLogcatUI(Settings settings)
		{
			InitializeComponent();
			m_tt = new ToolTip();
			m_settings = settings;
			m_device_list = new BindingList<string>();
			m_filterspecs = new BindingList<AndroidLogcat.FilterSpec>(m_settings.AndroidLogcat.FilterSpecs);
			Launch = new LaunchApp();

			const string prompt_text = "<Please set the path to adb.exe>";
			m_edit_adb_fullpath.ToolTip(m_tt, "The full path to the android debug bridge executable ('adb.exe')");
			m_edit_adb_fullpath.Text = prompt_text;
			m_edit_adb_fullpath.ForeColor = Color.LightGray;
			m_edit_adb_fullpath.GotFocus += (s,a) =>
				{
					if (m_edit_adb_fullpath.ForeColor != Color.LightGray) return;
					m_edit_adb_fullpath.Text = string.Empty;
					m_edit_adb_fullpath.ForeColor = Color.Black;
				};
			m_edit_adb_fullpath.LostFocus += (s,a) =>
				{
					if (m_edit_adb_fullpath.Text.HasValue()) return;
					m_edit_adb_fullpath.ForeColor = Color.LightGray;
					m_edit_adb_fullpath.Text = prompt_text;
				};
			m_edit_adb_fullpath.TextChanged += (s,a) =>
				{
					if (!((TextBox)s).Modified) return;
					SetAdbPath(m_edit_adb_fullpath.Text);
				};
			
			// Browse for the adb path
			m_btn_browse_adb.ToolTip(m_tt, "Browse the file system for the android debug bridge executable ('adb.exe')");
			m_btn_browse_adb.Click += (s,a) =>
				{
					var dlg = new OpenFileDialog{Filter = "adb.exe"};
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					SetAdbPath(dlg.FileName);
				};
			m_btn_browse_adb.Focus();
			
			// Devices list
			m_listbox_devices.ToolTip(m_tt, "The android devices current connected to adb. Hit 'Refresh' to repopulate this list");
			m_listbox_devices.DataSource = m_device_list;
			m_listbox_devices.SelectedIndexChanged += (s,a) => UpdateAdbCommand();

			// Log buffers (main and system by default)
			m_listbox_log_buffers.ToolTip(m_tt, "The android log buffers to output");
			m_listbox_log_buffers.DataSource = Enum.GetNames(typeof(AndroidLogcat.ELogBuffer)).Select(x => x.ToLowerInvariant()).ToArray();
			foreach (var x in m_settings.AndroidLogcat.LogBuffers) m_listbox_log_buffers.SetSelected((int)x,true);
			m_listbox_log_buffers.SelectedIndexChanged += (s,a) => UpdateAdbCommand();
			
			// Filter specs
			m_grid_filterspec.ToolTip(m_tt, "Configure filters to apply to the logcat output. Tag = '*' applies the filter to all log entries");
			m_grid_filterspec.AutoGenerateColumns = false;
			m_grid_filterspec.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName = "Tag",});
			m_grid_filterspec.Columns.Add(new DataGridViewComboBoxColumn{DataPropertyName = "Priority", DataSource = Enum.GetValues(typeof(AndroidLogcat.EFilterPriority)), Sorted = false});
			m_grid_filterspec.DataSource = m_filterspecs;
			m_filterspecs.ListChanged += (s,a) => UpdateAdbCommand();
			
			// Log format
			m_combo_log_format.ToolTip(m_tt, "Select the format of the log output");
			m_combo_log_format.DataSource = Enum.GetNames(typeof(AndroidLogcat.ELogFormat)).Select(x => x.ToLowerInvariant()).ToArray();
			m_combo_log_format.SelectedIndex = (int)m_settings.AndroidLogcat.LogFormat;
			m_combo_log_format.SelectedIndexChanged += (s,a) => UpdateAdbCommand();
			
			// Capture log output
			m_check_capture_to_log.ToolTip(m_tt, "Enable capturing the logcat output to a file");
			m_check_capture_to_log.Checked = m_settings.AndroidLogcat.CaptureOutputToFile;
			m_check_capture_to_log.CheckedChanged += (s,a) =>
				{
					m_combo_output_file.Enabled      = m_check_capture_to_log.Checked;
					m_btn_browse_output_file.Enabled = m_check_capture_to_log.Checked;
					m_check_append.Enabled           = m_check_capture_to_log.Checked;
				};
			
			// Log out capture file
			m_combo_output_file.ToolTip(m_tt, "The file path to save captured logcat output to");
			m_combo_output_file.Load(m_settings.AndroidLogcat.OutputFilepathHistory);
			m_combo_output_file.Enabled = false;

			m_btn_browse_output_file.ToolTip(m_tt, "Browse the file system for the file to write logcat output to");
			m_btn_browse_output_file.Enabled = false;
			m_btn_browse_output_file.Click += (s,a) =>
				{
					var dlg = new OpenFileDialog();
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					m_combo_output_file.Items.Insert(0, dlg.FileName);
					m_combo_output_file.SelectedIndex = 0;
				};
			
			// Append to existing
			m_check_append.ToolTip(m_tt, "If checked, captured output is appended to the capture file.\r\nIf not, then the capture file is overwritten");
			m_check_append.Checked = Launch.AppendOutputFile;
			m_check_append.Enabled = false;

			// Refresh button
			m_btn_refresh.ToolTip(m_tt, "Repopulate this dialog with data collected using adb.exe");
			m_btn_refresh.Click += (s,a) => PopulateUsingAdb();

			// Save settings on close
			FormClosing += (s,a)=>
				{
					// If launch is selected, add the launch command line to the history
					if (DialogResult == DialogResult.OK)
					{
						string exe, args;
						GenerateAdbCommand(out exe, out args);

						// Update the launch options
						Launch.Executable       = exe;
						Launch.Arguments        = args;
						Launch.WorkingDirectory = Path.GetDirectoryName(Launch.Executable) ?? string.Empty;
						Launch.OutputFilepath   = (string)m_combo_output_file.SelectedItem ?? string.Empty;
						Launch.ShowWindow       = false;
						Launch.AppendOutputFile = m_check_append.Checked;
						Launch.Streams          = StandardStreams.Stdout|StandardStreams.Stderr;

						// Save settings - Only save here, so that cancel causes settings to be unchanged
						var logcat_settings = new AndroidLogcat();
						logcat_settings.AdbFullPath         = m_edit_adb_fullpath.Text;
						logcat_settings.CaptureOutputToFile = m_check_capture_to_log.Checked;
						logcat_settings.AppendOutputFile    = m_check_append.Checked;
						logcat_settings.LogBuffers          = m_listbox_log_buffers.SelectedIndices.Cast<AndroidLogcat.ELogBuffer>().ToArray();
						logcat_settings.FilterSpecs         = m_filterspecs.ToArray();
						logcat_settings.LogFormat           = (AndroidLogcat.ELogFormat)m_combo_log_format.SelectedIndex;
						m_settings.AndroidLogcat = logcat_settings;

						var output_file_history = new List<string>(m_settings.AndroidLogcat.OutputFilepathHistory);
						Misc.AddToHistoryList(output_file_history, Launch.OutputFilepath, true, Constants.MaxOutputFileHistoryLength);
						m_settings.AndroidLogcat.OutputFilepathHistory = output_file_history.ToArray();
						m_settings.Save();
					}
				};

			AutoDetectAdbPath();
		}

		/// <summary>Search for the full path of adb.exe</summary>
		private void AutoDetectAdbPath()
		{
			// If the full path is saved in the settings, use that
			if (m_settings.AndroidLogcat.AdbFullPath.HasValue())
			{
				SetAdbPath(m_settings.AndroidLogcat.AdbFullPath);
				return;
			}
			
			// Look in likely spots
			foreach (var path in new[]
				{
					Environment.GetEnvironmentVariable("ANDROID_HOME"),
					Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
					Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
				})
			{
				if (string.IsNullOrEmpty(path)) continue;
				var dir  = new DirectoryInfo(path);
				var file = dir.EnumerateFiles("adb.exe", SearchOption.AllDirectories).FirstOrDefault();
				if (file == null) continue;
				SetAdbPath(file.FullName);
				break;
			}
		}

		/// <summary>Sets and saves the adb file path</summary>
		private void SetAdbPath(string path)
		{
			// Reject invalid paths
			if (path == null || !File.Exists(path))
				return;
			
			// If hint text is shown, clear it first
			if (m_edit_adb_fullpath.ForeColor == Color.LightGray)
			{
				m_edit_adb_fullpath.ForeColor = Color.Black;
				m_edit_adb_fullpath.Text = string.Empty;
			}
			
			// Only set when different
			if (m_edit_adb_fullpath.Text != path)
			{
				m_edit_adb_fullpath.Text = path;
				PopulateUsingAdb();
			}
		}

		/// <summary>Run the adb app returning the output</summary>
		private string Adb(string args)
		{
			// Determine the available devices
			var start = new ProcessStartInfo
				{
					FileName               = m_edit_adb_fullpath.Text,
					Arguments              = args,
					RedirectStandardOutput = true,
					RedirectStandardError  = true,
					UseShellExecute        =  false,
					CreateNoWindow         = true,
					WindowStyle            = ProcessWindowStyle.Hidden,
				};
			var proc = Process.Start(start);
			if (!proc.Start()) throw new Exception("Failed to start adb.exe");
			return proc.StandardOutput.ReadToEnd();
		}

		/// <summary>Populate the fields of the control using adb.exe</summary>
		private void PopulateUsingAdb()
		{
			try
			{
				// Only if adb.exe is found
				if (!File.Exists(m_edit_adb_fullpath.Text))
					return;
				
				// Set the version output
				m_text_adb_status.Text = Adb("version");
				m_text_adb_status.Visible = true;
				
				// Setup the device list
				var devices = Adb("devices").Split(new[]{Environment.NewLine,"\n","\r"}, StringSplitOptions.RemoveEmptyEntries);
				m_device_list.Clear();
				foreach (var device_row in devices.Skip(1))
				{
					var device = device_row.Split(new[]{" ","\t"}, StringSplitOptions.RemoveEmptyEntries);
					if (device.Length == 0) continue;
					m_device_list.Add(device[0].Trim());
				}
				
				UpdateAdbCommand();
			}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Error while running adb");
			}
		}

		/// <summary>Update the the logcat command that will be used</summary>
		private void UpdateAdbCommand()
		{
			string exe, args;
			GenerateAdbCommand(out exe, out args);
			m_edit_adb_command.Text =  "\"" + exe +  "\"" + args;
		}

		/// <summary>Generate the adb command line from the current ui fields</summary>
		private void GenerateAdbCommand(out string exe, out string args)
		{
			// adb.exe
			exe = m_edit_adb_fullpath.Text;

			var sb = new StringBuilder();

			// -s device-id
			if (m_listbox_devices.SelectedItem != null)
			{
				sb.Append(" -s ").Append(m_listbox_devices.SelectedItem);
			}
			
			// logcat
			sb.Append(" logcat");
			
			// log buffers
			foreach (var i in m_listbox_log_buffers.SelectedItems)
				sb.Append(" -b ").Append(i);
			
			// log format
			sb.Append(" -v ").Append(m_combo_log_format.SelectedItem.ToString().ToLowerInvariant());
			
			// Filter specs
			foreach (var i in m_filterspecs)
				sb.Append(' ').Append(i.Tag).Append(':').Append(i.Priority.ToString()[0]);
			
			args = sb.ToString();
		}
	}
}
