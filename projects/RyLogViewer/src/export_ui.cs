using System;
using System.IO;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.common;
using pr.extn;
using pr.gui;
using pr.util;
using pr.maths;
using System.Drawing;

namespace RyLogViewer
{
	public class ExportUI :Form
	{
		#region UI Elements
		private RadioButton m_radio_whole_file;
		private RadioButton m_radio_selection;
		private RadioButton m_radio_range;
		private ImageList m_image_list;
		private ValueBox m_tb_output_filepath;
		private TextBox m_tb_line_ending;
		private TextBox m_tb_col_delim;
		private Button m_btn_range_to_start;
		private Button m_btn_range_to_end;
		private Button m_btn_browse;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private Label m_lbl_output_file;
		private Label m_lbl_line_ending;
		private Label m_lbl_desc;
		private Label m_lbl_col_delim;
		private Label m_lbl_status;
		private ValueBox m_tb_range_min;
		private ValueBox m_tb_range_max;
		private ToolTip m_tt;
		#endregion

		public ExportUI(string output_filepath, string row_delim, string col_delim, Range byte_range)
		{
			InitializeComponent();

			OutputFilepath = output_filepath;
			RowDelim = row_delim;
			ColDelim = col_delim;
			ByteRangeLimits = byte_range;
			ByteRange = byte_range;

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);

			// Grab focus for force controls to commit their values
			Focus();

			if (DialogResult != DialogResult.OK)
				return;

			// Cancel if there's an error somewhere
			UpdateUI();
			e.Cancel = !m_btn_ok.Enabled;
		}

		/// <summary>The file to create</summary>
		public string OutputFilepath
		{
			get { return m_output_filepath; }
			set
			{
				if (m_output_filepath == value) return;
				m_output_filepath = value;
			}
		}
		private string m_output_filepath;

		/// <summary>The row delimiter string</summary>
		public string RowDelim
		{
			get { return m_row_delim; }
			set
			{
				if (m_row_delim == value) return;
				m_row_delim = value;
			}
		}
		private string m_row_delim;

		/// <summary>The column delimiter string</summary>
		public string ColDelim
		{
			get { return m_col_delim; }
			set
			{
				if (m_col_delim == value) return;
				m_col_delim = value;
			}
		}
		private string m_col_delim;

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
		public enum ERangeToExport
		{
			WholeFile,
			Selection,
			ByteRange
		}

		/// <summary>The maximum range limits</summary>
		public Range ByteRangeLimits
		{
			get { return m_range_limit; }
			private set
			{
				if (m_range_limit == value) return;
				m_range_limit = value;
			}
		}
		private Range m_range_limit;

		/// <summary>If RangeToExport is byte range, export this range</summary>
		public Range ByteRange
		{
			get { return m_range; }
			set
			{
				if (m_range == value) return;
				m_range = new Range(
					Math.Max(ByteRangeLimits.Beg, value.Beg),
					Math.Min(ByteRangeLimits.End, value.End));
			}
		}
		private Range m_range;

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// Output file
			m_tb_output_filepath.ToolTip(m_tt, "The path of the file to export to");
			m_tb_output_filepath.ValueType = typeof(string);
			m_tb_output_filepath.Text = OutputFilepath;
			m_tb_output_filepath.ValueChanged += (s,a) =>
			{
				OutputFilepath = m_tb_output_filepath.Text;
				UpdateUI();
			};

			// Browse button
			m_btn_browse.ToolTip(m_tt, "Browse to the location of where to save the file");
			m_btn_browse.Click += (s,a)=>
			{
				var filter = Util.FileDialogFilter("Text Files","*.txt","Comma Separated Values","*.csv","All Files","*.*");
				var dg = new SaveFileDialog
				{
					Title = "Choose an output file name",
					FileName = OutputFilepath,
					Filter = filter,
				};
				using (dg)
				{
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					m_tb_output_filepath.Text = dg.FileName;
					OutputFilepath = dg.FileName;
				}
			};

			// Radio buttons
			m_radio_whole_file.ToolTip(m_tt, "Export the entire file contents");
			m_radio_whole_file.Checked = true;
			m_radio_whole_file.CheckedChanged += (s,a) =>
			{
				UpdateUI();
			};

			m_radio_selection.ToolTip(m_tt, "Export the selected rows only");
			m_radio_selection.Checked = false;
			m_radio_selection.CheckedChanged += (s,a) =>
			{
				UpdateUI();
			};

