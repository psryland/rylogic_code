using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.gui;
using pr.util;

namespace RyLogViewer
{
	interface IPatternUI
	{
		/// <summary>Set a new pattern for the UI</summary>
		void NewPattern(IPattern pat);

		/// <summary>Select a pattern into the UI for editing</summary>
		void EditPattern(IPattern pat);

		/// <summary>Set focus to the primary input field</summary>
		void FocusInput();
	}
	public class PatternUI :UserControl ,IPatternUI
	{
		enum BtnImageIdx { AddNew = 0, Save = 1 }
		private const string RegexQuickRef = "RyLogViewer.docs.RegexQuickRef.html";
		
		// Some of these are public to allow clients to hide bits of the UI
		private readonly ToolTip m_tt;
		private HelpUI           m_dlg_help;
		private Pattern          m_pattern;
		private ImageList        m_image_list;
		private Button           m_btn_regex_help;
		private CheckBox         m_check_invert;
		private CheckBox         m_check_ignore_case;
		private Button           m_btn_add;
		private Label            m_lbl_match;
		private TextBox          m_edit_match;
		private RadioButton      m_radio_substring;
		private RadioButton      m_radio_wildcard;
		private RadioButton      m_radio_regex;
		private Panel            m_group_patntype;
		private Label            m_lbl_match_type;
		private SplitContainer   m_split;
		private DataGridView     m_grid_grps;
		private Label            m_lbl_groups;
		private RichTextBox      m_edit_test;
		
		/// <summary>The pattern being controlled by this UI</summary>
		public Pattern Pattern { get { return m_pattern; } }
		
		/// <summary>Access to the test text field</summary>
		public string TestText { get { return m_edit_test.Text; } set { m_edit_test.Text = value; } }

		/// <summary>True if the edited pattern is a new instance</summary>
		public bool IsNew { get; private set; }
		
		/// <summary>Return the Form for displaying the regex quick help (lazy loaded)</summary>
		private HelpUI RegexHelpUI
		{
			get
			{
				Debug.Assert(ParentForm != null);
				return m_dlg_help ?? (m_dlg_help = HelpUI.FromHtml(ParentForm, Resources.regex_quick_ref, "Regular Expressions Quick Reference", new Size(1,1) ,new Size(640,480) ,ToolForm.EPin.TopRight));
			}
		}
		
		/// <summary>Raised when the 'Add' button is hit and the pattern field contains a valid pattern</summary>
		public event EventHandler Add;
		
		public PatternUI()
		{
			InitializeComponent();
			m_pattern = null;
			m_tt = new ToolTip();
			string tt;

			// Pattern
			tt = "A substring or regular expression to match";
			m_lbl_match.ToolTip(m_tt, tt);
			m_edit_match.ToolTip(m_tt, tt);
			m_edit_match.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					Pattern.Expr = m_edit_match.Text;
					UpdateUI();
				};
			m_edit_match.KeyDown += (s,a)=>
				{
					a.Handled = a.KeyCode == Keys.Enter;
					if (a.Handled) m_btn_add.PerformClick();
				};
			
			// Regex help
			m_btn_regex_help.ToolTip(m_tt, "Displays a quick help guide for regular expressions");
			m_btn_regex_help.Click += (s,a)=>
				{
					RegexHelpUI.Display();
				};
			
			// Add/Update
			m_btn_add.ToolTip(m_tt, "Adds a new pattern, or updates an existing pattern");
			m_btn_add.Click += (s,a)=>
				{
					if (Add == null) return;
					if (!Pattern.ExprValid) return;
					Add(this, EventArgs.Empty);
				};
			
			// Substring
			m_radio_substring.ToolTip(m_tt, "Match any occurrence of the pattern as a substring");
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
			m_check_ignore_case.ToolTip(m_tt, "Enable to have the pattern ignore case when matching");
			m_check_ignore_case.Click += (s,a)=>
				{
					Pattern.IgnoreCase = m_check_ignore_case.Checked;
					UpdateUI();
				};
			
			// Invert
			m_check_invert.ToolTip(m_tt, "Invert the match result. e.g the pattern 'a' matches anything without the letter 'a' when this option is checked");
			m_check_invert.Click += (s,a)=>
				{
					Pattern.Invert = m_check_invert.Checked;
					UpdateUI();
				};
			
