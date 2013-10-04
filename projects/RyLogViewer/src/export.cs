using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.common;
using pr.extn;
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
			string tmp_settings_path = Path.Combine(Path.GetTempPath(), "rylog_settings_" + Guid.NewGuid() + ".xml");
			try
			{
				// Copy the settings to a tmp file so that we don't trash the normal settings
				if (PathEx.FileExists(startup_options.SettingsPath))
					File.Copy(startup_options.SettingsPath, tmp_settings_path);
				else
					new Settings().Save(tmp_settings_path);
				startup_options.SettingsPath = tmp_settings_path;

				// Load an instance of the app and the options.
				var m = new Main(startup_options);

				// Do the export
				using (var outp = new StreamWriter(new FileStream(startup_options.ExportPath, FileMode.Create, FileAccess.Write, FileShare.Read)))
				{
					try
					{
						var d = new BLIData(m, new SingleFile(startup_options.FileToLoad));
						using (d.file)
						{
							var rng = new[] { new Range(0, long.MaxValue) };
							string row_delimiter = Misc.Robitise(startup_options.RowDelim ?? m.m_settings.RowDelimiter);
							string col_delimiter = Misc.Robitise(startup_options.ColDelim ?? m.m_settings.ColDelimiter);

							if (startup_options.NoGUI)
							{
								using (var done = new ManualResetEvent(false))
								{
									ThreadPool.QueueUserWorkItem(x =>
										{
											// ReSharper disable AccessToDisposedClosure
											d.progress = (c,l) => true;
											DoExport(d, rng, row_delimiter, col_delimiter, outp);
											done.Set();
											// ReSharper restore AccessToDisposedClosure
										});

									done.WaitOne();
									Console.WriteLine(Resources.ExportCompletedSuccessfully);
								}
							}
							else
							{
								if (m.DoExportWithProgress(d, rng, row_delimiter, col_delimiter, outp))
									Console.WriteLine(Resources.ExportCompletedSuccessfully);
							}
						}
					}
					catch (Exception ex)
					{
						Environment.ExitCode = 1;
						Console.WriteLine("Export failed.\r\nError: {0}", ex.Message);
					}
				}
			}
			finally
			{
				if (PathEx.FileExists(tmp_settings_path))
					File.Delete(tmp_settings_path);
			}
		}

		/// <summary>Show the export dialog</summary>
		private void ShowExportDialog()
		{
			if (!FileOpen) return;
			var dg = new ExportUI(
				Path.ChangeExtension(m_file.PsuedoFilepath, ".exported" + Path.GetExtension(m_file.PsuedoFilepath)),
				Misc.Humanise(m_encoding.GetString(m_row_delim)),
				Misc.Humanise(m_encoding.GetString(m_col_delim)),
				FileByteRange);
			if (dg.ShowDialog(this) != DialogResult.OK) return;

			IEnumerable<Range> rng;
			switch (dg.RangeToExport)
			{
			default: throw new ArgumentOutOfRangeException();
			case ExportUI.ERangeToExport.WholeFile: rng = new[] { FileByteRange }; break;
			case ExportUI.ERangeToExport.Selection: rng = SelectedRowRanges; break;
			case ExportUI.ERangeToExport.ByteRange: rng = new[] { dg.ByteRange }; break;
			}

			string row_delimiter = Misc.Robitise(dg.RowDelim);
			string col_delimiter = Misc.Robitise(dg.ColDelim);

			// Do the export
			using (var outp = new StreamWriter(new FileStream(dg.OutputFilepath, FileMode.Create, FileAccess.Write, FileShare.Read)))
			{
				try
				{
					var d = new BLIData(this, m_file);
					if (DoExportWithProgress(d, rng, row_delimiter, col_delimiter, outp))
						MsgBox.Show(this, Resources.ExportCompletedSuccessfully, Resources.ExportComplete, MessageBoxButtons.OK);
				}
				catch (Exception ex)
				{
					Log.Exception(this, ex, "Export failed");
					MsgBox.Show(this, string.Format("Export failed.\r\nError: {0}", ex.Message), Resources.ExportFailed, MessageBoxButtons.OK);
				}
			}
		}

		/// <summary>
		/// Export the file 'filepath' using current filters to the stream 'outp'.
		/// Note: this method throws if an exception occurs in the background thread.</summary>
		/// <param name="d">A copy of the data needed to do the export</param>
		/// <param name="ranges">Byte ranges within 'filepath' to be exported</param>
		/// <param name="row_delimiter">The delimiter that defines rows (robitised)</param>
		/// <param name="col_delimiter">The delimiter that defines columns (robitised)</param>
		/// <param name="outp">The stream to write the exported file to</param>
		private bool DoExportWithProgress(BLIData d, IEnumerable<Range> ranges, string row_delimiter, string col_delimiter, StreamWriter outp)
		{
			// Although this search runs in a background thread, it's wrapped in a modal
			// dialog box, so it should be ok to use class members directly
			var export = new ProgressForm("Exporting...", null, null, ProgressBarStyle.Continuous, (s, a, cb) =>
				{
					// Report progress and test for cancel
					int last_progress = -1;
					d.progress = (scanned, length) =>
						{
							int progress = (int)(100 * Maths.Frac(0, scanned, length!=0?length:1));
							if (progress != last_progress)
							{
								cb(new ProgressForm.UserState { FractionComplete = progress * 0.01f });
								last_progress = progress;
							}
							return !s.CancelPending;
						};

					// Do the export
					DoExport(d, ranges, row_delimiter, col_delimiter, outp);
				}) { StartPosition = FormStartPosition.CenterParent };

			DialogResult res = DialogResult.Cancel;
			try { res = export.ShowDialog(this); }
			catch (OperationCanceledException) { }
			catch (Exception ex) { Misc.ShowMessage(this, "Exporting terminated due to an error.", "Export error", MessageBoxIcon.Error, ex); }
			return res == DialogResult.OK;
		}

		/// <summary>Export 'filepath' to 'outp'. This method uses the 'm_settings' object, so should
		/// only be called from a background thread, if the main thread is effectively blocked.</summary>
		/// <param name="d">A copy of the data needed to do the export</param>
		/// <param name="ranges">Byte ranges within the input file to export</param>
		/// <param name="row_delimiter">The row delimiter to use in the output file (robitised)</param>
		/// <param name="col_delimiter">The column delimiter to use in the output file (robitised)</param>
		/// <param name="outp">The output stream to write the exported result to</param>
		private static void DoExport(BLIData d, IEnumerable<Range> ranges, string row_delimiter, string col_delimiter, StreamWriter outp)
		{
			var line = new Line();

			// Call back for adding lines to the export result
			AddLineFunc add_line = (line_rng, baddr, fend, bf, enc) =>
				{
					if (line_rng.Empty && d.ignore_blanks)
						return true;

					// Parse the line from the buffer
					line.Read(baddr + line_rng.Begin, bf, (int)line_rng.Begin, (int)line_rng.Count, d.encoding, d.col_delim, null, d.transforms);

					// Keep searching while the text is filtered out or doesn't match the pattern
					if (!PassesFilters(line.RowText, d.filters)) return true;

					// Write to the output file
					outp.Write(string.Join(col_delimiter, line.Column));
					outp.Write(row_delimiter);
					return true;
				};

			byte[] buf = new byte[d.max_line_length];
			foreach (var rng in ranges)
			{
				// Find the start of a line (grow the range if necessary)
				var r = new Range(Maths.Clamp(rng.Begin, 0, d.file.Stream.Length), Maths.Clamp(rng.End, 0, d.file.Stream.Length));
				r.Begin = FindLineStart(d.file, r.Begin, r.End, d.row_delim, d.encoding, buf);

				// Read lines and write them to the export file
				FindLines(d.file, r.Begin, r.End, false, r.Count, add_line, d.encoding, d.row_delim, buf, d.progress);
			}
		}
	}
}
