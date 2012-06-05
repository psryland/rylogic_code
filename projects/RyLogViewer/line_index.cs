using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using pr.common;
using pr.gui;
using RyLogViewer.Properties;

namespace RyLogViewer
{
	public partial class Main
	{
		/// <summary>Returns a file stream for 'filepath' openned with R/W sharing</summary>
		private FileStream LoadFile(string filepath)
		{
			return new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, m_settings.LineCount * AvrBytesPerLine);
		}
		
		/// <summary>Reload the current file</summary>
		private void Reload()
		{
			if (FileOpen) BuildLineIndex(m_filepath);
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
				// Skip the byte order mask (BOM)
				if (pos == 0 && file.ReadByte() != -1 && file.ReadByte() != -1) {}
				
				// Ensure a 16-bit word boundary
				if ((file.Position % 2) == 1)
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
			catch (Exception ex)
			{
				if (ex is TargetInvocationException && ex.InnerException != null) ex = ex.InnerException;
				res = MessageBox.Show(this, string.Format(Resources.BuildLineIndexErrorMsg, ex.Message), Resources.ReadingFileFailed, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
			}
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
			if (!FileOpen)
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
							// This is run in the main thread
							Action AddToLineIndex = ()=>
							{
								AppendLineIndex(line_index, last_line, file_end);
								if (m_file.Length != m_file_end) m_file_changed.Signal(); // Run another update if the file changed while we were doing this
							};
							BeginInvoke(AddToLineIndex);
						}
					} catch (Exception ex) { Debug.WriteLine("Exception aborted UpdateLineIndex() call: {0}", ex.Message); }
				});
		}
		
		/// <summary>Append the contents of 'line_index' to the main list</summary>
		private void AppendLineIndex(List<Range> line_index, long last_line, long file_end)
		{
			if (line_index.Count == 0)
				return;
			
			// Cap the number of lines
			int total = m_line_index.Count + line_index.Count;
			if (total > m_settings.LineCount)
				m_line_index.RemoveRange(0, total - m_settings.LineCount);
			
			// Remove any lines that start on or after the old last known line
			for (int i = m_line_index.Count; i-- != 0;)
			{
				if (m_line_index[i].m_end < m_last_line) break;
				m_line_index.RemoveAt(i);
			}
			
			// Add the contents of 'line_index' to the main list
			m_line_index.AddRange(line_index);
			m_last_line = last_line;
			m_file_end  = file_end;
			Debug.Assert(m_last_line <= m_file_end, "'m_file_end' should always be greater than 'm_last_line'");
			UpdateUI();
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
				if (r.m_begin >= 0 && r.m_end <= max_addr)
					line_index.Add(r);
				else
					throw new DataException("Invalid text data. Check that the correct encoding is used");
			}
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
				include &= f.IsMatch(text);
				//if (f.Exclude) include &= !f.IsMatch(text);
				//else           include &=  f.IsMatch(text);
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
		
		/// <summary>Return a collection of the currently active filters</summary>
		private IEnumerable<Filter> ActiveFilters
		{
			get { return from ft in Filter.Import(m_settings.FilterPatterns) where ft.Active select ft; }
		}
	}
}
