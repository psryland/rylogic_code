﻿using System;
using System.Drawing;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gui;

namespace RyLogViewer
{
	public class FindUI :ToolForm
	{
		private readonly BindingSource m_history;
		private readonly ToolTip m_tt;
		private HelpUI m_dlg_help;
		private Pattern m_pattern;
		private Button m_btn_find_next;
		private Button m_btn_find_prev;
		private Label m_lbl_find_what;
		private ComboBox m_combo_pattern;
		private CheckBox m_check_ignore_case;
		private CheckBox m_check_invert;
		private RadioButton m_radio_regex;
		private RadioButton m_radio_wildcard;
		private TableLayoutPanel m_table;
		private Panel m_panel_top;
		private DataGridView m_grid;
		private Label m_lbl_prev_find_patterns;
		private Button m_btn_regex_help;
		private CheckBox m_check_whole_line;
		private Button m_btn_bookmarkall;
		private RadioButton m_radio_substring;

		public FindUI(Form owner, BindingSource history)
		:base(owner, EPin.TopRight, new Point(-270, +28), Size.Empty, false)
		{
			InitializeComponent();
			m_history = history;
			Pattern = new Pattern();
			m_tt = new ToolTip();

			// Find combo
			m_combo_pattern.DataSource = history;
			m_combo_pattern.DropDownClosed += (s,a)=>
				{
					var p = m_combo_pattern.SelectedItem;
					if (p == null) return;
					m_history.Remove(p);
					m_history.Insert(0, p);
					m_history.Position = 0;
					UpdateUI();
				};
			m_combo_pattern.TextChanged += (s,a)=>
				{
					Pattern.Expr = m_combo_pattern.Text;
				};
			m_combo_pattern.KeyDown += (s,a) =>
				{
					if (a.KeyCode == Keys.Down && !m_combo_pattern.DroppedDown)
						m_combo_pattern.DroppedDown = true;
				};
			m_history.CurrentItemChanged += (s,a)=>
				{
					var pattern = m_history.Current as Pattern;
					if (pattern == null) return;
					Pattern = new Pattern(pattern);
				};

			// Regex help
			m_btn_regex_help.ToolTip(m_tt, "Displays a quick help guide for regular expressions");
			m_btn_regex_help.Click += (s,a)=>
				{
					RegexHelpUI.Show();
				};

			// Search buttons
			m_btn_find_prev.ToolTip(m_tt, "Search backward through the log.\r\nKeyboard shortcut: <Shift>+<Enter>");
			m_btn_find_prev.Click += (s,a) => RaiseFindPrev(false);

			m_btn_find_next.ToolTip(m_tt, "Search forward through the log.\r\nKeyboard shortcut: <Enter>");
			m_btn_find_next.Click += (s,a) => RaiseFindNext(false);

			m_btn_bookmarkall.ToolTip(m_tt, "Set a bookmark for all found instances of the search pattern");
			m_btn_bookmarkall.Click += (s,a) => BookmarkAll();

			// Pattern type
			m_radio_substring.Checked = Pattern.PatnType == EPattern.Substring;
			m_radio_substring.Click += (s,a)=>
				{
					if (m_radio_substring.Checked) Pattern.PatnType = EPattern.Substring;
				};
			m_radio_wildcard .Checked = Pattern.PatnType == EPattern.Wildcard;
			m_radio_wildcard.Click += (s,a)=>
				{
					if (m_radio_wildcard.Checked) Pattern.PatnType = EPattern.Wildcard;
				};
			m_radio_regex    .Checked = Pattern.PatnType == EPattern.RegularExpression;
			m_radio_regex.Click += (s,a)=>
				{
					if (m_radio_regex.Checked) Pattern.PatnType = EPattern.RegularExpression;
				};

			// Ignore case
			m_check_ignore_case.ToolTip(m_tt, "Check to make searches ignore differences in case");
			m_check_ignore_case.CheckedChanged += (s,a)=>
				{
					Pattern.IgnoreCase = m_check_ignore_case.Checked;
				};

			// Whole line
			m_check_whole_line.ToolTip(m_tt, "Check to have searches match entire lines only");
			m_check_whole_line.CheckedChanged += (s,a) =>
				{
					Pattern.WholeLine = m_check_whole_line.Checked;
				};

			// Invert
			m_check_invert.ToolTip(m_tt, "Check to find instances that do not match the search pattern");
			m_check_invert.CheckedChanged += (s,a)=>
				{
					Pattern.Invert = m_check_invert.Checked;
				};

			// Quick find grid
			m_grid.AutoGenerateColumns = false;
			m_grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName = "Expr"});
			m_grid.DataSource = m_history;

