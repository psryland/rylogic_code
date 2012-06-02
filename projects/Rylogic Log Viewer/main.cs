using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;
using Rylogic_Log_Viewer.Properties;

namespace Rylogic_Log_Viewer
{
	public partial class Main :Form
	{
		private class LineCache
		{
			private const int LineCacheSize = 512;
			private readonly List<string> m_col   = new List<string>();
			private byte[] m_buf   = new byte[LineCacheSize];
			private int    m_index = -1;
			
			// Column accessors
			public int Count            { get { return m_col.Count; } }
			public string this[int col] { get { return m_col[col]; } }
			
			public void Cache(FileStream file, List<long> line_index, int index, byte column_delimiter)
			{
				if (m_index == index) return; // Already cached
				m_index = index;
				
				// Determine the length of the line
				long beg = line_index[index];
				long end = index+1 == line_index.Count ? file.Length : line_index[index+1];
				int len = (int)(end - beg);
				
				// Read the whole line into m_buf
				file.Seek(beg, SeekOrigin.Begin);
				m_buf = len <= m_buf.Length ? m_buf : new byte[len];
				int read = file.Read(m_buf, 0, len);
				if (read != len)
					throw new IOException("failed to read file over range ["+beg+","+end+"). Read "+read+"/"+len+" bytes.");
				
				// Split the line up into columns and convert to strings
				m_col.Clear();
				int b = 0, e = 0;
				for (; e != len; ++e)
				{
					if (m_buf[e] != column_delimiter) continue;
					m_col.Add(Encoding.ASCII.GetString(m_buf, b, e));
					b = e + 1;
				}
				if (b != e)
					m_col.Add(Encoding.ASCII.GetString(m_buf, b, e));
			}
		}
		private const int AvrBytesPerLine = 256;
		private const int CacheSize = 4096;
		
		private readonly Settings m_settings;          // App settings
		private readonly RecentFiles m_recent;         // Recent files
		private readonly FileSystemWatcher m_watch;    // A FS watcher to detect file changes
		private readonly EventBatcher m_file_changed;  // Event batcher for m_watch events
		private readonly List<Highlight> m_highlights;  // A list of the active highlights only
		private readonly LineCache m_line_cache;       // Optimisation for reading whole lines on multi-column grids
		private List<long> m_line_index;               // Byte offsets (from file begin) to the start of lines
		private string m_filepath;                     // The path of the log file we're viewing
		private FileStream m_file;                     // A file stream of 'm_filepath'
		private long m_end;                            // The current reference point in the file
		
		//todo:
		// command line
		// read stdin
		// do filtering
		// unicode text files


