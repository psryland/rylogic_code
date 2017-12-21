using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gui;
using pr.util;
using DataGridViewComboBoxColumn = pr.gui.DataGridViewComboBoxColumn;

namespace RyLogViewer
{
	public partial class AndroidLogcatUI :Form
	{
		private const string UsbDevice = "usb";

		private readonly ToolTip m_tt;
		private readonly AndroidLogcat m_settings;
		private readonly BindingList<string> m_device_list;
		private readonly BindingSource m_bs_device_list;
		private readonly BindingList<AndroidLogcat.FilterSpec> m_filterspecs;

		/// <summary>The command line to execute</summary>
		public readonly LaunchApp Launch;

		public AndroidLogcatUI(Settings settings)
		{
			InitializeComponent();
			m_tt = new ToolTip();
			m_settings = new AndroidLogcat(settings.AndroidLogcat);
			m_device_list = new BindingList<string>();
			m_bs_device_list = new BindingSource{DataSource = m_device_list};
			m_filterspecs = new BindingList<AndroidLogcat.FilterSpec>(m_settings.FilterSpecs.ToList());

			var output_filepaths = settings.OutputFilepathHistory;
			Launch = new LaunchApp();
			PreferredDevice = m_settings.SelectedDevice;

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

			// The version info printed by adb
			UpdateAdbVersionInfo(false);

			// Browse for the adb path
			m_btn_browse_adb.ToolTip(m_tt, "Browse the file system for the android debug bridge executable ('adb.exe')");
			m_btn_browse_adb.Click += (s,a) =>
				{
					var dlg = new OpenFileDialog{Filter = "Programs (*.exe)|*.exe", FileName =  "adb.exe"};
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					SetAdbPath(dlg.FileName);
				};
			m_btn_browse_adb.Focus();

			// Devices list
			m_listbox_devices.ToolTip(m_tt, "The android devices current connected to adb. Hit 'Refresh' to repopulate this list");
			m_listbox_devices.DataSource = m_bs_device_list;
			m_bs_device_list.CurrentChanged += (s,a) =>
				{
					UpdateAdbCommand();
					SelectPreferredOutputFile();
					m_btn_ok.Enabled = m_bs_device_list.CurrentOrDefault() != null;
				};

			// Connect button
			m_btn_connect.ToolTip(m_tt, "Add an android device not currently shown in the 'Connected Devices' list");
			m_btn_connect.Click += (s,a) => ConnectDevice();
			m_btn_connect.Enabled = false; // Until a valid adb path is set

			// Log buffers (main and system by default)
			m_listbox_log_buffers.ToolTip(m_tt, "The android log buffers to output");
			m_listbox_log_buffers.DataSource = Enum.GetNames(typeof(AndroidLogcat.ELogBuffer)).Select(x => x.ToLowerInvariant()).ToArray();
			foreach (var x in m_settings.LogBuffers) m_listbox_log_buffers.SetSelected((int)x,true);
			m_listbox_log_buffers.SelectedIndexChanged += (s,a) => UpdateAdbCommand();

			// Filter specs
			m_grid_filterspec.ToolTip(m_tt, "Configure filters to apply to the logcat output. Tag = '*' applies the filter to all log entries");
			m_grid_filterspec.AutoGenerateColumns = false;
			m_grid_filterspec.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = nameof(AndroidLogcat.FilterSpec.Tag)
			});
			m_grid_filterspec.Columns.Add(new DataGridViewComboBoxColumn
			{
				DataPropertyName = nameof(AndroidLogcat.FilterSpec.Priority),
				DataSource = Enum.GetValues(typeof(AndroidLogcat.EFilterPriority)),
				Sorted = false
			});
			m_grid_filterspec.DataSource = m_filterspecs;
			m_filterspecs.ListChanged += (s,a) => UpdateAdbCommand();

			// Log format
			m_combo_log_format.ToolTip(m_tt, "Select the format of the log output");
			m_combo_log_format.DataSource = Enum.GetNames(typeof(AndroidLogcat.ELogFormat)).Select(x => x.ToLowerInvariant()).ToArray();
			m_combo_log_format.SelectedIndex = (int)m_settings.LogFormat;
			m_combo_log_format.SelectedIndexChanged += (s,a) => UpdateAdbCommand();

