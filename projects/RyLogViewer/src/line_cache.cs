using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using pr.common;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public partial class Main
	{
		/// <summary>A cached line from the file</summary>
		public class Line
		{
			/// <summary>A column value within a line</summary>
			public class Col
			{
				/// <summary>The text for the column</summary>
				public readonly string Text;
				
				/// <summary>The highlight to use for this value. If null, that the line does not match any highlights</summary>
				public readonly Highlight HL;
				
				public Col(string text, Highlight hl)
				{
					Text = text;
					HL = hl;
				}
				public override string ToString()
				{
					return Text;
				}
			}
			
			/// <summary>The file offset for the start of this cached line</summary>
			public long LineStartAddr;
			
			/// <summary>The string for the whole row</summary>
			public string RowText;
			
			/// <summary>The column values for this line</summary>
			public readonly List<Col> Column;
			
			public Col this[int col_index]
			{
				get { return col_index >= 0 && col_index < Column.Count ? Column[col_index] : new Col("",null); }
			}
			
			public Line()
			{
				LineStartAddr = -1;
				Column = new List<Col>();
			}

			/// <summary>Populate this line from a buffer</summary>
			public void Read(long addr, byte[] buf, int start, int length, Encoding encoding, byte[] col_delim, List<Highlight> highlights, List<Transform> transforms)
			{
				LineStartAddr = addr;
				
				// Convert the buffer to text
				RowText = encoding.GetString(buf, start, length);
				//RowText = RowText.TrimEnd(new[]{'\r','\n'}); - don't do this, its the logs fault if it has weird newlines at the end of each row
				
				// Apply any transforms
				foreach (var tx in transforms)
					RowText = tx.Txfm(RowText);
				
				Column.Clear();
				
				// Split the line into columns
				if (col_delim.Length == 0) // Single column
				{
					Highlight hl = highlights != null ? highlights.FirstOrDefault(h => h.IsMatch(RowText)) : null;
					Column.Add(new Col(RowText, hl));
				}
				else // Multiple columns
				{
					int i = start, lasti = i;
					for (i = FindNextDelim(buf, i, length, col_delim, false); i != length; i = FindNextDelim(buf, i, length, col_delim, false))
					{
						string col_text = encoding.GetString(buf, lasti, i - lasti);
						Highlight hl = highlights != null ? highlights.FirstOrDefault(h => h.IsMatch(col_text)) : null;
						Column.Add(new Col(col_text, hl));
						lasti = i;
					}
				}
			}
		}
		private readonly List<Line> m_line_cache = new List<Line>();
		private byte[]              m_line_buf   = new byte[512];
		
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
			return ReadLine(m_line_index[row]);
		}

		/// <summary>Access info about a line (cached)</summary>
		private Line ReadLine(Range rng)
		{
			Debug.Assert(FileOpen);
			
			// Check if the line is already cached
			Line line = m_line_cache[(int)(rng.Begin % m_line_cache.Count)];
			if (line.LineStartAddr == rng.Begin) return line;
			
			// If not, read it from file and perform highlighting and transforming on it
			try
			{
				// Read the whole line into m_buf
				m_file.Flush();
				m_file.Seek(rng.Begin, SeekOrigin.Begin);
				m_line_buf = rng.Count <= m_line_buf.Length ? m_line_buf : new byte[rng.Count];
				int read = m_file.Read(m_line_buf, 0, (int)rng.Count);
				if (read != rng.Count) throw new IOException("failed to read file over range [{0},{1}) ({2} bytes). Read {3}/{2} bytes.".Fmt(rng.Begin, rng.End, rng.Count, read));
			
				line.Read(rng.Begin, m_line_buf, 0, read, m_encoding, m_col_delim, m_highlights, m_transforms);
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