			// Test text
			m_edit_test.ToolTip(m_tt, "A area for testing your pattern. Add any text you like here");
			m_edit_test.TextChanged += (s,a)=>
				{
					if (!((RichTextBox)s).Modified) return;
					UpdateUI();
				};
			
			// Groups
			m_grid_grps.AutoGenerateColumns = false;
			m_grid_grps.Columns.Add(new DataGridViewTextBoxColumn{Name="Tag"   ,HeaderText="Tag"   ,FillWeight=1 ,DataPropertyName = "Key"  });
			m_grid_grps.Columns.Add(new DataGridViewTextBoxColumn{Name="Value" ,HeaderText="Value" ,FillWeight=2 ,DataPropertyName = "Value"});
		}

		/// <summary>Set focus to the primary input field</summary>
		public void FocusInput()
		{
			m_edit_match.Focus();
		}

		/// <summary>Select 'pat' as a new pattern</summary>
		public void NewPattern(IPattern pat)
		{
			IsNew = true;
			m_pattern = (Pattern)pat;
			m_btn_add.ImageIndex = (int)BtnImageIdx.AddNew;
			m_btn_add.ToolTip(m_tt, "Add this new pattern");
			UpdateUI();
		}

		/// <summary>Select a pattern into the UI for editing</summary>
		public void EditPattern(IPattern pat)
		{
			IsNew = false;
			m_pattern = (Pattern)pat;
			m_btn_add.ImageIndex = (int)BtnImageIdx.Save;
			m_btn_add.ToolTip(m_tt, "Finish editing this pattern");
			UpdateUI();
		}
		
