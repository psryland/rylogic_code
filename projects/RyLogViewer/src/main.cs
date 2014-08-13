using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gui;
using pr.inet;
using pr.maths;
using pr.util;
using RyLogViewer.Properties;
using Timer = System.Windows.Forms.Timer;

namespace RyLogViewer
{
	public sealed partial class Main :Form ,IMainUI
	{
		private readonly StartupOptions m_startup_options;  // The options provided at startup
		private RecentFiles m_recent;              // Recent files
		private readonly FileWatch m_watch;                 // A helper for watching files
		private readonly Timer m_watch_timer;               // A timer for polling the file watcher
		private readonly EventBatcher m_batch_set_col_size; // A call batcher for setting the column widths
		private readonly List<Highlight> m_highlights;      // A list of the active highlights only
		private readonly List<Filter> m_filters;            // A list of the active filters only
		private readonly List<Transform> m_transforms;      // A list of the active transforms only
		private readonly List<ClkAction> m_clkactions;      // A list of the active click actions only
		private readonly FindUI m_find_ui;                  // The find dialog
		private readonly BookmarksUI m_bookmarks_ui;        // The bookmarks dialog
		private readonly NotifyIcon m_notify_icon;          // A system tray icon
		private ToolTip m_tt;                               // Tooltips
		private readonly Form[] m_tab_cycle;                // The forms that Ctrl+Tab cycles through
		private readonly RefCount m_suspend_grid_events;    // A ref count of nested calls that tell event handlers to ignore grid events
		private List<Range> m_line_index;                   // Byte offsets (from file begin) to the byte range of a line
		private Encoding m_encoding;                        // The file encoding
		private IFileSource m_file;                         // A file stream source
		private Licence m_license;                          // License data
		private byte[] m_row_delim;                         // The row delimiter converted from a string to a byte[] using the current encoding
		private byte[] m_col_delim;                         // The column delimiter, cached to prevent m_settings access in CellNeeded
		private int m_row_height;                           // The row height, cached to prevent settings lookups in CellNeeded
		private long m_filepos;                             // The byte offset (from file begin) to the start of the last known line
		private long m_fileend;                             // The last known size of the file
		private long m_bufsize;                             // Cached value of m_settings.FileBufSize
		private int m_line_cache_count;                     // The number of lines to scan about the currently selected row
		private bool m_alternating_line_colours;            // Cache the alternating line colours setting for performance
		private bool m_tail_enabled;                        // Cache whether tail mode is enabled
		private bool m_quick_filter_enabled;                // True if only rows with highlights should be displayed
		private bool m_first_row_is_odd;                    // Tracks whether the first row is odd or even for alternating row colours (not 100% accurate)
		private StringFormat m_strfmt;                      // Caches the tab stop sizes for rendering

		public Main(StartupOptions startup_options)
		{
			Log.Register(startup_options.LogFilePath, false);
			Log.Info(this, "App Startup: {0}".Fmt(DateTime.Now));

			m_startup_options     = startup_options;
			m_license             = new Licence(m_startup_options.AppDataDir);
			m_watch               = new FileWatch();
			m_watch_timer         = new Timer{Interval = Constants.FilePollingRate};
			m_batch_set_col_size  = new EventBatcher(SetGridColumnSizesImpl, TimeSpan.FromMilliseconds(100));
			m_highlights          = new List<Highlight>();
			m_filters             = new List<Filter>();
			m_transforms          = new List<Transform>();
			m_clkactions          = new List<ClkAction>();
			m_find_history        = new BindingSource{DataSource = new BindingList<Pattern>()};
			m_find_ui             = new FindUI(this, m_find_history){Visible = false};
			m_bookmarks           = new BindingList<Bookmark>();
			m_bs_bookmarks        = new BindingSource{DataSource = m_bookmarks};
			m_batch_refresh_bkmks = new EventBatcher(TimeSpan.FromMilliseconds(100));
			m_bookmarks_ui        = new BookmarksUI(this, m_bs_bookmarks){Visible = false};
			m_tt                  = new ToolTip();
			m_tab_cycle           = new Form[]{this, m_find_ui, m_bookmarks_ui};
			m_notify_icon         = new NotifyIcon{Icon = Icon};
			m_suspend_grid_events = new RefCount();
			m_line_index          = new List<Range>();
			m_file                = null;
			m_filepos             = 0;
			m_fileend             = 0;

			InitializeComponent();

			Settings              = new Settings(m_startup_options.SettingsPath){AutoSaveOnChanges = true};
			m_bufsize             = Settings.FileBufSize;
			m_line_cache_count    = Settings.LineCacheCount;
			m_tail_enabled        = Settings.TailEnabled;

			// Setup the UI
			SetupMenu();
			SetupToolbar();
			SetupScrollbar();
			SetupStatus();
			SetupGrid();
			SetupFileWatch();
			SetupFind();
			SetupBookmarks();

			InitCache();
			ApplySettings();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_tt);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>App settings</summary>
		public Settings Settings
		{
			get { return m_impl_settings; }
			set
			{
				if (m_impl_settings == value) return;
				if (m_impl_settings != null)
				{
					m_impl_settings.SettingChanged -= HandleSettingsChanged;
				}
				m_impl_settings = value;
				if (m_impl_settings != null)
				{
					m_impl_settings.SettingChanged += HandleSettingsChanged;
					ApplySettings();
				}
			}
		}
		private Settings m_impl_settings;
		private void HandleSettingsChanged(object sender, SettingChangedEventArgs args)
		{
			Log.Info(this, "Setting {0} changed from {1} to {2}".Fmt(args.Key,args.OldValue,args.NewValue));
		}

		/// <summary>The main UI as a form for use as the parent of child dialogs</summary>
		public Form MainWindow { get { return this; } }

		/// <summary>The currently loaded file source</summary>
		public IFileSource FileSource { get { return m_file; } }

		/// <summary>Setup menu options</summary>
		private void SetupMenu()
		{
			m_menu.Location = Point.Empty;
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
			m_menu_edit_selectall.Click                += (s,a) => DataGridViewExtensions.SelectAll(m_grid, new KeyEventArgs(Keys.Control|Keys.A));
			m_menu_edit_copy.Click                     += (s,a) => DataGridViewExtensions.Copy(m_grid, new KeyEventArgs(Keys.Control|Keys.C));
			m_menu_edit_jumpto.Click                   += (s,a) => JumpTo();
			m_menu_edit_find.Click                     += (s,a) => ShowFindDialog();
			m_menu_edit_find_next.Click                += (s,a) => FindNext(false);
			m_menu_edit_find_prev.Click                += (s,a) => FindPrev(false);
			m_menu_edit_toggle_bookmark.Click          += (s,a) => SetBookmark(SelectedRowIndex, Bit.EState.Toggle);
			m_menu_edit_next_bookmark.Click            += (s,a) => NextBookmark();
			m_menu_edit_prev_bookmark.Click            += (s,a) => PrevBookmark();
			m_menu_edit_clearall_bookmarks.Click       += (s,a) => ClearAllBookmarks();
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
			m_menu_tools_alwaysontop.Click             += (s,a) => SetAlwaysOnTop(!Settings.AlwaysOnTop);
			m_menu_tools_monitor_mode.Click            += (s,a) => EnableMonitorMode(!m_menu_tools_monitor_mode.Checked);
			m_menu_tools_clear_log_file.Click          += (s,a) => ClearLogFile();
			m_menu_tools_highlights.Click              += (s,a) => ShowOptions(SettingsUI.ETab.Highlights);
			m_menu_tools_filters.Click                 += (s,a) => ShowOptions(SettingsUI.ETab.Filters   );
			m_menu_tools_transforms.Click              += (s,a) => ShowOptions(SettingsUI.ETab.Transforms);
			m_menu_tools_actions.Click                 += (s,a) => ShowOptions(SettingsUI.ETab.Actions   );
			m_menu_tools_options.Click                 += (s,a) => ShowOptions(SettingsUI.ETab.General   );
			m_menu_help_view_help.Click                += (s,a) => ShowHelp();
			m_menu_help_firstruntutorial.Click         += (s,a) => ShowFirstRunTutorial();
			m_menu_help_totd.Click                     += (s,a) => ShowTotD();
			m_menu_help_visit_store.Click              += (s,a) => VisitStore();
			m_menu_help_register.Click                 += (s,a) => ShowActivation();
			m_menu_help_check_for_updates.Click        += (s,a) => CheckForUpdates(true);
			m_menu_help_about.Click                    += (s,a) => ShowAbout();
			m_menu_free_version.Click                  += ShowFreeVersionInfo;

			// Recent files menu
			m_recent = new RecentFiles(m_menu_file_recent, fp => OpenSingleLogFile(fp, true));
			m_recent.Import(Settings.RecentFiles);
		}

