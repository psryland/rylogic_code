using System;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace RyLogViewer
{
	public partial class Main
	{
		private readonly BindingSource m_find_history;

		/// <summary>Initialise the app's find search support</summary>
		private void InitFind()
		{
			// When the find events are fired on m_find_ui, trigger find next/prev
			m_find_ui.FindNext += FindNext;
			m_find_ui.FindPrev += FindPrev;
		}

		/// <summary>Show the find dialog</summary>
		private void ShowFindDialog()
		{
			// Initialise the find string from the selected row
			int row_index = SelectedRowIndex;
			if (row_index != -1)
			{
				var row_text = ReadLine(row_index).RowText.Trim();
				m_find_ui.Pattern = new Pattern(EPattern.Substring, row_text);
			}

			// Display the find window
			m_find_ui.Display();
		}

		/// <summary>Update the current find pattern to the text from row 'row_index'</summary>
		private void SetFindPattern(int row_index, bool find_next)
		{
			if (row_index == -1) return;
			var row_text = ReadLine(row_index).RowText.Trim();
			m_find_ui.Pattern = new Pattern(EPattern.Substring, row_text);
			if (find_next) FindNext(false);
			else           FindPrev(false);
		}

		/// <summary>Prepare to execute the find command. Returns true if the find should execute</summary>
		private bool PreFind()
		{
			// If there's nothing in the grid, don't do a find
			if (m_grid.RowCount == 0)
				return false;

			// Check if m_find_ui has a valid find pattern
			if (m_find_ui.Pattern.Expr.Length == 0 || !m_find_ui.Pattern.IsValid)
				return false;

			var pattern = new Pattern(m_find_ui.Pattern);

			// Remove any patterns with the same expr as 'pattern'
			m_find_history.RemoveIf<Pattern>(x => string.CompareOrdinal(x.Expr, pattern.Expr) == 0);
			m_find_history.Insert(0, pattern);
			m_find_history.Position = 0;

			// Cap the length of the find history
			while (m_find_history.Count > Constants.MaxFindHistory)
				m_find_history.RemoveAt(m_find_history.Count - 1);

			// Continue with the find
			return true;
		}

		/// <summary>Search for the next occurrence of a pattern in the file</summary>
		private void FindNext(bool from_start)
		{
			if (!PreFind())
				return;

			var start = from_start ? FileByteRange.Begin : SelectedRowByteRange.End;
			Log.Info(this, "FindNext starting from {0}".Fmt(start));

			long found;
			if (Find(m_find_ui.Pattern, start, false, out found) && found == -1)
				SetTransientStatusMessage("End of file", Color.Azure, Color.Blue);
		}

		/// <summary>Search for an earlier occurrence of a pattern in the grid</summary>
		private void FindPrev(bool from_end)
		{
			if (!PreFind())
				return;

			var start = from_end ? FileByteRange.End : SelectedRowByteRange.Begin;
			Log.Info(this, "FindPrev starting from {0}".Fmt(start));

			long found;
			if (Find(m_find_ui.Pattern, start, true, out found) && found == -1)
				SetTransientStatusMessage("Start of file", Color.Azure, Color.Blue);
		}

		/// <summary>Searches the file from 'start' looking for a match to 'pat'</summary>
		/// <returns>Returns true if a match is found, false otherwise. If true
		/// is returned 'found' contains the file byte offset of the first match</returns>
		private bool Find(Pattern pat, long start, bool backward, out long found)
		{
			var body = backward
				? (start == FileByteRange.Begin
					? "Searching forward from the start of the file..."
					: "Searching forward from the current selection position...")
				: (start == FileByteRange.End
					? "Searching backward from the end of the file..."
					: "Searching backward from the current selection position...");

			// Although this search runs in a background thread, it's wrapped in a modal
			// dialog box, so it should be ok to use class members directly
			long at = -1;
			var search = new ProgressForm("Searching...", body, null, ProgressBarStyle.Marquee, (s,a,cb)=>
				{
					var d = new BLIData(this, m_file);
					int last_progress = 0;
					d.progress = (scanned, length) =>
						{
							int progress = (int)(100 * Maths.Frac(0,scanned,length!=0?length:1));
							if (progress != last_progress)
							{
								cb(new ProgressForm.UserState{FractionComplete = progress * 0.01f});
								last_progress = progress;
							}
							return !s.CancelPending;
						};

					// Searching....
					at = DoFind(pat, start, backward, d);

					// We can call BuildLineIndex in this thread context because we know
					// we're in a modal dialog.
					if (at != -1 && !s.CancelPending)
					{
						this.BeginInvoke(() => SelectRowByAddr(at));
					}
				}){StartPosition = FormStartPosition.CenterParent};

			DialogResult res = DialogResult.Cancel;
			try { res = search.ShowDialog(this, 500); }
			catch (OperationCanceledException) {}
			catch (Exception ex) { Misc.ShowErrorMessage(this, ex, "Find terminated by an error.", "Find error", MessageBoxIcon.Error); }
			found = at;
			return res == DialogResult.OK;
		}

		/// <summary>
		/// Does the donkey work of searching for a pattern.
		/// Returns the byte address of the first match.</summary>
		private static long DoFind(Pattern pat, long start, bool backward, BLIData d)
		{
			long at = -1;
			using (d.file)
			{
				AddLineFunc test_line = (line, baddr, fend, buf, enc) =>
					{
						// Ignore blanks?
						if (line.Empty && d.ignore_blanks)
							return true;

						// Keep searching while the text is filtered out or doesn't match the pattern
						string text = d.encoding.GetString(buf, (int)line.Begin, (int)line.Count);
						if (!PassesFilters(text, d.filters) || !pat.IsMatch(text))
							return true;

						// Found a match
						at = baddr + line.Begin;
						return false; // Stop searching
					};

				// Search for files
				var line_buf = new byte[d.max_line_length];
				long count = backward ? start - 0 : d.fileend - start;
				FindLines(d.file, start, d.fileend, backward, count, test_line, d.encoding, d.row_delim, line_buf, d.progress);
				return at;
			}
		}
	}
}
