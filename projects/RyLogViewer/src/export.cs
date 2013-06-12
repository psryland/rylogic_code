using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.common;
using pr.gui;
using pr.maths;
using pr.util;

namespace RyLogViewer
{
	public partial class Main
	{
		/// <summary>Performs an export from the command line</summary>
		public static void ExportToFile(StartupOptions startup_options)
		{
			string tmp_settings_path = Path.Combine(Path.GetTempPath(), "rylog_settings_"+Guid.NewGuid()+".xml");
			try
			{
				// Copy the settings to a tmp file so that we don't trash the normal settings
				if (File.Exists(startup_options.SettingsPath))
					File.Copy(startup_options.SettingsPath, tmp_settings_path);
				else
					new Settings().Save(tmp_settings_path);
				startup_options.SettingsPath = tmp_settings_path;
				
				// Load an instance of the app.
				var m = new Main(startup_options);
				
				// Do the export
				using (var outp = new StreamWriter(new FileStream(startup_options.ExportPath, FileMode.Create, FileAccess.Write, FileShare.Read)))
				{
					try
					{
						string filepath = startup_options.FileToLoad;
						Range rng = new Range(0, long.MaxValue);
						string row_delimiter = Misc.Robitise(startup_options.RowDelim ?? m.m_settings.RowDelimiter);
						string col_delimiter = Misc.Robitise(startup_options.ColDelim ?? m.m_settings.ColDelimiter);
						
						if (startup_options.NoGUI)
						{
							using (var done = new ManualResetEvent(false))
							{
								ThreadPool.QueueUserWorkItem(x =>
									{
										// ReSharper disable AccessToDisposedClosure
										m.DoExport(filepath, rng, row_delimiter, col_delimiter, outp, (c,l)=>true);
										done.Set();
										// ReSharper restore AccessToDisposedClosure
									});
								
								done.WaitOne();
								Console.WriteLine(Resources.ExportCompletedSuccessfully);
							}
						}
						else
						{
							if (m.DoExportWithProgress(filepath ,rng ,row_delimiter ,col_delimiter, outp))
								Console.WriteLine(Resources.ExportCompletedSuccessfully);
						}
					}
					catch (Exception ex)
					{
						Environment.ExitCode = 1;
						Console.WriteLine(string.Format("Export failed.\r\nError: {0}",ex.Message));
					}
				}
			}
			finally
			{
				if (File.Exists(tmp_settings_path))
					File.Delete(tmp_settings_path);
			}
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
			
			string row_delimiter = Misc.Robitise(dg.RowDelim);
			string col_delimiter = Misc.Robitise(dg.ColDelim);
			
			// Do the export
			using (var outp = new StreamWriter(new FileStream(dg.OutputFilepath, FileMode.Create, FileAccess.Write, FileShare.Read)))
			{
				try
				{
					if (DoExportWithProgress(m_filepath, rng, row_delimiter, col_delimiter, outp))
						MessageBox.Show(this, Resources.ExportCompletedSuccessfully, Resources.ExportComplete, MessageBoxButtons.OK);
				}
				catch (Exception ex)
				{
					Log.Exception(this, ex, "Export failed");
					MessageBox.Show(this, string.Format("Export failed.\r\nError: {0}",ex.Message), Resources.ExportFailed, MessageBoxButtons.OK);
				}
			}
		}

		/// <summary>
		/// Export the file 'filepath' using current filters to the stream 'outp'.
		/// Note: this method throws if an exception occurs in the background thread.
		/// </summary>
		/// <param name="filepath">The name of the file to export</param>
		/// <param name="rng">The range of bytes within 'filepath' to be exported</param>
		/// <param name="row_delimiter">The delimiter that defines rows (robitised)</param>
		/// <param name="col_delimiter">The delimiter that defines columns (robitised)</param>
		/// <param name="outp">The stream to write the exported file to</param>
		private bool DoExportWithProgress(string filepath, Range rng, string row_delimiter, string col_delimiter, StreamWriter outp)
		{
			// Although this search runs in a background thread, it's wrapped in a modal
			// dialog box, so it should be ok to use class members directly
			ProgressForm export = new ProgressForm("Exporting...", "", (s,a)=>
				{
					BackgroundWorker bgw = (BackgroundWorker)s;
					
					// Report progress and test for cancel
					int last_progress = -1;
					ProgressFunc report_progress = (scanned, length) =>
						{
							int progress = (int)(100 * Maths.Frac(0, scanned, length));
							if (progress != last_progress) { bgw.ReportProgress(progress); last_progress = progress; }
							return !bgw.CancellationPending;
						};
					
					// Do the export
					DoExport(filepath ,rng ,row_delimiter ,col_delimiter ,outp ,report_progress);
					a.Cancel = bgw.CancellationPending;
				}){StartPosition = FormStartPosition.CenterParent};
			
			DialogResult res = DialogResult.Cancel;
			try { res = export.ShowDialog(this); }
			catch (OperationCanceledException) {}
			catch (Exception ex) { Misc.ShowErrorMessage(this, ex, "Exporting terminated due to an error.", "Export error"); }
			return res == DialogResult.OK;
		}

		/// <summary>Export 'filepath' to 'outp'. This method uses the 'm_settings' object, so should
		/// only be called from a background thread, if the main thread is effectively blocked.</summary>
		/// <param name="filepath">The filepath of the input file to perform the export on</param>
		/// <param name="rng">The byte range within the input file to export</param>
		/// <param name="row_delimiter">The row delimiter to use in the output file (robitised)</param>
		/// <param name="col_delimiter">The column delimiter to use in the output file (robitised)</param>
		/// <param name="outp">The output stream to write the exported result to</param>
		/// <param name="report_progress">Callback function for reporting progress and detecting cancel</param>
		private void DoExport(string filepath, Range rng, string row_delimiter, string col_delimiter, StreamWriter outp, ProgressFunc report_progress)
		{
			using (var file = LoadFile(filepath))
			{
				Line line = new Line();
				
				rng.Begin = Maths.Clamp(rng.Begin, 0, file.Length);
				rng.End   = Maths.Clamp(rng.End  , 0, file.Length);
				bool ignore_blanks = m_settings.IgnoreBlankLines;
				List<Filter>    ft_list = m_filters;
				List<Transform> tx_list = m_transforms;
				
				// Call back for adding lines to the export result
				AddLineFunc add_line = (line_rng, baddr, fend, bf, enc) =>
					{
						if (line_rng.Empty && ignore_blanks)
							return true;
						
						// Parse the line from the buffer
						line.Read(baddr + line_rng.Begin, bf, (int)line_rng.Begin, (int)line_rng.Count, m_encoding, m_col_delim, null, tx_list);
						
						// Keep searching while the text is filtered out or doesn't match the pattern
						if (!PassesFilters(line.RowText, ft_list)) return true;
						
						// Write to the output file
						outp.Write(string.Join(col_delimiter, line.Column));
						outp.Write(row_delimiter);
						return true;
					};
				
				byte[] buf = new byte[m_settings.MaxLineLength];
				
				// Find the start of a line (grow the range if necessary)
				rng.Begin = FindLineStart(file, rng.Begin, rng.End, m_row_delim, m_encoding, buf);
				
				// Read lines and write them to the export file
				FindLines(file, rng.Begin, rng.End, false, rng.Count, add_line, m_encoding, m_row_delim, buf, report_progress);
			}
		}
	}
}
