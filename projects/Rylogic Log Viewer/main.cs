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
		
		private const string AppTitle = "Rylogic Log Viewer";
		private const int AvrBytesPerLine = 256;
		private const int CacheSize = 4096;
		
		private readonly Settings m_settings;        // App settings
		private readonly RecentFiles m_recent;       // Recent files
		private readonly FileSystemWatcher m_watch;  // A FS watcher to detect file changes
		private readonly List<long> m_line_index;    // Byte offsets (from file begin) to the start of lines
		private readonly LineCache m_line_cache;     // Optimisation for reading whole lines on multi-column grids
		private readonly byte[] m_buf;               // A temporary buffer for reading sections of the file
		private FileStream m_file;                   // The file we're reading from (open with shared read/write)
		private string m_filepath;                   // The path of 'm_file'
		private long m_end;                          // The current reference point in the file
		
		public Main()
		{
			InitializeComponent();
			
			m_settings = new Settings();
			m_recent = new RecentFiles(m_menu_file_recent, OpenLogFile);
			m_recent.Import(m_settings.RecentFiles);
			m_watch = new FileSystemWatcher{NotifyFilter = NotifyFilters.LastWrite|NotifyFilters.Size};
			m_line_index = new List<long>();
			m_line_cache = new LineCache();
			m_buf = new byte[CacheSize];
			m_end = 0;
			
			// Menu
			m_menu_file_open.Click += (s,a) => OpenLogFile(null);
			m_menu_file_exit.Click += (s,a) => Close();
			
			// Toolbar
			m_btn_open_log.Click += (s,a) => OpenLogFile(null);
			
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
			m_grid.RowsDefaultCellStyle.BackColor = m_settings.LineColour1;
			m_grid.RowsDefaultCellStyle.SelectionBackColor = m_settings.LineSelectBackColour;
			m_grid.RowsDefaultCellStyle.SelectionForeColor = m_settings.LineSelectForeColour;
			if (m_settings.AlternateLineColours)
			{
				m_grid.AlternatingRowsDefaultCellStyle.Font = m_settings.Font;
				m_grid.AlternatingRowsDefaultCellStyle.BackColor = m_settings.LineColour2;
				m_grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = m_settings.LineSelectBackColour;
				m_grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = m_settings.LineSelectForeColour;
			}
			m_grid.DefaultCellStyle.SelectionBackColor = m_settings.LineSelectBackColour;
			m_grid.DefaultCellStyle.SelectionForeColor = m_settings.LineSelectForeColour;
		}
		
		/// <summary>Prompt to open a log file</summary>
		private void OpenLogFile(string filepath)
		{
			// Prompt for a file if none provided
			if (filepath == null)
			{
				var fd = new OpenFileDialog{Filter = "Text Files (*.txt;*.log;*.csv)|*.txt;*.log;*.csv|All files (*.*)|*.*", Multiselect = false};
				if (fd.ShowDialog() != DialogResult.OK) return;
				filepath = fd.FileName;
			}
			
			// Reject invalid filepaths
			if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
				return;
		
			m_recent.Add(filepath);
			m_settings.RecentFiles = m_recent.Export();
			m_settings.Save();
			
			// Setup the watcher to watch for file changes
			m_watch.Path = Path.GetDirectoryName(filepath);
			m_watch.Filter = Path.GetFileName(filepath);
			
			// Switch files
			FileStream file = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, m_settings.LineCount * AvrBytesPerLine);
			if (m_file != null) m_file.Close();
			m_filepath = filepath;
			m_file = file;
			m_end = file.Length;
			Text = AppTitle + " - " + m_filepath;
			
			BuildLineIndex();
		}
		
		/// <summary>Build a line index of the file</summary>
		private void BuildLineIndex()
		{
			// Do this in a background thread, it case it takes ages
			ProgressForm task = new ProgressForm("Building line index", "Reading the last "+m_settings.LineCount+" lines from "+m_filepath, (s,a)=>
				{
					BackgroundWorker bgw = (BackgroundWorker)s;
					
					m_line_index.Clear();
			
					// Read the last 'LineCount' line indices into memory
					// Search backward through the file counting lines to get to the byte indices
					m_file.Seek(m_end, SeekOrigin.Begin);
					for (long pos = m_end; pos != 0 && m_line_index.Count != m_settings.LineCount && !bgw.CancellationPending;)
					{
						int pc = 100 * m_line_index.Count / m_settings.LineCount;
						bgw.ReportProgress(pc);
						
						// The number of bytes to buffer
						int count = (int)Math.Min(pos, m_buf.Length);
			
						// Buffer file data
						m_file.Seek(pos - count, SeekOrigin.Begin);
						int read = m_file.Read(m_buf, 0, count);
						if (read != count)
							throw new IOException("failed to read file over range ["+(pos-count)+","+(pos+count)+"). Read "+read+"/"+count+" bytes.");
				
						// Search backwards counting lines
						for (;read-- != 0 && m_line_index.Count != m_settings.LineCount; --pos)
						{
							if (m_buf[read] != m_settings.RowDelimiter) continue;
							if (pos != m_end) m_line_index.Add(pos); // special case for last character == row delimiter
						}
						if (pos == 0)
							m_line_index.Add(0);
					}
			
					// Reverse the line index list so that the last line is at the end
					m_line_index.Reverse();
			
					for (int i = 0; i != m_line_index.Count; ++i)
						m_line_cache.Cache(m_file, m_line_index, i, m_settings.ColDelimiter);
					
					a.Cancel = bgw.CancellationPending;
				});
			
			task.ShowDialog(this);

			// Configure the grid
			m_line_cache.Cache(m_file, m_line_index, 0, m_settings.ColDelimiter);
			m_grid.ColumnHeadersVisible = m_line_cache.Count > 1;
			m_grid.ColumnCount = m_line_cache.Count;
			m_grid.RowCount = 0;
			m_grid.RowCount = m_line_index.Count;
		}
		
		/// <summary>Extend the line index past 'm_end'</summary>
		private void UpdateLineIndex()
		{
			// Get the range of bytes to read from the file
			long start = m_line_index.Count != 0 ? m_line_index[m_line_index.Count-1] : 0;
			long end = m_file.Length; // Read the new end of the file
			
			// If the file has shrunk, rebuild the index
			if (end < m_end)
			{
				BuildLineIndex();
				return;
			}
			
			var file = m_file.Cl(
			// Find the new line indices in a background thread
			ThreadPool.QueueUserWorkItem(a=>
				{
					var
			// Seek to the start of the last known line
			m_file.Seek(start, SeekOrigin.Begin);
			
			// Scan forward reading new lines
			List<long> new_lines = new List<long>();
			for (;m_file.Position != m_file.Length;)
			{
				// Buffer file data
				int read = m_file.Read(m_buf, 0, m_buf.Length);
				
				// Search for new lines
				for (int i = 0; i != read; ++i)
				{
					if (m_buf[i] != m_settings.RowDelimiter) continue;
					if (pos != m_end) m_line_index.Add(pos); // special case for last character == row delimiter
						}
						if (pos == 0)
							m_line_index.Add(0);
			}

			UpdateStatus();
		}
		
		/// <summary>Supply the grid with values</summary>
		private void CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			// Read the line into memory and split it into columns
			m_line_cache.Cache(m_file, m_line_index, e.RowIndex, m_settings.ColDelimiter);
			e.Value = m_line_cache[e.ColumnIndex];
		}
		
		/// <summary>Update the status bar</summary>
		private void UpdateStatus()
		{
			if (m_file == null)
				m_lbl_file_size.Text = "No File";
			else
			{
				StringBuilder sb = new StringBuilder(m_file.Length.ToString());
				for (int i = 1; i < sb.Length; ++i) if ((i%3) == 0) sb.Insert(sb.Length - i, ',');
				m_lbl_file_size.Text = string.Format("Size: {0} bytes", sb.ToString());
			}
		}
	}
}
