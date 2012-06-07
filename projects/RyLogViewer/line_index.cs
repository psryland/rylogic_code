using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using pr.common;

namespace RyLogViewer
{
	public partial class Main
	{
		/// <summary>Returns a file stream for 'filepath' openned with R/W sharing</summary>
		private static FileStream LoadFile(string filepath, int buffer_size = 4096)
		{
			return new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, buffer_size);
		}
		
		/// <summary>Reload the current file</summary>
		private void Reload()
		{
			if (FileOpen) BuildLineIndex(m_filepath, m_filepos, true, UpdateUI);
			UpdateUI();
		}
		
		/// <summary>Cause an incremental update to the line index</summary>
		private void UpdateLineIndex()
		{
			BuildLineIndex(m_filepath, m_filepos, false, ()=>
				{
					// On completion, check if the file has changed again and rerun if it has
					if (m_fileend != m_file.Length)
						m_file_changed.Signal();
				});
		}

		/// <summary>Generates the line index centred around 'filepos'.
		/// If 'filepos' is within the byte range of the current line_index then an incremental
		/// search for lines is done in the direction needed to recentre the line list around 'filepos'</summary>
		private void BuildLineIndex(string filepath, long filepos, bool reload, Action on_complete)
		{
			// Stop any existing build that might be in progress
			if (!m_sync_build_ended.WaitOne(0))
			{
				m_sync_cancel_building.Set();
				m_sync_build_ended.WaitOne(Timeout.Infinite);
			}
			
			// Find the byte range of the file currently loaded
			Range file_range = m_line_index.Count == 0
				? new Range(m_line_index.First().m_begin, m_line_index.Last().m_begin)
				: Range.Zero;
			
			// If this is not a 'reload', guess the encoding
			Encoding encoding = reload
				? (Encoding)GuessEncoding(filepath).Clone()
				: (Encoding)m_encoding.Clone();
			
			// If this is not a 'reload', guess the row_delimiters
			byte[] row_delim = reload
				? (byte[])GuessRowDelimiter(filepath, encoding).Clone()
				: (byte[])m_row_delim.Clone();
			
			List<Filter> filters = ActiveFilters.ToList();
			
			// The file position when 'm_line_index' was built
			long last_filepos        = m_filepos;
			long half_range          = (m_settings.LineCount * AvrBytesPerLine) / 2; // todo, change this to actual buffer size in MB
			bool include_blank_lines = false;//m_settings.IncludeBlankLines; // todo
			
			// Find the new line indices in a background thread
			ThreadPool.QueueUserWorkItem(a=>
				{
					try
					{
						// Signal that a build is underway
						m_sync_build_ended.Reset();
						
						// Generate the line index in a new buffer
						List<Range> line_index = new List<Range>();
						
						// A temporary buffer for reading sections of the file
						byte[] buf = new byte[CacheSize]; 
						long fileend;
						bool incremental;
						
						using (var file = LoadFile(filepath))
						{
							// Use a fixed file end so that additions to the file don't muck this
							// build up. Reducing the file size during this will probably cause an
							// exception but oh well...
							fileend = file.Length;
							
							// Seek to the first line that starts immediately before filepos
							filepos = FindLineStart(file, filepos, row_delim, encoding, buf);
							long scan_from = filepos;
							
							// Determine the range of bytes to scan in each direction
							long fwd_range = Math.Min(filepos -       0, half_range);
							long bwd_range = Math.Min(fileend - filepos, half_range);
							if (fwd_range != half_range) bwd_range += half_range - fwd_range;
							if (bwd_range != half_range) fwd_range += half_range - bwd_range;
							
							// If the filepos is within the range of the current line index
							// do an incremental update to recentre the line index about filepos
							incremental = file_range.Contains(filepos) && !reload;
							if (incremental)
							{
								// Reduce the byte range so we only scan in the direction we need
								if (filepos >= last_filepos)
								{
									bwd_range = 0;
									fwd_range = half_range - (file_range.m_end - filepos);
									scan_from = file_range.m_end;
								}
								else
								{
									bwd_range = half_range - (filepos - file_range.m_begin);
									fwd_range = 0;
									scan_from = file_range.m_begin;
								}
							}
							
							// Scan backward from 'scan_from' for 'bwd_range' bytes,
							// then scan forward from 'scan_from' for 'fwd_range' bytes.
							FindLines(file, scan_from, true , bwd_range, line_index, encoding, row_delim, filters, include_blank_lines, buf);
							FindLines(file, scan_from, false, fwd_range, line_index, encoding, row_delim, filters, include_blank_lines, buf);
						}
						
						// Marshal the results back to the main thread
						Action MergeLineIndexDelegate = () =>
						{
							MergeLineIndex(line_index, filepos, fileend, incremental);
							if (on_complete != null) on_complete();
						};
						BeginInvoke(MergeLineIndexDelegate);
					}
					catch (Exception ex)
					{
						Debug.WriteLine("Exception ended BuildLineIndex() call: {0}", ex.Message);
					}
					finally
					{
						m_sync_build_ended.Set();
					}
				});
		}
		
