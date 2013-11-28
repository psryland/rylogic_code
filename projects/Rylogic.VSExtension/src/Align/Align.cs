using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using pr.common;
using pr.extn;
using Span = pr.common.Span;

namespace Rylogic.VSExtension
{
	internal class Align
	{
		private class Token
		{
			/// <summary>The line number that the token is on</summary>
			public int LineNumber { get; private set; }

			/// <summary>The line that this token is on</summary>
			public ITextSnapshotLine Line { get; private set; }

			/// <summary>The line of text that this token is on</summary>
			public string LineText { get; private set; }

			/// <summary>The index position of the pattern in the priority list</summary>
			public int GrpIndex { get; private set; }

			/// <summary>The align group corresponding to GrpIndex</summary>
			public AlignGroup Grp { get; private set; }

			/// <summary>The index of the matching pattern within 'Grp'</summary>
			public int PatnIndex { get; private set; }

			/// <summary>The pattern that this token matches</summary>
			public AlignPattern Patn { get; private set; }

			/// <summary>The character range of the matched pattern on the line</summary>
			public Span Span { get; private set; }

			/// <summary>The minimum distance of this token from 'caret_pos'</summary>
			public int Distance(int caret_pos)
			{
				return Span.Contains(caret_pos) ? 0 : Math.Min(Math.Abs(Span.Begin - caret_pos), Math.Abs(Span.End - caret_pos));
			}

			/// <summary>The minimum character index that this token can be left shifted to</summary>
			public int MinCharIndex { get; private set; }

			/// <summary>The minimum column index that this token can be left shifted to</summary>
			public int MinColumnIndex { get; private set; }

			/// <summary>The current column index of the token</summary>
			public int CurrentColumnIndex { get; private set; }

			public Token(Align align, int line_number, int grp_index, int patn_index, Span span)
			{
				LineNumber = line_number;
				GrpIndex   = grp_index;
				Grp        = align.m_groups[grp_index];
				PatnIndex  = patn_index;
				Patn       = Grp.Patterns[patn_index];
				Span       = span;
				Line       = align.m_snapshot.GetLineFromLineNumber(line_number);
				LineText   = Line.GetText();

				int i; for (i = Span.Begin - 1; i >= 0 && char.IsWhiteSpace(LineText[i]); --i) {} ++i;
				var tab_size = align.m_view.Options.GetOptionValue(DefaultOptions.TabSizeOptionId);

				MinCharIndex       = i;
				MinColumnIndex     = CharIndexToColumnIndex(LineText, MinCharIndex, tab_size);
				CurrentColumnIndex = CharIndexToColumnIndex(LineText, Span.Begin  , tab_size);
			}

			/// <summary>Converts a char index into a column index for the given line</summary>
			private int CharIndexToColumnIndex(string line, int char_index, int tab_size)
			{
				// Careful, columns != char index because of tabs
				// Also, Surrogate pairs are pairs of char's that only take up one column
				var col = 0;
				for (var i = 0; i != char_index; ++i)
				{
					col += line[i] == '\t' ? tab_size - (col % tab_size) : 1;
					if (i < line.Length-1 && char.IsSurrogatePair(line[i], line[i+1]))
						++i;
				}
				return col;
			}

			public override string ToString()
			{
				return "Line: {0} Grp: {1} Patn: {2} Span: {3}".Fmt(LineNumber, Grp, Patn, Span);
			}
		}

		private readonly List<AlignGroup> m_groups;
		private readonly IWpfTextView m_view;
		private readonly ITextSnapshot m_snapshot;
		private readonly ITextCaret m_caret;

		public Align(IEnumerable<AlignGroup> groups, IWpfTextView view)
		{
			m_groups   = groups.ToList();
			m_view     = view;
			m_snapshot = m_view.TextSnapshot;
			m_caret    = m_view.Caret;

			// Get the current line number, line, and line position
			var line_number = m_snapshot.GetLineNumberFromPosition(m_caret.Position.BufferPosition);
			var line_range = FindLineRange();

			// Get the pattern groups to align on
			var grps = ChoosePatterns(line_number);

			// Find the edits to make
			var edits = FindAlignments(line_number, grps, line_range);

			// Make the alignment edits in an undo scope
			DoAligning(edits);
		}

		/// <summary>Return the maximum range of lines to apply aligning to</summary>
		private Span FindLineRange()
		{
			// If there is a selection that spans multiple lines, limit the aligning to those lines
			var selection = m_view.Selection;
			if (!selection.IsEmpty)
			{
				var start_line_number = m_snapshot.GetLineNumberFromPosition(selection.Start.Position);
				var end_line_number   = m_snapshot.GetLineNumberFromPosition(selection.End.Position);
				Debug.Assert(start_line_number <= end_line_number);
				if (start_line_number != end_line_number)
					return new Span(start_line_number, end_line_number - start_line_number);
			}
			return new Span(0, m_snapshot.LineCount);
		}

