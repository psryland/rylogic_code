using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
		private class LineCache
		{
			private const int LineCacheSize = 512;
			private readonly List<string> m_col = new List<string>();
			private byte[] m_buf   = new byte[LineCacheSize];
			private int    m_index = -1;
			
			// Column accessors
			public int Count            { get { return m_col.Count; } }
			public string this[int col] { get { return m_col[col]; } }
			public override string ToString() { return Count != 0 ? this[0] : ""; }
			
			/// <summary>Read a line from 'file' and divide it into columns</summary>
			public void Cache(FileStream file, Encoding encoding, List<Range> line_index, int index, byte column_delimiter)
			{
				if (m_index == index) return; // Already cached
				m_index = index;
				
				// Determine the length of the line
				Range rng = line_index[index];
				
				// Read the whole line into m_buf
				file.Seek(rng.m_begin, SeekOrigin.Begin);
				m_buf = rng.Count <= m_buf.Length ? m_buf : new byte[rng.Count];
				int read = file.Read(m_buf, 0, (int)rng.Count);
				if (read != rng.Count)
					throw new IOException("failed to read file over range ["+rng.m_begin+","+rng.m_end+"). Read "+read+"/"+rng.Count+" bytes.");
				
				// Split the line up into columns and convert to strings
				m_col.Clear();
				int b = 0, e = 0;
				for (; e != rng.Count; ++e)
				{
					if (m_buf[e] != column_delimiter) continue;
					m_col.Add(encoding.GetString(m_buf, b, e));
					b = e + 1;
				}
				if (b != e)
					m_col.Add(encoding.GetString(m_buf, b, e));
			}
		}

		private const string LogFileFilter = @"Text Files (*.txt;*.log;*.csv)|*.txt;*.log;*.csv|All files (*.*)|*.*";
		private const int AvrBytesPerLine = 256;
		private const int CacheSize = 4096;
		
		private readonly Settings m_settings;          // App settings
		private readonly RecentFiles m_recent;         // Recent files
		private readonly FileSystemWatcher m_watch;    // A FS watcher to detect file changes
		private readonly EventBatcher m_file_changed;  // Event batcher for m_watch events
		private readonly List<Highlight> m_highlights; // A list of the active highlights only
		private readonly LineCache m_line_cache;       // Optimisation for reading whole lines on multi-column grids
		private Encoding m_encoding;                   // The file encoding
		private List<Range> m_line_index;              // Byte offsets (from file begin) to the byte range of a line
		private string m_filepath;                     // The path of the log file we're viewing
		private FileStream m_file;                     // A file stream of 'm_filepath'
		private byte[] m_row_delim;                    // The row delimiter to use
		private long m_last_line;                      // The byte offset (from file begin) to the start of the last known line
		private long m_file_end;                       // The last known size of the file
		
		//todo:
		// unicode text files
		// find
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
			m_settings = new Settings();
			m_recent = new RecentFiles(m_menu_file_recent, OpenLogFile);
			m_recent.Import(m_settings.RecentFiles);
			m_watch        = new FileSystemWatcher{NotifyFilter = NotifyFilters.LastWrite|NotifyFilters.Size};
			m_file_changed = new EventBatcher(10, this);
			m_highlights   = new List<Highlight>();
			m_line_cache   = new LineCache();
			m_encoding     = GetEncoding(m_settings.Encoding);
			m_line_index   = null;
			m_filepath     = null;
			m_file         = null;
			m_last_line    = 0;
			m_file_end     = 0;
			
			// Menu
			m_menu.Move                             += (s,a) => { m_settings.MenuPosition = m_menu.Location; m_settings.Save(); };
			m_menu_file_open.Click                  += (s,a) => OpenLogFile(null);
			m_menu_file_close.Click                 += (s,a) => CloseLogFile();
			m_menu_file_exit.Click                  += (s,a) => Close();
			m_menu_edit_selectall.Click             += (s,a) => DataGridView_Extensions.SelectAll(m_grid, new KeyEventArgs(Keys.Control|Keys.A));
			m_menu_edit_copy.Click                  += (s,a) => DataGridView_Extensions.Copy(m_grid, new KeyEventArgs(Keys.Control|Keys.C));
			m_menu_edit_find.Click                  += (s,a) => {};//Find();
			m_menu_edit_find_next.Click             += (s,a) => {};
			m_menu_edit_find_prev.Click             += (s,a) => {};
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
			
			// Toolbar
			m_toolstrip.Move            += (s,a) => { m_settings.ToolsPosition = m_toolstrip.Location; m_settings.Save(); };
			m_btn_open_log.Click        += (s,a) => OpenLogFile(null);
			m_btn_refresh.Click         += (s,a) => Reload();
			m_check_tail.CheckedChanged += (s,a) => EnableTail(m_check_tail.Checked);
			m_btn_highlights.Click      += (s,a) => ShowOptions(SettingsUI.ETab.Highlights);
			m_btn_filters.Click         += (s,a) => ShowOptions(SettingsUI.ETab.Filters);
			
			// Status
			m_status.Move += (s,a) => { m_settings.StatusPosition = m_status.Location; m_settings.Save(); };
			
			// Setup the grid
			m_grid.RowCount            = 0;
			m_grid.AutoGenerateColumns = false;
			m_grid.KeyDown            += DataGridView_Extensions.SelectAll;
			m_grid.KeyDown            += DataGridView_Extensions.Copy;
			m_grid.CellValueNeeded    += CellValueNeeded;
			m_grid.CellPainting       += CellPainting;
			m_grid.SelectionChanged   += (s,a) => { UpdateStatus(); };
			m_grid.DataError          += (s,a) => { Debug.Assert(false); };
			
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
			
			// Shutdown
			FormClosing += (s,a) =>
				{
					m_settings.ScreenPosition = Location;
					m_settings.WindowSize = Size;
					m_settings.RecentFiles = m_recent.Export();
					m_settings.Save();
				};
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

		/// <summary>Set the encoding to use with loaded files</summary>
		private void SetEncoding(Encoding encoding)
		{
			m_encoding = encoding;
			m_settings.Encoding = m_encoding.EncodingName;
			m_settings.Save();
			Reload();
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
		
		/// <summary>Apply settings throughout the app</summary>
		private void ApplySettings()
		{
			// Line endings
			m_row_delim = m_encoding.GetBytes(m_settings.RowDelimiter.Replace("CR","\r").Replace("LF","\n"));
			
			// Tail
			m_watch.EnableRaisingEvents = m_file != null && m_settings.TailEnabled;
			
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

		/// <summary>Allow dropping of files</summary>
		protected override void OnDragEnter(DragEventArgs args)
		{
			FileDrop(args, true);
		}
		
		/// <summary>Do file drop</summary>
		protected override void OnDragDrop(DragEventArgs args)
		{
			FileDrop(args, false);
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

		/// <summary>Test 'text' against 'filters' and returns true if it should be included.
		/// Updates 'line' in the event that the filters include non-binary matches</summary>
		private static bool PassesFilters(List<Filter> filters, string text, out Range line)
		{
			// Test 'text' against each filter to see if it's included
			bool include = true;
			line = new Range(0, text.Length);
			foreach (var f in filters)
			{
				// First see if it passes this filter
				if (f.Exclude) include &= !f.IsMatch(text);
				else           include &=  f.IsMatch(text);
				if (!include) break;
				
				// If the filter is not binary, trim 'line' to the bounding range of matches
				if (!f.BinaryMatch)
				{
					Range bound = new Range(line.m_end, line.m_begin);
					foreach (var m in f.Match(text)) bound.Encompase(m);
					if (bound.Count >= 0)
					{
						line.m_begin = Math.Max(line.m_begin ,bound.m_begin);
						line.m_end   = Math.Min(line.m_end   ,bound.m_end  );
					}
				}
			}
			return include;
		}
		
		/// <summary>Reload the current file</summary>
		private void Reload()
		{
			if (m_file != null) BuildLineIndex(m_filepath);
			UpdateUI();
		}
		
		/// <summary>
		/// Buffer a maximum of 'count' bytes from 'stream' into 'buf'.
		/// If 'backward' is true the stream is seeked backward from the current position
		/// before reading and then seeked backward again after reading so that conceptually
		/// the file position moves in the direction of the read.
		/// Returns the number of bytes buffered in 'buf'</summary>
		private static int Buffer(Stream file, byte[] buf, int count, bool backward, Encoding encoding)
		{
			// The number of bytes to buffer
			count = Math.Min(count, buf.Length);
			if (count == 0) return 0;
			
			// Set the file position
			if (backward) file.Seek(-count, SeekOrigin.Current);
			long pos = file.Position;
			
			// Move to the start of a character
			if (encoding.Equals(Encoding.ASCII)) {}
			else if (encoding.Equals(Encoding.Unicode) || encoding.Equals(Encoding.BigEndianUnicode))
			{
				// Ensure a 16-bit word boundary
				if ((pos % 2) == 1)
					file.ReadByte();
			}
			else if (encoding.Equals(Encoding.UTF8))
			{
				// Some programs store a byte order mask (BOM) at the start of UTF-8 text files.
				// These bytes are 0xEF, 0xBB, 0xBF, so ignore them if present
				if (pos == 0 && file.ReadByte() == 0xEF && file.ReadByte() == 0xBB && file.ReadByte() == 0xBF) {}
				else file.Seek(pos, SeekOrigin.Begin);
				
				// UTF-8 encoding:
				//Bits Last code point  Byte 1    Byte 2    Byte 3    Byte 4    Byte 5   Byte 6
				//  7     U+007F        0xxxxxxx
				// 11     U+07FF        110xxxxx  10xxxxxx
				// 16     U+FFFF        1110xxxx  10xxxxxx  10xxxxxx
				// 21     U+1FFFFF      11110xxx  10xxxxxx  10xxxxxx  10xxxxxx
				// 26     U+3FFFFFF     111110xx  10xxxxxx  10xxxxxx  10xxxxxx  10xxxxxx
				// 31     U+7FFFFFFF    1111110x  10xxxxxx  10xxxxxx  10xxxxxx  10xxxxxx  10xxxxxx
				const int char_byte  = 0xbf; // 10111111
				for (;;)
				{
					int b = file.ReadByte();
					if (b == -1) return 0;              // End of file
					if ((b & char_byte) == b) continue; // If 'b' is a byte halfway through an encoded char, keep reading
					
					// Otherwise it's an ascii char or the start of a multibyte char
					// save the byte we read, and read the remander of 'count'
					file.Seek(-1, SeekOrigin.Current);
					break;
				}
			}
			
			// Buffer file data
			count -= (int)(file.Position - pos);
			int read = file.Read(buf, 0, count);
			if (read != count) throw new IOException("failed to read file over range ["+pos+","+(pos+count)+"). Read "+read+"/"+count+" bytes.");
			if (backward) file.Seek(-read, SeekOrigin.Current);
			return read;
		}
		
		/// <summary>Add a line to 'line_index'</summary>
		private static void AddLine(List<Range> line_index, byte[] buf, Range rng, long base_addr, long max_addr, Encoding encoding, List<Filter> filters)
		{
			if (rng.Empty) return;
			Range r; string text = encoding.GetString(buf, (int)rng.m_begin, (int)rng.Count);
			if (PassesFilters(filters, text, out r))
			{
				// Covert the text range to a byte range
				r.m_begin = base_addr + rng.m_begin + encoding.GetByteCount(text.Substring(0, (int)r.m_begin)); 
				r.m_end   = base_addr + rng.m_begin + encoding.GetByteCount(text.Substring(0, (int)r.m_end  ));
				Debug.Assert(r.m_begin <= r.m_end);
				Debug.Assert(r.m_begin >= 0 && r.m_end <= max_addr);
				line_index.Add(r);
			}
		}
		
		/// <summary>Build a line index of the file</summary>
		private void BuildLineIndex(string filepath)
		{
			// Local copies of variables used by the follow async task
			List<Range> line_index = new List<Range>();
			List<Filter> filters   = ActiveFilters.ToList();
			Encoding encoding      = (Encoding)m_encoding.Clone();
			byte[] row_delim       = (byte[])m_row_delim.Clone();
			int max_line_count     = m_settings.LineCount;
			long last_line         = 0;
			long file_end          = 0;
			
			// Do this in a background thread, in case it takes ages
			ProgressForm task = new ProgressForm(Resources.BuildingLineIndex, string.Format(Resources.ReadingXLineFromY, m_settings.LineCount, filepath), (s,a)=>
				{
					BackgroundWorker bgw = (BackgroundWorker)s;
					
					// A temporary buffer for reading sections of the file
					byte[] buf = new byte[CacheSize]; 
					
					// Search backward through the file counting lines to get to the byte offsets
					using (var file = LoadFile(filepath))
					{
						// Save the file length in 'file_end' and then use this value rather than
						// 'file.Length' just in case the file grows while we're using it.
						file_end = file.Length;
						file.Seek(file_end, SeekOrigin.Begin); // see above for why not Seek(0,SeekOrigin.End)
						for (;line_index.Count != max_line_count && !bgw.CancellationPending;)
						{
							int pc = 100 * line_index.Count / max_line_count;
							bgw.ReportProgress(pc);
							
							// Buffer data from the file
							int read = Buffer(file, buf, (int)file.Position, true, encoding);
							if (read == 0) break;

							// Search backwards counting lines
							long base_addr = file.Position;
							Range Brng = new Range(read, read); // The range within 'buf' of the line text
							for (;Brng.m_begin-- != 0 && line_index.Count != max_line_count;)
							{
								// Quick test using the first byte of the row delimiter
								if (buf[Brng.m_begin] != row_delim[0]) continue;
								
								// Test the remaining bytes of the row delimiter
								bool new_row = (Brng.m_begin + row_delim.Length) <= read;
								for (int i = 1; new_row && i != row_delim.Length; ++i)
									new_row &= buf[Brng.m_begin+i] == row_delim[i];
								if (!new_row) continue;

								// 'Brng.m_begin' points to the start of a row delimiter,
								// Shift it to point to the start of the line.
								// Add the line not including the row delimiter
								Brng.m_begin += row_delim.Length;
								AddLine(line_index, buf, Brng, base_addr, file_end, encoding, filters);
								if (last_line == 0) last_line = base_addr + Brng.m_begin; // save the offset to the start of the last known line
								Brng.m_end   = Brng.m_begin - row_delim.Length;
								Brng.m_begin = Brng.m_end;
							}
							if (line_index.Count != max_line_count)
							{
								++Brng.m_begin;
								AddLine(line_index, buf, Brng, base_addr, file_end, encoding, filters);
							}
						}
						Debug.Assert(line_index.Count <= max_line_count);
					}
					
					// Reverse the line index list so that the last line is at the end
					line_index.Reverse();
					a.Cancel = bgw.CancellationPending;
				});
			
			// Don't use the results if the task was cancelled
			DialogResult res;
			try { res = task.ShowDialog(this); }
			catch (Exception ex) { res = MessageBox.Show(this, string.Format(Resources.BuildLineIndexErrorMsg, ex.Message), Resources.ReadingFileFailed, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error); }
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
			m_filepath   = filepath;
			m_file       = LoadFile(m_filepath);
			m_last_line  = last_line;
			m_file_end   = file_end;
			UpdateUI();
		}
		
		/// <summary>Incrementally update the line index</summary>
		private void UpdateLineIndex()
		{
			if (m_file == null)
				return;
			
			// Get the range of bytes to read from the file. This range starts from the beginning
			// of the last known (unfiltered) line to the new file end. The last line is read again
			// because we may not have read the complete line last time.
			long file_end = m_file.Length;       // Read the new end of the file
			if (file_end == m_file_end) return;  // Same as last time? don't bother
			if (file_end <  m_file_end) { Reload(); return; } // If the file has shrunk, rebuild the index
			
			// Local copies of variables used by the follow async task
			List<Range> line_index = new List<Range>();
			List<Filter> filters   = ActiveFilters.ToList();
			Encoding encoding      = (Encoding)m_encoding.Clone();
			byte[] row_delim       = (byte[])m_row_delim.Clone();
			int max_line_count     = m_settings.LineCount;
			long last_line         = m_last_line;
			string filepath        = m_filepath;
			Debug.Assert(last_line  <= file_end, "'file_end' should always be greater than 'last_line'");
			
			// Find the new line indices in a background thread
			ThreadPool.QueueUserWorkItem(a=>
				{
					try
					{
						// A temporary buffer for reading sections of the file
						byte[] buf = new byte[CacheSize]; 
						
						// Read lines starting from the last known line
						using (var file = LoadFile(filepath))
						{
							// Seek to the start of the last known line
							file.Seek(last_line, SeekOrigin.Begin);
							
							// Scan forward reading new lines
							for (;line_index.Count != max_line_count;)
							{
								// Buffer data from the file
								int read = Buffer(file, buf, (int)(file_end - file.Position), false, encoding);
								if (read == 0) break;
								
								// Search forward for new lines
								long base_addr = file.Position - read;
								Range Brng = new Range(0,0); // The range within 'buf' of the line text
								for (;Brng.m_end != read && line_index.Count != max_line_count; ++Brng.m_end)
								{
									// Quick test using the first byte of the row delimiter
									if (buf[Brng.m_end] != row_delim[0]) continue;
									
									// Test the remaining bytes of the row delimiter
									bool new_row = (Brng.m_end + row_delim.Length) <= read;
									for (int i = 1; new_row && i != row_delim.Length; ++i)
										new_row &= buf[Brng.m_end+i] == row_delim[i];
									if (!new_row) continue;

									// 'Brng.m_end' points to the start of a row delimiter
									// Add the line not including row delimiter
									AddLine(line_index, buf, Brng, base_addr, file_end, encoding, filters);
									last_line = base_addr + Brng.m_begin; // save the offset to the start of the last known line
									Brng.m_begin = Brng.m_end + row_delim.Length;
									Brng.m_end   = Brng.m_begin - 1;
								}
								if (line_index.Count != max_line_count)
									AddLine(line_index, buf, Brng, base_addr, file_end, encoding, filters);
							}
						}

						Debug.Assert(line_index.Count <= max_line_count);
					
						// If we've detected 'max_line_count' new lines to add and still not reached
						// the end of the file throw it all away and treat like a file open
						if (line_index.Count == max_line_count)
						{
							Action<string> rebuild = BuildLineIndex;
							BeginInvoke(rebuild, filepath);
						}
						// Otherwise, marshal the results back to the main thread
						// so they can be added synchronously to m_line_index
						else
						{
							Action AddToLineIndex = ()=>
							{
								// This is run in the main thread
								if (line_index.Count != 0)
								{
									int total = m_line_index.Count + line_index.Count;
									if (total > m_settings.LineCount) m_line_index.RemoveRange(0, total - m_settings.LineCount);
									while (m_line_index.Count != 0 && m_line_index[m_line_index.Count - 1].m_begin >= m_last_line) m_line_index.RemoveAt(m_line_index.Count - 1);
									m_line_index.AddRange(line_index);
									m_last_line = last_line;
									m_file_end  = file_end;
									Debug.Assert(m_last_line <= m_file_end, "'m_file_end' should always be greater than 'm_last_line'");
								}
							
								// Run another update if the file changed while we were doing this
								if (m_file.Length != m_file_end) m_file_changed.Signal();
								UpdateUI();
							};
							BeginInvoke(AddToLineIndex);
						}
					} catch (Exception ex) { Debug.WriteLine("Exception aborted UpdateLineIndex() call: {0}", ex.Message); }
				});
		}
		
		/// <summary>Supply the grid with values</summary>
		private void CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			if (m_file == null) e.Value = null;
			else
			{
				try
				{
					// Read the line into memory and split it into columns
					m_line_cache.Cache(m_file, m_encoding, m_line_index, e.RowIndex, m_settings.ColDelimiter);
					e.Value = (e.ColumnIndex >= 0 && e.ColumnIndex < m_line_cache.Count) ? m_line_cache[e.ColumnIndex] : "";
				}
				catch { e.Value = ""; }
			}
		}
		
		/// <summary>Draw the cell appropriate to any highlighting</summary>
		private void CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			string value = (string)e.Value;
			
			// Check if the cell value matches any of the highlight patterns
			Highlight hl = m_highlights.FirstOrDefault(h => h.IsMatch(value));
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

		/// <summary>Display the options dialog</summary>
		private void ShowOptions(SettingsUI.ETab tab)
		{
			// Save the current filter patterns so we can detected changes to the
			// filters and reload the line index if they change.
			string filters = m_file != null ? m_settings.FilterPatterns : null;
			
			// Save current settings so the settingsUI starts with the most up to date
			// Show the settings dialog, then reload the settings
			m_settings.Save();
			new SettingsUI(tab).ShowDialog(this);
			m_settings.Reload();

			// If the filter patterns have changed, reload the file
			if (m_file != null && m_settings.FilterPatterns != filters)
				Reload();
			else
			{
				ApplySettings();
				Refresh();
			}
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

		/// <summary>Return a collection of the currently active filters</summary>
		private IEnumerable<Filter> ActiveFilters
		{
			get { return from ft in Filter.Import(m_settings.FilterPatterns) where ft.Active select ft; }
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
			if (m_file != null && m_line_index.Count != 0)
			{
				// Read a row from the data and show column headers if there is more than one
				m_line_cache.Cache(m_file, m_encoding, m_line_index, 0, m_settings.ColDelimiter);
				m_grid.ColumnHeadersVisible = m_line_cache.Count > 1;
				m_grid.ColumnCount = m_line_cache.Count;
				
				bool auto_scroll = AutoScrollTail;
				if (m_grid.RowCount != m_line_index.Count)
				{
					int selected = SelectedRow; // Preserve the selection across row count change
					m_grid.RowCount = 0; // Reset to zero first, this is more efficient for some reason
					m_grid.RowCount = m_line_index.Count;
					SelectedRow = auto_scroll ? m_grid.RowCount - 1 : selected;
				}
				if (auto_scroll) ShowLastRow();
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
			if (m_file == null)
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