		public Main()
		{
			InitializeComponent();
			m_settings = new Settings();
			m_recent = new RecentFiles(m_menu_file_recent, OpenLogFile);
			m_recent.Import(m_settings.RecentFiles);
			m_watch = new FileSystemWatcher{NotifyFilter = NotifyFilters.LastWrite|NotifyFilters.Size};
			m_file_changed = new EventBatcher(10, this);
			m_highlights = new List<Highlight>();
			m_line_cache = new LineCache();
			m_line_index = null;
			m_filepath = null;
			m_file = null;
			m_end = 0;
			
			// Menu
			m_menu_file_open.Click         += (s,a) => OpenLogFile(null);
			m_menu_file_close.Click        += (s,a) => CloseLogFile();
			m_menu_file_exit.Click         += (s,a) => Close();
			m_menu_edit_selectall.Click    += (s,a) => {};//SelectAll();
			m_menu_edit_copy.Click         += (s,a) => {};//Copy();
			m_menu_edit_find.Click         += (s,a) => {};//Find();
			m_menu_edit_find_next.Click    += (s,a) => {};
			m_menu_edit_find_prev.Click    += (s,a) => {};
			m_menu_tools_alwaysontop.Click += (s,a) => { m_settings.AlwaysOnTop = !m_settings.AlwaysOnTop; TopMost = m_settings.AlwaysOnTop; };
			m_menu_tools_highlights.Click  += (s,a) => ShowOptions(SettingsUI.ETab.Highlights);
			m_menu_tools_filters.Click     += (s,a) => ShowOptions(SettingsUI.ETab.Filters);
			m_menu_tools_options.Click     += (s,a) => ShowOptions(SettingsUI.ETab.General);
			m_menu_help_about.Click        += (s,a) => {};
			
			// Toolbar
			m_btn_open_log.Click        += (s,a) => OpenLogFile(null);
			m_check_tail.CheckedChanged += (s,a) => EnableTail(m_check_tail.Checked);
			m_btn_highlights.Click      += (s,a) => ShowOptions(SettingsUI.ETab.Highlights);
			m_btn_filters.Click         += (s,a) => ShowOptions(SettingsUI.ETab.Filters);
			
			// Setup the grid
			m_grid.RowCount            = 0;
			m_grid.AutoGenerateColumns = false;
			m_grid.KeyDown            += DataGridView_Extensions.SelectAll;
			m_grid.KeyDown            += DataGridView_Extensions.Copy;
			m_grid.CellValueNeeded    += CellValueNeeded;
			m_grid.CellPainting       += CellPainting;
			
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
					if (m_settings.RestoreScreenLoc)
					{
						Location = m_settings.ScreenPosition;
						Size = m_settings.WindowSize;
					}
					if (m_settings.LoadLastFile && File.Exists(m_settings.LastLoadedFile))
					{
						OpenLogFile(m_settings.LastLoadedFile);
					}
					ApplySettings();
					UpdateStatus();
				};
			
			// Shutdown
			FormClosing += (s,a) =>
				{
					m_settings.ScreenPosition = Location;
					m_settings.WindowSize = Size;
					m_settings.RecentFiles = m_recent.Export();
					m_settings.Save();
				};
		}
		
		/// <summary>True on auto detect of file changes</summary>
		private void EnableTail(bool enable)
		{
			m_settings.TailEnabled = enable;
			ApplySettings();
			UpdateLineIndex();
			SelectedRow = m_grid.RowCount - 1;
		}
		
		/// <summary>Apply settings throughout the app</summary>
		private void ApplySettings()
		{
			// Tail
			m_watch.EnableRaisingEvents = m_file != null && m_settings.TailEnabled;
			
			// Highlights;
			m_highlights.Clear();
			m_highlights.AddRange(from hl in Highlight.Import(m_settings.HighlightPatterns) where hl.Active select hl);

			// Check states
			m_check_tail.Checked = m_watch.EnableRaisingEvents;
			
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
		}
		
