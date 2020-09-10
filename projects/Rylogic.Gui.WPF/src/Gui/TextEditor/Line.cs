using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Media;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF.TextEditor
{
	public class Line :IList<Cell>
	{
		// Notes:
		//  - 'm_cells' should not include new-line characters.
		//    There is an implicit new line at the end of each line.
		//  - The new-line is not included in the returned text because
		//    that would require a dependency on the 'Document'
		//  - 'pixels_per_dip' is DIP/96.0, i.e. 1.0 if DPI is 96.0, 1.25 if DIP is 120.0, etc

		private List<Cell> m_cells = new List<Cell>();
		public Line()
		{}
		public Line(string text, ushort style = 0)
		{
			SetText(text, style);
		}

		/// <summary>Get the text of this line</summary>
		public virtual string Text
		{
			get
			{
				var sb = new StringBuilder(Count);
				foreach (var cell in m_cells) sb.Append(cell.ch);
				return sb.ToString();
			}
		}

		/// <summary>Set the line to the given text and style</summary>
		public void SetText(string text, ushort style)
		{
			m_cells.Resize(text.Length);
			for (int i = 0; i != text.Length; ++i)
				this[i] = new Cell(text[i], style);
		}

		/// <summary>Set a range of text on this line with the given style</summary>
		public void SetText(string text, ushort style, int start, int length)
		{
			if (start < 0)
				throw new ArgumentException(nameof(start), "Start index is out of range");
			if (length < 0)
				throw new ArgumentException(nameof(start), "Length is out of range");

			// Grow the line if necessary
			if (start + length > Count)
				m_cells.Resize(start + length);

			// Set the text and style
			for (int i = 0, j = start; i != length; ++i, ++j)
				this[j] = new Cell(text[i], style);
		}

		/// <summary>Line length</summary>
		public int Count => m_cells.Count;

		/// <summary>Access to each cell</summary>
		public Cell this[int index]
		{
			get => m_cells[index];
			set
			{
				if (m_cells[index] == value) return;
				if (value.ch == '\n' || value.ch == '\r') throw new Exception("Line text should not include new-line characters");
				m_cells[index] = value;
				Invalidate();
			}
		}

		/// <inheritdoc/>
		public int IndexOf(Cell item)
		{
			return m_cells.IndexOf(item);
		}

		/// <summary>Reset the line to empty</summary>
		public void Clear()
		{
			m_cells.Clear();
		}

		/// <summary>Append a cell to the line</summary>
		public void Add(Cell item)
		{
			m_cells.Add(new Cell());
			this[Count - 1] = item;
		}

		/// <summary>Insert a cell into the line</summary>
		public void Insert(int index, Cell item)
		{
			m_cells.Insert(index, new Cell());
			this[index] = item;
		}

		/// <summary>Delete a cell from the line</summary>
		public bool Remove(Cell item)
		{
			return m_cells.Remove(item);
		}

		/// <summary>Delete a cell at the given index position</summary>
		public void RemoveAt(int index)
		{
			m_cells.RemoveAt(index);
		}

		/// <inheritdoc/>
		public bool Contains(Cell item)
		{
			return m_cells.Contains(item);
		}

		/// <inheritdoc/>
		void ICollection<Cell>.CopyTo(Cell[] array, int arrayIndex)
		{
			m_cells.CopyTo(array, arrayIndex);
		}

		/// <summary></summary>
		bool ICollection<Cell>.IsReadOnly => false;

		/// <inheritdoc/>
		public IEnumerator<Cell> GetEnumerator()
		{
			return m_cells.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>Text ready to render</summary>
		public IEnumerable<FormattedText> FormattedText(StyleMap styles)
		{
			// No text on this line
			if (m_cells.Count == 0)
				yield break;

			// If the formatted text isn't cached, recreate it
			if (m_formatted.Count == 0)
			{
				m_formatted.Clear();
				Height = 0.0;

				var sb = new StringBuilder(256);
				for (int s = 0, e = 0; s != Count; s = e)
				{
					sb.Length = 0;

					// Find the span of characters with the same style
					sb.Append(m_cells[s].ch);
					for (e = s + 1; e != Count && m_cells[e].sty == m_cells[s].sty; ++e)
						sb.Append(m_cells[e].ch);

					// Get the style for the span
					var style = styles.TryGetValue(m_cells[s].sty, out var sty) && sty is TextStyle ts ? ts : TextStyle.Default;

					// Create formatted text for the span
					var formatted = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, style.Flow, style.Typeface, style.EmSize, style.ForeColor, style.PixelsPerDIP);
					m_formatted.Add(formatted);

					// Record the line height
					Height = Math.Max(Height, formatted.Height);
				}
			}

			// Return the formatted text
			foreach (var ft in m_formatted)
				yield return ft;
		}
		private List<FormattedText> m_formatted = new List<FormattedText>();

		/// <summary>Cause the cached formatted text to be recreated</summary>
		public void Invalidate()
		{
			m_formatted.Clear();
		}

		/// <summary>Line height is only know after formatting</summary>
		public double Height { get; private set; } = 0.0;
	}
}