			VisibleChanged += (s,a) =>
				{
					if (Visible)
					{
						m_combo_pattern.Focus();
						m_combo_pattern.Text = Pattern.Expr;
						m_combo_pattern.SelectAll();
					}
				};

			FormClosing += (s,a) =>
				{
					if (a.CloseReason != CloseReason.UserClosing) return;
					Hide();
					a.Cancel = true;
					Owner.Focus();
				};

			Disposed += (s,a) =>
				{
					m_tt.Dispose();
				};
		}

		/// <summary>The current find pattern</summary>
		public Pattern Pattern
		{
			get { return m_pattern; }
			set { m_pattern = value; UpdateUI(); }
		}

		/// <summary>An event called whenever the dialog gets a FindNext command</summary>
		public event Action<bool> FindNext;
		public void RaiseFindNext(bool from_start)
		{
			if (FindNext == null) return;
			FindNext(from_start);
		}

		/// <summary>An event called whenever the dialog gets a FindPrev command</summary>
		public event Action<bool> FindPrev;
		public void RaiseFindPrev(bool from_end)
		{
			if (FindPrev == null) return;
			FindPrev(from_end);
		}

		/// <summary>An event called when all matches should be bookmarked</summary>
		public event Action BookmarkAll;
		public void BookmarkAllResults()
		{
			if (BookmarkAll == null) return;
			BookmarkAll();
		}

		/// <summary>Handle global command keys</summary>
		protected override bool ProcessCmdKey(ref Message msg, Keys key_data)
		{
			switch (key_data)
			{
			default:
				var main = Owner as Main;
				if (main != null && main.HandleKeyDown(this, key_data)) return true;
				return base.ProcessCmdKey(ref msg, key_data);

			case Keys.Escape:
				Close();
				return true;

			case Keys.Enter:
				m_combo_pattern.DroppedDown = false;
				RaiseFindNext(false);
				return true;

			case Keys.Shift|Keys.Enter:
				m_combo_pattern.DroppedDown = false;
				RaiseFindPrev(false);
				return true;

			case Keys.Control|Keys.Enter:
				m_combo_pattern.DroppedDown = false;
				RaiseFindNext(true);
				return true;

			case Keys.Shift|Keys.Control|Keys.Enter:
				m_combo_pattern.DroppedDown = false;
				RaiseFindPrev(true);
				return true;
			}
		}

		/// <summary>Set the state of the controls</summary>
		private void UpdateUI()
		{
			if (m_in_update_ui) return;
			try
			{
				m_in_update_ui = true;
				m_combo_pattern.Text        = Pattern.Expr;
				m_radio_substring.Checked   = Pattern.PatnType == EPattern.Substring;
				m_radio_wildcard .Checked   = Pattern.PatnType == EPattern.Wildcard;
				m_radio_regex    .Checked   = Pattern.PatnType == EPattern.RegularExpression;
				m_check_ignore_case.Checked = Pattern.IgnoreCase;
				m_check_invert.Checked      = Pattern.Invert;
			}
			finally { m_in_update_ui = false; }
		}
		private bool m_in_update_ui;

		/// <summary>Return the Form for displaying the regex quick help (lazy loaded)</summary>
		private HelpUI RegexHelpUI
		{
			get { return m_dlg_help ?? (m_dlg_help = PatternUI.CreateRegexHelpUI(Owner)); }
			//HelpUI.FromHtml(Owner, Resources.regex_quick_ref, "Regular Expressions Quick Reference", new Point(1,1) ,new Size(640,480) ,EPin.TopRight))
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
			this.m_check_ignore_case = new System.Windows.Forms.CheckBox();
			this.m_check_invert = new System.Windows.Forms.CheckBox();
			this.m_radio_regex = new System.Windows.Forms.RadioButton();
			this.m_radio_wildcard = new System.Windows.Forms.RadioButton();
			this.m_radio_substring = new System.Windows.Forms.RadioButton();
			this.m_table = new System.Windows.Forms.TableLayoutPanel();
			this.m_panel_top = new System.Windows.Forms.Panel();
			this.m_check_whole_line = new System.Windows.Forms.CheckBox();
			this.m_btn_regex_help = new System.Windows.Forms.Button();
			this.m_lbl_prev_find_patterns = new System.Windows.Forms.Label();
			this.m_combo_pattern = new RyLogViewer.ComboBox();
			this.m_grid = new RyLogViewer.DataGridView();
			this.m_btn_bookmarkall = new System.Windows.Forms.Button();
			this.m_table.SuspendLayout();
			this.m_panel_top.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.SuspendLayout();
			//
			// m_btn_find_next
			//
			this.m_btn_find_next.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_find_next.Location = new System.Drawing.Point(157, 102);
			this.m_btn_find_next.Name = "m_btn_find_next";
			this.m_btn_find_next.Size = new System.Drawing.Size(94, 23);
			this.m_btn_find_next.TabIndex = 8;
			this.m_btn_find_next.Text = "Find &Next";
			this.m_btn_find_next.UseVisualStyleBackColor = true;
			//
			// m_btn_find_prev
			//
			this.m_btn_find_prev.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_find_prev.Location = new System.Drawing.Point(157, 73);
			this.m_btn_find_prev.Name = "m_btn_find_prev";
			this.m_btn_find_prev.Size = new System.Drawing.Size(94, 23);
			this.m_btn_find_prev.TabIndex = 7;
			this.m_btn_find_prev.Text = "Find &Previous";
			this.m_btn_find_prev.UseVisualStyleBackColor = true;
			//
			// m_lbl_find_what
			//
			this.m_lbl_find_what.AutoSize = true;
			this.m_lbl_find_what.Location = new System.Drawing.Point(1, 4);
			this.m_lbl_find_what.Name = "m_lbl_find_what";
			this.m_lbl_find_what.Size = new System.Drawing.Size(56, 13);
			this.m_lbl_find_what.TabIndex = 4;
			this.m_lbl_find_what.Text = "Find what:";
			//
			// m_check_ignore_case
			//
			this.m_check_ignore_case.AutoSize = true;
			this.m_check_ignore_case.Location = new System.Drawing.Point(10, 65);
			this.m_check_ignore_case.Name = "m_check_ignore_case";
			this.m_check_ignore_case.Size = new System.Drawing.Size(83, 17);
			this.m_check_ignore_case.TabIndex = 4;
			this.m_check_ignore_case.Text = "Ignore &Case";
			this.m_check_ignore_case.UseVisualStyleBackColor = true;
			//
			// m_check_invert
			//
			this.m_check_invert.AutoSize = true;
			this.m_check_invert.Location = new System.Drawing.Point(95, 65);
			this.m_check_invert.Name = "m_check_invert";
			this.m_check_invert.Size = new System.Drawing.Size(56, 30);
			this.m_check_invert.TabIndex = 5;
			this.m_check_invert.Text = "&Invert \r\nMatch";
			this.m_check_invert.UseVisualStyleBackColor = true;
			//
			// m_radio_regex
			//
			this.m_radio_regex.AutoSize = true;
			this.m_radio_regex.Location = new System.Drawing.Point(149, 45);
			this.m_radio_regex.Name = "m_radio_regex";
			this.m_radio_regex.Size = new System.Drawing.Size(75, 17);
			this.m_radio_regex.TabIndex = 3;
			this.m_radio_regex.Text = "&Reg. Expr.";
			this.m_radio_regex.UseVisualStyleBackColor = true;
			//
			// m_radio_wildcard
			//
			this.m_radio_wildcard.AutoSize = true;
			this.m_radio_wildcard.Location = new System.Drawing.Point(76, 45);
			this.m_radio_wildcard.Name = "m_radio_wildcard";
			this.m_radio_wildcard.Size = new System.Drawing.Size(67, 17);
			this.m_radio_wildcard.TabIndex = 2;
			this.m_radio_wildcard.Text = "Wil&dcard";
			this.m_radio_wildcard.UseVisualStyleBackColor = true;
			//
			// m_radio_substring
			//
			this.m_radio_substring.AutoSize = true;
			this.m_radio_substring.Location = new System.Drawing.Point(4, 45);
			this.m_radio_substring.Name = "m_radio_substring";
			this.m_radio_substring.Size = new System.Drawing.Size(69, 17);
			this.m_radio_substring.TabIndex = 1;
			this.m_radio_substring.Text = "&Substring";
			this.m_radio_substring.UseVisualStyleBackColor = true;
			//
			// m_table
			//
			this.m_table.ColumnCount = 1;
			this.m_table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table.Controls.Add(this.m_panel_top, 0, 0);
			this.m_table.Controls.Add(this.m_grid, 0, 1);
			this.m_table.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table.Location = new System.Drawing.Point(0, 0);
			this.m_table.Name = "m_table";
			this.m_table.RowCount = 2;
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.Size = new System.Drawing.Size(261, 305);
			this.m_table.TabIndex = 29;
			//
			// m_panel_top
			//
			this.m_panel_top.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_top.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_panel_top.Controls.Add(this.m_btn_bookmarkall);
			this.m_panel_top.Controls.Add(this.m_check_whole_line);
			this.m_panel_top.Controls.Add(this.m_btn_regex_help);
			this.m_panel_top.Controls.Add(this.m_lbl_prev_find_patterns);
			this.m_panel_top.Controls.Add(this.m_combo_pattern);
			this.m_panel_top.Controls.Add(this.m_lbl_find_what);
			this.m_panel_top.Controls.Add(this.m_btn_find_prev);
			this.m_panel_top.Controls.Add(this.m_btn_find_next);
			this.m_panel_top.Controls.Add(this.m_radio_regex);
			this.m_panel_top.Controls.Add(this.m_check_ignore_case);
			this.m_panel_top.Controls.Add(this.m_radio_wildcard);
			this.m_panel_top.Controls.Add(this.m_check_invert);
			this.m_panel_top.Controls.Add(this.m_radio_substring);
			this.m_panel_top.Location = new System.Drawing.Point(0, 0);
			this.m_panel_top.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_top.Name = "m_panel_top";
			this.m_panel_top.Size = new System.Drawing.Size(261, 140);
			this.m_panel_top.TabIndex = 0;
			//
			// m_check_whole_line
			//
			this.m_check_whole_line.AutoSize = true;
			this.m_check_whole_line.Location = new System.Drawing.Point(10, 81);
			this.m_check_whole_line.Name = "m_check_whole_line";
			this.m_check_whole_line.Size = new System.Drawing.Size(80, 17);
			this.m_check_whole_line.TabIndex = 30;
			this.m_check_whole_line.Text = "&Whole Line";
			this.m_check_whole_line.UseVisualStyleBackColor = true;
			//
			// m_btn_regex_help
			//
			this.m_btn_regex_help.Location = new System.Drawing.Point(230, 43);
			this.m_btn_regex_help.Name = "m_btn_regex_help";
			this.m_btn_regex_help.Size = new System.Drawing.Size(21, 21);
			this.m_btn_regex_help.TabIndex = 6;
			this.m_btn_regex_help.Text = "?";
			this.m_btn_regex_help.UseVisualStyleBackColor = true;
			//
			// m_lbl_prev_find_patterns
			//
			this.m_lbl_prev_find_patterns.AutoSize = true;
			this.m_lbl_prev_find_patterns.Location = new System.Drawing.Point(1, 125);
			this.m_lbl_prev_find_patterns.Name = "m_lbl_prev_find_patterns";
			this.m_lbl_prev_find_patterns.Size = new System.Drawing.Size(130, 13);
			this.m_lbl_prev_find_patterns.TabIndex = 29;
			this.m_lbl_prev_find_patterns.Text = "Previous Search Patterns:";
			//
			// m_combo_pattern
			//
			this.m_combo_pattern.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_pattern.FormattingEnabled = true;
			this.m_combo_pattern.Location = new System.Drawing.Point(4, 20);
			this.m_combo_pattern.Name = "m_combo_pattern";
			this.m_combo_pattern.Size = new System.Drawing.Size(247, 21);
			this.m_combo_pattern.TabIndex = 0;
			//
			// m_grid
			//
			this.m_grid.AllowUserToAddRows = false;
			this.m_grid.AllowUserToResizeColumns = false;
			this.m_grid.AllowUserToResizeRows = false;
			this.m_grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid.BackgroundColor = System.Drawing.SystemColors.Control;
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.ColumnHeadersVisible = false;
			this.m_grid.Location = new System.Drawing.Point(0, 140);
			this.m_grid.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid.MultiSelect = false;
			this.m_grid.Name = "m_grid";
			this.m_grid.ReadOnly = true;
			this.m_grid.RowHeadersVisible = false;
			this.m_grid.RowTemplate.Height = 18;
			this.m_grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid.Size = new System.Drawing.Size(261, 165);
			this.m_grid.TabIndex = 0;
			//
			// m_btn_bookmarkall
			//
			this.m_btn_bookmarkall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_bookmarkall.Location = new System.Drawing.Point(28, 99);
			this.m_btn_bookmarkall.Name = "m_btn_bookmarkall";
			this.m_btn_bookmarkall.Size = new System.Drawing.Size(94, 23);
			this.m_btn_bookmarkall.TabIndex = 31;
			this.m_btn_bookmarkall.Text = "&Bookmark All";
			this.m_btn_bookmarkall.UseVisualStyleBackColor = true;
			//
			// FindUI
			//
			this.AcceptButton = this.m_btn_find_next;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.ClientSize = new System.Drawing.Size(261, 305);
			this.Controls.Add(this.m_table);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(277, 145);
			this.Name = "FindUI";
			this.Text = "Find...";
			this.m_table.ResumeLayout(false);
			this.m_panel_top.ResumeLayout(false);
			this.m_panel_top.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.ResumeLayout(false);
		}

		#endregion
	}
}
