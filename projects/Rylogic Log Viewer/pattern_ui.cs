using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Rylogic_Log_Viewer
{
	public class PatternUI :UserControl
	{
		enum BtnImageIdx { AddNew = 0, Save = 1 }
		
		/// <summary>The pattern being controlled by this UI</summary>
		public Pattern Pattern { get { return m_pattern; } }
		private Pattern m_pattern;
		private ImageList m_image_list;
		private readonly ToolTip m_tt;
		
		/// <summary>True if the editted pattern is a new instance</summary>
		public bool IsNew { get; private set; }
		
		/// <summary>Raised when the 'Add' button is hit and the pattern field contains a valid pattern</summary>
		public event EventHandler Add;
		
		public PatternUI()
		{
			InitializeComponent();
			m_pattern = null;
			m_tt = new ToolTip();
			m_edit_pattern.TextChanged += (s,a)=>
				{
					Pattern.Expr = m_edit_pattern.Text;
					UpdateUI();
				};
			m_btn_add.Click += (s,a)=>
				{
					if (Add == null) return;
					if (Pattern.Expr.Length == 0) return;
					if (Pattern.IsRegex) try { new Regex(Pattern.Expr); } catch (ArgumentException) { return; }
					Add(this, EventArgs.Empty);
				};
			m_check_is_regex.CheckedChanged += (s,a)=>
				{
					Pattern.IsRegex = m_check_is_regex.Checked;
					UpdateUI();
				};
			m_check_ignore_case.CheckedChanged += (s,a)=>
				{
					Pattern.IgnoreCase = m_check_ignore_case.Checked;
					UpdateUI();
				};
			m_check_invert.CheckedChanged += (s,a)=>
				{
					Pattern.Invert = m_check_invert.Checked;
					UpdateUI();
				};
			m_check_active.CheckedChanged += (s,a)=>
				{
					Pattern.Active = m_check_active.Checked;
					UpdateUI();
				};
			m_edit_test.TextChanged += (s,a)=>
				{
					UpdateUI();
				};
		}
		private void UpdateUI()
		{
			SuspendLayout();
			m_edit_pattern.Text         = Pattern.Expr;
			m_check_is_regex.Checked    = Pattern.IsRegex;
			m_check_ignore_case.Checked = Pattern.IgnoreCase;
			m_check_invert.Checked      = Pattern.Invert;
			m_check_active.Checked      = Pattern.Active;
			
			// Preserve the current carot position
			int start = m_edit_test.SelectionStart;
			int length = m_edit_test.SelectionLength;
			m_edit_test.SelectAll();
			m_edit_test.SelectionBackColor = Color.White;
			foreach (var r in Pattern.Match(m_edit_test.Text))
			{
				m_edit_test.SelectionStart     = (int)r.First;
				m_edit_test.SelectionLength    = (int)r.Count;
				m_edit_test.SelectionBackColor = Color.LightBlue;
			}
			m_edit_test.SelectionStart  = start;
			m_edit_test.SelectionLength = length;
			ResumeLayout();
		}

		/// <summary>Select 'pat' as a new pattern</summary>
		public void NewPattern(Pattern pat)
		{
			IsNew = true;
			m_pattern = pat;
			m_btn_add.ImageIndex = (int)BtnImageIdx.AddNew;
			m_tt.SetToolTip(m_btn_add, "Add this new pattern");
			UpdateUI();
		}

		/// <summary>Select a pattern into the UI for editting</summary>
		public void EditPattern(Pattern pat)
		{
			IsNew = false;
			m_pattern = pat;
			m_btn_add.ImageIndex = (int)BtnImageIdx.Save;
			m_tt.SetToolTip(m_btn_add, "Finish editing this pattern");
			UpdateUI();
		}
		
		private CheckBox m_check_active;
		private CheckBox m_check_invert;
		private CheckBox m_check_is_regex;
		private CheckBox m_check_ignore_case;
		private Button m_btn_add;
		private Label m_lbl_hl_regexp;
		private TextBox m_edit_pattern;
		private RichTextBox m_edit_test;
		
		#region Component Designer generated code
		
		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>Clean up any resources being used.</summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
				components.Dispose();
			base.Dispose(disposing);
		}
		
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PatternUI));
			this.m_check_active = new System.Windows.Forms.CheckBox();
			this.m_check_invert = new System.Windows.Forms.CheckBox();
			this.m_check_is_regex = new System.Windows.Forms.CheckBox();
			this.m_check_ignore_case = new System.Windows.Forms.CheckBox();
			this.m_btn_add = new System.Windows.Forms.Button();
			this.m_lbl_hl_regexp = new System.Windows.Forms.Label();
			this.m_edit_pattern = new System.Windows.Forms.TextBox();
			this.m_edit_test = new System.Windows.Forms.RichTextBox();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			// 
			// m_check_active
			// 
			this.m_check_active.AutoSize = true;
			this.m_check_active.Location = new System.Drawing.Point(310, 29);
			this.m_check_active.Name = "m_check_active";
			this.m_check_active.Size = new System.Drawing.Size(56, 17);
			this.m_check_active.TabIndex = 19;
			this.m_check_active.Text = "Active";
			this.m_check_active.UseVisualStyleBackColor = true;
			// 
			// m_check_invert
			// 
			this.m_check_invert.AutoSize = true;
			this.m_check_invert.Location = new System.Drawing.Point(218, 29);
			this.m_check_invert.Name = "m_check_invert";
			this.m_check_invert.Size = new System.Drawing.Size(86, 17);
			this.m_check_invert.TabIndex = 18;
			this.m_check_invert.Text = "Invert Match";
			this.m_check_invert.UseVisualStyleBackColor = true;
			// 
			// m_check_is_regex
			// 
			this.m_check_is_regex.AutoSize = true;
			this.m_check_is_regex.Location = new System.Drawing.Point(6, 29);
			this.m_check_is_regex.Name = "m_check_is_regex";
			this.m_check_is_regex.Size = new System.Drawing.Size(117, 17);
			this.m_check_is_regex.TabIndex = 17;
			this.m_check_is_regex.Text = "Regular Expression";
			this.m_check_is_regex.UseVisualStyleBackColor = true;
			// 
			// m_check_ignore_case
			// 
			this.m_check_ignore_case.AutoSize = true;
			this.m_check_ignore_case.Location = new System.Drawing.Point(129, 29);
			this.m_check_ignore_case.Name = "m_check_ignore_case";
			this.m_check_ignore_case.Size = new System.Drawing.Size(83, 17);
			this.m_check_ignore_case.TabIndex = 16;
			this.m_check_ignore_case.Text = "Ignore Case";
			this.m_check_ignore_case.UseVisualStyleBackColor = true;
			// 
			// m_btn_add
			// 
			this.m_btn_add.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_add.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.m_btn_add.ImageIndex = 0;
			this.m_btn_add.ImageList = this.m_image_list;
			this.m_btn_add.Location = new System.Drawing.Point(366, 3);
			this.m_btn_add.Name = "m_btn_add";
			this.m_btn_add.Size = new System.Drawing.Size(46, 46);
			this.m_btn_add.TabIndex = 2;
			this.m_btn_add.UseVisualStyleBackColor = true;
			// 
			// m_lbl_hl_regexp
			// 
			this.m_lbl_hl_regexp.AutoSize = true;
			this.m_lbl_hl_regexp.Location = new System.Drawing.Point(3, 6);
			this.m_lbl_hl_regexp.Name = "m_lbl_hl_regexp";
			this.m_lbl_hl_regexp.Size = new System.Drawing.Size(44, 13);
			this.m_lbl_hl_regexp.TabIndex = 14;
			this.m_lbl_hl_regexp.Text = "Pattern:";
			this.m_lbl_hl_regexp.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_edit_pattern
			// 
			this.m_edit_pattern.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_pattern.Location = new System.Drawing.Point(53, 3);
			this.m_edit_pattern.Name = "m_edit_pattern";
			this.m_edit_pattern.Size = new System.Drawing.Size(311, 20);
			this.m_edit_pattern.TabIndex = 0;
			// 
			// m_edit_test
			// 
			this.m_edit_test.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_test.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_edit_test.Location = new System.Drawing.Point(6, 52);
			this.m_edit_test.Name = "m_edit_test";
			this.m_edit_test.Size = new System.Drawing.Size(406, 79);
			this.m_edit_test.TabIndex = 20;
			this.m_edit_test.Text = "Enter text here to test your pattern";
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "edit_add.png");
			this.m_image_list.Images.SetKeyName(1, "edit_save.png");
			// 
			// PatternUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Controls.Add(this.m_edit_test);
			this.Controls.Add(this.m_check_active);
			this.Controls.Add(this.m_check_invert);
			this.Controls.Add(this.m_check_is_regex);
			this.Controls.Add(this.m_check_ignore_case);
			this.Controls.Add(this.m_btn_add);
			this.Controls.Add(this.m_lbl_hl_regexp);
			this.Controls.Add(this.m_edit_pattern);
			this.MinimumSize = new System.Drawing.Size(420, 78);
			this.Name = "PatternUI";
			this.Size = new System.Drawing.Size(418, 137);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
