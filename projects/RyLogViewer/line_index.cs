using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using pr.common;
using pr.maths;

namespace RyLogViewer
{
	public partial class Main
	{
		/// <summary>Returns a file stream for 'filepath' openned with R/W sharing</summary>
		private static FileStream LoadFile(string filepath, int buffer_size = 4096)
		{
			return new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, buffer_size);
		}
		
		/// <summary>Returns the full byte range currently represented by 'm_line_index'</summary>
		private Range FileRange
		{
			get { return m_line_index.Count != 0 ? new Range(m_line_index.First().m_begin, m_line_index.Last().m_end) : Range.Zero; }
		}

		/// <summary>
		/// Returns the byte range of the file currently covered by 'm_line_index'
		/// Note: the range is between starts of lines, not the full range. This is because
		/// this is the only range we know is complete and doesn't contain partial lines</summary>
		private Range LineIndexRange
		{
			get { return m_line_index.Count != 0 ? new Range(m_line_index.First().m_begin, m_line_index.Last().m_begin) : Range.Zero; }
		}

		/// <summary>Returns the byte range of the currently displayed rows</summary>
		private Range DisplayedRowsRange
		{
			get
			{
				int b = Maths.Clamp(m_grid.FirstDisplayedScrollingRowIndex ,0 ,m_line_index.Count-1);
				int e = Maths.Clamp(b + m_grid.DisplayedRowCount(true)     ,0 ,m_line_index.Count-1);
				return b == e ? Range.Zero : new Range(m_line_index[b].m_begin, m_line_index[e].m_end);
			}
		}

		/// <summary>Returns the byte range of the currently selected row</summary>
		private Range SelectedRowRange
		{
			get
			{
				int row = SelectedRow;
				if (row < 0 || row >= m_line_index.Count) return Range.Zero;
				return new Range(m_line_index[row].m_begin, m_line_index[row].m_end);
			}
		}
		
		/// <summary>Reload the current file</summary>
		private void Reload()
		{
			if (!FileOpen) return;
			
			// Preserve the selected row if possible
			int selected_row =  SelectedRow;
			
			// Update m_line_index
			BuildLineIndex(m_filepath, m_filepos, true,
				(incremental, added, to_front)=>
				{
					if (!incremental)  SelectedRow = Maths.Clamp(selected_row, 0, m_line_index.Count);
					else if (to_front) SelectedRow = selected_row + added;
					UpdateUI();
				});
		}
		
		/// <summary>Cause an incremental update to the line index</summary>
		private void UpdateLineIndex(long filepos)
		{
			if (!FileOpen) return;
			
			// Preserve the selected row if possible
			int selected_row =  SelectedRow;
			
			// Update m_line_index
			BuildLineIndex(m_filepath, filepos, false,
				(incremental, added, to_front)=>
				{
					if (!incremental)  SelectedRow = Maths.Clamp(selected_row, 0, m_line_index.Count);
					else if (to_front) SelectedRow = selected_row + added;
					UpdateUI();
					
					// On completion, check if the file has changed again and rerun if it has
					if (m_fileend != m_file.Length)
						m_file_changed.Signal();
				});
		}
		
		/// <summary>
		/// An issue number for the build.
		/// Builder threads abort as soon as possible when they notice this
		/// value is not equal to their startup issue number</summary>
		private static int m_build_issue = 0;
		private bool m_reload_in_progress; // Should only be set/tested in the main thread
		private static void TestBuildCancelled(int build_issue)
		{
			if (Interlocked.CompareExchange(ref m_build_issue, build_issue, build_issue) != build_issue)
				throw new OperationCanceledException();
		}

		/// <summary>The call back called when a BuildLineIndex call completes</summary>
		private delegate void OnBuildComplete(bool incremental, int lines_added, bool to_front);
		
