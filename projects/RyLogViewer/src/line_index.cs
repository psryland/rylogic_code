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
using pr.stream;
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
						"No lines detected within a {0} byte block.\r\n" +
						"\r\n" +
						"This might be due to one or more lines in the log file being larger than {0} bytes, or that the line endings are not being detected correctly.\r\n" +
						"If lines longer than {0} bytes are expected, increase the 'Maximum Line Length' option under settings.\r\n" +
						"Otherwise, check the settings under the 'Line Ending' menu and the 'Encoding' menu. You may have to specify these values explicitly rather than using automatic detection."
						, m_buf_size);
				}
			}
		}

		/// <summary>Returns the byte range of the complete file, last time we checked its length. Begin is always 0</summary>
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
				int b = Maths.Clamp(m_grid.FirstDisplayedScrollingRowIndex, 0, m_line_index.Count - 1);
				int e = Maths.Clamp(b + m_grid.DisplayedRowCount(true), 0, m_line_index.Count - 1);
				return new Range(m_line_index[b].Begin, m_line_index[e].End);
			}
		}

		/// <summary>Returns the bounding byte ranges of the currently selected rows.</summary>
		private IEnumerable<Range> SelectedRowRanges
		{
			get
			{
				int row_index = -1;
				Range rng = Range.Invalid;
				foreach (DataGridViewRow r in m_grid.SelectedRows.Cast<DataGridViewRow>().OrderBy(x => x.Index))
				{
					if (row_index + 1 != r.Index)
					{
						if (row_index != -1) yield return rng;
						rng = Range.Invalid;
					}
					rng.Encompase(m_line_index[r.Index]);
					row_index = r.Index;
				}
				if (!rng.Equals(Range.Invalid)) yield return rng;
			}
		}

		/// <summary>
		/// An issue number for the build.
		/// Builder threads abort as soon as possible when they notice this
		/// value is not equal to their startup issue number</summary>
		private static int m_build_issue;
		private bool ReloadInProgress
		{
			get
			{
				Debug.Assert(Misc.IsMainThread, "ReloadInProgress should only be set/tested in the main thread");
				return m_reload_in_progress_impl;
			}
			set
			{
				Debug.Assert(Misc.IsMainThread, "ReloadInProgress should only be set/tested in the main thread");
				m_reload_in_progress_impl = value;
			}
		}
		private bool m_reload_in_progress_impl;
		private static bool BuildCancelled(int build_issue)
		{
			return Interlocked.CompareExchange(ref m_build_issue, build_issue, build_issue) != build_issue;
		}

		/// <summary>Cause a currently running BuildLineIndex call to be cancelled</summary>
		private void CancelBuildLineIndex()
		{
			Log.Info(this, "build (id {0}) cancelled".Fmt(m_build_issue));
			Interlocked.Increment(ref m_build_issue);
			UpdateStatusProgress(1, 1);
		}

		/// <summary>The data required to build the line index asynchronously</summary>
		private class BLIData
		{
			/// <summary>The source of data to read from</summary>
			public readonly IFileSource file;

			/// <summary>The centre location in the file around which to load data from</summary>
			public long filepos;

			/// <summary>The end of the file used when generating this build</summary>
			public readonly long fileend;

			/// <summary>True if existing cached data should be refreshed</summary>
			public readonly bool reload;

			/// <summary>The async build issue number at the time the build was started</summary>
			public readonly int build_issue;

			/// <summary>The maximum length of any one line (in bytes)</summary>
			public readonly int max_line_length;

			/// <summary>The maximum amount of file data to load</summary>
			public readonly long file_buffer_size;

			/// <summary>The number of lines to cache</summary>
			public readonly int line_cache_count;

			/// <summary>The line index that 'filepos' would have in the current line index</summary>
			public readonly int filepos_line_index;

			/// <summary>The number of lines in the the current line index</summary>
			public readonly int line_index_count;

			/// <summary>True to ignore blank lines</summary>
			public readonly bool ignore_blanks;

			/// <summary>The row delimiter</summary>
			public readonly byte[] row_delim;

			/// <summary>The column delimiter</summary>
			public readonly byte[] col_delim;

			/// <summary>Text encoding format of the source data</summary>
			public readonly Encoding encoding;

			/// <summary>The byte range of known complete lines</summary>
			public readonly Range cached_whole_line_range;

			/// <summary>Line filters</summary>
			public readonly List<IFilter> filters;

			/// <summary>Row transforms</summary>
			public readonly List<Transform> transforms;
 
			/// <summary>The progress callback to use (note: called in worker thread context)</summary>
			public ProgressFunc progress;

			// 'file_source' should be an open new instance of the file source
			public BLIData(Main main, IFileSource file_source, long filepos_ = 0, bool reload_ = false, int build_issue_ = 0)
			{
				reload             = reload_;
				build_issue        = build_issue_;

				// Use a fixed file end so that additions to the file don't muck this
				// build up. Reducing the file size during this will probably cause an
				// exception but oh well...
				file               = file_source.NewInstance().Open();
				fileend            = file.Stream.Length;
				filepos            = Maths.Clamp(filepos_, 0, fileend);
				filepos_line_index = LineIndex(main.m_line_index, filepos);

				max_line_length    = main.m_settings.MaxLineLength;
				file_buffer_size   = main.m_bufsize;
				line_cache_count   = main.m_line_cache_count;
				line_index_count   = main.m_line_index.Count;
				ignore_blanks      = main.m_settings.IgnoreBlankLines;

				// Find the byte range of the file currently loaded
				cached_whole_line_range = main.LineStartIndexRange;

				// Get a copy of the filters to apply
				filters = new List<IFilter>();
				if (main.m_quick_filter_enabled)
				{
					filters.AddRange(main.m_highlights.ToList<IFilter>());
				}
				filters.AddRange(main.m_filters);
				if (main.m_quick_filter_enabled)
				{
					filters.Add(Filter.RejectAll); // Add a RejectAll so that non-highlighted means discard
				}
				
				// Get a copy of the transforms
				transforms = main.m_transforms.ToList();

				Debug.Assert(main.m_encoding != null);
				Debug.Assert(main.m_row_delim != null);
				bool certain, autodetect;

				// If the settings say auto detect encoding, and this is a reload
				// then detect the encoding, otherwise use what it is currently set to
				certain = false;
				autodetect = main.m_settings.Encoding.Length == 0;
				encoding = reload_ && autodetect
					? GuessEncoding(file_source, out certain)
					: (Encoding)main.m_encoding.Clone();
				if (certain) main.m_encoding = (Encoding)encoding.Clone();

				// If the settings say auto detect the row delimiters, and this is a reload
				// then detect them, otherwise use what is currently set
				certain = false;
				autodetect = main.m_settings.RowDelimiter.Length == 0;
				row_delim = reload_ && autodetect
					? GuessRowDelimiter(file_source, encoding, main.m_settings.MaxLineLength, out certain)
					: (byte[])main.m_row_delim.Clone();
				if (certain) main.m_row_delim = (byte[])row_delim.Clone();

				col_delim = (byte[])main.m_col_delim.Clone();
			}
		}

		/// <summary>
		/// Generates the line index centred around 'filepos'.
		/// If 'filepos' is within the byte range of 'm_line_index' then an incremental search for
		/// lines is done in the direction needed to re-centre the line list around 'filepos'.
		/// If 'reload' is true a full rescan of the file is done</summary>
		private void BuildLineIndex(long filepos, bool reload, Action on_success = null)
		{
			try
			{
				// No file open, nothing to do
				if (!FileOpen)
					return;

				// Incremental updates cannot supplant reloads
				if (ReloadInProgress && reload == false)
					return;

				// Cause any existing builds to stop by changing the issue number
				Interlocked.Increment(ref m_build_issue);
				ReloadInProgress = reload;
				//Log.Info(this, "build start request (id {0}, reload: {1})".Fmt(m_build_issue, reload));
				Log.Info(this, "build start request (id {0}, reload: {1})\n{2}".Fmt(m_build_issue, reload, string.Empty));//new StackTrace(0,true)));

				// Make copies of variables for thread safety
				var bli_data = new BLIData(this, m_file, filepos, reload, m_build_issue);

				// Set up callbacks that marshal to the main thread
				bli_data.progress = (scanned,length) =>
				{
					this.BeginInvoke(() => UpdateStatusProgress(scanned, length));
					return true;
				};

				// Find the new line indices in a background thread
				ThreadPool.QueueUserWorkItem(
					x => BuildLineIndexAsync(bli_data,
						(d, range, line_index, error) => this.BeginInvoke( // Marshal the results back to the main thread
							() => BuildLineIndexComplete(d, range, line_index, error, on_success))));
			}
			catch (Exception ex)
			{
				BuildLineIndexTerminatedWithError(ex);
				ReloadInProgress = false;
			}
		}

		/// <summary>The grunt work of building the new line list.</summary>
		private static void BuildLineIndexAsync(BLIData d, Action<BLIData, Range, List<Range>, Exception> on_complete)
		{
			// This method runs in a background thread
			try
			{
				Log.Info("BLIAsync", "build started. (id {0}, reload {1})".Fmt(d.build_issue, d.reload));
				if (BuildCancelled(d.build_issue)) return;
				using (d.file.Open())
				{
					// A temporary buffer for reading sections of the file
					byte[] buf = new byte[d.max_line_length];

					// Seek to the first line that starts immediately before filepos
					d.filepos = FindLineStart(d.file, d.filepos, d.fileend, d.row_delim, d.encoding, buf);
					if (BuildCancelled(d.build_issue)) return;

					// Determine the range of bytes to scan in each direction
					Range rng = BufferRange(d.filepos, d.fileend, d.file_buffer_size);
					bool scan_backward = d.fileend - d.filepos >= d.filepos - 0; // scan in the most bound direction first
					int bwd_lines = d.line_cache_count / 2;
					int fwd_lines = d.line_cache_count / 2;

					// Incremental loading - only load what isn't already cached
					if (!d.reload && !d.cached_whole_line_range.Empty)
						IncrementalLoadRange(d, ref rng, ref bwd_lines, ref fwd_lines, ref scan_backward);

					var line_index = new List<Range>();
					if (!rng.Empty)
					{
						// Caps the number of lines read for each of the forward and backward searches
						// Wrapped in an array to pass by ref
						int[] line_limit = {0};

						// Line index buffers for collecting the results
						List<Range> fwd_line_buf = new List<Range>();
						List<Range> bwd_line_buf = new List<Range>();
						List<Range>[] line_buf = {null};

						AddLineFunc add_line = (line, baddr, fend, bf, enc) =>
							{
								if (line.Empty && d.ignore_blanks)
									return true;

								// Test 'text' against each filter to see if it's included
								// Note: not caching this string because we want to read immediate data
								// from the file to pick up file changes.
								string text = d.encoding.GetString(buf, (int)line.Begin, (int)line.Count);
								if (!PassesFilters(text, d.filters))
									return true;

								// Convert the byte range to a file range
								line = line.Shift(baddr);
								Debug.Assert(new Range(0, d.fileend).Contains(line));
								line_buf[0].Add(line);
								Debug.Assert(line_buf[0].Count <= line_limit[0]);
								return (fwd_line_buf.Count + bwd_line_buf.Count) < line_limit[0];
							};

						// Callback for updating progress
						ProgressFunc progress = (scanned, length) =>
							{
								int numer = fwd_line_buf.Count + bwd_line_buf.Count, denom = line_limit[0];
								return d.progress(numer, denom) && !BuildCancelled(d.build_issue);
							};

						if (BuildCancelled(d.build_issue)) return;

						// Scan twice, starting in the direction of the smallest range so that any
						// unused cache space is used by the search in the other direction
						long scan_from = Maths.Clamp(d.filepos, rng.Begin, rng.End);
						for (int a = 0; a != 2; ++a, scan_backward = !scan_backward)
						{
							line_buf[0] = scan_backward ? bwd_line_buf : fwd_line_buf;
							line_limit[0] += scan_backward ? bwd_lines : fwd_lines;
							long range = scan_backward ? scan_from - rng.Begin : rng.End - scan_from;
							FindLines(d.file, scan_from, d.fileend, scan_backward, range, add_line, d.encoding, d.row_delim, buf, progress);
							if (BuildCancelled(d.build_issue)) return;
						}

						// Scanning backward adds lines to the line index in reverse order,
						// we need to flip the buffer over the range that was added.
						bwd_line_buf.Reverse();
						
						line_index.Capacity = bwd_line_buf.Count + fwd_line_buf.Count;
						line_index.AddRange(bwd_line_buf);
						line_index.AddRange(fwd_line_buf);
					}

					// Job done
					on_complete(d, rng, line_index, null);
				}
			}
			catch (Exception ex)
			{
				on_complete(d, Range.Zero, null, ex);
			}
		}

		/// <summary>Called when building the line index completes (success or failure)</summary>
		private void BuildLineIndexComplete(BLIData d, Range range, List<Range> line_index, Exception error, Action on_success)
		{
			// This method runs in the main thread, so if the build issue is the same at
			// the start of this method it can't be changed until after this function returns.
			if (BuildCancelled(d.build_issue))
				return;

			ReloadInProgress = false;
			UpdateStatusProgress(1, 1);

			// If an error occurred
			if (error != null)
			{
				if (error is OperationCanceledException)
				{
				}
				else if (error is FileNotFoundException)
				{
					SetStaticStatusMessage("Error reading {0}".Fmt(Path.GetFileName(d.file.Name)), Color.White, Color.DarkRed);
				}
				else
				{
					Log.Exception(this, error, "Exception ended BuildLineIndex() call");
					BuildLineIndexTerminatedWithError(error);
				}
			}

			// Otherwise, merge the results into the main cache
			else
			{
				// Merge the line index results
				int row_delta = MergeLineIndex(range, line_index, d.file_buffer_size, d.filepos, d.fileend, d.reload);

				// Ensure the grid is updated
				UpdateUI(row_delta);

				if (on_success != null)
					on_success();

				// On completion, check if the file has changed again and rerun if it has
				m_watch.CheckForChangedFiles();

				// Trigger a collect to free up memory, this also has the 
				// side effect of triggering a signing test of the exe because
				// that test is done in a destructor
				GC.Collect();
			}
		}

		/// <summary>Called when scanning ends with an error. Shows an error dialog and turns off file watching</summary>
		private void BuildLineIndexTerminatedWithError(Exception err)
		{
			// Disable watched files, so we don't get an endless blizzard of error messages
			if (m_settings.WatchEnabled)
			{
				EnableWatch(false);
				m_btn_watch.ShowHintBalloon(m_balloon, "File watching disabled due to error. ");
			}
			Log.Exception(this, err, "Failed to build index list for {0}".Fmt(m_file.Name));
			Misc.ShowErrorMessage(this, err, "Scanning the log file ended with an error.", "Scanning file terminated");
		}

		/// <summary>Determine the data range to load to incrementally adjust the line cache</summary>
		private static void IncrementalLoadRange(BLIData d, ref Range scan_range, ref int bwd_lines, ref int fwd_lines, ref bool scan_backward)
		{
			// If the filepos is left of the cache centre, try to extent in left direction first
			// If the scan range in that direction is empty, try extending at the other end. The
			// aim is to try to get d.line_index_count as close to d.line_cache_count as possible
			// without loading data that is already cached.

			// Find the available scan range on both sides of the currently cached range
			var Lrange = new Range(scan_range.Begin, d.cached_whole_line_range.Begin);
			var Rrange = new Range(d.cached_whole_line_range.End, scan_range.End);
			if (Lrange.Count <= 0 && Rrange.Count <= 0)
			{
				// This means the scanned range is within the currently cached range
				scan_range.Count = 0;
				bwd_lines = 0;
				fwd_lines = 0;
				return;
			}

			// Decide which side is preferred based on where the filepos is relative to the cache centre
			// and which range contains an valid area to be scanned.
			var scan_front = Lrange.Count > 0 && Rrange.Count > 0
				? d.filepos_line_index < d.line_cache_count / 2
				: Lrange.Count > 0;

			// If the new filepos is within the range already cached, then we'll have one
			// half of the lines already cached, and the other half partially cached. Limit
			// the number of lines scanned so that filepos ends up in the centre of the scanned lines
			if (scan_front)
			{
				scan_range = Lrange;
				if (d.filepos_line_index < 0)
				{
					scan_backward = true;
				}
				else
				{
					fwd_lines = 0;
					bwd_lines = Math.Max(0, d.line_cache_count / 2 - (d.filepos_line_index - 0));
				}
			}
			else
			{
				scan_range = Rrange;
				if (d.filepos_line_index >= d.line_cache_count)
				{
					scan_backward = false;
				}
				else
				{
					bwd_lines = 0;
					fwd_lines = Math.Max(0, d.line_cache_count / 2 - (d.line_index_count - d.filepos_line_index));
				}
			}

			// This happens when the scan range is larger than the cached_whole_line_range and the portion to scan
			// is on the far side of the cache centre (relative to filepos_line_index)
			if (bwd_lines == 0 && fwd_lines == 0)
				scan_range.Count = 0;
		}

		/// <summary>Buffer a maximum of 'count' bytes from 'stream' into 'buf' (note,
		/// automatically capped at buf.Length). If 'backward' is true the stream is seeked
		/// backward from the current position before reading and then seeked backward again
		/// after reading so that conceptually the file position moves in the direction of
		/// the read. Returns the number of bytes buffered in 'buf'</summary>
		private static int Buffer(IFileSource file, long count, long fileend, Encoding encoding, bool backward, byte[] buf, out bool eof)
		{
			Debug.Assert(count >= 0);
			long pos = file.Stream.Position;
			eof = backward ? pos == 0 : pos == fileend;

			// The number of bytes to buffer
			count = Math.Min(count, buf.Length);
			count = Math.Min(count, backward ? file.Stream.Position : fileend - file.Stream.Position);
			if (count == 0) return 0;
			Debug.Assert(count > 0);
			
			// Set the file position to the location to read from
			if (backward) file.Stream.Seek(-count, SeekOrigin.Current);
			pos = file.Stream.Position;
			eof = backward ? pos == 0 : pos + count == fileend;

			// Move to the start of a character
			if (encoding.Equals(Encoding.ASCII)) { }
			else if (encoding.Equals(Encoding.Unicode) || encoding.Equals(Encoding.BigEndianUnicode))
			{
				// Skip the byte order mark (BOM)
				if (pos == 0 && file.Stream.ReadByte() != -1 && file.Stream.ReadByte() != -1) { }

				// Ensure a 16-bit word boundary
				if ((file.Stream.Position % 2) == 1)
					file.Stream.ReadByte();
			}
			else if (encoding.Equals(Encoding.UTF8))
			{
				// Some programs store a byte order mark (BOM) at the start of UTF-8 text files.
				// These bytes are 0xEF, 0xBB, 0xBF, so ignore them if present
				if (pos == 0 && file.Stream.ReadByte() == 0xEF && file.Stream.ReadByte() == 0xBB && file.Stream.ReadByte() == 0xBF) { }
				else file.Stream.Seek(pos, SeekOrigin.Begin);

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
				for (; ; )
				{
					int b = file.Stream.ReadByte();
					if (b == -1) return 0; // End of file
					if ((b & encode_mask) == encode_char) continue; // If 'b' is a byte halfway through an encoded char, keep reading

					// Otherwise it's an ascii char or the start of a multibyte char
					// save the byte we read, and read the remainder of 'count'
					file.Stream.Seek(-1, SeekOrigin.Current);
					break;
				}
			}

			// Buffer file data
			Debug.Assert(count >= file.Stream.Position - pos);
			count -= file.Stream.Position - pos;
			int read = file.Stream.Read(buf, 0, (int)Math.Min(count, buf.Length));
			if (read != count) throw new IOException("failed to read file over range [{0},{1}) ({2} bytes). Read {3}/{2} bytes.".Fmt(pos, pos + count, count, read));
			if (backward) file.Stream.Seek(-read, SeekOrigin.Current);
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
				for (int j = 1; is_match && j != delim.Length; ++j) is_match = buf[i + j] == delim[j];
				if (!is_match) continue;

				// 'i' now points to the start of the delimiter,
				// shift it forward to one past the delimiter.
				i += delim.Length;
				break;
			}
			return i;
		}

		/// <summary>Seek to the first line that starts immediately before filepos</summary>
		private static long FindLineStart(IFileSource file, long filepos, long fileend, byte[] row_delim, Encoding encoding, byte[] buf)
		{
			file.Stream.Seek(filepos, SeekOrigin.Begin);

			// Read a block into 'buf'
			bool eof;
			int read = Buffer(file, buf.Length, fileend, encoding, true, buf, out eof);
			if (read == 0) return 0; // assume the first character in the file is the start of a line

			// Scan for a line start
			int idx = FindNextDelim(buf, read - 1, read, row_delim, true);
			if (idx != -1) return file.Stream.Position + idx; // found
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
		private static void FindLines(IFileSource file, long filepos, long fileend, bool backward, long length, AddLineFunc add_line, Encoding encoding, byte[] row_delim, byte[] buf, ProgressFunc progress)
		{
			long scanned = 0, read_addr = filepos;
			for (;;)
			{
				// Progress update
				if (progress != null && !progress(scanned, length)) return;

				// Seek to the start position
				file.Stream.Seek(read_addr, SeekOrigin.Begin);

				// Buffer the contents of the file in 'buf'.
				long remaining = length - scanned; bool eof;
				int read = Buffer(file, remaining, fileend, encoding, backward, buf, out eof);
				if (read == 0) break;

				// Set iterator limits.
				// 'i' is where to start scanning from
				// 'iend' is the end of the range to scan
				// 'ilast' is the start of the last line found
				// 'base_addr' is the file offset from which buf was read
				int i = backward ? read - 1 : 0;
				int iend = backward ? -1 : read;
				int lasti = backward ? read : 0;
				long base_addr = backward ? file.Stream.Position : file.Stream.Position - read;

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
					long scan_increment = backward ? (read - lasti) : lasti;
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

					// ReSharper disable RedundantJumpStatement
					// Pass the detected line to the callback
					if (!add_line(line, base_addr, fileend, buf, encoding))
						return;
					// ReSharper restore RedundantJumpStatement

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
			if (replace || scan_range.Contains(old_rng))
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
					int new_idx = LineIndex(line_index, intersect.Begin);
					row_delta = new_idx - old_idx;
				}

				Log.Info(this, "Replacing results. Results contain {0} lines about filepos {1}/{2}".Fmt(line_index.Count, filepos, fileend));
				m_line_index = line_index;

				// Invalidate the cache since the cached data may now be different
				InvalidateCache();
			}

			// Merge 'line_index' with the existing m_line_index
			else
			{
				// Append to the front and trim the end
				if (scan_range.End < old_rng.End)
				{
					Log.Info(this, "Merging results front. Results contain {0} lines. filepos {1}/{2}".Fmt(line_index.Count, filepos, fileend));

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
					int i, iend = m_line_index.Count;
					for (i = Math.Min(m_settings.LineCacheCount, iend); i-- != 0; )
					{
						if (m_line_index[i].Begin - line_range.Begin > cache_range) continue;
						++i;
						m_line_index.RemoveRange(i, iend - i);
						break;
					}
				}
				// Or append to the back and trim the start
				else if (scan_range.Begin > old_rng.Begin)
				{
					Log.Info(this, "Merging results tail. Results contain {0} lines. filepos {1}/{2}".Fmt(line_index.Count, filepos, fileend));

					// Make sure there's no overlap of rows between 'scan_range' and 'm_line_index'
					while (m_line_index.Count != 0 && scan_range.Contains(m_line_index.Last().Begin))
						m_line_index.RemoveAt(m_line_index.Count - 1);

					m_line_index.AddRange(line_index);

					// Trim the head
					Range line_range = LineStartIndexRange;
					int i, iend = m_line_index.Count;
					for (i = Math.Max(0, iend - m_settings.LineCacheCount); i != iend; ++i)
					{
						if (line_range.End - m_line_index[i].End > cache_range) continue;
						--i;
						m_line_index.RemoveRange(0, i + 1);
						row_delta -= i + 1;
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
		private static bool PassesFilters(string text, IEnumerable<IFilter> filters)
		{
			foreach (var ft in filters)
			{
				if (!ft.IsMatch(text)) continue;
				return ft.IfMatch == EIfMatch.Keep;
			}
			return true;
		}

		/// <summary>Auto detect the line end format. Must be called from the main thread. 'certain' is true if the returned row delimiter isn't just a guess</summary>
		private static byte[] GuessRowDelimiter(IFileSource file, Encoding encoding, int max_line_length, out bool certain)
		{
			try
			{
				long pos = file.Stream.Position;
				using (Scope.Create(() => file.Stream.Position = 0, () => file.Stream.Position = pos))
				using (var r = new StreamReader(new UncloseableStream(file.Stream)))
				{
					// Read the maximum line length from the file
					var buf = new char[max_line_length + 1];
					int read = r.ReadBlock(buf, 0, max_line_length);
					if (read != 0)
					{
						// Look for the first newline/carrage return character
						int i = Array.FindIndex(buf, 0, read, c => c == '\n' || c == '\r');
						if (i != -1)
						{
							// Match \r\n, \r, or \n
							// Don't match more than this because they could represent consecutive blank lines
							certain = true;
							return buf[i] == '\r' && buf[i+1] == '\n'
								? encoding.GetBytes(buf, i, 2)
								: encoding.GetBytes(buf, i, 1);
						}
					}
				}
			}
			catch (FileNotFoundException) { } // Ignore failures, the file may not have any data yet

			certain = false;
			return encoding.GetBytes("\n");
		}

		/// <summary>Guess the text file encoding for the current file. 'certain' is true if the returned encoding isn't just a guess</summary>
		private static Encoding GuessEncoding(IFileSource file, out bool certain)
		{
			try
			{
				long pos = file.Stream.Position;
				using (Scope.Create(() => file.Stream.Position = 0, () => file.Stream.Position = pos))
				{
					Encoding encoding;

					var buf = new byte[4];
					int read = file.Stream.Read(buf, 0, buf.Length);

					// Look for a BOM
					if (read >= 3 && buf[0] == 0xEF && buf[1] == 0xBB && buf[2] == 0xBF)
						encoding = Encoding.UTF8;
					else if (read >= 2 && buf[0] == 0xFE && buf[1] == 0xFF)
						encoding = Encoding.BigEndianUnicode;
					else if (read >= 2 && buf[0] == 0xFF && buf[1] == 0xFE)
						encoding = Encoding.Unicode;
					else // If no valid bomb is found, assume UTF-8 as that is a superset of ASCII
						encoding = Encoding.UTF8;
					certain = true;
					return encoding;
				}
			}
			catch (FileNotFoundException) { } // Ignore failures, the file may not have any data yet

			certain = false;
			return Encoding.UTF8;
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
