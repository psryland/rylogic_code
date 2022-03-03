using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;
using Rylogic.Utility;

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
				if (Path_.FileExists(startup_options.SettingsPath))
					File.Copy(startup_options.SettingsPath, tmp_settings_path);
				else
					new Settings().Save(tmp_settings_path);
				startup_options.SettingsPath = tmp_settings_path;

				// Load an instance of the app and the options.
				var m = new Main(startup_options);

				// Override settings passed on the command line
				if (startup_options.RowDelim != null) m.Settings.RowDelimiter = startup_options.RowDelim;
				if (startup_options.ColDelim != null) m.Settings.ColDelimiter = startup_options.ColDelim;
				if (startup_options.PatternSetFilepath != null)
				{
					// Specifying a pattern set implies the filters and transforms should be enabled
					m.Settings.Patterns = PatternSet.Load(startup_options.PatternSetFilepath);
					m.Settings.FiltersEnabled = true;
					m.Settings.TransformsEnabled = true;
				}

				// Do the export
				using (var outp = new StreamWriter(new FileStream(startup_options.ExportPath, FileMode.Create, FileAccess.Write, FileShare.Read)))
				{
					try
					{
						var d = new BLIData(m, new SingleFile(startup_options.FileToLoad));
						using (d.file)
						{
							var rng = new[] { new RangeI(0, long.MaxValue) };
							var row_delimiter = Misc.Robitise(m.Settings.RowDelimiter);
							var col_delimiter = Misc.Robitise(m.Settings.ColDelimiter);

							if (startup_options.NoGUI)
							{
								using (var done = new ManualResetEvent(false))
								{
									ThreadPool.QueueUserWorkItem(x =>
									{
										d.progress = (c,l) => true;
										DoExport(d, rng, row_delimiter, col_delimiter, outp);
										done.Set();
									});

									done.WaitOne();
									if (!startup_options.Silent)
										Console.WriteLine("Export completed successfully.");
								}
							}
							else
							{
								if (m.DoExportWithProgress(d, rng, row_delimiter, col_delimiter, outp))
									if (!startup_options.Silent)
										Console.WriteLine("Export completed successfully.");
							}
						}
					}
					catch (Exception ex)
					{
						Environment.ExitCode = 1;
						if (!startup_options.Silent)
							Console.WriteLine($"Export failed.\r\n{ex.Message}");
					}
				}
			}
			finally
			{
				if (Path_.FileExists(tmp_settings_path))
					File.Delete(tmp_settings_path);
			}
		}

		/// <summary>Show the export dialog</summary>
		private void ShowExportDialog()
		{
			if (Src == null)
				return;

			// Determine the export file path
			var filepath = Settings.ExportFilepath;
			if (!filepath.HasValue())
				filepath = Path.ChangeExtension(Src.PsuedoFilepath, ".exported" + Path.GetExtension(Src.PsuedoFilepath));

			// Prompt for export settings
			var dlg = new ExportUI(filepath,
				Misc.Humanise(m_encoding.GetString(m_row_delim)),
				Misc.Humanise(m_encoding.GetString(m_col_delim)),
				FileByteRange);
			using (dlg)
			{
				if (dlg.ShowDialog(this) != DialogResult.OK)
					return;

				// Save the export filepath to the settings
				Settings.ExportFilepath = dlg.OutputFilepath;

				// Find the range to export
				IEnumerable<RangeI> rng;
				switch (dlg.RangeToExport)
				{
				default: throw new ArgumentOutOfRangeException();
				case ExportUI.ERangeToExport.WholeFile: rng = new[] { FileByteRange }; break;
				case ExportUI.ERangeToExport.Selection: rng = SelectedRowRanges; break;
				case ExportUI.ERangeToExport.ByteRange: rng = new[] { dlg.ByteRange }; break;
				}

				// Delimiters
				var row_delimiter = Misc.Robitise(dlg.RowDelim);
				var col_delimiter = Misc.Robitise(dlg.ColDelim);

				// Do the export
				using (var outp = new StreamWriter(new FileStream(dlg.OutputFilepath, FileMode.Create, FileAccess.Write, FileShare.Read)))
				{
					try
					{
						var d = new BLIData(this, Src);
						if (DoExportWithProgress(d, rng, row_delimiter, col_delimiter, outp))
							MsgBox.Show(this, "Export completed successfully.", Application.ProductName, MessageBoxButtons.OK);
					}
					catch (Exception ex)
					{
						Log.Write(ELogLevel.Error, ex, "Export failed");
						MsgBox.Show(this, string.Format("Export failed.\r\n{0}", ex.Message), Application.ProductName, MessageBoxButtons.OK);
					}
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
		private bool DoExportWithProgress(BLIData d, IEnumerable<RangeI> ranges, string row_delimiter, string col_delimiter, StreamWriter outp)
		{
			DialogResult res = DialogResult.Cancel;
			try
			{
				// Although this search runs in a background thread, it's wrapped in a modal
				// dialog box, so it should be ok to use class members directly
				var export = new ProgressForm("Exporting...", null, null, ProgressBarStyle.Continuous, (s, a, cb) =>
				{
					// Report progress and test for cancel
					int last_progress = -1;
					d.progress = (scanned, length) =>
					{
						int progress = (int)(100 * Math_.Frac(0, scanned, length!=0?length:1));
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

				using (export)
					res = export.ShowDialog(this);
			}
			catch (OperationCanceledException) { }
			catch (Exception ex) { Misc.ShowMessage(this, "Exporting terminated due to an error.", "Export error", MessageBoxIcon.Error, ex); }
			return res == DialogResult.OK;
		}

		/// <summary>Export 'filepath' to 'outp'.</summary>
		/// <param name="d">A copy of the data needed to do the export</param>
		/// <param name="ranges">Byte ranges within the input file to export</param>
		/// <param name="row_delimiter">The row delimiter to use in the output file (robitised)</param>
		/// <param name="col_delimiter">The column delimiter to use in the output file (robitised)</param>
		/// <param name="outp">The output stream to write the exported result to</param>
		private static void DoExport(BLIData d, IEnumerable<RangeI> ranges, string row_delimiter, string col_delimiter, StreamWriter outp)
		{
			var line = new Line();

			// Call back for adding lines to the export result
			AddLineFunc add_line = (line_rng, baddr, fend, bf, enc) =>
			{
				if (line_rng.Empty && d.ignore_blanks)
					return true;

				// Parse the line from the buffer
				line.Read(baddr + line_rng.Beg, bf, (int)line_rng.Beg, (int)line_rng.Size, d.encoding, d.col_delim, null, d.transforms);

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
				var r = new RangeI(Math_.Clamp(rng.Beg, 0, d.file.Stream.Length), Math_.Clamp(rng.End, 0, d.file.Stream.Length));
				r.Beg = FindLineStart(d.file, r.Beg, r.End, d.row_delim, d.encoding, buf);

				// Read lines and write them to the export file
				FindLines(d.file, r.Beg, r.End, false, r.Size, add_line, d.encoding, d.row_delim, buf, d.progress);
			}
		}
	}
}
