using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.common;
using pr.gui;
using pr.maths;

namespace RyLogViewer
{
	public partial class Main
	{
		//
		public static void ExportToFile(StartupOptions startup_options)
		{
			// todo

			// Create a temporary settings file so that we don't trash the normal one

			// refactor the export function so we can call the worker thread not in a progress dialog

			// add a 'no gui' option to allow uses to not show the export progress dialog
			var m = new Main(startup_options);

		}

		/// <summary>Show the export dialog</summary>
		private void ShowExportDialog()
		{
			if (!FileOpen) return;
			var dg = new ExportUI(
				Path.ChangeExtension(m_filepath, ".exported"+Path.GetExtension(m_filepath)),
				Misc.Humanise(m_encoding.GetString(m_row_delim)),
				Misc.Humanise(m_encoding.GetString(m_col_delim)),
				FileByteRange);
			if (dg.ShowDialog(this) != DialogResult.OK) return;
			
			Range rng;
			switch (dg.RangeToExport)
			{
			default: throw new ArgumentOutOfRangeException();
			case ExportUI.ERangeToExport.WholeFile: rng = FileByteRange; break;
			case ExportUI.ERangeToExport.Selection: rng = SelectedRowRange; break;
			case ExportUI.ERangeToExport.ByteRange: rng = dg.ByteRange; break;
			}
			
			// Do the export
			using (var outp = new StreamWriter(new FileStream(dg.OutputFilepath, FileMode.Create, FileAccess.Write, FileShare.Read)))
			{
				try
				{
					if (ExportLogFile(outp, m_filepath, rng, dg.ColDelim, dg.RowDelim))
						MessageBox.Show(this, Resources.ExportCompletedSuccessfully, Resources.ExportComplete, MessageBoxButtons.OK);
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, string.Format("Export failed.\r\nError: {0}",ex.Message), Resources.ExportFailed, MessageBoxButtons.OK);
				}
			}
		}

		/// <summary>
		/// Export the file 'filepath' using current filters to the stream 'outp'.
		/// Note: this method throws if an exception occurs in the background thread.
		/// </summary>
		/// <param name="outp">The stream to write the exported file to</param>
		/// <param name="filepath">The name of the file to export</param>
		/// <param name="rng">The range of bytes within 'filepath' to be exported</param>
		/// <param name="col_delim">The string to delimit columns with. (CR,LF,TAB converted to \r,\n,\t respectively)</param>
		/// <param name="row_delim">The string to delimit rows with. (CR,LF,TAB converted to \r,\n,\t respectively)</param>
		private bool ExportLogFile(StreamWriter outp, string filepath, Range rng, string col_delim, string row_delim)
		{
			// Although this search runs in a background thread, it's wrapped in a modal
			// dialog box, so it should be ok to use class members directly
			ProgressForm export = new ProgressForm("Exporting...", "", (s,a)=>
				{
					BackgroundWorker bgw = (BackgroundWorker)s;
					using (var file = LoadFile(filepath))
					{
						Line line             = new Line();
						int last_progress     = 0;
						rng.Begin             = Maths.Clamp(rng.Begin, 0, file.Length);
						rng.End               = Maths.Clamp(rng.End, 0, file.Length);
						row_delim             = Misc.Robitise(row_delim);
						col_delim             = Misc.Robitise(col_delim);
						bool ignore_blanks    = m_settings.IgnoreBlankLines;
						List<Filter> filters  = ActiveFilters.ToList();
						AddLineFunc test_line = (line_rng, baddr, fend, bf, enc) =>
							{
								int progress = (int)(100 * Maths.Ratio(rng.Begin, baddr + line_rng.End, rng.End));
								if (progress != last_progress) { bgw.ReportProgress(progress); last_progress = progress; }
								
								if (line_rng.Empty && ignore_blanks)
									return true;
								
								// Parse the line from the buffer
								line.Read(baddr + line_rng.Begin, bf, (int)line_rng.Begin, (int)line_rng.Count, m_encoding, m_col_delim, null, m_transforms);
								
								// Keep searching while the text is filtered out or doesn't match the pattern
								if (!PassesFilters(line.RowText, filters)) return true;
								
								// Write to the output file
								outp.Write(string.Join(col_delim, line.Column));
								outp.Write(row_delim);
								return true;
							};
						
						byte[] buf = new byte[m_settings.MaxLineLength];
						
						// Find the start of a line (grow the range if necessary)
						rng.Begin = FindLineStart(file, rng.Begin, rng.End, m_row_delim, m_encoding, buf);
						
						// Read lines and write them to the export file
						FindLines(file, rng.Begin, rng.End, false, rng.Count, test_line, m_encoding, m_row_delim, buf, (c,l) => !bgw.CancellationPending);
						a.Cancel = bgw.CancellationPending;
					}
				}){StartPosition = FormStartPosition.CenterParent};
			
			DialogResult res = DialogResult.Cancel;
			try { res = export.ShowDialog(this); }
			catch (OperationCanceledException) {}
			catch (Exception ex) { MessageBox.Show(this, "Exporting terminated due to an error.\r\nError Details:\r\n"+ex.Message, "Export error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
			return res == DialogResult.OK;
		}
	}
}
