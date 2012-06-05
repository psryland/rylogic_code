using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gui;
using pr.util;
using RyLogViewer.Properties;

namespace RyLogViewer
{
	public partial class Main :Form
	{
		//private class LineCache
		//{
		//    private const int LineCacheSize = 512;
		//    private readonly List<string> m_col = new List<string>();
		//    private byte[] m_buf   = new byte[LineCacheSize];
		//    private int    m_index = -1;
			
		//    // Column accessors
		//    public int Count            { get { return m_col.Count; } }
		//    public string this[int col] { get { return m_col[col]; } }
		//    public override string ToString() { return Count != 0 ? this[0] : ""; }
			
		//    /// <summary>Read a line from 'file' and divide it into columns</summary>
		//    public void Cache(FileStream file, Encoding encoding, List<Range> line_index, int index, string column_delimiter)
		//    {
		//        if (m_index == index) return; // Already cached
		//        m_index = index;
				
		//        // Determine the length of the line
		//        Range rng = line_index[index];
				
		//        // Read the whole line into m_buf
		//        file.Seek(rng.m_begin, SeekOrigin.Begin);
		//        m_buf = rng.Count <= m_buf.Length ? m_buf : new byte[rng.Count];
		//        int read = file.Read(m_buf, 0, (int)rng.Count);
		//        if (read != rng.Count) throw new IOException("failed to read file over range ["+rng.m_begin+","+rng.m_end+"). Read "+read+"/"+rng.Count+" bytes.");
		//        string line = encoding.GetString(m_buf, 0, read);

		//        // Split the line up into columns
		//        m_col.Clear();
		//        if (column_delimiter.Length != 0)
		//            m_col.AddRange(line.Split(new[]{column_delimiter}, StringSplitOptions.None));
		//        else
		//            m_col.Add(line);
		//    }
		//}

		private const string LogFileFilter = @"Text Files (*.txt;*.log;*.csv)|*.txt;*.log;*.csv|All files (*.*)|*.*";
		private const int AvrBytesPerLine = 256;
		private const int CacheSize = 4096;
		
		private readonly Settings m_settings;          // App settings
		private readonly RecentFiles m_recent;         // Recent files
		private readonly FileSystemWatcher m_watch;    // A FS watcher to detect file changes
		private readonly EventBatcher m_file_changed;  // Event batcher for m_watch events
		private readonly List<Highlight> m_highlights; // A list of the active highlights only
		private List<Range> m_line_index;              // Byte offsets (from file begin) to the byte range of a line
		private Encoding m_encoding;                   // The file encoding
		private string m_filepath;                     // The path of the log file we're viewing
		private FileStream m_file;                     // A file stream of 'm_filepath'
		private byte[] m_row_delim;                    // The row delimiter converted from a string to a byte[] using the current encoding
		private char[] m_col_delim;                    // The column delimiter, cached to prevent m_settings access in CellNeeded
		private int m_row_height;                      // The row height, cached to prevent settings lookups in CellNeeded
		private long m_last_line;                      // The byte offset (from file begin) to the start of the last known line
		private long m_file_end;                       // The last known size of the file
		
		//todo:
		// find
		// profile scrolling, get rid of duplicate line renderering (maybe cache actual strings?)
		// cache highlighted state
		// extend the line cache to hold more than one line
		// read stdin
		// Tooltips
		// partial highlighting
		// Large file selection on first line loads next earlier chunk
		// UpdateLineCount needs to reload from file end if more than LineCount new lines
		// Window transparency (Ghost mode) with click through
		// Tip of the Day content

