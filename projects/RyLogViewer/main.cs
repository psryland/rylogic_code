using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
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
	public partial class Main :Form
	{
		private readonly Settings m_settings;                 // App settings
		private readonly RecentFiles m_recent;                // Recent files
		private readonly FileWatch m_watch;                   // A helper for watching files
		private readonly Timer m_watch_timer;                 // A timer for polling the file watcher
		private readonly List<Highlight> m_highlights;        // A list of the active highlights only
		private readonly FindUI m_find_ui;                    // The find dialog
		private readonly NotifyIcon m_notify_icon;            // A system tray icon
		private readonly ToolTip m_tt;                        // Tooltips
		private Pattern m_last_find_pattern;                  // The pattern last used in a find
		private List<Range> m_line_index;                     // Byte offsets (from file begin) to the byte range of a line
		private Encoding m_encoding;                          // The file encoding
		private string m_filepath;                            // The path of the log file we're viewing
		private FileStream m_file;                            // A file stream of 'm_filepath'
		private byte[] m_row_delim;                           // The row delimiter converted from a string to a byte[] using the current encoding
		private byte[] m_col_delim;                           // The column delimiter, cached to prevent m_settings access in CellNeeded
		private int m_row_height;                             // The row height, cached to prevent settings lookups in CellNeeded
		private long m_filepos;                               // The byte offset (from file begin) to the start of the last known line
		private long m_fileend;                               // The last known size of the file
		private long m_bufsize;                               // Cached value of m_settings.FileBufSize
		private int m_line_cache_count;                      // The number of lines to scan about the currently selected row
		private int m_suspend_grid_events;                    // A ref count of nested called that tell event handlers to ignore grid events

		//bug:
		// ungraceful close of process capture file
		//todo:
		// 'Rewrite' - regex substitution
		// partial highlighting
		// Tip of the Day content

		public Main(string[] args)
		{
			Log.Register(null, false);
			Log.Info(this, "App Startup: {0}", DateTime.Now);

			InitializeComponent();
			AllowTransparency   = true;
			m_settings          = new Settings();
			m_recent            = new RecentFiles(m_menu_file_recent, OpenLogFile);
			m_watch             = new FileWatch();
			m_watch_timer       = new Timer{Interval = Constants.FilePollingRate};
			m_highlights        = new List<Highlight>();
			m_find_ui           = new FindUI(m_settings){Visible = false};
			m_tt                = new ToolTip();
			m_notify_icon       = new NotifyIcon{Icon = Icon};
			m_line_index        = new List<Range>();
			m_last_find_pattern = null;
			m_filepath          = null;
			m_file              = null;
			m_filepos           = 0;
			m_fileend           = 0;
			m_bufsize           = m_settings.FileBufSize;
			m_line_cache_count  = m_settings.LineCacheCount;
			
			m_settings.SettingChanged += (s,a)=> Log.Info(this, "Setting {0} changed from {1} to {2}", a.Key ,a.OldValue ,a.NewValue);
			
			// Menu
			m_menu.Move                             += (s,a) => m_settings.MenuPosition = m_menu.Location;
			m_menu_file_open.Click                  += (s,a) => OpenLogFile();
			m_menu_file_open_stdout.Click           += (s,a) => LogProgramOutput();
			m_menu_file_open_serial_port.Click      += (s,a) => LogSerialPort();
			m_menu_file_open_network.Click          += (s,a) => LogNetworkOutput();
			m_menu_file_open_named_pipe.Click       += (s,a) => LogNamedPipeOutput();
			m_menu_file_close.Click                 += (s,a) => CloseLogFile();
			m_menu_file_export.Click                += (s,a) => ShowExportDialog();
			m_menu_file_exit.Click                  += (s,a) => Close();
			m_menu_edit_selectall.Click             += (s,a) => DataGridView_Extensions.SelectAll(m_grid, new KeyEventArgs(Keys.Control|Keys.A));
			m_menu_edit_copy.Click                  += (s,a) => DataGridView_Extensions.Copy(m_grid, new KeyEventArgs(Keys.Control|Keys.C));
			m_menu_edit_find.Click                  += (s,a) => ShowFindDialog();
			m_menu_edit_find_next.Click             += (s,a) => FindNext(m_last_find_pattern);
			m_menu_edit_find_prev.Click             += (s,a) => FindPrev(m_last_find_pattern);
			m_menu_encoding_detect.Click            += (s,a) => SetEncoding(null);
			m_menu_encoding_ascii.Click             += (s,a) => SetEncoding(Encoding.ASCII           );
			m_menu_encoding_utf8.Click              += (s,a) => SetEncoding(Encoding.UTF8            );
			m_menu_encoding_ucs2_littleendian.Click += (s,a) => SetEncoding(Encoding.Unicode         );
			m_menu_encoding_ucs2_bigendian.Click    += (s,a) => SetEncoding(Encoding.BigEndianUnicode);
			m_menu_line_ending_detect.Click         += (s,a) => SetLineEnding(ELineEnding.Detect);
			m_menu_line_ending_cr.Click             += (s,a) => SetLineEnding(ELineEnding.CR    );
			m_menu_line_ending_crlf.Click           += (s,a) => SetLineEnding(ELineEnding.CRLF  );
			m_menu_line_ending_lf.Click             += (s,a) => SetLineEnding(ELineEnding.LF    );
			m_menu_line_ending_custom.Click         += (s,a) => SetLineEnding(ELineEnding.Custom);
			m_menu_tools_alwaysontop.Click          += (s,a) => SetAlwaysOnTop(!m_settings.AlwaysOnTop);
			m_menu_tools_highlights.Click           += (s,a) => ShowOptions(SettingsUI.ETab.Highlights);
			m_menu_tools_filters.Click              += (s,a) => ShowOptions(SettingsUI.ETab.Filters);
			m_menu_tools_clear_log_file.Click       += (s,a) => ClearLogFile();
			m_menu_tools_ghost_mode.Click           += (s,a) => EnableGhostMode(!m_menu_tools_ghost_mode.Checked);
			m_menu_tools_options.Click              += (s,a) => ShowOptions(SettingsUI.ETab.General);
			m_menu_help_totd.Click                  += (s,a) => ShowTotD();
			m_menu_help_check_for_updates.Click     += (s,a) => CheckForUpdates(true);
			m_menu_help_about.Click                 += (s,a) => ShowAbout();
			m_recent.Import(m_settings.RecentFiles);
			
			// Toolbar
			m_btn_open_log.ToolTipText      = Resources.OpenLogFile;
			m_btn_open_log.Click           += (s,a) => OpenLogFile();
			m_btn_refresh.ToolTipText       = Resources.ReloadLogFile;
			m_btn_refresh.Click            += (s,a) => BuildLineIndex(m_filepos, true);
			m_btn_highlights.ToolTipText    = Resources.ShowHighlightsDialog;
			m_btn_highlights.Click         += (s,a) => EnableHighlights(m_btn_highlights.Checked);
			m_btn_highlights.MouseDown     += (s,a) => { if (a.Button == MouseButtons.Right) ShowOptions(SettingsUI.ETab.Highlights); };
			m_btn_filters.ToolTipText       = Resources.ShowFiltersDialog;
			m_btn_filters.Click            += (s,a) => EnableFilters(m_btn_filters.Checked);
			m_btn_filters.MouseDown        += (s,a) => { if (a.Button == MouseButtons.Right) ShowOptions(SettingsUI.ETab.Filters); };
			m_btn_transforms.ToolTipText    = Resources.ShowTransformsDialog;
			m_btn_transforms.Click         += (s,a) => EnableTransforms(m_btn_transforms.Checked);
			m_btn_transforms.MouseDown     += (s,a) => { if (a.Button == MouseButtons.Right) ShowOptions(SettingsUI.ETab.Transforms); };
			m_btn_options.ToolTipText       = Resources.ShowOptionsDialog;
			m_btn_options.Click            += (s,a) => ShowOptions(SettingsUI.ETab.General);
			m_btn_jump_to_start.ToolTipText = Resources.ScrollToStart;
			m_btn_jump_to_start.Click      += (s,a) => BuildLineIndex(0, false, () => SelectedRow = 0);
			m_btn_jump_to_end.ToolTipText   = Resources.ScrollToEnd;
			m_btn_jump_to_end.Click        += (s,a) => BuildLineIndex(m_fileend, false, () => SelectedRow = m_grid.RowCount - 1);
			m_btn_tail.ToolTipText          = Resources.WatchForUpdates;
			m_btn_tail.Click               += (s,a) => EnableTail(m_btn_tail.Checked);
			m_toolstrip.Move               += (s,a) => m_settings.ToolsPosition = m_toolstrip.Location;
			ToolStripManager.Renderer       = new CheckedButtonRenderer();

			// Scrollbar
			m_scroll_file.ToolTip(m_tt, "Indicates the currently cached position in the log file");
			m_scroll_file.Ranges.Resize(3);
			m_scroll_file.ScrollEnd += (s,a)=>
				{
					// Update on ScrollEnd not value changed, since
					// UpdateUI() sets Value when the build is complete.
					var range = m_scroll_file.ThumbRange;
					long pos = (range.m_begin == 0) ? 0 : (range.m_end == m_fileend) ? m_fileend : range.Mid;
					Log.Info(this, "file scroll to {0}", pos);
					BuildLineIndex(pos, false);
				};

			// Status
			m_status.Move += (s,a) => m_settings.StatusPosition = m_status.Location;
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
			m_grid.MouseUp             += (s,a) => LoadNearBoundary();
			m_grid.CellValueNeeded     += CellValueNeeded;
			m_grid.CellPainting        += CellPainting;
			m_grid.SelectionChanged    += GridSelectionChanged;
			m_grid.RowHeightInfoNeeded += (s,a) => { a.Height = m_row_height; };
			m_grid.DataError           += (s,a) => Debug.Assert(false);
			m_grid.Scroll              += (s,a) => UpdateFileScroll();

			// Grid context menu
			m_cmenu_copy.Click         += (s,a) => DataGridView_Extensions.Copy(m_grid);
			m_cmenu_select_all.Click   += (s,a) => DataGridView_Extensions.SelectAll(m_grid);
			m_cmenu_grid.VisibleChanged += (s,a) =>
				{
					m_cmenu_copy.Enabled = m_grid.SelectedCells.Count != 0;
					m_cmenu_select_all.Enabled = m_grid.RowCount != 0;
				};
			
			// File Watcher
			m_watch_timer.Tick += (s,a)=> m_watch.CheckForChangedFiles();
			
			// Find
			m_find_ui.Tag = Size.Empty;
			m_find_ui.FindNext += FindNext;
			m_find_ui.FindPrev += FindPrev;
			m_find_ui.Move     += (s,e)=> { m_find_ui.Tag = new Size(m_find_ui.Location.X - Location.X, m_find_ui.Location.Y - Location.Y); };
			
			// Startup
			Shown += (s,a)=> Startup(args);
			
			// User input
			KeyDown += (s,a) => HandleKeyDown(a);
			
			// File Drop
			DragEnter += (s,a) => FileDrop(a, true);
			DragDrop  += (s,a) => FileDrop(a, false);
			
			// Resize
			SizeChanged += (s,a)=> UpdateUI();

			// Main window move
			Move += (s,a)=>
				{
					m_find_ui.Location = Location + (Size)m_find_ui.Tag;
				};

			// Shutdown
			FormClosing += (s,a) =>
				{
					m_settings.ScreenPosition = Location;
					m_settings.WindowSize = Size;
					m_settings.RecentFiles = m_recent.Export();
				};
		}

		/// <summary>Called the first time the app is displayed</summary>
		private void Startup(string[] args)
		{
			// Last screen position
			if (m_settings.RestoreScreenLoc)
			{
				Location = m_settings.ScreenPosition;
				Size = m_settings.WindowSize;
				m_find_ui.Tag = new Size(Size.Width - m_find_ui.Width - 8, 28);
			}
					
			// Parse commandline
			if (args.Length != 0)
			{
				ParseCommandLine(args);
			}
			else if (m_settings.LoadLastFile && File.Exists(m_settings.LastLoadedFile))
			{
				OpenLogFile(m_settings.LastLoadedFile);
			}
					
			// Show the TotD
			if (m_settings.ShowTOTD)
				ShowTotD();

			// Check for updates
			if (m_settings.CheckForUpdates)
				CheckForUpdates(false);

			InitCache();
			ApplySettings();
		}

		/// <summary>Returns true if there is a log file currently open</summary>
		private bool FileOpen
		{
			get { return m_file != null; }
		}
		
		/// <summary>Parse the command line parameters</summary>
		private void ParseCommandLine(IEnumerable<string> args)
		{
			string file_to_load = null;
			foreach (var arg in args)
			{
				if (arg[0] == '-')
				{
					string cmd = arg.Substring(1).ToLower();
					switch (cmd)
					{
					default:
						MessageBox.Show(this, string.Format("'{0}' is not a valid command line switch", cmd), Resources.UnknownCmdLineOption, MessageBoxButtons.OK, MessageBoxIcon.Information);
						break;
					}
				}
				else
				{
					file_to_load = arg;
				}
			}
			if (file_to_load != null)
				OpenLogFile(file_to_load);
		}
		
		/// <summary>Close the current log file</summary>
		private void CloseLogFile()
		{
			using (Scope.Create(()=>++m_suspend_grid_events, ()=>--m_suspend_grid_events))
			{
				m_line_index.Clear();
				m_watch.Remove(m_filepath);
				if (m_buffered_process    != null) m_buffered_process.Dispose();
				if (m_buffered_netconn    != null) m_buffered_netconn.Dispose();
				if (m_buffered_serialconn != null) m_buffered_serialconn.Dispose();
				if (m_buffered_pipeconn   != null) m_buffered_pipeconn.Dispose();
				if (FileOpen) m_file.Dispose();
				m_buffered_process = null;
				m_buffered_netconn = null;
				m_buffered_serialconn = null;
				m_buffered_pipeconn = null;
				m_filepath = null;
				m_file = null;
				m_filepos = 0;
				m_fileend = 0;
			}
			UpdateUI();
		}
		
		/// <summary>Prompt to open a log file</summary>
		private void OpenLogFile(string filepath, bool add_to_recent)
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
			
				// Reject invalid filepaths
				if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
				{
					MessageBox.Show(this, string.Format(Resources.InvalidFileMsg, filepath), Resources.InvalidFilePath, MessageBoxButtons.OK,MessageBoxIcon.Error);
					return;
				}
				
				if (add_to_recent)
				{
					m_recent.Add(filepath);
					m_settings.RecentFiles = m_recent.Export();
					m_settings.LastLoadedFile = filepath;
				}
				
				// Switch files - open the file to make sure it's accessible (and to hold a lock)
				CloseLogFile();
				m_file = LoadFile(filepath);
				m_filepath = filepath;
				m_filepos = m_settings.OpenAtEnd ? m_file.Length : 0;
				
				// Setup the watcher to watch for file changes
				// Start the build in an invoke so that checking for file changes doesn't cause reentrancy
				m_watch.Add(m_filepath, (fp,ctx) => { OnFileChanged(); return true; });
				m_watch_timer.Enabled = FileOpen && m_settings.TailEnabled;
				
				BuildLineIndex(m_filepos, true, ()=>{ SelectedRow = m_settings.OpenAtEnd ? m_grid.RowCount - 1 : 0; });
				return;
			}
			catch (Exception ex) { MessageBox.Show(this, string.Format(Resources.FailedToOpenXDueToErrorY, filepath ,ex.Message), Resources.FailedToLoadFile, MessageBoxButtons.OK, MessageBoxIcon.Error); }
			CloseLogFile();
		}
		private void OpenLogFile(string filepath = null)
		{
			OpenLogFile(filepath, true);
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
			LogNetworkConnection(dg.Conn);
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
			long len = m_file.Length;
			Log.Info(this, "File {0} changed. File length: {1}", m_filepath, len);
			BuildLineIndex(AutoScrollTail ? m_file.Length : m_filepos, !m_settings.FileChangesAdditive);
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
		
		/// <summary>Draw the cell appropriate to any highlighting</summary>
		private void CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			// Leave rendering to the grid while events are suspended
			if (GridEventsBlocked)
			{
				e.Handled = false;
				return;
			}
			
			// Check if the cell value has a highlight pattern it matches
			Highlight hl = (e.RowIndex != -1) ? ReadLine(e.RowIndex)[e.ColumnIndex].HL : null;
			if (hl == null)
			{
				e.PaintBackground(e.ClipBounds, e.RowIndex == SelectedRow);
				e.PaintContent(e.ClipBounds);
			}
			else if (hl.BinaryMatch)
			{
				e.CellStyle.BackColor = hl.BackColour;
				e.CellStyle.ForeColor = hl.ForeColour;
				e.PaintBackground(e.ClipBounds, false);
				e.PaintContent(e.ClipBounds);
			}
			else
			{
				//todo
				e.PaintBackground(e.ClipBounds, false);
				e.PaintContent(e.ClipBounds);
			}
		}
		
		/// <summary>Handler for selections made in the grid</summary>
		private void GridSelectionChanged(object sender, EventArgs e)
		{
			if (GridEventsBlocked) return;
			UpdateStatus();
		}
		
		/// <summary>
		/// Tests whether the currently selected row is near the start or end of
		/// the line range and causes a reload if it is</summary>
		private void LoadNearBoundary()
		{
			if (m_grid.RowCount < Constants.AutoScrollAtBoundaryLimit) return;
			const float limit = 1f / Constants.AutoScrollAtBoundaryLimit;
			float ratio = Maths.Ratio(0, SelectedRow, m_grid.RowCount - 1);
			if (ratio < 0f + limit) BuildLineIndex(LineStartIndexRange.m_begin, false);
			if (ratio > 1f - limit) BuildLineIndex(LineStartIndexRange.m_end  , false);
		}

		/// <summary>Handle key down events for the grid</summary>
		private void HandleKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)                { CancelBuildLineIndex(); }
			if (e.KeyCode == Keys.F5)                    { BuildLineIndex(m_filepos, true); e.Handled = true; }
			if (e.KeyCode == Keys.PageUp && e.Control)   { BuildLineIndex(0        , false, () => SelectedRow = 0                  ); e.Handled = true; }
			if (e.KeyCode == Keys.PageDown && e.Control) { BuildLineIndex(m_fileend, false, () => SelectedRow = m_grid.RowCount - 1); e.Handled = true; }
			e.Handled = false;
		}
		
		/// <summary>Show the find dialog</summary>
		private void ShowFindDialog()
		{
			// Initialise the find string from the selected row
			int row = SelectedRow;
			if (row != -1)
				m_find_ui.Pattern = ReadLine(row).RowText;
			
			// Display the find window
			m_find_ui.Location = Location + (Size)m_find_ui.Tag;
			if (!m_find_ui.Visible)
				m_find_ui.Show(this);
			else
				m_find_ui.Focus();
		}
		
		/// <summary>Searches the file from 'start' looking for a match to 'pat'</summary>
		/// <returns>Returns true if a match is found, false otherwise. If true
		/// is returned 'found' contains the file byte offset of the first match</returns>
		private bool Find(Pattern pat, long start, bool backward, out long found)
		{
			// Although this search runs in a background thread, it's wrapped in a modal
			// dialog box, so it should be ok to use class members directly
			
			long at = -1;
			ProgressForm search = new ProgressForm("Searching...", "", (s,a)=>
				{
					BackgroundWorker bgw = (BackgroundWorker)s;
					
					using (var file = LoadFile(m_filepath))
					{
						int last_progress     = 0;
						bool ignore_blanks    = m_settings.IgnoreBlankLines;
						List<Filter> filters  = ActiveFilters.ToList();
						AddLineFunc test_line = (line, baddr, fend, bf, enc) =>
							{
								int progress = backward
									? (int)(100 * (1f - Maths.Ratio(0, baddr + line.m_begin, start)))
									: (int)(100 * Maths.Ratio(start, baddr + line.m_end ,m_fileend));
								if (progress != last_progress) { bgw.ReportProgress(progress); last_progress = progress; }
								
								// Ignore blanks?
								if (line.Empty && ignore_blanks)
									return true;
								
								// Keep searching while the text is filtered out or doesn't match the pattern
								string text = m_encoding.GetString(bf, (int)line.m_begin, (int)line.Count);
								if (!PassesFilters(text, filters) || !pat.IsMatch(text))
									return true;
								
								// Found a match
								at = baddr + line.m_begin;
								return false; // Stop searching
							};
						
						// Search for files
						byte[] buf = new byte[m_settings.MaxLineLength];
						long count = backward ? start - 0 : m_fileend - start;
						FindLines(file, start, m_fileend, backward, count, test_line, m_encoding, m_row_delim, buf, (c,l) => !bgw.CancellationPending);
						
						// We can call BuildLineIndex in this thread context because we know
						// we're in a modal dialog.
						if (at != -1)
						{
							Action select = ()=>SelectRowByAddr(at);
							Invoke(select);
						}
						
						a.Cancel = bgw.CancellationPending;
					}
				}){StartPosition = FormStartPosition.CenterParent};
			
			DialogResult res = DialogResult.Cancel;
			try { res = search.ShowDialog(this); }
			catch (OperationCanceledException) {}
			catch (Exception ex) { MessageBox.Show(this, "Find terminated by an error.\r\nError Details:\r\n"+ex.Message, "Find error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
			found = at;
			return res == DialogResult.OK;
		}
		
		/// <summary>Search for the next occurance of a pattern in the file</summary>
		private void FindNext(Pattern pat)
		{
			if (pat == null || m_grid.RowCount == 0) return;
			m_last_find_pattern = pat;
			
			var start = m_line_index[SelectedRow].m_end;
			Log.Info(this, "FindNext starting from {0}", start);
			
			long found;
			if (Find(pat, start, false, out found) && found == -1)
				SetTransientStatusMessage("End of file", Color.Azure, Color.Blue);
		}
		
		/// <summary>Search for an earlier occurance of a pattern in the grid</summary>
		private void FindPrev(Pattern pat)
		{
			if (pat == null || m_grid.RowCount == 0) return;
			m_last_find_pattern = pat;
			
			var start = SelectedRowRange.m_begin;
			Log.Info(this, "FindPrev starting from {0}", start);
			
			long found;
			if (Find(pat, start, true, out found) && found == -1)
				SetTransientStatusMessage("Start of file", Color.Azure, Color.Blue);
		}

		/// <summary>Show the export dialog</summary>
		private void ShowExportDialog()
		{
			if (!FileOpen) return;
			var dg = new ExportUI(
				Path.ChangeExtension(m_filepath, ".exported"+Path.GetExtension(m_filepath)),
				Misc.Humanise(m_encoding.GetString(m_row_delim)),
				Misc.Humanise(m_encoding.GetString(m_col_delim)),
				FileByteRange);
			if (dg.ShowDialog(this) != DialogResult.OK) return;
			
			Range rng;
			switch (dg.RangeToExport)
			{
			default: throw new ArgumentOutOfRangeException();
			case ExportUI.ERangeToExport.WholeFile: rng = FileByteRange; break;
			case ExportUI.ERangeToExport.Selection: rng = SelectedRowRange; break;
			case ExportUI.ERangeToExport.ByteRange: rng = dg.ByteRange; break;
			}
			
			// Do the export
			using (var outp = new StreamWriter(new FileStream(dg.OutputFilepath, FileMode.Create, FileAccess.Write, FileShare.Read)))
			{
				try
				{
					if (ExportLogFile(outp, m_filepath, rng, dg.ColDelim, dg.RowDelim))
						MessageBox.Show(this, Resources.ExportCompletedSuccessfully, Resources.ExportComplete, MessageBoxButtons.OK);
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, string.Format("Export failed.\r\nError: {0}",ex.Message), Resources.ExportFailed, MessageBoxButtons.OK);
				}
			}
		}

		/// <summary>
		/// Export the file 'filepath' using current filters to the stream 'outp'.
		/// Note: this method throws if an exception occurs in the background thread.
		/// </summary>
		/// <param name="outp">The stream to write the exported file to</param>
		/// <param name="filepath">The name of the file to export</param>
		/// <param name="rng">The range of bytes within 'filepath' to be exported</param>
		/// <param name="col_delim">The string to delimit columns with. (CR,LF,TAB converted to \r,\n,\t respectively)</param>
		/// <param name="row_delim">The string to delimit rows with. (CR,LF,TAB converted to \r,\n,\t respectively)</param>
		private bool ExportLogFile(StreamWriter outp, string filepath, Range rng, string col_delim, string row_delim)
		{
			// Although this search runs in a background thread, it's wrapped in a modal
			// dialog box, so it should be ok to use class members directly
			ProgressForm export = new ProgressForm("Exporting...", "", (s,a)=>
				{
					BackgroundWorker bgw = (BackgroundWorker)s;
					using (var file = LoadFile(filepath))
					{
						Line line             = new Line();
						int last_progress     = 0;
						rng.m_begin           = Maths.Clamp(rng.m_begin, 0, file.Length);
						rng.m_end             = Maths.Clamp(rng.m_end, 0, file.Length);
						row_delim             = Misc.Robitise(row_delim);
						col_delim             = Misc.Robitise(col_delim);
						bool ignore_blanks    = m_settings.IgnoreBlankLines;
						List<Filter> filters  = ActiveFilters.ToList();
						AddLineFunc test_line = (line_rng, baddr, fend, bf, enc) =>
							{
								int progress = (int)(100 * Maths.Ratio(rng.m_begin, baddr + line_rng.m_end, rng.m_end));
								if (progress != last_progress) { bgw.ReportProgress(progress); last_progress = progress; }
								
								if (line_rng.Empty && ignore_blanks)
									return true;
								
								// Parse the line from the buffer
								line.Read(baddr + line_rng.m_begin, bf, (int)line_rng.m_begin, (int)line_rng.Count, m_encoding, m_col_delim, null);
								
								// Keep searching while the text is filtered out or doesn't match the pattern
								if (!PassesFilters(line.RowText, filters)) return true;
								
								// Write to the output file
								outp.Write(string.Join(col_delim, line.Column));
								outp.Write(row_delim);
								return true;
							};
						
						byte[] buf = new byte[m_settings.MaxLineLength];
						
						// Find the start of a line (grow the range if necessary)
						rng.m_begin = FindLineStart(file, rng.m_begin, rng.m_end, m_row_delim, m_encoding, buf);
						
						// Read lines and write them to the export file
						FindLines(file, rng.m_begin, rng.m_end, false, rng.Count, test_line, m_encoding, m_row_delim, buf, (c,l) => !bgw.CancellationPending);
						a.Cancel = bgw.CancellationPending;
					}
				}){StartPosition = FormStartPosition.CenterParent};
			
			DialogResult res = DialogResult.Cancel;
			try { res = export.ShowDialog(this); }
			catch (OperationCanceledException) {}
			catch (Exception ex) { MessageBox.Show(this, "Exporting terminated due to an error.\r\nError Details:\r\n"+ex.Message, "Export error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
			return res == DialogResult.OK;
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
			OpenLogFile(files[0]);
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
			m_settings.FiltersEnabled = enable;
			ApplySettings();
			BuildLineIndex(m_filepos, true);
		}

		/// <summary>Turn on/off transforms</summary>
		private void EnableTransforms(bool enable)
		{
			m_settings.TransformsEnabled = enable;
			ApplySettings();
			BuildLineIndex(m_filepos, true);
		}

		/// <summary>Turn on/off tail mode</summary>
		private void EnableTail(bool enable)
		{
			m_settings.TailEnabled = enable;
			ApplySettings();
			if (enable) BuildLineIndex(m_filepos, m_settings.FileChangesAdditive);
		}
		
		/// <summary>Try to remove data from the log file</summary>
		private void ClearLogFile()
		{
			if (!FileOpen) return;
			var res = MessageBox.Show(this, string.Format("Attempt to clear the contents of file {0}?\r\nNote, this may not work if another application holds an exclusive lock on the file.",m_filepath), Resources.ConfirmClearLog, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
			if (res != DialogResult.OK) return;
			try
			{
				using (new FileStream(m_filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {}
				OpenLogFile(m_filepath); // reopen the file to purge any cached data
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, string.Format("Clearing file {0} failed.\r\nReason: {1}",m_filepath, ex.Message), Resources.ClearLogFailed, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
			string enc_name = encoding == null ? "" : encoding.EncodingName;
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
				ShowOptions(SettingsUI.ETab.General);
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
		private void ShowOptions(SettingsUI.ETab tab)
		{
			// Save current settings so the settingsUI starts with the most up to date
			// Show the settings dialog, then reload the settings
			var ui = new SettingsUI(tab);
			ui.ShowDialog(this);
			m_settings.Reload();
			
			ApplySettings();
			
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
		
		/// <summary>Show the TotD dialog</summary>
		private void ShowTotD()
		{
			new TipOfTheDay(m_settings).ShowDialog(this);
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
						
					Action done = () => HandleCheckForUpdateResult(res, err, show_dialog);
					BeginInvoke(done);
				};
			
			// Start the check for updates
			if (show_dialog)
			{
				var dg = new ProgressForm("Checking for Updates", "Querying the server for latest version information...", (s,a)=>
					{
						BackgroundWorker bgw = (BackgroundWorker)s;
						bgw.ReportProgress(100, new ProgressForm.UserState{ProgressBarStyle = ProgressBarStyle.Marquee, Icon = Icon});
						IAsyncResult async = INet.BeginCheckForUpdate(Constants.AppIdentifier, Constants.UpdateURL, null);
						
						// Wait till the operation completes, or until cancel is singled
						for (;!bgw.CancellationPending && !async.AsyncWaitHandle.WaitOne(500);) {}
						a.Cancel = bgw.CancellationPending;

						if (!a.Cancel) callback(async);
						else INet.CancelCheckForUpdate(async);
					});
				dg.ShowDialog(this);
			}
			else
			{
				// Start the asynchronous check for updates
				INet.BeginCheckForUpdate(Constants.AppIdentifier, Constants.UpdateURL, callback);
			}
		}

		/// <summary>Handle the results of a check for updates</summary>
		private void HandleCheckForUpdateResult(INet.CheckForUpdateResult res, Exception error, bool show_dialog)
		{
			if (error != null)
			{
				SetTransientStatusMessage("Check for updates error", Color.Red, SystemColors.Control);
				if (show_dialog) MessageBox.Show(this, string.Format("Check for updates failed\r\nError: {0}", error.Message), "Check for Updates Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				Version this_version = Assembly.GetExecutingAssembly().GetName().Version;
				if (this_version.CompareTo(res.Version) >  0)
				{
					SetTransientStatusMessage("Development version running");
					if (show_dialog) MessageBox.Show(this, "This version is newer than the latest version", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else if (this_version.CompareTo(res.Version) == 0)
				{
					SetTransientStatusMessage("Latest version running");
					if (show_dialog) MessageBox.Show(this, "This is the latest version", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					SetTransientStatusMessage("New Version Available!", Color.Green, SystemColors.Control);
					if (show_dialog)
					{
						var dg = new NewVersionForm
						{
							CurrentVersion = this_version.ToString(),
							LatestVersion  = res.Version.ToString(),
							WebsiteUrl     = res.InfoURL,
							DownloadUrl    = res.DownloadURL
						};
						dg.ShowDialog(this);
					}
				}
			}
		}

		/// <summary>Show the about dialog</summary>
		private void ShowAbout()
		{
			new About().ShowDialog(this);
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
			if (row_delim.Length == 0) return m_row_delim;
			return m_encoding.GetBytes(Misc.Robitise(row_delim));
		}

		/// <summary>Convert a column delimiter string into an encoded byte array</summary>
		private byte[] GetColDelim(string col_delim)
		{
			// If 'col_delim' is empty, then there is not column delimiter.
			return m_encoding.GetBytes(Misc.Robitise(col_delim));
		}

		/// <summary>Selected the row in the file that contains the byte offset 'addr'</summary>
		private void SelectRowByAddr(long addr)
		{
			// Ensure 'addr' is cached
			BuildLineIndex(addr, false, ()=>
			{
				// When the file is cached, select the row
				int idx = m_line_index.BinarySearch(r => r.m_end < addr ? -1 : r.m_begin > addr ? 1 : 0);
				if (idx < 0) idx = ~idx;
				SelectedRow = idx;
			});
		}

		/// <summary>
		/// Get/Set the currently selected grid row. Get returns -1 if there are no rows in the grid.
		/// Setting the selected row clamps to the range [0,RowCount) and makes it visible in the grid (if possible)</summary>
		private int SelectedRow
		{
			get { return m_grid.SelectedRows.Count != 0 ? m_grid.SelectedRows[0].Index : -1; }
			set
			{
				using (Scope.Create(()=>++m_suspend_grid_events, ()=>--m_suspend_grid_events))
				{
					value = m_grid.SelectRow(value);
					Log.Info(this, "Row {0} selected", value);
					
					if (m_grid.RowCount != 0 && value != -1)
					{
						int display_row = value - m_grid.DisplayedRowCount(true) / 2;
						m_grid.FirstDisplayedScrollingRowIndex = Maths.Clamp(display_row, 0, m_grid.RowCount - 1);
						UpdateStatus();
					}
				}
			}
		}
		
		/// <summary>Return true if we should auto scroll</summary>
		private bool AutoScrollTail
		{
			get
			{
				// Auto scroll if the last row of the file is visible and selected in the grid
				int row_delim_length = m_row_delim != null ? m_row_delim.Length : 0;
				return
					m_grid.RowCount != 0 &&                                         // the grid has data
					m_line_index.Count != 0 &&                                      // rows of the file are cached
					SelectedRow == m_grid.RowCount - 1 &&                           // last row selected
					m_grid.Rows[m_grid.RowCount - 1].Displayed &&                   // last row displayed
					m_line_index.Last().Contains(m_fileend - row_delim_length - 1); // last row in the file
			}
		}

		/// <summary>Scroll the grid to make the last row visible</summary>
		private void ShowLastRow()
		{
			if (m_grid.RowCount == 0) return;
			int displayed_rows = m_grid.DisplayedRowCount(false);
			int first_row = Math.Max(0, m_grid.RowCount - displayed_rows);
			m_grid.FirstDisplayedScrollingRowIndex = first_row;
			Log.Info(this, "Showing last row. First({0}) + Displayed({1}) = {2}. RowCount = {3}", first_row, displayed_rows, first_row + displayed_rows, m_grid.RowCount);
		}

		/// <summary>Returns true if grid event handlers should process grid events</summary>
		private bool GridEventsBlocked
		{
			get { return m_suspend_grid_events != 0; }
		}

		/// <summary>Helper for setting the grid row count without event handlers being fired</summary>
		private void SetGridRowCount(int count, int row_delta)
		{
			bool auto_scroll_tail = AutoScrollTail;
			if (m_grid.RowCount != count || row_delta != 0)
			{
				// Preserve the selected row index and first visible row index (if possible)
				int first_vis = m_grid.FirstDisplayedScrollingRowIndex;
				int selected = SelectedRow;
				SelectedRow = -1;
				
				Log.Info(this, "RowCount changed. Row delta {0}. Selected row: {1}->{2}. First visible row: {3}->{4}. Auto scroll {5}" ,row_delta ,selected ,selected+row_delta ,first_vis ,first_vis+row_delta ,auto_scroll_tail);
				m_grid.RowCount = 0;
				m_grid.RowCount = count;
				
				// Restore the selected row, and the first visible row
				if (count != 0)
				{
					// Restore the selected row, and the first visible row
					m_grid.SelectRow(auto_scroll_tail ? m_grid.RowCount - 1 : selected + row_delta);
					if (first_vis != -1) m_grid.FirstDisplayedScrollingRowIndex = Maths.Clamp(first_vis + row_delta, 0, m_grid.RowCount - 1);
				}
			}
			if (auto_scroll_tail) ShowLastRow();
		}
		
		/// <summary>
		/// Apply settings throughout the app.
		/// This method is called on startup to apply initial settings and
		/// after the settings dialog has been shown and closed. It needs to
		/// update anything that is only changed in the settings.
		/// Note: it does't trigger a file reload.</summary>
		private void ApplySettings()
		{
			Log.Info(this, "Applying settings");
			
			// Cached settings for performance, don't overwrite auto detected cached values tho
			m_encoding   = GetEncoding(m_settings.Encoding);
			m_row_delim  = GetRowDelim(m_settings.RowDelimiter);
			m_col_delim  = GetColDelim(m_settings.ColDelimiter);
			m_row_height = m_settings.RowHeight;
			m_bufsize    = m_settings.FileBufSize;
			m_line_cache_count = m_settings.LineCacheCount;
			
			// Tail
			m_watch_timer.Enabled = FileOpen && m_settings.TailEnabled;
			
			// Highlights;
			m_highlights.Clear();
			if (m_settings.HighlightsEnabled)
				m_highlights.AddRange(from hl in Highlight.Import(m_settings.HighlightPatterns) where hl.Active select hl);
			
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
			
			// Position UI elements
			m_menu.Location      = m_settings.MenuPosition;
			m_toolstrip.Location = m_settings.ToolsPosition;
			m_status.Location    = m_settings.StatusPosition;
			
			// Ensure rows are rerendered
			InvalidateCache();
			UpdateUI();
		}
		
		/// <summary>
		/// Update the UI with the current line index.
		/// This method should be called whenever a changes occurs that requires
		/// UI elements to be updated/redrawn. Note: it doesn't trigger a file reload.</summary>
		private void UpdateUI(int row_delta = 0)
		{
			Log.Info(this, "UpdateUI. Row delta {0}", row_delta);
			
			// Don't suspend events by removing/adding handlers because that pattern doesn't nest
			using (m_grid.SuspendLayout(true))
			using (Scope.Create(()=>++m_suspend_grid_events, ()=>--m_suspend_grid_events))
			{
				// Configure the grid
				if (m_line_index.Count != 0)
				{
					// Ensure the grid has the correct number of rows
					SetGridRowCount(m_line_index.Count, row_delta);
					
					// Measure each column's preferred width
					int[] col_widths = new int[m_grid.ColumnCount];
					int total_width = m_grid.Columns.Cast<DataGridViewColumn>().Sum(
						col => col_widths[col.Index] = col.GetPreferredWidth(DataGridViewAutoSizeColumnMode.DisplayedCells, true));
					
					// Resize columns. If the total width is less than the control width use the control width instead
					if (total_width < m_grid.Width && m_grid.AutoSizeColumnsMode != DataGridViewAutoSizeColumnsMode.Fill)
					{
						m_grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
						m_grid.AutoResizeColumns();
					}
					else if (total_width > m_grid.Width && m_grid.AutoSizeColumnsMode != DataGridViewAutoSizeColumnsMode.None)
					{
						m_grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
						foreach (DataGridViewColumn col in m_grid.Columns)
							col.Width = col_widths[col.Index];
					}
				}
				else
				{
					m_grid.ColumnHeadersVisible = false;
					m_grid.ColumnCount = 1;
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
			m_btn_highlights.Checked = m_settings.HighlightsEnabled;
			m_btn_filters.Checked    = m_settings.FiltersEnabled;
			m_btn_transforms.Checked = m_settings.TransformsEnabled;
			m_btn_tail.Checked       = m_watch_timer.Enabled;
			
			// The file scroll bar is only visible when part of the file is loaded
			m_scroll_file.Width = m_settings.FileScrollWidth;
			m_scroll_file.Ranges[(int)SubRangeScrollRange.FileRange     ].Color = m_settings.ScrollBarFileRangeColour;
			m_scroll_file.Ranges[(int)SubRangeScrollRange.DisplayedRange].Color = m_settings.ScrollBarDisplayRangeColour;
			m_scroll_file.Ranges[(int)SubRangeScrollRange.SelectedRange ].Color = m_settings.LineSelectBackColour;
			
			// Status and title
			UpdateStatus();
		}
		
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
				Text = string.Format("{0} - {1}" ,m_filepath ,Resources.AppTitle);
				m_status_spring.Text = "";
				
				// Add comma's to a large number
				Func<StringBuilder,StringBuilder> Pretty = sb=>
					{
						for (int i = sb.Length, j = 0; i-- != 0; ++j)
							if ((j%3) == 2 && i != 0) sb.Insert(i, ',');
						return sb;
					};

				// Get current file position
				int r = SelectedRow;
				long p = (r != -1) ? m_line_index[r].m_begin : 0;
				StringBuilder pos = Pretty(new StringBuilder(p.ToString(CultureInfo.InvariantCulture)));
				StringBuilder len = Pretty(new StringBuilder(m_file.Length.ToString(CultureInfo.InvariantCulture)));
				
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
				m_status_progress.Value = (int)(Maths.Ratio(0, current, total) * 100);
			}
		}

		/// <summary>Update the indicator ranges on the file scroll bar</summary>
		private void UpdateFileScroll()
		{
			Range range = LineIndexRange;//BufferRange(m_filepos, m_fileend, m_bufsize);
			int row_delim_len = m_row_delim != null ? m_row_delim.Length : 0;
			if (range.Count < m_fileend - row_delim_len)
			{
				if (!range.Equals(m_scroll_file.ThumbRange))
					Log.Info(this, "File scroll set to [{0},{1}) within file [{2},{3})", range.m_begin, range.m_end, FileByteRange.m_begin, FileByteRange.m_end);
				
				m_scroll_file.Visible    = true;
				m_scroll_file.TotalRange = FileByteRange;
				m_scroll_file.ThumbRange = range;
				m_scroll_file.Ranges[(int)SubRangeScrollRange.FileRange].Range      = range;
				m_scroll_file.Ranges[(int)SubRangeScrollRange.DisplayedRange].Range = DisplayedRowsRange;
				m_scroll_file.Ranges[(int)SubRangeScrollRange.SelectedRange].Range  = SelectedRowRange;
			}
			else
			{
				m_scroll_file.Visible = false;
			}
		}
		
		/// <summary>Create a message that displays for a period then disappears</summary>
		private void SetTransientStatusMessage(string text, Color frcol, Color bkcol, int display_time_ms = 2000)
		{
			m_status_message.Text = text;
			m_status_message.Visible = true;
			m_status_message.ForeColor = frcol;
			m_status_message.BackColor = bkcol;
			
			// If the status message has a timer already, dispose it
			Timer timer = m_status_message.Tag as Timer;
			if (timer != null) timer.Dispose();
			
			// Attach a new timer to the status message
			m_status_message.Tag = timer = new Timer{Enabled = true, Interval = display_time_ms};
			timer.Tick += (s,a)=>
				{
					// When the timer fires, if we're still associated with
					// the status message, null out the text and remove ourself
					if (s != m_status_message.Tag) return;
					m_status_message.Text = Resources.Idle;
					m_status_message.Visible = false;
					m_status_message.Tag = null;
					((Timer)s).Dispose();
				};
		}
		private void SetTransientStatusMessage(string text, int display_time_ms = 2000)
		{
			SetTransientStatusMessage(text, SystemColors.ControlText, SystemColors.Control, display_time_ms);
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
