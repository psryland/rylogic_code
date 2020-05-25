using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.TextAligner
{
	internal class Unalign
	{
		private readonly IWpfTextView m_view;
		private readonly ITextSnapshot m_snapshot;
		private readonly int m_tab_size;

		public Unalign(IWpfTextView view)
		{
			m_view     = view;
			m_snapshot = m_view.TextSnapshot;
			m_tab_size = m_view.Options.GetOptionValue(DefaultOptions.TabSizeOptionId);

			// Get the current line number, line, line position, and selection
			var selection = new Selection(m_view);

			// Unalign text
			DoUnaligning(selection);
		}

		/// <summary>Performs the aligning using the given edits</summary>
		private void DoUnaligning(Selection selection)
		{
			// Create an undo scope
			using var text = m_snapshot.TextBuffer.CreateEdit();

			// The character used to identify literal strings.
			// Null when not within a literal string.
			char? quote = null;

			// Process lines in reverse order
			var line_range = selection.Lines;
			for (int j = line_range.Endi; j >= line_range.Begi; --j)
			{
				var line = text.Snapshot.GetLineFromLineNumber(j);
				var str = line.GetText();

				// Process text within each line in reverse order
				int s = line.Length, e = line.Length;
				for (int i = line.Length; i-- != 0;)
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
	