			// Capture log output
			m_check_capture_to_log.ToolTip(m_tt, "Enable capturing the logcat output to a file");
			m_check_capture_to_log.Checked = m_settings.CaptureOutputToFile;
			m_check_capture_to_log.CheckedChanged += (s,a) =>
				{
					m_combo_output_file.Enabled      = m_check_capture_to_log.Checked;
					m_btn_browse_output_file.Enabled = m_check_capture_to_log.Checked;
					m_check_append.Enabled           = m_check_capture_to_log.Checked;
				};

			// Log out capture file
			m_combo_output_file.ToolTip(m_tt, "The file path to save captured logcat output to");
			m_combo_output_file.Load(output_filepaths);
			m_combo_output_file.Enabled = m_check_capture_to_log.Checked;

			// Browse for capture output file
			m_btn_browse_output_file.ToolTip(m_tt, "Browse the file system for the file to write logcat output to");
			m_btn_browse_output_file.Enabled = m_check_capture_to_log.Checked;
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
			m_check_append.Enabled = m_check_capture_to_log.Checked;

			// Refresh button
			m_btn_refresh.ToolTip(m_tt, "Repopulate this dialog with data collected using adb.exe");
			m_btn_refresh.Click += (s,a) => PopulateUsingAdb();

			// Reset adb button
			m_btn_resetadb.ToolTip(m_tt, "Restart the adb server process.\r\nCan solve problems when connecting to devices");
			m_btn_resetadb.Enabled = false; // unless there is an adb path
			m_btn_resetadb.Click += (s,a) =>
				{
					var adbs = Process.GetProcessesByName("adb");
					foreach (var adb in adbs) adb.Kill();
					Adb("start-server");
					PopulateUsingAdb();
				};

			// Ok button
			m_btn_ok.Enabled = m_listbox_devices.SelectedItem != null;

			Load += (s,a) =>
				{
					AutoDetectAdbPath();
				};

			// Save settings on close
			FormClosing += (s,a)=>
				{
					// If launch is selected, add the launch command line to the history
					if (DialogResult == DialogResult.OK)
					{
						// Cancelled until we're sure we have a valid launch command
						DialogResult = DialogResult.Cancel;

						// Save settings - Only save here, so that cancel causes settings to be unchanged
						m_settings.AdbFullPath         = m_edit_adb_fullpath.Text;
						m_settings.CaptureOutputToFile = m_check_capture_to_log.Checked;
						m_settings.AppendOutputFile    = m_check_append.Checked;
						m_settings.LogBuffers          = m_listbox_log_buffers.SelectedIndices.Cast<AndroidLogcat.ELogBuffer>().ToArray();
						m_settings.FilterSpecs         = m_filterspecs.ToArray();
						m_settings.LogFormat           = (AndroidLogcat.ELogFormat)m_combo_log_format.SelectedIndex;
						m_settings.SelectedDevice      = m_listbox_devices.SelectedItem.ToString();
						settings.AndroidLogcat = m_settings;
						settings.RowDelimiter = "<CR><CR><LF>";

						// Use cancelled if no device was selected or no valid adb path found
						if (ValidAdbPath && m_listbox_devices.SelectedItem != null)
						{
							string exe, args;
							GenerateAdbCommand(out exe, out args);

							// Update the launch options
							Launch.Executable       = exe;
							Launch.Arguments        = args;
							Launch.WorkingDirectory = Path.GetDirectoryName(exe) ?? string.Empty;
							Launch.OutputFilepath   = m_settings.CaptureOutputToFile ? m_combo_output_file.Text : string.Empty;
							Launch.ShowWindow       = false;
							Launch.AppendOutputFile = m_check_append.Checked;
							Launch.Streams          = StandardStreams.Stdout|StandardStreams.Stderr;
							DialogResult = DialogResult.OK;
						}
					}

					// If the adb full path has changed, update the settings even if DialogResult == cancel
					else if (ValidAdbPath && settings.AndroidLogcat.AdbFullPath != m_edit_adb_fullpath.Text)
					{
						var save = new AndroidLogcat(settings.AndroidLogcat);
						save.AdbFullPath = m_edit_adb_fullpath.Text;
						settings.AndroidLogcat = save;
					}
				};