		/// <summary>Returns a file stream for 'filepath' openned with R/W sharing</summary>
		private FileStream LoadFile(string filepath)
		{
			return new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, m_settings.LineCount * AvrBytesPerLine);
		}
		
		/// <summary>Close the current log file</summary>
		private void CloseLogFile()
		{
			if (m_file != null) m_file.Dispose();
			m_line_index = null;
			m_filepath = null;
			m_file = null;
			m_end = 0;
			UpdateUI();
		}
		
		/// <summary>Prompt to open a log file</summary>
		private void OpenLogFile(string filepath)
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
				MessageBox.Show(this, Resources.InvalidFilePath, string.Format(Resources.InvalidFileMsg, filepath), MessageBoxButtons.OK,MessageBoxIcon.Error);
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
		}
		
		/// <summary>Build a line index of the file</summary>
		private void BuildLineIndex(string filepath)
		{
			// Variables used by the follow async task
			int max_line_count = m_settings.LineCount;
			byte row_delimiter = m_settings.RowDelimiter;
			List<long> line_index = new List<long>();
			long end = 0;
			
			// Do this in a background thread, in case it takes ages
			ProgressForm task = new ProgressForm(Resources.BuildingLineIndex, string.Format(Resources.ReadingXLineFromY, m_settings.LineCount, filepath), (s,a)=>
				{
					BackgroundWorker bgw = (BackgroundWorker)s;
					
					// A temporary buffer for reading sections of the file
					byte[] buf = new byte[CacheSize]; 
					
					// Read the last 'LineCount' line indices into memory
					// Search backward through the file counting lines to get to the byte indices
					using (var file = LoadFile(filepath))
					{
						// Save the file length in 'end' and then use this value rather than
						// 'file.Length' just in case the file gets modified while we're using it.
						end = file.Length;
						file.Seek(end, SeekOrigin.Begin); // see above for why not Seek(0,SeekOrigin.End)
						for (long pos = end; pos != 0 && line_index.Count != max_line_count && !bgw.CancellationPending;)
						{
							int pc = 100 * line_index.Count / max_line_count;
							bgw.ReportProgress(pc);
								
							// The number of bytes to buffer
							int count = (int)Math.Min(pos, buf.Length);
								
							// Buffer file data
							file.Seek(pos - count, SeekOrigin.Begin);
							int read = file.Read(buf, 0, count);
							if (read != count) throw new IOException("failed to read file over range ["+(pos-count)+","+(pos+count)+"). Read "+read+"/"+count+" bytes.");
								
							// Search backwards counting lines
							for (;read-- != 0 && line_index.Count != max_line_count; --pos)
							{
								if (buf[read] != row_delimiter) continue;
								if (pos != end) line_index.Add(pos); // pos == end is a special case for last character == row delimiter
							}
							if (pos == 0) line_index.Add(0);
						}
					}
					
					// Reverse the line index list so that the last line is at the end
					line_index.Reverse();
					a.Cancel = bgw.CancellationPending;
				});
			
			// Don't use the results if the task was cancelled
			DialogResult res;
			try { res = task.ShowDialog(this); }
			catch (Exception ex) { res = MessageBox.Show(this, Resources.ReadingFileFailed, string.Format(Resources.BuildLineIndexErrorMsg, ex.Message), MessageBoxButtons.RetryCancel, MessageBoxIcon.Error); }
			if (res == DialogResult.Retry)
			{
				Action<string> retry = BuildLineIndex;
				BeginInvoke(retry, filepath);
				return;
			}
			if (res != DialogResult.OK)
				return;
			
			// Update the member variables once the line index is complete
			m_line_index = line_index;
			m_filepath = filepath;
			m_file = LoadFile(m_filepath);
			m_end = end;
			UpdateUI();
		}
		
		/// <summary>Extend the line index past 'm_end'</summary>
		private void UpdateLineIndex()
		{
			// Get the range of bytes to read from the file
			long pos = m_line_index.Count != 0 ? m_line_index[m_line_index.Count-1] : 0;
			long end = m_file.Length; // Read the new end of the file
			if (end < m_end)
			{
				// If the file has shrunk, rebuild the index
				BuildLineIndex(m_filepath);
				return;
			}
			
			// Variables used by the follow async task
			string filepath = m_filepath;
			byte row_delim = m_settings.RowDelimiter;
			
			// Find the new line indices in a background thread
			ThreadPool.QueueUserWorkItem(a=>
				{
					try
					{
						List<long> line_index = new List<long>();
						using (var file = LoadFile(filepath))
						{
							// Seek to the start of the last known line
							file.Seek(pos, SeekOrigin.Begin);
						
							// A temporary buffer for reading sections of the file
							byte[] buf = new byte[CacheSize]; 
						
							// Scan forward reading new lines
							for (;pos != end;)
							{
								// Buffer file data
								int count = (int)Math.Min(end - pos, buf.Length);
								int read = file.Read(buf, 0, count);
							
								// Search for new lines
								for (int i = 0; i != read; ++i, ++pos)
								{
									if (buf[i] != row_delim) continue;
									if (pos+1 != end) line_index.Add(pos+1); // special case for last character == row delimiter
								}
							}
						}
					
						// Marshal the results back to the main thread
						Action AddToLineIndex = ()=>
						{
							m_line_index.AddRange(line_index);
							m_end = end;
							if (m_file.Length != m_end) m_file_changed.Signal(); // Run it again if the files changed again
							UpdateUI();
						};
						BeginInvoke(AddToLineIndex);
					}
					catch (Exception)
					{}
				});
		}
		
		/// <summary>Supply the grid with values</summary>
		private void CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			if (m_file == null) e.Value = null;
			else
			{
				// Read the line into memory and split it into columns
				m_line_cache.Cache(m_file, m_line_index, e.RowIndex, m_settings.ColDelimiter);
				e.Value = (e.ColumnIndex >= 0 && e.ColumnIndex < m_line_cache.Count) ? m_line_cache[e.ColumnIndex] : "";
			}
		}
		
		/// <summary>Draw the cell appropriate to any highlighting</summary>
		private void CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			string value = (string)e.Value;
			
			// Check if the cell value matches any of the highlight patterns
			Highlight hl = null;
			foreach (var h in m_highlights)
			{
				if (!h.IsMatch(value)) continue;
				hl = h;
				break;
			}

			if (hl == null)
			{
				e.PaintBackground(e.ClipBounds, e.RowIndex == SelectedRow);
				e.PaintContent(e.ClipBounds);
			}
			else if (hl.FullColumn)
			{
				e.CellStyle.BackColor = hl.BackColour;
				e.CellStyle.ForeColor = hl.ForeColour;
				e.PaintBackground(e.ClipBounds, false);
				e.PaintContent(e.ClipBounds);
			}
			else
			{
				e.PaintBackground(e.ClipBounds, false);
				e.PaintContent(e.ClipBounds);
			}
		}

		/// <summary>Display the options dialog</summary>
		private void ShowOptions(SettingsUI.ETab tab)
		{
			m_settings.Save();
			var settings = new SettingsUI(tab);
			settings.ShowDialog(this);
			m_settings.Reload();
			ApplySettings();
			Refresh();
		}
		
		/// <summary>Get/Set the currently selected grid row</summary>
		private int SelectedRow
		{
			get { return m_grid.SelectedRows.Count != 0 ? m_grid.SelectedRows[0].Index : -1; }
			set { if (value != SelectedRow) m_grid.SelectRow(value); }
		}
		
		/// <summary>Return true if we should auto scroll</summary>
		private bool AutoScrollTail
		{
			get { return SelectedRow == m_grid.RowCount - 1; }
		}

		/// <summary>Update the UI with the current line index</summary>
		private void UpdateUI()
		{
			// Configure the grid
			if (m_file != null)
			{
				// Read a row from the data and show column headers if there is more than one
				m_line_cache.Cache(m_file, m_line_index, 0, m_settings.ColDelimiter);
				m_grid.ColumnHeadersVisible = m_line_cache.Count > 1;
				m_grid.ColumnCount = m_line_cache.Count;
				if (m_grid.RowCount != m_line_index.Count)
				{
					// Preserve the selection across row count change
					bool auto_scroll = AutoScrollTail;
					int selected = SelectedRow;
					
					m_grid.RowCount = 0; // Reset to zero first, this is more efficient for some reason
					m_grid.RowCount = m_line_index.Count;
					SelectedRow = auto_scroll ? m_grid.RowCount - 1 : selected;
					if (auto_scroll) m_grid.FirstDisplayedScrollingRowIndex = m_grid.RowCount - m_grid.DisplayedRowCount(false);
				}
			}
			else
			{
				m_grid.ColumnHeadersVisible = false;
				m_grid.RowCount = 0;
				m_grid.ColumnCount = 0;
			}
			UpdateStatus();
		}
		
		/// <summary>Update the status bar</summary>
		private void UpdateStatus()
		{
			if (m_file == null)
			{
				Text = Resources.AppTitle;
				m_lbl_file_size.Text = Resources.NoFile;
			}
			else
			{
				Text = string.Format("{0} - {1}" ,m_filepath ,Resources.AppTitle);
				StringBuilder sb = new StringBuilder(m_file.Length.ToString());
				for (int i = sb.Length, j = 0; i-- != 0; ++j) if ((j%3) == 2 && i != 0) sb.Insert(i, ',');
				m_lbl_file_size.Text = string.Format(Resources.SizeXBytes, sb);
			}
		}
	}
}