		/// <summary>Buffer a maximum of 'count' bytes from 'stream' into 'buf' (note,
		/// automatically capped at buf.Length). If 'backward' is true the stream is seeked
		/// backward from the current position before reading and then seeked backward again
		/// after reading so that conceptually the file position moves in the direction of
		/// the read. Returns the number of bytes buffered in 'buf'</summary>
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
				// Skip the byte order mark (BOM)
				if (pos == 0 && file.ReadByte() != -1 && file.ReadByte() != -1) {}
				
				// Ensure a 16-bit word boundary
				if ((file.Position % 2) == 1)
					file.ReadByte();
			}
			else if (encoding.Equals(Encoding.UTF8))
			{
				// Some programs store a byte order mark (BOM) at the start of UTF-8 text files.
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
		
		/// <summary>Returns the index in 'buf' of the start of a line, starting from 'start'.
		/// If not found, returns -1 when searching backwards, or length when searching forwards</summary>
		private static int FindLineStart(byte[] buf, int start, int length, byte[] row_delim, bool backward)
		{
			Debug.Assert(start <= length);
			int i = start, di = backward ? -1 : 1;
			for (; i >= 0 && i < length; i += di)
			{
				// Quick test using the first byte of the row delimiter
				if (buf[i] != row_delim[0]) continue;
				
				// Test the remaining bytes of the row delimiter
				bool is_line_end = (i + row_delim.Length) <= length;
				for (int j = 1; is_line_end && j != row_delim.Length; ++j) is_line_end = buf[i+j] == row_delim[j];
				if (!is_line_end) continue;
				
				// 'i' now points to the start of a row delimiter,
				// shift it forward to the start of the line.
				i += row_delim.Length;
				break;
			}
			return i;
		}
		
		/// <summary>Seek to the first line that starts immediately before filepos</summary>
		private static long FindLineStart(FileStream file, long filepos, byte[] row_delim, Encoding encoding, byte[] buf)
		{
			file.Seek(filepos, SeekOrigin.Begin);
			for (;;)
			{
				// Read a block into 'buf'
				int read = Buffer(file, buf, buf.Length, true, encoding);
				if (read == 0) return 0; // assume the first character in the file is the start of a line
				
				// Scan for a line start
				int idx = FindLineStart(buf, read, read, row_delim, true);
				if (idx != -1) return file.Position + idx; // found
			}
		}
		
		/// <summary>Add a line to 'line_index'.
		/// 'base_addr' is the file position that 'rng' is relative to.
		/// 'buf' is the buffer that holds the line data read from the file.</summary>
		private static void AddLine(List<Range> line_index, long base_addr, Range rng, byte[] buf, Encoding encoding, IEnumerable<Filter> filters)
		{
			// Test 'text' against each filter to see if it's included
			// Note: not caching this string because we want to read immediate data
			// from the file to pick up file changes.
			string text = encoding.GetString(buf, (int)rng.m_begin, (int)rng.Count);
			foreach (var f in filters)
				if (!f.IsMatch(text))
					return;
			
			// Convert the byte range to a file range
			rng.Shift(base_addr);
			line_index.Add(rng);
		}

		/// <summary>Scan the file from 'filepos' adding whole lines to 'line_index' until 'length' bytes have been read</summary>
		/// <param name="file">The file to scan</param>
		/// <param name="filepos">The position in the file to start scanning from </param>
		/// <param name="backward">The direction to scan</param>
		/// <param name="length">The number of bytes to scan over</param>
		/// <param name="line_index">The line index to add the found line ranges to</param>
		/// <param name="encoding">The text file encoding</param>
		/// <param name="row_delim">The bytes that identify an end of line</param>
		/// <param name="filters">Filters that remove lines from the results</param>
		/// <param name="include_blank_lines">True to include blank lines in the output</param>
		/// <param name="buf">A buffer to use when buffering file data</param>
		private static void FindLines(
			FileStream file,
			long filepos,
			bool backward,
			long length,
			List<Range> line_index,
			Encoding encoding,
			byte[] row_delim,
			IEnumerable<Filter> filters,
			bool include_blank_lines,
			byte[] buf)
		{
			int initial_count = line_index.Count;
			
			// Seek to the start position (should be the start of a line).
			file.Seek(filepos, SeekOrigin.Begin);
			for (long scanned = 0; scanned != length;)
			{
				// Check where we need to cancel
				if (m_sync_cancel_building.WaitOne(0))
					return;
				
				// Read as much as possible into 'buf'
				int remaining = (int)(length - scanned);
				int read = Buffer(file, buf, remaining, backward, encoding);
				if (read <= row_delim.Length) break;
				
				int i      = backward ? read - row_delim.Length : 0;
				int iend   = backward ? -1 : read;
				int di     = backward ? -1 : 1;
				int lasti  = i;
				
				// Scan the buffer for lines
				long base_addr = file.Position;
				for (i = FindLineStart(buf, i+di, read, row_delim, backward); i != iend; lasti = i, i = FindLineStart(buf, i+di, read, row_delim, backward))
				{
					// 'i' points to the start of a line,
					// 'lasti' points to the start of the last line we found
					// Get the range in buf containing the line
					Range line = backward
						? new Range(i, lasti - row_delim.Length)
						: new Range(lasti, i - row_delim.Length);
					
					// Add the line to the line index
					if (include_blank_lines || !line.Empty)
						AddLine(line_index, base_addr, line, buf, encoding, filters);
				}
				
				// If 'length' bytes have passed through 'buf' then we're done.
				// otherwise, add whole lines to the scanned count so we catch
				// lines that span the buffer boundary
				if (read == remaining) break;
				scanned += backward ? (read - lasti) : lasti;
			}
			
			// Scanning backward adds lines to the line index in reverse order,
			// we need to flip the buffer over the range we've added
			if (backward)
			{
				int final_count = line_index.Count;
				line_index.Reverse(initial_count, final_count - initial_count);
			}
		}
		
		/// <summary>Merge or replace 'm_line_index' with 'line_index'</summary>
		private void MergeLineIndex(List<Range> line_index, long filepos, long fileend, bool incremental)
		{
			// If not incremental, just replace 'm_line_index'
			if (!incremental)
				m_line_index = line_index;
			
			// Otherwise append to the front
			else if (filepos < m_filepos)
			{
				m_line_index.InsertRange(0, line_index);
			}
			
			// Or append to the back
			else
			{
				m_line_index.AddRange(line_index);
			}
			
			// Save the new file position and length
			m_filepos = filepos;
			m_fileend = fileend;
		}
		
		/// <summary>Return a collection of the currently active filters</summary>
		private IEnumerable<Filter> ActiveFilters
		{
			get { return from ft in Filter.Import(m_settings.FilterPatterns) where ft.Active select ft; }
		}
		
		/// <summary>Auto detect the line end format. Must be called from the main thread</summary>
		private byte[] GuessRowDelimiter(string filepath, Encoding encoding)
		{
			// Auto detect if the settings say "detect", that way m_row_delim
			// is cached as the appropriate row delimiter while the current file is loaded
			if (m_settings.RowDelimiter.Length == 0)
			{
				using (var r = new StreamReader(LoadFile(filepath)))
				{
					int idx = -1, ofs = 0;
					const int len = 1024;
					char[] buf = new char[len + 1];
					for (int read = r.ReadBlock(buf, ofs, len); read != 0; read = r.ReadBlock(buf, ofs, len))
					{
						idx = Array.FindIndex(buf, 0, read, c => c == '\n' || c == '\r');
						if (idx == -1) continue;
						if (idx == read - 1) { buf[0] = buf[read-1]; ofs = 1; continue; }
						break;
					}
					if (idx != -1)
					{
						m_row_delim = buf[idx] == '\r' && buf[idx+1] == '\n'
							? encoding.GetBytes(buf, idx, 2)
							: encoding.GetBytes(buf, idx, 1);
					}
				}
			}
			return m_row_delim;
		}
		
		/// <summary>Guess the text file encoding</summary>
		private Encoding GuessEncoding(string filepath)
		{
			// Auto detect if the settings say "detect", that way m_encoding
			// is cached as the appropriate encoding while the current file is loaded
			if (m_settings.Encoding.Length == 0)
			{
				using (var file = LoadFile(filepath))
				{
					// Look for a BOM
					byte[] buf = new byte[4];
					int read = file.Read(buf, 0, buf.Length);
					if (read >= 3 && buf[0] == 0xEF && buf[1] == 0xBB && buf[2] == 0xBF)
 						m_encoding = Encoding.UTF8;
					else if (read >= 2 && buf[0] == 0xFE && buf[1] == 0xFF)
						m_encoding = Encoding.BigEndianUnicode;
					else if (read >= 2 && buf[0] == 0xFF && buf[1] == 0xFE)
						m_encoding = Encoding.Unicode;
					else // If no valid bomb is found, assume UTF-8 as that is a superset of ASCII
						m_encoding = Encoding.UTF8;
				}
			}
			return m_encoding;
		}
	}
}