		/// <summary>Update UI elements based on the current settings</summary>
		private void UpdateUI()
		{
			if (m_in_update_iu) return;
			try
			{
				m_in_update_iu = true;
				SuspendLayout();
				m_edit_match.Text           = Pattern.Expr;
				m_radio_substring.Checked   = Pattern.PatnType == EPattern.Substring;
				m_radio_wildcard.Checked    = Pattern.PatnType == EPattern.Wildcard;
				m_radio_regex.Checked       = Pattern.PatnType == EPattern.RegularExpression;
				m_check_ignore_case.Checked = Pattern.IgnoreCase;
				m_check_invert.Checked      = Pattern.Invert;
				
				m_btn_add.Enabled = Pattern.ExprValid;
				
				// Highlight the expression background to show valid regex
				m_edit_match.BackColor = Misc.FieldBkColor(Pattern.ExprValid);
			
				// Preserve the current caret position
				int start = m_edit_test.SelectionStart;
				int length = m_edit_test.SelectionLength;
				m_edit_test.SelectAll();
				m_edit_test.SelectionBackColor = Color.White;
				foreach (var r in Pattern.Match(m_edit_test.Text))
				{
					m_edit_test.SelectionStart     = r.Index;
					m_edit_test.SelectionLength    = r.Count;
					m_edit_test.SelectionBackColor = Color.LightBlue;
				}
				m_edit_test.SelectionStart  = start;
				m_edit_test.SelectionLength = length;

				// Populate the groups grid
				m_grid_grps.DataSource = Pattern.CaptureGroups(m_edit_test.Text).ToList();
			}
			finally
			{
				m_in_update_iu = false;
				ResumeLayout();
			}
		}
		private bool m_in_update_iu;
		
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
			this.m_check_invert = new System.Windows.Forms.CheckBox();
			this.m_check_ignore_case = new System.Windows.Forms.CheckBox();
			this.m_btn_add = new System.Windows.Forms.Button();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_lbl_match = new System.Windows.Forms.Label();
			this.m_edit_match = new System.Windows.Forms.TextBox();
			this.m_edit_test = new System.Windows.Forms.RichTextBox();
			this.m_btn_regex_help = new System.Windows.Forms.Button();
			this.m_radio_substring = new System.Windows.Forms.RadioButton();
			this.m_radio_wildcard = new System.Windows.Forms.RadioButton();
			this.m_radio_regex = new System.Windows.Forms.RadioButton();
			this.m_group_patntype = new System.Windows.Forms.Panel();
			this.m_lbl_match_type = new System.Windows.Forms.Label();
			this.m_split = new System.Windows.Forms.SplitContainer();
			this.m_grid_grps = new System.Windows.Forms.DataGridView();
			this.m_lbl_groups = new System.Windows.Forms.Label();
			this.m_group_patntype.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split)).BeginInit();
			this.m_split.Panel1.SuspendLayout();
			this.m_split.Panel2.SuspendLayout();
			this.m_split.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_grps)).BeginInit();
			this.SuspendLayout();
			// 
			// m_check_invert
			// 
			this.m_check_invert.AutoSize = true;
			this.m_check_invert.Location = new System.Drawing.Point(164, 52);
			this.m_check_invert.Name = "m_check_invert";
			this.m_check_invert.Size = new System.Drawing.Size(86, 17);
			this.m_check_invert.TabIndex = 3;
			this.m_check_invert.Text = "Invert Match";
			this.m_check_invert.UseVisualStyleBackColor = true;
			// 
			// m_check_ignore_case
			// 
			this.m_check_ignore_case.AutoSize = true;
			this.m_check_ignore_case.Location = new System.Drawing.Point(75, 52);
			this.m_check_ignore_case.Name = "m_check_ignore_case";
			this.m_check_ignore_case.Size = new System.Drawing.Size(83, 17);
			this.m_check_ignore_case.TabIndex = 2;
			this.m_check_ignore_case.Text = "Ignore Case";
			this.m_check_ignore_case.UseVisualStyleBackColor = true;
			// 
			// m_btn_add
			// 
			this.m_btn_add.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_add.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.m_btn_add.ImageIndex = 0;
			this.m_btn_add.ImageList = this.m_image_list;
			this.m_btn_add.Location = new System.Drawing.Point(356, 3);
			this.m_btn_add.Name = "m_btn_add";
			this.m_btn_add.Size = new System.Drawing.Size(46, 46);
			this.m_btn_add.TabIndex = 8;
			this.m_btn_add.UseVisualStyleBackColor = true;
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "edit_add.png");
			this.m_image_list.Images.SetKeyName(1, "edit_save.png");
			// 
			// m_lbl_match
			// 
			this.m_lbl_match.AutoSize = true;
			this.m_lbl_match.Location = new System.Drawing.Point(30, 32);
			this.m_lbl_match.Name = "m_lbl_match";
			this.m_lbl_match.Size = new System.Drawing.Size(40, 13);
			this.m_lbl_match.TabIndex = 14;
			this.m_lbl_match.Text = "Match:";
			this.m_lbl_match.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_edit_match
			// 
			this.m_edit_match.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_match.Location = new System.Drawing.Point(75, 29);
			this.m_edit_match.Name = "m_edit_match";
			this.m_edit_match.Size = new System.Drawing.Size(252, 20);
			this.m_edit_match.TabIndex = 0;
			// 
			// m_edit_test
			// 
			this.m_edit_test.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_edit_test.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_edit_test.Location = new System.Drawing.Point(0, 0);
			this.m_edit_test.Name = "m_edit_test";
			this.m_edit_test.Size = new System.Drawing.Size(267, 85);
			this.m_edit_test.TabIndex = 0;
			this.m_edit_test.Text = "Enter text here to test your pattern";
			// 
			// m_btn_regex_help
			// 
			this.m_btn_regex_help.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_regex_help.Location = new System.Drawing.Point(333, 3);
			this.m_btn_regex_help.Name = "m_btn_regex_help";
			this.m_btn_regex_help.Size = new System.Drawing.Size(22, 21);
			this.m_btn_regex_help.TabIndex = 7;
			this.m_btn_regex_help.Text = "?";
			this.m_btn_regex_help.UseVisualStyleBackColor = true;
			// 
			// m_radio_substring
			// 
			this.m_radio_substring.AutoSize = true;
			this.m_radio_substring.Location = new System.Drawing.Point(3, 3);
			this.m_radio_substring.Name = "m_radio_substring";
			this.m_radio_substring.Size = new System.Drawing.Size(69, 17);
			this.m_radio_substring.TabIndex = 1;
			this.m_radio_substring.TabStop = true;
			this.m_radio_substring.Text = "Substring";
			this.m_radio_substring.UseVisualStyleBackColor = true;
			// 
			// m_radio_wildcard
			// 
			this.m_radio_wildcard.AutoSize = true;
			this.m_radio_wildcard.Location = new System.Drawing.Point(73, 3);
			this.m_radio_wildcard.Name = "m_radio_wildcard";
			this.m_radio_wildcard.Size = new System.Drawing.Size(67, 17);
			this.m_radio_wildcard.TabIndex = 2;
			this.m_radio_wildcard.TabStop = true;
			this.m_radio_wildcard.Text = "Wildcard";
			this.m_radio_wildcard.UseVisualStyleBackColor = true;
			// 
			// m_radio_regex
			// 
			this.m_radio_regex.AutoSize = true;
			this.m_radio_regex.Location = new System.Drawing.Point(141, 3);
			this.m_radio_regex.Name = "m_radio_regex";
			this.m_radio_regex.Size = new System.Drawing.Size(116, 17);
			this.m_radio_regex.TabIndex = 3;
			this.m_radio_regex.TabStop = true;
			this.m_radio_regex.Text = "Regular Expression";
			this.m_radio_regex.UseVisualStyleBackColor = true;
			// 
			// m_group_patntype
			// 
			this.m_group_patntype.Controls.Add(this.m_radio_substring);
			this.m_group_patntype.Controls.Add(this.m_radio_wildcard);
			this.m_group_patntype.Controls.Add(this.m_radio_regex);
			this.m_group_patntype.Location = new System.Drawing.Point(75, 3);
			this.m_group_patntype.Name = "m_group_patntype";
			this.m_group_patntype.Size = new System.Drawing.Size(258, 23);
			this.m_group_patntype.TabIndex = 1;
			// 
			// m_lbl_match_type
			// 
			this.m_lbl_match_type.AutoSize = true;
			this.m_lbl_match_type.Location = new System.Drawing.Point(3, 8);
			this.m_lbl_match_type.Name = "m_lbl_match_type";
			this.m_lbl_match_type.Size = new System.Drawing.Size(67, 13);
			this.m_lbl_match_type.TabIndex = 15;
			this.m_lbl_match_type.Text = "Match Type:";
			this.m_lbl_match_type.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_split
			// 
			this.m_split.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_split.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_split.Location = new System.Drawing.Point(3, 75);
			this.m_split.Name = "m_split";
			// 
			// m_split.Panel1
			// 
			this.m_split.Panel1.Controls.Add(this.m_edit_test);
			// 
			// m_split.Panel2
			// 
			this.m_split.Panel2.Controls.Add(this.m_grid_grps);
			this.m_split.Size = new System.Drawing.Size(402, 87);
			this.m_split.SplitterDistance = 269;
			this.m_split.TabIndex = 16;
			// 
			// m_grid_grps
			// 
			this.m_grid_grps.AllowUserToAddRows = false;
			this.m_grid_grps.AllowUserToDeleteRows = false;
			this.m_grid_grps.AllowUserToResizeRows = false;
			this.m_grid_grps.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_grps.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
			this.m_grid_grps.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_grid_grps.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_grps.ColumnHeadersVisible = false;
			this.m_grid_grps.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_grps.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.m_grid_grps.Location = new System.Drawing.Point(0, 0);
			this.m_grid_grps.MultiSelect = false;
			this.m_grid_grps.Name = "m_grid_grps";
			this.m_grid_grps.ReadOnly = true;
			this.m_grid_grps.RowHeadersVisible = false;
			this.m_grid_grps.RowTemplate.Height = 16;
			this.m_grid_grps.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_grps.Size = new System.Drawing.Size(127, 85);
			this.m_grid_grps.TabIndex = 0;
			// 
			// m_lbl_groups
			// 
			this.m_lbl_groups.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_groups.AutoSize = true;
			this.m_lbl_groups.Location = new System.Drawing.Point(352, 59);
			this.m_lbl_groups.Name = "m_lbl_groups";
			this.m_lbl_groups.Size = new System.Drawing.Size(53, 13);
			this.m_lbl_groups.TabIndex = 17;
			this.m_lbl_groups.Text = "...Groups:";
			this.m_lbl_groups.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// PatternUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_lbl_groups);
			this.Controls.Add(this.m_split);
			this.Controls.Add(this.m_lbl_match_type);
			this.Controls.Add(this.m_btn_regex_help);
			this.Controls.Add(this.m_lbl_match);
			this.Controls.Add(this.m_edit_match);
			this.Controls.Add(this.m_check_invert);
			this.Controls.Add(this.m_check_ignore_case);
			this.Controls.Add(this.m_btn_add);
			this.Controls.Add(this.m_group_patntype);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.MinimumSize = new System.Drawing.Size(408, 104);
			this.Name = "PatternUI";
			this.Size = new System.Drawing.Size(408, 165);
			this.m_group_patntype.ResumeLayout(false);
			this.m_group_patntype.PerformLayout();
			this.m_split.Panel1.ResumeLayout(false);
			this.m_split.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split)).EndInit();
			this.m_split.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_grps)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