		/// <summary>Generates the line index centred around 'filepos'.
		/// If 'filepos' is within the byte range of the current line_index then an incremental
		/// search for lines is done in the direction needed to recentre the line list around 'filepos'
		/// 'on_complete'</summary>
		private void BuildLineIndex(string filepath, long filepos, bool reload, OnBuildComplete on_complete)
		{
			// If the file position hasn't moved, do bother doing anything
			if (filepos == m_filepos && !reload)
			{
				if (on_complete != null) on_complete(true, 0, false);
				return;
			}
			
			// Incremental updates cannot supplant reloads
			if (m_reload_in_progress && reload == false)
				return;
			
			// Cause any existing builds to stop by changing the issue number
			Interlocked.Increment(ref m_build_issue);
			m_reload_in_progress = reload;
			
			// Find the byte range of the file currently loaded
			Range file_range       = FileRange;
			Range line_index_range = LineIndexRange;
			
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
			long last_filepos  = m_filepos;
			long half_range    = m_settings.FileBufSizeKB * 512; // * 1 KB / 2
			bool ignore_blanks = m_settings.IgnoreBlankLines;
			
			// Find the new line indices in a background thread
			ThreadPool.QueueUserWorkItem(bi =>
				{
					try
					{
						int build_issue = (int)bi;
						TestBuildCancelled(build_issue);
						
						// Generate the line index in a new buffer
						List<Range> line_index = new List<Range>();
						
						// A temporary buffer for reading sections of the file
						byte[] buf = new byte[Constants.FileReadChunkSize]; 
						long fileend;
						bool incremental;
						
						using (var file = LoadFile(filepath))
						{
							// Use a fixed file end so that additions to the file don't muck this
							// build up. Reducing the file size during this will probably cause an
							// exception but oh well...
							fileend = file.Length;
							
							// Seek to the first line that starts immediately before filepos
							filepos = FindLineStart(file, filepos, fileend, row_delim, encoding, buf);
							TestBuildCancelled(build_issue);
							long scan_from = filepos;
							
							// Determine the range of bytes to scan in each direction
							long bwd_range = Math.Min(filepos -       0, half_range);
							long fwd_range = Math.Min(fileend - filepos, half_range);
							if      (fwd_range != half_range) bwd_range += half_range - fwd_range;
							else if (bwd_range != half_range) fwd_range += half_range - bwd_range;
							
							// If the filepos is within the range of the current line index
							// do an incremental update to recentre the line index about filepos
							incremental = file_range.Contains(filepos) && !reload;
							if (incremental)
							{
								// Reduce the byte range so we only scan in the direction we need
								if (filepos >= last_filepos)
								{
									bwd_range = 0;
									fwd_range = Maths.Clamp(half_range - (line_index_range.m_end - filepos), 0, fileend);
									scan_from = line_index_range.m_end;
								}
								else
								{
									bwd_range = Maths.Clamp(half_range - (filepos - line_index_range.m_begin), 0, fileend);
									fwd_range = 0;
									scan_from = line_index_range.m_begin;
								}
							}
							
							// Scan backward from 'scan_from' for 'bwd_range' bytes,
							// then scan forward from 'scan_from' for 'fwd_range' bytes.
							FindLines(file, scan_from, fileend, true , bwd_range, line_index, encoding, row_delim, filters, ignore_blanks, buf, build_issue);
							FindLines(file, scan_from, fileend, false, fwd_range, line_index, encoding, row_delim, filters, ignore_blanks, buf, build_issue);
						}
						
						// Marshal the results back to the main thread
						Action MergeLineIndexDelegate = () =>
						{
							// This lamdba runs in the main thread, so if the build issue is the same at
							// the start of this method it can't be changed until after this function returns.
							if (m_build_issue != build_issue) return;
							MergeLineIndex(line_index, half_range*2, filepos, fileend, incremental);
							if (on_complete != null) on_complete(incremental, line_index.Count, filepos < m_filepos);
							m_reload_in_progress = false;
						};
						BeginInvoke(MergeLineIndexDelegate);
					}
					catch (OperationCanceledException) {}
					catch (Exception ex) { Debug.WriteLine("Exception ended BuildLineIndex() call: " + ex.Message); }
				}, m_build_issue);
		}
		
