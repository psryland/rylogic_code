using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rylogic.Gui.WPF.TextEditor
{
	public class TextDocument
	{
		// Notes:
		//  - Storing lines as a simple list may not be efficient enough. AvalonEdit uses
		//    a Red/Black tree, presumably to make inserting lines more efficient and accessing
		//    a line by character index. It's probably worth avoiding relying on the list properties
		//    of 'Lines' except for simple stuff. I could replace 'Lines' with an IList-like tree
		//    implementation... one day...

		/// <summary>The lines of text in the document</summary>
		public IList<Line> Lines { get; } = new List<Line>();

		/// <summary>The styles</summary>
		public StyleMap Styles { get; } = new StyleMap();

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

		/// <summary>Gets a document line by character offset.</summary>
		public Line LineAt(int offset)
		{
			var len = 0L;
			foreach (var line in Lines)
			{
				if (offset < len + line.Count + LineEnd.Length) return line;
				len += line.Count + LineEnd.Length;
			}
			throw new IndexOutOfRangeException("Character offset is beyond the range of the document");
		}
	}
}