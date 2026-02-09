using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using Rylogic.Common;
using Rylogic.Utility;

namespace RyLogViewer
{
	public class LineCache : IReadOnlyList<ILine>, INotifyCollectionChanged, IDisposable
	{
		private readonly IList<ILine?> m_cache;
		private readonly IReport m_report;
		private byte[] m_line_buf;

		public LineCache(LineIndex line_index, ILineFormatter formatter, IReport report)
		{
			m_cache = new ILine[1000];
			m_line_buf = new byte[512];
			m_report = report;
			LineIndex = line_index;
			Formatter = formatter;
		}
		public void Dispose()
		{
			LineIndex = null!;
			Formatter = null!;
		}

		/// <summary></summary>
		public event NotifyCollectionChangedEventHandler? CollectionChanged;
		
		/// <summary>The formatter to use to interpret lines</summary>
		public ILineFormatter Formatter
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				InvalidateCache();
			}
		} = null!;

		/// <summary>The line index that provides the byte ranges of log data lines</summary>
		public LineIndex LineIndex
		{
			get => m_line_index;
			set
			{
				if (m_line_index == value) return;
				if (m_line_index != null)
				{
					m_line_index.BuildComplete -= HandleBuildComplete;
					Util.Dispose(ref m_src!);
				}
				m_line_index = value;
				if (m_line_index != null)
				{
					m_line_index.BuildComplete += HandleBuildComplete;
					m_src = m_line_index.Src.OpenStream();
					InvalidateCache();
				}

				// Handlers
				void HandleBuildComplete(object? sender, BuildCompleteEventArgs e)
				{
					// Invalidate the cache over the memory range of the lines that were added.
					InvalidateCache(e.NewRange);
					CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
				}
			}
		}
		private LineIndex m_line_index = null!;
		private Stream m_src = null!;

		/// <summary>The number of lines available</summary>
		public int Count => LineIndex.Count;

		/// <summary>Access a line from the log</summary>
		public ILine this[int index] => ReadLine(index);

		/// <summary>Returns the line data for the line in the m_line_index at position 'row'</summary>
		private ILine ReadLine(int row)
		{
			if (row < 0 || row >= LineIndex.Count)
				throw new Exception($"Row index {row} is not within the line index range [0,{LineIndex.Count})");

			return ReadLine(LineIndex[row]);
		}

		/// <summary>Access info about a line (cached)</summary>
		private ILine ReadLine(RangeI rng)
		{
			// The position in the cache for file range 'rng'
			var cache_index = rng.Begi % m_cache.Count;

			// Check if the line is already cached
			var line = m_cache[cache_index];
			if (line?.FileByteRange == rng)
				return line;

			// If not, read it from file and perform highlighting and transforming on it
			try
			{
				// Read the whole line into a buffer
				m_src.Seek(rng.Beg, SeekOrigin.Begin);
				m_line_buf = rng.Size <= m_line_buf.Length ? m_line_buf : new byte[rng.Size];
				var read = m_src.Read(m_line_buf, 0, (int)rng.Size);
				if (read != rng.Size)
					throw new IOException($"Failed to read file over range [{rng.Beg},{rng.End}) ({rng.Size} bytes). Read {read}/{rng.Size} bytes.");

				// Convert the line of data into an 'ILine'
				line = Formatter.CreateLine(m_line_buf, 0, read, rng);
				m_cache[cache_index] = line;
				return line;
			}
			catch (Exception ex)
			{
				m_report.ErrorPopup("Failed to read source data", ex);
				return new ErrorLine("<read failed>", rng);
			}
		}

		/// <summary>Invalidate cache entries for lines that overlap the given memory range</summary>
		private void InvalidateCache(RangeI rng)
		{
			for (var i = 0; i != m_cache.Count; ++i)
			{
				if (m_cache[i] is not ILine line) continue;
				if (line.FileByteRange.Intersect(rng).Empty) continue;
				m_cache[i] = null;
			}
		}

		/// <summary>Invalidate all cache entries</summary>
		private void InvalidateCache()
		{
			for (var i = 0; i != m_cache.Count; ++i)
				m_cache[i] = null;
		}

		#region IEnumerable
		public IEnumerator<ILine> GetEnumerator()
		{
			for (int i = 0, iend = Count; i != iend; ++i)
				yield return this[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
