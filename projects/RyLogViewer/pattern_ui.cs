using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using pr.util;

namespace RyLogViewer
{
	public class PatternUI :UserControl
	{
		enum BtnImageIdx { AddNew = 0, Save = 1 }
		
		// Some of these are public to allow clients to hide bits of the UI
		private readonly ToolTip m_tt;
		private Pattern     m_pattern;
		private ImageList   m_image_list;
		private Button      m_btn_regex_help;
		public  CheckBox    m_check_binary;
		public  CheckBox    m_check_active;
		public  CheckBox    m_check_invert;
		public  CheckBox    m_check_ignore_case;
		public  Button      m_btn_add;
		private Label       m_lbl_hl_regexp;
		public  TextBox     m_edit_pattern;
		private RadioButton m_radio_substring;
		private RadioButton m_radio_wildcard;
		private RadioButton m_radio_regex;
		public  RichTextBox m_edit_test;
		
		/// <summary>The pattern being controlled by this UI</summary>
		public Pattern Pattern { get { return m_pattern; } }
		
		/// <summary>True if the editted pattern is a new instance</summary>
		public bool IsNew { get; private set; }
		
		/// <summary>Raised when the 'Add' button is hit and the pattern field contains a valid pattern</summary>
		public event EventHandler Add;
		
		public PatternUI()
		{
			InitializeComponent();
			m_pattern = null;
			m_tt = new ToolTip();
			
			// Pattern
			m_edit_pattern.ToolTip(m_tt, "A substring or regular expression to match");
			m_edit_pattern.TextChanged += (s,a)=>
				{
					Pattern.Expr = m_edit_pattern.Text;
					UpdateUI();
				};
			m_edit_pattern.KeyDown += (s,a)=>
				{
					if (a.KeyCode == Keys.Enter)
						m_btn_add.PerformClick();
				};
			
			// Regex help
			m_btn_regex_help.ToolTip(m_tt, "Displays a quick help guide for regular expressions");
			m_btn_regex_help.Click += (s,a)=>
				{
					ShowQuickHelp();
				};
			
			// Add/Update
			m_btn_add.ToolTip(m_tt, "Adds a new pattern, or updates an existing pattern");
			m_btn_add.Click += (s,a)=>
				{
					if (Add == null) return;
					if (Pattern.Expr.Length == 0) return;
					if (!Pattern.ExprValid) return;
					Add(this, EventArgs.Empty);
				};
			
			// Active
			m_check_active.ToolTip(m_tt, "Uncheck to temporarily disable the pattern");
			m_check_active.CheckedChanged += (s,a)=>
				{
					Pattern.Active = m_check_active.Checked;
					UpdateUI();
				};
			
			// Substring
			m_radio_substring.ToolTip(m_tt, "Match any occurance of the pattern as a substring");
			m_radio_substring.Click += (s,a)=>
				{
					if (m_radio_substring.Checked) Pattern.PatnType = EPattern.Substring;
					UpdateUI();
				};
			
			// Wildcard
			m_radio_wildcard.ToolTip(m_tt, "Match using wildcards, where '*' matches any number of characters and '?' matches any single character");
			m_radio_wildcard.Click += (s,a)=>
				{
					if (m_radio_wildcard.Checked) Pattern.PatnType = EPattern.Wildcard;
					UpdateUI();
				};
			
			// Regex
			m_radio_regex.ToolTip(m_tt, "Match using a regular expression");
			m_radio_regex.Click += (s,a)=>
				{
					if (m_radio_regex.Checked) Pattern.PatnType = EPattern.RegularExpression;
					UpdateUI();
				};
			
			// Ignore case
			m_check_ignore_case.ToolTip(m_tt, "Check if the pattern should ignore case when matching");
			m_check_ignore_case.CheckedChanged += (s,a)=>
				{
					Pattern.IgnoreCase = m_check_ignore_case.Checked;
					UpdateUI();
				};
			
			// Invert
			m_check_invert.ToolTip(m_tt, "Invert the match result. e.g the pattern 'a' matches anything without the letter 'a' when this option is checked");
			m_check_invert.CheckedChanged += (s,a)=>
				{
					Pattern.Invert = m_check_invert.Checked;
					UpdateUI();
				};
			
			// Binary
			m_check_binary.ToolTip(m_tt, "When checked, any match within a value is treated as if the whole value matches");
			m_check_binary.CheckedChanged += (s,a)=>
				{
					Pattern.BinaryMatch = m_check_binary.Checked;
					UpdateUI();
				};
			
			// Test text
			m_edit_test.ToolTip(m_tt, "A area for testing your pattern. Add any text you like here");
			m_edit_test.TextChanged += (s,a)=>
				{
					UpdateUI();
				};
		}