		/// <summary>Returns the column index that tokens should be aligned to</summary>
		private int FindAlignColumn(IEnumerable<Token> toks)
		{
			var offset = Range.Invalid;
			var column = 0;
			foreach (var tok in toks)
			{
				offset.Begin = Math.Min(offset.Begin, tok.Patn.Offset);
				offset.End   = Math.Max(offset.End,   tok.Patn.Offset);
				column       = Math.Max(column, tok.MinColumnIndex);
			}
			if (column != 0) column++; // Add a single whitespace, unless at column 0
			column += (int)offset.Count;
			return column;
		}

		/// <summary>Return a list of alignment patterns in priority order.</summary>
		private List<AlignGroup> ChoosePatterns(int line_number)
		{
			// If there is a selection, see if we should use the selected text at the align pattern
			var selection = m_view.Selection;
			if (!selection.IsEmpty)
			{
				// Only use single line selections on the same line as the caret
				var start_line_number = m_snapshot.GetLineNumberFromPosition(selection.Start.Position);
				var end_line_number   = m_snapshot.GetLineNumberFromPosition(selection.End.Position);
				if (start_line_number == line_number && end_line_number == line_number)
				{
					var start_line = selection.Start.Position.GetContainingLine();
					var s = selection.Start.Position - start_line.Start.Position;
					var e = selection.End.Position   - start_line.Start.Position;
					var expr = start_line.GetText().Substring(s, e - s);
					return new[]{new AlignGroup("Selection", new AlignPattern(EPattern.Substring, expr))}.ToList();
				}
			}

			// Make a copy of the groups
			var groups = m_groups.ToList();

			// If the cursor is next to an alignment pattern, move that pattern to the front of the priority list
			var line = m_snapshot.GetLineFromLineNumber(line_number);
			var line_text = line.GetText();
			var column = m_caret.Position.BufferPosition - line.Start.Position;

			// Find matches that span, are immediately to the right, or immediately to the left (priority order)
			AlignGroup spanning = null;
			AlignGroup rightof = null;
			AlignGroup leftof  = null;
			for (var i = 0; i != groups.Count; ++i)
			{
				var grp = groups[i];
				foreach (var pat in grp.Patterns)
				foreach (var match in pat.AllMatches(line_text))
				{
					// Spanning matches have the highest priority
					if (match.Begin < column && match.End > column)
						spanning = grp;

					// Matches to the right separated only by whitespace are the next highest priority
					if (match.Begin >= column && string.IsNullOrWhiteSpace(line_text.Substring(column, match.Begin - column)))
						rightof = grp;

					// Matches to the left separated only by whitespace are next
					if (match.End <= column && string.IsNullOrWhiteSpace(line_text.Substring(match.End, column - match.End)))
						leftof = grp;
				}
			}

			// Move the 'near' patterns to the front of the priority list
			if (leftof   != null) { m_groups.Remove(leftof); m_groups.Insert(0, leftof); }
			if (rightof  != null) { m_groups.Remove(rightof); m_groups.Insert(0, rightof); }
			if (spanning != null) { m_groups.Remove(spanning); m_groups.Insert(0, spanning); }

			return m_groups;
		}

		/// <summary>Scans 'line' for alignment boundaries, returning a collection in the order they occur within the line</summary>
		private List<Token> FindAlignBoundariesOnLine(int line_number, IEnumerable<AlignGroup> grps)
		{
			var tokens = new List<Token>();

			if (line_number < 0 || line_number >= m_snapshot.LineCount)
				return tokens;

			var line = m_snapshot.GetLineFromLineNumber(line_number);
			var line_text = line.GetText();

			var grp_index = -1;
			foreach (var grp in grps)
			{
				++grp_index;
				var patn_index = -1;
				foreach (var patn in grp.Patterns)
				{
					++patn_index;
					foreach (var match in patn.AllMatches(line_text))
						tokens.Add(new Token(this, line_number, grp_index, patn_index, match));
				}
			}

			tokens.Sort((lhs,rhs) => lhs.Span.Begin.CompareTo(rhs.Span.Begin));
			return tokens;
		}

		/// <summary>Returns a collection of the edits to make to do the aligning</summary>
		private List<Token> FindAlignments(int line_number, List<AlignGroup> grps, Span line_range)
		{
			var line  = m_snapshot.GetLineFromLineNumber(line_number);
			var caret = m_caret.Position.BufferPosition - line.Start.Position;

			// Get the align boundaries on the current line
			var boundaries = FindAlignBoundariesOnLine(line_number, grps);

			// Sort the boundaries by pattern priority, then by distance from the caret
			var ordered = boundaries.OrderBy(x => x.GrpIndex).ThenBy(x => x.Distance(caret));

			var edits = new List<Token>();

			// Find the first boundary that can be aligned
			foreach (var align in ordered)
			{
				// Each time we come round, the previous 'align' should have resulted in nothing
				// to align so edits should always be empty here
				Debug.Assert(edits.Count == 0);

				// Find the index of 'align' within 'boundaries' for 'align's pattern type
				var token_index = 0;
				foreach (var b in boundaries)
				{
					if (ReferenceEquals(b, align)) break;
					if (b.GrpIndex == align.GrpIndex) ++token_index;
				}

				// For each successive adjacent row, look for an alignment boundary at the same index
				edits.AddRange(FindAlignmentEdits(align, token_index, grps, -1, line_range));
				edits.AddRange(FindAlignmentEdits(align, token_index, grps, +1, line_range));

				// No edits to be made, means try the next alignment candidate
				if (edits.Count == 0)
					continue;

				edits.Insert(0, align);

				// If there are edits but they are all already aligned at the
				// correct column, then move on to the next candidate.
				var column = FindAlignColumn(edits);
				if (edits.All(x => x.CurrentColumnIndex == column))
				{
					edits.Clear();
					continue;
				}

				break;
			}
			return edits;
		}

