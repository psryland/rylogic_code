using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using pr.gui;
using pr.util;

namespace RyLogViewer
{
	public class FindUI :ToolForm
	{
		private readonly Settings m_settings;
		private readonly Pattern m_pattern;
		private readonly List<string> m_history;
		private readonly ToolTip m_tt;
		private Button m_btn_find_next;
		private Button m_btn_find_prev;
		private Label m_lbl_find_what;
		private ComboBox m_combo_pattern;
		private CheckBox m_check_ignore_case;
		private CheckBox m_check_invert;
		private RadioButton m_radio_regex;
		private RadioButton m_radio_wildcard;
		private RadioButton m_radio_substring;
		
		/// <summary>The search pattern</summary>
		public string Pattern
		{
			get { return m_pattern.Expr; }
			set { m_pattern.Expr = m_combo_pattern.Text = value; }
		}

		/// <summary>An event called whenever the dialog gets a FindNext command</summary>
		public event Action<Pattern> FindNext;
		public void RaiseFindNext()
		{
			if (!UseFindExpr) return;
			m_btn_find_next.Enabled = false;
			if (FindNext != null) FindNext(m_pattern);
			m_btn_find_next.Enabled = true;
		}
		
		/// <summary>An event called whenever the dialog gets a FindPrev command</summary>
		public event Action<Pattern> FindPrev;
		public void RaiseFindPrev()
		{
			if (!UseFindExpr) return;
			m_btn_find_prev.Enabled = false;
			if (FindPrev != null) FindPrev(m_pattern);
			m_btn_find_prev.Enabled = true;
		}
		
		public FindUI(Form owner, Settings settings)
		:base(owner, new Size(-270, +28), Size.Empty, EPin.TopRight, false)
		{
			InitializeComponent();
			m_settings = settings;
			m_pattern  = new Pattern();
			m_history  = new List<string>(m_settings.FindHistory);
			m_tt       = new ToolTip();
			
			// Search buttons
			m_btn_find_prev.ToolTip(m_tt, "Search backward through the log.\r\nKeyboard shortcut: <Shift>+<Enter>");
			m_btn_find_prev.Click += (s,a) => RaiseFindPrev();
			
			m_btn_find_next.ToolTip(m_tt, "Search forward through the log.\r\nKeyboard shortcut: <Enter>");
			m_btn_find_next.Click += (s,a) => RaiseFindNext();
			
			// Pattern type
			m_radio_substring.Checked = m_pattern.PatnType == EPattern.Substring;
			m_radio_substring.Click += (s,a)=>
				{
					if (m_radio_substring.Checked) m_pattern.PatnType = EPattern.Substring;
				};
			m_radio_wildcard .Checked = m_pattern.PatnType == EPattern.Wildcard;
			m_radio_wildcard.Click += (s,a)=>
				{
					if (m_radio_wildcard.Checked) m_pattern.PatnType = EPattern.Wildcard;
				};
			m_radio_regex    .Checked = m_pattern.PatnType == EPattern.RegularExpression;
			m_radio_regex.Click += (s,a)=>
				{
					if (m_radio_regex.Checked) m_pattern.PatnType = EPattern.RegularExpression;
				};
			
			// Ignore case
			m_check_ignore_case.ToolTip(m_tt, "Check to make searches ignore differences in case");
			m_check_ignore_case.CheckedChanged += (s,a)=>
				{
					m_pattern.IgnoreCase = m_check_ignore_case.Checked;
				};
			
			// Invert
			m_check_invert.ToolTip(m_tt, "Check to find instances that do not match the search pattern");
			m_check_invert.CheckedChanged += (s,a)=>
				{
					m_pattern.Invert = m_check_invert.Checked;
				};
			
			// Shown
			VisibleChanged += (s,a)=>
				{
					if (Visible)
					{
						m_combo_pattern.Focus();
						m_combo_pattern.Text = Pattern;
						m_combo_pattern.SelectAll();
						
					}
				};
			FormClosing += (s,a)=>
				{
					if (a.CloseReason != CloseReason.UserClosing) return;
					Hide();
					a.Cancel = true;
					Owner.Focus();
				};
		}
		