		/// <summary>Select 'pat' as a new pattern</summary>
		public void NewPattern(Pattern pat)
		{
			IsNew = true;
			m_pattern = pat;
			m_btn_add.ImageIndex = (int)BtnImageIdx.AddNew;
			m_btn_add.ToolTip(m_tt, "Add this new pattern");
			UpdateUI();
		}

		/// <summary>Select a pattern into the UI for editting</summary>
		public void EditPattern(Pattern pat)
		{
			IsNew = false;
			m_pattern = pat;
			m_btn_add.ImageIndex = (int)BtnImageIdx.Save;
			m_btn_add.ToolTip(m_tt, "Finish editing this pattern");
			UpdateUI();
		}
		
		/// <summary>Show a window containing quick help info</summary>
		private void ShowQuickHelp()
		{
			var win = new Form
			{
				FormBorderStyle = FormBorderStyle.SizableToolWindow,
				StartPosition = FormStartPosition.Manual,
				ShowInTaskbar = true,
			};
			var edit = new WebBrowser
			{
				Dock = DockStyle.Fill,
			};
			win.Controls.Add(edit);

			const string RegexHelpNotFound = @"<p>Regular Expression Quick Reference resource data not found</p>";
			Stream help = Assembly.GetExecutingAssembly().GetManifestResourceStream("RyLogViewer.docs.RegexQuickRef.html");
			edit.DocumentText = (help == null) ? RegexHelpNotFound : new StreamReader(help).ReadToEnd();
			
			win.Location = PointToScreen(Location) + new Size(Width, 0);
			win.Size = new Size(640,480);
			win.Show(this);
		}
		
