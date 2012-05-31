using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using pr.gui;
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
		
		private readonly Settings m_settings;        // App settings
		private readonly RecentFiles m_recent;       // Recent files
		private readonly FileSystemWatcher m_watch;  // A FS watcher to detect file changes
		private readonly LineCache m_line_cache;     // Optimisation for reading whole lines on multi-column grids
		private List<long> m_line_index;             // Byte offsets (from file begin) to the start of lines
		private string m_filepath;                   // The path of the log file we're viewing
		private FileStream m_file;                   // A file stream of 'm_filepath'
		private long m_end;                          // The current reference point in the file
		
		public Main()
		{
			InitializeComponent();
			
			m_settings = new Settings();
			m_recent = new RecentFiles(m_menu_file_recent, OpenLogFile);
			m_recent.Import(m_settings.RecentFiles);
			m_watch = new FileSystemWatcher{NotifyFilter = NotifyFilters.LastWrite|NotifyFilters.Size};
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
			m_menu_tools_highlights.Click  += (s,a) => {};
			m_menu_tools_filters.Click     += (s,a) => {};
			m_menu_tools_options.Click     += (s,a) => ShowOptions();
			m_menu_help_about.Click        += (s,a) => {};
			
			// Toolbar
			m_btn_open_log.Click        += (s,a) => OpenLogFile(null);
			m_check_tail.CheckedChanged += (s,a) => {};
			m_btn_highlights.Click      += (s,a) => {};
			m_btn_filter.Click          += (s,a) => {};
			
			// Setup the grid
			m_grid.AutoGenerateColumns = false;
			m_grid.CellValueNeeded += CellValueNeeded;
			m_grid.RowCount = 0;
			
			ApplySettings();
			UpdateStatus();
			
			// Watcher
			m_watch.Changed += (s,a) => UpdateLineIndex();
			
			// Shutdown
			FormClosing += (s,a) =>
				{
					m_settings.RecentFiles = m_recent.Export();
					m_settings.Save();
				};
		}
		
		/// <summary>Apply settings throughout the app</summary>
		private void ApplySettings()
		{
			// Row styles
			m_grid.RowsDefaultCellStyle.Font = m_settings.Font;
			m_grid.RowsDefaultCellStyle.ForeColor = m_settings.LineForeColour1;
			m_grid.RowsDefaultCellStyle.BackColor = m_settings.LineBackColour1;
			m_grid.RowsDefaultCellStyle.SelectionBackColor = m_settings.LineSelectBackColour;
			m_grid.RowsDefaultCellStyle.SelectionForeColor = m_settings.LineSelectForeColour;
			if (m_settings.AlternateLineColours)
			{
				m_grid.AlternatingRowsDefaultCellStyle.Font = m_settings.Font;
				m_grid.AlternatingRowsDefaultCellStyle.BackColor = m_settings.LineBackColour2;
				m_grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = m_settings.LineSelectBackColour;
				m_grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = m_settings.LineSelectForeColour;
			}
			m_grid.DefaultCellStyle.SelectionBackColor = m_settings.LineSelectBackColour;
			m_grid.DefaultCellStyle.SelectionForeColor = m_settings.LineSelectForeColour;
			
			// Tail
			m_watch.EnableRaisingEvents = m_settings.TailEnabled;
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
			// Since this dialog is modal, we can use the members of the main thread
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
							if (read != count)
								throw new IOException("failed to read file over range ["+(pos-count)+","+(pos+count)+"). Read "+read+"/"+count+" bytes.");
				
							// Search backwards counting lines
							for (;read-- != 0 && line_index.Count != max_line_count; --pos)
							{
								if (buf[read] != row_delimiter) continue;
								if (pos != end) line_index.Add(pos); // special case for last character == row delimiter
							}
							if (pos == 0) line_index.Add(0);
						}
					}
					
					// Reverse the line index list so that the last line is at the end
					line_index.Reverse();
					
					a.Cancel = bgw.CancellationPending;
				});
			
			// Don't use the results if the task was cancelled
			if (task.ShowDialog(this) != DialogResult.OK)
			{
				CloseLogFile();
				return;
			}
			
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
					List<long> line_index = new List<long>();
					using (var file = LoadFile(filepath))
					{
						// Seek to the start of the last known line
						file.Seek(pos, SeekOrigin.Begin);
						
						// A temporary buffer for reading sections of the file
						byte[] buf = new byte[CacheSize]; 
						
						// Scan forward reading new lines
						for (;file.Position != end;)
						{
							// Buffer file data
							int read = file.Read(buf, 0, buf.Length);
							
							// Search for new lines
							for (int i = 0; i != read; ++i)
							{
								if (buf[i] != row_delim) continue;
								if (pos != end) line_index.Add(pos); // special case for last character == row delimiter
							}
							if (pos == 0) line_index.Add(0);
						}
					}
					
					// Marshal the results back to the main thread
					Action AddToLineIndex = ()=>
					{
						m_line_index.AddRange(line_index);
						m_end = end;
						UpdateUI();
					};
					BeginInvoke(AddToLineIndex);
				});
		}
		
		/// <summary>Supply the grid with values</summary>
		private void CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			// Read the line into memory and split it into columns
			m_line_cache.Cache(m_file, m_line_index, e.RowIndex, m_settings.ColDelimiter);
			e.Value = m_line_cache[e.ColumnIndex];
		}
		
		/// <summary>Display the options dialog</summary>
		private void ShowOptions()
		{
			m_settings.Save();
			var settings = new SettingsUI();
			if (settings.ShowDialog(this) != DialogResult.OK) return;
			m_settings.Reload();
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
					// Reset to zero first, this is more efficient so some reason
					m_grid.RowCount = 0;
					m_grid.RowCount = m_line_index.Count;
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
				for (int i = 1; i < sb.Length; ++i) if ((i%3) == 0) sb.Insert(sb.Length - i, ',');
				m_lbl_file_size.Text = string.Format(Resources.SizeXBytes, sb);
			}
		}
	}
}
