using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rylogic.Gui.WPF.TextEditor
{
	public class Document
	{
		public IList<Line> Lines { get; } = new List<Line>();

		/// <summary>Get/Set the document as plain text</summary>
		public string Text
		{
			get
			{
				var sb = new StringBuilder(TextLength);
				foreach (var ch in Characters) sb.Append(ch);
				return sb.ToString();
			}
		}

		/// <summary>The total length of the document</summary>
		public int TextLength => Lines.Sum(x => x.Count + LineEnd.Length);

		/// <summary>The number of lines</summary>
		public int LineCount => Lines.Count;

		/// <summary>Implicit new line character(s) for the end of each line</summary>
		public string LineEnd { get; set; } = "\n";

		/// <summary>Enumerate each cell in the document</summary>
		public IEnumerable<Cell> Cells
		{
			get
			{
				foreach (var line in Lines)
				{
					foreach (var cell in line)
						yield return cell;
					foreach (var ch in LineEnd)
						yield return new Cell(ch, 0);
				}
			}
		}

		/// <summary>Enumerate each character in the document</summary>
		public IEnumerable<char> Characters => Cells.Select(x => x.ch);
	}
}