		/// <summary>Handle key presses</summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (Owner is Main)
			{
				((Main)Owner).HandleKeyDown(e);
				if (e.Handled) return;
			}
			if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.F3)
			{
				if (!e.Shift) RaiseFindNext();
				else          RaiseFindPrev();
				e.Handled = true;
				return;
			}
			base.OnKeyDown(e);
		}
		
		/// <summary>Returns true if the find expression can be used</summary>
		private bool UseFindExpr
		{
			get
			{
				if (m_combo_pattern.Text.Length == 0) return false;
				m_pattern.Expr = m_combo_pattern.Text;
				
				// Update the history
				Misc.AddToHistoryList(m_history, m_pattern.Expr, false, Constants.MaxFindHistory);
				m_settings.FindHistory = m_history.ToArray();
				
				// Repopulate the combo
				m_combo_pattern.Items.Clear();
				foreach (var h in m_history)
					m_combo_pattern.Items.Add(h);
				
				return true;
			}
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FindUI));
			this.m_btn_find_next = new System.Windows.Forms.Button();
			this.m_btn_find_prev = new System.Windows.Forms.Button();
			this.m_lbl_find_what = new System.Windows.Forms.Label();
			this.m_combo_pattern = new System.Windows.Forms.ComboBox();
			this.m_check_ignore_case = new System.Windows.Forms.CheckBox();
			this.m_check_invert = new System.Windows.Forms.CheckBox();
			this.m_radio_regex = new System.Windows.Forms.RadioButton();
			this.m_radio_wildcard = new System.Windows.Forms.RadioButton();
			this.m_radio_substring = new System.Windows.Forms.RadioButton();
			this.SuspendLayout();
			// 
			// m_btn_find_next
			// 
			this.m_btn_find_next.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_find_next.Location = new System.Drawing.Point(168, 80);
			this.m_btn_find_next.Name = "m_btn_find_next";
			this.m_btn_find_next.Size = new System.Drawing.Size(89, 23);
			this.m_btn_find_next.TabIndex = 1;
			this.m_btn_find_next.Text = "Find &Next";
			this.m_btn_find_next.UseVisualStyleBackColor = true;
			// 
			// m_btn_find_prev
			// 
			this.m_btn_find_prev.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_find_prev.Location = new System.Drawing.Point(168, 51);
			this.m_btn_find_prev.Name = "m_btn_find_prev";
			this.m_btn_find_prev.Size = new System.Drawing.Size(89, 23);
			this.m_btn_find_prev.TabIndex = 2;
			this.m_btn_find_prev.Text = "Find &Previous";
			this.m_btn_find_prev.UseVisualStyleBackColor = true;
			// 
			// m_lbl_find_what
			// 
			this.m_lbl_find_what.AutoSize = true;
			this.m_lbl_find_what.Location = new System.Drawing.Point(1, 7);
			this.m_lbl_find_what.Name = "m_lbl_find_what";
			this.m_lbl_find_what.Size = new System.Drawing.Size(56, 13);
			this.m_lbl_find_what.TabIndex = 4;
			this.m_lbl_find_what.Text = "Find what:";
			// 
			// m_combo_pattern
			// 
			this.m_combo_pattern.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_pattern.FormattingEnabled = true;
			this.m_combo_pattern.Location = new System.Drawing.Point(4, 23);
			this.m_combo_pattern.Name = "m_combo_pattern";
			this.m_combo_pattern.Size = new System.Drawing.Size(253, 21);
			this.m_combo_pattern.TabIndex = 0;
			// 
			// m_check_ignore_case
			// 
			this.m_check_ignore_case.AutoSize = true;
			this.m_check_ignore_case.Location = new System.Drawing.Point(81, 51);
			this.m_check_ignore_case.Name = "m_check_ignore_case";
			this.m_check_ignore_case.Size = new System.Drawing.Size(83, 17);
			this.m_check_ignore_case.TabIndex = 4;
			this.m_check_ignore_case.Text = "Ignore &Case";
			this.m_check_ignore_case.UseVisualStyleBackColor = true;
			// 
			// m_check_invert
			// 
			this.m_check_invert.AutoSize = true;
			this.m_check_invert.Location = new System.Drawing.Point(81, 67);
			this.m_check_invert.Name = "m_check_invert";
			this.m_check_invert.Size = new System.Drawing.Size(86, 17);
			this.m_check_invert.TabIndex = 5;
			this.m_check_invert.Text = "&Invert Match";
			this.m_check_invert.UseVisualStyleBackColor = true;
			// 
			// m_radio_regex
			// 
			this.m_radio_regex.AutoSize = true;
			this.m_radio_regex.Location = new System.Drawing.Point(8, 82);
			this.m_radio_regex.Name = "m_radio_regex";
			this.m_radio_regex.Size = new System.Drawing.Size(116, 17);
			this.m_radio_regex.TabIndex = 28;
			this.m_radio_regex.Text = "Regular Expression";
			this.m_radio_regex.UseVisualStyleBackColor = true;
			// 
			// m_radio_wildcard
			// 
			this.m_radio_wildcard.AutoSize = true;
			this.m_radio_wildcard.Location = new System.Drawing.Point(8, 66);
			this.m_radio_wildcard.Name = "m_radio_wildcard";
			this.m_radio_wildcard.Size = new System.Drawing.Size(67, 17);
			this.m_radio_wildcard.TabIndex = 4;
			this.m_radio_wildcard.Text = "Wildcard";
			this.m_radio_wildcard.UseVisualStyleBackColor = true;
			// 
			// m_radio_substring
			// 
			this.m_radio_substring.AutoSize = true;
			this.m_radio_substring.Location = new System.Drawing.Point(8, 50);
			this.m_radio_substring.Name = "m_radio_substring";
			this.m_radio_substring.Size = new System.Drawing.Size(69, 17);
			this.m_radio_substring.TabIndex = 3;
			this.m_radio_substring.Text = "Substring";
			this.m_radio_substring.UseVisualStyleBackColor = true;
			// 
			// FindUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(261, 111);
			this.Controls.Add(this.m_radio_regex);
			this.Controls.Add(this.m_radio_wildcard);
			this.Controls.Add(this.m_radio_substring);
			this.Controls.Add(this.m_check_invert);
			this.Controls.Add(this.m_check_ignore_case);
			this.Controls.Add(this.m_combo_pattern);
			this.Controls.Add(this.m_lbl_find_what);
			this.Controls.Add(this.m_btn_find_prev);
			this.Controls.Add(this.m_btn_find_next);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MaximumSize = new System.Drawing.Size(640, 145);
			this.MinimumSize = new System.Drawing.Size(277, 145);
			this.Name = "FindUI";
			this.ShowInTaskbar = false;
			this.Text = "Find...";
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion
	}
}