		/// <summary>
		/// Searches above (dir == -1) or below (dir == +1) for alignment tokens that occur
		/// with the same token index as 'align'. Returns all found.</summary>
		private IEnumerable<Token> FindAlignmentEdits(Token align, int token_index, List<AlignGroup> grps, int dir, Span line_range)
		{
			for (var i = align.LineNumber + dir; line_range.Contains(i); i += dir)
			{
				// Get the alignment boundaries on this line
				var boundaries = FindAlignBoundariesOnLine(i, grps);

				// Look for a token that matches 'align' at 'token_index' position
				var match = (Token)null;
				var idx = -1;
				foreach (var b in boundaries)
				{
					if (b.GrpIndex != align.GrpIndex) continue;
					if (++idx != token_index) continue;
					match = b;
					break;
				}

				// No alignment boundary found, stop searching in this direction
				if (match == null)
					yield break;

				// Found an alignment boundary.
				yield return match;
			}
		}

		/// <summary>Performs the aligning using the given edits</summary>
		private void DoAligning(List<Token> edits)
		{
			// Nothing to do
			if (edits.Count == 0)
				return;

			// Create an undo scope
			using (var text = m_snapshot.TextBuffer.CreateEdit())
			{
				// Find the column to align to
				var column = FindAlignColumn(edits);

				foreach (var edit in edits)
				{
					// Delete all preceding whitespace
					text.Delete(edit.Line.Start.Position + edit.MinCharIndex, edit.Span.Begin - edit.MinCharIndex);

					// Insert whitespace to align
					var ws = column - edit.MinColumnIndex;
					Debug.Assert(ws > 0);
					text.Insert(edit.Line.Start.Position + edit.MinCharIndex, new string(' ', ws));
				}
				text.Apply();
			}
		}
	}
}

		///// <summary>Find the algin boundaries for lines on, above, and below line_number</summary>
		//private Dictionary<int, List<Token>> FindPivots(int line_number, List<Pattern> patns)
		//{
		//	var tokens = new Dictionary<int, List<Token>>();

		//	// find highest priority aligner
		//	// align on that

		//	// Check the current line, line above, and line below
		//	var above = FindAlignBoundariesOnLine(line_number - 1, patns);
		//	var curr  = FindAlignBoundariesOnLine(line_number    , patns);
		//	var below = FindAlignBoundariesOnLine(line_number + 1, patns);

		//	// Reduce these lists to a single list
		//	if curr[0] == above[0] || curr[0] == below[0] => keep
		//	else chuck curr[0]
		//	tokens.Add(line_number, root);

		//	var allow = root;

		//	// Search lines above
		//	for (var i = line_number - 1; i >= 0; --i)
		//	{
		//		var avail = FindAlignBoundariesOnLine(i, patns);
		//		avail.RemoveIf(p => !allow.Contains(p), true);
		//		if (avail.Count == 0) break;
		//		pivots.Add(i, avail);
		//		allow = avail;
		//	}

		//	allow = root;

		//	// Search lines below
		//	for (var i = line_number + 1; i < m_snapshot.LineCount; ++i)
		//	{
		//		var avail = FindAlignBoundariesOnLine(i, patns);
		//		avail.RemoveIf(p => !allow.Contains(p), true);
		//		if (avail.Count == 0) break;
		//		pivots.Add(i, avail);
		//		allow = avail;
		//	}

		//	return pivots;
		//}

		///// <summary>Return the first pivot that could potentially be aligned</summary>
		//private Token Reduce(Dictionary<int, List<Token>> pivots, int line_number)
		//{
		//	// Criteria:
		//	//  pivot not in pivots_above or pivots_below? reject
		//	//  pivot found in above or below but are already aligned
		//	return pivots[line_number].FirstOrDefault();
		//}

		//private Token Reduce(List<Token> pivots, List<Token> pivots_above, List<Token> pivots_below)
		//{
		//	for (int i = 0, iend = pivots.Count; i != iend; ++i)
		//	{
		//		var pivot = pivots[i];
		//	}
		//	foreach (var pivot in pivots)
		//	{
		//		//
		//	}
		//	return null;
		//}