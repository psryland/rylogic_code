﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using pr.common;
using pr.extn;
using pr.script;

namespace Rylogic.VSExtension
{
	internal class Align
	{
		private class Token
		{
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
			public Range Span { get; private set; }

			/// <summary>The minimum distance of this token from 'caret_pos'</summary>
			public int Distance(int caret_pos) { return (int)(Span.Contains(caret_pos) ? 0 : Math.Min(Math.Abs(Span.Begin - caret_pos), Math.Abs(Span.End - caret_pos))); }

			/// <summary>The minimum character index that this token can be left shifted to</summary>
			public int MinCharIndex { get; private set; }

			/// <summary>The minimum column index that this token can be left shifted to</summary>
			public int MinColumnIndex { get; private set; }

			/// <summary>The current column index of the token</summary>
			public int CurrentColumnIndex { get; private set; }

			public Token(AlignGroup grp, int grp_index, int patn_index, ITextSnapshotLine line, int line_number, Range span, int tab_size)
			{
				Grp        = grp;
				GrpIndex   = grp_index;
				Patn       = grp.Patterns[patn_index];
				Span       = span;
				Line       = line;
				LineNumber = line_number;

				var line_text  = Line.GetText();
				int i; for (i = (int)(Span.Begin - 1); i >= 0 && char.IsWhiteSpace(line_text[i]); --i) {} ++i;

				MinCharIndex       = i;
				MinColumnIndex     = CharIndexToColumnIndex(line_text, MinCharIndex, tab_size);
				CurrentColumnIndex = CharIndexToColumnIndex(line_text, (int)Span.Begin, tab_size);
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
		private Range FindLineRange()
		{
			// If there is a selection that spans multiple lines, limit the aligning to those lines
			var selection = m_view.Selection;
			if (!selection.IsEmpty)
			{
				var start_line_number = m_snapshot.GetLineNumberFromPosition(selection.Start.Position);
				var end_line_number   = m_snapshot.GetLineNumberFromPosition(selection.End.Position);
				Debug.Assert(start_line_number <= end_line_number);
				if (start_line_number != end_line_number)
					return new Range(start_line_number, end_line_number);
			}
			return new Range(0, m_snapshot.LineCount);
		}

		private struct AlignPos
		{
			///<summary>The column index to align to</summary>
			public readonly int Column;

			/// <summary>The range of characters around the align column</summary>
			public readonly Range Span;

			public AlignPos(int column, Range span) { Column = column; Span = span; }
		}

		/// <summary>Returns the column index and range for aligning</summary>
		private AlignPos FindAlignColumn(IEnumerable<Token> toks)
		{
			var min_column = 0;
			var leading_ws = 0;
			var span = Range.Zero; // include 0 in the range
			foreach (var tok in toks)
			{
				span.Encompase(tok.Patn.Position);
				min_column  = Math.Max(min_column, tok.MinColumnIndex);
				leading_ws |= Math.Max(leading_ws, tok.Grp.LeadingSpace);
			}
			if (min_column != 0) min_column += leading_ws; // Add leading whitespace, unless at column 0
			return new AlignPos(min_column, span);
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
					var start_line_text = start_line.GetText();
					var s = selection.Start.Position - start_line.Start.Position;
					var e = selection.End.Position   - start_line.Start.Position;
					var expr = start_line_text.Substring(s, e - s);
					var ofs = expr.TakeWhile(char.IsWhiteSpace).Count(); // Count leading whitespace
					expr = expr.Substring(ofs);                          // Strip leading whitespace
					return new[]{new AlignGroup("Selection", 0, new AlignPattern(EPattern.Substring, expr, ofs))}.ToList();
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
					if (match.Begin >= column && string.IsNullOrWhiteSpace(line_text.Substring(column, match.Begini - column)))
						rightof = grp;

					// Matches to the left separated only by whitespace are next
					if (match.End <= column && string.IsNullOrWhiteSpace(line_text.Substring(match.Endi, column - match.Endi)))
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
			var tab_size = m_view.Options.GetOptionValue(DefaultOptions.TabSizeOptionId);

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
						tokens.Add(new Token(grp, grp_index, patn_index, line, line_number, match, tab_size));
				}
			}

			tokens.Sort((lhs,rhs) => lhs.Span.Begin.CompareTo(rhs.Span.Begin));
			return tokens;
		}

		/// <summary>Returns a collection of the edits to make to do the aligning</summary>
		private List<Token> FindAlignments(int line_number, List<AlignGroup> grps, Range line_range)
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

				// Record whether this aligner is within a string or not
//todo...maybe				var within_string = CodeUtil.IsWithinString(line.GetText(), align.Span.Begini);

				// For each successive adjacent row, look for an alignment boundary at the same index
				edits.AddRange(FindAlignmentEdits(align, token_index, grps, -1, line_range));
				edits.AddRange(FindAlignmentEdits(align, token_index, grps, +1, line_range));

				// No edits to be made, means try the next alignment candidate
				if (edits.Count == 0)
					continue;

				edits.Insert(0, align);

				// If there are edits but they are all already aligned at the
				// correct column, then move on to the next candidate.
				var column_index = FindAlignColumn(edits).Column;
				if (edits.All(x => x.CurrentColumnIndex - x.Patn.Offset == column_index))
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
		private IEnumerable<Token> FindAlignmentEdits(Token align,int token_index,List<AlignGroup> grps,int dir,Range line_range)
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
					if ((b.MinCharIndex == 0) != (align.MinCharIndex == 0)) continue; // Don't align things on their own line, with things that aren't on their own line
					if (++idx != token_index) continue;
//todo...maybe		if (CodeUtil.IsWithinString(b.Line.GetText(), b.Span.Begini) != within_string) break;
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
			using (ITextEdit text = m_snapshot.TextBuffer.CreateEdit())
			{
				// Sort in descending line order
				edits.Sort((l,r) => r.LineNumber.CompareTo(l.LineNumber));

				// Find the column to align to
				var pos = FindAlignColumn(edits);
				var col = pos.Column - pos.Span.Begini;
				Debug.Assert(pos.Span.Begini <= 0, "0 should be included in the span");

				foreach (var edit in edits)
				{
					var ws_head = col - edit.MinColumnIndex + edit.Patn.Position.Begini;
					var ws_tail = pos.Span.Endi - edit.Patn.Position.Endi;

					// Careful with order, we need to apply the edits assuming 'line' isn't changed with each one

					// Insert whitespace after the pattern if needed
					if (ws_tail > 0)
						text.Insert(edit.Line.Start.Position + edit.Span.Endi, new string(' ', ws_tail));

					// Delete all preceding whitespace
					text.Delete(edit.Line.Start.Position + edit.MinCharIndex, edit.Span.Begini - edit.MinCharIndex);

					// Insert whitespace to align
					if (ws_head > 0)
						text.Insert(edit.Line.Start.Position + edit.MinCharIndex, new string(' ', ws_head));
				}
				text.Apply();
			}
		}
	}
}