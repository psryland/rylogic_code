using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Rylogic.Common;

namespace Rylogic.TextAligner
{
	/// <summary></summary>
	[DebuggerDisplay("{Description,nq}")]
	internal class Token
	{
		public Token(AlignGroup grp, int grp_index, int patn_index, ITextSnapshotLine line, int line_number, RangeI span, int tab_size)
		{
			Grp = grp;
			GrpIndex = grp_index;
			Patn = grp.Patterns[patn_index];
			Span = span;
			Line = line;
			LineNumber = line_number;

			var line_text = Line.GetText();
			int i; for (i = (int)(Span.Beg - 1); i >= 0 && char.IsWhiteSpace(line_text[i]); --i) { }
			++i;

			MinCharIndex = i;
			MinColumnIndex = CharIndexToColumnIndex(line_text, MinCharIndex, tab_size);
			CurrentCharIndex = (int)Span.Beg;
			CurrentColumnIndex = CharIndexToColumnIndex(line_text, (int)Span.Beg, tab_size);
		}

		/// <summary>The align group corresponding to GrpIndex</summary>
		public AlignGroup Grp { get; private set; }

		/// <summary>The index position of the pattern in the priority list</summary>
		public int GrpIndex { get; private set; }

		/// <summary>The pattern that this token matches</summary>
		public AlignPattern Patn { get; private set; }

		/// <summary>The line that this token is on</summary>
		public ITextSnapshotLine Line { get; private set; }

		/// <summary>The line number that the token is on</summary>
		public int LineNumber { get; private set; }

		/// <summary>The character range of the matched pattern on the line</summary>
		public RangeI Span { get; private set; }

		/// <summary>The minimum distance of this token from 'caret_pos'</summary>
		public int Distance(int caret_pos)
		{
			return !Span.Contains(caret_pos)
				? (int)Math.Min(Math.Abs(Span.Beg - caret_pos), Math.Abs(Span.End - caret_pos)) 
				: 0;
		}

		/// <summary>The minimum character index that this token can be left shifted to</summary>
		public int MinCharIndex { get; private set; }

		/// <summary>The minimum column index that this token can be left shifted to</summary>
		public int MinColumnIndex { get; private set; }

		/// <summary>The current char index of the token</summary>
		public int CurrentCharIndex { get; private set; }

		/// <summary>The current column index of the token</summary>
		public int CurrentColumnIndex { get; private set; }

		/// <summary>Converts a char index into a column index for the given line</summary>
		private int CharIndexToColumnIndex(string line, int char_index, int tab_size)
		{
			// Careful, columns != char index because of tabs
			// Also, Surrogate pairs are pairs of char's that only take up one column
			var col = 0;
			for (var i = 0; i != char_index; ++i)
			{
				col += line[i] == '\t' ? tab_size - (col % tab_size) : 1;
				if (i < line.Length - 1 && char.IsSurrogatePair(line[i], line[i + 1]))
					++i;
			}
			return col;
		}

		/// <summary></summary>
		public string Description => $"Line: {LineNumber} Grp: {Grp} Patn: {Patn.Description} Span: {Span}";
	}
}