		/// <summary>Setup the toolbar</summary>
		private void SetupToolbar()
		{
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
			m_btn_jump_to_start.Click      += (s,a) => JumpToStart();
			m_btn_jump_to_end.ToolTipText   = "Selected the last row in the log file";
			m_btn_jump_to_end.Click        += (s,a) => JumpToEnd();
			m_btn_tail.ToolTipText          = Resources.WatchTail;
			m_btn_tail.Click               += (s,a) => EnableTail(m_btn_tail.Checked);
			m_btn_watch.ToolTipText         = Resources.WatchForUpdates;
			m_btn_watch.Click              += (s,a) => EnableWatch(m_btn_watch.Checked);
			m_btn_additive.ToolTipText      = Resources.AdditiveMode;
			m_btn_additive.Click           += (s,a) => EnableAdditive(m_btn_additive.Checked);
			ToolStripManager.Renderer       = new CheckedButtonRenderer();
		}

		/// <summary>Setup the side scrollbar</summary>
		private void SetupScrollbar()
		{
			m_scroll_file.ToolTip(m_tt, "Indicates the currently cached position in the log file\r\nClicking within here moves the cached position within the log file");
			m_scroll_file.MinThumbSize = 1;
			m_scroll_file.ScrollEnd += OnScrollFileScrollEnd;
		}

		/// <summary>Setup the status bar</summary>
		private void SetupStatus()
		{
			m_status.Location             = Point.Empty;
			m_status_progress.ToolTipText = "Press escape to cancel";
			m_status_progress.Minimum     = 0;
			m_status_progress.Maximum     = 100;
			m_status_progress.Visible     = false;
			m_status_progress.Text        = "Test";
		}

		/// <summary>Setup the main grid</summary>
		private void SetupGrid()
		{
			m_grid.RowCount                  = 0;
			m_grid.AutoGenerateColumns       = false;
			m_grid.KeyDown                  += DataGridViewExtensions.SelectAll;
			m_grid.KeyDown                  += DataGridViewExtensions.Copy;
			m_grid.KeyDown                  += GridKeyDown;
			m_grid.MouseUp                  += (s,a) => GridMouseButton(a, false);
			m_grid.MouseDown                += (s,a) => GridMouseButton(a, true);
			m_grid.CellValueNeeded          += CellValueNeeded;
			m_grid.RowPrePaint              += RowPrePaint;
			m_grid.SelectionChanged         += GridSelectionChanged;
			m_grid.CellDoubleClick          += CellDoubleClick;
			m_grid.ColumnDividerDoubleClick += (s,a) => { SetGridColumnSizesImpl(); a.Handled = true; };
			m_grid.RowHeightInfoNeeded      += RowHeightNeeded;
			m_grid.DataError                += (s,a) => Debug.Assert(false);
			m_grid.Scroll                   += (s,a) => GridScroll();

			// Grid context menu
			m_cmenu_grid.ItemClicked    += GridContextMenu;
			m_cmenu_grid.VisibleChanged += SetGridContextMenuVisibility;
		}

		/// <summary>Setup the file watcher</summary>
		private void SetupFileWatch()
		{
			m_watch_timer.Tick += (s,a)=>
				{
					if (ReloadInProgress) return;
					if (WindowState == FormWindowState.Minimized) return;
					try { m_watch.CheckForChangedFiles(); }
					catch (Exception ex) { Log.Exception(this, ex, "CheckForChangedFiles failed"); }
				};
		}

