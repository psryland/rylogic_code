using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.gui;
using pr.maths;
using pr.util;

namespace RyLogViewer
{
	public partial class Main
	{
		private Pattern m_last_find_pattern; // The pattern last used in a find
		
		/// <summary>Show the find dialog</summary>
		private void ShowFindDialog()
		{
			// Initialise the find string from the selected row
			int init_row = SelectedRow;
			if (init_row != -1)
				m_find_ui.Pattern = ReadLine(init_row).RowText;
			
			// Display the find window
			m_find_ui.Display();
		}
		
		/// <summary>Searches the file from 'start' looking for a match to 'pat'</summary>
		/// <returns>Returns true if a match is found, false otherwise. If true
		/// is returned 'found' contains the file byte offset of the first match</returns>
		private bool Find(Pattern pat, long start, bool backward, out long found)
		{
			// Although this search runs in a background thread, it's wrapped in a modal
			// dialog box, so it should be ok to use class members directly
			
			long at = -1;
			ProgressForm search = new ProgressForm("Searching...", "", (s,a)=>
				{
					BackgroundWorker bgw = (BackgroundWorker)s;
					
					using (var file = LoadFile(m_filepath))
					{
						int last_progress     = 0;
						bool ignore_blanks    = m_settings.IgnoreBlankLines;
						List<Filter> filters  = ActiveFilters.ToList();
						AddLineFunc test_line = (line, baddr, fend, bf, enc) =>
							{
								int progress = backward
									? (int)(100 * (1f - Maths.Ratio(0, baddr + line.Begin, start)))
									: (int)(100 * Maths.Ratio(start, baddr + line.End ,m_fileend));
								if (progress != last_progress) { bgw.ReportProgress(progress); last_progress = progress; }
								
								// Ignore blanks?
								if (line.Empty && ignore_blanks)
									return true;
								
								// Keep searching while the text is filtered out or doesn't match the pattern
								string text = m_encoding.GetString(bf, (int)line.Begin, (int)line.Count);
								if (!PassesFilters(text, filters) || !pat.IsMatch(text))
									return true;
								
								// Found a match
								at = baddr + line.Begin;
								return false; // Stop searching
							};
						
						// Search for files
						byte[] buf = new byte[m_settings.MaxLineLength];
						long count = backward ? start - 0 : m_fileend - start;
						FindLines(file, start, m_fileend, backward, count, test_line, m_encoding, m_row_delim, buf, (c,l) => !bgw.CancellationPending);
						
						// We can call BuildLineIndex in this thread context because we know
						// we're in a modal dialog.
						if (at != -1)
						{
							Action select = ()=>SelectRowByAddr(at);
							Invoke(select);
						}
						
						a.Cancel = bgw.CancellationPending;
					}
				}){StartPosition = FormStartPosition.CenterParent};
			
			DialogResult res = DialogResult.Cancel;
			try
			{
				m_last_find_pattern = pat;
				res = search.ShowDialog(this);
			}
			catch (OperationCanceledException) {}
			catch (Exception ex) { Misc.ShowErrorMessage(this, ex, "Find terminated by an error.", "Find error"); }
			found = at;
			return res == DialogResult.OK;
		}
		
		/// <summary>Search for the next occurrence of a pattern in the file</summary>
		private void FindNext(Pattern pat)
		{
			if (pat == null || m_grid.RowCount == 0) return;
			
			var start = m_line_index[SelectedRow].End;
			Log.Info(this, "FindNext starting from {0}", start);
			
			long found;
			if (Find(pat, start, false, out found) && found == -1)
				SetTransientStatusMessage("End of file", Color.Azure, Color.Blue);
		}
		
		/// <summary>Search for an earlier occurrence of a pattern in the grid</summary>
		private void FindPrev(Pattern pat)
		{
			if (pat == null || m_grid.RowCount == 0) return;
			
			var start = SelectedRowRange.Begin;
			Log.Info(this, "FindPrev starting from {0}", start);
			
			long found;
			if (Find(pat, start, true, out found) && found == -1)
				SetTransientStatusMessage("Start of file", Color.Azure, Color.Blue);
		}
	}
}
