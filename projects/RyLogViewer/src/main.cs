using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.inet;
using pr.maths;
using pr.util;
using RyLogViewer.Properties;
using Timer = System.Windows.Forms.Timer;

namespace RyLogViewer
{
	public sealed partial class Main :Form
	{
		private readonly StartupOptions m_startup_options;    // The options provided at startup
		private readonly Settings m_settings;                 // App settings
		private readonly RecentFiles m_recent;                // Recent files
		private readonly FileWatch m_watch;                   // A helper for watching files
		private readonly Timer m_watch_timer;                 // A timer for polling the file watcher
		private readonly EventBatcher m_batch_set_col_size;   // A call batcher for setting the column widths
		private readonly List<Highlight> m_highlights;        // A list of the active highlights only
		private readonly List<Filter> m_filters;              // A list of the active filters only
		private readonly List<Transform> m_transforms;        // A list of the active transforms only
		private readonly List<ClkAction> m_clkactions;        // A list of the active click actions only
		private readonly FindUI m_find_ui;                    // The find dialog
		private readonly BookmarksUI m_bookmarks_ui;          // The bookmarks dialog
		private readonly NotifyIcon m_notify_icon;            // A system tray icon
		private readonly ToolTip m_tt;                        // Tooltips
		private readonly ToolTip m_balloon;                   // A hint balloon tooltip
		private readonly Form[] m_tab_cycle;                  // The forms that Ctrl+Tab cycles through
		private readonly RefCount m_suspend_grid_events;      // A ref count of nested calls that tell event handlers to ignore grid events
		private List<Range> m_line_index;                     // Byte offsets (from file begin) to the byte range of a line
		private Encoding m_encoding;                          // The file encoding
		private IFileSource m_file;                           // A file stream source
		private byte[] m_row_delim;                           // The row delimiter converted from a string to a byte[] using the current encoding
		private byte[] m_col_delim;                           // The column delimiter, cached to prevent m_settings access in CellNeeded
		private int m_row_height;                             // The row height, cached to prevent settings lookups in CellNeeded
		private long m_filepos;                               // The byte offset (from file begin) to the start of the last known line
		private long m_fileend;                               // The last known size of the file
		private long m_bufsize;                               // Cached value of m_settings.FileBufSize
		private int m_line_cache_count;                       // The number of lines to scan about the currently selected row
		private bool m_alternating_line_colours;              // Cache the alternating line colours setting for performance
		private bool m_tail_enabled;                          // Cache whether tail mode is enabled
		private bool m_quick_filter_enabled;                  // True if only rows with highlights should be displayed
		private bool m_first_row_is_odd;                      // Tracks whether the first row is odd or even for alternating row colours (not 100% accurate)

