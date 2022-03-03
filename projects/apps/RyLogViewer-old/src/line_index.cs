using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;
using Rylogic.Streams;
using Rylogic.Utility;

namespace RyLogViewer
{
	public partial class Main
	{
		/// <summary>The byte ranges of lines in a file</summary>
		private readonly List<RangeI> m_line_index = new List<RangeI>();

		/// <summary>Returns the byte range of the complete file, last time we checked its length. Begin is always 0</summary>
		private RangeI FileByteRange
		{
			get { return new RangeI(0, m_fileend); }
		}

		/// <summary>Returns the byte range within the file that would be buffered given 'filepos'</summary>
		private static RangeI CalcBufferRange(long filepos, long fileend, long buf_size)
		{
			var rng = new RangeI();
			long ovr, hbuf = buf_size / 2;

			// Start with the range that has 'filepos' in the middle
			rng.Beg = filepos - hbuf;
			rng.End   = filepos + hbuf;

			// Any overflow, add to the other range
			if ((ovr = 0       - rng.Beg) > 0) { rng.Beg += ovr; rng.End   += ovr; }
			if ((ovr = rng.End - fileend) > 0) { rng.End   -= ovr; rng.Beg -= ovr; }
			if ((ovr = 0       - rng.Beg) > 0) { rng.Beg += ovr; }

			Debug.Assert(rng.Beg >= 0 && rng.End <= fileend && rng.Beg <= rng.End);
			return rng;
		}

		/// <summary>Returns the number of lines that would be each side of the cache centre for 'num_lines'</summary>
		private static RangeI CalcLineRange(long num_lines)
		{
			 var bwd_lines = Math.Max(1, num_lines / 2);
			 var fwd_lines = Math.Max(2, num_lines - bwd_lines);
			return new RangeI(bwd_lines, fwd_lines);
		}

		/// <summary>Returns the full byte range currently represented by 'm_line_index'</summary>
		private RangeI LineIndexRange
		{
			get { return m_line_index.Count != 0 ? new RangeI(m_line_index.Front().Beg, m_line_index.Back().End) : RangeI.Zero; }
		}

		/// <summary>
		/// Returns the byte range of the file currently covered by 'm_line_index'
		/// Note: the range is between starts of lines, not the full range. This is because
		/// this is the only range we know is complete and doesn't contain partial lines</summary>
		private RangeI LineStartIndexRange
		{
			get { return m_line_index.Count != 0 ? new RangeI(m_line_index.Front().Beg, m_line_index.Back().Beg) : RangeI.Zero; }
		}

		/// <summary>Returns the byte range of the currently displayed rows</summary>
		private RangeI DisplayedRowsRange
		{
			get
			{
				if (m_line_index.Count == 0) return RangeI.Zero;
				int b = Math_.Clamp(m_grid.FirstDisplayedScrollingRowIndex, 0, m_line_index.Count - 1);
				int e = Math_.Clamp(b + m_grid.DisplayedRowCount(true), 0, m_line_index.Count - 1);
				return new RangeI(m_line_index[b].Beg, m_line_index[e].End);
			}
		}