		/// <summary>Buffer a maximum of 'count' bytes from 'stream' into 'buf' (note,
		/// automatically capped at buf.Length). If 'backward' is true the stream is seeked
		/// backward from the current position before reading and then seeked backward again
		/// after reading so that conceptually the file position moves in the direction of
		/// the read. Returns the number of bytes buffered in 'buf'</summary>
		private static int Buffer(Stream file, int count, long fileend, Encoding encoding, bool backward, byte[] buf)
		{
			// The number of bytes to buffer
			count = Math.Min(count, buf.Length);
			count = Math.Min(count, (int)(backward ? file.Position : fileend - file.Position));
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
				const int encode_mask = 0xC0; // 11000000
				const int encode_char = 0x80; // 10000000
				for (;;)
				{
					int b = file.ReadByte();
					if (b == -1) return 0; // End of file
					if ((b & encode_mask) == encode_char) continue; // If 'b' is a byte halfway through an encoded char, keep reading
					
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
			Debug.Assert(start >= -1 && start <= length);
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
		private static long FindLineStart(FileStream file, long filepos, long fileend, byte[] row_delim, Encoding encoding, byte[] buf)
		{
			file.Seek(filepos, SeekOrigin.Begin);
			for (;;)
			{
				// Read a block into 'buf'
				int read = Buffer(file, buf.Length, fileend, encoding, true, buf);
				if (read == 0) return 0; // assume the first character in the file is the start of a line
				
				// Scan for a line start
				int idx = FindLineStart(buf, read - 1, read, row_delim, true);
				if (idx != -1) return file.Position + idx; // found
			}
		}
		
		/// <summary>Add a line to 'line_index'.
		/// 'base_addr' is the file position that 'rng' is relative to.
		/// 'buf' is the buffer that holds the line data read from the file.</summary>
		private static void AddLine(List<Range> line_index, long base_addr, long fileend, Range rng, byte[] buf, Encoding encoding, IEnumerable<Filter> filters)
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
			Debug.Assert(new Range(0,fileend).Contains(rng));
			line_index.Add(rng);
		}

		/// <summary>Scan the file from 'filepos' adding whole lines to 'line_index' until 'length' bytes have been read</summary>
		/// <param name="file">The file to scan</param>
		/// <param name="filepos">The position in the file to start scanning from </param>
		/// <param name="fileend">The current known length of the file</param>
		/// <param name="backward">The direction to scan</param>
		/// <param name="length">The number of bytes to scan over</param>
		/// <param name="line_index">The line index to add the found line ranges to</param>
		/// <param name="encoding">The text file encoding</param>
		/// <param name="row_delim">The bytes that identify an end of line</param>
		/// <param name="filters">Filters that remove lines from the results</param>
		/// <param name="ignore_blanks">True to ignore blank lines in the output</param>
		/// <param name="buf">A buffer to use when buffering file data</param>
		/// <param name="build_issue">The build issue number assign to this thread</param>
		private static void FindLines(FileStream file, long filepos, long fileend, bool backward, long length, List<Range> line_index, Encoding encoding, byte[] row_delim, IEnumerable<Filter> filters, bool ignore_blanks, byte[] buf, int build_issue)
		{
			int initial_count = line_index.Count;
			
			long read_addr = filepos;
			for (long scanned = 0; scanned != length;)
			{
				// Check whether we need to cancel
				TestBuildCancelled(build_issue);
				
				// Seek to the start position (should be the start of a line).
				file.Seek(read_addr, SeekOrigin.Begin);
				
				// Read as much as possible into 'buf'
				int remaining = (int)(length - scanned);
				int read = Buffer(file, remaining, fileend, encoding, backward, buf);
				if (read < row_delim.Length) break;
				
				// filepos is the start of a line, so when reading backwards
				// start searching from before the row delimiter
				int lasti      = backward ? read : 0;
				int i          = backward ? read - 1 - row_delim.Length : 0;
				int iend       = backward ? -1 : read;
				long base_addr = backward ? file.Position : file.Position - read;
				
				// Scan the buffer for lines
				for (i = FindLineStart(buf, i, read, row_delim, backward); i != iend; i = FindLineStart(buf, i, read, row_delim, backward))
				{
					// 'i' points to the start of a line,
					// 'lasti' points to the start of the last line we found
					// Get the range in buf containing the line
					Range line = backward
						? new Range(i, lasti - row_delim.Length)
						: new Range(lasti, i - row_delim.Length);
					
					// Add the line to the line index
					if (!line.Empty || !ignore_blanks)
						AddLine(line_index, base_addr, fileend, line, buf, encoding, filters);
					
					lasti = i;
					if (backward) i -= row_delim.Length + 1;
				}
				
				// If 'length' bytes have passed through 'buf' then we're done.
				// otherwise, add whole lines to the scanned count so we catch
				// lines that span the buffer boundary
				if (read == remaining) break;
				scanned   += backward ?  (read - lasti) : lasti;
				read_addr =  filepos + (backward ? -scanned : +scanned);
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
		private void MergeLineIndex(List<Range> line_index, long cache_range, long filepos, long fileend, bool incremental)
		{
			// If not incremental, just replace 'm_line_index'
			if (!incremental)
			{
				m_line_index = line_index;
			}
			else if (line_index.Count != 0)
			{
				// Otherwise append to the front and trim the end
				if (filepos < m_filepos)
				{
					m_line_index.InsertRange(0, line_index);
					
					// Trim the tail
					Range line_range = LineIndexRange;
					int i,iend = m_line_index.Count;
					for (i = iend; i-- != 0;)
					{
						if (m_line_index[i].m_begin - line_range.m_begin > cache_range) continue;
						++i;
						break;
					}
					m_line_index.RemoveRange(i, iend-i);
				}
				// Or append to the back and trim the start
				else
				{
					m_line_index.AddRange(line_index);
				
					// Trim the head
					Range line_range = LineIndexRange;
					int i,iend = m_line_index.Count;
					for (i = 0; i != iend; ++i)
					{
						if (line_range.m_end - m_line_index[i].m_end > cache_range) continue;
						--i;
						break;
					}
					m_line_index.RemoveRange(0, i+1);
				}
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
