using System.Windows.Forms;
using RyLogViewer;
using DataGridView = System.Windows.Forms.DataGridView;

namespace RexBionics
{
	public partial class RexLogUI :Form
	{
		public RexLogUI()
		{
			InitializeComponent();
			m_output_filepath_ui.AppendOutputFile;

			FormClosing += (s,a) =>
				{
					//if (DialogResult == DialogResult.OK)
					//{
					//}
				};
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private TextBox m_edit_type_desc_filepath;
		private Label m_lbl_type_desc_filepath;
		private Button m_btn_type_desc_browse;
		private DataGridView m_grid;
		private Label m_lbl_log_files;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private Button m_btn_browse;
		private OutputFilepathUI m_output_filepath_ui;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_edit_type_desc_filepath = new System.Windows.Forms.TextBox();
			this.m_lbl_type_desc_filepath = new System.Windows.Forms.Label();
			this.m_btn_type_desc_browse = new System.Windows.Forms.Button();
			this.m_grid = new System.Windows.Forms.DataGridView();
			this.m_lbl_log_files = new System.Windows.Forms.Label();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_btn_browse = new System.Windows.Forms.Button();
			this.m_output_filepath_ui = new RyLogViewer.OutputFilepathUI();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.SuspendLayout();
			//
			// m_edit_type_desc_filepath
			//
			this.m_edit_type_desc_filepath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_type_desc_filepath.Location = new System.Drawing.Point(24, 32);
			this.m_edit_type_desc_filepath.Name = "m_edit_type_desc_filepath";
			this.m_edit_type_desc_filepath.Size = new System.Drawing.Size(280, 20);
			this.m_edit_type_desc_filepath.TabIndex = 0;
			//
			// m_lbl_type_desc_filepath
			//
			this.m_lbl_type_desc_filepath.AutoSize = true;
			this.m_lbl_type_desc_filepath.Location = new System.Drawing.Point(12, 16);
			this.m_lbl_type_desc_filepath.Name = "m_lbl_type_desc_filepath";
			this.m_lbl_type_desc_filepath.Size = new System.Drawing.Size(109, 13);
			this.m_lbl_type_desc_filepath.TabIndex = 1;
			this.m_lbl_type_desc_filepath.Text = "Type Description File:";
			//
			// m_btn_type_desc_browse
			//
			this.m_btn_type_desc_browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_type_desc_browse.Location = new System.Drawing.Point(310, 30);
			this.m_btn_type_desc_browse.Name = "m_btn_type_desc_browse";
			this.m_btn_type_desc_browse.Size = new System.Drawing.Size(34, 23);
			this.m_btn_type_desc_browse.TabIndex = 2;
			this.m_btn_type_desc_browse.Text = "...";
			this.m_btn_type_desc_browse.UseVisualStyleBackColor = true;
			//
			// m_grid
			//
			this.m_grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.Location = new System.Drawing.Point(24, 71);
			this.m_grid.Name = "m_grid";
			this.m_grid.Size = new System.Drawing.Size(320, 166);
			this.m_grid.TabIndex = 3;
			//
			// m_lbl_log_files
			//
			this.m_lbl_log_files.AutoSize = true;
			this.m_lbl_log_files.Location = new System.Drawing.Point(12, 55);
			this.m_lbl_log_files.Name = "m_lbl_log_files";
			this.m_lbl_log_files.Size = new System.Drawing.Size(52, 13);
			this.m_lbl_log_files.TabIndex = 4;
			this.m_lbl_log_files.Text = "Log Files:";
			//
			// m_btn_ok
			//
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(188, 318);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 5;
			this.m_btn_ok.Text = "&OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			//
			// m_btn_cancel
			//
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(269, 318);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 6;
			this.m_btn_cancel.Text = "&Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			//
			// m_btn_browse
			//
			this.m_btn_browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_browse.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_browse.Location = new System.Drawing.Point(15, 318);
			this.m_btn_browse.Name = "m_btn_browse";
			this.m_btn_browse.Size = new System.Drawing.Size(75, 23);
			this.m_btn_browse.TabIndex = 7;
			this.m_btn_browse.Text = "&Browse";
			this.m_btn_browse.UseVisualStyleBackColor = true;
			//
			// m_output_filepath_ui
			//
			this.m_output_filepath_ui.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_output_filepath_ui.AppendOutputFile = false;
			this.m_output_filepath_ui.Location = new System.Drawing.Point(24, 243);
			this.m_output_filepath_ui.Name = "m_output_filepath_ui";
			this.m_output_filepath_ui.OutputFilepath = "";
			this.m_output_filepath_ui.Size = new System.Drawing.Size(320, 62);
			this.m_output_filepath_ui.TabIndex = 8;
			//
			// RexLogUI
			//
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(356, 353);
			this.Controls.Add(this.m_btn_browse);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_lbl_log_files);
			this.Controls.Add(this.m_grid);
			this.Controls.Add(this.m_btn_type_desc_browse);
			this.Controls.Add(this.m_lbl_type_desc_filepath);
			this.Controls.Add(this.m_edit_type_desc_filepath);
			this.Controls.Add(this.m_output_filepath_ui);
			this.MinimumSize = new System.Drawing.Size(283, 272);
			this.Name = "RexLogUI";
			this.Text = "RexLogUI";
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion
	}
}
