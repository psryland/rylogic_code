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
using pr.container;
using pr.extn;
using pr.gui;
using pr.inet;
using pr.maths;
using pr.util;
using pr.win32;
using RyLogViewer.Properties;
using Timer = System.Windows.Forms.Timer;

namespace RyLogViewer
{
	public sealed partial class Main :Form ,IMainUI
	{
		private readonly List<Highlight> m_highlights;     // A list of the active highlights only
		private readonly List<Filter> m_filters;           // A list of the active filters only
		private readonly List<Transform> m_transforms;     // A list of the active transforms only
		private readonly List<ClkAction> m_clkactions;     // A list of the active click actions only
		private readonly NotifyIcon m_notify_icon;         // A system tray icon
		private readonly Form[] m_tab_cycle;               // The forms that Ctrl+Tab cycles through
		private readonly RefCount m_suspend_grid_events;   // A ref count of nested calls that tell event handlers to ignore grid events
		private EventBatcher m_batch_set_col_size;         // A call batcher for setting the column widths
		private Encoding m_encoding;                       // The file encoding
		private byte[] m_row_delim;                        // The row delimiter converted from a string to a byte[] using the current encoding
		private byte[] m_col_delim;                        // The column delimiter, cached to prevent Settings access in CellNeeded
		private int m_row_height;                          // The row height, cached to prevent settings lookups in CellNeeded
		private long m_filepos;                            // The byte offset (from file begin) to the start of the last known line
		private long m_fileend;                            // The last known size of the file
		private long m_bufsize;                            // Cached value of Settings.FileBufSize
		private int m_line_cache_count;                    // The number of lines to scan about the currently selected row
		private bool m_alternating_line_colours;           // Cache the alternating line colours setting for performance
		private bool m_tail_enabled;                       // Cache whether tail mode is enabled
		private bool m_quick_filter_enabled;               // True if only rows with highlights should be displayed
		private bool m_first_row_is_odd;                   // Tracks whether the first row is odd or even for alternating row colours (not 100% accurate)
		private StringFormat m_strfmt;                     // Caches the tab stop sizes for rendering
		private RecentFiles m_recent_logfiles;             // Recent files
		private RecentFiles m_recent_pattern_sets;         // Recent pattern sets

		#region UI Elements
		private System.Windows.Forms.ToolStrip m_toolstrip;
		private System.Windows.Forms.ToolStripButton m_btn_open_log;
		private System.Windows.Forms.MenuStrip m_menu;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_open;
		private System.Windows.Forms.ToolStripSeparator m_sep1;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_exit;
		private System.Windows.Forms.StatusStrip m_status;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_recent;
		private System.Windows.Forms.ToolStripSeparator m_sep2;
		private System.Windows.Forms.ToolStripButton m_btn_jump_to_end;
		private System.Windows.Forms.ToolStripContainer m_toolstrip_cont;
		private DataGridView m_grid;
		private System.Windows.Forms.ToolStripSeparator m_sep;
		private System.Windows.Forms.ToolStripButton m_btn_highlights;
		private System.Windows.Forms.ToolStripButton m_btn_filters;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_close;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_selectall;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_copy;
		private System.Windows.Forms.ToolStripSeparator m_sep3;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_find;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_find_next;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_find_prev;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_alwaysontop;
		private System.Windows.Forms.ToolStripSeparator m_sep4;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_highlights;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_filters;
		private System.Windows.Forms.ToolStripSeparator m_sep5;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_options;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_about;
		private System.Windows.Forms.ToolStripStatusLabel m_status_filesize;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton m_btn_refresh;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_totd;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem m_menu_encoding;
		private System.Windows.Forms.ToolStripMenuItem m_menu_encoding_ascii;
		private System.Windows.Forms.ToolStripMenuItem m_menu_encoding_utf8;
		private System.Windows.Forms.ToolStripMenuItem m_menu_encoding_ucs2_bigendian;
		private System.Windows.Forms.ToolStripMenuItem m_menu_encoding_ucs2_littleendian;
		private System.Windows.Forms.ToolStripStatusLabel m_status_spring;
		private System.Windows.Forms.ToolStripStatusLabel m_status_message_trans;
		private System.Windows.Forms.ToolStripStatusLabel m_status_line_end;
		private System.Windows.Forms.ToolStripMenuItem m_menu_encoding_detect;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripStatusLabel m_status_encoding;
		private pr.gui.SubRangeScroll m_scroll_file;
		private System.Windows.Forms.TableLayoutPanel m_table;
		private System.Windows.Forms.ToolStripButton m_btn_watch;
		private System.Windows.Forms.ContextMenuStrip m_cmenu_grid;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_copy;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_select_all;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_export;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_clear_log_file;
		private System.Windows.Forms.ToolStripProgressBar m_status_progress;
		private System.Windows.Forms.ToolStripMenuItem m_menu_line_ending;
		private System.Windows.Forms.ToolStripMenuItem m_menu_line_ending_detect;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem m_menu_line_ending_cr;
		private System.Windows.Forms.ToolStripMenuItem m_menu_line_ending_crlf;
		private System.Windows.Forms.ToolStripMenuItem m_menu_line_ending_lf;
		private System.Windows.Forms.ToolStripMenuItem m_menu_line_ending_custom;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_monitor_mode;
		private System.Windows.Forms.ToolStripButton m_btn_jump_to_start;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_check_for_updates;
		private System.Windows.Forms.ToolStripButton m_btn_transforms;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.ToolStripButton m_btn_additive;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_highlight_row;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_filter_row;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_transform_row;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_action_row;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_transforms;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_find_next;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_find_prev;
		private System.Windows.Forms.ToolStripButton m_btn_actions;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tools_actions;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_visit_web_site;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_register;
		private System.Windows.Forms.ToolStripButton m_btn_tail;
		private System.Windows.Forms.ToolStripButton m_btn_bookmarks;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_toggle_bookmark;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_next_bookmark;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_prev_bookmark;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_clearall_bookmarks;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_bookmarks;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_toggle_bookmark;
		private System.Windows.Forms.ToolStripMenuItem m_cmenu_clear_log;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator15;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_data_sources;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_wizards_androidlogcat;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator16;
		private System.Windows.Forms.ToolStripButton m_btn_find;
		private System.Windows.Forms.ToolStripButton m_btn_quick_filter;
		private System.Windows.Forms.ToolStripStatusLabel m_status_message_fixed;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_wizards_aggregatelogfile;
		private System.Windows.Forms.ToolStripMenuItem m_menu_free_version;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_view_help;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
		private System.Windows.Forms.ToolStripMenuItem m_menu_help_firstruntutorial;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_open_stdout;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_open_serial_port;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_open_network;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_open_named_pipe;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator19;
		private System.Windows.Forms.ToolStripMenuItem m_menu_edit_jumpto;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator17;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator18;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_pattern_set;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_load_pattern_set;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_save_pattern_set;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator21;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_import_patterns;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator20;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_recent_pattern_sets;
		private ToolStripStatusLabel m_status_selection;
		private System.Windows.Forms.ToolTip m_tt;
		#endregion

		public Main(StartupOptions startup_options)
		{
			Log.Register(startup_options.LogFilePath, false);
			Log.Info(this, "App Startup: {0}".Fmt(DateTime.Now));
			InitializeComponent();

			StartupOptions        = startup_options;
			m_highlights          = new List<Highlight>();
			m_filters             = new List<Filter>();
			m_transforms          = new List<Transform>();
			m_clkactions          = new List<ClkAction>();
			m_suspend_grid_events = new RefCount();
			m_batch_set_col_size  = new EventBatcher(SetGridColumnSizesImpl, TimeSpan.FromMilliseconds(100));
			m_tab_cycle           = new Form[]{this, FindUI, BookmarksUI};
			m_notify_icon         = new NotifyIcon{Icon = Icon};
			Licence               = new Licence(StartupOptions.LicenceFilepath);
			Watch                 = new FileWatch();
			WatchTimer            = new Timer();
			FindHistory           = new BindingSource<Pattern>{DataSource = new BindingListEx<Pattern>()};
			Bookmarks             = new BindingSource<Bookmark>{DataSource = new BindingListEx<Bookmark>()};
			FindUI                = new FindUI(this, FindHistory);
			BookmarksUI           = new BookmarksUI(this, Bookmarks);
			Settings              = new Settings(StartupOptions.SettingsPath);
			Src                   = null;
			m_filepos             = 0;
			m_fileend             = 0;

			m_bufsize             = Settings.FileBufSize;
			m_line_cache_count    = Settings.LineCacheCount;
			m_tail_enabled        = Settings.TailEnabled;

			SetupUI();

			InitCache();
			ApplySettings();
		}
		protected override void Dispose(bool disposing)
		{
			Src = null;
			FindUI = null;
			BookmarksUI = null;
			WatchTimer = null;
			Watch = null;
			Util.Dispose(ref m_batch_set_col_size);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
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

			// Save the screen position
			Settings.ScreenPosition = Location;
			Settings.WindowSize = Size;
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
		protected override bool ProcessCmdKey(ref Message msg, Keys key_data)
		{
			if (HandleKeyDown(this, key_data)) return true;
			return base.ProcessCmdKey(ref msg, key_data);
		}

		/// <summary>The main UI as a form for use as the parent of child dialogs</summary>
		public Form MainWindow
		{
			get { return this; }
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
					m_impl_settings.SettingsSaving -= HandleSettingsSaved;
				}
				m_impl_settings = value;
				if (m_impl_settings != null)
				{
					m_impl_settings.SettingsSaving += HandleSettingsSaved;
					m_impl_settings.SettingChanged += HandleSettingsChanged;
					ApplySettings();
				}
			}
		}
		private Settings m_impl_settings;
		private void HandleSettingsSaved(object sender, SettingsSavingEventArgs e)
		{
			ApplySettings();
		}
		private void HandleSettingsChanged(object sender, SettingChangedEventArgs args)
		{
			Log.Info(this, "Setting {0} changed from {1} to {2}".Fmt(args.Key,args.OldValue,args.NewValue));
		}

		/// <summary>The options provided at startup</summary>
		public StartupOptions StartupOptions
		{
			[DebuggerStepThrough] get { return m_su_options; }
			set
			{
				if (m_su_options == value) return;
				m_su_options = value;
			}
		}
		private StartupOptions m_su_options;

		/// <summary>The currently loaded log data source</summary>
		public IFileSource Src
		{
			[DebuggerStepThrough] get { return m_src; }
			set
			{
				if (m_src == value) return;
				if (m_src != null)
				{
					// Clear bookmarks
					Bookmarks.Clear();

					using (m_suspend_grid_events.Scope())
					{
						// Abort any BLI in progress
						CancelBuildLineIndex();

						// Stop watching related files
						Watch.Remove(m_src.Filepaths);

						SelectionChanged = null;
						m_line_index.Clear();
						m_grid.RowCount = 0;
						m_last_hint = EHeuristicHint.None;

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
					}

					// Clear status
					SetTransientStatusMessage(null);
					SetStaticStatusMessage(null);

					Util.Dispose(ref m_src);
					m_filepos = 0;
					m_fileend = 0;
				}
				m_src = value;
				if (m_src != null)
				{
					m_src.Open();
					m_filepos = Settings.OpenAtEnd ? m_src.Stream.Length : 0;

					// Set up the watcher to watch for file changes
					Watch.Add(m_src.Filepaths, (fp,ctx) =>
					{
						OnFileChanged();
						return true;
					});
					WatchTimer.Enabled = Settings.WatchEnabled;

					// Start the initial load of log data
					BuildLineIndex(m_filepos, true, () =>
					{
						SelectedRowIndex = Settings.OpenAtEnd ? m_grid.RowCount - 1 : 0;
						SetGridColumnSizes(true);
					});
				}

				// Initiate a UI update after any existing queued events
				this.BeginInvoke(() => UpdateUI());
			}
		}
		private IFileSource m_src;

		/// <summary>A helper for watching files</summary>
		private FileWatch Watch
		{
			get { return m_watch; }
			set
			{
				if (m_watch == value) return;
				m_watch = value;
			}
		}
		private FileWatch m_watch;
		
		/// <summary>A timer for polling the file watcher</summary>
		private Timer WatchTimer
		{
			get { return m_watch_timer; }
			set
			{
				if (m_watch_timer == value) return;
				if (m_watch_timer != null)
				{
					m_watch_timer.Tick -= HandleWatchTimerTick;
				}
				m_watch_timer = value;
				if (m_watch_timer != null)
				{
					m_watch_timer.Interval = Constants.FilePollingRate;
					m_watch_timer.Tick += HandleWatchTimerTick;
				}
			}
		}
		private Timer m_watch_timer;
		private void HandleWatchTimerTick(object sender, EventArgs e)
		{
			if (ReloadInProgress) return;
			if (WindowState == FormWindowState.Minimized) return;
			try { Watch.CheckForChangedFiles(); }
			catch (Exception ex)
			{
				Log.Exception(this, ex, "CheckForChangedFiles failed");
			}
		}