			m_radio_range.ToolTip(m_tt, "Export a specific byte range within the file.\r\nThe byte range will automatically be expanded to start at the beginning of a line");
			m_radio_range.Checked = false;
			m_radio_range.CheckedChanged += (s,a) =>
			{
				UpdateUI();
			};

			// Line ending
			m_tb_line_ending.ToolTip(m_tt, "The characters to end each line in the exported file.\r\nUse '<CR>', '<LF>', or '<TAB>' for carriage return, line feed, or tab respectively");
			m_tb_line_ending.Text = RowDelim;
			m_tb_line_ending.TextChanged += (s,a) =>
			{
				RowDelim = m_tb_line_ending.Text;
				UpdateUI();
			};

			// Column delimiter
			m_tb_col_delim.ToolTip(m_tt, "The characters to use to separate columns in the exported file.\r\nUse '<CR>', '<LF>', or '<TAB>' for carriage return, line feed, or tab respectively");
			m_tb_col_delim.Text = ColDelim;
			m_tb_col_delim.TextChanged += (s,a) =>
			{
				ColDelim = m_tb_col_delim.Text;
				UpdateUI();
			};

			// Byte Range
			m_tb_range_min.ValueType = typeof(long);
			m_tb_range_min.ValidateText = t => long_.TryParse(t) != null;
			m_tb_range_min.Value = ByteRange.Beg;
			m_tb_range_min.ValueChanged += (s,a) =>
			{
				if (m_tb_range_min.Valid)
					ByteRange = new Range((long)m_tb_range_min.Value, ByteRange.End);
				UpdateUI();
			};

			m_tb_range_max.ValueType = typeof(long);
			m_tb_range_max.ValidateText = t => long_.TryParse(t) != null;
			m_tb_range_max.Value = ByteRange.End;
			m_tb_range_max.ValueChanged += (s,a) =>
			{
				if (m_tb_range_max.Valid)
					ByteRange = new Range(ByteRange.Beg, (long)m_tb_range_max.Value);
				UpdateUI();
			};

