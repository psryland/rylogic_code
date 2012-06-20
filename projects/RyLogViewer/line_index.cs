using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace RyLogViewer
{
	public partial class Main
	{
		/// <summary>Returns a file stream for 'filepath' openned with R/W sharing</summary>
		private static FileStream LoadFile(string filepath, int buffer_size = 4096)
		{
			return new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite|FileShare.Delete, buffer_size, FileOptions.RandomAccess);
		}
		
		/// <summary>Returns the byte range of the complete file, last time we checked its length</summary>
		private Range FileByteRange
		{
			get { return new Range(0, m_fileend); }
		}

		/// <summary>Returns the byte range within the file that would be buffered given 'filepos'</summary>
		private static Range BufferRange(long filepos, long fileend, long buf_size)
		{
			Range rng = new Range();
			long ovr, hbuf = buf_size / 2;
			
			// Start with the range that has filepos in the middle
			rng.m_begin = filepos - hbuf;
			rng.m_end   = filepos + hbuf;
			
			// Any overflow, add to the other range
			if ((ovr = 0     - rng.m_begin) > 0) { rng.m_begin += ovr; rng.m_end   += ovr; }
			if ((ovr = rng.m_end - fileend) > 0) { rng.m_end   -= ovr; rng.m_begin -= ovr; }
			if ((ovr = 0     - rng.m_begin) > 0) { rng.m_begin += ovr; }
			
			Debug.Assert(rng.m_begin >= 0 && rng.m_end <= fileend && rng.m_begin <= rng.m_end);
			return rng;
		}

		/// <summary>Returns the full byte range currently represented by 'm_line_index'</summary>
		private Range LineIndexRange
		{
			get { return m_line_index.Count != 0 ? new Range(m_line_index.First().m_begin, m_line_index.Last().m_end) : Range.Zero; }
		}

		/// <summary>
		/// Returns the byte range of the file currently covered by 'm_line_index'
		/// Note: the range is between starts of lines, not the full range. This is because
		/// this is the only range we know is complete and doesn't contain partial lines</summary>
		private Range LineStartIndexRange
		{
			get { return m_line_index.Count != 0 ? new Range(m_line_index.First().m_begin, m_line_index.Last().m_begin) : Range.Zero; }
		}

		/// <summary>Returns the byte range of the currently displayed rows</summary>
		private Range DisplayedRowsRange
		{
			get
			{
				if (m_line_index.Count == 0) return Range.Zero;
				int b = Maths.Clamp(m_grid.FirstDisplayedScrollingRowIndex ,0 ,m_line_index.Count-1);
				int e = Maths.Clamp(b + m_grid.DisplayedRowCount(true)     ,0 ,m_line_index.Count-1);
				return new Range(m_line_index[b].m_begin, m_line_index[e].m_end);
			}
		}

		/// <summary>Returns the bounding byte range of the currently selected rows.</summary>
		private Range SelectedRowRange
		{
			get
			{
				Range rng = Range.Invalid;
				foreach (DataGridViewRow r in m_grid.SelectedRows)
					rng.Encompase(m_line_index[r.Index]);
				return !rng.Equals(Range.Invalid) ? rng : Range.Zero;
			}
		}
		
		/// <summary>
		/// An issue number for the build.
		/// Builder threads abort as soon as possible when they notice this
		/// value is not equal to their startup issue number</summary>
		private static int m_build_issue = 0;
		private bool m_reload_in_progress; // Should only be set/tested in the main thread
		private static bool BuildCancelled(int build_issue)
		{
			return Interlocked.CompareExchange(ref m_build_issue, build_issue, build_issue) != build_issue;
		}

		/// <summary>Cause a currently running BuildLineIndex call to be cancelled</summary>
		private void CancelBuildLineIndex() // will be used when caching numbers of lines, not byte range
		{
			Log.Info("build (id {0}) cancelled", m_build_issue);
			Interlocked.Increment(ref m_build_issue);
			UpdateStatusProgress(1,1);
		}

		/// <summary>
		/// Generates the line index centred around 'filepos'.
		/// If 'filepos' is within the byte range of 'm_line_index' then an incremental search for
		/// lines is done in the direction needed to recentre the line list around 'filepos'.
		/// If 'reload' is true a full rescan of the file is done</summary>
		private void BuildLineIndex(long filepos, bool reload, Action on_success = null)
		{
			// No file open, nothing to do
			if (!FileOpen)
				return;
				
			// Incremental updates cannot supplant reloads
			if (m_reload_in_progress && reload == false)
				return;
				
			// If the file position hasn't moved, don't bother doing anything
			if (filepos == m_filepos && !reload)
			{
				if (on_success != null) on_success(); // consider this success
				return;
			}
				
			// Cause any existing builds to stop by changing the issue number
			Interlocked.Increment(ref m_build_issue);
			m_reload_in_progress = reload;
			//Log.Info("build start request (id {0})", m_build_issue);
			Log.Info("build start request (id {0})\n{1}", m_build_issue, Util.StackTrace(0,9));
				
			// Find the byte range of the file currently loaded
			Range line_index_range  = LineIndexRange;
			
			// If this is not a 'reload', guess the encoding
			Encoding encoding = reload
				? (Encoding)GuessEncoding(m_filepath).Clone()
				: (Encoding)m_encoding.Clone();
			
			// If this is not a 'reload', guess the row_delimiters
			byte[] row_delim = reload || m_row_delim == null
				? (byte[])GuessRowDelimiter(m_filepath, encoding).Clone()
				: (byte[])m_row_delim.Clone();
				
			List<Filter> filters = ActiveFilters.ToList();
			
			long bufsize          = m_bufsize;
			int  line_cache_count = m_line_cache_count;
			long last_filepos     = m_filepos;
			int  next_line_index  = LineIndex(m_line_index, filepos);
			int  last_line_count  = m_line_index.Count;
			bool ignore_blanks    = m_settings.IgnoreBlankLines;
			
			// Find the new line indices in a background thread
			ThreadPool.QueueUserWorkItem(bi =>
				{
					int build_issue = (int)bi;
					try
					{
						Log.Info("build started. (id {0})", build_issue);
						if (BuildCancelled(build_issue)) return;
						using (var file = LoadFile(m_filepath))
						{
							// A temporary buffer for reading sections of the file
							byte[] buf = new byte[Constants.FileReadChunkSize];
						
							// Use a fixed file end so that additions to the file don't muck this
							// build up. Reducing the file size during this will probably cause an
							// exception but oh well...
							long fileend = file.Length;
							
							// Seek to the first line that starts immediately before filepos
							filepos = FindLineStart(file, filepos, fileend, row_delim, encoding, buf);
							if (BuildCancelled(build_issue)) return;
							
							// True if 'filepos' has moved toward the end of the file
							bool toward_start = filepos < last_filepos;
							bool scan_backward = fileend - filepos >= filepos - 0; // scan in the most bound direction first
							
							// Determine the range of bytes to scan in each direction
							Range rng = BufferRange(filepos, fileend, bufsize);
							
							// The number of lines to cache in the forward and backward directions
							int bwd_lines = line_cache_count/2; 
							int fwd_lines = line_cache_count/2;
							
							// Incremental loading
							if (!reload)
							{
								// If the range overlaps the currently cached range (and this isn't a reload)
								// reduce the range to only scan the range that isn't cached
								bool incremental;
								if (toward_start) { incremental = line_index_range.m_begin < rng.m_end;   if (incremental) rng.m_end   = Math.Max(rng.m_begin, line_index_range.m_begin); }
								else              { incremental = line_index_range.m_end   > rng.m_begin; if (incremental) rng.m_begin = Math.Min(rng.m_end,   line_index_range.m_end); }
								Debug.Assert(rng.Count >= 0);
								
								// If the range has been reduced, then the number of lines to be scanned may also have reduced
								// If 'scan_from' is within the currently cached lines, then we can limit the number of lines to
								// scan. If outside the currently cached range, then we can't predict how many lines are needed,
								// but if we search in the unbound direction first for up to 'line_cache_count/2' lines, then the
								// number of lines found in the bound direction will be <= 'line_cache_count/2' and these found
								// lines can be incrementally added to the current cache
								if (incremental)
								{
									// If 'filepos' is within the currently cached range, limit the number of lines to scan
									if (next_line_index >= 0 && next_line_index < line_cache_count)
									{
										// We can only incrementally add lines to either to start or end of the current
										// line cache. Make sure the range and number of lines to scan are correct.
										if (toward_start)
										{
											fwd_lines = 0;
											bwd_lines -= Maths.Clamp(next_line_index - 0, 0, line_cache_count/2);
										}
										else
										{
											bwd_lines = 0;
											fwd_lines -= Maths.Clamp(last_line_count - next_line_index, 0, line_cache_count/2);
										}
									}
									else
									{
										scan_backward = toward_start;
									}
								}
								Debug.Assert(rng.Count >= 0);
								Debug.Assert(fwd_lines + bwd_lines != 0 || rng.Empty);
							}
							
							// Caps the number of lines read for each of the forward and backward searches
							// Wrapped in an array to prevent 'access to modified closure'
							int[] line_limit = {0};
							List<Range>[] line_buf = {null};
							
							// Line index buffers for collecting the results
							List<Range> fwd_line_buf = new List<Range>();
							List<Range> bwd_line_buf = new List<Range>();
							
							// Callback function that adds lines to 'line_index'
							AddLineFunc add_line = (line, baddr, fend, bf, enc) =>
								{
									if (line.Empty && ignore_blanks)
										return true;
										
									// Test 'text' against each filter to see if it's included
									// Note: not caching this string because we want to read immediate data
									// from the file to pick up file changes.
									string text = encoding.GetString(buf, (int)line.m_begin, (int)line.Count);
									if (!PassesFilters(text, filters))
										return true;
									
									// Convert the byte range to a file range
									line.Shift(baddr);
									Debug.Assert(new Range(0,fileend).Contains(line));
									line_buf[0].Add(line);
									Debug.Assert(line_buf[0].Count <= line_limit[0]);
									return (fwd_line_buf.Count + bwd_line_buf.Count) < line_limit[0];
								};
							
							// Callback for updating progress
							ProgressFunc progress = (scanned, length) =>
								{
									Action update_progress_bar = () => UpdateStatusProgress(scanned, length);//fwd_line_buf.Count + bwd_line_buf.Count, line_cache_count);
									BeginInvoke(update_progress_bar);
									return !BuildCancelled(build_issue);
								};
							
							// Scan twice, starting in the direction of the smallest range so that any
							// unused cache space is used by the search in the other direction
							long scan_from = Maths.Clamp(filepos, rng.m_begin, rng.m_end);
							for (int a = 0; a != 2; ++a, scan_backward = !scan_backward)
							{
								line_buf[0]    = scan_backward ? bwd_line_buf : fwd_line_buf;
								line_limit[0] += scan_backward ? bwd_lines    : fwd_lines;    // Push out the limit of allowed lines
								long range     = scan_backward ? scan_from - rng.m_begin : rng.m_end - scan_from;
								FindLines(file, scan_from, fileend, scan_backward, range, add_line, encoding, row_delim, buf, progress);
								if (BuildCancelled(build_issue)) return;
							}
							
							// Scanning backward adds lines to the line index in reverse order,
							// we need to flip the buffer over the range that was added.
							bwd_line_buf.Reverse();
							List<Range> line_index = bwd_line_buf.Concat(fwd_line_buf).ToList();
							
							// Marshal the results back to the main thread
							Action MergeLineIndexDelegate = () =>
							{
								// This lamdba runs in the main thread, so if the build issue is the same at
								// the start of this method it can't be changed until after this function returns.
								if (BuildCancelled(build_issue)) return;
							
								// Merge the line index results
								int row_delta = MergeLineIndex(line_index, bufsize, filepos, fileend, reload);
							
								// Ensure the grid is updated
								UpdateUI(row_delta);
							
								// On completion, check if the file has changed again and rerun if it has
								m_watch.CheckForChangedFiles();
							
								if (on_success != null) on_success();
								m_reload_in_progress = false;
							};
							BeginInvoke(MergeLineIndexDelegate);
						}
					}
					catch (OperationCanceledException) {}
					catch (Exception ex) { Debug.WriteLine("Exception ended BuildLineIndex() call: " + ex.Message); }
					finally
					{
						Action update_progress_bar = ()=>{ UpdateStatusProgress(1,1); };
						BeginInvoke(update_progress_bar);
					}
				}, m_build_issue);
		}
		
		/// <summary>Buffer a maximum of 'count' bytes from 'stream' into 'buf' (note,
		/// automatically capped at buf.Length). If 'backward' is true the stream is seeked
		/// backward from the current position before reading and then seeked backward again
		/// after reading so that conceptually the file position moves in the direction of
		/// the read. Returns the number of bytes buffered in 'buf'</summary>
		private static int Buffer(Stream file, int count, long fileend, Encoding encoding, bool backward, byte[] buf, out bool eof)
		{
			long pos = file.Position;
			eof = backward ? pos == 0 : pos == fileend;
			
			// The number of bytes to buffer
			count = Math.Min(count, buf.Length);
			count = Math.Min(count, (int)(backward ? file.Position : fileend - file.Position));
			if (count == 0) return 0;
			
			// Set the file position to the location to read from
			if (backward) file.Seek(-count, SeekOrigin.Current);
			pos = file.Position;
			eof = backward ? pos == 0 : pos + count == fileend;
			
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
		
		/// <summary>
		/// Returns the index in 'buf' of one past the next delimiter, starting from 'start'.
		/// If not found, returns -1 when searching backwards, or length when searching forwards</summary>
		private static int FindNextDelim(byte[] buf, int start, int length, byte[] delim, bool backward)
		{
			Debug.Assert(start >= -1 && start <= length);
			int i = start, di = backward ? -1 : 1;
			for (; i >= 0 && i < length; i += di)
			{
				// Quick test using the first byte of the delimiter
				if (buf[i] != delim[0]) continue;
				
				// Test the remaining bytes of the delimiter
				bool is_match = (i + delim.Length) <= length;
				for (int j = 1; is_match && j != delim.Length; ++j) is_match = buf[i+j] == delim[j];
				if (!is_match) continue;
				
				// 'i' now points to the start of the delimiter,
				// shift it forward to one past the delimiter.
				i += delim.Length;
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
				bool eof;
				int read = Buffer(file, buf.Length, fileend, encoding, true, buf, out eof);
				if (read == 0) return 0; // assume the first character in the file is the start of a line
				
				// Scan for a line start
				int idx = FindNextDelim(buf, read - 1, read, row_delim, true);
				if (idx != -1) return file.Position + idx; // found
			}
		}
		
		/// <summary>Callback function called by FindLines that is called with each line found</summary>
		/// <param name="rng">The byte range of the line within 'buf', not including row delimiter</param>
		/// <param name="base_addr">The base address in the file that buf is relative to</param>
		/// <param name="fileend">The current known end position of the file</param>
		/// <param name="buf">Buffer containing the buffered file data</param>
		/// <param name="encoding">The text encoding used</param>
		/// <returns>Return true to continue adding lines, false to stop</returns>
		private delegate bool AddLineFunc(Range rng, long base_addr, long fileend, byte[] buf, Encoding encoding);

		/// <summary>Callback function called periodically while finding lines</summary>
		/// <returns>Return true to continue finding, false to abort</returns>
		private delegate bool ProgressFunc(long scanned, long length);

		/// <summary>Scan the file from 'filepos' adding whole lines to 'line_index' until 'length' bytes have been read</summary>
		/// <param name="file">The file to scan</param>
		/// <param name="filepos">The position in the file to start scanning from </param>
		/// <param name="fileend">The current known length of the file</param>
		/// <param name="backward">The direction to scan</param>
		/// <param name="length">The number of bytes to scan over</param>
		/// <param name="add_line">Callback function called with each detected line</param>
		/// <param name="encoding">The text file encoding</param>
		/// <param name="row_delim">The bytes that identify an end of line</param>
		/// <param name="buf">A buffer to use when buffering file data</param>
		/// <param name="progress">Callback function to report progress and allow the find to abort</param>
		private static void FindLines(FileStream file, long filepos, long fileend, bool backward, long length, AddLineFunc add_line, Encoding encoding, byte[] row_delim, byte[] buf, ProgressFunc progress)
		{
			long read_addr = filepos;
			for (long scanned = 0; scanned != length;)
			{
				// Progress update
				if (progress != null && !progress(scanned, length)) return;
				
				// Seek to the start position (should be the start of a line).
				file.Seek(read_addr, SeekOrigin.Begin);
				
				// Read as much as possible into 'buf'
				int remaining = (int)(length - scanned); bool eof;
				int read = Buffer(file, remaining, fileend, encoding, backward, buf, out eof);
				if (read < row_delim.Length) break;
				
				// filepos is the start of a line, so when reading backwards
				// start searching from before the row delimiter
				int lasti      = backward ? read : 0;
				int i          = backward ? read - 1 - row_delim.Length : 0;
				int iend       = backward ? -1 : read;
				long base_addr = backward ? file.Position : file.Position - read;
				
				// Scan the buffer for lines
				for (i = FindNextDelim(buf, i, read, row_delim, backward); i != iend; i = FindNextDelim(buf, i, read, row_delim, backward))
				{
					// 'i' points to the start of a line,
					// 'lasti' points to the start of the last line we found
					// Get the range in buf containing the line
					Range line = backward
						? new Range(i, lasti - row_delim.Length)
						: new Range(lasti, i - row_delim.Length);
					
					// Pass the detected line to the callback
					if (!add_line(line, base_addr, fileend, buf, encoding))
						return;
					
					lasti = i;
					if (backward) i -= row_delim.Length + 1;
				}
				
				// If scanning backwards and we hit the start of the file, treat this as a line start
				// If scanning forward and we hit the end of the file, treat this as the last line (might be partial)
				if (eof)
				{
					if (backward) ++i;
					else
					{
						// If not a partial row, offset i so that we don't remove the row delimiter below
						if (Util.Compare(buf, i-row_delim.Length, row_delim.Length, row_delim, 0, row_delim.Length) != 0)
							i += row_delim.Length;
					}
					
					// 'i' points to the start of a line,
					// 'lasti' points to the start of the last line we found
					// Get the range in buf containing the line
					Range line = backward
						? new Range(i, lasti - row_delim.Length)
						: new Range(lasti, i - row_delim.Length);
					
					// Pass the detected line to the callback
					if (!add_line(line, base_addr, fileend, buf, encoding))
						return;
					
					break;
				}

				// Otherwise, if we've scanned 'length' bytes then we're done.
				if (read == remaining)
					break;
				
				// Otherwise, add whole lines to the scanned count so we catch
				// lines that span the buffer boundary
				scanned   += backward ?  (read - lasti) : lasti;
				read_addr =  filepos + (backward ? -scanned : +scanned);
			}
		}
		
		/// <summary>
		/// Merge or replace 'm_line_index' with 'line_index'.
		/// Returns the delta position for how a row moves once 'line_index' has been added to 'm_line_index'</summary>
		private int MergeLineIndex(List<Range> line_index, long cache_range, long filepos, long fileend, bool replace)
		{
			int row_delta = 0;
			Range old_rng = m_line_index.Count != 0 ? new Range(m_line_index.First().m_begin, m_line_index.Last().m_end) : Range.Zero;
			Range new_rng =   line_index.Count != 0 ? new Range(  line_index.First().m_begin,   line_index.Last().m_end) : Range.Zero;
			
			// Replace the cached line index with 'line_index'
			if (replace)
			{
				// Use any range overlap to work out the row delta. 
				Range intersect = old_rng.Intersect(new_rng);
				
				// If the ranges overlap, we can search for the begin address of the intersect in both ranges to
				// get the row delta. If the don't overlap, the best we can do is say the direction.
				if (intersect.Empty)
					row_delta = intersect.m_begin == old_rng.m_end ? -line_index.Count : line_index.Count;
				else
				{
					int old_idx = LineIndex(m_line_index, intersect.m_begin);
					int new_idx = LineIndex(  line_index, intersect.m_begin);
					row_delta = new_idx - old_idx;
				}
				
				Log.Info("Replacing results. {0} lines about filepos {1}/{2}", line_index.Count, filepos, fileend);
				m_line_index = line_index;
				
				// Invalidate the cache since the cached data may now be different
				InvalidateCache();
			}
			else if (line_index.Count != 0)
			{
				// Append to the front and trim the end
				if (filepos < m_filepos)
				{
					// Make sure there's no overlap of rows between line_index and m_line_index
					while (m_line_index.Count != 0 && new_rng.Contains(m_line_index.First().m_begin))
					{
						m_line_index.RemoveAt(0);
						--row_delta;
					}
					
					Log.Info("Merging results front. {0} lines added. filepos {1}/{2}", line_index.Count, filepos, fileend);
					m_line_index.InsertRange(0, line_index);
					row_delta += line_index.Count;
					
					// Trim the tail
					Range line_range = LineStartIndexRange;
					int i,iend = m_line_index.Count;
					for (i = Math.Min(m_settings.LineCacheCount, iend); i-- != 0;)
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
					// Make sure there's no overlap of rows between line_index and m_line_index
					while (m_line_index.Count != 0 && new_rng.Contains(m_line_index.Last().m_begin))
						m_line_index.RemoveAt(m_line_index.Count - 1);
					
					Log.Info("Merging results tail. {0} lines added. filepos {1}/{2}", line_index.Count, filepos, fileend);
					m_line_index.AddRange(line_index);
					
					// Trim the head
					Range line_range = LineStartIndexRange;
					int i,iend = m_line_index.Count;
					for (i = Math.Max(0, iend - m_settings.LineCacheCount); i != iend; ++i)
					{
						if (line_range.m_end - m_line_index[i].m_end > cache_range) continue;
						--i;
						break;
					}
					m_line_index.RemoveRange(0, i+1);
					row_delta -= i+1;
				}
				
				// Invalidate the cache since the cached data may now be different
				InvalidateCache(new_rng);
			}
			
			// Save the new file position and length
			m_filepos = filepos;
			m_fileend = fileend;
			return row_delta;
		}
		
		/// <summary>Return a collection of the currently active filters</summary>
		private IEnumerable<Filter> ActiveFilters
		{
			get
			{
				if (!m_settings.FiltersEnabled) return Enumerable.Empty<Filter>();
				return from ft in Filter.Import(m_settings.FilterPatterns) where ft.Active select ft;
			}
		}

		/// <summary>Test 'text' against each filter to see if it returns a positive match</summary>
		/// <returns>Returns false if at least one filter returned no match</returns>
		private static bool PassesFilters(string text, IEnumerable<Filter> filters)
		{
			foreach (var f in filters)
				if (!f.IsMatch(text))
					return false;
			return true;
		}
		
		/// <summary>Auto detect the line end format. Must be called from the main thread</summary>
		private byte[] GuessRowDelimiter(string filepath, Encoding encoding)
		{
			// If the settings don't contain a row delimiter, then auto detect it from 'filepath'.
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
			// If we still don't know what the row delimiter is, guess,
			// but don't update 'm_row_delim' so that we try to guess again later
			return m_row_delim ?? encoding.GetBytes("\n");
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
		
		/// <summary>
		/// Returns the index in 'line_index' for the line that contains 'filepos'.
		/// Returns '-1' if 'filepos' is before the first range and 'line_index.Count'
		/// if 'filepos' is after the last range</summary>
		private int LineIndex(List<Range> line_index, long filepos)
		{
			// Careful, comparing filepos to line starts, if idx == line_index.Count
			// it could be in the last line, or after the last line.
			if (line_index.Count == 0) return 0;
			if (filepos >= line_index.Last().m_end) return line_index.Count;
			int idx = line_index.BinarySearch(line => Maths.Compare(line.m_begin, filepos));
			return idx >= 0 ? idx : ~idx - 1;
		}
	}
}



							//long scan_from = filepos;
							//// If there is some overlap with the range of the last line index with the new
							//// range,  do an incremental update to recentre the line index about filepos
							//incremental = line_index_range.Contains(filepos) && !reload;
							//if (incremental)
							//{
							//    // Reduce the byte range so we only scan in the direction we need
							//    scan_from = toward_end
							//        ? (rng.m_begin = line_starts_range.m_end)
							//        : (rng.m_end = line_starts_range.m_begin);
								
							//    Debug.Assert(rng.m_begin >= 0 && rng.m_end <= fileend && rng.m_begin <= rng.m_end);
							//    //bwd_range = scan_from - rng.m_begin;
							//    //fwd_range = rng.m_end - scan_from;
							//    //bwd_lines = toward_end ? last_line_count                   : 0;
							//    //fwd_lines = toward_end ? last_line_count - next_line_index : last_line_count;
							//}
							//else
							//{
							//long bwd_range = scan_from - rng.m_begin;
							//long fwd_range = rng.m_end - scan_from;
							
							
							
							//int  bwd_lines = 0; // The number of lines already cached before 'scan_from'
							//int  fwd_lines = 0; // The number of lines already cached after 'scan_from'
							//}