		/// <summary>Licence data</summary>
		public Licence Licence
		{
			get { return m_lic; }
			set
			{
				if (m_lic == value) return;
				if (m_lic != null)
				{}
				m_lic = value;
				if (m_lic != null)
				{}
			}
		}
		private Licence m_lic;

		/// <summary>Set up the UI Elements</summary>
		private void SetupUI()
		{
			#region Menu
			{
				m_menu.Location = Point.Empty;

				// File menu
				m_menu_file_open.Click                     += (s,a) => OpenSingleLogFile(null, true);
				m_menu_file_wizards_androidlogcat.Click    += (s,a) => AndroidLogcatWizard();
				m_menu_file_wizards_aggregatelogfile.Click += (s,a) => AggregateFileWizard();
				m_menu_file_open_stdout.Click              += (s,a) => LogProgramOutput();
				m_menu_file_open_serial_port.Click         += (s,a) => LogSerialPort();
				m_menu_file_open_network.Click             += (s,a) => LogNetworkOutput();
				m_menu_file_open_named_pipe.Click          += (s,a) => LogNamedPipeOutput();
				m_menu_file_close.Click                    += (s,a) => Src = null;
				m_menu_file_load_pattern_set.Click         += (s,a) => LoadPatternSet(null);
				m_menu_file_save_pattern_set.Click         += (s,a) => SavePatternSet(null);
				m_menu_file_import_patterns.Click          += (s,a) => ImportPatterns(null);
				m_recent_pattern_sets                       = new RecentFiles(m_menu_file_recent_pattern_sets, fp => LoadPatternSet(fp)).Import(Settings.RecentPatternSets);
				m_recent_pattern_sets.RecentListChanged    += (s,a) => Settings.RecentPatternSets = m_recent_pattern_sets.Export();
				m_menu_file_export.Click                   += (s,a) => ShowExportDialog();
				m_recent_logfiles                           = new RecentFiles(m_menu_file_recent, fp => OpenSingleLogFile(fp, true)).Import(Settings.RecentFiles);
				m_recent_logfiles.RecentListChanged        += (s,a) => Settings.RecentFiles = m_recent_logfiles.Export();
				m_menu_file_exit.Click                     += (s,a) => Close();

				// Edit menu
				m_menu_edit_selectall.Click                += (s,a) => DataGridViewEx.SelectAll(m_grid, new KeyEventArgs(Keys.Control|Keys.A));
				m_menu_edit_copy.Click                     += (s,a) => DataGridViewEx.Copy(m_grid, new KeyEventArgs(Keys.Control|Keys.C));
				m_menu_edit_jumpto.Click                   += (s,a) => JumpTo();
				m_menu_edit_find.Click                     += (s,a) => ShowFindDialog();
				m_menu_edit_find_next.Click                += (s,a) => FindNext(false);
				m_menu_edit_find_prev.Click                += (s,a) => FindPrev(false);
				m_menu_edit_toggle_bookmark.Click          += (s,a) => SetBookmark(SelectedRowIndex, Bit.EState.Toggle);
				m_menu_edit_next_bookmark.Click            += (s,a) => NextBookmark();
				m_menu_edit_prev_bookmark.Click            += (s,a) => PrevBookmark();
				m_menu_edit_clearall_bookmarks.Click       += (s,a) => ClearAllBookmarks();
				m_menu_edit_bookmarks.Click                += (s,a) => ShowBookmarksDialog();

				// Encoding menu
				m_menu_encoding_detect.Click               += (s,a) => SetEncoding(null);
				m_menu_encoding_ascii.Click                += (s,a) => SetEncoding(Encoding.ASCII           );
				m_menu_encoding_utf8.Click                 += (s,a) => SetEncoding(Encoding.UTF8            );
				m_menu_encoding_ucs2_littleendian.Click    += (s,a) => SetEncoding(Encoding.Unicode         );
				m_menu_encoding_ucs2_bigendian.Click       += (s,a) => SetEncoding(Encoding.BigEndianUnicode);

				// Line ending menu
				m_menu_line_ending_detect.Click            += (s,a) => SetLineEnding(ELineEnding.Detect);
				m_menu_line_ending_cr.Click                += (s,a) => SetLineEnding(ELineEnding.CR    );
				m_menu_line_ending_crlf.Click              += (s,a) => SetLineEnding(ELineEnding.CRLF  );
				m_menu_line_ending_lf.Click                += (s,a) => SetLineEnding(ELineEnding.LF    );
				m_menu_line_ending_custom.Click            += (s,a) => SetLineEnding(ELineEnding.Custom);

				// Tools menu
				m_menu_tools_alwaysontop.Click             += (s,a) => SetAlwaysOnTop(!Settings.AlwaysOnTop);
				m_menu_tools_monitor_mode.Click            += (s,a) => EnableMonitorMode(!m_menu_tools_monitor_mode.Checked);
				m_menu_tools_clear_log_file.Click          += (s,a) => ClearLogFile();
				m_menu_tools_highlights.Click              += (s,a) => ShowOptions(SettingsUI.ETab.Highlights);
				m_menu_tools_filters.Click                 += (s,a) => ShowOptions(SettingsUI.ETab.Filters   );
				m_menu_tools_transforms.Click              += (s,a) => ShowOptions(SettingsUI.ETab.Transforms);
				m_menu_tools_actions.Click                 += (s,a) => ShowOptions(SettingsUI.ETab.Actions   );
				m_menu_tools_options.Click                 += (s,a) => ShowOptions(SettingsUI.ETab.General   );

				// Help menu
				m_menu_help_view_help.Click                += (s,a) => ShowHelp();
				m_menu_help_firstruntutorial.Click         += (s,a) => ShowFirstRunTutorial();
				m_menu_help_totd.Click                     += (s,a) => ShowTotD();
				m_menu_help_visit_web_site.Click           += (s,a) => VisitWebSite();
				m_menu_help_register.Click                 += (s,a) => ShowActivation();
				m_menu_help_check_for_updates.Click        += (s,a) => CheckForUpdates(true);
				m_menu_help_about.Click                    += (s,a) => ShowAbout();
				m_menu_free_version.Click                  += ShowFreeVersionInfo;
			}
			#endregion
			#region Tool bar
			{
				m_toolstrip.Location            = new Point(0,30);
				m_btn_open_log.ToolTipText      = "Open a log file.";
				m_btn_open_log.Click           += (s,a) => OpenSingleLogFile(null, true);
				m_btn_refresh.ToolTipText       = "Reload the current log file.";
				m_btn_refresh.Click            += (s,a) => BuildLineIndex(m_filepos, true);
				m_btn_quick_filter.ToolTipText  = "Quick filter; keep highlighted rows only.";
				m_btn_quick_filter.Click       += (s,a) => EnableQuickFilter(m_btn_quick_filter.Checked);
				m_btn_highlights.ToolTipText    = "Left click to enable/disable highlighting.\r\nRight click to show the highlighting dialog.";
				m_btn_highlights.Click         += (s,a) => EnableHighlights(m_btn_highlights.Checked);
				m_btn_highlights.MouseDown     += (s,a) => { if (a.Button == MouseButtons.Right) ShowOptions(SettingsUI.ETab.Highlights); };
				m_btn_filters.ToolTipText       = "Left click to enable/disable filters.\r\nRight click to show the filters dialog.";
				m_btn_filters.Click            += (s,a) => EnableFilters(m_btn_filters.Checked);
				m_btn_filters.MouseDown        += (s,a) => { if (a.Button == MouseButtons.Right) ShowOptions(SettingsUI.ETab.Filters); };
				m_btn_transforms.ToolTipText    = "Left click to enable/disable transforms.\r\nRight click to show the transforms dialog.";
				m_btn_transforms.Click         += (s,a) => EnableTransforms(m_btn_transforms.Checked);
				m_btn_transforms.MouseDown     += (s,a) => { if (a.Button == MouseButtons.Right) ShowOptions(SettingsUI.ETab.Transforms); };
				m_btn_actions.ToolTipText       = "Left click to enable/disable actions.\r\nRight click to show the actions dialog.";
				m_btn_actions.Click            += (s,a) => EnableActions(m_btn_actions.Checked);
				m_btn_actions.MouseDown        += (s,a) => { if (a.Button == MouseButtons.Right) ShowOptions(SettingsUI.ETab.Actions); };
				m_btn_find.ToolTipText          = "Show the find dialog.";
				m_btn_find.Click               += (s,a) => ShowFindDialog();
				m_btn_bookmarks.ToolTipText     = "Show the bookmarks dialog.";
				m_btn_bookmarks.Click          += (s,a) => ShowBookmarksDialog();
				m_btn_jump_to_start.ToolTipText = "Selected the first row in the log file.";
				m_btn_jump_to_start.Click      += (s,a) => JumpToStart();
				m_btn_jump_to_end.ToolTipText   = "Selected the last row in the log file.";
				m_btn_jump_to_end.Click        += (s,a) => JumpToEnd();
				m_btn_tail.ToolTipText          = "Automatically scroll to the bottom of the log as new data is loaded.";
				m_btn_tail.Click               += (s,a) => EnableTail(m_btn_tail.Checked);
				m_btn_watch.ToolTipText         = "Watch the file and update automatically when it changes.";
				m_btn_watch.Click              += (s,a) => EnableWatch(m_btn_watch.Checked);
				m_btn_additive.ToolTipText      = "Assume log file changes are additive only. If enabled, only additions to the\r\n" +
												  "file will be scanned allowing increased performance. The view may get out of sync if the\r\n"+
												  "log file is modified in a way that isn't additive (e.g. lines swapped, deleted, etc)";
				m_btn_additive.Click           += (s,a) => EnableAdditive(m_btn_additive.Checked);
				ToolStripManager.Renderer       = new CheckedButtonRenderer();
			}
			#endregion
			#region Scroll bar
			{
				m_scroll_file.ToolTip(m_tt, "Indicates the currently cached position in the log file\r\nClicking within here moves the cached position within the log file");
				m_scroll_file.MinThumbSize = 1;
				m_scroll_file.ScrollEnd += OnScrollFileScrollEnd;
			}
			#endregion
			#region Status
			{
				m_status.Location             = Point.Empty;
				m_status_progress.ToolTipText = "Press escape to cancel";
				m_status_progress.Minimum     = 0;
				m_status_progress.Maximum     = 100;
				m_status_progress.Visible     = false;
				m_status_progress.Text        = "Test";
			}
			#endregion
			#region Grid
			{
				m_grid.RowCount                  = 0;
				m_grid.AutoGenerateColumns       = false;
				m_grid.KeyDown                  += DataGridViewEx.SelectAll;
				m_grid.KeyDown                  += DataGridViewEx.Copy;
				m_grid.KeyDown                  += GridKeyDown;
				m_grid.MouseUp                  += (s,a) => GridMouseButton(a, false);
				m_grid.MouseDown                += (s,a) => GridMouseButton(a, true);
				m_grid.MouseMove                += (s,a) => GridMouseMove(a);
				m_grid.CellDoubleClick          += CellDoubleClick;
				m_grid.CellValueNeeded          += CellValueNeeded;
				m_grid.RowPrePaint              += RowPrePaint;
				m_grid.SelectionChanged         += GridSelectionChanged;
				m_grid.ColumnDividerDoubleClick += (s,a) => { SetGridColumnSizesImpl(); a.Handled = true; };
				m_grid.RowHeightInfoNeeded      += RowHeightNeeded;
				m_grid.DataError                += (s,a) => Debug.Assert(false);
				m_grid.Scroll                   += (s,a) => GridScroll();

				// Grid context menu
				m_cmenu_grid.ItemClicked    += GridContextMenu;
				m_cmenu_grid.VisibleChanged += SetGridContextMenuVisibility;
			}
			#endregion

			SetupFind();
			SetupBookmarks();
		}

		/// <summary>Update the UI with the current line index.</summary>
		private void UpdateUI(int row_delta = 0)
		{
			// This method should be called whenever a change occurs that requires UI elements
			// to be updated/redrawn. Note: it doesn't trigger a file reload.
			if (m_in_update_ui != 0) return;
			using (Scope.Create(() => ++m_in_update_ui, () => --m_in_update_ui))
			{
				Log.Info(this, "UpdateUI. Row delta {0}".Fmt(row_delta));
				using (m_grid.SuspendRedraw(true))
				{
					// Configure the grid
					if (m_line_index.Count != 0)
					{
						// Ensure the grid has the correct number of rows
						using (m_suspend_grid_events.Scope())
							SetGridRowCount(m_line_index.Count, row_delta);
					}
					else
					{
						m_grid.ColumnHeadersVisible = false;
						using (m_suspend_grid_events.Scope())
							SetGridRowCount(0, 0);
					}
					SetGridColumnSizes(false);
				}

				// Configure menus
				bool file_open                            = Src != null;
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
				m_menu_tools_alwaysontop         .Checked = Settings.AlwaysOnTop;
				m_menu_tools_clear_log_file.Enabled = file_open;

				// Reread the licence
				Licence = new Licence(StartupOptions.LicenceFilepath);
				m_menu_free_version.Visible = !Licence.Valid;

				// Tool bar
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

				// Redraw the grid
				m_grid.Invalidate();
			}
		}
		private int m_in_update_ui;

