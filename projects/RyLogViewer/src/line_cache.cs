using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using pr.common;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public partial class Main
	{
		private readonly List<Line> m_line_cache = new List<Line>();
		private byte[]              m_line_buf   = new byte[512];

		/// <summary>Cached graphics object for measuring text sizes</summary>
		private Graphics m_gfx { get { return m_impl_gfx ?? (m_impl_gfx = CreateGraphics()); } }
		private Graphics m_impl_gfx;

		/// <summary>Initialise the line cache</summary>
		private void InitCache()
		{
			m_line_cache.Capacity = 1000;
			for (int i = 0; i != m_line_cache.Capacity; ++i)
				m_line_cache.Add(new Line());
		}

		/// <summary>Returns the line data for the line in the m_line_index at position 'row'</summary>
		private Line ReadLine(int row)
		{
			if (row < 0 || row >= m_line_index.Count) throw new Exception("Row index {0} is not within the line index range [0,{1})".Fmt(row, m_line_index.Count));
			return ReadLine(m_line_index[row]);
		}

		/// <summary>Access info about a line (cached)</summary>
		private Line ReadLine(Range rng)
		{
			Debug.Assert(FileOpen);

			// Check if the line is already cached
			Line line = m_line_cache[(int)(rng.Beg % m_line_cache.Count)];
			if (line.LineStartAddr == rng.Beg) return line;

			// If not, read it from file and perform highlighting and transforming on it
			try
			{
				// Read the whole line into m_buf
				//m_file.Flush(); why??
				m_file.Stream.Seek(rng.Beg, SeekOrigin.Begin);
				m_line_buf = rng.Size <= m_line_buf.Length ? m_line_buf : new byte[rng.Size];
				int read = m_file.Stream.Read(m_line_buf, 0, (int)rng.Size);
				if (read != rng.Size) throw new IOException("failed to read file over range [{0},{1}) ({2} bytes). Read {3}/{2} bytes.".Fmt(rng.Beg, rng.End, rng.Size, read));

				// Cache data for the line
				line.Read(rng.Beg, m_line_buf, 0, read, m_encoding, m_col_delim, m_highlights, m_transforms);

				// Save the text size
				line.TextSize = m_gfx.MeasureString(line.RowText, m_grid.RowsDefaultCellStyle.Font);

				return line;
			}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Failed to read source data");
				line.RowText = "<read failed>";
				return line;
			}
		}

		/// <summary>Invalidate cache entries for lines within a memory range</summary>
		private void InvalidateCache(Range rng)
		{
			foreach (var line in m_line_cache)
				if (rng.Contains(line.LineStartAddr))
					line.LineStartAddr = -1;
		}

		/// <summary>Invalidate all cache entries</summary>
		private void InvalidateCache()
		{
			foreach (var line in m_line_cache)
				line.LineStartAddr = -1;
		}
	}
}