		public Main(StartupOptions startup_options)
		{
			m_startup_options = startup_options;
			m_settings = new Settings(m_startup_options.SettingsPath);

			Log.Register(m_settings.LogFilePath, false);
			Log.Info(this, "App Startup: {0}".Fmt(DateTime.Now));

			InitializeComponent();
			AllowTransparency = true;

			if (m_settings.RestoreScreenLoc)
			{
				StartPosition = FormStartPosition.Manual;
				Location = m_settings.ScreenPosition;
				Size = m_settings.WindowSize;
			}
			
			// Recent files menu
			m_recent = new RecentFiles(m_menu_file_recent, fp => OpenSingleLogFile(fp, true));
			m_recent.ClearRecentFilesListEvent += (s,a) =>
				{
					var res = MessageBox.Show(this, "Do you want to clear the recent files list?", "Clear Recent Files", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
					a.Cancel = res == DialogResult.Cancel;
				};
			
			m_watch               = new FileWatch();
			m_watch_timer         = new Timer{Interval = Constants.FilePollingRate};
			m_batch_set_col_size  = new EventBatcher(100, this);
			m_highlights          = new List<Highlight>();
			m_filters             = new List<Filter>();
			m_transforms          = new List<Transform>();
			m_clkactions          = new List<ClkAction>();
			m_find_history        = new BindingSource{DataSource = new BindingList<Pattern>()};
			m_find_ui             = new FindUI(this, m_find_history){Visible = false};
			m_bookmarks           = new BindingList<Bookmark>();
			m_bs_bookmarks        = new BindingSource{DataSource = m_bookmarks};
			m_bookmarks_ui        = new BookmarksUI(this, m_bs_bookmarks){Visible = false};
			m_tt                  = new ToolTip();
			m_balloon             = new ToolTip{IsBalloon = true};
			m_tab_cycle           = new Form[]{this, m_find_ui, m_bookmarks_ui};
			m_notify_icon         = new NotifyIcon{Icon = Icon};
			m_suspend_grid_events = new RefCount();
			m_line_index          = new List<Range>();
			m_file                = null;
			m_filepos             = 0;
			m_fileend             = 0;
			m_bufsize             = m_settings.FileBufSize;
			m_line_cache_count    = m_settings.LineCacheCount;
			m_tail_enabled        = m_settings.TailEnabled;

			var lic = new Licence(m_startup_options.AppDataDir);
			m_menu_evaluation_version.Visible = !lic.Valid;

			// Startup options
			ApplyStartupOptions();
			
			m_settings.SettingChanged += (s,a)=> Log.Info(this, "Setting {0} changed from {1} to {2}".Fmt(a.Key ,a.OldValue ,a.NewValue));
			
			// Menu
			m_menu.Location                             = Point.Empty;
			m_menu_file_open.Click                     += (s,a) => OpenSingleLogFile(null, true);
			m_menu_file_wizards_androidlogcat.Click    += (s,a) => AndroidLogcatWizard();
			m_menu_file_wizards_aggregatelogfile.Click += (s,a) => AggregateFileWizard();
			m_menu_file_open_stdout.Click              += (s,a) => LogProgramOutput();
			m_menu_file_open_serial_port.Click         += (s,a) => LogSerialPort();
			m_menu_file_open_network.Click             += (s,a) => LogNetworkOutput();
			m_menu_file_open_named_pipe.Click          += (s,a) => LogNamedPipeOutput();
			m_menu_file_close.Click                    += (s,a) => CloseLogFile();
			m_menu_file_export.Click                   += (s,a) => ShowExportDialog();
			m_menu_file_exit.Click                     += (s,a) => Close();
			m_menu_edit_selectall.Click                += (s,a) => DataGridView_Extensions.SelectAll(m_grid, new KeyEventArgs(Keys.Control|Keys.A));
			m_menu_edit_copy.Click                     += (s,a) => DataGridView_Extensions.Copy(m_grid, new KeyEventArgs(Keys.Control|Keys.C));
			m_menu_edit_find.Click                     += (s,a) => ShowFindDialog();
			m_menu_edit_find_next.Click                += (s,a) => FindNext();
			m_menu_edit_find_prev.Click                += (s,a) => FindPrev();
			m_menu_edit_toggle_bookmark.Click          += (s,a) => ToggleBookmark(SelectedRowIndex);
			m_menu_edit_next_bookmark.Click            += (s,a) => NextBookmark();
			m_menu_edit_prev_bookmark.Click            += (s,a) => PrevBookmark();
			m_menu_edit_clearall_bookmarks.Click       += (s,a) => m_bookmarks.Clear();
			m_menu_edit_bookmarks.Click                += (s,a) => ShowBookmarksDialog();
			m_menu_encoding_detect.Click               += (s,a) => SetEncoding(null);
			m_menu_encoding_ascii.Click                += (s,a) => SetEncoding(Encoding.ASCII           );
			m_menu_encoding_utf8.Click                 += (s,a) => SetEncoding(Encoding.UTF8            );
			m_menu_encoding_ucs2_littleendian.Click    += (s,a) => SetEncoding(Encoding.Unicode         );
			m_menu_encoding_ucs2_bigendian.Click       += (s,a) => SetEncoding(Encoding.BigEndianUnicode);
			m_menu_line_ending_detect.Click            += (s,a) => SetLineEnding(ELineEnding.Detect);
			m_menu_line_ending_cr.Click                += (s,a) => SetLineEnding(ELineEnding.CR    );
			m_menu_line_ending_crlf.Click              += (s,a) => SetLineEnding(ELineEnding.CRLF  );
			m_menu_line_ending_lf.Click                += (s,a) => SetLineEnding(ELineEnding.LF    );
			m_menu_line_ending_custom.Click            += (s,a) => SetLineEnding(ELineEnding.Custom);
			m_menu_tools_alwaysontop.Click             += (s,a) => SetAlwaysOnTop(!m_settings.AlwaysOnTop);
			m_menu_tools_ghost_mode.Click              += (s,a) => EnableGhostMode(!m_menu_tools_ghost_mode.Checked);
			m_menu_tools_clear_log_file.Click          += (s,a) => ClearLogFile();
			m_menu_tools_highlights.Click              += (s,a) => ShowOptions(SettingsUI.ETab.Highlights);
			m_menu_tools_filters.Click                 += (s,a) => ShowOptions(SettingsUI.ETab.Filters   );
			m_menu_tools_transforms.Click              += (s,a) => ShowOptions(SettingsUI.ETab.Transforms);
			m_menu_tools_actions.Click                 += (s,a) => ShowOptions(SettingsUI.ETab.Actions   );
			m_menu_tools_options.Click                 += (s,a) => ShowOptions(SettingsUI.ETab.General   );
			m_menu_help_view_help.Click                += (s,a) => ShowHelp();
			m_menu_help_totd.Click                     += (s,a) => ShowTotD();
			m_menu_help_visit_store.Click              += (s,a) => VisitStore();
			m_menu_help_register.Click                 += (s,a) => ShowActivation();
			m_menu_help_check_for_updates.Click        += (s,a) => CheckForUpdates(true);
			m_menu_help_about.Click                    += (s,a) => ShowAbout();
			m_menu_evaluation_version.Click            += (s,a) => ShowAbout();
			m_recent.Import(m_settings.RecentFiles);
			
			// Toolbar
			m_toolstrip.Location            = new Point(0,30);
			m_btn_open_log.ToolTipText      = Resources.OpenLogFile;
			m_btn_open_log.Click           += (s,a) => OpenSingleLogFile(null, true);
			m_btn_refresh.ToolTipText       = Resources.ReloadLogFile;
			m_btn_refresh.Click            += (s,a) => BuildLineIndex(m_filepos, true);
			m_btn_quick_filter.ToolTipText  = Resources.QuickFilter;
			m_btn_quick_filter.Click       += (s,a) => EnableQuickFilter(m_btn_quick_filter.Checked);
			m_btn_highlights.ToolTipText    = Resources.ShowHighlightsDialog;
			m_btn_highlights.Click         += (s,a) => EnableHighlights(m_btn_highlights.Checked);
			m_btn_highlights.MouseDown     += (s,a) => { if (a.Button == MouseButtons.Right) ShowOptions(SettingsUI.ETab.Highlights); };
			m_btn_filters.ToolTipText       = Resources.ShowFiltersDialog;
			m_btn_filters.Click            += (s,a) => EnableFilters(m_btn_filters.Checked);
			m_btn_filters.MouseDown        += (s,a) => { if (a.Button == MouseButtons.Right) ShowOptions(SettingsUI.ETab.Filters); };
			m_btn_transforms.ToolTipText    = Resources.ShowTransformsDialog;
			m_btn_transforms.Click         += (s,a) => EnableTransforms(m_btn_transforms.Checked);
			m_btn_transforms.MouseDown     += (s,a) => { if (a.Button == MouseButtons.Right) ShowOptions(SettingsUI.ETab.Transforms); };
			m_btn_actions.ToolTipText       = Resources.ShowActionsDialog;
			m_btn_actions.Click            += (s,a) => EnableActions(m_btn_actions.Checked);
			m_btn_actions.MouseDown        += (s,a) => { if (a.Button == MouseButtons.Right) ShowOptions(SettingsUI.ETab.Actions); };
			m_btn_find.ToolTipText          = Resources.ShowFindDialog;
			m_btn_find.Click               += (s,a) => ShowFindDialog();
			m_btn_bookmarks.ToolTipText     = Resources.ShowBookmarksDialog;
			m_btn_bookmarks.Click          += (s,a) => ShowBookmarksDialog();
			m_btn_jump_to_start.ToolTipText = "Selected the first row in the log file";
			m_btn_jump_to_start.Click      += (s,a) => BuildLineIndex(0, false, () => SelectedRowIndex = 0);
			m_btn_jump_to_end.ToolTipText   = "Selected the last row in the log file";
			m_btn_jump_to_end.Click        += (s,a) => BuildLineIndex(m_fileend, false, () => SelectedRowIndex = m_grid.RowCount - 1);
			m_btn_tail.ToolTipText          = Resources.WatchTail;
			m_btn_tail.Click               += (s,a) => EnableTail(m_btn_tail.Checked);
			m_btn_watch.ToolTipText         = Resources.WatchForUpdates;
			m_btn_watch.Click              += (s,a) => EnableWatch(m_btn_watch.Checked);
			m_btn_additive.ToolTipText      = Resources.AdditiveMode;
			m_btn_additive.Click           += (s,a) => EnableAdditive(m_btn_additive.Checked);
			ToolStripManager.Renderer       = new CheckedButtonRenderer();
			
			// Scrollbar
			m_scroll_file.ToolTip(m_tt, "Indicates the currently cached position in the log file");
			m_scroll_file.MinThumbSize = 1;
			m_scroll_file.ScrollEnd += (s,a)=>
				{
					// Update on ScrollEnd not value changed, since
					// UpdateUI() sets Value when the build is complete.
					var range = m_scroll_file.ThumbRange;
					long pos = (range.Begin == 0) ? 0 : (range.End == m_fileend) ? m_fileend : range.Mid;
					Log.Info(this, "file scroll to {0}".Fmt(pos));
					BuildLineIndex(pos, false);
				};

			// Status
			m_status.Location             = Point.Empty;
			m_status_progress.ToolTipText = "Press escape to cancel";
			m_status_progress.Minimum     = 0;
			m_status_progress.Maximum     = 100;
			m_status_progress.Visible     = false;
			m_status_progress.Text        = "Test";

			// Setup the grid
			m_grid.RowCount             = 0;
			m_grid.AutoGenerateColumns  = false;
			m_grid.KeyDown             += DataGridView_Extensions.SelectAll;
			m_grid.KeyDown             += DataGridView_Extensions.Copy;
			m_grid.KeyUp               += (s,a) => LoadNearBoundary();
			m_grid.MouseUp             += (s,a) => GridMouseButton(a, false);
			m_grid.MouseDown           += (s,a) => GridMouseButton(a, true);
			m_grid.CellValueNeeded     += CellValueNeeded;
			m_grid.RowPrePaint         += RowPrePaint;
			m_grid.CellPainting        += CellPainting;
			m_grid.RowPostPaint        += RowPostPaint;
			m_grid.SelectionChanged    += GridSelectionChanged;
			m_grid.CellDoubleClick     += CellDoubleClick;
			m_grid.RowHeightInfoNeeded += (s,a) => { a.Height = m_row_height; };
			m_grid.DataError           += (s,a) => Debug.Assert(false);
			m_grid.Scroll              += (s,a) => GridScroll();

			// Grid context menu
			m_cmenu_grid.ItemClicked    += GridContextMenu;
			m_cmenu_grid.VisibleChanged += SetGridContextMenuVisibility;

			// File Watcher
			m_watch_timer.Tick += (s,a)=>
				{
					if (ReloadInProgress) return;
					if (WindowState == FormWindowState.Minimized) return;
					try { m_watch.CheckForChangedFiles(); }
					catch (Exception ex) { Log.Exception(this, ex, "CheckForChangedFiles failed"); }
				};
			
			// Column size event batcher
			m_batch_set_col_size.Action += SetGridColumnSizesImpl;
			
			// Find
			InitFind();
			
			// Bookmarks
			m_bs_bookmarks.PositionChanged += (s,a) => SelectBookmark(m_bs_bookmarks.Position);
			m_bookmarks_ui.NextBookmark    += NextBookmark;
			m_bookmarks_ui.PrevBookmark    += PrevBookmark;
			
			// Startup
			Shown += (s,a)=> Startup();
			
			// File Drop
			DragEnter += (s,a) => FileDrop(a, true);
			DragDrop  += (s,a) => FileDrop(a, false);
			
			// Resize
			SizeChanged += (s,a)=> UpdateUI();

			// Shutdown
			FormClosing += (s,a) =>
				{
					m_settings.ScreenPosition = Location;
					m_settings.WindowSize = Size;
					m_settings.RecentFiles = m_recent.Export();
				};
			
			InitCache();
			ApplySettings();
		}

		/// <summary>Apply the startup options</summary>
		private void ApplyStartupOptions()
		{
			StartupOptions su = m_startup_options;
			
			// If a pattern set file path is given, replace the patterns in 'm_settings'
			// with the contents of the file
			if (su.HighlightSetPath != null)
			{
				try
				{
					XDocument doc = XDocument.Load(su.HighlightSetPath);
					if (doc.Root == null) throw new InvalidDataException("Invalid highlight set, root xml node not found");
					if (doc.Root.Element(XmlTag.Highlight) == null) throw new InvalidDataException("Highlight set file does not contain any highlight descriptions");
					m_settings.HighlightPatterns = doc.ToString(SaveOptions.None);
				}
				catch (Exception ex)
				{
					Misc.ShowErrorMessage(this, ex, string.Format("Could not load highlight pattern set {0}.", su.HighlightSetPath), Resources.LoadPatternSetFailed);
				}
			}
			if (su.FilterSetPath != null)
			{
				try
				{
					XDocument doc = XDocument.Load(su.FilterSetPath);
					if (doc.Root == null) throw new InvalidDataException("Invalid filter set, root xml node not found");
					if (doc.Root.Element(XmlTag.Filter) == null) throw new InvalidDataException("Filter set file does not contain any filter descriptions");
					m_settings.FilterPatterns = doc.ToString(SaveOptions.None);
				}
				catch (Exception ex)
				{
					Misc.ShowErrorMessage(this, ex, string.Format("Could not load filter pattern set {0}.", su.FilterSetPath), Resources.LoadPatternSetFailed);
				}
			}
			if (su.TransformSetPath != null)
			{
				try
				{
					XDocument doc = XDocument.Load(su.TransformSetPath);
					if (doc.Root == null) throw new InvalidDataException("Invalid transform set, root xml node not found");
					if (doc.Root.Element(XmlTag.Transform) == null) throw new InvalidDataException("Transform set file does not contain any transform descriptions");
					m_settings.TransformPatterns = doc.ToString(SaveOptions.None);
				}
				catch (Exception ex)
				{
					Misc.ShowErrorMessage(this, ex, string.Format("Could not load transform pattern set {0}.", su.TransformSetPath), Resources.LoadPatternSetFailed);
				}
			}
		}

		/// <summary>Called the first time the app is displayed</summary>
		private void Startup()
		{
			StartupOptions su = m_startup_options;
			
			// Parse command line
			if (su.FileToLoad != null)
			{
				OpenSingleLogFile(su.FileToLoad, true);
			}
			else if (m_settings.LoadLastFile && File.Exists(m_settings.LastLoadedFile))
			{
				OpenSingleLogFile(m_settings.LastLoadedFile, true);
			}
			
			// Show the TotD
			if (m_settings.ShowTOTD)
				ShowTotD();
			
			// Check for updates
			if (m_settings.CheckForUpdates)
				CheckForUpdates(false);
		}

		/// <summary>Returns true if there is a log file currently open</summary>
		private bool FileOpen
		{
			get { return m_file != null; }
		}
		
		/// <summary>Close the current log file</summary>
		private void CloseLogFile()
		{
			using (m_suspend_grid_events.Refer)
			{
				CancelBuildLineIndex();
				m_line_index.Clear();
				if (FileOpen) m_watch.Remove(m_file.Filepaths);
				if (m_buffered_process    != null) m_buffered_process.Dispose();
				if (m_buffered_tcp_netconn != null) m_buffered_tcp_netconn.Dispose();
				if (m_buffered_udp_netconn != null) m_buffered_udp_netconn.Dispose();
				if (m_buffered_serialconn != null) m_buffered_serialconn.Dispose();
				if (m_buffered_pipeconn   != null) m_buffered_pipeconn.Dispose();
				m_buffered_process = null;
				m_buffered_tcp_netconn = null;
				m_buffered_udp_netconn = null;
				m_buffered_serialconn = null;
				m_buffered_pipeconn = null;
				if (FileOpen) m_file.Dispose();
				m_file = null;
				m_filepos = 0;
				m_fileend = 0;
				SetTransientStatusMessage(null);
				SetStaticStatusMessage(null);
			}

			// Initiate a UI update after any existing queued events
			this.BeginInvoke(() => UpdateUI());
		}

		/// <summary>Checks each file in 'filepaths' is valid. Returns false and displays an error if not</summary>
		private bool ValidateFilepaths(IEnumerable<string> filepaths)
		{
			foreach (var file in filepaths)
			{
				// Reject invalid file paths
				if (!file.HasValue())
				{
					MessageBox.Show(this, "File path is invalid", Resources.InvalidFilePath, MessageBoxButtons.OK,MessageBoxIcon.Error);
					return false;
				}

				// Check that the file exists, this can take ages if 'filepath' is a network file
				if (!Misc.FileExists(this, file))
				{
					if (m_recent.IsInRecents(file))
					{
						var res = MessageBox.Show(this, "File path '{0}' is invalid or does not exist\r\n\r\nRemove from recent files list?".Fmt(file), Resources.InvalidFilePath, MessageBoxButtons.YesNo,MessageBoxIcon.Error);
						if (res == DialogResult.Yes)
							m_recent.Remove(file, true);
					}
					else
					{
						MessageBox.Show(this, "File path '{0}' is invalid or does not exist".Fmt(file), Resources.InvalidFilePath, MessageBoxButtons.OK,MessageBoxIcon.Error);
					}
					return false;
				}
			}
			return true;
		}

		/// <summary>Adopt a new file source, closing any previously open file source</summary>
		private void NewFileSource(IFileSource file)
		{
			CloseLogFile();
			m_file = file.Open();

			m_filepos = m_settings.OpenAtEnd ? m_file.Stream.Length : 0;

			// Setup the watcher to watch for file changes
			m_watch.Add(m_file.Filepaths, (fp,ctx) => { OnFileChanged(); return true; });
			m_watch_timer.Enabled = FileOpen && m_settings.WatchEnabled;

			BuildLineIndex(m_filepos, true, ()=>
				{
					SelectedRowIndex = m_settings.OpenAtEnd ? m_grid.RowCount - 1 : 0;
						
					// Show a hint if filters are active, the file isn't empty, but there are no visible rows
					if (m_grid.RowCount == 0 && m_fileend != 0)
					{
						if (m_filters.Count != 0)        ShowHintBalloon("Filters are currently active", m_btn_filters);
						else if (m_quick_filter_enabled) ShowHintBalloon("Filters are currently active", m_btn_quick_filter);
					}
				});
		}

		/// <summary>Open a single log file, prompting if 'filepath' is null</summary>
		private void OpenSingleLogFile(string filepath, bool add_to_recent)
		{
			try
			{
				// Prompt for a file if none provided
				if (filepath == null)
				{
					var fd = new OpenFileDialog{Filter = Resources.LogFileFilter, Multiselect = false};
					if (fd.ShowDialog() != DialogResult.OK) return;
					filepath = fd.FileName;
				}

				// Check the filepath is valid
				if (!ValidateFilepaths(Enumerable.Repeat(filepath, 1)))
					return;

				if (add_to_recent)
				{
					m_recent.Add(filepath);
					m_settings.RecentFiles = m_recent.Export();
					m_settings.LastLoadedFile = filepath;
				}
				
				// Switch files - open the file to make sure it's accessible (and to hold a lock)
				NewFileSource(new SingleFile(filepath));
			}
			catch (Exception ex)
			{
				Misc.ShowErrorMessage(this, ex, "Failed to open file {0} due to an error.".Fmt(filepath), Resources.FailedToLoadFile);
				CloseLogFile();
			}
		}

		/// <summary>Open multiple log files in aggregate</summary>
		private void OpenAggregateLogFile(List<string> filepaths)
		{
			try
			{
				// Check the filepath is valid
				if (!ValidateFilepaths(filepaths))
					return;

				// Switch files - open the file to make sure it's accessible (and to hold a lock)
				NewFileSource(new AggregateFile(filepaths));
			}
			catch (Exception ex)
			{
				Misc.ShowErrorMessage(this, ex, "Failed to open aggregate log files due to an error.", Resources.FailedToLoadFile);
				CloseLogFile();
			}
		}

		/// <summary>Show the android device log wizard</summary>
		private void AndroidLogcatWizard()
		{
			var dg = new AndroidLogcatUI(m_settings);
			if (dg.ShowDialog(this) != DialogResult.OK) return;
			ApplySettings();
			LaunchProcess(dg.Launch);
		}

		/// <summary>Show the aggregate log file wizard</summary>
		private void AggregateFileWizard()
		{
			var dg = new AggregateFilesUI();
			if (dg.ShowDialog(this) != DialogResult.OK) return;
			
			var filepaths = dg.Filepaths.ToList();
			if (filepaths.Count == 0) return;
			if (filepaths.Count == 1)
				OpenSingleLogFile(filepaths[0], true);
			else
				OpenAggregateLogFile(filepaths);
		}

		/// <summary>Open a standard out connection</summary>
		private void LogProgramOutput()
		{
			var dg = new ProgramOutputUI(m_settings);
			if (dg.ShowDialog(this) != DialogResult.OK) return;
			LaunchProcess(dg.Launch);
		}

		/// <summary>Open a serial port connection and log the received data</summary>
		private void LogSerialPort()
		{
			var dg = new SerialConnectionUI(m_settings);
			if (dg.ShowDialog(this) != DialogResult.OK) return;
			LogSerialConnection(dg.Conn);
		}

		/// <summary>Open a network connection and log the received data</summary>
		private void LogNetworkOutput()
		{
			var dg = new NetworkConnectionUI(m_settings);
			if (dg.ShowDialog(this) != DialogResult.OK) return;
			
			if (dg.Conn.ProtocolType == ProtocolType.Tcp)
				LogTcpNetConnection(dg.Conn);
			else if (dg.Conn.ProtocolType == ProtocolType.Udp)
				LogUdpNetConnection(dg.Conn);
		}

		/// <summary>Open a named pipe and log the received data</summary>
		private void LogNamedPipeOutput()
		{
			var dg = new NamedPipeUI(m_settings);
			if (dg.ShowDialog(this) != DialogResult.OK) return;
			LogNamedPipeConnection(dg.Conn);
		}

		/// <summary>Called when the log file is noticed to have changed</summary>
		private void OnFileChanged()
		{
			long len = m_file.Stream.Length;
			Log.Info(this, "File {0} changed. File length: {1}".Fmt(m_file.Name, len));
			long filepos = AutoScrollTail ? m_file.Stream.Length : m_filepos;
			bool reload  = m_file.Stream.Length < m_fileend || !m_settings.FileChangesAdditive;
			BuildLineIndex(filepos, reload);
		}

		/// <summary>Supply the grid with values</summary>
		private void CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			if (!FileOpen || GridEventsBlocked) e.Value = null;
			else
			{
				try { e.Value = ReadLine(e.RowIndex)[e.ColumnIndex].Text; }
				catch { e.Value = ""; }
			}
		}

