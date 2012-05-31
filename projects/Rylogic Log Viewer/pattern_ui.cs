using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Rylogic_Log_Viewer
{
	public class PatternUI :UserControl
	{
		/// <summary>The pattern being controlled by this UI</summary>
		public Pattern Pattern
		{
			get { return m_pattern; }
			set { m_pattern = value ?? new Pattern(); }
		}
		private Pattern m_pattern;
		
		/// <summary>Raised when the 'Add' button is hit</summary>
		public event EventHandler Add;
		
		public PatternUI()
		{
			InitializeComponent();
			Pattern = null;
			m_edit_pattern.TextChanged += (s,a)=>
				{
					Pattern.Expr = m_edit_pattern.Text;
					UpdateUI();
				};
			m_btn_add.Click += (s,a)=>
				{
					if (Add != null)
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
		}
		private void UpdateUI()
		{
			SuspendLayout();
			m_edit_pattern.Text         = Pattern.Expr;
			m_check_is_regex.Checked    = Pattern.IsRegex;
			m_check_ignore_case.Checked = Pattern.IgnoreCase;
			m_check_invert.Checked      = Pattern.Invert;
			m_check_active.Checked      = Pattern.Active;
			
			m_edit_test.SelectAll();
			m_edit_test.SelectionBackColor = Color.White;
			foreach (var r in Pattern.Match(m_edit_test.Text))
			{
				m_edit_test.SelectionStart     = (int)r.First;
				m_edit_test.SelectionLength    = (int)r.Count;
				m_edit_test.SelectionBackColor = Color.LightBlue;
			}
			ResumeLayout();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PatternUI));
			this.m_check_active = new System.Windows.Forms.CheckBox();
			this.m_check_invert = new System.Windows.Forms.CheckBox();
			this.m_check_is_regex = new System.Windows.Forms.CheckBox();
			this.m_check_ignore_case = new System.Windows.Forms.CheckBox();
			this.m_btn_add = new System.Windows.Forms.Button();
			this.m_lbl_hl_regexp = new System.Windows.Forms.Label();
			this.m_edit_pattern = new System.Windows.Forms.TextBox();
			this.m_edit_test = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			// 
			// m_check_active
			// 
			this.m_check_active.AutoSize = true;
			this.m_check_active.Location = new System.Drawing.Point(365, 29);
			this.m_check_active.Name = "m_check_active";
			this.m_check_active.Size = new System.Drawing.Size(56, 17);
			this.m_check_active.TabIndex = 19;
			this.m_check_active.Text = "Active";
			this.m_check_active.UseVisualStyleBackColor = true;
			// 
			// m_check_invert
			// 
			this.m_check_invert.AutoSize = true;
			this.m_check_invert.Location = new System.Drawing.Point(273, 29);
			this.m_check_invert.Name = "m_check_invert";
			this.m_check_invert.Size = new System.Drawing.Size(86, 17);
			this.m_check_invert.TabIndex = 18;
			this.m_check_invert.Text = "Invert Match";
			this.m_check_invert.UseVisualStyleBackColor = true;
			// 
			// m_check_is_regex
			// 
			this.m_check_is_regex.AutoSize = true;
			this.m_check_is_regex.Location = new System.Drawing.Point(61, 29);
			this.m_check_is_regex.Name = "m_check_is_regex";
			this.m_check_is_regex.Size = new System.Drawing.Size(117, 17);
			this.m_check_is_regex.TabIndex = 17;
			this.m_check_is_regex.Text = "Regular Expression";
			this.m_check_is_regex.UseVisualStyleBackColor = true;
			// 
			// m_check_ignore_case
			// 
			this.m_check_ignore_case.AutoSize = true;
			this.m_check_ignore_case.Location = new System.Drawing.Point(184, 29);
			this.m_check_ignore_case.Name = "m_check_ignore_case";
			this.m_check_ignore_case.Size = new System.Drawing.Size(83, 17);
			this.m_check_ignore_case.TabIndex = 16;
			this.m_check_ignore_case.Text = "Ignore Case";
			this.m_check_ignore_case.UseVisualStyleBackColor = true;
			// 
			// m_btn_add
			// 
			this.m_btn_add.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_add.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("m_btn_add.BackgroundImage")));
			this.m_btn_add.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.m_btn_add.Location = new System.Drawing.Point(383, 0);
			this.m_btn_add.Name = "m_btn_add";
			this.m_btn_add.Size = new System.Drawing.Size(24, 24);
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
			this.m_edit_pattern.Size = new System.Drawing.Size(324, 20);
			this.m_edit_pattern.TabIndex = 0;
			// 
			// m_edit_test
			// 
			this.m_edit_test.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_test.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_edit_test.Location = new System.Drawing.Point(6, 52);
			this.m_edit_test.Multiline = false;
			this.m_edit_test.Name = "m_edit_test";
			this.m_edit_test.Size = new System.Drawing.Size(408, 20);
			this.m_edit_test.TabIndex = 20;
			this.m_edit_test.Text = "Enter text here to test your pattern";
			// 
			// PatternUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
			this.Size = new System.Drawing.Size(420, 78);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
