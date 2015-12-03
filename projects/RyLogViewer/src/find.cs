using System;
using System.Drawing;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace RyLogViewer
{
	public partial class Main
	{
		private readonly BindingSource m_find_history;

		/// <summary>Set up the app's find search support</summary>
		private void SetupFind()
		{
			// When the find events are fired on m_find_ui, trigger find next/prev
			m_find_ui.FindNext += FindNext;
			m_find_ui.FindPrev += FindPrev;
			m_find_ui.BookmarkAll += FindBookmarkAll;
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
			m_find_ui.Show();
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

			// Remove any patterns with the same expression as 'pattern'
			m_find_history.RemoveIf<Pattern>(x => string.CompareOrdinal(x.Expr, pattern.Expr) == 0);
			m_find_history.Insert(0, pattern);
			m_find_history.Position = 0;
			m_find_history.ResetBindings(false);

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
				? (start == FileByteRange.End
					? "Searching backward from the end of the file..."
					: "Searching backward from the current selection position...")
				: (start == FileByteRange.Begin
					? "Searching forward from the start of the file..."
					: "Searching forward from the current selection position...");

			// Although this search runs in a background thread, it's wrapped in a modal
			// dialog box, so it should be ok to use class members directly
			long at = -1;
			var search = new ProgressForm("Searching...", body, null, ProgressBarStyle.Marquee, (s,a,cb)=>
				{
					var d = new BLIData(this, m_file, fileend_:m_fileend);
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
					DoFind(pat, start, backward, d, rng =>
						{
							at = rng.Begin;
							return false;
						});

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
			catch (Exception ex) { Misc.ShowMessage(this, "Find terminated by an error.", "Find error", MessageBoxIcon.Error, ex); }
			found = at;
			return res == DialogResult.OK;
		}

		/// <summary>Searches the entire file and bookmarks all locations that match the find pattern</summary>
		private void FindBookmarkAll()
		{
			if (!PreFind())
				return;

			try
			{
				Log.Info(this, "FindBookmarkAll");

				var pat = m_find_ui.Pattern;
				const string body = "Bookmarking all found instances...";

				// Although this search runs in a background thread, it's wrapped in a modal
				// dialog box, so it should be ok to use class members directly
				var search = new ProgressForm("Searching...", body, null, ProgressBarStyle.Marquee, (s,a,cb)=>
					{
						var d = new BLIData(this, m_file, fileend_:m_fileend);

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
						DoFind(pat, 0, false, d, rng =>
							{
								this.BeginInvoke(() => SetBookmark(rng, Bit.EState.Set));
								return true;
							});
					}){StartPosition = FormStartPosition.CenterParent};

				search.ShowDialog(this, 500);
			}
			catch (OperationCanceledException) {}
			catch (Exception ex)
			{
				Misc.ShowMessage(this, "Find terminated by an error.", "Find error", MessageBoxIcon.Error, ex);
			}
		}

		/// <summary>Does the donkey work of searching for a pattern.
		/// Returns the byte address of the first match.</summary>
		private static void DoFind(Pattern pat, long start, bool backward, BLIData d, Func<Range, bool> on_found)
		{
			using (d.file)
			{
				var line = new Line();
				AddLineFunc test_line = (line_rng, baddr, fend, bf, enc) =>
					{
						// Ignore blanks?
						if (line_rng.Empty && d.ignore_blanks)
							return true;

						// Parse the line from the buffer
						line.Read(baddr + line_rng.Begin, bf, (int)line_rng.Begin, (int)line_rng.Size, d.encoding, d.col_delim, null, d.transforms);

						// Keep searching while the text is filtered out or doesn't match the pattern
						if (!PassesFilters(line.RowText, d.filters) || !pat.IsMatch(line.RowText))
							return true;

						// Found a match
						return on_found(new Range(baddr + line_rng.Begin, baddr + line_rng.End));
					};

				// Search for files
				var line_buf = new byte[d.max_line_length];
				long count = backward ? start - 0 : d.fileend - start;
				FindLines(d.file, start, d.fileend, backward, count, test_line, d.encoding, d.row_delim, line_buf, d.progress);
			}
		}
	}
}