		/// <summary>Called before drawing the row background</summary>
		private void RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
		{
			e.Handled = false;
			var grid = (DataGridView)sender;
			if (e.RowIndex < 0 || e.RowIndex >= grid.RowCount) return;

			// Leave rendering to the grid while events are suspended
			if (GridEventsBlocked || !FileOpen)
				return;

			// Read the line from the cache
			var line = ReadLine(e.RowIndex);
			var bounds = e.RowBounds;
			Highlight hl;

			e.PaintParts &= ~DataGridViewPaintParts.ContentBackground;
			e.PaintParts &= ~DataGridViewPaintParts.Border;

			// Give the illusion that the alternating row colour is moving with the overall file
			Color bkcolour = m_grid.RowsDefaultCellStyle.BackColor;
			if (m_alternating_line_colours)
			{
				var cs = ((e.RowIndex & 1) == 1) == m_first_row_is_odd
					? m_grid.RowsDefaultCellStyle
					: m_grid.AlternatingRowsDefaultCellStyle;

				bkcolour = cs.BackColor;
			}

			// If the line is bookmarked, use the bookmark colour
			if (m_bookmarks.Count != 0 && m_bookmarks.BinarySearch(x => x.Position.CompareTo(line.LineStartAddr)) >= 0)
				bkcolour = m_settings.BookmarkColour;

			// If the whole row is highlighted, do it
			else if (line.Column.Count == 1 && (hl = line[0].HL) != null)
				bkcolour = hl.BackColour; // Assuming hl.BinaryMatch here... todo support partial row highlighting...

			// Paint the background
			using (var b = new SolidBrush(bkcolour))
				e.Graphics.FillRectangle(b, bounds);

			// If the row is selected, use the selection colour
			if ((e.State & DataGridViewElementStates.Selected) != 0)
			{
				// Fill the selected area in semi-transparent
				using (var b = new SolidBrush(Color.FromArgb(128, m_grid.DefaultCellStyle.SelectionBackColor)))
					e.Graphics.FillRectangle(b, bounds);
			}

			// Only using RowPrePaint to draw row backgrounds, therefore
			// e.Handled is false to cause the CellPainting and RowPostPaint
			// methods to be called
		}