		/// <summary>Returns the bounding byte ranges of the currently selected rows.</summary>
		private IEnumerable<RangeI> SelectedRowRanges
		{
			get
			{
				var row_index = -1;
				var rng = RangeI.Invalid;
				foreach (var r in m_grid.SelectedRows().OrderBy(x => x.Index))
				{
					if (row_index + 1 != r.Index)
					{
						if (row_index != -1) yield return rng;
						rng = RangeI.Invalid;
					}
					rng.Grow(m_line_index[r.Index]);
					row_index = r.Index;
				}
				if (!rng.Equals(RangeI.Invalid)) yield return rng;
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
			Log.Write(ELogLevel.Info, $"build (id {m_build_issue}) cancelled");
			Interlocked.Increment(ref m_build_issue);
			UpdateStatusProgress(1, 1);
		}

		/// <summary>The data required to build the line index asynchronously</summary>
		private class BLIData
		{
			// 'file_source' should be an open new instance of the file source
			public BLIData(Main main, IFileSource file_source, long filepos_ = 0, long fileend_ = long.MaxValue, bool reload_ = false, int build_issue_ = 0)
			{
				build_issue = build_issue_;

				// Use a fixed file end so that additions to the file don't muck this
				// build up. Reducing the file size during this will probably cause an
				// exception but oh well...
				file               = file_source.NewInstance().Open();
				fileend            = Math.Min(file.Stream.Length, fileend_);
				filepos            = Math_.Clamp(filepos_, 0, fileend);
				filepos_line_index = LineIndex(main.m_line_index, filepos);

				max_line_length    = main.Settings.MaxLineLength;
				file_buffer_size   = main.m_bufsize;
				line_cache_count   = main.m_line_cache_count;
				line_index_count   = main.m_line_index.Count;
				ignore_blanks      = main.Settings.IgnoreBlankLines;

				// We have to reload if 'filepos' moves outside the current cached range because
				// we don't know how many lines there are between the new 'filepos' and the nearest
				// edge of the cached data.
				reload = reload_ || !filepos_line_index.Within(0, line_cache_count);

				// Find the byte range of the file currently loaded
				cached_whole_line_range = main.LineStartIndexRange;

				// Get a copy of the filters to apply
				filters = new List<IFilter>();
				filters.AddRange(main.m_filters);
				if (main.m_quick_filter_enabled)
				{
					filters.AddRange(main.m_highlights.ToList<IFilter>());
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
				autodetect = main.Settings.Encoding.Length == 0;
				encoding = reload_ && autodetect
					? GuessEncoding(file_source, out certain)
					: (Encoding)main.m_encoding.Clone();
				if (certain) main.m_encoding = (Encoding)encoding.Clone();

				// If the settings say auto detect the row delimiters, and this is a reload
				// then detect them, otherwise use what is currently set
				certain = false;
				autodetect = main.Settings.RowDelimiter.Length == 0;
				row_delim = reload_ && autodetect
					? GuessRowDelimiter(file_source, encoding, main.Settings.MaxLineLength, out certain)
					: (byte[])main.m_row_delim.Clone();
				if (certain) main.m_row_delim = (byte[])row_delim.Clone();

				col_delim = (byte[])main.m_col_delim.Clone();
			}

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

			/// <summary>The line index that 'filepos' would have in the current line index. -1 if 'filepos' is before the current cached range, or 'm_line_index.Count' if after</summary>
			public readonly int filepos_line_index;

			/// <summary>The number of lines in the current line index</summary>
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
			public readonly RangeI cached_whole_line_range;

			/// <summary>Line filters</summary>
			public readonly List<IFilter> filters;

			/// <summary>Row transforms</summary>
			public readonly List<Transform> transforms;

			/// <summary>The progress callback to use (note: called in worker thread context)</summary>
			public ProgressFunc progress;
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
				if (Src == null)
					return;

				// Incremental updates cannot supplant reloads
				if (ReloadInProgress && reload == false)
					return;

				// Cause any existing builds to stop by changing the issue number
				Interlocked.Increment(ref m_build_issue);
				Log.Write(ELogLevel.Info, $"build start request (id {m_build_issue}, reload: {reload})\n{string.Empty}");//new StackTrace(0,true)));
				ReloadInProgress = reload;

				// Make copies of variables for thread safety
				var bli_data = new BLIData(this, Src, filepos_:filepos, reload_:reload, build_issue_:m_build_issue)
				{
					// Set up callbacks that marshal to the main thread
					progress = (scanned,length) =>
					{
						this.BeginInvoke(() => UpdateStatusProgress(scanned, length));
						return true;
					},
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

		/// <summary>The grunt work of building the new line index.</summary>
		private static void BuildLineIndexAsync(BLIData d, Action<BLIData, RangeI, List<RangeI>, Exception> on_complete)
		{
			// This method runs in a background thread
			// All we're doing here is loading data around 'd.filepos' so that there are an equal number
			// of lines on either side. This can be optimised however because the existing range of
			// cached data probably overlaps the range we want loaded.
			try
			{
				Log.Write(ELogLevel.Info, "BLIAsync", $"build started. (id {d.build_issue}, reload {d.reload})");
				if (BuildCancelled(d.build_issue)) return;
				using (d.file)
				{
					// A temporary buffer for reading sections of the file
					var buf = new byte[d.max_line_length];

					// Seek to the first line that starts immediately before 'filepos'
					d.filepos = FindLineStart(d.file, d.filepos, d.fileend, d.row_delim, d.encoding, buf);
					if (BuildCancelled(d.build_issue)) return;

					// Determine the range to scan and the number of lines in each direction
					var scan_backward = (d.fileend - d.filepos) > (d.filepos - 0); // scan in the most bound direction first
					var scan_range = CalcBufferRange(d.filepos, d.fileend, d.file_buffer_size);
					var line_range = CalcLineRange(d.line_cache_count);
					var bwd_lines = line_range.Begi;
					var fwd_lines = line_range.Endi;

					// Incremental loading - only load what isn't already cached.
					// If the 'filepos' is left of the cache centre, try to extent in left direction first.
					// If the scan range in that direction is empty, try extending at the other end. The
					// aim is to try to get d.line_index_count as close to d.line_cache_count as possible
					// without loading data that is already cached.
					#region Incremental loading
					if (!d.reload && !d.cached_whole_line_range.Empty)
					{
						// Determine the direction the cached range is moving based on where 'filepos' is relative
						// to the current cache centre and which range contains an valid area to be scanned.
						// With incremental scans we can only update one side of the cache because the returned line index has to
						// be a contiguous block of lines. This means one of 'bwd_lines' or 'fwd_lines' must be zero.
						var Lrange = new RangeI(scan_range.Beg, d.cached_whole_line_range.Beg);
						var Rrange = new RangeI(d.cached_whole_line_range.End, scan_range.End);
						var dir =
							(!Lrange.Empty && !Rrange.Empty) ? Math.Sign(2*d.filepos_line_index - d.line_cache_count) :
							(!Lrange.Empty) ? -1 :
							(!Rrange.Empty) ? +1 :
							0;

						// Determine the number of lines to scan, based on direction
						if (dir < 0)
						{
							scan_backward = true;
							scan_range = Lrange;
							bwd_lines -=  Math_.Clamp(d.filepos_line_index - 0, 0, bwd_lines);
							fwd_lines = 0;
						}
						else if (dir > 0)
						{
							scan_backward = false;
							scan_range = Rrange;
							bwd_lines = 0;
							fwd_lines -= Math_.Clamp(d.line_index_count - d.filepos_line_index - 1, 0, fwd_lines);
						}
						else if (dir == 0)
						{
							bwd_lines = 0;
							fwd_lines = 0;
							scan_range = RangeI.Zero;
						}
					}
					#endregion

					Debug.Assert(bwd_lines + fwd_lines <= d.line_cache_count);

					// Build the collection of line byte ranges to add to the cache
					var line_index = new List<RangeI>();
					if (bwd_lines != 0 || fwd_lines != 0)
					{
						// Line index buffers for collecting the results
						var fwd_line_buf = new List<RangeI>();
						var bwd_line_buf = new List<RangeI>();

						// Data used in the 'add_line' callback. Updated for forward and backward passes
						var lbd = new LineBufferData
						{
							line_buf = null, // pointer to either 'fwd_line_buf' or 'bwd_line_buf'
							line_limit = 0,  // Caps the number of lines read for each of the forward and backward searches
						};

						// Callback for adding line byte ranges to a line buffer
						AddLineFunc add_line = (line, baddr, fend, bf, enc) =>
						{
							if (line.Empty && d.ignore_blanks)
								return true;

							// Test 'text' against each filter to see if it's included
							// Note: not caching this string because we want to read immediate data
							// from the file to pick up file changes.
							string text = d.encoding.GetString(buf, (int)line.Beg, (int)line.Size);
							if (!PassesFilters(text, d.filters))
								return true;

							// Convert the byte range to a file range
							line = line.Shift(baddr);
							Debug.Assert(new RangeI(0, d.fileend).Contains(line));
							lbd.line_buf.Add(line);
							Debug.Assert(lbd.line_buf.Count <= lbd.line_limit);
							return (fwd_line_buf.Count + bwd_line_buf.Count) < lbd.line_limit;
						};

						// Callback for updating progress
						ProgressFunc progress = (scanned, length) =>
						{
							int numer = fwd_line_buf.Count + bwd_line_buf.Count, denom = lbd.line_limit;
							return d.progress(numer, denom) && !BuildCancelled(d.build_issue);
						};

						// Scan twice, starting in the direction of the smallest range so that any
						// unused cache space is used by the search in the other direction
						var scan_from = Math_.Clamp(d.filepos, scan_range.Beg, scan_range.End);
						for (int a = 0; a != 2; ++a, scan_backward = !scan_backward)
						{
							if (BuildCancelled(d.build_issue)) return;

							lbd.line_buf = scan_backward ? bwd_line_buf : fwd_line_buf;
							lbd.line_limit += scan_backward ? bwd_lines : fwd_lines;
							if ((bwd_line_buf.Count + fwd_line_buf.Count) < lbd.line_limit)
							{
								var length = scan_backward ? scan_from - scan_range.Beg : scan_range.End - scan_from;
								FindLines(d.file, scan_from, d.fileend, scan_backward, length, add_line, d.encoding, d.row_delim, buf, progress);
							}
						}

						// Scanning backward adds lines to the line index in reverse order.
						bwd_line_buf.Reverse();

						// 'line_index' should be a contiguous block of byte offset ranges for
						// the lines around 'd.filepos'. If 'd.reload' is false, then the line
						// index will only contain byte offset ranges that are not currently cached.
						line_index.Capacity = bwd_line_buf.Count + fwd_line_buf.Count;
						line_index.AddRange(bwd_line_buf);
						line_index.AddRange(fwd_line_buf);
					}

					// Job done
					on_complete(d, scan_range, line_index, null);
				}
			}
			catch (Exception ex)
			{
				on_complete(d, RangeI.Zero, null, ex);
			}
		}
		private class LineBufferData
		{
			public List<RangeI> line_buf;
			public int line_limit;
		}

		/// <summary>Called when building the line index completes (success or failure)</summary>
		private void BuildLineIndexComplete(BLIData d, RangeI range, List<RangeI> line_index, Exception error, Action on_success)
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
					SetStaticStatusMessage($"Error reading {Path.GetFileName(d.file.Name)}", Color.White, Color.DarkRed);
				}
				else
				{
					Log.Write(ELogLevel.Error, error, "Exception ended BuildLineIndex() call");
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
				Watch.CheckForChangedFiles();

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
			if (Settings.WatchEnabled)
			{
				EnableWatch(false);
				Misc.ShowHint(m_btn_watch, "File watching disabled due to error.");
			}
			Log.Write(ELogLevel.Error, err, $"Failed to build index list for {Src.Name}");
			if (err is NoLinesException)
			{
				Misc.ShowMessage(this, err.Message, "Scanning file terminated", MessageBoxIcon.Information);
			}
			else
			{
				Misc.ShowMessage(this, "Scanning the log file ended with an error.", "Scanning file terminated", MessageBoxIcon.Error, err);
			}
		}

		/// <summary>Buffer a maximum of 'count' bytes from 'stream' into 'buf' (note,
		/// automatically capped at buf.Length). If 'backward' is true the stream is 'seek'ed
		/// backward from the current position before reading and then 'seek'ed backward again
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
				// Bits Last code point  Byte 1    Byte 2    Byte 3    Byte 4    Byte 5   Byte 6
				//   7     U+007F        0xxxxxxx
				//  11     U+07FF        110xxxxx  10xxxxxx
				//  16     U+FFFF        1110xxxx  10xxxxxx  10xxxxxx
				//  21     U+1FFFFF      11110xxx  10xxxxxx  10xxxxxx  10xxxxxx
				//  26     U+3FFFFFF     111110xx  10xxxxxx  10xxxxxx  10xxxxxx  10xxxxxx
				//  31     U+7FFFFFFF    1111110x  10xxxxxx  10xxxxxx  10xxxxxx  10xxxxxx  10xxxxxx
				const int encode_mask = 0xC0; // 11000000
				const int encode_char = 0x80; // 10000000
				for (;;)
				{
					int b = file.Stream.ReadByte();
					if (b == -1) return 0; // End of file
					if ((b & encode_mask) == encode_char) continue; // If 'b' is a byte halfway through an encoded char, keep reading

					// Otherwise it's an ASCII char or the start of a multi-byte char
					// save the byte we read, and read the remainder of 'count'
					file.Stream.Seek(-1, SeekOrigin.Current);
					break;
				}
			}

			// Buffer file data
			// We don't add the BOM to the buffer because the purpose of this function is to return lines of text from the file source.
			// If we've been asked to read from 'filepos' == 0 and  'count' is less than the BOM.Length then return 0 bytes read. This
			// means that if a UTF-8 BOM is present, reading 1 byte => read=0, reading 2 bytes => read=0, reading 3 bytes =>read=0,
			// reading 4 bytes => read = 4, reading 5 bytes => read=5, etc
			if (count < file.Stream.Position - pos)
				return 0;

			count -= file.Stream.Position - pos;
			int read = file.Stream.Read(buf, 0, (int)Math.Min(count, buf.Length));
			if (read != count) throw new IOException($"Failed to read file over range [{pos},{pos + count}) ({count} bytes). Read {read}/{count} bytes.");
			if (backward) file.Stream.Seek(-read, SeekOrigin.Current);
			return read;
		}

		/// <summary>Seek to the first line that starts immediately before 'filepos'</summary>
		private static long FindLineStart(IFileSource file, long filepos, long fileend, byte[] row_delim, Encoding encoding, byte[] buf)
		{
			file.Stream.Seek(filepos, SeekOrigin.Begin);

			// Read a block into 'buf'
			bool eof;
			int read = Buffer(file, buf.Length, fileend, encoding, true, buf, out eof);
			if (read == 0) return 0; // assume the first character in the file is the start of a line

			// Scan for a line start
			int idx = Misc.FindNextDelim(buf, read - 1, read, row_delim, true);
			if (idx != -1) return file.Stream.Position + idx; // found
			if (filepos == read) return 0; // assume the first character in the file is the start of a line
			throw new NoLinesException(read);
		}

		/// <summary>Callback function called by FindLines that is called with each line found</summary>
		/// <param name="rng">The byte range of the line within 'buf' not including the row delimiter</param>
		/// <param name="base_addr">The base address in the file that buf is relative to</param>
		/// <param name="fileend">The current known end position of the file</param>
		/// <param name="buf">Buffer containing the buffered file data</param>
		/// <param name="encoding">The text encoding used</param>
		/// <returns>Return true to continue adding lines, false to stop</returns>
		private delegate bool AddLineFunc(RangeI rng, long base_addr, long fileend, byte[] buf, Encoding encoding);

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
				for (i = Misc.FindNextDelim(buf, i, read, row_delim, backward); i != iend; i = Misc.FindNextDelim(buf, i, read, row_delim, backward))
				{
					// 'i' points to the start of a line,
					// 'lasti' points to the start of the last line we found
					// Get the range in buf containing the line
					RangeI line = backward
						? new RangeI(i, lasti - row_delim.Length)
						: new RangeI(lasti, i - row_delim.Length);

					// Pass the detected line to the callback
					if (!add_line(line, base_addr, fileend, buf, encoding))
						return;

					lasti = i;
					if (backward) i -= row_delim.Length + 1;
				}

				// From 'lasti' to the end (or start in the backwards case) of the buffer represents
				// a (possibly partial) line. If we read a full buffer load last time, then we'll go
				// round again trying to read another buffer load, starting from 'lasti'.
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
					RangeI line = backward
						? new RangeI(i + 1, lasti - row_delim.Length)
						: new RangeI(lasti, i - (IsRowDelim(buf, i - row_delim.Length, row_delim) ? row_delim.Length : 0));

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
		private int MergeLineIndex(RangeI scan_range, List<RangeI> line_index, long cache_range, long filepos, long fileend, bool replace)
		{
			// Main thread context
			// Both old and new ranges are expected to be contiguous
			var old_rng = m_line_index.Count != 0 ? new RangeI(m_line_index.Front().Beg, m_line_index.Back().End) : RangeI.Zero;
			var new_rng =   line_index.Count != 0 ? new RangeI(  line_index.Front().Beg,   line_index.Back().End) : RangeI.Zero;
			int row_delta = 0;

			// Replace 'm_line_index' with 'line_index'
			if (replace)
			{
				// Try to determine the row delta
				// Use any range overlap to work out the row delta.
				// If the ranges overlap, we can search for the start address of the intersection in both
				// ranges to get the row delta. If the don't overlap, the best we can do is say the direction.
				var intersect = old_rng.Intersect(new_rng);
				if (!intersect.Empty)
				{
					var old_idx = LineIndex(m_line_index, intersect.Beg);
					var new_idx = LineIndex(  line_index, intersect.Beg);
					row_delta = new_idx - old_idx;
				}
				else
				{
					row_delta = intersect.Beg == old_rng.End ? -line_index.Count : line_index.Count;
				}

				Log.Write(ELogLevel.Info, $"Replacing results. Results contain {line_index.Count} lines about file position {filepos}/{fileend}");
				m_line_index.Assign(line_index);

				// Invalidate cached lines
				InvalidateCache();
			}

			// The new range is to the left of the old range
			else if (new_rng.Beg < old_rng.Beg && new_rng.End <= old_rng.End)
			{
				Log.Write(ELogLevel.Info, $"Merging results front. Results contain {line_index.Count} lines. File position {filepos}/{fileend}");

				// Make sure there's no overlap by removing data from 'm_line_index'
				var trim = 0; for (; trim != m_line_index.Count && m_line_index[trim].Beg < new_rng.End; ++trim) {}
				m_line_index.RemoveRange(0, trim);
				row_delta -= trim;

				// Insert the new lines
				m_line_index.InsertRange(0, line_index);
				row_delta += line_index.Count;

				// Trim the tail
				if (m_line_index.Count > m_line_cache_count)
					m_line_index.RemoveRange(m_line_cache_count, m_line_index.Count - m_line_cache_count);

				// Invalidate the cache over the memory range of the lines we've just added.
				InvalidateCache(new_rng);
			}

			// The new range is to the right of the old range
			else if (new_rng.Beg >= old_rng.Beg && new_rng.End > old_rng.End)
			{
				Log.Write(ELogLevel.Info, $"Merging results back. Results contain {line_index.Count} lines. File position {filepos}/{fileend}");

				// Make sure there's no overlap by removing data from 'm_line_index'
				var trim = 0; for (; trim != m_line_index.Count && m_line_index.Back(trim).End > new_rng.Beg; ++trim) {}
				m_line_index.RemoveRange(m_line_index.Count - trim, trim);

				// Insert the new lines
				m_line_index.InsertRange(m_line_index.Count, line_index);

				// Trim the head
				if (m_line_index.Count > m_line_cache_count)
				{
					row_delta -= m_line_index.Count - m_line_cache_count;
					m_line_index.RemoveRange(0, m_line_index.Count - m_line_cache_count);
				}

				// Invalidate the cache over the memory range of the lines we've just added.
				InvalidateCache(new_rng);
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
						// Look for the first newline/carriage return character
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
		private static int LineIndex(List<RangeI> line_index, long filepos)
		{
			// Careful, comparing 'filepos' to line starts, if idx == line_index.Count
			// it could be in the last line, or after the last line.
			if (line_index.Count == 0) return 0;
			if (filepos >= line_index.Back().End) return line_index.Count;
			int idx = line_index.BinarySearch(line => line.Beg.CompareTo(filepos));
			return idx >= 0 ? idx : ~idx - 1;
		}
	}
}