			// Reset to start
			m_btn_range_to_start.Click += (s,a) =>
			{
				m_tb_range_min.Value = ByteRangeLimits.Beg;
				UpdateUI();
			};
			m_btn_range_to_end.Click += (s,a) =>
			{
				m_tb_range_max.Value = ByteRangeLimits.End;
				UpdateUI();
			};
		}

		/// <summary>Update the enabled state of UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			m_btn_range_to_start.Enabled = m_radio_range.Checked;
			m_btn_range_to_end  .Enabled = m_radio_range.Checked;
			m_tb_range_min.Enabled = m_radio_range.Checked;
			m_tb_range_max.Enabled = m_radio_range.Checked;

			var status = string.Empty;
			for (;;)
			{
				if (!Path_.IsValidFilepath(OutputFilepath, false))
				{
					status = "Invalid export file path";
					break;
				}
				if (ByteRange.Count <= 0)
				{
					status = "Invalid export data range";
					break;
				}
				break;
			}
			m_lbl_status.Text = status;
			m_lbl_status.ForeColor = Color.DarkRed;
			m_btn_ok.Enabled = !status.HasValue();
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExportUI));
			this.m_lbl_desc = new System.Windows.Forms.Label();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_radio_whole_file = new System.Windows.Forms.RadioButton();
			this.m_radio_selection = new System.Windows.Forms.RadioButton();
			this.m_radio_range = new System.Windows.Forms.RadioButton();
			this.m_btn_range_to_start = new System.Windows.Forms.Button();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_btn_range_to_end = new System.Windows.Forms.Button();
			this.m_tb_output_filepath = new pr.gui.ValueBox();
			this.m_btn_browse = new System.Windows.Forms.Button();
			this.m_lbl_output_file = new System.Windows.Forms.Label();
			this.m_lbl_line_ending = new System.Windows.Forms.Label();
			this.m_tb_line_ending = new System.Windows.Forms.TextBox();
			this.m_tb_col_delim = new System.Windows.Forms.TextBox();
			this.m_lbl_col_delim = new System.Windows.Forms.Label();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_lbl_status = new System.Windows.Forms.Label();
			this.m_tb_range_min = new pr.gui.ValueBox();
			this.m_tb_range_max = new pr.gui.ValueBox();
			this.SuspendLayout();
			// 
			// m_lbl_desc
			// 
			this.m_lbl_desc.AutoSize = true;
			this.m_lbl_desc.Location = new System.Drawing.Point(7, 19);
			this.m_lbl_desc.Name = "m_lbl_desc";
			this.m_lbl_desc.Size = new System.Drawing.Size(274, 13);
			this.m_lbl_desc.TabIndex = 0;
			this.m_lbl_desc.Text = "Export the current file, applying filters and transformations";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(203, 147);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 11;
			this.m_btn_ok.Text = "Export";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(284, 147);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 12;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_radio_whole_file
			// 
			this.m_radio_whole_file.AutoSize = true;
			this.m_radio_whole_file.Location = new System.Drawing.Point(24, 73);
			this.m_radio_whole_file.Name = "m_radio_whole_file";
			this.m_radio_whole_file.Size = new System.Drawing.Size(72, 17);
			this.m_radio_whole_file.TabIndex = 2;
			this.m_radio_whole_file.Text = "Whole file";
			this.m_radio_whole_file.UseVisualStyleBackColor = true;
			// 
			// m_radio_selection
			// 
			this.m_radio_selection.AutoSize = true;
			this.m_radio_selection.Location = new System.Drawing.Point(24, 96);
			this.m_radio_selection.Name = "m_radio_selection";
			this.m_radio_selection.Size = new System.Drawing.Size(104, 17);
			this.m_radio_selection.TabIndex = 3;
			this.m_radio_selection.Text = "Current selection";
			this.m_radio_selection.UseVisualStyleBackColor = true;
			// 
			// m_radio_range
			// 
			this.m_radio_range.AutoSize = true;
			this.m_radio_range.Location = new System.Drawing.Point(24, 119);
			this.m_radio_range.Name = "m_radio_range";
			this.m_radio_range.Size = new System.Drawing.Size(93, 17);
			this.m_radio_range.TabIndex = 4;
			this.m_radio_range.Text = "Specific range";
			this.m_radio_range.UseVisualStyleBackColor = true;
			// 
			// m_btn_range_to_start
			// 
			this.m_btn_range_to_start.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_range_to_start.ImageIndex = 0;
			this.m_btn_range_to_start.ImageList = this.m_image_list;
			this.m_btn_range_to_start.Location = new System.Drawing.Point(123, 115);
			this.m_btn_range_to_start.Name = "m_btn_range_to_start";
			this.m_btn_range_to_start.Size = new System.Drawing.Size(24, 23);
			this.m_btn_range_to_start.TabIndex = 5;
			this.m_btn_range_to_start.UseVisualStyleBackColor = true;
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "player_start.png");
			this.m_image_list.Images.SetKeyName(1, "player_end.png");
			// 
			// m_btn_range_to_end
			// 
			this.m_btn_range_to_end.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_range_to_end.ImageIndex = 1;
			this.m_btn_range_to_end.ImageList = this.m_image_list;
			this.m_btn_range_to_end.Location = new System.Drawing.Point(335, 115);
			this.m_btn_range_to_end.Name = "m_btn_range_to_end";
			this.m_btn_range_to_end.Size = new System.Drawing.Size(24, 23);
			this.m_btn_range_to_end.TabIndex = 8;
			this.m_btn_range_to_end.UseVisualStyleBackColor = true;
			// 
			// m_tb_output_filepath
			// 
			this.m_tb_output_filepath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_output_filepath.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_output_filepath.BackColorValid = System.Drawing.Color.White;
			this.m_tb_output_filepath.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_output_filepath.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_output_filepath.Location = new System.Drawing.Point(74, 45);
			this.m_tb_output_filepath.Name = "m_tb_output_filepath";
			this.m_tb_output_filepath.Size = new System.Drawing.Size(243, 20);
			this.m_tb_output_filepath.TabIndex = 0;
			this.m_tb_output_filepath.UseValidityColours = true;
			this.m_tb_output_filepath.Value = null;
			// 
			// m_btn_browse
			// 
			this.m_btn_browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse.Location = new System.Drawing.Point(323, 43);
			this.m_btn_browse.Name = "m_btn_browse";
			this.m_btn_browse.Size = new System.Drawing.Size(36, 23);
			this.m_btn_browse.TabIndex = 1;
			this.m_btn_browse.Text = ". . .";
			this.m_btn_browse.UseVisualStyleBackColor = true;
			// 
			// m_lbl_output_file
			// 
			this.m_lbl_output_file.AutoSize = true;
			this.m_lbl_output_file.Location = new System.Drawing.Point(7, 48);
			this.m_lbl_output_file.Name = "m_lbl_output_file";
			this.m_lbl_output_file.Size = new System.Drawing.Size(61, 13);
			this.m_lbl_output_file.TabIndex = 12;
			this.m_lbl_output_file.Text = "Output File:";
			// 
			// m_lbl_line_ending
			// 
			this.m_lbl_line_ending.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_line_ending.AutoSize = true;
			this.m_lbl_line_ending.Location = new System.Drawing.Point(188, 75);
			this.m_lbl_line_ending.Name = "m_lbl_line_ending";
			this.m_lbl_line_ending.Size = new System.Drawing.Size(65, 13);
			this.m_lbl_line_ending.TabIndex = 13;
			this.m_lbl_line_ending.Text = "Line ending:";
			// 
			// m_tb_line_ending
			// 
			this.m_tb_line_ending.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_line_ending.Location = new System.Drawing.Point(259, 70);
			this.m_tb_line_ending.Name = "m_tb_line_ending";
			this.m_tb_line_ending.Size = new System.Drawing.Size(100, 20);
			this.m_tb_line_ending.TabIndex = 9;
			// 
			// m_tb_col_delim
			// 
			this.m_tb_col_delim.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_col_delim.Location = new System.Drawing.Point(259, 91);
			this.m_tb_col_delim.Name = "m_tb_col_delim";
			this.m_tb_col_delim.Size = new System.Drawing.Size(100, 20);
			this.m_tb_col_delim.TabIndex = 10;
			// 
			// m_lbl_col_delim
			// 
			this.m_lbl_col_delim.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_col_delim.AutoSize = true;
			this.m_lbl_col_delim.Location = new System.Drawing.Point(167, 94);
			this.m_lbl_col_delim.Name = "m_lbl_col_delim";
			this.m_lbl_col_delim.Size = new System.Drawing.Size(86, 13);
			this.m_lbl_col_delim.TabIndex = 16;
			this.m_lbl_col_delim.Text = "Column delimiter:";
			// 
			// m_lbl_status
			// 
			this.m_lbl_status.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_status.Location = new System.Drawing.Point(4, 147);
			this.m_lbl_status.Name = "m_lbl_status";
			this.m_lbl_status.Size = new System.Drawing.Size(193, 26);
			this.m_lbl_status.TabIndex = 17;
			this.m_lbl_status.Text = "Status";
			// 
			// m_tb_range_min
			// 
			this.m_tb_range_min.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_range_min.BackColor = System.Drawing.Color.White;
			this.m_tb_range_min.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_range_min.BackColorValid = System.Drawing.Color.White;
			this.m_tb_range_min.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_range_min.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_range_min.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_range_min.Location = new System.Drawing.Point(153, 117);
			this.m_tb_range_min.Name = "m_tb_range_min";
			this.m_tb_range_min.Size = new System.Drawing.Size(87, 20);
			this.m_tb_range_min.TabIndex = 6;
			this.m_tb_range_min.UseValidityColours = true;
			this.m_tb_range_min.Value = null;
			// 
			// m_tb_range_max
			// 
			this.m_tb_range_max.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_range_max.BackColor = System.Drawing.Color.White;
			this.m_tb_range_max.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_range_max.BackColorValid = System.Drawing.Color.White;
			this.m_tb_range_max.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_range_max.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_range_max.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_range_max.Location = new System.Drawing.Point(246, 117);
			this.m_tb_range_max.Name = "m_tb_range_max";
			this.m_tb_range_max.Size = new System.Drawing.Size(87, 20);
			this.m_tb_range_max.TabIndex = 7;
			this.m_tb_range_max.UseValidityColours = true;
			this.m_tb_range_max.Value = null;
			// 
			// ExportUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(371, 182);
			this.Controls.Add(this.m_tb_range_max);
			this.Controls.Add(this.m_tb_range_min);
			this.Controls.Add(this.m_lbl_status);
			this.Controls.Add(this.m_lbl_col_delim);
			this.Controls.Add(this.m_tb_col_delim);
			this.Controls.Add(this.m_tb_line_ending);
			this.Controls.Add(this.m_lbl_line_ending);
			this.Controls.Add(this.m_lbl_output_file);
			this.Controls.Add(this.m_btn_browse);
			this.Controls.Add(this.m_tb_output_filepath);
			this.Controls.Add(this.m_btn_range_to_end);
			this.Controls.Add(this.m_btn_range_to_start);
			this.Controls.Add(this.m_radio_range);
			this.Controls.Add(this.m_radio_selection);
			this.Controls.Add(this.m_radio_whole_file);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_lbl_desc);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ExportUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Export";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