		/// <summary>Draw the cell appropriate to any highlighting</summary>
		private void CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			e.Handled = false;
			var grid = (DataGridView)sender;
			if (e.RowIndex < 0 || e.RowIndex >= grid.RowCount) return;

			// Leave rendering to the grid while events are suspended
			if (GridEventsBlocked || !FileOpen)
				return;

			e.Handled = true;

			// Read the line from the cache
			var line = ReadLine(e.RowIndex);
			Highlight hl;

			// If the line is bookmarked, use the bookmark colour
			if (m_bookmarks.Count != 0 && m_bookmarks.BinarySearch(x => x.Position.CompareTo(line.LineStartAddr)) >= 0)
			{
				e.PaintContent(e.ClipBounds);
			}

			// Check if the cell value has a highlight pattern it matches
			else if ((hl = line[e.ColumnIndex].HL) != null)
			{
				if (hl.BinaryMatch)
				{
					e.CellStyle.BackColor = hl.BackColour;
					e.CellStyle.ForeColor = hl.ForeColour;
					e.PaintContent(e.ClipBounds);
				}
				else
				{
					//todo
					e.PaintContent(e.ClipBounds);
				}
			}
			else
			{
				e.PaintContent(e.ClipBounds);
			}
		}

		/// <summary>Called after the cells have been drawn</summary>
		private void RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex < 0 || e.RowIndex >= grid.RowCount) return;

			// Leave rendering to the grid while events are suspended
			if (GridEventsBlocked || !FileOpen)
				return;

			var bounds = e.RowBounds;

			// Draw a box around the selection
			if ((e.State & DataGridViewElementStates.Selected) != 0)
			{
				const float pen_width = 3f, pen_hwidth = pen_width*0.5f;
				var fbounds = new RectangleF(bounds.Left + pen_hwidth, bounds.Top + pen_hwidth, bounds.Width - pen_width, bounds.Height - pen_width);

				// Draw a border around the selection
				using (var p = new Pen(m_grid.DefaultCellStyle.SelectionBackColor, pen_width){StartCap = LineCap.Square, EndCap = LineCap.Square})
				{
					e.Graphics.DrawLine(p, fbounds.Left , fbounds.Top, fbounds.Left , fbounds.Bottom);
					e.Graphics.DrawLine(p, fbounds.Right, fbounds.Top, fbounds.Right, fbounds.Bottom);
					if (e.RowIndex == 0 || !grid.Rows[e.RowIndex - 1].Selected)
					{
						e.Graphics.DrawLine(p, fbounds.Left, fbounds.Top, fbounds.Right, fbounds.Top);
					}
					if (e.RowIndex == grid.RowCount - 1 || !grid.Rows[e.RowIndex + 1].Selected)
					{
						e.Graphics.DrawLine(p, fbounds.Left, fbounds.Bottom, fbounds.Right, fbounds.Bottom);
					}
				}
			}
		}

		/// <summary>Handler for selections made in the grid</summary>
		private void GridSelectionChanged(object sender, EventArgs e)
		{
			if (GridEventsBlocked) return;
			EnableTail(SelectedRowIndex == m_grid.RowCount - 1);
			UpdateStatus();
		}

		/// <summary>Handler for cell double clicks</summary>
		private void CellDoubleClick(object sender, DataGridViewCellEventArgs args)
		{
			if (!m_settings.ActionsEnabled) return;
			if (args.RowIndex < 0 || args.RowIndex > m_line_index.Count) return;

			var line = ReadLine(args.RowIndex);
			var columns = line.Column;
			if (args.ColumnIndex < 0 || args.ColumnIndex > columns.Count) return;

			// Read the text for the column and look for a matching action
			var text = columns[args.ColumnIndex].Text;
			foreach (var a in m_clkactions)
			{
				if (!a.IsMatch(text)) continue;
				try { a.Execute(text, m_file.FilepathAt(line.LineStartAddr)); }
				catch { SetTransientStatusMessage("Action Failed", Color.Red, SystemColors.Control); }
				break;
			}
		}

		/// <summary>Handler for mouse down/up events on the grid</summary>
		private void GridMouseButton(MouseEventArgs args, bool button_down)
		{
			switch (args.Button)
			{
			case MouseButtons.Left:
				if (!button_down) LoadNearBoundary();
				break;
			case MouseButtons.Right:
				if (button_down && SelectedRowCount <= 1)
				{
					var hit = m_grid.HitTest(args.X, args.Y);
					if (hit.RowIndex >= 0 && hit.RowIndex < m_grid.RowCount)
						SelectedRowIndex = hit.RowIndex;
				}
				break;
			}
		}

		/// <summary>Handle grid context menu actions</summary>
		private void GridContextMenu(object sender, ToolStripItemClickedEventArgs args)
		{
			// Note: I'm not attaching event handlers to each context menu item because
			// some items require the selected row to be set and some don't. Plus the
			// individual handlers don't have access to the click location

			// Find the row that the context menu was opened on
			var pt = m_grid.PointToClient(((ContextMenuStrip)sender).Location);
			var hit = m_grid.HitTest(pt.X, pt.Y);
			if (hit.RowIndex < 0 || hit.RowIndex >= m_grid.RowCount)
				return;

			// Clipboard operations
			if (args.ClickedItem == m_cmenu_copy      ) { DataGridView_Extensions.Copy(m_grid);      return; }
			if (args.ClickedItem == m_cmenu_select_all) { DataGridView_Extensions.SelectAll(m_grid); return; }
			if (args.ClickedItem == m_cmenu_clear_log ) { ClearLogFile(); return; }

			if (args.ClickedItem == m_cmenu_highlight_row) { ShowOptions(SettingsUI.ETab.Highlights); return; }
			if (args.ClickedItem == m_cmenu_filter_row   ) { ShowOptions(SettingsUI.ETab.Filters   ); return; }
			if (args.ClickedItem == m_cmenu_transform_row) { ShowOptions(SettingsUI.ETab.Transforms); return; }
			if (args.ClickedItem == m_cmenu_action_row   ) { ShowOptions(SettingsUI.ETab.Actions   ); return; }

			// Find operations
			if (args.ClickedItem == m_cmenu_find_next) { m_find_ui.Pattern.Expr = ReadLine(hit.RowIndex).RowText; m_find_ui.RaiseFindNext(); return; }
			if (args.ClickedItem == m_cmenu_find_prev) { m_find_ui.Pattern.Expr = ReadLine(hit.RowIndex).RowText; m_find_ui.RaiseFindPrev(); return; }

			// Bookmarks
			if (args.ClickedItem == m_cmenu_toggle_bookmark) { ToggleBookmark(hit.RowIndex); }
		}

		/// <summary>Set the visibility of context menu options</summary>
		private void SetGridContextMenuVisibility(object sender, EventArgs args)
		{
			bool has_rows                   = m_grid.RowCount != 0;
			bool has_selected_rows          = SelectedRowCount != 0;
			m_cmenu_grid.Tag                = MousePosition; // save the mouse position in 'Tag'
			m_cmenu_copy.Enabled            = has_selected_rows;
			m_cmenu_select_all.Enabled      = has_rows;
			m_cmenu_clear_log.Enabled       = has_rows;
			m_cmenu_highlight_row.Enabled   = has_selected_rows;
			m_cmenu_filter_row.Enabled      = has_selected_rows;
			m_cmenu_transform_row.Enabled   = has_selected_rows;
			m_cmenu_action_row.Enabled      = has_selected_rows;
			m_cmenu_find_next.Enabled       = has_rows;
			m_cmenu_find_prev.Enabled       = has_rows;
			m_cmenu_toggle_bookmark.Enabled = has_rows;
		}

		/// <summary>Called when the grid is scrolled</summary>
		private void GridScroll()
		{
			if (GridEventsBlocked) return;
			UpdateFileScroll();
			SetGridColumnSizes();

			// Selected rows use transparency so we need to invalidate the entire row
			foreach (var r in m_grid.GetRowsWithState(DataGridViewElementStates.Displayed|DataGridViewElementStates.Selected))
				m_grid.InvalidateRow(r.Index);
		}

		/// <summary>Tests whether the currently selected row is near the start or end of the line range and causes a reload if it is</summary>
		private void LoadNearBoundary()
		{
			if (m_grid.RowCount < Constants.AutoScrollAtBoundaryLimit) return;
			const float Limit = 1f / Constants.AutoScrollAtBoundaryLimit;
			float ratio = Maths.Frac(0, SelectedRowIndex, m_grid.RowCount - 1);
			if (ratio < 0f + Limit) BuildLineIndex(LineStartIndexRange.Begin, false);
			if (ratio > 1f - Limit) BuildLineIndex(LineStartIndexRange.End  , false);
		}

		/// <summary>Handle global command keys</summary>
		protected override bool ProcessCmdKey(ref Message msg, Keys key_data)
		{
			if (HandleKeyDown(this, key_data)) return true;
			return base.ProcessCmdKey(ref msg, key_data);
		}

		/// <summary>Handle key down events for the main form</summary>
		public bool HandleKeyDown(Form caller, Keys keys)
		{
			switch (keys)
			{
			default: return false;
			case Keys.Escape:                     CancelBuildLineIndex();             return true;
			case Keys.F2:                         NextBookmark();                     return true;
			case Keys.F2|Keys.Shift:              PrevBookmark();                     return true;
			case Keys.F2|Keys.Control:            ToggleBookmark(SelectedRowIndex);        return true;
			case Keys.F3:                         FindNext();                         return true;
			case Keys.F3|Keys.Shift:              FindPrev();                         return true;
			case Keys.F3|Keys.Control:            SetFindPattern(SelectedRowIndex, true);  return true;
			case Keys.F3|Keys.Shift|Keys.Control: SetFindPattern(SelectedRowIndex, false); return true;
			case Keys.F5:                         BuildLineIndex(m_filepos, true);    return true;
			case Keys.PageUp  |Keys.Control:      BuildLineIndex(0        , false, () => SelectedRowIndex = 0                  ); return true;
			case Keys.PageDown|Keys.Control:      BuildLineIndex(m_fileend, false, () => SelectedRowIndex = m_grid.RowCount - 1); return true;
			case Keys.Tab|Keys.Control:
				{
					int idx = (m_tab_cycle.IndexOf(x => ReferenceEquals(x,caller)) + 1) % m_tab_cycle.Length;
					for (int j = 0, jend = m_tab_cycle.Length; j != jend; ++j, idx = (idx+1)%jend)
					{
						if (!m_tab_cycle[idx].Visible) continue;
						m_tab_cycle[idx].Focus();
						break;
					}
					return true;
				}
			}
		}
		
		/// <summary>Handle file drop</summary>
		private void FileDrop(DragEventArgs args, bool test_can_drop)
		{
			args.Effect = DragDropEffects.None;
			
			// File drop only
			if (!args.Data.GetDataPresent(DataFormats.FileDrop))
				return;
			
			// Single file drop only
			string[] files = (string[])args.Data.GetData(DataFormats.FileDrop);
			if (files.Length != 1)
				return;
			
			args.Effect = DragDropEffects.All;
			if (test_can_drop) return;
			
			// Open the dropped file
			OpenSingleLogFile(files[0], true);
		}

		/// <summary>Turn on/off quick filter mode</summary>
		private void EnableQuickFilter(bool enable)
		{
			int row_index = SelectedRowIndex;
			var current = row_index != -1 ? m_line_index[row_index].Begin : -1;
			m_settings.QuickFilterEnabled = enable;
			ApplySettings();
			BuildLineIndex(m_filepos, true, ()=>
				{
					SelectedRowIndex = LineIndex(m_line_index, current);
				});
		}

		/// <summary>Turn on/off highlights</summary>
		private void EnableHighlights(bool enable)
		{
			m_settings.HighlightsEnabled = enable;
			ApplySettings();
			InvalidateCache();
			m_grid.Refresh();
		}

		/// <summary>Turn on/off filters</summary>
		private void EnableFilters(bool enable)
		{
			int row_index = SelectedRowIndex;
			var current = row_index != -1 ? m_line_index[row_index].Begin : -1;
			m_settings.FiltersEnabled = enable;
			ApplySettings();
			BuildLineIndex(m_filepos, true, ()=>
				{
					SelectedRowIndex = LineIndex(m_line_index, current);
				});
		}

		/// <summary>Turn on/off transforms</summary>
		private void EnableTransforms(bool enable)
		{
			m_settings.TransformsEnabled = enable;
			ApplySettings();
			BuildLineIndex(m_filepos, true);
		}

		/// <summary>Turn on/off actions</summary>
		private void EnableActions(bool enabled)
		{
			m_settings.ActionsEnabled = enabled;
			ApplySettings();
		}

		/// <summary>Turn on/off tail mode</summary>
		private void EnableTail(bool enabled)
		{
			if (enabled == m_tail_enabled) return;
			m_settings.TailEnabled = m_tail_enabled = enabled;
			ApplySettings();
		}

		/// <summary>Turn on/off tail mode</summary>
		private void EnableWatch(bool enable)
		{
			m_settings.WatchEnabled = enable;
			ApplySettings();
			if (enable) BuildLineIndex(m_filepos, m_settings.FileChangesAdditive);
		}

		/// <summary>Turn on/off additive only mode</summary>
		private void EnableAdditive(bool enable)
		{
			m_settings.FileChangesAdditive = enable;
			ApplySettings();
			if (!enable) BuildLineIndex(m_filepos, true);
		}

		/// <summary>Try to remove data from the log file</summary>
		private void ClearLogFile()
		{
			var err = m_file.Clear();
			if (err == null)
			{
				InvalidateCache();
			}
			else
			{
				MessageBox.Show(this, string.Format(
					"Clearing file {0} failed.\r\n" +
					"Reason: {1}\r\n" +
					"\r\n" +
					"Usually clearing the log file fails if another application holds an " +
					"exclusive lock on the file. Stop any processes that are using the file " +
					"and try again. Note, if you are using 'Log Program Output', the program " +
					"you are running may be holding the file lock."
					,m_file.Name
					,err.Message)
					,Resources.ClearLogFailed
					,MessageBoxButtons.OK
					,MessageBoxIcon.Information);
			}
		}

		/// <summary>Enable/Disable ghost mode</summary>
		private void EnableGhostMode(bool enabled)
		{
			if (enabled)
			{
				var dg = new GhostModeUI(this);
				if (dg.ShowDialog(this) != DialogResult.OK) return;
				m_menu_tools_ghost_mode.Checked = true;
				
				Opacity = dg.Alpha;
				if (dg.ClickThru)
				{
					uint style = Win32.GetWindowLong(Handle, Win32.GWL_EXSTYLE);
					style = Bit.SetBits(style, Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT, true);
					Win32.SetWindowLong(Handle, Win32.GWL_EXSTYLE, style);
				}
				
				// Self removing icon clicked delegate
				EventHandler icon_clicked = null;
				icon_clicked = (s,a)=>
					{
						// ReSharper disable AccessToModifiedClosure
						m_notify_icon.Visible = false;
						m_notify_icon.Click -= icon_clicked;
						EnableGhostMode(false);
						// ReSharper restore AccessToModifiedClosure
					};
				
				m_notify_icon.Click += icon_clicked;
				m_notify_icon.ShowBalloonTip(1000, "Ghost Mode", "Click here to cancel ghost mode", ToolTipIcon.Info);
				m_notify_icon.Text = "Click to disable ghost mode";
				m_notify_icon.Visible = true;
			}
			else
			{
				m_menu_tools_ghost_mode.Checked = false;
				Opacity = 1f;
				uint style = Win32.GetWindowLong(Handle, Win32.GWL_EXSTYLE);
				style = Bit.SetBits(style, Win32.WS_EX_TRANSPARENT, false);
				Win32.SetWindowLong(Handle, Win32.GWL_EXSTYLE, style);
			}
		}

		/// <summary>Set the encoding to use with loaded files. 'null' means auto detect</summary>
		private void SetEncoding(Encoding encoding)
		{
			string enc_name = encoding == null ? string.Empty : encoding.EncodingName;
			if (enc_name == m_settings.Encoding) return; // not changed.
			
			// If a specific encoding is given, use it.
			// Otherwise leave it as whatever it is now, but reloading
			// the file will cause it to be auto detected.
			if (encoding != null)
				m_encoding = encoding;

			m_settings.Encoding = enc_name;
			ApplySettings();
			BuildLineIndex(m_filepos, true);
		}
		
		/// <summary>Set the line ending to use with loaded files</summary>
		private void SetLineEnding(ELineEnding ending)
		{
			string row_delim;
			switch (ending)
			{
			default: throw new ApplicationException("Unknown line ending type");
			case ELineEnding.Detect: row_delim = ""; break;
			case ELineEnding.CR:     row_delim = "<CR>"; break;
			case ELineEnding.CRLF:   row_delim = "<CR><LF>"; break;
			case ELineEnding.LF:     row_delim = "<LF>"; break;
			case ELineEnding.Custom:
				ShowOptions(SettingsUI.ETab.General, SettingsUI.ESpecial.ShowLineEndingTip);
				return;
			}
			if (row_delim == m_settings.RowDelimiter) return; // not changed
			m_settings.RowDelimiter = row_delim;
			ApplySettings();
			BuildLineIndex(m_filepos, true);
		}

		/// <summary>Set always on top</summary>
		private void SetAlwaysOnTop(bool onatop)
		{
			m_menu_tools_alwaysontop.Checked = onatop;
			m_settings.AlwaysOnTop = onatop;
			TopMost = onatop;
		}
		
		/// <summary>Display the options dialog</summary>
		private void ShowOptions(SettingsUI.ETab tab, SettingsUI.ESpecial special = SettingsUI.ESpecial.None)
		{
			string row_text = "";
			string test_text = PatternUI.DefaultTestText;
			int init_row = SelectedRowIndex;
			if (init_row != -1)
				row_text = test_text = ReadLine(init_row).RowText.Trim();
			
			// Save current settings so the settingsUI starts with the most up to date
			// Show the settings dialog, then reload the settings
			var ui = new SettingsUI(m_settings, tab, special);
			switch (tab)
			{
			default: throw new ArgumentOutOfRangeException("tab");
			case SettingsUI.ETab.General: break;
			case SettingsUI.ETab.LogView: break;
			case SettingsUI.ETab.Highlights:
				ui.HighlightUI.Pattern.Expr = row_text;
				ui.HighlightUI.TestText = test_text;
				break;
			case SettingsUI.ETab.Filters:
				ui.FilterUI.Pattern.Expr = row_text;
				ui.FilterUI.TestText = test_text;
				break;
			case SettingsUI.ETab.Transforms:
				ui.TransformUI.Pattern.Expr = row_text;
				ui.TransformUI.Pattern.Replace = row_text;
				ui.TransformUI.TestText = test_text;
				break;
			case SettingsUI.ETab.Actions:
				ui.ActionUI.Pattern.Expr = row_text;
				ui.ActionUI.TestText = test_text;
				break;
			}
			ui.ShowDialog(this);
			ApplySettings();
			
			// Show hints if patterns where added but there will be no visible
			// change on the main view because that behaviour is disabled
			if      (ui.HighlightsChanged && !m_settings.HighlightsEnabled) ShowHintBalloon("Highlights are currently disabled", m_btn_highlights);
			else if (ui.FiltersChanged    && !m_settings.FiltersEnabled   ) ShowHintBalloon("Filters are currently disabled"   , m_btn_filters);
			else if (ui.TransformsChanged && !m_settings.TransformsEnabled) ShowHintBalloon("Transforms are currently disabled", m_btn_transforms);
			else if (ui.ActionsChanged    && !m_settings.ActionsEnabled   ) ShowHintBalloon("Actions are currently disabled"   , m_btn_actions);
			
			if ((ui.WhatsChanged & EWhatsChanged.FileParsing) != 0)
			{
				BuildLineIndex(m_filepos, true);
			}
			else if ((ui.WhatsChanged & EWhatsChanged.Rendering) != 0)
			{
				InvalidateCache();
				UpdateUI();
			}
		}

		/// <summary>Launch a web browser in order to view the html documentation</summary>
		private void ShowHelp()
		{
			try
			{
				var dir = Path.GetDirectoryName(Application.ExecutablePath) ?? string.Empty;
				var start_page = Path.Combine(dir, @"docs\help.html");
				Process.Start(start_page);
			}
			catch (Exception ex)
			{
				MessageBox.Show(this,
					"Unable to display the help documentation do to an error.\r\n" +
					"Error Message: {0}\r\n".Fmt(ex.Message) +
					"\r\n" +
					"The expected location of the main documentation file is <install directory>\\docs\\help.html",
					"Missing help files", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>Show the TotD dialog</summary>
		private void ShowTotD()
		{
			new TipOfTheDay(m_settings).ShowDialog(this);
		}

		/// <summary>Returns the web proxy to use, if specified in the settings</summary>
		private IWebProxy Proxy
		{
			get
			{
				IWebProxy proxy = WebRequest.DefaultWebProxy;
				if (m_settings.UseWebProxy && !m_settings.WebProxyHost.HasValue())
				{
					try { proxy =  new WebProxy(m_settings.WebProxyHost, m_settings.WebProxyPort); }
					catch (Exception ex) { Log.Exception(this, ex, "Failed to create web proxy for {0}:{1}".Fmt(m_settings.WebProxyHost, m_settings.WebProxyPort)); }
				}
				return proxy;
			}
		}

		/// <summary>Check a remote server for the latest version information</summary>
		private void CheckForUpdates(bool show_dialog)
		{
			// The callback to call when the check for updates results are in
			Action<IAsyncResult> callback = ar =>
				{
					INet.CheckForUpdateResult res = null; Exception err = null;
					try { res = INet.EndCheckForUpdate(ar); }
					catch (OperationCanceledException) {}
					catch (Exception ex) { err = ex; }

					this.BeginInvoke(() => HandleCheckForUpdateResult(res, err, show_dialog));
				};

			string update_url = m_settings.CheckForUpdatesServer + "versions/rylogviewer.xml";

			// Start the check for updates
			if (show_dialog)
			{
				var dg = new ProgressForm("Checking for Updates", "Querying the server for latest version information...", null, ProgressBarStyle.Marquee, (s,a,cb)=>
					{
						cb(new ProgressForm.UserState{ProgressBarStyle = ProgressBarStyle.Marquee, Icon = Icon});
						IAsyncResult async = INet.BeginCheckForUpdate(Constants.AppIdentifier, update_url, null, Proxy);

						// Wait till the operation completes, or until cancel is singled
						for (;!s.CancelPending && !async.AsyncWaitHandle.WaitOne(500);) {}

						if (!s.CancelPending) callback(async);
						else INet.CancelCheckForUpdate(async);
					});
				dg.ShowDialog(this);
			}
			else
			{
				// Start the asynchronous check for updates
				INet.BeginCheckForUpdate(Constants.AppIdentifier, update_url, callback, Proxy);
			}
		}

		/// <summary>Handle the results of a check for updates</summary>
		private void HandleCheckForUpdateResult(INet.CheckForUpdateResult res, Exception error, bool show_dialog)
		{
			if (error != null)
			{
				SetTransientStatusMessage("Check for updates error", Color.Red, SystemColors.Control);
				if (show_dialog) Misc.ShowErrorMessage(this, error, "Check for updates failed", "Check for Updates");
			}
			else
			{
				Version this_version, othr_version;
				try
				{
					this_version = Assembly.GetExecutingAssembly().GetName().Version;
					othr_version = new Version(res.Version);
				}
				catch (Exception)
				{
					SetTransientStatusMessage("Version Information Unavailable");
					if (show_dialog) MessageBox.Show(this, "The server was contacted but version information was not available", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
				if (this_version.CompareTo(othr_version) >  0)
				{
					SetTransientStatusMessage("Development version running");
					if (show_dialog) MessageBox.Show(this, "This version is newer than the latest version", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else if (this_version.CompareTo(othr_version) == 0)
				{
					SetTransientStatusMessage("Latest version running");
					if (show_dialog) MessageBox.Show(this, "This is the latest version", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					SetTransientStatusMessage("New Version Available!", Color.Green, SystemColors.Control);
					if (show_dialog)
					{
						var dg = new NewVersionForm(this_version.ToString(), othr_version.ToString(), res.InfoURL, res.DownloadURL, Proxy);
						dg.ShowDialog(this);
					}
				}
			}
		}

		/// <summary>Show the about dialog</summary>
		private void ShowAbout()
		{
			new About(new Licence(m_startup_options.AppDataDir)).ShowDialog(this);
		}

		/// <summary>Convert an encoding name to encoding object</summary>
		private Encoding GetEncoding(string enc_name)
		{
			// If 'enc_name' is empty, this is the auto detect case, in which case we don't
			// modify it from what it's already set to. Auto detect will set it on file load.
			if (enc_name.Length == 0) return m_encoding ?? Encoding.UTF8;
			if (enc_name == Encoding.ASCII           .EncodingName) return Encoding.ASCII           ;
			if (enc_name == Encoding.UTF8            .EncodingName) return Encoding.UTF8            ;
			if (enc_name == Encoding.Unicode         .EncodingName) return Encoding.Unicode         ;
			if (enc_name == Encoding.BigEndianUnicode.EncodingName) return Encoding.BigEndianUnicode;
			throw new NotSupportedException("Text file encoding '"+enc_name+"' is not supported");
		}

		/// <summary>Convert a row delimiter string into an encoded byte array</summary>
		private byte[] GetRowDelim(string row_delim)
		{
			// If 'row_delim' is empty, this is the auto detect case, in which case we don't
			// modify it from what it's already set to. Auto detect will set it on file load.
			if (row_delim.Length == 0) return m_row_delim ?? m_encoding.GetBytes("\n");
			return m_encoding.GetBytes(Misc.Robitise(row_delim));
		}

		/// <summary>Convert a column delimiter string into an encoded byte array</summary>
		private byte[] GetColDelim(string col_delim)
		{
			// If 'col_delim' is empty, then there is no column delimiter.
			return m_encoding.GetBytes(Misc.Robitise(col_delim));
		}

		/// <summary>Select the row in the file that contains the byte offset 'addr'</summary>
		private void SelectRowByAddr(long addr)
		{
			// If 'addr' is within the currently loaded range, select the row now
			if (LineIndexRange.Contains(addr))
			{
				SelectedRowIndex = LineIndex(m_line_index, addr);
			}
			// Otherwise, load the range around 'addr', then select the row
			else
			{
				BuildLineIndex(addr, false, ()=> SelectedRowIndex = LineIndex(m_line_index, addr));
			}
		}

		/// <summary>
		/// Get/Set the currently selected grid row. Get returns -1 if there are no rows in the grid.
		/// Setting the selected row clamps to the range [0,RowCount) and makes it visible in the grid (if possible)</summary>
		private int SelectedRowIndex
		{
			get
			{
				// If the current row is selected, return that
				var row = m_grid.CurrentRow;
				if (row != null && row.Selected) return row.Index;
				return -1;
			}
			set
			{
				using (m_suspend_grid_events.Refer)
				{
					value = m_grid.SelectRow(value);
					Log.Info(this, "Row {0} selected".Fmt(value));
					if (m_grid.RowCount != 0 && value != -1)
						UpdateStatus();
				}
			}
		}

		/// <summary>Returns the number of selected rows</summary>
		private int SelectedRowCount
		{
			get { return m_grid.Rows.GetRowCount(DataGridViewElementStates.Selected); }
		}

		/// <summary>Returns the byte offset of the selected row, or 0 if there is no selection</summary>
		private Range SelectedRowByteRange
		{
			get
			{
				var idx = SelectedRowIndex;
				if (idx == -1) idx = 0;
				Debug.Assert(idx >= 0 && idx < m_line_index.Count, "SelectedRowByteOffset should not be called when there are no lines");
				return m_line_index[idx];
			}
		}

		/// <summary>Return true if we should auto scroll</summary>
		private bool AutoScrollTail
		{
			get { return m_tail_enabled; }
		}

		/// <summary>Scroll the grid to make the last row visible</summary>
		private void ShowLastRow()
		{
			if (m_grid.RowCount == 0) return;
			int displayed_rows = m_grid.DisplayedRowCount(false);
			int first_row = Math.Max(0, m_grid.RowCount - displayed_rows);
			m_grid.FirstDisplayedScrollingRowIndex = first_row;
			Log.Info(this, "Showing last row. First({0}) + Displayed({1}) = {2}. RowCount = {3}".Fmt(first_row, displayed_rows, first_row + displayed_rows, m_grid.RowCount));
		}

		/// <summary>Returns true if grid event handlers should process grid events</summary>
		private bool GridEventsBlocked
		{
			get { return m_suspend_grid_events.Count != 0; }
		}

		/// <summary>Helper for setting the grid row count without event handlers being fired</summary>
		private void SetGridRowCount(int count, int row_delta)
		{
			bool auto_scroll_tail = AutoScrollTail;
			if (m_grid.RowCount != count || row_delta != 0)
			{
				// Record data so that we can preserve the selected rows and first visible rows
				int first_vis = m_grid.FirstDisplayedScrollingRowIndex;
				var selected = SelectedRowIndex;
				var selected_rows = m_grid.SelectedRows.Cast<DataGridViewRow>().Select(x => x.Index + row_delta).OrderBy(x => x).ToList();
				SelectedRowIndex = -1;
				
				Log.Info(this, "RowCount changed. Row delta {0}.".Fmt(row_delta));
				m_grid.RowCount = 0;
				m_grid.RowCount = count;
				
				// Restore the selected row, and the first visible row
				if (count != 0)
				{
					// Restore the selected rows, and the first visible row
					if (first_vis != -1) m_grid.FirstDisplayedScrollingRowIndex = Maths.Clamp(first_vis + row_delta, 0, m_grid.RowCount - 1);
					if (auto_scroll_tail) SelectedRowIndex = m_grid.RowCount - 1;//m_grid.SelectRow(m_grid.RowCount - 1);
					else if (selected != -1)
					{
						m_grid.SelectRow(selected + row_delta);

						int i = selected_rows.BinarySearch(x => x.CompareTo(0));
						for (i = (i >= 0) ? i : ~i; i != selected_rows.Count; ++i)
							m_grid.Rows[i].Selected = true;
					}
				}
			}
			if (auto_scroll_tail) ShowLastRow();
		}

		/// <summary>Helper for setting the grid column size based on currently displayed content</summary>
		private void SetGridColumnSizes()
		{
			if (m_grid.ColumnCount == 0) return;
			m_batch_set_col_size.Signal();
		}
		private void SetGridColumnSizesImpl()
		{
			int grid_width = m_grid.DisplayRectangle.Width - 2;
			
			// Measure each column's preferred width
			int[] col_widths = new int[m_grid.ColumnCount];
			int total_width = 0;
			foreach (DataGridViewColumn col in m_grid.Columns)
				total_width += col_widths[col.Index] = col.GetPreferredWidth(DataGridViewAutoSizeColumnMode.DisplayedCells, true);
			
			// Resize columns. If the total width is less than the control width use the control width instead
			float scale = Maths.Max((float)grid_width / total_width, 1f);
			foreach (DataGridViewColumn col in m_grid.Columns)
				col.Width = (int)(col_widths[col.Index] * scale);
		}

		/// <summary>
		/// Apply settings throughout the app.
		/// This method is called on startup to apply initial settings and
		/// after the settings dialog has been shown and closed. It needs to
		/// update anything that is only changed in the settings.
		/// Note: it doesn't trigger a file reload.</summary>
		private void ApplySettings()
		{
			Log.Info(this, "Applying settings");
			
			// Cached settings for performance, don't overwrite auto detected cached values though
			m_encoding                 = GetEncoding(m_settings.Encoding);
			m_row_delim                = GetRowDelim(m_settings.RowDelimiter);
			m_col_delim                = GetColDelim(m_settings.ColDelimiter);
			m_row_height               = m_settings.RowHeight;
			m_bufsize                  = m_settings.FileBufSize;
			m_line_cache_count         = m_settings.LineCacheCount;
			m_alternating_line_colours = m_settings.AlternateLineColours;
			m_tail_enabled             = m_settings.TailEnabled;
			m_quick_filter_enabled     = m_settings.QuickFilterEnabled;
			
			// Tail
			m_watch_timer.Enabled = FileOpen && m_settings.WatchEnabled;
			
			// Highlights;
			m_highlights.Clear();
			if (m_settings.HighlightsEnabled)
				m_highlights.AddRange(Highlight.Import(m_settings.HighlightPatterns).Where(x => x.Active));
			
			// Filters
			m_filters.Clear();
			if (m_settings.FiltersEnabled)
				m_filters.AddRange(Filter.Import(m_settings.FilterPatterns).Where(x => x.Active));
			
			// Transforms
			m_transforms.Clear();
			if (m_settings.TransformsEnabled)
				m_transforms.AddRange(Transform.Import(m_settings.TransformPatterns).Where(x => x.Active));
			
			// Click Actions
			m_clkactions.Clear();
			if (m_settings.ActionsEnabled)
				m_clkactions.AddRange(ClkAction.Import(m_settings.ActionPatterns).Where(x => x.Active));
			
			// Grid
			int col_count = m_settings.ColDelimiter.Length != 0 ? Maths.Clamp(m_settings.ColumnCount, 1, 255) : 1;
			m_grid.ColumnHeadersVisible = col_count > 1;
			m_grid.ColumnCount = col_count;
			m_grid.RowsDefaultCellStyle = new DataGridViewCellStyle
			{
				Font = m_settings.Font,
				ForeColor = m_settings.LineForeColour1,
				BackColor = m_settings.LineBackColour1,
				SelectionBackColor = m_settings.LineSelectBackColour,
				SelectionForeColor = m_settings.LineSelectForeColour,
			};
			if (m_settings.AlternateLineColours)
			{
				m_grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
				{
					Font = m_settings.Font,
					BackColor = m_settings.LineBackColour2,
					SelectionBackColor = m_settings.LineSelectBackColour,
					SelectionForeColor = m_settings.LineSelectForeColour,
				};
			}
			else
			{
				m_grid.AlternatingRowsDefaultCellStyle = m_grid.RowsDefaultCellStyle;
			}
			m_grid.DefaultCellStyle.SelectionBackColor = m_settings.LineSelectBackColour;
			m_grid.DefaultCellStyle.SelectionForeColor = m_settings.LineSelectForeColour;

			// File scroll
			m_scroll_file.TrackColor = m_settings.ScrollBarFileRangeColour;
			m_scroll_file.ThumbColor = m_settings.ScrollBarCachedRangeColour;

			// Ensure rows are re-rendered
			InvalidateCache();
			UpdateUI();
		}
		
		/// <summary>
		/// Update the UI with the current line index.
		/// This method should be called whenever a changes occurs that requires
		/// UI elements to be updated/redrawn. Note: it doesn't trigger a file reload.</summary>
		private void UpdateUI(int row_delta = 0)
		{
			if (m_in_update_ui) return;
			try
			{
				m_in_update_ui = true;
				Log.Info(this, "UpdateUI. Row delta {0}".Fmt(row_delta));
			
				// Don't suspend events by removing/adding handlers because that pattern doesn't nest
				using (m_grid.SuspendLayout(true))
				{
					// Configure the grid
					if (m_line_index.Count != 0)
					{
						// Give the illusion that the alternating row colour is moving with the overall file
						if ((Math.Abs(row_delta) & 1) == 1)
							m_first_row_is_odd = !m_first_row_is_odd;
						
						// Ensure the grid has the correct number of rows
						using (m_suspend_grid_events.Refer)
							SetGridRowCount(m_line_index.Count, row_delta);
					
						SetGridColumnSizes();
					}
					else
					{
						m_grid.ColumnHeadersVisible = false;
						m_grid.ColumnCount = 1;
						using (m_suspend_grid_events.Refer)
							SetGridRowCount(0, 0);
					}
				}
				m_grid.Refresh();
			
				// Configure menus
				bool file_open                            = FileOpen;
				string enc                                = m_settings.Encoding;
				string row_delim                          = m_settings.RowDelimiter;
				m_menu_file_export.Enabled                = file_open;
				m_menu_file_close.Enabled                 = file_open;
				m_menu_edit_selectall.Enabled             = file_open;
				m_menu_edit_copy.Enabled                  = file_open;
				m_menu_edit_find.Enabled                  = file_open;
				m_menu_edit_find_next.Enabled             = file_open;
				m_menu_edit_find_prev.Enabled             = file_open;
				m_menu_edit_toggle_bookmark.Enabled       = file_open;
				m_menu_edit_next_bookmark.Enabled         = file_open;
				m_menu_edit_prev_bookmark.Enabled         = file_open;
				m_menu_edit_clearall_bookmarks.Enabled    = file_open;
				m_menu_edit_bookmarks.Enabled             = file_open;
				m_menu_encoding_detect           .Checked = enc.Length == 0;
				m_menu_encoding_ascii            .Checked = enc == Encoding.ASCII.EncodingName;
				m_menu_encoding_utf8             .Checked = enc == Encoding.UTF8.EncodingName;
				m_menu_encoding_ucs2_littleendian.Checked = enc == Encoding.Unicode.EncodingName;
				m_menu_encoding_ucs2_bigendian   .Checked = enc == Encoding.BigEndianUnicode.EncodingName;
				m_menu_line_ending_detect        .Checked = row_delim.Length == 0;
				m_menu_line_ending_cr            .Checked = row_delim == "<CR>";
				m_menu_line_ending_crlf          .Checked = row_delim == "<CR><LF>";
				m_menu_line_ending_lf            .Checked = row_delim == "<LF>";
				m_menu_line_ending_custom        .Checked = row_delim.Length != 0 && row_delim != "<CR>" && row_delim != "<CR><LF>" && row_delim != "<LF>";
				m_menu_tools_clear_log_file.Enabled                = FileOpen;
			
				// Toolbar
				m_btn_quick_filter.Checked = m_settings.QuickFilterEnabled;
				m_btn_highlights.Checked   = m_settings.HighlightsEnabled;
				m_btn_filters.Checked      = m_settings.FiltersEnabled;
				m_btn_transforms.Checked   = m_settings.TransformsEnabled;
				m_btn_actions.Checked      = m_settings.ActionsEnabled;
				m_btn_tail.Checked         = m_settings.TailEnabled;
				m_btn_watch.Checked        = m_watch_timer.Enabled;
				m_btn_additive.Checked     = m_settings.FileChangesAdditive;
			
				// Status and title
				UpdateStatus();
			}
			finally
			{
				m_in_update_ui = false;
			}
		}
		private bool m_in_update_ui;
		
		/// <summary>Update the status bar</summary>
		private void UpdateStatus()
		{
			Debug.Assert(m_grid.RowCount == m_line_index.Count, "The grid is not up to date");
			if (!FileOpen)
			{
				Text = Resources.AppTitle;
				m_status_spring.Text      = Resources.NoFile;
				m_status_filesize.Visible = false;
				m_status_line_end.Visible = false;
				m_status_encoding.Visible = false;
			}
			else
			{
				Text = "{0} - {1}".Fmt(m_file.Name,Resources.AppTitle);
				m_status_spring.Text = "";
				
				// Add comma's to a large number
				Func<StringBuilder,StringBuilder> pretty = sb=>
					{
						for (int i = sb.Length, j = 0; i-- != 0; ++j)
							if ((j%3) == 2 && i != 0) sb.Insert(i, ',');
						return sb;
					};

				// Get current file position
				int r = SelectedRowIndex;
				long p = (r != -1) ? m_line_index[r].Begin : 0;
				StringBuilder pos = pretty(new StringBuilder(p.ToString(CultureInfo.InvariantCulture)));
				StringBuilder len = pretty(new StringBuilder(m_file.Stream.Length.ToString(CultureInfo.InvariantCulture)));
				
				m_status_filesize.Text = string.Format(Resources.PositionXofYBytes, pos, len);
				m_status_filesize.Visible = true;
				m_status_line_end.Text = string.Format(Resources.LineEndingX, m_row_delim == null ? "unknown" : Misc.Humanise(m_encoding.GetString(m_row_delim)));
				m_status_line_end.Visible = true;
				m_status_encoding.Text = string.Format(Resources.EncodingX, m_encoding.EncodingName);
				m_status_encoding.Visible = true;
			}
			
			UpdateFileScroll();
		}
		
		/// <summary>Update the status bar progress bar</summary>
		private void UpdateStatusProgress(long current, long total)
		{
			if (current == total)
			{
				m_status_progress.Visible = false;
				m_status_progress.Value = m_status_progress.Maximum;
			}
			else
			{
				m_status_progress.Visible = true;
				m_status_progress.Value = (int)(Maths.Frac(0, current, total) * 100);
			}
		}

		/// <summary>Update the indicator ranges on the file scroll bar</summary>
		private void UpdateFileScroll()
		{
			Range range = LineIndexRange;
			if (!range.Equals(m_scroll_file.ThumbRange))
				Log.Info(this, "File scroll set to [{0},{1}) within file [{2},{3})".Fmt(range.Begin, range.End, FileByteRange.Begin, FileByteRange.End));

			m_scroll_file.Visible    = true;
			m_scroll_file.TotalRange = FileByteRange;
			m_scroll_file.ThumbRange = range;
			m_scroll_file.Width      = m_settings.FileScrollWidth;

			m_scroll_file.Ranges.Clear();
			m_scroll_file.Ranges.Add(new SubRangeScroll.SubRange(DisplayedRowsRange, m_settings.ScrollBarDisplayRangeColour));
			foreach (var sel_range in SelectedRowRanges)
				m_scroll_file.Ranges.Add(new SubRangeScroll.SubRange(sel_range, m_settings.LineSelectBackColour));

			// Add marks for the bookmarked positions
			var bkmark_colour = m_settings.BookmarkColour;
			foreach (var bk in m_bookmarks)
				m_scroll_file.Ranges.Add(new SubRangeScroll.SubRange(bk.Range, bkmark_colour));

			m_scroll_file.Refresh();
		}

		/// <summary>Create a message that displays for a period then disappears. Use null or "" to hide the status</summary>
		private void SetTransientStatusMessage(string text, Color frcol, Color bkcol, TimeSpan display_time_ms)
		{
			m_status_message_trans.Text = text ?? string.Empty;
			m_status_message_trans.Visible = text.HasValue();
			m_status_message_trans.ForeColor = frcol;
			m_status_message_trans.BackColor = bkcol;

			// If the status message has a timer already, dispose it
			Timer timer = m_status_message_trans.Tag as Timer;
			if (timer != null) timer.Dispose();

			// Attach a new timer to the status message
			if (text.HasValue() && display_time_ms != TimeSpan.MaxValue)
			{
				m_status_message_trans.Tag = timer = new Timer{Enabled = true, Interval = (int)display_time_ms.TotalMilliseconds};
				timer.Tick += (s,a)=>
					{
						// When the timer fires, if we're still associated with
						// the status message, null out the text and remove our self
						if (s != m_status_message_trans.Tag) return;
						m_status_message_trans.Text = Resources.Idle;
						m_status_message_trans.Visible = false;
						m_status_message_trans.Tag = null;
						((Timer)s).Dispose();
					};
			}
		}
		private void SetTransientStatusMessage(string text, Color frcol, Color bkcol)
		{
			SetTransientStatusMessage(text, frcol, bkcol, TimeSpan.FromSeconds(2));
		}
		private void SetTransientStatusMessage(string text)
		{
			SetTransientStatusMessage(text, SystemColors.ControlText, SystemColors.Control);
		}

		/// <summary>Create a status message that displays until cleared. Use null or "" to hide the status</summary>
		private void SetStaticStatusMessage(string text, Color frcol, Color bkcol)
		{
			m_status_message_fixed.Text = text ?? string.Empty;
			m_status_message_fixed.Visible = text.HasValue();
			m_status_message_fixed.ForeColor = frcol;
			m_status_message_fixed.BackColor = bkcol;
		}
		private void SetStaticStatusMessage(string text)
		{
			SetStaticStatusMessage(text, SystemColors.ControlText, SystemColors.Control);
		}

		/// <summary>Display the hint balloon</summary>
		private void ShowHintBalloon(string msg, ToolStripItem item)
		{
			var parent = item.GetCurrentParent();
			if (parent == null) return;
			var pt = item.Bounds.Location;
			pt.Offset(-2,-32);
			m_balloon.Show(msg, parent, pt, 2000);
		}

		/// <summary>Custom button renderer because the office 'checked' state buttons look crap</summary>
		private class CheckedButtonRenderer :ToolStripProfessionalRenderer
		{
			protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
			{
				ToolStripButton btn = e.Item as ToolStripButton;
				if (btn == null || !btn.Checked) { base.OnRenderButtonBackground(e); return; }
				Rectangle r = Rectangle.Inflate(e.Item.ContentRectangle, 2, 2);
				
				VisualStyleRenderer style = btn.Selected
					? new VisualStyleRenderer(VisualStyleElement.Button.PushButton.Hot)
					: new VisualStyleRenderer(VisualStyleElement.Button.PushButton.Pressed);
				
				style.DrawBackground(e.Graphics, r);
				style.DrawEdge(e.Graphics, r, Edges.Left|Edges.Top|Edges.Right|Edges.Bottom ,EdgeStyle.Etched, EdgeEffects.Mono);
			}
		}
	}
}
