using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using pr.extn;

namespace Rylogic.VSExtension
{
	internal class Align
	{
		internal class Settings
		{
			/// <summary>The patterns to align on, regex's in priority order</summary>
			public string[] AlignPatterns { get; set; }

			/// <summary>True if '+=' should be aligned along with '=' </summary>
			public bool TreatOpAssignAsAssign { get; set; }

			public Settings()
			{
				AlignPatterns = new []
				{
					// matches '=' when optionally preceded by +,-,*,/,%,&,| and not by '!' or '=', and not followed by '='
					@"((?<![!=])[\+\-\*\/\%\^\&\|]?=(?!=))", // Assignment
					@",",
					@"\(",
					@"\)",
					@"\bm_\w*", // Matches members prefixed with 'm_'
					@"{",
					@"}",
					@"-"
				};
				TreatOpAssignAsAssign = true;
			}
		}

		private class Pivot
		{
			/// <summary>The pattern to align on</summary>
			public string m_align_pattern;

			/// <summary>The column that the pattern starts on</summary>
			public int m_column;

			public Pivot(string patn, int column)
			{
				m_align_pattern = patn;
				m_column = column;
			}

			public override bool Equals(object obj) { return base.Equals(obj); }
			protected bool Equals(Pivot other)      { return string.Equals(m_align_pattern,other.m_align_pattern) && m_column == other.m_column; }
			public override int GetHashCode()       { unchecked { return ((m_align_pattern != null ? m_align_pattern.GetHashCode() : 0)*397) ^ m_column; } }
		}

		private readonly Settings m_settings;
		private readonly IWpfTextView m_view;
		private readonly ITextSnapshot m_snapshot;
		private readonly ITextCaret m_caret;

		public Align(Settings settings, IWpfTextView view)
		{
			m_settings = settings;
			m_view     = view;
			m_snapshot = m_view.TextSnapshot;
			m_caret    = m_view.Caret;

			// Get the current line number
			var line_number = m_snapshot.GetLineNumberFromPosition(m_caret.Position.BufferPosition);

			// Get the pattern to align on
			var patns = ChoosePatterns(line_number);

			// Look for alignment candidates on the current line and lines above and below
			var pivots = FindPivots(line_number, patns);

			// Look for the first pivot that could be aligned
			var pivot = Reduce(pivots, line_number);
		}

		/// <summary>Return a list of alignment patterns in priority order.</summary>
		private List<string> ChoosePatterns(int line_number)
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
					return new[]{start_line.GetText().Substring(s, e - s)}.ToList();
				}
			}

			// Otherwise, get the patterns from the settings
			var patns = m_settings.AlignPatterns.ToList();

			// If the cursor is next to an alignment pattern, move that pattern to the front of the priority list
			var longest_pattern_length = patns.Max(x => x.Length);
			var line = m_snapshot.GetLineFromLineNumber(line_number);

			// Read characters forwards and backwards until 'longest_pattern_length' non-white space chars have been read
			var adj_words = new[]
			{
				ReadWord(line.GetText(), m_caret.Position.BufferPosition - line.Start.Position, longest_pattern_length, -1),
				ReadWord(line.GetText(), m_caret.Position.BufferPosition - line.Start.Position, longest_pattern_length, +1)
			};

			// Compare the adjacent words with the patterns, if found, move those patterns to the front of the priority list
			foreach (var adj in adj_words.Where(x => x.HasValue()))
			{
				for (int i = 0, iend = patns.Count; i != iend; ++i)
				{
					var patn = patns[i];
					if (string.CompareOrdinal(patn, 0, adj, 0, patn.Length) != 0) continue;
					patns.RemoveAt(i);
					patns.Insert(0, patn);
					break;
				}
			}

			return patns;
		}

		/// <summary>Read characters forwards or backwards based on 'direction' until 'count' non-white space chars have been read</summary>
		private string ReadWord(string text, int start, int count, int direction)
		{
			var sb   = new StringBuilder();
			var i    = direction > 0 ? start : start - 1;
			var iend = direction > 0 ? text.Length : -1;
			for (; i != iend && sb.Length != count; i += direction)
			{
				if (Char.IsWhiteSpace(text[i])) continue;
				sb.Append(text[i]);
			}
			if (direction < 0) sb.Reverse();
			return sb.ToString();
		}

		/// <summary>Find the pivots for lines on, above, and below line_number</summary>
		private Dictionary<int, List<Pivot>> FindPivots(int line_number, List<string> patns)
		{
			var pivots = new Dictionary<int, List<Pivot>>();

			// Start with all pivots on 'line_number'
			var root = FindPivotsOnLine(line_number, patns);
			pivots.Add(line_number, root);

			var allow = root;

			// Search lines above
			for (var i = line_number - 1; i >= 0; --i)
			{
				var avail = FindPivotsOnLine(i, patns);
				avail.RemoveIf(p => !allow.Contains(p), true);
				if (avail.Count == 0) break;
				pivots.Add(i, avail);
				allow = avail;
			}

			allow = root;

			// Search lines below
			for (var i = line_number + 1; i < m_snapshot.LineCount; ++i)
			{
				var avail = FindPivotsOnLine(i, patns);
				avail.RemoveIf(p => !allow.Contains(p), true);
				if (avail.Count == 0) break;
				pivots.Add(i, avail);
				allow = avail;
			}

			return pivots;
		}

		/// <summary>Scan 'line' for potential alignment candidates, and return all found in priority order</summary>
		private List<Pivot> FindPivotsOnLine(int line_number, IEnumerable<string> patns)
		{
			var pivots = new List<Pivot>();

			if (line_number < 0 || line_number >= m_snapshot.LineCount)
				return pivots;

			var text = m_snapshot.GetLineFromLineNumber(line_number).GetText();
			foreach (var patn in patns)
			{
				// Find all instances of 'patn' in 'text'
				for (var i = 0; (i = text.IndexOf(patn, i, StringComparison.Ordinal)) != -1;)
				{
					pivots.Add(new Pivot(patn, i));
				}
			}

			return pivots;
		}

		/// <summary>Return the first pivot that could potentially be aligned</summary>
		private Pivot Reduce(Dictionary<int, List<Pivot>> pivots, int line_number)
		{
			// Criteria:
			//  pivot not in pivots_above or pivots_below? reject
			//  pivot found in above or below but are already aligned
			return pivots[line_number].FirstOrDefault();
		}

		private Pivot Reduce(List<Pivot> pivots, List<Pivot> pivots_above, List<Pivot> pivots_below)
		{
			for (int i = 0, iend = pivots.Count; i != iend; ++i)
			{
				var pivot = pivots[i];
			}
			foreach (var pivot in pivots)
			{
				//
			}
			return null;
		}
	}
}
