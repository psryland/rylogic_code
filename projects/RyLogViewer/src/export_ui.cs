using System;
using System.IO;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.common;
using pr.extn;
using pr.gui;
using pr.util;

namespace RyLogViewer
{
	public partial class ExportUI :Form
	{
		public enum ERangeToExport
		{
			WholeFile,
			Selection,
			ByteRange
		}

		public ExportUI(string output_filepath, string row_delim, string col_delim, Range byte_range)
		{
			InitializeComponent();
			var m_tt = new ToolTip();
			string tt;

			// Output file
			tt = "The path of the file to export to";
			m_edit_output_filepath.TextChanged += UpdateUI;
			m_edit_output_filepath.Text = output_filepath;
			m_edit_output_filepath.ToolTip(m_tt, tt);
			m_lbl_output_file.ToolTip(m_tt, tt);

			// Browse button
			m_btn_browse.ToolTip(m_tt, "Browse to the location of where to save the file");
			m_btn_browse.Click += (s,a)=>
				{
					var filter = Util.FileDialogFilter("Text Files","*.txt","Comma Separated Values","*.csv","All Files","*.*");
					var dg = new SaveFileDialog{Title = Resources.ChooseOutputFileName, CheckPathExists = true, OverwritePrompt = false, Filter = filter, AddExtension = true};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					m_edit_output_filepath.Text = dg.FileName;
				};

			// Radio buttons
			m_radio_whole_file.Checked = true;
			m_radio_whole_file.CheckedChanged += UpdateUI;
			m_radio_whole_file.ToolTip(m_tt, "Export the entire file contents");

			m_radio_selection.Checked = false;
			m_radio_selection.CheckedChanged += UpdateUI;
			m_radio_selection.ToolTip(m_tt, "Export the selected rows only");

			m_radio_range.Checked = false;
			m_radio_range.CheckedChanged += UpdateUI;
			m_radio_range.ToolTip(m_tt, "Export a specific byte range within the file.\r\nThe byte range will automatically be expanded to start at the beginning of a line");

			// Line ending
			tt = "The characters to end each line in the exported file.\r\nUse '<CR>', '<LF>', or '<TAB>' for carriage return, line feed, or tab respectively";
			m_lbl_line_ending.ToolTip(m_tt, tt);
			m_edit_line_ending.ToolTip(m_tt, tt);
			m_edit_line_ending.Text = row_delim;

			// Column delimiter
			tt = "The characters to use to separate columns in the exported file.\r\nUse '<CR>', '<LF>', or '<TAB>' for carriage return, line feed, or tab respectively";
			m_lbl_col_delim.ToolTip(m_tt, tt);
			m_edit_col_delim.ToolTip(m_tt, tt);
			m_edit_col_delim.Text = col_delim;

			// Byte Range
			m_spinner_range_min.ToolTip(m_tt, "The start of the data range (in bytes)");
			m_spinner_range_min.Minimum = byte_range.Begin;
			m_spinner_range_min.Maximum = byte_range.End;
			m_spinner_range_min.Value = byte_range.Begin;

			m_spinner_range_max.ToolTip(m_tt, "The end of the data range (in bytes)");
			m_spinner_range_max.Minimum = byte_range.Begin;
			m_spinner_range_max.Maximum = byte_range.End;
			m_spinner_range_max.Value = byte_range.End;
			m_btn_range_to_start.Click += (s,a) => m_spinner_range_min.Value = byte_range.Begin;
			m_btn_range_to_end  .Click += (s,a) => m_spinner_range_max.Value = byte_range.End;

			UpdateUI();
		}

		/// <summary>Validate on shutdown</summary>
		protected override void OnFormClosing(FormClosingEventArgs a)
		{
			base.OnFormClosing(a);

			if (a.CloseReason != CloseReason.None && a.CloseReason != CloseReason.UserClosing) return;
			if (DialogResult != DialogResult.OK) return;

			// Don't allow the export button without a valid filepath
			if (!OutputFilepath.HasValue())
			{
				MsgBox.Show(this, Resources.OutputFileMissingMsg, Resources.OutputFileMissing, MessageBoxButtons.OK, MessageBoxIcon.Hand);
				a.Cancel = true;
			}

			// Prompt if overwriting a file
			if (Path_.FileExists(OutputFilepath))
			{
				var res = MsgBox.Show(this, string.Format("{0} already exists. Overwrite it?", OutputFilepath), Resources.ConfirmOverwrite, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
				a.Cancel = res != DialogResult.Yes;
			}
		}

		/// <summary>The file to create</summary>
		public string OutputFilepath
		{
			get { return m_edit_output_filepath.Text; }
		}

		/// <summary>What to export</summary>
		public ERangeToExport RangeToExport
		{
			get
			{
				if (m_radio_range.Checked) return ERangeToExport.ByteRange;
				if (m_radio_selection.Checked) return ERangeToExport.Selection;
				return ERangeToExport.WholeFile;
			}
		}

		/// <summary>The row delimiter string</summary>
		public string RowDelim
		{
			get { return m_edit_line_ending.Text; }
		}

		/// <summary>The column delimiter string</summary>
		public string ColDelim
		{
			get { return m_edit_col_delim.Text; }
		}

		/// <summary>If RangeToExport is byte range, export this range</summary>
		public Range ByteRange
		{
			get { return new Range((long)m_spinner_range_min.Value, (long)m_spinner_range_max.Value); }
		}

		/// <summary>Update the enabled state of UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			m_btn_range_to_start.Enabled = m_radio_range.Checked;
			m_btn_range_to_end  .Enabled = m_radio_range.Checked;
			m_spinner_range_min .Enabled = m_radio_range.Checked;
			m_spinner_range_max .Enabled = m_radio_range.Checked;
			m_btn_ok.Enabled = Path_.IsValidFilepath(OutputFilepath, false);
		}
	}
}