		/// <summary>Apply the startup options</summary>
		private void ApplyStartupOptions()
		{
			AllowTransparency = true;
			if (Settings.RestoreScreenLoc)
			{
				StartPosition = FormStartPosition.Manual;
				Location = Settings.ScreenPosition;
				Size = Settings.WindowSize;
			}

			// If delimiters are given, replace the delimiters in the settings
			if (StartupOptions.RowDelim != null)
				Settings.RowDelimiter = StartupOptions.RowDelim;
			if (StartupOptions.ColDelim != null)
				Settings.ColDelimiter = StartupOptions.ColDelim;

			// If a pattern set file path is given, replace the patterns in 'Settings' with the contents of the file
			if (StartupOptions.PatternSetFilepath != null)
			{
				try { Settings.Patterns = PatternSet.Load(StartupOptions.PatternSetFilepath); }
				catch (Exception ex)
				{
					Misc.ShowMessage(this, "Could not load highlight pattern set {0}.".Fmt(StartupOptions.PatternSetFilepath), Application.ProductName, MessageBoxIcon.Error, ex);
				}
			}
		}

		/// <summary>Called the first time the app is displayed</summary>
		private void Startup()
		{
			// Startup options
			ApplyStartupOptions();

			// Look for plugin data sources, called here so the UI is displayed over the main window
			InitCustomDataSources();

			// Parse command line
			if (StartupOptions.FileToLoad != null)
			{
				OpenSingleLogFile(StartupOptions.FileToLoad, true);
			}
			else if (Settings.LoadLastFile && Path_.FileExists(Settings.LastLoadedFile))
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

			// Set always on top
			if (Settings.AlwaysOnTop)
				TopMost = true;

			Settings.FirstRun = false;
		}

		/// <summary>Load a pattern set file</summary>
		private void LoadPatternSet(string filepath)
		{
			// Prompt for a file if not given
			if (!filepath.HasValue())
			{
				using (var dlg = new OpenFileDialog { Title = "Load Pattern Set", Filter = Constants.PatternSetFilter, InitialDirectory = Settings.PatternSetDirectory })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					Settings.PatternSetDirectory = Path_.Directory(dlg.FileName);
					filepath = dlg.FileName;
				}
			}

