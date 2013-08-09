using System.IO;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.common;
using pr.extn;

namespace RyLogViewer
{
	public partial class ExportUI :Form
	{
		private readonly ToolTip m_tt;
		
		/// <summary></summary>
		public enum ERangeToExport
		{
			WholeFile,
			Selection,
			ByteRange
		}
		
		/// <summary>The file to create</summary>
		public string OutputFilepath;
		
		/// <summary>What to export</summary>
		public ERangeToExport RangeToExport;
		
		/// <summary>The row delimiter string</summary>
		public string RowDelim;
		
		/// <summary>The column delimiter string</summary>
		public string ColDelim;
		
		/// <summary>If RangeToExport is byte range, export this range</summary>
		public Range ByteRange;

		public ExportUI(string output_filepath, string row_delim, string col_delim, Range byte_range)
		{
			InitializeComponent();
			m_tt           = new ToolTip();
			OutputFilepath = output_filepath;
			RangeToExport  = ERangeToExport.WholeFile;
			RowDelim       = row_delim;
			ColDelim       = col_delim;
			ByteRange      = byte_range;
			string tt;
			
			// Output file
			tt = "The path of the file to export to";
			m_lbl_output_file.ToolTip(m_tt, tt);
			m_edit_output_filepath.ToolTip(m_tt, tt);
			m_edit_output_filepath.Text = OutputFilepath;
			m_edit_output_filepath.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					OutputFilepath = m_edit_output_filepath.Text;
				};
			
			// Browse button
			m_btn_browse.ToolTip(m_tt, "Browse to the location of where to save the file");
			m_btn_browse.Click += (s,a)=>
				{
					SaveFileDialog dg = new SaveFileDialog{Title = Resources.ChooseOutputFileName, CheckPathExists = true, OverwritePrompt = false, Filter = Resources.LogFileFilter};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					m_edit_output_filepath.Text = dg.FileName;
				};
			
			// Radio buttons
			m_radio_whole_file.ToolTip(m_tt, "Export the entire file contents");
			m_radio_whole_file.Checked = RangeToExport == ERangeToExport.WholeFile;
			m_radio_whole_file.CheckedChanged += (s,a)=>
				{
					RangeToExport = ERangeToExport.WholeFile;
					UpdateUI();
				};
			m_radio_selection.ToolTip(m_tt, "Export the selected rows only");
			m_radio_selection.Checked = RangeToExport == ERangeToExport.Selection;
			m_radio_selection.CheckedChanged += (s,a)=>
				{
					RangeToExport = ERangeToExport.Selection;
					UpdateUI();
				};
			m_radio_range.ToolTip(m_tt, "Export a specific byte range within the file.\r\nThe byte range will automatically be expanded to start at the beginning of a line");
			m_radio_range.Checked = RangeToExport == ERangeToExport.ByteRange;
			m_radio_range.CheckedChanged += (s,a)=>
				{
					RangeToExport = ERangeToExport.ByteRange;
					UpdateUI();
				};
			
			// Line ending
			tt = "The characters to end each line in the exported file.\r\nUse '<CR>', '<LF>', or '<TAB>' for carriage return, line feed, or tab respectively";
			m_lbl_line_ending.ToolTip(m_tt, tt);
			m_edit_line_ending.ToolTip(m_tt, tt);
			m_edit_line_ending.Text = RowDelim;
			m_edit_line_ending.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					RowDelim = m_edit_line_ending.Text;
				};
			
			// Column delimiter
			tt = "The characters to use to separate columns in the exported file.\r\nUse '<CR>', '<LF>', or '<TAB>' for carriage return, line feed, or tab respectively";
			m_lbl_col_delim.ToolTip(m_tt, tt);
			m_edit_col_delim.ToolTip(m_tt, tt);
			m_edit_col_delim.Text = ColDelim;
			m_edit_col_delim.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					ColDelim = m_edit_col_delim.Text;
				};
			
			// Byte Range
			m_btn_range_to_start.Click += (s,a)=>
				{
					m_spinner_range_min.Value = ByteRange.Begin;
				};
			m_btn_range_to_end.Click += (s,a)=>
				{
					m_spinner_range_max.Value = ByteRange.End;
				};
			m_spinner_range_min.ToolTip(m_tt, "The start of the byte range (in bytes)");
			m_spinner_range_min.Minimum = ByteRange.Begin;
			m_spinner_range_min.Maximum = ByteRange.End;
			m_spinner_range_min.Value = ByteRange.Begin;
			m_spinner_range_min.ValueChanged += (s,a)=>
				{
					ByteRange.Begin = (long)m_spinner_range_min.Value;
				};
			m_spinner_range_max.ToolTip(m_tt, "The end of the byte range (in bytes)");
			m_spinner_range_max.Minimum = ByteRange.Begin;
			m_spinner_range_max.Maximum = ByteRange.End;
			m_spinner_range_max.Value = ByteRange.End;
			m_spinner_range_max.ValueChanged += (s,a)=>
				{
					ByteRange.End = (long)m_spinner_range_max.Value;
				};
			
			// Validate on shutdown
			FormClosing += (s,a)=>
				{
					if (a.CloseReason != CloseReason.None && a.CloseReason != CloseReason.UserClosing) return;
					if (DialogResult != DialogResult.OK) return;

					// Don't allow the export button without a valid filepath
					if (string.IsNullOrEmpty(OutputFilepath))
					{
						MessageBox.Show(this, Resources.OutputFileMissingMsg, Resources.OutputFileMissing, MessageBoxButtons.OK, MessageBoxIcon.Hand);
						a.Cancel = true;
					}
					
					// Prompt if overwriting a file
					if (File.Exists(OutputFilepath))
					{
						DialogResult res = MessageBox.Show(this, string.Format("{0} already exists. Overwrite it?", OutputFilepath), Resources.ConfirmOverwrite, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
						a.Cancel = res != DialogResult.Yes;
					}
				};

			Disposed += (s,a) =>
				{
					m_tt.Dispose();
				};

			UpdateUI();
		}

		/// <summary>Update the enabled state of UI elements</summary>
		private void UpdateUI()
		{
			bool byte_range = RangeToExport == ERangeToExport.ByteRange;
			m_edit_output_filepath.Text  = OutputFilepath;
			m_btn_range_to_start.Enabled = byte_range;
			m_btn_range_to_end  .Enabled = byte_range;
			m_spinner_range_min .Enabled = byte_range;
			m_spinner_range_max .Enabled = byte_range;
		}
	}
}
