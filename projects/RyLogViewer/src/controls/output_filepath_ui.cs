using System.Collections.Generic;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	/// <summary>Helper control for displaying the output filepath combo</summary>
	public class OutputFilepathUI :UserControl
	{
		#region UI Elements
		private Label m_lbl_output_file;
		private Button m_btn_browse_output;
		private CheckBox m_check_append;
		private ComboBox m_cb_filepaths;
		private ToolTip m_tt;
		#endregion

		public OutputFilepathUI()
		{
			InitializeComponent();
		
			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Set the filepaths to show in the drop-down history</summary>
		public IEnumerable<string> OutputFilepathHistory
		{
			set
			{
				foreach (var i in value)
					m_cb_filepaths.Items.Add(i);
			}
		}

		/// <summary>The full filepath of the selected output file</summary>
		public string OutputFilepath
		{
			get { return m_cb_filepaths.Text; }
			set { m_cb_filepaths.Text = value; UpdateUI(); }
		}

		/// <summary>True if the user wishes to append to the output file rather than overwrite</summary>
		public bool AppendOutputFile
		{
			get { return m_check_append.Checked; }
			set { m_check_append.Checked = value; }
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			// Output file
			m_cb_filepaths.ToolTip(m_tt, "The file to save captured program output in.\r\nLeave blank to not save captured output");
			m_cb_filepaths.TextChanged += (s,a)=>
			{
				UpdateUI();
			};

			// Browse output file
			m_btn_browse_output.Click += (s,a)=>
			{
				var dg = new SaveFileDialog{Filter = Constants.LogFileFilter, CheckPathExists = true, OverwritePrompt = false};
				if (dg.ShowDialog(this) != DialogResult.OK) return;
				OutputFilepath = dg.FileName;
			};
		}

		/// <summary>Enable/Disable bits of the UI based on current settings</summary>
		private void UpdateUI()
		{
			m_check_append.Enabled = OutputFilepath.Length != 0;
		}

		#region Component Designer generated code

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
			this.components = new System.ComponentModel.Container();
			this.m_lbl_output_file = new System.Windows.Forms.Label();
			this.m_btn_browse_output = new System.Windows.Forms.Button();
			this.m_check_append = new System.Windows.Forms.CheckBox();
			this.m_cb_filepaths = new RyLogViewer.ComboBox();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			// 
			// m_lbl_output_file
			// 
			this.m_lbl_output_file.AutoSize = true;
			this.m_lbl_output_file.Location = new System.Drawing.Point(-3, 0);
			this.m_lbl_output_file.Name = "m_lbl_output_file";
			this.m_lbl_output_file.Size = new System.Drawing.Size(149, 13);
			this.m_lbl_output_file.TabIndex = 59;
			this.m_lbl_output_file.Text = "File to write program output to:";
			// 
			// m_btn_browse_output
			// 
			this.m_btn_browse_output.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse_output.Location = new System.Drawing.Point(217, 14);
			this.m_btn_browse_output.Name = "m_btn_browse_output";
			this.m_btn_browse_output.Size = new System.Drawing.Size(34, 23);
			this.m_btn_browse_output.TabIndex = 1;
			this.m_btn_browse_output.Text = "...";
			this.m_btn_browse_output.UseVisualStyleBackColor = true;
			// 
			// m_check_append
			// 
			this.m_check_append.AutoSize = true;
			this.m_check_append.Location = new System.Drawing.Point(16, 43);
			this.m_check_append.Name = "m_check_append";
			this.m_check_append.Size = new System.Drawing.Size(113, 17);
			this.m_check_append.TabIndex = 2;
			this.m_check_append.Text = "Append to existing";
			this.m_check_append.UseVisualStyleBackColor = true;
			// 
			// m_cb_filepaths
			// 
			this.m_cb_filepaths.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_filepaths.FormattingEnabled = true;
			this.m_cb_filepaths.Location = new System.Drawing.Point(16, 16);
			this.m_cb_filepaths.Name = "m_cb_filepaths";
			this.m_cb_filepaths.Size = new System.Drawing.Size(195, 21);
			this.m_cb_filepaths.TabIndex = 0;
			// 
			// OutputFilepathUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_cb_filepaths);
			this.Controls.Add(this.m_lbl_output_file);
			this.Controls.Add(this.m_btn_browse_output);
			this.Controls.Add(this.m_check_append);
			this.Name = "OutputFilepathUI";
			this.Size = new System.Drawing.Size(251, 59);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
