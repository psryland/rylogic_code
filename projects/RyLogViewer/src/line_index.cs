using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
		private class NoLinesException :Exception
		{
			private readonly int m_buf_size;
			public NoLinesException(int buf_size) { m_buf_size = buf_size; }
			public override string Message
			{
				get
				{
					return string.Format(
						"No lines detected within a {0} byte block.\r\n"+
						"\r\n"+
						"This might be due to a line in the log file being larger than {0} bytes, or that the line endings are not being detected correctly.\r\n"+
						"If lines longer than {0} bytes are expected, increase the 'Maximum Line Length' option under settings.\r\n"+
						"Otherwise, check the line ending and text encoding settings. You may have to specify these values explicitly rather than using automatic detection."
						,m_buf_size);
				}
			}
		}
		
		/// <summary>Returns a file stream for 'filepath' opened with R/W sharing</summary>
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
			var rng = new Range();
			long ovr, hbuf = buf_size / 2;
			
			// Start with the range that has filepos in the middle
			rng.Begin = filepos - hbuf;
			rng.End   = filepos + hbuf;
			
			// Any overflow, add to the other range
			if ((ovr = 0     - rng.Begin) > 0) { rng.Begin += ovr; rng.End   += ovr; }
			if ((ovr = rng.End - fileend) > 0) { rng.End   -= ovr; rng.Begin -= ovr; }
			if ((ovr = 0     - rng.Begin) > 0) { rng.Begin += ovr; }
			
			Debug.Assert(rng.Begin >= 0 && rng.End <= fileend && rng.Begin <= rng.End);
			return rng;
		}

		/// <summary>Returns the full byte range currently represented by 'm_line_index'</summary>
		private Range LineIndexRange
		{
			get { return m_line_index.Count != 0 ? new Range(m_line_index.First().Begin, m_line_index.Last().End) : Range.Zero; }
		}

		/// <summary>
		/// Returns the byte range of the file currently covered by 'm_line_index'
		/// Note: the range is between starts of lines, not the full range. This is because
		/// this is the only range we know is complete and doesn't contain partial lines</summary>
		private Range LineStartIndexRange
		{
			get { return m_line_index.Count != 0 ? new Range(m_line_index.First().Begin, m_line_index.Last().Begin) : Range.Zero; }
		}

		/// <summary>Returns the byte range of the currently displayed rows</summary>
		private Range DisplayedRowsRange
		{
			get
			{
				if (m_line_index.Count == 0) return Range.Zero;
				int b = Maths.Clamp(m_grid.FirstDisplayedScrollingRowIndex ,0 ,m_line_index.Count-1);
				int e = Maths.Clamp(b + m_grid.DisplayedRowCount(true)     ,0 ,m_line_index.Count-1);
				return new Range(m_line_index[b].Begin, m_line_index[e].End);
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
		private static int m_build_issue;
		private bool m_reload_in_progress; // Should only be set/tested in the main thread
		private static bool BuildCancelled(int build_issue)
		{
			return Interlocked.CompareExchange(ref m_build_issue, build_issue, build_issue) != build_issue;
		}

		/// <summary>Cause a currently running BuildLineIndex call to be cancelled</summary>
		private void CancelBuildLineIndex() // will be used when caching numbers of lines, not byte range
		{
			Log.Info(this, "build (id {0}) cancelled".Fmt(m_build_issue));
			Interlocked.Increment(ref m_build_issue);
			UpdateStatusProgress(1,1);
		}

		/// <summary>
		/// Generates the line index centred around 'filepos'.
		/// If 'filepos' is within the byte range of 'm_line_index' then an incremental search for
		/// lines is done in the direction needed to re-centre the line list around 'filepos'.
		/// If 'reload' is true a full rescan of the file is done</summary>
		private void BuildLineIndex(long filepos, bool reload, Action on_success = null)
		{
			Exception err;
			try
			{
				// No file open, nothing to do
				if (!FileOpen)
					return;
				
				// Incremental updates cannot supplant reloads
				if (m_reload_in_progress && reload == false)
					return;
				
				// Cause any existing builds to stop by changing the issue number
				Interlocked.Increment(ref m_build_issue);
				m_reload_in_progress = reload;
				Log.Info(this, "build start request (id {0})".Fmt(m_build_issue));
				//Log.Info(this, "build start request (id {0})\n{1}", m_build_issue, Util.StackTrace(0,9));
			
				// Find the byte range of the file currently loaded
				Range line_start_range = LineStartIndexRange;
			
				// If this is not a 'reload', guess the encoding
				Encoding encoding = reload
					? (Encoding)GuessEncoding(m_filepath).Clone()
					: (Encoding)m_encoding.Clone();
			
				// If this is not a 'reload', guess the row_delimiters
				byte[] row_delim = reload || m_row_delim == null
					? (byte[])GuessRowDelimiter(m_filepath, encoding).Clone()
					: (byte[])m_row_delim.Clone();
				
				List<Filter> filters = m_filters.ToList();
			
				long max_line_length  = m_settings.MaxLineLength;
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
							Log.Info(this, "build started. (id {0})".Fmt(build_issue));
							if (BuildCancelled(build_issue)) return;
							using (var file = LoadFile(m_filepath))
							{
								// A temporary buffer for reading sections of the file
								byte[] buf = new byte[max_line_length];
						
								// Use a fixed file end so that additions to the file don't muck this
								// build up. Reducing the file size during this will probably cause an
								// exception but oh well...
								long fileend = file.Length;
								filepos = Maths.Clamp(filepos, 0, fileend);
							
								// Seek to the first line that starts immediately before filepos
								filepos = FindLineStart(file, filepos, fileend, row_delim, encoding, buf);
								if (BuildCancelled(build_issue)) return;
							
								// Determine the range of bytes to scan in each direction
								Range rng = BufferRange(filepos, fileend, bufsize);
								bool scan_backward = fileend - filepos >= filepos - 0; // scan in the most bound direction first
								
								// The number of lines to cache in the forward and backward directions
								int bwd_lines = line_cache_count/2; 
								int fwd_lines = line_cache_count/2;
							
								// Incremental loading
								reload |= filepos == last_filepos;
								if (!reload)
								{
									// True if 'filepos' has moved toward the start of the file
									bool toward_start = filepos < last_filepos;
								
									// If the range overlaps the currently cached range (and this isn't a reload)
									// reduce the range to only scan the range that isn't cached
									bool incremental;
									if (toward_start) { incremental = line_start_range.Begin < rng.End;   if (incremental) rng.End   = Math.Max(rng.Begin, line_start_range.Begin); }
									else              { incremental = line_start_range.End   > rng.Begin; if (incremental) rng.Begin = Math.Min(rng.End,   line_start_range.End); }
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
												if (bwd_lines == 0) rng.Begin = rng.End;
											}
											else
											{
												bwd_lines = 0;
												fwd_lines -= Maths.Clamp(last_line_count - next_line_index, 0, line_cache_count/2);
												if (fwd_lines == 0) rng.End = rng.Begin;
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
							
								// Line index buffers for collecting the results
								List<Range> fwd_line_buf = new List<Range>();
								List<Range> bwd_line_buf = new List<Range>();
								List<Range>[] line_buf = {null};
							
								// Callback function that adds lines to 'line_index'
								AddLineFunc add_line = (line, baddr, fend, bf, enc) =>
									{
										if (line.Empty && ignore_blanks)
											return true;
										
										// Test 'text' against each filter to see if it's included
										// Note: not caching this string because we want to read immediate data
										// from the file to pick up file changes.
										string text = encoding.GetString(buf, (int)line.Begin, (int)line.Count);
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
										Action update_progress_bar = () => UpdateStatusProgress(scanned, length);
										BeginInvoke(update_progress_bar);
										return !BuildCancelled(build_issue);
									};
								
								if (BuildCancelled(build_issue)) return;
								
								// Scan twice, starting in the direction of the smallest range so that any
								// unused cache space is used by the search in the other direction
								long scan_from = Maths.Clamp(filepos, rng.Begin, rng.End);
								for (int a = 0; a != 2; ++a, scan_backward = !scan_backward)
								{
									line_buf[0]    = scan_backward ? bwd_line_buf : fwd_line_buf;
									line_limit[0] += scan_backward ? bwd_lines    : fwd_lines;    // Push out the limit of allowed lines
									long range     = scan_backward ? scan_from - rng.Begin : rng.End - scan_from;
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
									// This lambda runs in the main thread, so if the build issue is the same at
									// the start of this method it can't be changed until after this function returns.
									if (BuildCancelled(build_issue)) return;
									
									// Merge the line index results
									int row_delta = MergeLineIndex(rng, line_index, bufsize, filepos, fileend, reload);
									
									// Ensure the grid is updated
									UpdateUI(row_delta);
									
									// On completion, check if the file has changed again and rerun if it has
									m_watch.CheckForChangedFiles();
									
									if (on_success != null) on_success();
									m_reload_in_progress = false;
									
									// Trigger a collect to free up memory, this also has the 
									// side effect of triggering a signing test of the exe because
									// that test is done in a destructor
									GC.Collect();
								};
								BeginInvoke(MergeLineIndexDelegate);
							}
						}
						catch (OperationCanceledException) {}
						catch (FileNotFoundException)
						{
							Action report_error = () => SetTransientStatusMessage(string.Format("Error reading {0}", Path.GetFileName(m_filepath)), Color.White, Color.DarkRed);
							BeginInvoke(report_error);
						}
						catch (Exception ex)
						{
							Log.Exception(this, ex, "Exception ended BuildLineIndex() call");
							Action report_error = () => Misc.ShowErrorMessage(this, ex, "Scanning the log file ended with an error.", "Scanning file terminated");
							BeginInvoke(report_error);
						}
						finally
						{
							Action update_progress_bar = () => UpdateStatusProgress(1,1);
							BeginInvoke(update_progress_bar);
							m_reload_in_progress = false;
						}
					}, m_build_issue);
				return;
			}
			catch (Exception ex) { err = ex; }
			m_reload_in_progress = false;
			Log.Exception(this, err, "Failed to build index list for {0}".Fmt(m_filepath));
			Misc.ShowErrorMessage(this, err, "Scanning the log file ended with an error.", "Scanning file terminated");
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
					// save the byte we read, and read the remainder of 'count'
					file.Seek(-1, SeekOrigin.Current);
					break;
				}
			}
			
			// Buffer file data
			count -= (int)(file.Position - pos);
			int read = file.Read(buf, 0, count);
			if (read != count) throw new IOException("failed to read file over range [{0},{1}) ({2} bytes). Read {3}/{2} bytes.".Fmt(pos,pos+count,count,read));
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
			
			// Read a block into 'buf'
			bool eof;
			int read = Buffer(file, buf.Length, fileend, encoding, true, buf, out eof);
			if (read == 0) return 0; // assume the first character in the file is the start of a line
			
			// Scan for a line start
			int idx = FindNextDelim(buf, read - 1, read, row_delim, true);
			if (idx != -1) return file.Position + idx; // found
			if (filepos == read) return 0; // assume the first character in the file is the start of a line
			throw new NoLinesException(read);
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

		/// <summary>Scan the file from 'filepos' adding whole lines to 'line_index' until 'length' bytes have been read or 'add_line' returns false</summary>
		/// <param name="file">The file to scan</param>
		/// <param name="filepos">The position in the file to start scanning from</param>
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
			long scanned = 0, read_addr = filepos;
			for (;;)
			{
				// Progress update
				if (progress != null && !progress(scanned, length)) return;
				
				// Seek to the start position
				file.Seek(read_addr, SeekOrigin.Begin);
				
				// Buffer the contents of the file in 'buf'.
				int remaining = (int)(length - scanned); bool eof;
				int read = Buffer(file, remaining, fileend, encoding, backward, buf, out eof);
				if (read == 0) break;
				
				// Set iterator limits.
				// 'i' is where to start scanning from
				// 'iend' is the end of the range to scan
				// 'ilast' is the start of the last line found
				// 'base_addr' is the file offset from which buf was read
				int i          = backward ? read - 1 : 0;
				int iend       = backward ? -1 : read;
				int lasti      = backward ? read : 0;
				long base_addr = backward ? file.Position : file.Position - read;
				
				// If we're searching backwards and 'i' is at the end of a line,
				// we don't want to count that as the first found line so adjust 'i'.
				// If not however, then 'i' is partway through a line or at the end
				// of a file without a row delimiter at the end and we want to include
				// this (possibly partial) line.
				if (backward && IsRowDelim(buf, read - row_delim.Length, row_delim))
					i -= row_delim.Length;
				
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
				
				// From lasti to the end (or start in the backwards case) of the buffer represents
				// a (possibly partial) line. If we read a full buffer load last time, then we'll go
				// round again trying to read another buffer load, starting from lasti.
				if (read == buf.Length)
				{
					// Make sure we're always making progress
					long scan_increment = backward ?  (read - lasti) : lasti;
					if (scan_increment == 0) // No lines detected in this block
						throw new NoLinesException(read);
				
					scanned += scan_increment;
					read_addr = filepos + (backward ? -scanned : +scanned);
				}
				// Otherwise, we're read to the end (or start) of the file, or to the limit 'length'.
				// What's left in the buffer may be a partial line.
				else
				{
					// 'i' points to 'iend',
					// 'lasti' points to the start of the last line we found
					// Get the range in buf containing the line
					Range line = backward
						? new Range(i + 1, lasti - row_delim.Length)
						: new Range(lasti, i - (IsRowDelim(buf, i - row_delim.Length, row_delim) ? row_delim.Length : 0));
					
					// Pass the detected line to the callback
					if (!add_line(line, base_addr, fileend, buf, encoding))
						return;
					
					break;
				}
			}
		}
		
		/// <summary>
		/// Merge or replace 'm_line_index' with 'line_index'.
		/// Returns the delta position for how a row moves once 'line_index' has been added to 'm_line_index'</summary>
		private int MergeLineIndex(Range scan_range, List<Range> line_index, long cache_range, long filepos, long fileend, bool replace)
		{
			int row_delta = 0;
			Range old_rng = m_line_index.Count != 0 ? new Range(m_line_index.First().Begin, m_line_index.Last().End) : Range.Zero;
			Range new_rng =   line_index.Count != 0 ? new Range(  line_index.First().Begin,   line_index.Last().End) : Range.Zero;
			
			// Replace the cached line index with 'line_index'
			if (replace)
			{
				// Use any range overlap to work out the row delta. 
				Range intersect = old_rng.Intersect(new_rng);
				
				// If the ranges overlap, we can search for the begin address of the intersect in both ranges to
				// get the row delta. If the don't overlap, the best we can do is say the direction.
				if (intersect.Empty)
					row_delta = intersect.Begin == old_rng.End ? -line_index.Count : line_index.Count;
				else
				{
					int old_idx = LineIndex(m_line_index, intersect.Begin);
					int new_idx = LineIndex(  line_index, intersect.Begin);
					row_delta = new_idx - old_idx;
				}
				
				Log.Info(this, "Replacing results. {0} lines about filepos {1}/{2}".Fmt(line_index.Count, filepos, fileend));
				m_line_index = line_index;
				
				// Invalidate the cache since the cached data may now be different
				InvalidateCache();
			}
			else
			{
				// Append to the front and trim the end
				if (filepos < m_filepos)
				{
					Log.Info(this, "Merging results front. {0} lines added. filepos {1}/{2}".Fmt(line_index.Count, filepos, fileend));
					
					// Make sure there's no overlap of rows between 'scan_range' and m_line_index
					while (m_line_index.Count != 0 && scan_range.Contains(m_line_index.First().Begin))
					{
						m_line_index.RemoveAt(0);
						--row_delta;
					}
					
					m_line_index.InsertRange(0, line_index);
					row_delta += line_index.Count;
					
					// Trim the tail
					Range line_range = LineStartIndexRange;
					int i,iend = m_line_index.Count;
					for (i = Math.Min(m_settings.LineCacheCount, iend); i-- != 0;)
					{
						if (m_line_index[i].Begin - line_range.Begin > cache_range) continue;
						++i;
						m_line_index.RemoveRange(i, iend-i);
						break;
					}
				}
				// Or append to the back and trim the start
				else
				{
					Log.Info(this, "Merging results tail. {0} lines added. filepos {1}/{2}".Fmt(line_index.Count, filepos, fileend));
					
					// Make sure there's no overlap of rows between 'scan_range' and 'm_line_index'
					while (m_line_index.Count != 0 && scan_range.Contains(m_line_index.Last().Begin))
						m_line_index.RemoveAt(m_line_index.Count - 1);
					
					m_line_index.AddRange(line_index);
					
					// Trim the head
					Range line_range = LineStartIndexRange;
					int i,iend = m_line_index.Count;
					for (i = Math.Max(0, iend - m_settings.LineCacheCount); i != iend; ++i)
					{
						if (line_range.End - m_line_index[i].End > cache_range) continue;
						--i;
						m_line_index.RemoveRange(0, i+1);
						row_delta -= i+1;
						break;
					}
				}
				
				// Invalidate the cache since the cached data may now be different
				InvalidateCache(scan_range);
			}
			
			// Save the new file position and length
			m_filepos = filepos;
			m_fileend = fileend;
			return row_delta;
		}
		
		/// <summary>Tests 'text' against each of the filters in 'filters'</summary>
		private static bool PassesFilters(string text, IEnumerable<Filter> filters)
		{
			foreach (var ft in filters)
			{
				if (!ft.IsMatch(text)) continue;
				return ft.IfMatch == Filter.EIfMatch.Keep;
			}
			return true;
		}

		/// <summary>Auto detect the line end format. Must be called from the main thread</summary>
		private byte[] GuessRowDelimiter(string filepath, Encoding encoding)
		{
			// If the settings don't contain a row delimiter, then auto detect it from 'filepath'.
			if (m_settings.RowDelimiter.Length == 0)
			{
				try
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
				catch (FileNotFoundException) {}
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
				try
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
				catch (FileNotFoundException) { m_encoding = Encoding.UTF8; }
			}
			return m_encoding;
		}
		
		/// <summary>Returns true if buf[index] matches row_delim. Handles out of bounds</summary>
		private static bool IsRowDelim(byte[] buf, int index, byte[] row_delim)
		{
			if (index < 0 || index + row_delim.Length > buf.Length)
				return false;
			
			for (int i = 0; i != row_delim.Length; ++i)
				if (buf[index + i] != row_delim[i])
					return false;
			
			return true;
		}

		/// <summary>
		/// Returns the index in 'line_index' for the line that contains 'filepos'.
		/// Returns '-1' if 'filepos' is before the first range and 'line_index.Count'
		/// if 'filepos' is after the last range</summary>
		private static int LineIndex(List<Range> line_index, long filepos)
		{
			// Careful, comparing filepos to line starts, if idx == line_index.Count
			// it could be in the last line, or after the last line.
			if (line_index.Count == 0) return 0;
			if (filepos >= line_index.Last().End) return line_index.Count;
			int idx = line_index.BinarySearch(line => Maths.Compare(line.Begin, filepos));
			return idx >= 0 ? idx : ~idx - 1;
		}
	}
}