		// Handlers
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			Startup();
		}
		protected override void OnSizeChanged(EventArgs e)
		{
			// OnSize gets called from InitializeComponents() so we need to handle
			// the line index not being initialised yet.
			base.OnSizeChanged(e);
			if (Settings != null)
				UpdateUI();
		}
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			Shutdown();
		}
		protected override void OnDragEnter(DragEventArgs e)
		{
			base.OnDragEnter(e);
			FileDrop(e, true);
		}
		protected override void OnDragDrop(DragEventArgs e)
		{
			base.OnDragDrop(e);
			FileDrop(e, false);
		}

		/// <summary>Apply the startup options</summary>
		private void ApplyStartupOptions()
		{
			StartupOptions su = m_startup_options;

			AllowTransparency = true;
			if (Settings.RestoreScreenLoc)
			{
				StartPosition = FormStartPosition.Manual;
				Location = Settings.ScreenPosition;
				Size = Settings.WindowSize;
			}

			// If a pattern set file path is given, replace the patterns in 'm_settings'
			// with the contents of the file
			if (su.HighlightSetPath != null)
			{
				try
				{
					var doc = XDocument.Load(su.HighlightSetPath);
					if (doc.Root == null) throw new InvalidDataException("Invalid highlight set, root xml node not found");
					if (doc.Root.Element(XmlTag.Highlight) == null) throw new InvalidDataException("Highlight set file does not contain any highlight descriptions");
					Settings.HighlightPatterns = Highlight.Import(doc.ToString(SaveOptions.None)).ToArray();
				}
				catch (Exception ex)
				{
					Misc.ShowMessage(this, string.Format("Could not load highlight pattern set {0}.", su.HighlightSetPath), Resources.LoadPatternSetFailed, MessageBoxIcon.Error, ex);
				}
			}
			if (su.FilterSetPath != null)
			{
				try
				{
					var doc = XDocument.Load(su.FilterSetPath);
					if (doc.Root == null) throw new InvalidDataException("Invalid filter set, root xml node not found");
					if (doc.Root.Element(XmlTag.Filter) == null) throw new InvalidDataException("Filter set file does not contain any filter descriptions");
					Settings.FilterPatterns = Filter.Import(doc.ToString(SaveOptions.None)).ToArray();
				}
				catch (Exception ex)
				{
					Misc.ShowMessage(this, string.Format("Could not load filter pattern set {0}.", su.FilterSetPath), Resources.LoadPatternSetFailed, MessageBoxIcon.Error, ex);
				}
			}
			if (su.TransformSetPath != null)
			{
				try
				{
					var doc = XDocument.Load(su.TransformSetPath);
					if (doc.Root == null) throw new InvalidDataException("Invalid transform set, root xml node not found");
					if (doc.Root.Element(XmlTag.Transform) == null) throw new InvalidDataException("Transform set file does not contain any transform descriptions");
					Settings.TransformPatterns = Transform.Import(doc.ToString(SaveOptions.None)).ToArray();
				}
				catch (Exception ex)
				{
					Misc.ShowMessage(this, string.Format("Could not load transform pattern set {0}.", su.TransformSetPath), Resources.LoadPatternSetFailed, MessageBoxIcon.Error, ex);
				}
			}
		}

		/// <summary>Called the first time the app is displayed</summary>
		private void Startup()
		{
			StartupOptions su = m_startup_options;

			// Startup options
			ApplyStartupOptions();

			// Look for plugin data sources, called here so the UI is displayed over the main window
			InitCustomDataSources();

			// Parse command line
			if (su.FileToLoad != null)
			{
				OpenSingleLogFile(su.FileToLoad, true);
			}
			else if (Settings.LoadLastFile && PathEx.FileExists(Settings.LastLoadedFile))
			{
				OpenSingleLogFile(Settings.LastLoadedFile, true);
			}

			// Show the first run tutorial
			if (Settings.FirstRun)
			{
				Settings.FirstRun = false;
				const string msg =
					"This appears to be the first time you've run RyLogViewer.\r\n"+
					"Would you like a quick tour?\r\n"+
					"\r\n"+
					"If not, you can always run this tutorial again by selecting it from the Help menu.";
				if (MsgBox.Show(this, msg, "First Run", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
					ShowFirstRunTutorial();
				else
					ShowTotD();
			}

			// Show the TotD if enabled
			else if (Settings.ShowTOTD)
				ShowTotD();

			// Check for updates
			if (Settings.CheckForUpdates)
				CheckForUpdates(false);

			Settings.FirstRun = false;
		}

		/// <summary>Calls as the main form is closing</summary>
		private void Shutdown()
		{
			Settings.ScreenPosition = Location;
			Settings.WindowSize = Size;
			Settings.RecentFiles = m_recent.Export();
		}

		/// <summary>Returns true if there is a log file currently open</summary>
		public bool FileOpen
		{
			get { return m_file != null; }
		}

		/// <summary>Close the current log file</summary>
		public void CloseLogFile()
		{
			m_bookmarks.Clear();

			using (m_suspend_grid_events.Reference)
			{
				CancelBuildLineIndex();
				SelectionChanged = null;
				m_line_index.Clear();
				m_grid.RowCount = 0;
				m_last_hint = EHeuristicHint.None;
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
					MsgBox.Show(this, "File path is invalid", Resources.InvalidFilePath, MessageBoxButtons.OK,MessageBoxIcon.Error);
					return false;
				}

				// Check that the file exists, this can take ages if 'filepath' is a network file
				if (!Misc.FileExists(this, file))
				{
					if (m_recent.IsInRecents(file))
					{
						var res = MsgBox.Show(this, "File path '{0}' is invalid or does not exist\r\n\r\nRemove from recent files list?".Fmt(file), Resources.InvalidFilePath, MessageBoxButtons.YesNo,MessageBoxIcon.Error);
						if (res == DialogResult.Yes)
							m_recent.Remove(file, true);
					}
					else
					{
						MsgBox.Show(this, "File path '{0}' is invalid or does not exist".Fmt(file), Resources.InvalidFilePath, MessageBoxButtons.OK,MessageBoxIcon.Error);
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

			m_filepos = Settings.OpenAtEnd ? m_file.Stream.Length : 0;

			// Setup the watcher to watch for file changes
			m_watch.Add(m_file.Filepaths, (fp,ctx) => { OnFileChanged(); return true; });
			m_watch_timer.Enabled = FileOpen && Settings.WatchEnabled;

			BuildLineIndex(m_filepos, true, ()=>
				{
					SelectedRowIndex = Settings.OpenAtEnd ? m_grid.RowCount - 1 : 0;
					SetGridColumnSizes(true);
				});
		}

		/// <summary>Open a single log file, prompting if 'filepath' is null</summary>
		public void OpenSingleLogFile(string filepath, bool add_to_recent)
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
					Settings.RecentFiles = m_recent.Export();
					Settings.LastLoadedFile = filepath;
				}

				// Switch files - open the file to make sure it's accessible (and to hold a lock)
				NewFileSource(new SingleFile(filepath));
			}
			catch (Exception ex)
			{
				Misc.ShowMessage(this, "Failed to open file {0} due to an error.".Fmt(filepath), Resources.FailedToLoadFile, MessageBoxIcon.Error, ex);
				CloseLogFile();
			}
		}

		/// <summary>Open multiple log files in aggregate</summary>
		public void OpenAggregateLogFile(List<string> filepaths)
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
				Misc.ShowMessage(this, "Failed to open aggregate log files due to an error.", Resources.FailedToLoadFile, MessageBoxIcon.Error, ex);
				CloseLogFile();
			}
		}

		/// <summary>Show the aggregate log file wizard</summary>
		private void AggregateFileWizard()
		{
			var dg = new AggregateFilesUI(this);
			if (dg.ShowDialog(this) != DialogResult.OK) return;

			var filepaths = dg.Filepaths.ToList();
			if (filepaths.Count == 0) return;
			if (filepaths.Count == 1)
				OpenSingleLogFile(filepaths[0], true);
			else
				OpenAggregateLogFile(filepaths);
		}

		/// <summary>Called when the log file is noticed to have changed</summary>
		private void OnFileChanged()
		{
			long len = m_file.Stream.Length;
			Log.Info(this, "File {0} changed. File length: {1}".Fmt(m_file.Name, len));
			long filepos = AutoScrollTail ? m_file.Stream.Length : m_filepos;
			bool reload  = m_file.Stream.Length < m_fileend || !Settings.FileChangesAdditive;
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

		/// <summary>Supply the height needed for the given row</summary>
		private void RowHeightNeeded(object s, DataGridViewRowHeightInfoNeededEventArgs a)
		{
			if (a.RowIndex >= 0 && a.RowIndex < m_line_index.Count)
			{
				var line = ReadLine(a.RowIndex);
				a.Height = Math.Max(m_row_height, (int)(line.TextSize.Height + 0.5f));
			}
			else
			{
				a.Height = m_row_height;
			}
		}

		/// <summary>Returns the cell style to use for a given row index</summary>
		private DataGridViewCellStyle RowCellStyle(int row_index)
		{
			if (!m_alternating_line_colours)
				return m_grid.RowsDefaultCellStyle;

			// Give the illusion that the alternating row colour is moving with the overall file
			return ((row_index & 1) == 1) == m_first_row_is_odd
				? m_grid.RowsDefaultCellStyle
				: m_grid.AlternatingRowsDefaultCellStyle;
		}

		/// <summary>Paint the background for a row</summary>
		private void PaintRowBackground(Graphics gfx, int row_index, Line line, Rectangle row_bounds, bool selected)
		{
			var cs = RowCellStyle(row_index);

			// If the line is bookmarked, use the bookmark colour
			if (m_bookmarks.Count != 0 && m_bookmarks.BinarySearch(x => x.Position.CompareTo(line.LineStartAddr)) >= 0)
			{
				using (var b = new SolidBrush(Settings.BookmarkColour))
					gfx.FillRectangle(b, row_bounds);
			}
			else
			{
				// Paint the whole row background
				using (var b = new SolidBrush(cs.BackColor))
					gfx.FillRectangle(b, row_bounds);

				// Paint each cell
				var cellbounds = row_bounds.Shifted(-m_grid.HorizontalScrollingOffset, 0);
				for (int i = 0, iend = Math.Min(line.Column.Count,m_grid.ColumnCount); i != iend; ++i, cellbounds.X += cellbounds.Width)
				{
					cellbounds.Width = m_grid[i,row_index].Size.Width;

					var col = line.Column[i];

					if (col.HL.Count == 0)
						continue;

					// Paint the highlighting backgrounds
					foreach (var hl in col.HL)
					{
						// Binary match patterns highlight the whole cell
						if (hl.BinaryMatch)
						{
							using (var b = new SolidBrush(hl.BackColour))
								gfx.FillRectangle(b, cellbounds);
						}
							// Otherwise, highlight only the matching parts of the line
						else
						{
							using (gfx.SaveState())
							{
								// Create a clip region for the highlighted parts of the line
								gfx.SetClip(Rectangle.Empty, CombineMode.Replace);
								var fmt = new StringFormat(m_strfmt);
								fmt.SetMeasurableCharacterRanges(hl.Match(col.Text).Select(x => new CharacterRange(x.Begini, x.Sizei)).ToArray());
								foreach (var r in gfx.MeasureCharacterRanges(col.Text, cs.Font, cellbounds, fmt))
								{
									var bnd = r.GetBounds(gfx);
									gfx.SetClip(new RectangleF(bnd.X - 1f, cellbounds.Y, bnd.Width + 1f, cellbounds.Height) , CombineMode.Union);
								}

								// Paint the highlighted parts of the cell
								using (var b = new SolidBrush(hl.BackColour))
									gfx.FillRectangle(b, cellbounds);

								//gfx.SetClip(cellbounds, CombineMode.Xor);

								//// Paint the cell with the background colour
								//using (var b = new SolidBrush(cs.BackColor))
								//	gfx.FillRectangle(b, cellbounds);
							}
						}
					}
				}
			}

			// If the row is selected, alpha blend the selection colour
			if (selected)
			{
				// Fill the selected area in semi-transparent
				using (var b = new SolidBrush(Color.FromArgb(0xC0, cs.SelectionBackColor)))
					gfx.FillRectangle(b, row_bounds);
			}
		}

		/// <summary>Paint the contents of a row</summary>
		private void PaintRowContent(Graphics gfx, int row_index, Line line, Rectangle row_bounds, bool selected)
		{
			var cs = RowCellStyle(row_index);

			var cellbounds = row_bounds.Shifted(-m_grid.HorizontalScrollingOffset, 0);
			for (int i = 0, iend = Math.Min(line.Column.Count, m_grid.ColumnCount); i != iend; ++i, cellbounds.X += cellbounds.Width)
			{
				cellbounds.Width = m_grid[i,row_index].Size.Width;

				var col = line.Column[i];
				var sz = gfx.MeasureString(col.Text, cs.Font);
				var textbounds = new RectangleF(cellbounds.X, cellbounds.Y + Math.Max(0, (cellbounds.Height - sz.Height)/2), cellbounds.Width, sz.Height);

				// If selected, use the selection colour
				if (selected)
				{
					using (var b = new SolidBrush(cs.SelectionForeColor))
						gfx.DrawString(col.Text, cs.Font, b, textbounds, m_strfmt);
				}
				// If no highlights use the default row colour
				else if (col.HL.Count == 0)
				{
					using (var b = new SolidBrush(cs.ForeColor))
						gfx.DrawString(col.Text, cs.Font, b, textbounds, m_strfmt);
				}
				// Paint the highlighted content
				else
				{
					// Only paint the cell content once, using the last highlight
					// This is because overdrawing the text looks shit
					var hl = col.HL.Last();
					{
						// Binary match patterns highlight the whole cell
						if (hl.BinaryMatch)
						{
							using (var b = new SolidBrush(hl.ForeColour))
								gfx.DrawString(col.Text, cs.Font, b, textbounds, m_strfmt);
						}
						// Otherwise, highlight only the matching parts of the line
						else
						{
							using (gfx.SaveState())
							{
								// Create a clip region for the highlighted parts of the line
								gfx.SetClip(Rectangle.Empty, CombineMode.Replace);
								var fmt = new StringFormat(m_strfmt);
								fmt.SetMeasurableCharacterRanges(hl.Match(col.Text).Select(x => new CharacterRange(x.Begini, x.Sizei)).ToArray());
								foreach (var r in gfx.MeasureCharacterRanges(col.Text, cs.Font, textbounds, fmt))
									gfx.SetClip(r.GetBounds(gfx), CombineMode.Union);

								// Paint the highlighted parts of the cell
								using (var b = new SolidBrush(hl.ForeColour))
									gfx.DrawString(col.Text, cs.Font, b, textbounds, m_strfmt);

								gfx.SetClip(cellbounds, CombineMode.Xor);

								// Paint the cell content with the default colour
								using (var b = new SolidBrush(cs.ForeColor))
									gfx.DrawString(col.Text, cs.Font, b, textbounds, m_strfmt);
							}
						}
					}
				}
			}
		}

		/// <summary>Paint row overlays, such as the selection box</summary>
		private void PaintRowOverlay(Graphics gfx, int row_index, Rectangle row_bounds, bool selected)
		{
			var cs = RowCellStyle(row_index);

			// Draw a box around the selection
			if (selected)
			{
				const float pen_width = 3f, pen_hwidth = pen_width*0.5f;
				var fbounds = new RectangleF(row_bounds.Left + pen_hwidth, row_bounds.Top + pen_hwidth, row_bounds.Width - pen_width, row_bounds.Height - pen_width);

				// Draw a border around the selection
				using (var p = new Pen(cs.SelectionBackColor, pen_width){StartCap = LineCap.Square, EndCap = LineCap.Square})
				{
					gfx.DrawLine(p, fbounds.Left , fbounds.Top, fbounds.Left , fbounds.Bottom);
					gfx.DrawLine(p, fbounds.Right, fbounds.Top, fbounds.Right, fbounds.Bottom);
					if (row_index == 0 || !m_grid.Rows[row_index - 1].Selected)
					{
						gfx.DrawLine(p, fbounds.Left, fbounds.Top, fbounds.Right, fbounds.Top);
					}
					if (row_index == m_grid.RowCount - 1 || !m_grid.Rows[row_index + 1].Selected)
					{
						gfx.DrawLine(p, fbounds.Left, fbounds.Bottom, fbounds.Right, fbounds.Bottom);
					}
				}
			}
		}

		/// <summary>Called before drawing the row background</summary>
		private void RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
		{
			e.Handled = false;
			var grid = (DataGridView)sender;
			if (e.RowIndex < 0 || e.RowIndex >= grid.RowCount)
				return;

			// Leave rendering to the grid while events are suspended
			if (GridEventsBlocked || !FileOpen)
				return;

			// Read the line from the cache
			var line = ReadLine(e.RowIndex);
			var bounds = e.RowBounds;
			var selected = (e.State & DataGridViewElementStates.Selected) != 0;

			e.Graphics.CompositingQuality = CompositingQuality.GammaCorrected;
			e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

			PaintRowBackground(e.Graphics, e.RowIndex, line, bounds, selected);
			PaintRowContent(e.Graphics, e.RowIndex, line, bounds, selected);
			PaintRowOverlay(e.Graphics, e.RowIndex, bounds, selected);
			e.Handled = true;
		}

		/// <summary>Handler for selections made in the grid</summary>
		private void GridSelectionChanged(object sender, EventArgs e)
		{
			if (GridEventsBlocked) return;
			if (m_tail_enabled && SelectedRowIndex != m_grid.RowCount - 1)
				EnableTail(false);

			// We need to invalidate the selected rows because of the selection border.
			// Without this bits of the selection border get left behind because the rendering
			// process goes:
			// Select row 2 (say) -> draws top and bottom border because row 1 and 3 aren't selected
			// Select row 3 -> draws row 3 but not row 2 because it hasn't changed (except it needs to
			// because row 3 is now selected so the bottom border should not be draw for row 2).
			foreach (var r in m_grid.GetRowsWithState(DataGridViewElementStates.Displayed|DataGridViewElementStates.Selected))
				m_grid.InvalidateRow(r.Index);

			UpdateStatus();
			CycleColours();

			// Raise an event whenever the selection changes
			RaiseSelectionChanged();
		}

		/// <summary>Handler for cell double clicks</summary>
		private void CellDoubleClick(object sender, DataGridViewCellEventArgs args)
		{
			if (!Settings.ActionsEnabled) return;
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

		/// <summary>Handle key presses on the grid</summary>
		private void GridKeyDown(object s, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
				LoadNearBoundary();
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
			if (args.ClickedItem == m_cmenu_copy      ) { DataGridViewExtensions.Copy(m_grid);      return; }
			if (args.ClickedItem == m_cmenu_select_all) { DataGridViewExtensions.SelectAll(m_grid); return; }
			if (args.ClickedItem == m_cmenu_clear_log ) { ClearLogFile(); return; }

			if (args.ClickedItem == m_cmenu_highlight_row) { ShowOptions(SettingsUI.ETab.Highlights); return; }
			if (args.ClickedItem == m_cmenu_filter_row   ) { ShowOptions(SettingsUI.ETab.Filters   ); return; }
			if (args.ClickedItem == m_cmenu_transform_row) { ShowOptions(SettingsUI.ETab.Transforms); return; }
			if (args.ClickedItem == m_cmenu_action_row   ) { ShowOptions(SettingsUI.ETab.Actions   ); return; }

			// Find operations
			if (args.ClickedItem == m_cmenu_find_next) { m_find_ui.Pattern.Expr = ReadLine(hit.RowIndex).RowText; m_find_ui.RaiseFindNext(false); return; }
			if (args.ClickedItem == m_cmenu_find_prev) { m_find_ui.Pattern.Expr = ReadLine(hit.RowIndex).RowText; m_find_ui.RaiseFindPrev(false); return; }

			// Bookmarks
			if (args.ClickedItem == m_cmenu_toggle_bookmark) { SetBookmark(hit.RowIndex, Bit.EState.Toggle); }
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
			SetGridColumnSizes(false);

			// Selected rows use transparency so we need to invalidate the entire row
			foreach (var r in m_grid.GetRowsWithState(DataGridViewElementStates.Displayed|DataGridViewElementStates.Selected))
				m_grid.InvalidateRow(r.Index);
		}

		/// <summary>Called on the ScrollEnd event for the file scroll indicator</summary>
		private void OnScrollFileScrollEnd(object sender, EventArgs args)
		{
			// Update on ScrollEnd not value changed, since
			// UpdateUI() sets Value when the build is complete.

			// Find the new centre position of the thumb
			var range = m_scroll_file.ThumbRange;
			long pos = (range.Begin == 0) ? 0 : (range.End == m_fileend) ? m_fileend : range.Mid;
			Log.Info(this, "file scroll to {0}".Fmt(pos));

			// Set the new selected row from the mouse up position
			var pt = m_scroll_file.PointToClient(MousePosition);
			var sel_pos = (long)(Maths.Frac(1, pt.Y, m_scroll_file.Height - 1) * FileByteRange.Size);
			BuildLineIndex(pos, false, () => { SelectRowByAddr(sel_pos); });
		}

		/// <summary>Tests whether the currently selected row is near the start or end of the line range and causes a reload if it is</summary>
		private void LoadNearBoundary()
		{
			if (m_grid.RowCount < Constants.AutoScrollAtBoundaryLimit)
				return;
			if (SelectedRowIndex < 0 || SelectedRowIndex >= m_grid.RowCount)
				return;

			const float Limit = 1f / Constants.AutoScrollAtBoundaryLimit;
			float ratio = Maths.Frac(0, SelectedRowIndex, m_grid.RowCount - 1);
			if (ratio < 0f + Limit && LineIndexRange.Begin > m_encoding.GetPreamble().Length)
				BuildLineIndex(LineStartIndexRange.Begin, false);
			if (ratio > 1f - Limit && LineIndexRange.End < m_fileend - m_row_delim.Length)
				BuildLineIndex(LineStartIndexRange.End  , false);
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
			case Keys.F2|Keys.Control:            SetBookmark(SelectedRowIndex, Bit.EState.Toggle); return true;
			case Keys.F3:                         FindNext(false);                    return true;
			case Keys.F3|Keys.Shift:              FindPrev(false);                    return true;
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
			var files = (string[])args.Data.GetData(DataFormats.FileDrop);
			if (files.Length != 1)
				return;

			args.Effect = DragDropEffects.All;
			if (test_can_drop) return;

			// Open the dropped file
			OpenSingleLogFile(files[0], true);
		}

		/// <summary>Turn on/off quick filter mode</summary>
		public void EnableQuickFilter(bool enable)
		{
			var has_selection = SelectedRowIndex != -1;
			var ofs = has_selection ? SelectedRowByteRange.Begin : -1;

			Settings.QuickFilterEnabled = enable;
			ApplySettings();
			BuildLineIndex(m_filepos, true, ()=>
				{
					if (has_selection)
						SelectRowByAddr(ofs);
				});
		}

		/// <summary>Turn on/off highlights</summary>
		public void EnableHighlights(bool enable)
		{
			var has_selection = SelectedRowIndex != -1;
			var bli_needed = Settings.QuickFilterEnabled;
			var ofs = has_selection && bli_needed ? SelectedRowByteRange.Begin : -1;

			Settings.HighlightsEnabled = enable;
			ApplySettings();
			InvalidateCache();
			m_grid.Refresh();
			if (bli_needed)
			{
				BuildLineIndex(m_filepos, true, () =>
					{
						if (has_selection)
							SelectRowByAddr(ofs);
					});
			}
		}

		/// <summary>Turn on/off filters</summary>
		public void EnableFilters(bool enable)
		{
			var has_selection = SelectedRowIndex != -1;
			var ofs = has_selection ? SelectedRowByteRange.Begin : -1;

			Settings.FiltersEnabled = enable;
			ApplySettings();
			BuildLineIndex(m_filepos, true, ()=>
				{
					if (has_selection)
						SelectRowByAddr(ofs);
				});
		}

		/// <summary>Turn on/off transforms</summary>
		public void EnableTransforms(bool enable)
		{
			Settings.TransformsEnabled = enable;
			ApplySettings();
			BuildLineIndex(m_filepos, true);
		}

		/// <summary>Turn on/off actions</summary>
		public void EnableActions(bool enabled)
		{
			Settings.ActionsEnabled = enabled;
			ApplySettings();
		}

		/// <summary>Jumps to a specific byte offset into the file</summary>
		private void JumpTo()
		{
			var dlg = new JumpToUi(this, FileByteRange.Begin, FileByteRange.End);
			if (dlg.ShowDialog(this) != DialogResult.OK) return;
			SelectRowByAddr(dlg.Address);
		}

		/// <summary>Handler for the Jump to Start button</summary>
		private void JumpToStart()
		{
			EnableTail(false);
			BuildLineIndex(0, false, () =>
				{
					SelectedRowIndex = 0;
					ShowFirstRow();
				});
		}

		/// <summary>Handler for the Jump to End button</summary>
		private void JumpToEnd()
		{
			//EnableTail(false);
			BuildLineIndex(m_fileend, false, () =>
				{
					SelectedRowIndex = m_grid.RowCount - 1;
					ShowLastRow();
				});
		}

		/// <summary>Turn on/off tail mode</summary>
		public void EnableTail(bool enabled)
		{
			if (enabled == m_tail_enabled) return;
			Settings.TailEnabled = m_tail_enabled = enabled;
			ApplySettings();
		}

		/// <summary>Turn on/off tail mode</summary>
		public void EnableWatch(bool enable)
		{
			Settings.WatchEnabled = enable;
			ApplySettings();
			if (enable)
				BuildLineIndex(m_filepos, Settings.FileChangesAdditive);
		}

		/// <summary>Turn on/off additive only mode</summary>
		public void EnableAdditive(bool enable)
		{
			Settings.FileChangesAdditive = enable;
			ApplySettings();
			if (!enable)
				BuildLineIndex(m_filepos, true);
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
				MsgBox.Show(this, string.Format(
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

		/// <summary>Enable/Disable monitor mode</summary>
		public void EnableMonitorMode(bool enable)
		{
			if (enable)
			{
				var dg = new MonitorModeUI(this) {AlwaysOnTop = Settings.AlwaysOnTop};
				if (dg.ShowDialog(this) != DialogResult.OK) return;
				m_menu_tools_monitor_mode.Checked = true;
				SetAlwaysOnTop(dg.AlwaysOnTop);
				ShowWindowFrame = false;

				Opacity = dg.Alpha;
				if (dg.ClickThru)
				{
					uint style = Win32.GetWindowLong(Handle, Win32.GWL_EXSTYLE);
					style = Bit.SetBits(style, Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT, true);
					Win32.SetWindowLong(Handle, Win32.GWL_EXSTYLE, style);
				}

				// Self removing icon clicked delegate
				EventHandler icon_clicked = null;
				icon_clicked = (s, a) =>
					{
						// ReSharper disable AccessToModifiedClosure
						m_notify_icon.Visible = false;
						m_notify_icon.Click -= icon_clicked;
						EnableMonitorMode(false);
						// ReSharper restore AccessToModifiedClosure
					};

				m_notify_icon.Click += icon_clicked;
				m_notify_icon.ShowBalloonTip(1000, "Monitor Mode", "Click here to cancel monitor mode", ToolTipIcon.Info);
				m_notify_icon.Text = "Click to disable monitor mode";
				m_notify_icon.Visible = true;
			}
			else
			{
				m_menu_tools_monitor_mode.Checked = false;
				ShowWindowFrame = true;
				Opacity = 1f;
				{
					uint style = Win32.GetWindowLong(Handle, Win32.GWL_EXSTYLE);
					style = Bit.SetBits(style, Win32.WS_EX_TRANSPARENT, false);
					Win32.SetWindowLong(Handle, Win32.GWL_EXSTYLE, style);
				}
			}
		}

		/// <summary>Show or hide the window frame</summary>
		private bool ShowWindowFrame
		{
			set
			{
				if (value)
				{
					m_grid.ScrollBars = ScrollBars.Both;
					m_grid.Margin = new Padding(3);
					m_table.Margin = new Padding(3);
					m_status.Visible = true;
					m_toolstrip.Visible = true;
					m_menu.Visible = true;
					m_scroll_file.Visible = true;
					FormBorderStyle = FormBorderStyle.Sizable;
				}
				else
				{
					// Order is important here, it affects the position of tool bars
					FormBorderStyle = FormBorderStyle.None;
					m_scroll_file.Visible = false;
					m_menu.Visible = false;
					m_toolstrip.Visible = false;
					m_status.Visible = false;
					m_table.Margin = new Padding(0);
					m_grid.Margin = new Padding(0);
					m_grid.ScrollBars = ScrollBars.None;
				}
			}
		}

		/// <summary>Set the encoding to use with loaded files. 'null' means auto detect</summary>
		public void SetEncoding(Encoding encoding)
		{
			string enc_name = encoding == null ? string.Empty : encoding.EncodingName;
			if (enc_name == Settings.Encoding) return; // not changed.

			// If a specific encoding is given, use it.
			// Otherwise leave it as whatever it is now, but reloading
			// the file will cause it to be auto detected.
			if (encoding != null)
				m_encoding = encoding;

			Settings.Encoding = enc_name;
			ApplySettings();
			BuildLineIndex(m_filepos, true);
		}

		/// <summary>Set the line ending to use with loaded files</summary>
		public void SetLineEnding(ELineEnding ending)
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
			if (row_delim == Settings.RowDelimiter) return; // not changed
			Settings.RowDelimiter = row_delim;
			ApplySettings();
			BuildLineIndex(m_filepos, true);
		}

		/// <summary>Set always on top</summary>
		private void SetAlwaysOnTop(bool onatop)
		{
			m_menu_tools_alwaysontop.Checked = onatop;
			Settings.AlwaysOnTop = onatop;
			TopMost = onatop;
		}

		/// <summary>Display the options dialog</summary>
		public void ShowOptions(SettingsUI.ETab tab, SettingsUI.ESpecial special = SettingsUI.ESpecial.None)
		{
			string row_text = "";
			string test_text = PatternUI.DefaultTestText;
			int init_row = SelectedRowIndex;
			if (init_row != -1)
				row_text = test_text = ReadLine(init_row).RowText.Trim();

			// Show the settings dialog, then reload the settings
			//using (EventsSnapshot.Capture(m_settings, EventsSnapshot.Restore.AssertNoChange)) // Prevent 'm_settings' holding references to 'ui'
			using (var ui = new SettingsUI(this, tab, special))
			{
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
				if      (ui.HighlightsChanged && !Settings.HighlightsEnabled) Misc.ShowHint(m_btn_highlights ,"Highlights are currently disabled");
				else if (ui.FiltersChanged    && !Settings.FiltersEnabled   ) Misc.ShowHint(m_btn_filters    ,"Filters are currently disabled"   );
				else if (ui.TransformsChanged && !Settings.TransformsEnabled) Misc.ShowHint(m_btn_transforms ,"Transforms are currently disabled");
				else if (ui.ActionsChanged    && !Settings.ActionsEnabled   ) Misc.ShowHint(m_btn_actions    ,"Actions are currently disabled"   );

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
		}

		/// <summary>Launch a web browser in order to view the html documentation</summary>
		private void ShowHelp()
		{
			const string HelpStartPage = @"docs\welcome.html";
			try
			{
				var path = Misc.ResolveAppPath(HelpStartPage);
				Process.Start(path);
			}
			catch (Exception ex)
			{
				MsgBox.Show(this,
					"Unable to display the help documentation do to an error.\r\n" +
					"Error Message: {0}\r\n".Fmt(ex.Message) +
					"\r\n" +
					"The expected location of the main documentation file is <install directory>\\"+HelpStartPage,
					"Missing help files", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>Show the first run tutorial</summary>
		private void ShowFirstRunTutorial()
		{
			try
			{
				var tut = new FirstRunTutorial(this);
				tut.Display();
			}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "First run tutorial failed");
				Misc.ShowMessage(this, "An error occurred when trying to display the first run tutorial", "First Run Tutorial Failed", MessageBoxIcon.Error, ex);
			}
		}

		/// <summary>Show the TotD dialog</summary>
		private void ShowTotD()
		{
			//using (EventsSnapshot.Capture(m_settings, EventsSnapshot.Restore.AssertNoChange))
			using (var totd = new TipOfTheDay(this))
				totd.ShowDialog(this);
		}

		/// <summary>Returns the web proxy to use, if specified in the settings</summary>
		private IWebProxy Proxy
		{
			get
			{
				IWebProxy proxy = WebRequest.DefaultWebProxy;
				if (Settings.UseWebProxy && !Settings.WebProxyHost.HasValue())
				{
					try { proxy =  new WebProxy(Settings.WebProxyHost, Settings.WebProxyPort); }
					catch (Exception ex) { Log.Exception(this, ex, "Failed to create web proxy for {0}:{1}".Fmt(Settings.WebProxyHost, Settings.WebProxyPort)); }
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

			string update_url = Settings.CheckForUpdatesServer + "versions/rylogviewer.xml";

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
				if (show_dialog) Misc.ShowMessage(this, "Check for updates failed", "Check for Updates", MessageBoxIcon.Error, error);
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
					if (show_dialog) MsgBox.Show(this, "The server was contacted but version information was not available", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
				if (this_version.CompareTo(othr_version) >  0)
				{
					SetTransientStatusMessage("Development version running");
					if (show_dialog) MsgBox.Show(this, "This version is newer than the latest version", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else if (this_version.CompareTo(othr_version) == 0)
				{
					SetTransientStatusMessage("Latest version running");
					if (show_dialog) MsgBox.Show(this, "This is the latest version", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
			new About(m_startup_options).ShowDialog(this);
		}

		/// <summary>Display info about the app being a free version</summary>
		private void ShowFreeVersionInfo(object sender = null, EventArgs args = null)
		{
			var dlg = HelpUI.FromHtml(this, Resources.free_version, "RyLogViewer Free Edition", Point.Empty, new Size(480,640), ToolForm.EPin.Centre);
			dlg.FormBorderStyle = FormBorderStyle.Sizable;
			dlg.ShowDialog(this);
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

		/// <summary>Return true if we should auto scroll</summary>
		private bool AutoScrollTail
		{
			get { return m_tail_enabled; }
		}

		/// <summary>Scroll the grid to make the first row visible</summary>
		private void ShowFirstRow()
		{
			if (m_grid.RowCount == 0) return;
			m_grid.FirstDisplayedScrollingRowIndex = 0;
			Log.Info(this, "Showing first row.");
		}

		/// <summary>Scroll the grid to make the last row visible</summary>
		private void ShowLastRow()
		{
			if (m_grid.RowCount == 0) return;
			int displayed_rows = m_grid.DisplayedRowCount(false);
			int first_row = Math.Max(0, m_grid.RowCount - displayed_rows);
			m_grid.TryScrollToRowIndex(first_row);
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
				var selected_rows = m_grid.SelectedRows.Cast<DataGridViewRow>().Select(x => x.Index).OrderBy(x => x).ToList();
				SelectedRowIndex = -1;
				m_grid.ClearSelection();

				// We need to set the row count even if it hasn't changed, because doing so
				// has the side effect of the grid recalculating it's scroll bar sizes from
				// the (now possibly different) row heights.
				var row_diff = count - m_grid.RowCount;
				Log.Info(this, "RowCount changed {0} -> {1}.".Fmt(m_grid.RowCount, count));
				m_grid.RowCount = 0;
				m_grid.RowCount = count;

				// If the number of rows added to the grid is not equal to the row delta
				// then the oddness of grid row zero depends on the difference
				if (row_diff != -row_delta)
				{
					// Give the illusion that the alternating row colour is moving with the overall file
					var row_zero_shift = Math.Max(-row_delta - row_diff, 0);
					if ((row_zero_shift & 1) == 1)
						m_first_row_is_odd = !m_first_row_is_odd;
				}
				Log.Info(this, "Row delta {0}.".Fmt(row_delta));

				// Restore the selected row, and the first visible row
				if (count != 0)
				{
					// Restore the selected rows, and the first visible row
					if (auto_scroll_tail)
					{
						SelectedRowIndex = m_grid.RowCount - 1;
					}
					else if (selected != -1)
					{
						m_grid.SelectRow(selected + row_delta);

						// Select the rows that were previously selected.
						// Find the index of the first selected row that is within the new range
						int rd = row_delta; // modified closure...
						int i = selected_rows.BinarySearch(x => x.CompareTo(-rd));
						for (i = (i >= 0) ? i : ~i; i != selected_rows.Count; ++i)
						{
							var s = selected_rows[i] + row_delta;
							if (s >= count) break; // can stop at the first selected row outside the new range
							m_grid.Rows[s].Selected = true;
						}

						// Restore the first visible row after setting the current selected row, because
						// changing the 'CurrentCell' also changes the scroll position
						if (first_vis != -1)
							m_grid.FirstDisplayedScrollingRowIndex = Maths.Clamp(first_vis + row_delta, 0, m_grid.RowCount - 1);
					}
				}
			}
			if (auto_scroll_tail) ShowLastRow();
		}

		/// <summary>Helper for setting the grid column size based on currently displayed content</summary>
		private void SetGridColumnSizes(bool force)
		{
			if (m_grid.ColumnCount == 0) return;
			if (!force && m_grid.ColumnHeadersVisible) return;
			m_batch_set_col_size.Signal();
		}
		private void SetGridColumnSizesImpl()
		{
			int grid_width = m_grid.DisplayRectangle.Width - 2;
			int col_count = m_grid.ColumnCount;

			m_grid.ColumnHeadersVisible = col_count > 1;

			// Measure each column's preferred width
			var col_widths = Enumerable.Repeat(30f, col_count).ToArray();
			using (var gfx = m_grid.CreateGraphics())
			{
				foreach (var row in m_grid.GetRowsWithState(DataGridViewElementStates.Displayed))
				{
					for (int i = 0, iend = Math.Min(col_count, row.Cells.Count); i != iend; ++i)
					{
						// For some reason, cell.GetPreferredSize or col.GetPreferredWidth don't return correct values
						var cell = row.Cells[i];
						var sz = gfx.MeasureString((string)cell.Value, cell.InheritedStyle.Font);
						var w = Maths.Clamp(sz.Width + 10, 30, 64000); // DGV throws if width is greater than 65535
						col_widths[i] = Math.Max(col_widths[i], w);
					}
				}
			}
			var total_width = Math.Max(col_widths.Sum(), 1);

			// Resize columns. If the total width is less than the control width use the control width instead
			var scale = Maths.Max(grid_width / total_width, 1f);
			foreach (DataGridViewColumn col in m_grid.Columns)
				col.Width = (int)(col_widths[col.Index] * scale);
		}

		/// <summary>
		/// Apply settings throughout the app.
		/// This method is called on startup to apply initial settings and
		/// after the settings dialog has been shown and closed. It needs to
		/// update anything that is only changed in the settings.
		/// Note: it doesn't trigger a file reload.</summary>
		public void ApplySettings()
		{
			Log.Info(this, "Applying settings");

			// Cached settings for performance, don't overwrite auto detected cached values though
			m_encoding                 = GetEncoding(Settings.Encoding);
			m_row_delim                = GetRowDelim(Settings.RowDelimiter);
			m_col_delim                = GetColDelim(Settings.ColDelimiter);
			m_row_height               = Settings.RowHeight;
			m_bufsize                  = Settings.FileBufSize;
			m_line_cache_count         = Settings.LineCacheCount;
			m_alternating_line_colours = Settings.AlternateLineColours;
			m_tail_enabled             = Settings.TailEnabled;
			m_quick_filter_enabled     = Settings.QuickFilterEnabled;

			// Tail
			m_watch_timer.Enabled = FileOpen && Settings.WatchEnabled;

			// Highlights;
			m_highlights.Clear();
			if (Settings.HighlightsEnabled)
			{
				m_highlights.AddRange(Settings.HighlightPatterns.Where(x => x.Active));
				UseLicensedFeature(FeatureName.Highlighting, new HighlightingCountLimiter(this));
			}

			// Filters
			m_filters.Clear();
			if (Settings.FiltersEnabled)
			{
				m_filters.AddRange(Settings.FilterPatterns.Where(x => x.Active));
				UseLicensedFeature(FeatureName.Filtering, new FilteringCountLimiter(this));
			}

			// Transforms
			m_transforms.Clear();
			if (Settings.TransformsEnabled)
				m_transforms.AddRange(Settings.TransformPatterns.Where(x => x.Active));

			// Click Actions
			m_clkactions.Clear();
			if (Settings.ActionsEnabled)
				m_clkactions.AddRange(Settings.ActionPatterns.Where(x => x.Active));

			// Grid
			int col_count = Settings.ColDelimiter.Length != 0 ? Maths.Clamp(Settings.ColumnCount, 1, 255) : 1;
			var col_count_changed = m_grid.ColumnCount != col_count;
			m_grid.Font = Settings.Font;
			m_grid.ColumnHeadersVisible = col_count > 1;
			m_grid.ColumnCount = col_count;
			m_grid.RowsDefaultCellStyle = new DataGridViewCellStyle
			{
				Font = Settings.Font,
				ForeColor = Settings.LineForeColour1,
				BackColor = Settings.LineBackColour1,
				SelectionBackColor = Settings.LineSelectBackColour,
				SelectionForeColor = Settings.LineSelectForeColour,
			};
			if (col_count_changed)
			{
				m_grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
			}
			if (Settings.AlternateLineColours)
			{
				m_grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
				{
					Font = Settings.Font,
					ForeColor = Settings.LineForeColour2,
					BackColor = Settings.LineBackColour2,
					SelectionBackColor = Settings.LineSelectBackColour,
					SelectionForeColor = Settings.LineSelectForeColour,
				};
			}
			else
			{
				m_grid.AlternatingRowsDefaultCellStyle = m_grid.RowsDefaultCellStyle;
			}
			m_grid.DefaultCellStyle.SelectionBackColor = Settings.LineSelectBackColour;
			m_grid.DefaultCellStyle.SelectionForeColor = Settings.LineSelectForeColour;

			// Set the string format
			using (var gfx = CreateGraphics())
			{
				var sz = gfx.MeasureString(" ", m_grid.Font);
				m_strfmt = new StringFormat(StringFormatFlags.NoWrap|StringFormatFlags.MeasureTrailingSpaces);
				m_strfmt.SetTabStops(0, Enumerable.Repeat(sz.Width * Settings.TabSizeInSpaces, 50).ToArray());
			}

			// File scroll
			m_scroll_file.TrackColor = Settings.ScrollBarFileRangeColour;
			m_scroll_file.ThumbColor = Settings.ScrollBarCachedRangeColour;

			// Ensure rows are re-rendered
			InvalidateCache();
			UpdateUI();
		}

		/// <summary>
		/// Update the UI with the current line index.
		/// This method should be called whenever a change occurs that requires
		/// UI elements to be updated/redrawn. Note: it doesn't trigger a file reload.</summary>
		private void UpdateUI(int row_delta = 0)
		{
			if (m_in_update_ui) return;
			using (Scope.Create(() => m_in_update_ui = true, () => m_in_update_ui = false))
			{
				Log.Info(this, "UpdateUI. Row delta {0}".Fmt(row_delta));
				using (m_grid.SuspendRedraw(true))//using (m_grid.SuspendLayout(true))
				{
					// Configure the grid
					if (m_line_index.Count != 0)
					{
						// Ensure the grid has the correct number of rows
						using (m_suspend_grid_events.Reference)
							SetGridRowCount(m_line_index.Count, row_delta);
					}
					else
					{
						m_grid.ColumnHeadersVisible = false;
						using (m_suspend_grid_events.Reference)
							SetGridRowCount(0, 0);
					}
					SetGridColumnSizes(false);
				}

				// Configure menus
				bool file_open                            = FileOpen;
				string enc                                = Settings.Encoding;
				string row_delim                          = Settings.RowDelimiter;
				m_menu_file_export.Enabled                = file_open;
				m_menu_file_close.Enabled                 = file_open;
				m_menu_edit_selectall.Enabled             = file_open;
				m_menu_edit_copy.Enabled                  = file_open;
				m_menu_edit_jumpto.Enabled                = file_open;
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
				m_menu_tools_clear_log_file.Enabled = file_open;
				m_menu_tools_alwaysontop.Checked = Settings.AlwaysOnTop;

				// Reread the licence
				m_license = new Licence(m_startup_options.AppDataDir);
				m_menu_free_version.Visible = !m_license.Valid;

				// Toolbar
				m_btn_quick_filter.Checked = Settings.QuickFilterEnabled;
				m_btn_highlights.Checked   = Settings.HighlightsEnabled;
				m_btn_filters.Checked      = Settings.FiltersEnabled;
				m_btn_transforms.Checked   = Settings.TransformsEnabled;
				m_btn_actions.Checked      = Settings.ActionsEnabled;
				m_btn_tail.Checked         = Settings.TailEnabled;
				m_btn_watch.Checked        = Settings.WatchEnabled;
				m_btn_additive.Checked     = Settings.FileChangesAdditive;

				// Status and title
				UpdateStatus();

				// Make suggestions for typically confusing situations
				HeuristicHints();
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
				Text = "{0} - {1}".Fmt(Settings.FullPathInTitle ? m_file.PsuedoFilepath : m_file.Name, Resources.AppTitle);
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
				StringBuilder len = pretty(new StringBuilder(FileByteRange.End.ToString(CultureInfo.InvariantCulture)));

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
			if (current == total || total == 0)
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

			m_scroll_file.TotalRange = FileByteRange;
			m_scroll_file.ThumbRange = range;
			m_scroll_file.Width      = Settings.FileScrollWidth;

			m_scroll_file.ClearIndicatorRanges();
			m_scroll_file.AddIndicatorRange(DisplayedRowsRange, Settings.ScrollBarDisplayRangeColour);
			foreach (var sel_range in SelectedRowRanges)
				m_scroll_file.AddIndicatorRange(sel_range, Settings.LineSelectBackColour);

			// Add marks for the bookmarked positions
			var bkmark_colour = Settings.BookmarkColour;
			foreach (var bk in m_bookmarks)
				m_scroll_file.AddIndicatorRange(bk.Range, bkmark_colour);

			m_scroll_file.Refresh();
		}

		/// <summary>Show a hint balloon for situations that users might find confusing but are still valid</summary>
		private void HeuristicHints()
		{
			int row_count = m_grid.RowCount;
			int col_count = m_grid.ColumnCount;

			// Show a hint if filters are active, the file isn't empty, but there are no visible rows
			if (m_last_hint != EHeuristicHint.FiltersActive &&
				row_count == 0 &&
				m_fileend != 0)
			{
				if (m_filters.Count != 0)        Misc.ShowHint(m_btn_filters     , "No visible data due to currently active filters");
				else if (m_quick_filter_enabled) Misc.ShowHint(m_btn_quick_filter, "No visible data due to currently active filters");
				m_last_hint = EHeuristicHint.FiltersActive;
			}

			// If there is one row, and that row contains more than one '\n' or '\r', and the line endings are not set
			// to detect, then suggest that maybe the line endings need changing
			if (m_last_hint != EHeuristicHint.LineEndings &&
				row_count == 1 &&
				Settings.RowDelimiter != string.Empty &&
				ReadLine(0).RowText.Count(c => c == '\n' || c == '\r') > 2)
			{
				Misc.ShowHint(m_menu_line_ending, "Only one line detected, check line ending");
				m_last_hint = EHeuristicHint.LineEndings;
			}

			// If there is one row, and it's fairly long, and the encoding is not set to detect
			if (m_last_hint != EHeuristicHint.Encoding &&
				row_count == 1 &&
				Settings.Encoding != string.Empty &&
				m_line_index[0].Size > 1024)
			{
				Misc.ShowHint(m_menu_encoding, "Only one line detected, check text encoding");
				m_last_hint = EHeuristicHint.Encoding;
			}
		}
		private enum EHeuristicHint { None, FiltersActive, LineEndings, Encoding }
		private EHeuristicHint m_last_hint;

		/// <summary>Create a message that displays for a period then disappears. Use null or "" to hide the status</summary>
		public void SetTransientStatusMessage(string text, Color frcol, Color bkcol, TimeSpan display_time_ms)
		{
			m_status_message_trans.SetStatusMessage(text, null, null, false, frcol, bkcol, display_time_ms);
			//m_status_message_trans.Text = text ?? string.Empty;
			//m_status_message_trans.Visible = text.HasValue();
			//m_status_message_trans.ForeColor = frcol;
			//m_status_message_trans.BackColor = bkcol;

			//// If the status message has a timer already, dispose it
			//Timer timer = m_status_message_trans.Tag as Timer;
			//if (timer != null) timer.Dispose();

			//// Attach a new timer to the status message
			//if (text.HasValue() && display_time_ms != TimeSpan.MaxValue)
			//{
			//	m_status_message_trans.Tag = timer = new Timer{Enabled = true, Interval = (int)display_time_ms.TotalMilliseconds};
			//	timer.Tick += (s,a)=>
			//		{
			//			// When the timer fires, if we're still associated with
			//			// the status message, null out the text and remove our self
			//			if (s != m_status_message_trans.Tag) return;
			//			m_status_message_trans.Text = Resources.Idle;
			//			m_status_message_trans.Visible = false;
			//			m_status_message_trans.Tag = null;
			//			((Timer)s).Dispose();
			//		};
			//}
		}
		public void SetTransientStatusMessage(string text, Color frcol, Color bkcol)
		{
			SetTransientStatusMessage(text, frcol, bkcol, TimeSpan.FromSeconds(2));
		}
		public void SetTransientStatusMessage(string text)
		{
			SetTransientStatusMessage(text, SystemColors.ControlText, SystemColors.Control);
		}

		/// <summary>Create a status message that displays until cleared. Use null or "" to hide the status</summary>
		public void SetStaticStatusMessage(string text, Color frcol, Color bkcol)
		{
			m_status_message_fixed.SetStatusMessage(text, null, Resources.Idle, false, frcol, bkcol);
			//m_status_message_fixed.Text = text ?? string.Empty;
			//m_status_message_fixed.Visible = text.HasValue();
			//m_status_message_fixed.ForeColor = frcol;
			//m_status_message_fixed.BackColor = bkcol;
		}
		public void SetStaticStatusMessage(string text)
		{
			SetStaticStatusMessage(text, SystemColors.ControlText, SystemColors.Control);
		}

		/// <summary>Cycles colours for the 'free edition' menu item</summary>
		private void CycleColours()
		{
			//return; //hack
			//if (!m_menu_free_version.Visible) return;
			//m_free_version_menu_colour.H += 0.01f;
			//if (m_free_version_menu_colour.H > 1f) m_free_version_menu_colour.H = 0f;
			//m_menu_free_version.ForeColor = m_free_version_menu_colour.ToColor();
		}
		//private HSV m_free_version_menu_colour = HSV.FromColor(Color.Red);

		/// <summary>Custom button renderer because the office 'checked' state buttons look crap</summary>
		private class CheckedButtonRenderer :ToolStripProfessionalRenderer
		{
			protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
			{
				var btn = e.Item as ToolStripButton;
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