			try
			{
				Settings.Patterns = PatternSet.Load(filepath);
				m_recent_pattern_sets.Add(filepath);
			}
			catch (Exception ex)
			{
				MsgBox.Show(this, "Could not load pattern set.\r\n{0}".Fmt(ex.Message), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>Save the current patterns as a pattern set</summary>
		private void SavePatternSet(string filepath)
		{
			// Prompt for a file if not given
			if (!filepath.HasValue())
			{
				using (var dlg = new SaveFileDialog { Title = "Save Pattern Set", Filter = Constants.PatternSetFilter, InitialDirectory = Settings.PatternSetDirectory })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					Settings.PatternSetDirectory = Path_.Directory(dlg.FileName);
					filepath = dlg.FileName;
				}
			}

			try
			{
				Settings.Patterns.Save(filepath);
				m_recent_pattern_sets.Add(filepath, true);
			}
			catch (Exception ex)
			{
				MsgBox.Show(this, "Could not create a pattern set from the current patterns.\r\n{0}".Fmt(ex.Message), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>Import patterns from a set</summary>
		private void ImportPatterns(string filepath)
		{
			// Prompt for a file if not given
			if (!filepath.HasValue())
			{
				using (var dlg = new OpenFileDialog { Title = "Import Patterns from Set", Filter = Constants.PatternSetFilter, InitialDirectory = Settings.PatternSetDirectory })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					Settings.PatternSetDirectory = Path_.Directory(dlg.FileName);
					filepath = dlg.FileName;
				}
			}

			try
			{
				var set = PatternSet.Load(filepath);
				using (var dlg = new PatternSetUI(set) { StartPosition = FormStartPosition.CenterParent })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK)
						 return;

					var patns = Settings.Patterns;
					if (set.Highlights.As<IFeatureTreeItem>().Allowed)
					{
						foreach (var hl in set.Highlights.Where(x => x.As<IFeatureTreeItem>().Allowed))
							patns.Highlights.AddIfUnique(hl);
					}
					if (set.Filters.As<IFeatureTreeItem>().Allowed)
					{
						foreach (var ft in set.Filters.Where(x => x.As<IFeatureTreeItem>().Allowed))
							patns.Filters.AddIfUnique(ft);
					}
					if (set.Transforms.As<IFeatureTreeItem>().Allowed)
					{
						foreach (var tx in set.Transforms.Where(x => x.As<IFeatureTreeItem>().Allowed))
							patns.Transforms.AddIfUnique(tx);
					}
					if (set.Actions.As<IFeatureTreeItem>().Allowed)
					{
						foreach (var ac in set.Actions.Where(x => x.As<IFeatureTreeItem>().Allowed))
							patns.Actions.AddIfUnique(ac);
					}
					Settings.Save();
				}
			}
			catch (Exception ex)
			{
				MsgBox.Show(this, "Failed to import patterns\r\n{0}".Fmt(ex.Message), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>Checks each file in 'filepaths' is valid. Returns false and displays an error if not</summary>
		private bool ValidateFilepaths(IEnumerable<string> filepaths)
		{
			foreach (var file in filepaths)
			{
				// Reject invalid file paths
				if (!file.HasValue())
				{
					MsgBox.Show(this, "File path is invalid", Application.ProductName, MessageBoxButtons.OK,MessageBoxIcon.Error);
					return false;
				}

				// Check that the file exists, this can take ages if 'filepath' is a network file
				if (!Misc.FileExists(this, file))
				{
					if (m_recent_logfiles.IsInRecents(file))
					{
						var res = MsgBox.Show(this, "File path '{0}' is invalid or does not exist\r\n\r\nRemove from recent files list?".Fmt(file), Application.ProductName, MessageBoxButtons.YesNo,MessageBoxIcon.Error);
						if (res == DialogResult.Yes)
							m_recent_logfiles.Remove(file, true);
					}
					else
					{
						MsgBox.Show(this, "File path '{0}' is invalid or does not exist".Fmt(file), Application.ProductName, MessageBoxButtons.OK,MessageBoxIcon.Error);
					}
					return false;
				}
			}
			return true;
		}

		/// <summary>Open a single log file, prompting if 'filepath' is null</summary>
		public void OpenSingleLogFile(string filepath, bool add_to_recent)
		{
			try
			{
				// Prompt for a file if none provided
				if (filepath == null)
				{
					var fd = new OpenFileDialog{Filter = Constants.LogFileFilter, Multiselect = false};
					if (fd.ShowDialog() != DialogResult.OK) return;
					filepath = fd.FileName;
				}

				// Check the filepath is valid
				if (!ValidateFilepaths(new[] { filepath }))
					return;

				// Add the file to the recent files list
				if (add_to_recent)
				{
					m_recent_logfiles.Add(filepath);
					Settings.LastLoadedFile = filepath;
				}

				// Switch files - open the file to make sure it's accessible (and to hold a lock)
				Src = new SingleFile(filepath);
			}
			catch (Exception ex)
			{
				Misc.ShowMessage(this, "Failed to open file {0} due to an error.".Fmt(filepath), Application.ProductName, MessageBoxIcon.Error, ex);
				Src = null;
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
				Src = new AggregateFile(filepaths);
			}
			catch (Exception ex)
			{
				Misc.ShowMessage(this, "Failed to open aggregate log files due to an error.", Application.ProductName, MessageBoxIcon.Error, ex);
				Src = null;
			}
		}

		/// <summary>Show the aggregate log file wizard</summary>
		private void AggregateFileWizard()
		{
			using (var dg = new AggregateFilesUI(this))
			{
				if (dg.ShowDialog(this) != DialogResult.OK)
					return;

				var filepaths = dg.Filepaths.ToList();
				if (filepaths.Count == 0) return;
				if (filepaths.Count == 1)
					OpenSingleLogFile(filepaths[0], true);
				else
					OpenAggregateLogFile(filepaths);
			}
		}

		/// <summary>Called when the log file is noticed to have changed</summary>
		private void OnFileChanged()
		{
			long len = Src.Stream.Length;
			Log.Info(this, "File {0} changed. File length: {1}".Fmt(Src.Name, len));
			long filepos = AutoScrollTail ? Src.Stream.Length : m_filepos;
			bool reload  = Src.Stream.Length < m_fileend || !Settings.FileChangesAdditive;
			BuildLineIndex(filepos, reload);
		}

		/// <summary>Supply the grid with values</summary>
		private void CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			if (Src == null || GridEventsBlocked)
			{
				e.Value = null;
			}
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
			if (Bookmarks.Count != 0 && Bookmarks.BinarySearch(x => x.Position.CompareTo(line.LineStartAddr)) >= 0)
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
								fmt.SetMeasurableCharacterRanges(hl.Match(col.Text).Select(x => new CharacterRange(x.Begi, x.Sizei)).ToArray());
								foreach (var r in gfx.MeasureCharacterRanges(col.Text, cs.Font, cellbounds, fmt))
								{
									var bnd = r.GetBounds(gfx);
									gfx.SetClip(new RectangleF(bnd.X - 1f, cellbounds.Y, bnd.Width + 1f, cellbounds.Height) , CombineMode.Union);
								}

								// Paint the highlighted parts of the cell
								using (var b = new SolidBrush(hl.BackColour))
									gfx.FillRectangle(b, cellbounds);
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
					var hl = col.HL.Back();
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
								fmt.SetMeasurableCharacterRanges(hl.Match(col.Text).Select(x => new CharacterRange(x.Begi, x.Sizei)).ToArray());
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
			if (GridEventsBlocked || Src == null)
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

			// Can't move the cache on selection change because the selection is changed
			// when the cache moves (in a different message). This means the cache move has
			// to be handled by key down, mouse down, mouse move events.
			// LoadNearBoundary();

			// We need to invalidate the selected rows because of the selection border.
			// Without this bits of the selection border get left behind because the rendering
			// process goes:
			// Select row 2 (say) -> draws top and bottom border because row 1 and 3 aren't selected
			// Select row 3 -> draws row 3 but not row 2 because it hasn't changed (except it needs to
			// because row 3 is now selected so the bottom border should not be draw for row 2).
			foreach (var r in m_grid.GetRowsWithState(DataGridViewElementStates.Displayed|DataGridViewElementStates.Selected))
				m_grid.InvalidateRow(r.Index);

			// Update the status bar (after current row has been set)
			this.BeginInvoke(UpdateStatus);

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
				try { a.Execute(text, Src.FilepathAt(line.LineStartAddr)); }
				catch { SetTransientStatusMessage("Action Failed", Color.Red, SystemColors.Control); }
				break;
			}
		}

		/// <summary>Handle key presses on the grid</summary>
		private void GridKeyDown(object s, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Up || e.KeyCode == Keys.PageUp)
				LoadNearBoundary();
			if (e.KeyCode == Keys.Down || e.KeyCode == Keys.PageDown)
				LoadNearBoundary();
		}

		/// <summary>Handler for mouse down/up events on the grid</summary>
		private void GridMouseButton(MouseEventArgs args, bool button_down)
		{
			switch (args.Button)
			{
			case MouseButtons.Left:
				if (button_down)
				{
					m_move_cache_on_mouse_up = true;
				}
				else
				{
					if (m_move_cache_on_mouse_up)
						LoadNearBoundary();
				}
				break;
			case MouseButtons.Right:
				if (button_down && SelectedRowCount <= 1)
				{
					var hit = m_grid.HitTest(args.X, args.Y);
					if (hit.RowIndex.Within(0, m_grid.RowCount))
						SelectedRowIndex = hit.RowIndex;
				}
				break;
			}
		}
		private bool m_move_cache_on_mouse_up;

		/// <summary>Handler for mouse move events on the grid</summary>
		private void GridMouseMove(MouseEventArgs a)
		{
			if (m_grid.RowCount == 0)
				return;

			// The grid captures the mouse during mouse selection
			if (a.Button == MouseButtons.Left)
			{
				var row0 = m_grid.Rows[0];
				var row1 = m_grid.Rows[m_grid.RowCount-1];
				if (row0.Selected || row1.Selected)
				{
					LoadNearBoundary();
					m_move_cache_on_mouse_up = false;
				}
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
			if (args.ClickedItem == m_cmenu_copy      ) { DataGridViewEx.Copy(m_grid);      return; }
			if (args.ClickedItem == m_cmenu_select_all) { DataGridViewEx.SelectAll(m_grid); return; }
			if (args.ClickedItem == m_cmenu_clear_log ) { ClearLogFile(); return; }

			if (args.ClickedItem == m_cmenu_highlight_row) { ShowOptions(SettingsUI.ETab.Highlights); return; }
			if (args.ClickedItem == m_cmenu_filter_row   ) { ShowOptions(SettingsUI.ETab.Filters   ); return; }
			if (args.ClickedItem == m_cmenu_transform_row) { ShowOptions(SettingsUI.ETab.Transforms); return; }
			if (args.ClickedItem == m_cmenu_action_row   ) { ShowOptions(SettingsUI.ETab.Actions   ); return; }

			// Find operations
			if (args.ClickedItem == m_cmenu_find_next) { FindUI.Pattern.Expr = ReadLine(hit.RowIndex).RowText; FindUI.RaiseFindNext(false); return; }
			if (args.ClickedItem == m_cmenu_find_prev) { FindUI.Pattern.Expr = ReadLine(hit.RowIndex).RowText; FindUI.RaiseFindPrev(false); return; }

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

			// Move the cache if the first or last row becomes visible
			LoadNearBoundary();

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
			long pos = (range.Beg == 0) ? 0 : (range.End == m_fileend) ? m_fileend : range.Mid;
			Log.Info(this, "file scroll to {0}".Fmt(pos));

			// Set the new selected row from the mouse up position
			var pt = m_scroll_file.PointToClient(MousePosition);
			var sel_pos = (long)(Maths.Frac(1, pt.Y, m_scroll_file.Height - 1) * FileByteRange.Size);
			BuildLineIndex(pos, false, () => { SelectRowByAddr(sel_pos); });
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
			var ofs = has_selection ? SelectedRowByteRange.Beg : -1;

			Settings.QuickFilterEnabled = enable;
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
			var ofs = has_selection && bli_needed ? SelectedRowByteRange.Beg : -1;

			Settings.HighlightsEnabled = enable;

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
			var ofs = has_selection ? SelectedRowByteRange.Beg : -1;

			Settings.FiltersEnabled = enable;
			BuildLineIndex(m_filepos, true, () =>
			{
				if (has_selection)
					SelectRowByAddr(ofs);
			});
		}

		/// <summary>Turn on/off transforms</summary>
		public void EnableTransforms(bool enable)
		{
			Settings.TransformsEnabled = enable;
			BuildLineIndex(m_filepos, true);
		}

		/// <summary>Turn on/off actions</summary>
		public void EnableActions(bool enabled)
		{
			Settings.ActionsEnabled = enabled;
		}

		/// <summary>Jumps to a specific byte offset into the file</summary>
		private void JumpTo()
		{
			var dlg = new JumpToUi(this, FileByteRange.Beg, FileByteRange.End);
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
		}

		/// <summary>Turn on/off tail mode</summary>
		public void EnableWatch(bool enable)
		{
			Settings.WatchEnabled = enable;
			if (enable)
				BuildLineIndex(m_filepos, Settings.FileChangesAdditive);
		}

		/// <summary>Turn on/off additive only mode</summary>
		public void EnableAdditive(bool enable)
		{
			Settings.FileChangesAdditive = enable;
			if (!enable)
				BuildLineIndex(m_filepos, true);
		}

		/// <summary>Try to remove data from the log file</summary>
		private void ClearLogFile()
		{
			var err = Src.Clear();
			if (err == null)
			{
				InvalidateCache();
			}
			else
			{
				MsgBox.Show(this, string.Format(
					"Clearing file {0} failed.\r\n" +
					"{1}\r\n" +
					"\r\n" +
					"Usually clearing the log file fails if another application holds an " +
					"exclusive lock on the file. Stop any processes that are using the file " +
					"and try again. Note, if you are using 'Log Program Output', the program " +
					"you are running may be holding the file lock."
					,Src.Name
					,err.Message)
					,Application.ProductName
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
			//using (EventsSnapshot.Capture(Settings, EventsSnapshot.Restore.AssertNoChange)) // Prevent 'Settings' holding references to 'ui'
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
			const string HelpStartPage = @"docs\help.html";
			try
			{
				var path = Util.ResolveAppPath(HelpStartPage);
				Process.Start(path);
			}
			catch (Exception ex)
			{
				MsgBox.Show(this,
					"Unable to display the help documentation.\r\n" +
					"Error Message: {0}\r\n".Fmt(ex.Message) +
					"\r\n" +
					"The expected location of the main documentation file is:\r\n" +
					"  <install directory>\\"+HelpStartPage,
					"Missing help files", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>Show the first run tutorial</summary>
		private void ShowFirstRunTutorial()
		{
			try
			{
				var tut = new FirstRunTutorial(this);
				tut.Show();
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
			//using (EventsSnapshot.Capture(Settings, EventsSnapshot.Restore.AssertNoChange))
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

			var update_url = Settings.CheckForUpdatesServer + "rylogviewer/latest_version.xml";

			// Start the check for updates
			if (show_dialog)
			{
				var dlg = new ProgressForm("Checking for Updates", "Querying the server for latest version information...", null, ProgressBarStyle.Marquee, (s,a,cb)=>
				{
					cb(new ProgressForm.UserState{ProgressBarStyle = ProgressBarStyle.Marquee, Icon = Icon});
					var async = INet.BeginCheckForUpdate(Constants.AppIdentifier, update_url, null, Proxy);

					// Wait till the operation completes, or until cancel is signalled
					for (;!s.CancelPending && !async.AsyncWaitHandle.WaitOne(500);) {}

					if (!s.CancelPending) callback(async);
					else INet.CancelCheckForUpdate(async);
				});
				using (dlg)
					dlg.ShowDialog(this);
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
				if (show_dialog) Misc.ShowMessage(this, "Check for updates failed", Application.ProductName, MessageBoxIcon.Error, error);
			}
			else
			{
				Version this_version, othr_version;
				try
				{
					this_version = new Version(Util.AppVersion);
					othr_version = new Version(res.Version);
				}
				catch (Exception)
				{
					SetTransientStatusMessage("Version Information Unavailable");
					if (show_dialog) MsgBox.Show(this, "The server was contacted but version information was not available", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
				if (this_version.CompareTo(othr_version) >  0)
				{
					SetTransientStatusMessage("Development version running");
					if (show_dialog) MsgBox.Show(this, "This version is newer than the latest version", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else if (this_version.CompareTo(othr_version) == 0)
				{
					SetTransientStatusMessage("Latest version running");
					if (show_dialog) MsgBox.Show(this, "This is the latest version", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					SetTransientStatusMessage("New Version Available!", Color.Green, SystemColors.Control);
					if (show_dialog)
					{
						using (var dg = new NewVersionForm(this_version.ToString(), othr_version.ToString(), res.InfoURL, res.DownloadURL, Proxy))
							dg.ShowDialog(this);
					}
				}
			}
		}

		/// <summary>Show the about dialog</summary>
		private void ShowAbout()
		{
			new About(StartupOptions).ShowDialog(this);
		}

		/// <summary>Display info about the app being a free version</summary>
		private void ShowFreeVersionInfo(object sender = null, EventArgs args = null)
		{
			using (var dlg = new HelpUI(this, HelpUI.EContent.Html, Application.ProductName, Resources.free_version, Point.Empty, new Size(480, 640), ToolForm.EPin.Centre, modal: true))
			{
				dlg.FormBorderStyle = FormBorderStyle.Sizable;
				dlg.Html.ResolveContent += (s,a) =>
				{
					switch (a.Url.ToString())
					{
					case Cmd.visit_store:
						VisitWebSite();
						dlg.Close();
						break;
					}
				};
				dlg.ShowDialog(this);
			}
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

		/// <summary>Tests whether the currently selected row is near the start or end of the cached line range and causes a reload if it is</summary>
		private void LoadNearBoundary()
		{
			// Want to be able to drag select across a cache move.
			if (m_grid.RowCount == 0)
				return;

			// The centre of the new cache range
			var centre_row = -1;

			// If the displayed row count is less than half the cache size scroll
			// when the first/last row is selected. This prevents the selected row "jumping".
			if (m_line_index.Count / 2 < m_grid.DisplayedRowCount(true))
			{
				var selected = SelectedRowIndex;
				if (selected == 0 && LineIndexRange.Beg > m_encoding.GetPreamble().Length)
					centre_row = 0;
				if (selected == m_grid.RowCount-1 && LineIndexRange.End < m_fileend - m_row_delim.Length)
					centre_row = m_line_index.Count-1;
			}
			// Otherwise, scroll when the first/last row is displayed
			else
			{
				var first = m_grid.FirstDisplayedScrollingRowIndex;
				var last  = first + m_grid.DisplayedRowCount(false) - 1;
				if (first == 0 && LineIndexRange.Beg > m_encoding.GetPreamble().Length)
					centre_row = 0;
				if (last >= m_grid.RowCount-1 && LineIndexRange.End < m_fileend - m_row_delim.Length)
					centre_row = m_line_index.Count-1;
			}

			// Not moving the cache?
			if (centre_row == -1L)
				return;

			// Detect when mouse selection is happening
			var hti = m_grid.HitTestEx(m_grid.PointToClient(MousePosition));

			// We don't want to move the cache if doing so will cause selected rows to move out of memory.
			var sel_range = m_grid.SelectedRowIndexRange();
			var line_range = CalcLineRange(Settings.LineCacheCount);
			var limit_cache_move = sel_range.Count > 1 ||
				(MouseButtons == MouseButtons.Left && hti.Type != DataGridViewHitTestType.HorizontalScrollBar && hti.Type != DataGridViewHitTestType.VerticalScrollBar);

			// Clamp the cache move
			if (limit_cache_move)
			{
				if (centre_row == m_line_index.Count-1) centre_row = (int)Math.Min(m_line_index.Count-1, sel_range.Beg + line_range.Begi    ); // shift cache centre down
				if (centre_row == 0                   ) centre_row = (int)Math.Max(0                   , sel_range.End - line_range.Endi + 1); // shift cache centre up
			}

			// Move the cache range
			var cache_centre = m_line_index[centre_row].Beg;
			BuildLineIndex(cache_centre, false);
		}

		/// <summary>Helper for setting the grid row count without event handlers being fired</summary>
		private void SetGridRowCount(int count, int row_delta)
		{
			bool auto_scroll_tail = AutoScrollTail;
			if (m_grid.RowCount != count || row_delta != 0)
			{
				Log.Info(this, "RowCount changed {0} -> {1}.".Fmt(m_grid.RowCount, count));
				Log.Info(this, "Row delta {0}.".Fmt(row_delta));

				// Record data so that we can preserve the selected rows and first visible rows
				var selected_rows = m_grid.SelectedRowIndices().ToArray();
				int first_vis     = m_grid.FirstDisplayedScrollingRowIndex;
				var cell_addr     = m_grid.CurrentCellAddress;
				var anchor        = m_grid.SelectionAnchorCell;
				var selected      = SelectedRowIndex;

				// Clear selection
				SelectedRowIndex  = -1;

				// We need to set the row count even if it hasn't changed, because doing so
				// has the side effect of the grid recalculating it's scroll bar sizes from
				// the (now possibly different) row heights.
				var row_diff = count - m_grid.RowCount;
				using (m_grid.SuspendLayout(true))
				{
					m_grid.RowCount = 0;
					m_grid.RowCount = count;
				}

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
						// If this is during a mouse drag selection, select from the anchor to the mouse pointer
						if (MouseButtons == MouseButtons.Left && m_grid.Capture)
						{
							// Clamp the mouse point to within the horizontal range of the grid.
							var pt = new Point(m_grid.RightToLeft == RightToLeft.Yes ? m_grid.Width-1 : 1, m_grid.PointToClient(MousePosition).Y);

							// Adjust the anchor row
							var anchor_y = Maths.Clamp(anchor.Y + row_delta, 0, count-1);

							// Find the row under the mouse (or nearest to it)
							var hti = m_grid.HitTestEx(pt);
							var row_index = hti.RowIndex.Within(0, count) ? hti.RowIndex : pt.Y < 0 ? 0 : count - 1;

							// Clear the selection and set the current cell
							m_grid.SelectSingleRow(row_index);
							m_grid.SelectionAnchorCell = new Point(anchor.X, anchor_y);
							m_grid.TrackRow = anchor_y;
							m_grid.TrackRowEdge = row_index;

							// Select rows from the anchor row to the current mouse position
							var selfirst = Math.Min(anchor_y,  row_index);
							var selcount = Math.Abs(anchor_y - row_index) + 1;
							using (m_grid.SuspendSelectionChanged())
								for (var i = 0; i != selcount; ++i)
									m_grid.Rows[selfirst + i].Selected = true;
						}
						else
						{
							// Clear the selection and set the current row
							m_grid.SelectSingleRow(selected + row_delta);

							// Select the rows that were previously selected.
							selected_rows.Sort();
							var ibeg = selected_rows.BinarySearch(x => (x + row_delta).CompareTo(    0), find_insert_position:true);
							var iend = selected_rows.BinarySearch(x => (x + row_delta).CompareTo(count), find_insert_position:true);
							using (m_grid.SuspendSelectionChanged())
								for (var i = ibeg; i != iend; ++i)
									m_grid.Rows[selected_rows[i] + row_delta].Selected = true;
						}

						// Restore the first visible row after setting the current selected row, because
						// changing the 'CurrentCell' also changes the scroll position
						if (first_vis != -1)
							m_grid.FirstDisplayedScrollingRowIndex = Maths.Clamp(first_vis + row_delta, 0, m_grid.RowCount - 1);
					}
				}

				// If the number of rows added to the grid is not equal to the row delta
				// then the oddness of grid row zero depends on the difference
				if (row_diff != -row_delta)
				{
					// Give the illusion that the alternating row colour is moving with the overall file
					var row_zero_shift = Math.Max(-row_delta - row_diff, 0);
					if ((row_zero_shift & 1) == 1)
						m_first_row_is_odd = !m_first_row_is_odd;
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

			// UI options
			TopMost = Settings.AlwaysOnTop;

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
			if (WatchTimer != null)
			{
				WatchTimer.Enabled = Src != null && Settings.WatchEnabled;
			}

			// Highlights;
			m_highlights.Clear();
			if (Settings.HighlightsEnabled)
			{
				m_highlights.AddRange(Settings.Patterns.Highlights.Where(x => x.Active));
				UseLicensedFeature(FeatureName.Highlighting, new HighlightingCountLimiter(this));
			}

			// Filters
			m_filters.Clear();
			if (Settings.FiltersEnabled)
			{
				m_filters.AddRange(Settings.Patterns.Filters.Where(x => x.Active));
				UseLicensedFeature(FeatureName.Filtering, new FilteringCountLimiter(this));
			}

			// Transforms
			m_transforms.Clear();
			if (Settings.TransformsEnabled)
			{
				m_transforms.AddRange(Settings.Patterns.Transforms.Where(x => x.Active));
			}

			// Click Actions
			m_clkactions.Clear();
			if (Settings.ActionsEnabled)
			{
				m_clkactions.AddRange(Settings.Patterns.Actions.Where(x => x.Active));
			}

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

		/// <summary>Update the status bar</summary>
		private void UpdateStatus()
		{
			if (Src == null || m_grid.RowCount == 0 || m_grid.RowCount != m_line_index.Count)
			{
				Text = Application.ProductName;
				m_status_spring.Text = "No File or Data Source";
				m_status_filesize.Visible = false;
				m_status_line_end.Visible = false;
				m_status_encoding.Visible = false;
				UpdateStatusProgress(0,0);
			}
			else
			{
				Text = "{0} - {1}".Fmt(Settings.FullPathInTitle ? Src.PsuedoFilepath : Src.Name, Application.ProductName);
				m_status_spring.Text = string.Empty;

				// Get current file position / selection
				var r = SelectedRowIndex;
				var pos = (r != -1) ? m_line_index[r].Beg : 0;
				m_status_filesize.Text = "Position: {0:N0} / {1:N0} bytes".Fmt(pos, FileByteRange.End);
				m_status_filesize.Visible = true;

				// Selection
				var sel_range = m_grid.SelectedRowIndexRange();
				var rg = (r != -1) ? new Range(m_line_index[sel_range.Begi].Beg, m_line_index[sel_range.Endi].End) : SelectedRowByteRange;
				m_status_selection.Text = "Selection: [{0:N0} - {1:N0}] ({2} bytes)".Fmt(rg.Beg, rg.End, rg.Size);
				m_status_selection.Visible = true;

				// Line ending characters
				m_status_line_end.Text = "Line Ending: {0}".Fmt(m_row_delim == null ? "unknown" : Misc.Humanise(m_encoding.GetString(m_row_delim)));
				m_status_line_end.Visible = true;

				// Encoding characters
				m_status_encoding.Text = "Encoding: {0}".Fmt(m_encoding.EncodingName);
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
				Log.Info(this, "File scroll set to [{0},{1}) within file [{2},{3})".Fmt(range.Beg, range.End, FileByteRange.Beg, FileByteRange.End));

			m_scroll_file.TotalRange = FileByteRange;
			m_scroll_file.ThumbRange = range;
			m_scroll_file.Width      = Settings.FileScrollWidth;

			m_scroll_file.ClearIndicatorRanges();
			m_scroll_file.AddIndicatorRange(DisplayedRowsRange, Settings.ScrollBarDisplayRangeColour);
			foreach (var sel_range in SelectedRowRanges)
				m_scroll_file.AddIndicatorRange(sel_range, Settings.LineSelectBackColour);

			// Add marks for the bookmarked positions
			var bkmark_colour = Settings.BookmarkColour;
			foreach (var bk in Bookmarks)
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
			m_status_message_trans.SetStatusMessage(msg:text, fr_color:frcol, bk_color:bkcol, display_time:display_time_ms);
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
			m_status_message_fixed.SetStatusMessage(msg:text, fr_color:frcol, bk_color:bkcol);
			//m_status_message_fixed.Text = text ?? string.Empty;
			//m_status_message_fixed.Visible = text.HasValue();
			//m_status_message_fixed.ForeColor = frcol;
			//m_status_message_fixed.BackColor = bkcol;
		}
		public void SetStaticStatusMessage(string text)
		{
			SetStaticStatusMessage(text, SystemColors.ControlText, SystemColors.Control);
		}

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

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
			this.m_toolstrip = new System.Windows.Forms.ToolStrip();
			this.m_btn_open_log = new System.Windows.Forms.ToolStripButton();
			this.m_btn_refresh = new System.Windows.Forms.ToolStripButton();
			this.m_btn_quick_filter = new System.Windows.Forms.ToolStripButton();
			this.m_sep = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_highlights = new System.Windows.Forms.ToolStripButton();
			this.m_btn_filters = new System.Windows.Forms.ToolStripButton();
			this.m_btn_transforms = new System.Windows.Forms.ToolStripButton();
			this.m_btn_actions = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_find = new System.Windows.Forms.ToolStripButton();
			this.m_btn_bookmarks = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_jump_to_start = new System.Windows.Forms.ToolStripButton();
			this.m_btn_jump_to_end = new System.Windows.Forms.ToolStripButton();
			this.m_btn_tail = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_watch = new System.Windows.Forms.ToolStripButton();
			this.m_btn_additive = new System.Windows.Forms.ToolStripButton();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_data_sources = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open_stdout = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open_serial_port = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open_network = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open_named_pipe = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator19 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_wizards_aggregatelogfile = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_wizards_androidlogcat = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_recent = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator18 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_close = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator16 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_export = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_pattern_set = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_recent_pattern_sets = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator20 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_load_pattern_set = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_save_pattern_set = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator21 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_import_patterns = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_selectall = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_copy = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep3 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_edit_find = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_find_next = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_find_prev = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_edit_jumpto = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator17 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_edit_toggle_bookmark = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_next_bookmark = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_prev_bookmark = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_clearall_bookmarks = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_edit_bookmarks = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_encoding = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_encoding_detect = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_encoding_ascii = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_encoding_utf8 = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_encoding_ucs2_littleendian = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_encoding_ucs2_bigendian = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_line_ending = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_line_ending_detect = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_line_ending_cr = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_line_ending_crlf = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_line_ending_lf = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_line_ending_custom = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_alwaysontop = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_monitor_mode = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep4 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_tools_clear_log_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep5 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_tools_highlights = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_filters = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_transforms = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_actions = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_tools_options = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_help = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_help_view_help = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_help_firstruntutorial = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_help_totd = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_help_visit_web_site = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_help_register = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_help_check_for_updates = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_help_about = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_free_version = new System.Windows.Forms.ToolStripMenuItem();
			this.m_status = new System.Windows.Forms.StatusStrip();
			this.m_status_filesize = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_status_line_end = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_status_encoding = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_status_spring = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_status_progress = new System.Windows.Forms.ToolStripProgressBar();
			this.m_status_message_trans = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_status_message_fixed = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_toolstrip_cont = new System.Windows.Forms.ToolStripContainer();
			this.m_table = new System.Windows.Forms.TableLayoutPanel();
			this.m_cmenu_grid = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.m_cmenu_select_all = new System.Windows.Forms.ToolStripMenuItem();
			this.m_cmenu_copy = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator15 = new System.Windows.Forms.ToolStripSeparator();
			this.m_cmenu_clear_log = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.m_cmenu_highlight_row = new System.Windows.Forms.ToolStripMenuItem();
			this.m_cmenu_filter_row = new System.Windows.Forms.ToolStripMenuItem();
			this.m_cmenu_transform_row = new System.Windows.Forms.ToolStripMenuItem();
			this.m_cmenu_action_row = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.m_cmenu_find_next = new System.Windows.Forms.ToolStripMenuItem();
			this.m_cmenu_find_prev = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
			this.m_cmenu_toggle_bookmark = new System.Windows.Forms.ToolStripMenuItem();
			this.m_grid = new RyLogViewer.DataGridView();
			this.m_scroll_file = new pr.gui.SubRangeScroll();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_status_selection = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_toolstrip.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.m_status.SuspendLayout();
			this.m_toolstrip_cont.BottomToolStripPanel.SuspendLayout();
			this.m_toolstrip_cont.ContentPanel.SuspendLayout();
			this.m_toolstrip_cont.TopToolStripPanel.SuspendLayout();
			this.m_toolstrip_cont.SuspendLayout();
			this.m_table.SuspendLayout();
			this.m_cmenu_grid.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.SuspendLayout();
			// 
			// m_toolstrip
			// 
			this.m_toolstrip.Dock = System.Windows.Forms.DockStyle.None;
			this.m_toolstrip.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.m_toolstrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_btn_open_log,
            this.m_btn_refresh,
            this.m_btn_quick_filter,
            this.m_sep,
            this.m_btn_highlights,
            this.m_btn_filters,
            this.m_btn_transforms,
            this.m_btn_actions,
            this.toolStripSeparator9,
            this.m_btn_find,
            this.m_btn_bookmarks,
            this.toolStripSeparator1,
            this.m_btn_jump_to_start,
            this.m_btn_jump_to_end,
            this.m_btn_tail,
            this.toolStripSeparator8,
            this.m_btn_watch,
            this.m_btn_additive});
			this.m_toolstrip.Location = new System.Drawing.Point(3, 24);
			this.m_toolstrip.Name = "m_toolstrip";
			this.m_toolstrip.Size = new System.Drawing.Size(428, 31);
			this.m_toolstrip.TabIndex = 0;
			// 
			// m_btn_open_log
			// 
			this.m_btn_open_log.AutoSize = false;
			this.m_btn_open_log.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_open_log.Image = global::RyLogViewer.Properties.Resources.folder_with_file;
			this.m_btn_open_log.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_open_log.Margin = new System.Windows.Forms.Padding(0);
			this.m_btn_open_log.Name = "m_btn_open_log";
			this.m_btn_open_log.Size = new System.Drawing.Size(28, 28);
			this.m_btn_open_log.Text = "Open Log File";
			// 
			// m_btn_refresh
			// 
			this.m_btn_refresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_refresh.Image = global::RyLogViewer.Properties.Resources.Refresh;
			this.m_btn_refresh.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_refresh.Name = "m_btn_refresh";
			this.m_btn_refresh.Size = new System.Drawing.Size(28, 28);
			this.m_btn_refresh.Text = "Refresh";
			// 
			// m_btn_quick_filter
			// 
			this.m_btn_quick_filter.CheckOnClick = true;
			this.m_btn_quick_filter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_quick_filter.Image = global::RyLogViewer.Properties.Resources.quick_filter;
			this.m_btn_quick_filter.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_quick_filter.Name = "m_btn_quick_filter";
			this.m_btn_quick_filter.Size = new System.Drawing.Size(28, 28);
			this.m_btn_quick_filter.Text = "Quick Filter";
			// 
			// m_sep
			// 
			this.m_sep.Name = "m_sep";
			this.m_sep.Size = new System.Drawing.Size(6, 31);
			// 
			// m_btn_highlights
			// 
			this.m_btn_highlights.CheckOnClick = true;
			this.m_btn_highlights.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_highlights.Image = global::RyLogViewer.Properties.Resources.highlight;
			this.m_btn_highlights.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_highlights.Name = "m_btn_highlights";
			this.m_btn_highlights.Size = new System.Drawing.Size(28, 28);
			this.m_btn_highlights.Text = "Highlights";
			// 
			// m_btn_filters
			// 
			this.m_btn_filters.CheckOnClick = true;
			this.m_btn_filters.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_filters.Image = global::RyLogViewer.Properties.Resources.filter;
			this.m_btn_filters.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_filters.Name = "m_btn_filters";
			this.m_btn_filters.Size = new System.Drawing.Size(28, 28);
			this.m_btn_filters.Text = "Filters";
			// 
			// m_btn_transforms
			// 
			this.m_btn_transforms.CheckOnClick = true;
			this.m_btn_transforms.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_transforms.Image = global::RyLogViewer.Properties.Resources.exchange;
			this.m_btn_transforms.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_transforms.Name = "m_btn_transforms";
			this.m_btn_transforms.Size = new System.Drawing.Size(28, 28);
			this.m_btn_transforms.Text = "Transforms";
			// 
			// m_btn_actions
			// 
			this.m_btn_actions.CheckOnClick = true;
			this.m_btn_actions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_actions.Image = global::RyLogViewer.Properties.Resources.execute;
			this.m_btn_actions.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_actions.Name = "m_btn_actions";
			this.m_btn_actions.Size = new System.Drawing.Size(28, 28);
			this.m_btn_actions.Text = "Actions";
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(6, 31);
			// 
			// m_btn_find
			// 
			this.m_btn_find.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_find.Image = global::RyLogViewer.Properties.Resources.find_search;
			this.m_btn_find.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_find.Name = "m_btn_find";
			this.m_btn_find.Size = new System.Drawing.Size(28, 28);
			this.m_btn_find.Text = "toolStripButton1";
			// 
			// m_btn_bookmarks
			// 
			this.m_btn_bookmarks.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_bookmarks.Image = global::RyLogViewer.Properties.Resources.bookmark;
			this.m_btn_bookmarks.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_bookmarks.Name = "m_btn_bookmarks";
			this.m_btn_bookmarks.Size = new System.Drawing.Size(28, 28);
			this.m_btn_bookmarks.Text = "Bookmarks";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 31);
			// 
			// m_btn_jump_to_start
			// 
			this.m_btn_jump_to_start.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_jump_to_start.Image = global::RyLogViewer.Properties.Resources.green_up;
			this.m_btn_jump_to_start.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_jump_to_start.Name = "m_btn_jump_to_start";
			this.m_btn_jump_to_start.Size = new System.Drawing.Size(28, 28);
			this.m_btn_jump_to_start.Text = "File Start";
			this.m_btn_jump_to_start.ToolTipText = "Jump to the file start";
			// 
			// m_btn_jump_to_end
			// 
			this.m_btn_jump_to_end.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_jump_to_end.Image = global::RyLogViewer.Properties.Resources.green_down;
			this.m_btn_jump_to_end.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.m_btn_jump_to_end.Name = "m_btn_jump_to_end";
			this.m_btn_jump_to_end.Size = new System.Drawing.Size(28, 28);
			this.m_btn_jump_to_end.Text = "File End";
			this.m_btn_jump_to_end.ToolTipText = "Jump to the file end";
			// 
			// m_btn_tail
			// 
			this.m_btn_tail.CheckOnClick = true;
			this.m_btn_tail.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_tail.Image = global::RyLogViewer.Properties.Resources.bottom;
			this.m_btn_tail.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_tail.Name = "m_btn_tail";
			this.m_btn_tail.Size = new System.Drawing.Size(28, 28);
			this.m_btn_tail.Text = "Tail Mode";
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(6, 31);
			// 
			// m_btn_watch
			// 
			this.m_btn_watch.CheckOnClick = true;
			this.m_btn_watch.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_watch.Image = global::RyLogViewer.Properties.Resources.Eyeball;
			this.m_btn_watch.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_watch.Name = "m_btn_watch";
			this.m_btn_watch.Size = new System.Drawing.Size(28, 28);
			this.m_btn_watch.Text = "Live Update";
			// 
			// m_btn_additive
			// 
			this.m_btn_additive.CheckOnClick = true;
			this.m_btn_additive.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_additive.Image = global::RyLogViewer.Properties.Resources.edit_add;
			this.m_btn_additive.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_additive.Name = "m_btn_additive";
			this.m_btn_additive.Size = new System.Drawing.Size(28, 28);
			this.m_btn_additive.Text = "Additive Only";
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file,
            this.m_menu_edit,
            this.m_menu_encoding,
            this.m_menu_line_ending,
            this.m_menu_tools,
            this.m_menu_help,
            this.m_menu_free_version});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(835, 24);
			this.m_menu.TabIndex = 1;
			this.m_menu.Text = "m_menu";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_open,
            this.m_menu_file_data_sources,
            this.m_menu_file_recent,
            this.toolStripSeparator18,
            this.m_menu_file_close,
            this.toolStripSeparator16,
            this.m_menu_file_export,
            this.m_sep1,
            this.m_menu_file_pattern_set,
            this.m_sep2,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_open
			// 
			this.m_menu_file_open.Name = "m_menu_file_open";
			this.m_menu_file_open.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.m_menu_file_open.Size = new System.Drawing.Size(190, 22);
			this.m_menu_file_open.Text = "&Open Log File";
			// 
			// m_menu_file_data_sources
			// 
			this.m_menu_file_data_sources.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_open_stdout,
            this.m_menu_file_open_serial_port,
            this.m_menu_file_open_network,
            this.m_menu_file_open_named_pipe,
            this.toolStripSeparator19,
            this.m_menu_file_wizards_aggregatelogfile,
            this.m_menu_file_wizards_androidlogcat});
			this.m_menu_file_data_sources.Name = "m_menu_file_data_sources";
			this.m_menu_file_data_sources.Size = new System.Drawing.Size(190, 22);
			this.m_menu_file_data_sources.Text = "Data &Sources";
			// 
			// m_menu_file_open_stdout
			// 
			this.m_menu_file_open_stdout.Name = "m_menu_file_open_stdout";
			this.m_menu_file_open_stdout.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_open_stdout.Text = "Log &Program Output...";
			// 
			// m_menu_file_open_serial_port
			// 
			this.m_menu_file_open_serial_port.Name = "m_menu_file_open_serial_port";
			this.m_menu_file_open_serial_port.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_open_serial_port.Text = "Log &Serial Port...";
			// 
			// m_menu_file_open_network
			// 
			this.m_menu_file_open_network.Name = "m_menu_file_open_network";
			this.m_menu_file_open_network.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_open_network.Text = "Log Ne&twork Connection...";
			// 
			// m_menu_file_open_named_pipe
			// 
			this.m_menu_file_open_named_pipe.Name = "m_menu_file_open_named_pipe";
			this.m_menu_file_open_named_pipe.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_open_named_pipe.Text = "Log &Named Pipe...";
			// 
			// toolStripSeparator19
			// 
			this.toolStripSeparator19.Name = "toolStripSeparator19";
			this.toolStripSeparator19.Size = new System.Drawing.Size(213, 6);
			// 
			// m_menu_file_wizards_aggregatelogfile
			// 
			this.m_menu_file_wizards_aggregatelogfile.Name = "m_menu_file_wizards_aggregatelogfile";
			this.m_menu_file_wizards_aggregatelogfile.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_wizards_aggregatelogfile.Text = "A&ggregate Log File...";
			// 
			// m_menu_file_wizards_androidlogcat
			// 
			this.m_menu_file_wizards_androidlogcat.Name = "m_menu_file_wizards_androidlogcat";
			this.m_menu_file_wizards_androidlogcat.Size = new System.Drawing.Size(216, 22);
			this.m_menu_file_wizards_androidlogcat.Text = "&Android Logcat...";
			// 
			// m_menu_file_recent
			// 
			this.m_menu_file_recent.Name = "m_menu_file_recent";
			this.m_menu_file_recent.Size = new System.Drawing.Size(190, 22);
			this.m_menu_file_recent.Text = "&Recent Files";
			// 
			// toolStripSeparator18
			// 
			this.toolStripSeparator18.Name = "toolStripSeparator18";
			this.toolStripSeparator18.Size = new System.Drawing.Size(187, 6);
			// 
			// m_menu_file_close
			// 
			this.m_menu_file_close.Name = "m_menu_file_close";
			this.m_menu_file_close.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.m_menu_file_close.Size = new System.Drawing.Size(190, 22);
			this.m_menu_file_close.Text = "&Close Log";
			// 
			// toolStripSeparator16
			// 
			this.toolStripSeparator16.Name = "toolStripSeparator16";
			this.toolStripSeparator16.Size = new System.Drawing.Size(187, 6);
			// 
			// m_menu_file_export
			// 
			this.m_menu_file_export.Name = "m_menu_file_export";
			this.m_menu_file_export.Size = new System.Drawing.Size(190, 22);
			this.m_menu_file_export.Text = "&Export...";
			// 
			// m_sep1
			// 
			this.m_sep1.Name = "m_sep1";
			this.m_sep1.Size = new System.Drawing.Size(187, 6);
			// 
			// m_menu_file_pattern_set
			// 
			this.m_menu_file_pattern_set.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_recent_pattern_sets,
            this.toolStripSeparator20,
            this.m_menu_file_load_pattern_set,
            this.m_menu_file_save_pattern_set,
            this.toolStripSeparator21,
            this.m_menu_file_import_patterns});
			this.m_menu_file_pattern_set.Name = "m_menu_file_pattern_set";
			this.m_menu_file_pattern_set.Size = new System.Drawing.Size(190, 22);
			this.m_menu_file_pattern_set.Text = "Pattern Sets";
			// 
			// m_menu_file_recent_pattern_sets
			// 
			this.m_menu_file_recent_pattern_sets.Name = "m_menu_file_recent_pattern_sets";
			this.m_menu_file_recent_pattern_sets.Size = new System.Drawing.Size(119, 22);
			this.m_menu_file_recent_pattern_sets.Text = "Recent";
			// 
			// toolStripSeparator20
			// 
			this.toolStripSeparator20.Name = "toolStripSeparator20";
			this.toolStripSeparator20.Size = new System.Drawing.Size(116, 6);
			// 
			// m_menu_file_load_pattern_set
			// 
			this.m_menu_file_load_pattern_set.Name = "m_menu_file_load_pattern_set";
			this.m_menu_file_load_pattern_set.Size = new System.Drawing.Size(119, 22);
			this.m_menu_file_load_pattern_set.Text = "Load";
			// 
			// m_menu_file_save_pattern_set
			// 
			this.m_menu_file_save_pattern_set.Name = "m_menu_file_save_pattern_set";
			this.m_menu_file_save_pattern_set.Size = new System.Drawing.Size(119, 22);
			this.m_menu_file_save_pattern_set.Text = "Save";
			// 
			// toolStripSeparator21
			// 
			this.toolStripSeparator21.Name = "toolStripSeparator21";
			this.toolStripSeparator21.Size = new System.Drawing.Size(116, 6);
			// 
			// m_menu_file_import_patterns
			// 
			this.m_menu_file_import_patterns.Name = "m_menu_file_import_patterns";
			this.m_menu_file_import_patterns.Size = new System.Drawing.Size(119, 22);
			this.m_menu_file_import_patterns.Text = "Import...";
			// 
			// m_sep2
			// 
			this.m_sep2.Name = "m_sep2";
			this.m_sep2.Size = new System.Drawing.Size(187, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.m_menu_file_exit.Size = new System.Drawing.Size(190, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// m_menu_edit
			// 
			this.m_menu_edit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_edit_selectall,
            this.m_menu_edit_copy,
            this.m_sep3,
            this.m_menu_edit_find,
            this.m_menu_edit_find_next,
            this.m_menu_edit_find_prev,
            this.toolStripSeparator13,
            this.m_menu_edit_jumpto,
            this.toolStripSeparator17,
            this.m_menu_edit_toggle_bookmark,
            this.m_menu_edit_next_bookmark,
            this.m_menu_edit_prev_bookmark,
            this.m_menu_edit_clearall_bookmarks,
            this.m_menu_edit_bookmarks});
			this.m_menu_edit.Name = "m_menu_edit";
			this.m_menu_edit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F2)));
			this.m_menu_edit.Size = new System.Drawing.Size(39, 20);
			this.m_menu_edit.Text = "&Edit";
			// 
			// m_menu_edit_selectall
			// 
			this.m_menu_edit_selectall.Name = "m_menu_edit_selectall";
			this.m_menu_edit_selectall.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.m_menu_edit_selectall.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_selectall.Text = "Select &All";
			// 
			// m_menu_edit_copy
			// 
			this.m_menu_edit_copy.Name = "m_menu_edit_copy";
			this.m_menu_edit_copy.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.m_menu_edit_copy.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_copy.Text = "&Copy";
			// 
			// m_sep3
			// 
			this.m_sep3.Name = "m_sep3";
			this.m_sep3.Size = new System.Drawing.Size(255, 6);
			// 
			// m_menu_edit_find
			// 
			this.m_menu_edit_find.Name = "m_menu_edit_find";
			this.m_menu_edit_find.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this.m_menu_edit_find.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_find.Text = "&Find...";
			// 
			// m_menu_edit_find_next
			// 
			this.m_menu_edit_find_next.Name = "m_menu_edit_find_next";
			this.m_menu_edit_find_next.ShortcutKeys = System.Windows.Forms.Keys.F3;
			this.m_menu_edit_find_next.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_find_next.Text = "Find &Next";
			// 
			// m_menu_edit_find_prev
			// 
			this.m_menu_edit_find_prev.Name = "m_menu_edit_find_prev";
			this.m_menu_edit_find_prev.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F3)));
			this.m_menu_edit_find_prev.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_find_prev.Text = "Find &Previous";
			// 
			// toolStripSeparator13
			// 
			this.toolStripSeparator13.Name = "toolStripSeparator13";
			this.toolStripSeparator13.Size = new System.Drawing.Size(255, 6);
			// 
			// m_menu_edit_jumpto
			// 
			this.m_menu_edit_jumpto.Name = "m_menu_edit_jumpto";
			this.m_menu_edit_jumpto.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G)));
			this.m_menu_edit_jumpto.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_jumpto.Text = "Jump to...";
			// 
			// toolStripSeparator17
			// 
			this.toolStripSeparator17.Name = "toolStripSeparator17";
			this.toolStripSeparator17.Size = new System.Drawing.Size(255, 6);
			// 
			// m_menu_edit_toggle_bookmark
			// 
			this.m_menu_edit_toggle_bookmark.Name = "m_menu_edit_toggle_bookmark";
			this.m_menu_edit_toggle_bookmark.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F2)));
			this.m_menu_edit_toggle_bookmark.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_toggle_bookmark.Text = "Toggle Bookmark";
			// 
			// m_menu_edit_next_bookmark
			// 
			this.m_menu_edit_next_bookmark.Name = "m_menu_edit_next_bookmark";
			this.m_menu_edit_next_bookmark.ShortcutKeys = System.Windows.Forms.Keys.F2;
			this.m_menu_edit_next_bookmark.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_next_bookmark.Text = "Next Bookmark";
			// 
			// m_menu_edit_prev_bookmark
			// 
			this.m_menu_edit_prev_bookmark.Name = "m_menu_edit_prev_bookmark";
			this.m_menu_edit_prev_bookmark.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F2)));
			this.m_menu_edit_prev_bookmark.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_prev_bookmark.Text = "Previous Bookmark";
			// 
			// m_menu_edit_clearall_bookmarks
			// 
			this.m_menu_edit_clearall_bookmarks.Name = "m_menu_edit_clearall_bookmarks";
			this.m_menu_edit_clearall_bookmarks.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F2)));
			this.m_menu_edit_clearall_bookmarks.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_clearall_bookmarks.Text = "Clear All Bookmarks";
			// 
			// m_menu_edit_bookmarks
			// 
			this.m_menu_edit_bookmarks.Name = "m_menu_edit_bookmarks";
			this.m_menu_edit_bookmarks.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.B)));
			this.m_menu_edit_bookmarks.Size = new System.Drawing.Size(258, 22);
			this.m_menu_edit_bookmarks.Text = "Bookmarks...";
			// 
			// m_menu_encoding
			// 
			this.m_menu_encoding.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_encoding_detect,
            this.toolStripSeparator3,
            this.m_menu_encoding_ascii,
            this.m_menu_encoding_utf8,
            this.m_menu_encoding_ucs2_littleendian,
            this.m_menu_encoding_ucs2_bigendian});
			this.m_menu_encoding.Name = "m_menu_encoding";
			this.m_menu_encoding.Size = new System.Drawing.Size(69, 20);
			this.m_menu_encoding.Text = "E&ncoding";
			// 
			// m_menu_encoding_detect
			// 
			this.m_menu_encoding_detect.Name = "m_menu_encoding_detect";
			this.m_menu_encoding_detect.Size = new System.Drawing.Size(185, 22);
			this.m_menu_encoding_detect.Text = "&Detect Automatically";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(182, 6);
			// 
			// m_menu_encoding_ascii
			// 
			this.m_menu_encoding_ascii.Name = "m_menu_encoding_ascii";
			this.m_menu_encoding_ascii.Size = new System.Drawing.Size(185, 22);
			this.m_menu_encoding_ascii.Text = "ASCII";
			// 
			// m_menu_encoding_utf8
			// 
			this.m_menu_encoding_utf8.Name = "m_menu_encoding_utf8";
			this.m_menu_encoding_utf8.Size = new System.Drawing.Size(185, 22);
			this.m_menu_encoding_utf8.Text = "UTF-8";
			// 
			// m_menu_encoding_ucs2_littleendian
			// 
			this.m_menu_encoding_ucs2_littleendian.Name = "m_menu_encoding_ucs2_littleendian";
			this.m_menu_encoding_ucs2_littleendian.Size = new System.Drawing.Size(185, 22);
			this.m_menu_encoding_ucs2_littleendian.Text = "UCS-2 (little endian)";
			// 
			// m_menu_encoding_ucs2_bigendian
			// 
			this.m_menu_encoding_ucs2_bigendian.Name = "m_menu_encoding_ucs2_bigendian";
			this.m_menu_encoding_ucs2_bigendian.Size = new System.Drawing.Size(185, 22);
			this.m_menu_encoding_ucs2_bigendian.Text = "UCS-2 (big endian)";
			// 
			// m_menu_line_ending
			// 
			this.m_menu_line_ending.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_line_ending_detect,
            this.toolStripSeparator7,
            this.m_menu_line_ending_cr,
            this.m_menu_line_ending_crlf,
            this.m_menu_line_ending_lf,
            this.m_menu_line_ending_custom});
			this.m_menu_line_ending.Name = "m_menu_line_ending";
			this.m_menu_line_ending.Size = new System.Drawing.Size(81, 20);
			this.m_menu_line_ending.Text = "&Line Ending";
			// 
			// m_menu_line_ending_detect
			// 
			this.m_menu_line_ending_detect.Name = "m_menu_line_ending_detect";
			this.m_menu_line_ending_detect.Size = new System.Drawing.Size(185, 22);
			this.m_menu_line_ending_detect.Text = "Detect &Automatically";
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(182, 6);
			// 
			// m_menu_line_ending_cr
			// 
			this.m_menu_line_ending_cr.Name = "m_menu_line_ending_cr";
			this.m_menu_line_ending_cr.Size = new System.Drawing.Size(185, 22);
			this.m_menu_line_ending_cr.Text = "CR";
			// 
			// m_menu_line_ending_crlf
			// 
			this.m_menu_line_ending_crlf.Name = "m_menu_line_ending_crlf";
			this.m_menu_line_ending_crlf.Size = new System.Drawing.Size(185, 22);
			this.m_menu_line_ending_crlf.Text = "CR+LF";
			// 
			// m_menu_line_ending_lf
			// 
			this.m_menu_line_ending_lf.Name = "m_menu_line_ending_lf";
			this.m_menu_line_ending_lf.Size = new System.Drawing.Size(185, 22);
			this.m_menu_line_ending_lf.Text = "LF";
			// 
			// m_menu_line_ending_custom
			// 
			this.m_menu_line_ending_custom.Name = "m_menu_line_ending_custom";
			this.m_menu_line_ending_custom.Size = new System.Drawing.Size(185, 22);
			this.m_menu_line_ending_custom.Text = "Custom";
			// 
			// m_menu_tools
			// 
			this.m_menu_tools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_tools_alwaysontop,
            this.m_menu_tools_monitor_mode,
            this.m_sep4,
            this.m_menu_tools_clear_log_file,
            this.m_sep5,
            this.m_menu_tools_highlights,
            this.m_menu_tools_filters,
            this.m_menu_tools_transforms,
            this.m_menu_tools_actions,
            this.toolStripSeparator6,
            this.m_menu_tools_options});
			this.m_menu_tools.Name = "m_menu_tools";
			this.m_menu_tools.Size = new System.Drawing.Size(47, 20);
			this.m_menu_tools.Text = "&Tools";
			// 
			// m_menu_tools_alwaysontop
			// 
			this.m_menu_tools_alwaysontop.Name = "m_menu_tools_alwaysontop";
			this.m_menu_tools_alwaysontop.Size = new System.Drawing.Size(153, 22);
			this.m_menu_tools_alwaysontop.Text = "Always On &Top";
			// 
			// m_menu_tools_monitor_mode
			// 
			this.m_menu_tools_monitor_mode.Name = "m_menu_tools_monitor_mode";
			this.m_menu_tools_monitor_mode.Size = new System.Drawing.Size(153, 22);
			this.m_menu_tools_monitor_mode.Text = "&Monitor Mode";
			// 
			// m_sep4
			// 
			this.m_sep4.Name = "m_sep4";
			this.m_sep4.Size = new System.Drawing.Size(150, 6);
			// 
			// m_menu_tools_clear_log_file
			// 
			this.m_menu_tools_clear_log_file.Name = "m_menu_tools_clear_log_file";
			this.m_menu_tools_clear_log_file.Size = new System.Drawing.Size(153, 22);
			this.m_menu_tools_clear_log_file.Text = "&Clear Log File";
			// 
			// m_sep5
			// 
			this.m_sep5.Name = "m_sep5";
			this.m_sep5.Size = new System.Drawing.Size(150, 6);
			// 
			// m_menu_tools_highlights
			// 
			this.m_menu_tools_highlights.Name = "m_menu_tools_highlights";
			this.m_menu_tools_highlights.Size = new System.Drawing.Size(153, 22);
			this.m_menu_tools_highlights.Text = "&Highlights";
			// 
			// m_menu_tools_filters
			// 
			this.m_menu_tools_filters.Name = "m_menu_tools_filters";
			this.m_menu_tools_filters.Size = new System.Drawing.Size(153, 22);
			this.m_menu_tools_filters.Text = "&Filters";
			// 
			// m_menu_tools_transforms
			// 
			this.m_menu_tools_transforms.Name = "m_menu_tools_transforms";
			this.m_menu_tools_transforms.Size = new System.Drawing.Size(153, 22);
			this.m_menu_tools_transforms.Text = "&Transforms";
			// 
			// m_menu_tools_actions
			// 
			this.m_menu_tools_actions.Name = "m_menu_tools_actions";
			this.m_menu_tools_actions.Size = new System.Drawing.Size(153, 22);
			this.m_menu_tools_actions.Text = "&Actions";
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(150, 6);
			// 
			// m_menu_tools_options
			// 
			this.m_menu_tools_options.Name = "m_menu_tools_options";
			this.m_menu_tools_options.Size = new System.Drawing.Size(153, 22);
			this.m_menu_tools_options.Text = "&Options";
			// 
			// m_menu_help
			// 
			this.m_menu_help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_help_view_help,
            this.m_menu_help_firstruntutorial,
            this.m_menu_help_totd,
            this.toolStripSeparator11,
            this.m_menu_help_visit_web_site,
            this.m_menu_help_register,
            this.toolStripSeparator12,
            this.m_menu_help_check_for_updates,
            this.toolStripSeparator2,
            this.m_menu_help_about});
			this.m_menu_help.Name = "m_menu_help";
			this.m_menu_help.Size = new System.Drawing.Size(44, 20);
			this.m_menu_help.Text = "&Help";
			// 
			// m_menu_help_view_help
			// 
			this.m_menu_help_view_help.Name = "m_menu_help_view_help";
			this.m_menu_help_view_help.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F1)));
			this.m_menu_help_view_help.Size = new System.Drawing.Size(173, 22);
			this.m_menu_help_view_help.Text = "View &Help";
			// 
			// m_menu_help_firstruntutorial
			// 
			this.m_menu_help_firstruntutorial.Name = "m_menu_help_firstruntutorial";
			this.m_menu_help_firstruntutorial.Size = new System.Drawing.Size(173, 22);
			this.m_menu_help_firstruntutorial.Text = "&First Run Tutorial";
			// 
			// m_menu_help_totd
			// 
			this.m_menu_help_totd.Name = "m_menu_help_totd";
			this.m_menu_help_totd.Size = new System.Drawing.Size(173, 22);
			this.m_menu_help_totd.Text = "&Tip of the Day";
			// 
			// toolStripSeparator11
			// 
			this.toolStripSeparator11.Name = "toolStripSeparator11";
			this.toolStripSeparator11.Size = new System.Drawing.Size(170, 6);
			// 
			// m_menu_help_visit_web_site
			// 
			this.m_menu_help_visit_web_site.Name = "m_menu_help_visit_web_site";
			this.m_menu_help_visit_web_site.Size = new System.Drawing.Size(173, 22);
			this.m_menu_help_visit_web_site.Text = "&Visit Web Site";
			// 
			// m_menu_help_register
			// 
			this.m_menu_help_register.Name = "m_menu_help_register";
			this.m_menu_help_register.Size = new System.Drawing.Size(173, 22);
			this.m_menu_help_register.Text = "&Activate Licence...";
			// 
			// toolStripSeparator12
			// 
			this.toolStripSeparator12.Name = "toolStripSeparator12";
			this.toolStripSeparator12.Size = new System.Drawing.Size(170, 6);
			// 
			// m_menu_help_check_for_updates
			// 
			this.m_menu_help_check_for_updates.Name = "m_menu_help_check_for_updates";
			this.m_menu_help_check_for_updates.Size = new System.Drawing.Size(173, 22);
			this.m_menu_help_check_for_updates.Text = "Check for &Updates";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(170, 6);
			// 
			// m_menu_help_about
			// 
			this.m_menu_help_about.Name = "m_menu_help_about";
			this.m_menu_help_about.Size = new System.Drawing.Size(173, 22);
			this.m_menu_help_about.Text = "&About";
			// 
			// m_menu_free_version
			// 
			this.m_menu_free_version.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.m_menu_free_version.ForeColor = System.Drawing.Color.Red;
			this.m_menu_free_version.Name = "m_menu_free_version";
			this.m_menu_free_version.Padding = new System.Windows.Forms.Padding(40, 0, 40, 0);
			this.m_menu_free_version.Size = new System.Drawing.Size(244, 20);
			this.m_menu_free_version.Text = "RyLogViewer - Free Edition ...";
			// 
			// m_status
			// 
			this.m_status.Dock = System.Windows.Forms.DockStyle.None;
			this.m_status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status_filesize,
            this.m_status_selection,
            this.m_status_line_end,
            this.m_status_encoding,
            this.m_status_spring,
            this.m_status_progress,
            this.m_status_message_trans,
            this.m_status_message_fixed});
			this.m_status.Location = new System.Drawing.Point(0, 0);
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(835, 24);
			this.m_status.TabIndex = 3;
			this.m_status.Text = "statusStrip1";
			// 
			// m_status_filesize
			// 
			this.m_status_filesize.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_status_filesize.Name = "m_status_filesize";
			this.m_status_filesize.Size = new System.Drawing.Size(128, 19);
			this.m_status_filesize.Text = "Size: 2147483647 bytes";
			this.m_status_filesize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// m_status_line_end
			// 
			this.m_status_line_end.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_status_line_end.Name = "m_status_line_end";
			this.m_status_line_end.Size = new System.Drawing.Size(76, 19);
			this.m_status_line_end.Text = "Line Ending:";
			// 
			// m_status_encoding
			// 
			this.m_status_encoding.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_status_encoding.Name = "m_status_encoding";
			this.m_status_encoding.Size = new System.Drawing.Size(64, 19);
			this.m_status_encoding.Text = "Encoding:";
			// 
			// m_status_spring
			// 
			this.m_status_spring.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_status_spring.Name = "m_status_spring";
			this.m_status_spring.Size = new System.Drawing.Size(163, 19);
			this.m_status_spring.Spring = true;
			this.m_status_spring.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// m_status_progress
			// 
			this.m_status_progress.Name = "m_status_progress";
			this.m_status_progress.Size = new System.Drawing.Size(100, 18);
			this.m_status_progress.Visible = false;
			// 
			// m_status_message_trans
			// 
			this.m_status_message_trans.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_status_message_trans.Name = "m_status_message_trans";
			this.m_status_message_trans.Size = new System.Drawing.Size(108, 19);
			this.m_status_message_trans.Text = "Transient Message";
			this.m_status_message_trans.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.m_status_message_trans.Visible = false;
			// 
			// m_status_message_fixed
			// 
			this.m_status_message_fixed.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_status_message_fixed.Name = "m_status_message_fixed";
			this.m_status_message_fixed.Size = new System.Drawing.Size(89, 19);
			this.m_status_message_fixed.Text = "Static Message";
			this.m_status_message_fixed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.m_status_message_fixed.Visible = false;
			// 
			// m_toolstrip_cont
			// 
			// 
			// m_toolstrip_cont.BottomToolStripPanel
			// 
			this.m_toolstrip_cont.BottomToolStripPanel.Controls.Add(this.m_status);
			// 
			// m_toolstrip_cont.ContentPanel
			// 
			this.m_toolstrip_cont.ContentPanel.AutoScroll = true;
			this.m_toolstrip_cont.ContentPanel.Controls.Add(this.m_table);
			this.m_toolstrip_cont.ContentPanel.Margin = new System.Windows.Forms.Padding(0);
			this.m_toolstrip_cont.ContentPanel.Size = new System.Drawing.Size(835, 413);
			this.m_toolstrip_cont.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_toolstrip_cont.LeftToolStripPanelVisible = false;
			this.m_toolstrip_cont.Location = new System.Drawing.Point(0, 0);
			this.m_toolstrip_cont.Margin = new System.Windows.Forms.Padding(0);
			this.m_toolstrip_cont.Name = "m_toolstrip_cont";
			this.m_toolstrip_cont.RightToolStripPanelVisible = false;
			this.m_toolstrip_cont.Size = new System.Drawing.Size(835, 492);
			this.m_toolstrip_cont.TabIndex = 6;
			this.m_toolstrip_cont.Text = "m_toolstrip_cont";
			// 
			// m_toolstrip_cont.TopToolStripPanel
			// 
			this.m_toolstrip_cont.TopToolStripPanel.Controls.Add(this.m_menu);
			this.m_toolstrip_cont.TopToolStripPanel.Controls.Add(this.m_toolstrip);
			// 
			// m_table
			// 
			this.m_table.ColumnCount = 3;
			this.m_table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.m_table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.m_table.ContextMenuStrip = this.m_cmenu_grid;
			this.m_table.Controls.Add(this.m_grid, 0, 0);
			this.m_table.Controls.Add(this.m_scroll_file, 1, 0);
			this.m_table.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table.Location = new System.Drawing.Point(0, 0);
			this.m_table.Name = "m_table";
			this.m_table.RowCount = 1;
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table.Size = new System.Drawing.Size(835, 413);
			this.m_table.TabIndex = 5;
			// 
			// m_cmenu_grid
			// 
			this.m_cmenu_grid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_cmenu_select_all,
            this.m_cmenu_copy,
            this.toolStripSeparator15,
            this.m_cmenu_clear_log,
            this.toolStripSeparator10,
            this.m_cmenu_highlight_row,
            this.m_cmenu_filter_row,
            this.m_cmenu_transform_row,
            this.m_cmenu_action_row,
            this.toolStripSeparator4,
            this.m_cmenu_find_next,
            this.m_cmenu_find_prev,
            this.toolStripSeparator14,
            this.m_cmenu_toggle_bookmark});
			this.m_cmenu_grid.Name = "m_cmenu_grid";
			this.m_cmenu_grid.Size = new System.Drawing.Size(168, 248);
			// 
			// m_cmenu_select_all
			// 
			this.m_cmenu_select_all.Name = "m_cmenu_select_all";
			this.m_cmenu_select_all.Size = new System.Drawing.Size(167, 22);
			this.m_cmenu_select_all.Text = "Select &All";
			// 
			// m_cmenu_copy
			// 
			this.m_cmenu_copy.Name = "m_cmenu_copy";
			this.m_cmenu_copy.Size = new System.Drawing.Size(167, 22);
			this.m_cmenu_copy.Text = "&Copy";
			// 
			// toolStripSeparator15
			// 
			this.toolStripSeparator15.Name = "toolStripSeparator15";
			this.toolStripSeparator15.Size = new System.Drawing.Size(164, 6);
			// 
			// m_cmenu_clear_log
			// 
			this.m_cmenu_clear_log.Name = "m_cmenu_clear_log";
			this.m_cmenu_clear_log.Size = new System.Drawing.Size(167, 22);
			this.m_cmenu_clear_log.Text = "C&lear Log";
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			this.toolStripSeparator10.Size = new System.Drawing.Size(164, 6);
			// 
			// m_cmenu_highlight_row
			// 
			this.m_cmenu_highlight_row.Name = "m_cmenu_highlight_row";
			this.m_cmenu_highlight_row.Size = new System.Drawing.Size(167, 22);
			this.m_cmenu_highlight_row.Text = "&Highlight Row...";
			// 
			// m_cmenu_filter_row
			// 
			this.m_cmenu_filter_row.Name = "m_cmenu_filter_row";
			this.m_cmenu_filter_row.Size = new System.Drawing.Size(167, 22);
			this.m_cmenu_filter_row.Text = "&Filter Row...";
			// 
			// m_cmenu_transform_row
			// 
			this.m_cmenu_transform_row.Name = "m_cmenu_transform_row";
			this.m_cmenu_transform_row.Size = new System.Drawing.Size(167, 22);
			this.m_cmenu_transform_row.Text = "&Transform Row...";
			// 
			// m_cmenu_action_row
			// 
			this.m_cmenu_action_row.Name = "m_cmenu_action_row";
			this.m_cmenu_action_row.Size = new System.Drawing.Size(167, 22);
			this.m_cmenu_action_row.Text = "&Action Row...";
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(164, 6);
			// 
			// m_cmenu_find_next
			// 
			this.m_cmenu_find_next.Name = "m_cmenu_find_next";
			this.m_cmenu_find_next.Size = new System.Drawing.Size(167, 22);
			this.m_cmenu_find_next.Text = "Find &Next";
			// 
			// m_cmenu_find_prev
			// 
			this.m_cmenu_find_prev.Name = "m_cmenu_find_prev";
			this.m_cmenu_find_prev.Size = new System.Drawing.Size(167, 22);
			this.m_cmenu_find_prev.Text = "Find &Previous";
			// 
			// toolStripSeparator14
			// 
			this.toolStripSeparator14.Name = "toolStripSeparator14";
			this.toolStripSeparator14.Size = new System.Drawing.Size(164, 6);
			// 
			// m_cmenu_toggle_bookmark
			// 
			this.m_cmenu_toggle_bookmark.Name = "m_cmenu_toggle_bookmark";
			this.m_cmenu_toggle_bookmark.Size = new System.Drawing.Size(167, 22);
			this.m_cmenu_toggle_bookmark.Text = "Toggle &Bookmark";
			// 
			// m_grid
			// 
			this.m_grid.AllowUserToAddRows = false;
			this.m_grid.AllowUserToDeleteRows = false;
			this.m_grid.AllowUserToOrderColumns = true;
			this.m_grid.AllowUserToResizeRows = false;
			this.m_grid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.ContextMenuStrip = this.m_cmenu_grid;
			this.m_grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.m_grid.Location = new System.Drawing.Point(3, 3);
			this.m_grid.Name = "m_grid";
			this.m_grid.ReadOnly = true;
			this.m_grid.RowHeadersVisible = false;
			this.m_grid.RowTemplate.Height = 18;
			this.m_grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid.ShowCellErrors = false;
			this.m_grid.ShowCellToolTips = false;
			this.m_grid.ShowEditingIcon = false;
			this.m_grid.ShowRowErrors = false;
			this.m_grid.Size = new System.Drawing.Size(805, 407);
			this.m_grid.TabIndex = 3;
			this.m_grid.VirtualMode = true;
			// 
			// m_scroll_file
			// 
			this.m_scroll_file.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_scroll_file.LargeChange = ((long)(1));
			this.m_scroll_file.Location = new System.Drawing.Point(814, 3);
			this.m_scroll_file.MinimumSize = new System.Drawing.Size(10, 10);
			this.m_scroll_file.MinThumbSize = 20;
			this.m_scroll_file.Name = "m_scroll_file";
			this.m_scroll_file.Overlay = null;
			this.m_scroll_file.OverlayAttributes = null;
			this.m_scroll_file.Size = new System.Drawing.Size(18, 407);
			this.m_scroll_file.SmallChange = ((long)(1));
			this.m_scroll_file.TabIndex = 4;
			this.m_scroll_file.ThumbColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
			this.m_scroll_file.TrackColor = System.Drawing.SystemColors.ControlLight;
			// 
			// m_status_selection
			// 
			this.m_status_selection.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_status_selection.Name = "m_status_selection";
			this.m_status_selection.Size = new System.Drawing.Size(59, 19);
			this.m_status_selection.Text = "Selection";
			// 
			// Main
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(835, 492);
			this.Controls.Add(this.m_toolstrip_cont);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MainMenuStrip = this.m_menu;
			this.MinimumSize = new System.Drawing.Size(200, 220);
			this.Name = "Main";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Rylogic Log Viewer";
			this.m_toolstrip.ResumeLayout(false);
			this.m_toolstrip.PerformLayout();
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			this.m_status.ResumeLayout(false);
			this.m_status.PerformLayout();
			this.m_toolstrip_cont.BottomToolStripPanel.ResumeLayout(false);
			this.m_toolstrip_cont.BottomToolStripPanel.PerformLayout();
			this.m_toolstrip_cont.ContentPanel.ResumeLayout(false);
			this.m_toolstrip_cont.TopToolStripPanel.ResumeLayout(false);
			this.m_toolstrip_cont.TopToolStripPanel.PerformLayout();
			this.m_toolstrip_cont.ResumeLayout(false);
			this.m_toolstrip_cont.PerformLayout();
			this.m_table.ResumeLayout(false);
			this.m_cmenu_grid.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
