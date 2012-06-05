using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using pr.common;

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
			}
			
			/// <summary>The line in the file that is cached in this object</summary>
			public int Index;
			
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
				Index = -1;
				Column = new List<Col>();
			}
		}
		private readonly List<Line> m_line_cache = new List<Line>();
		private byte[]              m_line_buf   = new byte[512];
		
		/// <summary>Initialise the line cache</summary>
		private void InitCache()
		{
			m_line_cache.Capacity = 500;
			for (int i = 0; i != m_line_cache.Capacity; ++i)
				m_line_cache.Add(new Line());
		}

		/// <summary>Access info about a line (cached)</summary>
		private Line CacheLine(int index)
		{
			Debug.Assert(FileOpen);
			
			// Check if the line is already cached
			Line line = m_line_cache[index % m_line_cache.Count];
			if (line.Index == index) return line;
			
			// If not, read it from file and perform highlighting tests on it
			Range rng = m_line_index[index];

			// Read the whole line into m_buf
			m_file.Seek(rng.m_begin, SeekOrigin.Begin);
			m_line_buf = rng.Count <= m_line_buf.Length ? m_line_buf : new byte[rng.Count];
			int read = m_file.Read(m_line_buf, 0, (int)rng.Count);
			if (read != rng.Count) throw new IOException("failed to read file over range ["+rng.m_begin+","+rng.m_end+"). Read "+read+"/"+rng.Count+" bytes.");
			
			// Convert the buffer to text
			line.RowText = m_encoding.GetString(m_line_buf, 0, read);
			
			// Split the line into columns
			line.Column.Clear();
			if (m_col_delim.Length != 0)
			{
				// Multiple columns...
				foreach (string col_value in line.RowText.Split(m_col_delim))
				{
					string col_text = col_value;
					Highlight hl = m_highlights.FirstOrDefault(h => h.IsMatch(col_text));
					line.Column.Add(new Line.Col(col_text, hl));
				}
			}
			else
			{
				// Single column
				Highlight hl = m_highlights.FirstOrDefault(h => h.IsMatch(line.RowText));
				line.Column.Add(new Line.Col(line.RowText, hl));
			}
			line.Index = index;
			return line;
		}

		/// <summary>Invalidate all cache entries</summary>
		private void InvalidateCache()
		{
			foreach (var line in m_line_cache)
				line.Index = -1;
		}
	}
}