/*
		
		/// <summary>Build a line index of the file</summary>
		private void BuildLineIndex(string filepath)
		{
			// Local copies of variables used by the follow async task
			List<Range> line_index = new List<Range>();
			List<Filter> filters   = ActiveFilters.ToList();
			Encoding encoding      = (Encoding)GuessEncoding(filepath).Clone();
			byte[] row_delim       = (byte[])GuessRowDelimiter(filepath, m_encoding).Clone();
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
			try
			{
				if (task.ShowDialog(this) != DialogResult.OK)
					return;
			}
			catch (Exception ex)
			{
				if (ex is TargetInvocationException && ex.InnerException != null) ex = ex.InnerException;
				MessageBox.Show(this, string.Format(Resources.BuildLineIndexErrorMsg, ex.Message), Resources.ReadingFileFailed, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			
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
		
		/// <summary>
		/// Test 'text' against 'filters' and returns true if it should be included.
		/// Updates 'line' in the event that the filters include non-binary matches</summary>
		private static bool PassesFilters(IEnumerable<Filter> filters, string text, out Range line)
		{
			// Test 'text' against each filter to see if it's included
			line = new Range(0, text.Length);
			foreach (var f in filters)
			{
				// First see if it passes this filter
				if (!f.IsMatch(text))
					return false;
				
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
			return true;
		}
		
 */