		public Main(string[] args)
		{
			InitializeComponent();
			m_settings     = new Settings();
			m_recent       = new RecentFiles(m_menu_file_recent, OpenLogFile);
			m_watch        = new FileSystemWatcher{NotifyFilter = NotifyFilters.LastWrite|NotifyFilters.Size};
			m_file_changed = new EventBatcher(10, this);
			m_highlights   = new List<Highlight>();
			m_line_index   = new List<Range>();
			m_filepath     = null;
			m_file         = null;
			m_last_line    = 0;
			m_file_end     = 0;
			InitCache();
			ApplySettings();
			
			// Menu
			m_menu.Move                             += (s,a) => { m_settings.MenuPosition = m_menu.Location; m_settings.Save(); };
			m_menu_file_open.Click                  += (s,a) => OpenLogFile(null);
			m_menu_file_close.Click                 += (s,a) => CloseLogFile();
			m_menu_file_exit.Click                  += (s,a) => Close();
			m_menu_edit_selectall.Click             += (s,a) => DataGridView_Extensions.SelectAll(m_grid, new KeyEventArgs(Keys.Control|Keys.A));
			m_menu_edit_copy.Click                  += (s,a) => DataGridView_Extensions.Copy(m_grid, new KeyEventArgs(Keys.Control|Keys.C));
			m_menu_edit_find.Click                  += (s,a) => ShowFindDialog();
			m_menu_edit_find_next.Click             += (s,a) => FindNext();
			m_menu_edit_find_prev.Click             += (s,a) => FindPrev();
			m_menu_encoding_ascii.Click             += (s,a) => SetEncoding(Encoding.ASCII           );
			m_menu_encoding_utf8.Click              += (s,a) => SetEncoding(Encoding.UTF8            );
			m_menu_encoding_ucs2_littleendian.Click += (s,a) => SetEncoding(Encoding.Unicode         );
			m_menu_encoding_ucs2_bigendian.Click    += (s,a) => SetEncoding(Encoding.BigEndianUnicode);
			m_menu_tools_alwaysontop.Click          += (s,a) => { m_settings.AlwaysOnTop = !m_settings.AlwaysOnTop; TopMost = m_settings.AlwaysOnTop; };
			m_menu_tools_highlights.Click           += (s,a) => ShowOptions(SettingsUI.ETab.Highlights);
			m_menu_tools_filters.Click              += (s,a) => ShowOptions(SettingsUI.ETab.Filters);
			m_menu_tools_options.Click              += (s,a) => ShowOptions(SettingsUI.ETab.General);
			m_menu_help_totd.Click                  += (s,a) => ShowTotD();
			m_menu_help_about.Click                 += (s,a) => ShowAbout();
			m_recent.Import(m_settings.RecentFiles);
			
			// Toolbar
			m_toolstrip.Move            += (s,a) => { m_settings.ToolsPosition = m_toolstrip.Location; m_settings.Save(); };
			m_btn_open_log.Click        += (s,a) => OpenLogFile(null);
			m_btn_refresh.Click         += (s,a) => Reload();
			m_check_tail.CheckedChanged += (s,a) => EnableTail(m_check_tail.Checked);
			m_btn_highlights.Click      += (s,a) => ShowOptions(SettingsUI.ETab.Highlights);
			m_btn_filters.Click         += (s,a) => ShowOptions(SettingsUI.ETab.Filters);
			m_btn_open_log.ToolTipText   = "Open a log file";   
			m_btn_refresh.ToolTipText    = "Reload the current log file";
			m_check_tail.ToolTipText     = "Scroll to the last log file row, and automatically load changes";
			m_btn_highlights.ToolTipText = "Show the highlights dialog";
			m_btn_filters.ToolTipText    = "Show the filters dialog";
			
			// Status
			m_status.Move += (s,a) => { m_settings.StatusPosition = m_status.Location; m_settings.Save(); };
			
			// Setup the grid
			m_grid.RowCount             = 0;
			m_grid.AutoGenerateColumns  = false;
			m_grid.KeyDown             += DataGridView_Extensions.SelectAll;
			m_grid.KeyDown             += DataGridView_Extensions.Copy;
			m_grid.CellValueNeeded     += CellValueNeeded;
			m_grid.CellPainting        += CellPainting;
			m_grid.RowHeightInfoNeeded += (s,a) => { a.Height = m_row_height; };
			m_grid.SelectionChanged    += (s,a) => { UpdateStatus(); };
			m_grid.DataError           += (s,a) => { Debug.Assert(false); };
			
			// Settings
			m_settings.SettingsLoaded += (s,a) => ApplySettings();
			
			// Watcher
			m_file_changed.Action += UpdateLineIndex;
			m_watch.Changed += (s,a) =>
				{
					// This happens sometimes, not really sure why
					if (m_file.Length == 0) return;
					m_file_changed.Signal();
				};
			
			// Startup
			Shown += (s,a)=>
				{
					// Last screen position
					if (m_settings.RestoreScreenLoc)
					{
						Location = m_settings.ScreenPosition;
						Size = m_settings.WindowSize;
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
					ApplySettings();
					UpdateUI();

					// Show the TotD
					if (m_settings.ShowTOTD)
						ShowTotD();
				};
			
			// File Drop
			DragEnter += (s,a) => FileDrop(a, true);
			DragDrop  += (s,a) => FileDrop(a, false);

			// Shutdown
			FormClosing += (s,a) =>
				{
					m_settings.ScreenPosition = Location;
					m_settings.WindowSize = Size;
					m_settings.RecentFiles = m_recent.Export();
					m_settings.Save();
				};
		}

		/// <summary>Returns true if there is a log file currently open</summary>
		private bool FileOpen
		{
			get { return m_file != null; }
		}

		/// <summary>Parse the command line parameters</summary>
		private void ParseCommandLine(string[] args)
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

		/// <summary>Apply settings throughout the app</summary>
		private void ApplySettings()
		{
			// Apply line count limit
			if (m_line_index != null && m_line_index.Count > m_settings.LineCount)
				Reload(); // todo, move this
			
			// Cached settings for performance
			m_encoding   = GetEncoding(m_settings.Encoding);
			m_row_delim  = m_encoding.GetBytes(m_settings.RowDelimiter.Replace("CR","\r").Replace("LF","\n"));
			m_col_delim  = m_settings.ColDelimiter.ToCharArray();
			m_row_height = m_settings.RowHeight;
			
			// Tail
			m_watch.EnableRaisingEvents = FileOpen && m_settings.TailEnabled;
			
			// Highlights;
			m_highlights.Clear();
			m_highlights.AddRange(from hl in Highlight.Import(m_settings.HighlightPatterns) where hl.Active select hl);
			
			// Check states
			m_check_tail.Checked = m_settings.TailEnabled;
			
			// Row styles
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
		}
		
		/// <summary>Close the current log file</summary>
		private void CloseLogFile()
		{
			if (FileOpen) m_file.Dispose();
			m_line_index.Clear();
			m_filepath = null;
			m_file = null;
			m_last_line = 0;
			m_file_end = 0;
			UpdateUI();
		}
		
		/// <summary>Prompt to open a log file</summary>
		private void OpenLogFile(string filepath)
		{
			// Prompt for a file if none provided
			if (filepath == null)
			{
				var fd = new OpenFileDialog{Filter = LogFileFilter, Multiselect = false};
				if (fd.ShowDialog() != DialogResult.OK) return;
				filepath = fd.FileName;
			}
			
			// Reject invalid filepaths
			if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
			{
				MessageBox.Show(this, string.Format(Resources.InvalidFileMsg, filepath), Resources.InvalidFilePath, MessageBoxButtons.OK,MessageBoxIcon.Error);
				return;
			}
		
			m_recent.Add(filepath);
			m_settings.RecentFiles = m_recent.Export();
			m_settings.LastLoadedFile = filepath;
			m_settings.Save();
			
			// Setup the watcher to watch for file changes
			m_watch.Path = Path.GetDirectoryName(filepath);
			m_watch.Filter = Path.GetFileName(filepath);
			
			// Switch files
			CloseLogFile();
			BuildLineIndex(filepath);
			ApplySettings(); // ensure settings that need a file to work are turned on
		}

		/// <summary>Supply the grid with values</summary>
		private void CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			if (!FileOpen) e.Value = null;
			else
			{
				try { e.Value = CacheLine(e.RowIndex)[e.ColumnIndex].Text; }
				catch { e.Value = ""; }
			}
		}
		