			Disposed += (s,a) =>
				{
					m_tt.Dispose();
				};
		}

		/// <summary>The device to connect to if available</summary>
		private string PreferredDevice { get; set; }

		/// <summary>Clear or set the adb version info</summary>
		private void UpdateAdbVersionInfo(bool read)
		{
			if (read && ValidAdbPath)
			{
				m_text_adb_status.Text = Adb("version").Trim('\r','\n');
				m_text_adb_status.ForeColor = Color.ForestGreen;
				m_text_adb_status.Visible = true;
			}
			else
			{
				m_text_adb_status.Text = "No connection to adb.exe";
				m_text_adb_status.ForeColor = Color.Firebrick;
				m_text_adb_status.Visible = true;
			}
		}

		/// <summary>True if the adb path appears valid</summary>
		private bool ValidAdbPath
		{
			get { return Path_.FileExists(m_edit_adb_fullpath.Text); }
		}

		/// <summary>Search for the full path of adb.exe</summary>
		private void AutoDetectAdbPath()
		{
			// If the full path is saved in the settings, use that (if it's valid)
			if (Path_.FileExists(m_settings.AdbFullPath))
			{
				SetAdbPath(m_settings.AdbFullPath);
				return;
			}

			// Quick check the most likely spot
			var likely_path = Path.Combine(Environment.GetEnvironmentVariable("ANDROID_HOME") ?? string.Empty, @"platform-tools\adb.exe");
			if (Path_.FileExists(likely_path))
			{
				SetAdbPath(likely_path);
				return;
			}

			// Not found? longer search...
			const string msg =
				"Searching for the Android Debugging Bridge application...\r\n"+
				"\r\n"+
				"Trying Path: {0}...\r\n"+
				"\r\n"+
				"Click cancel to locate it manually.";

			var adb_path = string.Empty;
			var search_paths = new[]
			{
				Environment.GetEnvironmentVariable("ANDROID_HOME"),
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Android\android-sdk\platform-tools",
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Android\android-sdk\platform-tools",
				@"D:\Program Files (x86)\Android\android-sdk\platform-tools",
				@"D:\Program Files\Android\android-sdk\platform-tools",
				@"E:\Program Files (x86)\Android\android-sdk\platform-tools",
				@"E:\Program Files\Android\android-sdk\platform-tools",
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
				@"C:\"
			};

			var res = DialogResult.Cancel;
			try
			{
				// Search for 'adb.exe'
				var find_adb = new ProgressForm("Locating 'adb.exe'...", msg.Fmt(string.Empty), Icon, ProgressBarStyle.Marquee, (s,a,cb) =>
				{
					foreach (var path in search_paths)
					{
						if (s.CancelPending || adb_path.HasValue())
							break;

						if (path == null || !Directory.Exists(path))
							continue;

						cb(new ProgressForm.UserState{Description = msg.Fmt(path), ForceLayout = false});
						Func<string,bool> progress = dir =>
						{
							cb(ProgressForm.UserState.Empty);
							return !s.CancelPending;
						};

						foreach (var fi in Path_.EnumFileSystem(path, SearchOption.AllDirectories, regex_filter:@"adb\.exe", progress:progress))
						{
							// Found one!
							adb_path = fi.FullPath;
							return;
						}
					}
				})
				{
					StartPosition = FormStartPosition.CenterParent,
					ClientSize = new Size(640, 280),
				};
				using (find_adb)
					res = find_adb.ShowDialog(this);
			}
			catch (OperationCanceledException) {}
			catch (Exception ex) { Misc.ShowMessage(this, "An error occurred while searching for 'adb.exe'", "Locating 'adb.exe' failed", MessageBoxIcon.Error, ex); }
			if (res != DialogResult.OK)
				return;

			SetAdbPath(adb_path);
		}

		/// <summary>Sets and saves the adb file path</summary>
		private void SetAdbPath(string path)
		{
			// Reject invalid paths
			if (!Path_.FileExists(path))
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
			var result = string.Empty;
			var desc = "Executing:\r\n\r\n{0} {1}".Fmt(m_edit_adb_fullpath.Text, args);

			try
			{
				var run = new ProgressForm("Adb Command Executing", desc, Icon, ProgressBarStyle.Marquee, (form,x,cb) =>
				{
					// Determine the available devices
					using (var proc = new Process())
					{
						// The only way to read both stdout and stderr and avoid
						// deadlocks is to do it this way...
						proc.StartInfo = new ProcessStartInfo
						{
							FileName               = m_edit_adb_fullpath.Text,
							Arguments              = args,
							RedirectStandardOutput = true,
							RedirectStandardError  = true,
							UseShellExecute        = false,
							CreateNoWindow         = true,
							WindowStyle            = ProcessWindowStyle.Hidden,
						};
						var sb_out = new StringBuilder();
						var sb_err = new StringBuilder();
						proc.OutputDataReceived += (s,a) => sb_out.AppendLine(a.Data);
						proc.ErrorDataReceived  += (s,a) => sb_err.AppendLine(a.Data);
						if (!proc.Start())
							throw new Exception("Failed to start adb.exe");

						proc.BeginOutputReadLine();
						proc.BeginErrorReadLine();

						while (!proc.WaitForExit(200))
						{
							if (!form.CancelPending) continue;
							proc.Kill();
							proc.WaitForExit();
							break;
						}

						// If the command completed, read the output
						if (proc.ExitCode < 0)
							throw new Exception("Error code: {0}\r\nResult:\r\n{1}".Fmt(proc.ExitCode, sb_err.ToString()));

						proc.Close();

						Log.Debug(this, sb_out.ToString());
						Log.Debug(this, sb_err.ToString());

						result = sb_out.ToString();
					}
				});
				using (run)
					return run.ShowDialog(this, 500) == DialogResult.OK ? result : string.Empty;
			}
			catch (Exception ex)
			{
				MsgBox.Show(this, "Adb returned an error.\r\n{0}".Fmt(ex.InnerException.MessageFull()), "Adb Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return string.Empty;
			}
		}

		/// <summary>Populate the fields of the control using adb.exe</summary>
		private void PopulateUsingAdb()
		{
			try
			{
				m_btn_connect.Enabled = false;

				// Only if adb.exe is found
				if (!ValidAdbPath)
					return;

				UpdateAdbVersionInfo(true);

				// Setup the device list
				var output = Adb("devices");
				var ofs = output.IndexOf("List", StringComparison.Ordinal);
				var list = ofs != -1 ? output.Substring(ofs) : string.Empty;
				var devices = list.Split(new[]{Environment.NewLine,"\n","\r"}, StringSplitOptions.RemoveEmptyEntries);
				m_device_list.Clear();
				foreach (var device_row in devices.Skip(1))
				{
					var device = device_row.Split(new[]{" ","\t"}, StringSplitOptions.RemoveEmptyEntries);
					if (device.Length == 0) continue;
					m_device_list.Add(device[0].Trim());
				}

				// Select the preferred device
				if (PreferredDevice.HasValue())
				{
					int idx = PreferredDevice == UsbDevice
						? m_device_list.IndexOf(x => !x.Contains(".")) // Select the one that doesn't look like an ip address
						: m_device_list.IndexOf(x => x.Contains(PreferredDevice));
					if (idx != -1)
						m_bs_device_list.CurrencyManager.Position = idx;
				}

				// Enable the connect button
				m_btn_connect.Enabled = true;
				m_btn_resetadb.Enabled = true;

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
				sb.Append(" -s ").Append(m_listbox_devices.SelectedItem);

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

		/// <summary>If capture output is checked, see if there's an output file in the combo box that might be preferred.</summary>
		private void SelectPreferredOutputFile()
		{
			// If capture output is checked, see if there's an output file in the combo box that might be preferred
			if (!m_check_capture_to_log.Checked)
				return;

			// Use the IP address as a regex expression, allowing any character to delim the numbers
			var pattern = m_bs_device_list.Current.ToString().Substring(null, ":");
			var idx = m_combo_output_file.Items.Cast<string>().IndexOf(x => Regex.IsMatch(x, pattern));
			if (idx != -1)
				m_combo_output_file.SelectedIndex = idx;
		}

		/// <summary>Show the connect to device dialog</summary>
		private void ConnectDevice()
		{
			var dlg = new AndroidConnectDeviceUI(m_settings);
			if (dlg.ShowDialog(this) != DialogResult.OK) return;

			var result = (m_settings.ConnectionType == AndroidLogcat.EConnectionType.Tcpip)
				? Adb("connect " + m_settings.IPAddressHistory[0])
				: Adb("usb");

			if (!string.IsNullOrWhiteSpace(result))
				MsgBox.Show(this, result, "Adb Connect Result", MessageBoxButtons.OK, MessageBoxIcon.Information);

			// Record the preferred device
			PreferredDevice = (m_settings.ConnectionType == AndroidLogcat.EConnectionType.Tcpip)
				? m_settings.IPAddressHistory[0] : UsbDevice;

			this.BeginInvoke(PopulateUsingAdb);
		}
	}
}
