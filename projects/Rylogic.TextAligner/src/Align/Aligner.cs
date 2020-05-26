using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.TextAligner
{
	internal class Aligner
	{
		private readonly IWpfTextView m_view;
		private readonly ITextSnapshot m_snapshot;
		private readonly List<AlignGroup> m_groups;
		private readonly EAlignCharacters m_style;
		private readonly EAction m_action;
		private readonly int m_tab_size;

		public Aligner(IEnumerable<AlignGroup> groups, EAlignCharacters style, IWpfTextView view, EAction action)
		{
			m_view     = view;
			m_groups   = groups.ToList();
			m_snapshot = m_view.TextSnapshot;
			m_tab_size = m_view.Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
			m_action   = action;
			m_style    = style;

			// Get the current line number, line, line position, and selection
			var selection = new Selection(m_view);

			// Prioritise the pattern groups to align on
			var grps = PrioritisePatterns(selection);

			// Find the edits to make
			var edits = FindAlignments(selection, grps, out var fallback_line_span);

			// Make the alignment edits
			if (edits.Count != 0)
			{
				switch (action)
				{
				case EAction.Align:
					DoAligning(edits);
					break;
				case EAction.Unalign:
					DoUnaligning(edits);
					break;
				}
			}
			else if (m_action == EAction.Unalign)
			{
				// If there is no selection then use the 'fallback_line_span' if available. This provides behaviour that the user
				// would otherwise interpret as a bug. Unalignment, when an alignment pattern is used, removes trailing whitespace
				// from all rows affected by the aligning. When there is nothing left to unalign, the user would still expected
				// trailing whitespace to be removed from a block that would otherwise be (un)aligned.
				var line_range = (selection.IsEmpty && fallback_line_span != null)
					? fallback_line_span.Value : selection.Lines;

				UnalignSelection(line_range);
			}
		}

		/// <summary>Return a list of alignment patterns in priority order.</summary>
		private List<AlignGroup> PrioritisePatterns(Selection selection)
		{
			// If there is a selection, see if we should use the selected text at the align pattern
			// Only use single line, non-whole-line selections on the same line as the caret
			var pattern_selection = !selection.IsEmpty && selection.IsSingleLine && !selection.IsWholeLines;
			if (pattern_selection)
			{
				var s = selection.Pos.Begi - selection.SLine.Start.Position;
				var e = selection.Pos.Endi - selection.SLine.Start.Position;
				var text = selection.SLine.GetText();
				var expr = text.Substring(s, Math.Min(e - s, text.Length - s));
				var ofs = expr.TakeWhile(char.IsWhiteSpace).Count(); // Count leading whitespace
				expr = expr.Substring(ofs);                          // Strip leading whitespace
				return new[]{new AlignGroup("Selection", 0, new AlignPattern(EPattern.Substring, expr, ofs))}.ToList();
			}

			// Make a copy of the groups
			var groups = m_groups.ToList();

			// If the cursor is next to an alignment pattern, move that pattern to the front of the priority list.
			// Only do this when there isn't a multi line selection as the 'near-pattern' behaviour is confusing.
			if (selection.IsEmpty)
			{
				var line = selection.SLine;
				var line_text = line.GetText();
				var column = selection.Pos.Begi - line.Start.Position;
				Debug.Assert(column >= 0 && column <= (line.End.Position - line.Start.Position));

				var spanning = (AlignGroup?)null;
				var rightof = (AlignGroup?)null;
				var leftof = (AlignGroup?)null;

				// Find matches that span, are immediately to the right, or immediately to the left (priority order)
				for (var i = 0; i != groups.Count; ++i)
				{
					var grp = groups[i];
					foreach (var pat in grp.Patterns)
					foreach (var match in pat.AllMatches(line_text))
					{
						// Spanning matches have the highest priority
						if (match.Beg < column && match.End > column)
							spanning = grp;

						// Matches to the right separated only by whitespace are the next highest priority
						if (match.Beg >= column && string.IsNullOrWhiteSpace(line_text.Substring(column, match.Begi - column)))
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
			}

			return m_groups;
		}

		/// <summary>Returns a collection of the edits to make to do the aligning</summary>
		private List<Token> FindAlignments(Selection selection, List<AlignGroup> grps, out RangeI? fallback_line_span)
		{
			var line = m_snapshot.GetLineFromLineNumber(selection.CaretLineNumber);
			var caret = selection.CaretPos - line.Start.Position;
			fallback_line_span = null;

			// If the selection spans multiple lines, limit the aligning to those lines.
			// If a whole single line is selected, treat that like multiple selected lines.
			// It means aligning won't do anything, but I think that's what a user would expect,
			// consistent with selecting more than 1 line.
			var line_range = selection.IsSingleLine && !selection.IsWholeLines
				? new RangeI(0, m_snapshot.LineCount - 1)
				: selection.Lines;

			// Get the align boundaries on the current line
			var boundaries = FindAlignBoundariesOnLine(selection.CaretLineNumber, grps);

			// Sort the boundaries by pattern priority, then by distance from the caret
			var ordered = m_action switch
				{
					EAction.Align => boundaries.OrderBy(x => x.GrpIndex).ThenBy(x => x.CurrentCharIndex),
					EAction.Unalign => boundaries.OrderByDescending(x => x.GrpIndex).ThenBy(x => x.CurrentCharIndex),
					_ => throw new Exception($"Unknown alignment action: {m_action}"),
				};

			// Find the first boundary that can be aligned
			var edits = new List<Token>();
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

				var pos = FindAlignColumn(edits, 0);
				var all_first_on_line = edits.All(x => x.MinColumnIndex == 0);
				var col = all_first_on_line ? align.CurrentColumnIndex - align.Patn.Offset : pos.Column - pos.Span.Begi;

				switch (m_action)
				{
				// For aligning we want the highest priority group that is not already aligned.
				case EAction.Align:
					{
						// If there are edits but they are all already aligned at the
						// correct column, then move on to the next candidate.
						var already_aligned = edits.All(x => x.CurrentColumnIndex - x.Patn.Offset == col);
						if (already_aligned)
						{
							edits.Clear();
							continue;
						}
						break;
					}

				// For unaligning we want the lowest priority group that is not currently 'unaligned'.
				case EAction.Unalign:
					{
						// If there are edits but they are all already the leading space distance
						// from their minimum column, then they are all 'unaligned', move on to the next candidate.
						var already_unaligned = edits.All(x => x.CurrentColumnIndex - x.MinColumnIndex == x.Grp.LeadingSpace);
						if (already_unaligned)
						{
							// Determine the range of lines spanned by the highest priority group
							fallback_line_span = RangeI.From(edits.Select(x => x.LineNumber));
							edits.Clear();
							continue;
						}
						break;
					}
				}

				// Found the best match
				break;
			}
			return edits;
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
						tokens.Add(new Token(grp, grp_index, patn_index, line, line_number, match, m_tab_size));
				}
			}

			tokens.Sort((lhs,rhs) => lhs.Span.Beg.CompareTo(rhs.Span.Beg));
			return tokens;
		}

		/// <summary>
		/// Searches above (dir == -1) or below (dir == +1) for alignment tokens that occur
		/// with the same token index as 'align'. Returns all found.</summary>
		private IEnumerable<Token> FindAlignmentEdits(Token align, int token_index, List<AlignGroup> grps, int dir, RangeI line_range)
		{
			for (var i = align.LineNumber + dir; line_range.ContainsInclusive(i); i += dir)
			{
				// Get the alignment boundaries on this line
				var boundaries = FindAlignBoundariesOnLine(i, grps);

				// Look for a token that matches 'align' at 'token_index' position
				var match = (Token?)null;
				var idx = -1;
				foreach (var b in boundaries)
				{
					if (b.GrpIndex != align.GrpIndex) continue;
					if (++idx != token_index) continue;
					if ((b.MinCharIndex == 0) != (align.MinCharIndex == 0)) break; // Don't align things on their own line, with things that aren't on their own line
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

		/// <summary>Returns the column index and range for aligning</summary>
		private AlignPos FindAlignColumn(IEnumerable<Token> toks, int min_column)
		{
			var leading_ws = 0;
			var span = RangeI.Zero; // include 0 in the range
			var all_line_starts = true;
			foreach (var tok in toks)
			{
				span.Encompass(tok.Patn.Position);
				all_line_starts &= tok.MinColumnIndex == 0;
				min_column = Math.Max(min_column, tok.MinColumnIndex);
				leading_ws = Math.Max(leading_ws, tok.Grp.LeadingSpace);
			}

			// Add the leading whitespace to the minimum column, unless the
			// alignment is the first non-whitespace character on the line.
			if (!all_line_starts)
			{
				min_column += leading_ws;

				// Round up to the next tab boundary
				if (m_style == EAlignCharacters.Tabs)
					min_column = Util.PadTo(min_column, m_tab_size);
			}
			return new AlignPos(min_column, span);
		}

		/// <summary>Performs the aligning using the given edits</summary>
		private void DoAligning(List<Token> edits)
		{
			// The first edit is the line that the aligning is based on, if the token we're aligning
			// to is the first thing on the line don't align to column zero, leave the leading whitespace as is
			var min_column = edits[0].MinColumnIndex != 0 ? 0 : edits[0].CurrentColumnIndex;
			var leading_ws = edits[0].Line.GetText().Substring(0, edits[0].CurrentCharIndex);

			// Create an undo scope
			using var text = m_snapshot.TextBuffer.CreateEdit();

			// Sort in descending line order so that the edits are applied from end to start,
			// meaning character index positions aren't invalidated as each line is edited.
			edits.Sort((l, r) => r.LineNumber.CompareTo(l.LineNumber));

			// Find the column to align to
			var pos = FindAlignColumn(edits, min_column);
			var col = pos.Column - pos.Span.Begi;
			Debug.Assert(pos.Span.Begi <= 0, "0 should be included in the span");

			// Align each line to 'pos'
			foreach (var edit in edits)
			{
				// Careful with order, this order is choosen so that edits are
				// applied from right to left, preventing indices being invalidated.

				// Insert whitespace after the pattern if needed
				var ws_tail = Math.Max(0, pos.Span.Endi - (edit.Patn.Offset + edit.Span.Sizei));
				if (ws_tail > 0)
				{
					// Always use space characters for trailing padding
					var ins = edit.Line.Start.Position + edit.Span.Endi;
					text.Insert(ins, new string(' ', ws_tail));
				}

				// Delete all preceding whitespace
				text.Delete(edit.Line.Start.Position + edit.MinCharIndex, edit.Span.Begi - edit.MinCharIndex);

				// Create the aligning whitespace based on the alignment style
				string ws = string.Empty;
				if (min_column != 0)
				{
					// Copy the whitespace from the aligning line
					ws = leading_ws;
				}
				else
				{
					switch (m_style)
					{
					case EAlignCharacters.Spaces:
						{
							// In 'spaces' mode, simply pad from MinColumnIndex to 'col', adjusting for offset
							var count = col - edit.MinColumnIndex + edit.Patn.Offset;
							ws = new string(' ', count);
							break;
						}
					case EAlignCharacters.Tabs:
						{
							// In 'tabs' mode, the alignment column 'col' will be a multiple of the tab size.
							// Add tabs between 'MinColumnIndex' and 'col', then adjust by 'Offset' using spaces if needed.
							// Note: Don't simplify, 'tab_count' relies on integer truncation here.
							var tab_count = ((col - edit.MinColumnIndex + m_tab_size - 1) / m_tab_size) + (edit.Patn.Offset / m_tab_size);
							var spc_count = (m_tab_size + edit.Patn.Offset % m_tab_size) % m_tab_size;
							ws = new string('\t', tab_count) + new string(' ', spc_count);
							break;
						}
					case EAlignCharacters.Mixed:
						{
							// In 'mixed' mode. the alignment column 'col' will be the same as for 'spaces' mode.
							// Insert tabs up to the nearest tab boundary, then spaces after that.
							// Note: Don't simplify, 'tab_count' relies on integer truncation here.
							var tab_count = (col / m_tab_size) - (edit.MinColumnIndex / m_tab_size) + (edit.Patn.Offset / m_tab_size);
							var spc_count = (col + edit.Patn.Offset) % m_tab_size;
							if (tab_count == 0) spc_count -= (edit.MinColumnIndex % m_tab_size);
							ws = new string('\t', tab_count) + new string(' ', spc_count);
							break;
						}
					default:
						{
							throw new Exception($"Unsupported whitespace style: {m_style}");
						}
					}
				}

				// Insert whitespace to align
				if (ws.Length != 0)
				{
					var ins = edit.Line.Start.Position + edit.MinCharIndex;
					text.Insert(ins, ws);
				}
			}
			text.Apply();
		}

		/// <summary>Performs the aligning using the given edits</summary>
		private void DoUnaligning(List<Token> edits)
		{
			// The first edit is the line that the aligning is based on, if the token we're aligning
			// to is the first thing on the line don't align to column zero, leave the leading whitespace as is
			var min_column = edits[0].MinColumnIndex != 0 ? 0 : edits[0].CurrentColumnIndex;
			var leading_ws = edits[0].Line.GetText().Substring(0, edits[0].CurrentCharIndex);

			// Create an undo scope
			using var text = m_snapshot.TextBuffer.CreateEdit();

			// Sort in descending line order so that the edits are applied from end to start,
			// meaning character index positions aren't invalidated as each line is edited.
			edits.Sort((l, r) => r.LineNumber.CompareTo(l.LineNumber));

			// 'Unalign' each line prior to the alignment group
			foreach (var edit in edits)
			{
				// Trim trailing whitespace
				var str = edit.Line.GetText();
				var i = str.Length;
				for (; i-- != 0 && char.IsWhiteSpace(str[i]);) { }
				if (++i != str.Length)
					text.Delete(edit.Line.Start.Position + i, str.Length - i);

				// Delete the white space before the alignment pattern.
				// Insert a single character if the pattern doesn't have leading white space, and isn't the start of a line
				text.Delete(edit.Line.Start.Position + edit.MinCharIndex, edit.CurrentCharIndex - edit.MinCharIndex);
				if (edit.Grp.LeadingSpace != 0 && edit.MinCharIndex != 0)
					text.Insert(edit.Line.Start.Position + edit.MinCharIndex, " ");
			}
			text.Apply();
		}

		/// <summary>Unalign selected text</summary>
		private void UnalignSelection(RangeI line_range)
		{
			// Create an undo scope
			using var text = m_snapshot.TextBuffer.CreateEdit();

			// The character used to identify literal strings.
			// Null when not within a literal string.
			char? quote = null;

			// Process lines in reverse order
			for (int j = line_range.Endi; j >= line_range.Begi; --j)
			{
				var line = text.Snapshot.GetLineFromLineNumber(j);
				var str = line.GetText();

				// Process text within each line in reverse order
				int s = line.Length, e = s;
				for (int i = s; i-- != 0;)
				{
					if (quote != null)
					{
						// If currently within a literal string...
						// Search for the starting quote (ignoring escaped quotes)
						if (str[i] == quote.Value && (i == 0 || str[i - 1] != '\\'))
							quote = null; // not within a string any more

						s = e = i;
					}
					else if (!char.IsWhiteSpace(str[i]))
					{
						// Replace consecutive white-space with a single whitespace character
						if (s != e)
						{
							// Remove all white space if at the end of a line,
							// otherwise replace with a single whitespace character
							text.Delete(line.Start.Position + s, e - s);
							if (e != line.Length)
								text.Insert(line.Start.Position + s, " ");
						}

						// Found the end of a literal string
						if (quote == null && str[i] == '"' || str[i] == '\'')
							quote = str[i];

						s = e = i;
					}
					else
					{
						// Consecutive whitespace
						s = i;
					}
				}

				// If the whole line is white space (and not part of a literal string), delete all of it
				if (e == line.Length && quote == null)
					text.Delete(line.Start.Position, e - s);
			}

			// Apply the edits
			text.Apply();
		}
	}
}