		/// <summary>Update UI elements based on the current settings</summary>
		private void UpdateUI()
		{
			SuspendLayout();
			m_edit_pattern.Text         = Pattern.Expr;
			m_check_active.Checked      = Pattern.Active;
			m_radio_substring.Checked   = Pattern.PatnType == EPattern.Substring;
			m_radio_wildcard.Checked    = Pattern.PatnType == EPattern.Wildcard;
			m_radio_regex.Checked       = Pattern.PatnType == EPattern.RegularExpression;
			m_check_ignore_case.Checked = Pattern.IgnoreCase;
			m_check_invert.Checked      = Pattern.Invert;
			m_check_binary.Checked      = Pattern.BinaryMatch;
			
			m_btn_add.Enabled = m_edit_pattern.Text.Length != 0;
			
			// Highlight the expression background to show valid regexp
			m_edit_pattern.BackColor = Pattern.ExprValid ? Color.LightGreen : Color.LightSalmon;
			
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
			this.m_check_ignore_case = new System.Windows.Forms.CheckBox();
			this.m_btn_add = new System.Windows.Forms.Button();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_lbl_hl_regexp = new System.Windows.Forms.Label();
			this.m_edit_pattern = new System.Windows.Forms.TextBox();
			this.m_edit_test = new System.Windows.Forms.RichTextBox();
			this.m_btn_regex_help = new System.Windows.Forms.Button();
			this.m_check_binary = new System.Windows.Forms.CheckBox();
			this.m_radio_substring = new System.Windows.Forms.RadioButton();
			this.m_radio_wildcard = new System.Windows.Forms.RadioButton();
			this.m_radio_regex = new System.Windows.Forms.RadioButton();
			this.SuspendLayout();
			// 
			// m_check_active
			// 
			this.m_check_active.AutoSize = true;
			this.m_check_active.Location = new System.Drawing.Point(201, 26);
			this.m_check_active.Name = "m_check_active";
			this.m_check_active.Size = new System.Drawing.Size(56, 17);
			this.m_check_active.TabIndex = 19;
			this.m_check_active.Text = "Active";
			this.m_check_active.UseVisualStyleBackColor = true;
			// 
			// m_check_invert
			// 
			this.m_check_invert.AutoSize = true;
			this.m_check_invert.Location = new System.Drawing.Point(112, 43);
			this.m_check_invert.Name = "m_check_invert";
			this.m_check_invert.Size = new System.Drawing.Size(86, 17);
			this.m_check_invert.TabIndex = 18;
			this.m_check_invert.Text = "Invert Match";
			this.m_check_invert.UseVisualStyleBackColor = true;
			// 
			// m_check_ignore_case
			// 
			this.m_check_ignore_case.AutoSize = true;
			this.m_check_ignore_case.Location = new System.Drawing.Point(112, 27);
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
			this.m_btn_add.Location = new System.Drawing.Point(282, 3);
			this.m_btn_add.Name = "m_btn_add";
			this.m_btn_add.Size = new System.Drawing.Size(46, 46);
			this.m_btn_add.TabIndex = 2;
			this.m_btn_add.UseVisualStyleBackColor = true;
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "edit_add.png");
			this.m_image_list.Images.SetKeyName(1, "edit_save.png");
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
			this.m_edit_pattern.Size = new System.Drawing.Size(204, 20);
			this.m_edit_pattern.TabIndex = 0;
			// 
			// m_edit_test
			// 
			this.m_edit_test.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_test.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_edit_test.Location = new System.Drawing.Point(3, 79);
			this.m_edit_test.Name = "m_edit_test";
			this.m_edit_test.Size = new System.Drawing.Size(328, 22);
			this.m_edit_test.TabIndex = 20;
			this.m_edit_test.Text = "Enter text here to test your pattern";
			// 
			// m_btn_regex_help
			// 
			this.m_btn_regex_help.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_regex_help.Location = new System.Drawing.Point(259, 3);
			this.m_btn_regex_help.Name = "m_btn_regex_help";
			this.m_btn_regex_help.Size = new System.Drawing.Size(22, 21);
			this.m_btn_regex_help.TabIndex = 21;
			this.m_btn_regex_help.Text = "?";
			this.m_btn_regex_help.UseVisualStyleBackColor = true;
			// 
			// m_check_binary
			// 
			this.m_check_binary.AutoSize = true;
			this.m_check_binary.Location = new System.Drawing.Point(201, 43);
			this.m_check_binary.Name = "m_check_binary";
			this.m_check_binary.Size = new System.Drawing.Size(80, 17);
			this.m_check_binary.TabIndex = 22;
			this.m_check_binary.Text = "Full Column";
			this.m_check_binary.UseVisualStyleBackColor = true;
			// 
			// m_radio_substring
			// 
			this.m_radio_substring.AutoSize = true;
			this.m_radio_substring.Location = new System.Drawing.Point(6, 26);
			this.m_radio_substring.Name = "m_radio_substring";
			this.m_radio_substring.Size = new System.Drawing.Size(69, 17);
			this.m_radio_substring.TabIndex = 23;
			this.m_radio_substring.TabStop = true;
			this.m_radio_substring.Text = "Substring";
			this.m_radio_substring.UseVisualStyleBackColor = true;
			// 
			// m_radio_wildcard
			// 
			this.m_radio_wildcard.AutoSize = true;
			this.m_radio_wildcard.Location = new System.Drawing.Point(6, 42);
			this.m_radio_wildcard.Name = "m_radio_wildcard";
			this.m_radio_wildcard.Size = new System.Drawing.Size(67, 17);
			this.m_radio_wildcard.TabIndex = 24;
			this.m_radio_wildcard.TabStop = true;
			this.m_radio_wildcard.Text = "Wildcard";
			this.m_radio_wildcard.UseVisualStyleBackColor = true;
			// 
			// m_radio_regex
			// 
			this.m_radio_regex.AutoSize = true;
			this.m_radio_regex.Location = new System.Drawing.Point(6, 58);
			this.m_radio_regex.Name = "m_radio_regex";
			this.m_radio_regex.Size = new System.Drawing.Size(116, 17);
			this.m_radio_regex.TabIndex = 25;
			this.m_radio_regex.TabStop = true;
			this.m_radio_regex.Text = "Regular Expression";
			this.m_radio_regex.UseVisualStyleBackColor = true;
			// 
			// PatternUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Controls.Add(this.m_radio_regex);
			this.Controls.Add(this.m_radio_wildcard);
			this.Controls.Add(this.m_radio_substring);
			this.Controls.Add(this.m_check_binary);
			this.Controls.Add(this.m_btn_regex_help);
			this.Controls.Add(this.m_edit_test);
			this.Controls.Add(this.m_check_active);
			this.Controls.Add(this.m_check_invert);
			this.Controls.Add(this.m_check_ignore_case);
			this.Controls.Add(this.m_btn_add);
			this.Controls.Add(this.m_lbl_hl_regexp);
			this.Controls.Add(this.m_edit_pattern);
			this.MinimumSize = new System.Drawing.Size(334, 104);
			this.Name = "PatternUI";
			this.Size = new System.Drawing.Size(334, 104);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