		/// <summary>Draw the cell appropriate to any highlighting</summary>
		private void CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			// Check if the cell value has a highlight pattern it matches
			Highlight hl = (e.RowIndex != -1) ? CacheLine(e.RowIndex)[e.ColumnIndex].HL : null;
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

		/// <summary>Show the find dialog</summary>
		private void ShowFindDialog()
		{
			Point pt = m_grid.PointToScreen(m_grid.Location);
			var find = new FindUI();
			find.FindNext += (s,a) => FindNext();
			find.FindPrev += (s,a) => FindPrev();
			find.Location = pt + new Size(Width - find.Width, 0);
			find.Show(this);
		}
		
		/// <summary>Search for the next occurance of a pattern in the grid</summary>
		private void FindNext()
		{
		}
		
		/// <summary>Search for an earlier occurance of a pattern in the grid</summary>
		private void FindPrev()
		{
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
		
		/// <summary>True on auto detect of file changes</summary>
		private void EnableTail(bool enable)
		{
			// If the last row isn't visible scroll it into view before enabling tail
			if (!LastRowVisible)
			{
				ShowLastRow();
			}
			else
			{
				m_settings.TailEnabled = enable;
			}
			ApplySettings();
			UpdateLineIndex();
			SelectedRow = m_grid.RowCount - 1;
		}
		
		/// <summary>Set the encoding to use with loaded files</summary>
		private void SetEncoding(Encoding encoding)
		{
			m_encoding = encoding;
			m_settings.Encoding = m_encoding.EncodingName;
			m_settings.Save();
			ApplySettings();
			Reload();
		}
		
		/// <summary>Display the options dialog</summary>
		private void ShowOptions(SettingsUI.ETab tab)
		{
			// Save the current highlight and filter patterns so we can detected
			// changes to them and reload the line index if they change.
			string highlights = FileOpen ? m_settings.HighlightPatterns : null;
			string filters    = FileOpen ? m_settings.FilterPatterns    : null;
			
			// Save current settings so the settingsUI starts with the most up to date
			// Show the settings dialog, then reload the settings
			m_settings.Save();
			new SettingsUI(tab).ShowDialog(this);
			m_settings.Reload();
			ApplySettings();
			
			// If the highlight patterns have changed, invalidate the line cache
			if (FileOpen && m_settings.HighlightPatterns != highlights)
				InvalidateCache();
			
			// If the filter patterns have changed, reload the file
			if (FileOpen && m_settings.FilterPatterns != filters)
				Reload();
			
			Refresh();
		}
		
		/// <summary>Show the TotD dialog</summary>
		private void ShowTotD()
		{
			new TipOfTheDay(m_settings).ShowDialog(this);
		}

		/// <summary>Show the about dialog</summary>
		private void ShowAbout()
		{
			new About().ShowDialog(this);
		}

		/// <summary>Convert an encoding name to encoding object</summary>
		private static Encoding GetEncoding(string encoding_name)
		{
			if (encoding_name == Encoding.ASCII           .EncodingName) return Encoding.ASCII           ;
			if (encoding_name == Encoding.UTF8            .EncodingName) return Encoding.UTF8            ;
			if (encoding_name == Encoding.Unicode         .EncodingName) return Encoding.Unicode         ;
			if (encoding_name == Encoding.BigEndianUnicode.EncodingName) return Encoding.BigEndianUnicode;
			throw new NotSupportedException("Text file encoding '"+encoding_name+"' is not supported");
		}

		/// <summary>Get/Set the currently selected grid row</summary>
		private int SelectedRow
		{
			get { return m_grid.SelectedRows.Count != 0 ? m_grid.SelectedRows[0].Index : -1; }
			set { if (value != SelectedRow) m_grid.SelectRow(value); }
		}
		
		/// <summary>Returns true if the last row is visible</summary>
		private bool LastRowVisible
		{
			get { return m_grid.RowCount == 0 || m_grid.Rows[m_grid.RowCount-1].Displayed; }
		}

		/// <summary>Return true if we should auto scroll</summary>
		private bool AutoScrollTail
		{
			get { return LastRowVisible && SelectedRow == m_grid.RowCount - 1; }
		}

		/// <summary>Scroll the grid to make the last row visible</summary>
		private void ShowLastRow()
		{
			if (m_grid.RowCount == 0) return;
			int displayed_rows = m_grid.DisplayedRowCount(false);
			m_grid.FirstDisplayedScrollingRowIndex = Math.Max(0, m_grid.RowCount - displayed_rows);
		}

		/// <summary>Update the UI with the current line index</summary>
		private void UpdateUI()
		{
			// Configure the grid
			if (m_line_index.Count != 0)
			{
				// Read a row from the data and show column headers if there is more than one
				Line line = CacheLine(m_line_index.Count - 1);
				m_grid.ColumnHeadersVisible = line.Column.Count > 1;
				m_grid.ColumnCount = line.Column.Count;
				
				bool auto_scroll = AutoScrollTail;
				if (m_grid.RowCount != m_line_index.Count)
				{
					int selected = SelectedRow; // Preserve the selection across row count change
					m_grid.RowCount = 0; // Reset to zero first, this is more efficient for some reason
					m_grid.RowCount = m_line_index.Count;
					SelectedRow = auto_scroll ? m_grid.RowCount - 1 : selected;
				}
				if (auto_scroll) ShowLastRow();
				m_grid.Refresh();
			}
			else
			{
				m_grid.ColumnHeadersVisible = false;
				m_grid.RowCount = 0;
				m_grid.ColumnCount = 0;
			}
			
			// Configure menus
			m_menu_encoding_ascii            .Checked = m_settings.Encoding == Encoding.ASCII.EncodingName;
			m_menu_encoding_utf8             .Checked = m_settings.Encoding == Encoding.UTF8.EncodingName;
			m_menu_encoding_ucs2_littleendian.Checked = m_settings.Encoding == Encoding.Unicode.EncodingName;
			m_menu_encoding_ucs2_bigendian   .Checked = m_settings.Encoding == Encoding.BigEndianUnicode.EncodingName;
			
			// Status and title
			UpdateStatus();
		}
		
		/// <summary>Update the status bar</summary>
		private void UpdateStatus()
		{
			if (!FileOpen)
			{
				Text = Resources.AppTitle;
				m_lbl_file_size.Text = Resources.NoFile;
			}
			else
			{
				Text = string.Format("{0} - {1}" ,m_filepath ,Resources.AppTitle);
				
				// Add comma's to a large number
				Func<StringBuilder,StringBuilder> Pretty = (sb)=>
					{
						for (int i = sb.Length, j = 0; i-- != 0; ++j)
							if ((j%3) == 2 && i != 0) sb.Insert(i, ',');
						return sb;
					};

				// Get current file position
				int r = SelectedRow;
				long p = (r != -1) ? m_line_index[SelectedRow].m_begin : 0;
				StringBuilder pos = Pretty(new StringBuilder(p.ToString()));
				StringBuilder len = Pretty(new StringBuilder(m_file.Length.ToString()));
				m_lbl_file_size.Text = string.Format(Resources.PositionXofYBytes, pos, len);
			}
		}
	}
}
