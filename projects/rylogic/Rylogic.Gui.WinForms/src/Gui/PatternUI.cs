using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WinForms
{
	/// <summary>A user control for editing 'Patterns'</summary>
	public class PatternUI :PatternUIImpl, IPatternUI
	{
		// Notes:
		// Create an instance of this control.
		//  - You can use ShowDialog() to show a default pattern editor form
		//  - Or use FormWrap on the PatternUI to create a form.
		// Use 'NewPattern()' to create a new pattern or 'EditPattern()' to edit an existing pattern.
		// Remember to hook up the Commit event to handle saving the pattern, closing the form, etc

		public const string DefaultTestText = "Enter text here to test your pattern";
		private static readonly Color FieldValid   = Color.LightGreen;
		private static readonly Color FieldInvalid = Color.Salmon;
		private static readonly Color[] BkColors = new[]
		{
			Color.LightGreen, Color.LightBlue, Color.LightCoral, Color.LightSalmon, Color.Violet, Color.LightSkyBlue,
			Color.Aquamarine, Color.Yellow, Color.Orchid, Color.GreenYellow, Color.PaleGreen, Color.Goldenrod, Color.MediumTurquoise
		};

		#region UI Elements
		private ImageList      m_il;
		private Button         m_btn_regex_help;
		private CheckBox       m_check_ignore_case;
		private CheckBox       m_check_whole_line;
		private CheckBox       m_check_invert;
		private Button         m_btn_add;
		private Label          m_lbl_match;
		private TextBox        m_edit_match;
		private RadioButton    m_radio_substring;
		private RadioButton    m_radio_wildcard;
		private RadioButton    m_radio_regex;
		private Panel          m_panel_patntype;
		private Label          m_lbl_match_type;
		private SplitContainer m_split;
		private DataGridView   m_grid_grps;
		private Label          m_lbl_groups;
		private Panel          m_panel_flags;
		private RichTextBox    m_edit_test;
		#endregion

		/// <summary>Creates a Pattern edit control.</summary>
		public PatternUI()
		{
			InitializeComponent();
			Touched = false;

			// Image list 
			m_il.ImageSize = new Size(28, 28);
			m_il.ColorDepth = ColorDepth.Depth32Bit;
			m_il.Images.Add("add", Resources.edit_add);
			m_il.Images.Add("save", Resources.edit_save);

			// Pattern
			// Tool tip set in UpdateUI
			m_edit_match.TextChanged += (s,a)=>
			{
				if (!((TextBox)s).Modified) return;
				Pattern.Expr = m_edit_match.Text;
				Touched = true;
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
				RegexHelpUI.Show();
			};

			// Add/Update
			m_btn_add.ToolTip(m_tt, "Adds a new pattern, or updates an existing pattern");
			m_btn_add.Click += (s,a)=>
			{
				if (!CommitEnabled) return;
				RaiseCommitEvent();
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
			m_check_ignore_case.CheckedChanged += (s,a)=>
			{
				Pattern.IgnoreCase = m_check_ignore_case.Checked;
				Touched = true;
				UpdateUI();
			};

			// Whole line
			m_check_whole_line.ToolTip(m_tt, "If checked, the entire line must match the pattern for it to be considered a match");
			m_check_whole_line.CheckedChanged += (s,a) =>
			{
				Pattern.WholeLine = m_check_whole_line.Checked;
				Touched = true;
				UpdateUI();
			};

			// Invert
			m_check_invert.ToolTip(m_tt, "Invert the match result. e.g the pattern 'a' matches anything without the letter 'a' when this option is checked");
			m_check_invert.CheckedChanged += (s,a)=>
			{
				Pattern.Invert = m_check_invert.Checked;
				Touched = true;
				UpdateUI();
			};

			// Test text
			m_edit_test.ToolTip(m_tt, "An area for testing your pattern.\r\nAdd any text you like here");
			m_edit_test.Text = DefaultTestText;
			m_edit_test.TextChanged += (s,a)=>
			{
				if (!((RichTextBox)s).Modified) return;
				UpdateUI();
			};
			int last_selected_line = -1;
			m_edit_test.SelectionChanged += (s,a) =>
			{
				if (m_edit_test.SelectionLength != 0) return;
				var idx = m_edit_test.GetLineFromCharIndex(m_edit_test.SelectionStart);
				if (last_selected_line != idx) last_selected_line = idx; else return;
				UpdateUI();
			};

			// Groups
			m_grid_grps.AutoGenerateColumns = false;
			m_grid_grps.Columns.Add(new DataGridViewTextBoxColumn{Name="Tag"   ,HeaderText="Tag"   ,FillWeight=1 ,DataPropertyName = "Key"  , ToolTipText = "The names of the capture groups identified in the match pattern"});
			m_grid_grps.Columns.Add(new DataGridViewTextBoxColumn{Name="Value" ,HeaderText="Value" ,FillWeight=2 ,DataPropertyName = "Value", ToolTipText = "The values of the capture groups based on the test text and the match pattern"});
			m_grid_grps.DataError += (s,a) => Debug.Assert(false, $"Data error in groups grid: {a.Exception.MessageFull()}");
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Access to the test text field</summary>
		public override string TestText
		{
			get { return m_edit_test.Text; }
			set { m_edit_test.Text = value; }
		}

		/// <summary>Set focus to the primary input field</summary>
		public override void FocusInput()
		{
			m_edit_match.Focus();
		}

		/// <summary>Update UI elements based on the current pattern state</summary>
		protected override void UpdateUIInternal()
		{
			if (Pattern == null) return;
			m_edit_match.Text           = Pattern.Expr;
			m_radio_substring.Checked   = Pattern.PatnType == EPattern.Substring;
			m_radio_wildcard.Checked    = Pattern.PatnType == EPattern.Wildcard;
			m_radio_regex.Checked       = Pattern.PatnType == EPattern.RegularExpression;
			m_check_ignore_case.Checked = Pattern.IgnoreCase;
			m_check_whole_line.Checked  = Pattern.WholeLine;
			m_check_invert.Checked      = Pattern.Invert;

			m_btn_add.ToolTip(m_tt, IsNew ? "Add this new pattern" : "Save changes to this pattern");
			m_btn_add.ImageKey = IsNew ? "add" : "save";
			m_btn_add.Enabled = CommitEnabled;

			// Highlight the expression background to show valid regex
			var ex = Pattern.ValidateExpr();
			var tt = ex == null
				? "The pattern used to match rows in the log file."
				: "Invalid match pattern - " + ex.Message;
			m_lbl_match.ToolTip(m_tt, tt);
			m_edit_match.ToolTip(m_tt, tt);
			m_edit_match.BackColor = Pattern.IsValid ? FieldValid : FieldInvalid;

			// Update the highlighting of the test text if valid
			if (Pattern.IsValid)
			{
				var lines = m_edit_test.Lines;

				// Preserve the current caret position
				using (m_edit_test.SelectionScope())
				{
					// Reset the highlighting
					m_edit_test.SelectAll();
					m_edit_test.SelectionBackColor = Color.White;

					// Apply the highlighting to each line in the test text
					for (int i = 0, iend = lines.Length; i != iend; ++i)
					{
						int j = 0; foreach (var r in Pattern.Match(lines[i]))
						{
							int starti = m_edit_test.GetFirstCharIndexFromLine(i);

							// Highlight the capture groups in the test text
							m_edit_test.Select(starti + r.Begi, r.Sizei);
							m_edit_test.SelectionBackColor = BkColors[j++ % BkColors.Length];
						}
					}
				}

				// Updates the caps data based on the line that the cursor's in
				var line_index = m_edit_test.GetLineFromCharIndex(m_edit_test.SelectionStart);
				var line = line_index >= 0 && line_index < lines.Length ? lines[line_index] : string.Empty;
				var groups = new Dictionary<string, string>();
				foreach (var name in Pattern.CaptureGroupNames) groups[name] = string.Empty;
				foreach (var cap in Pattern.CaptureGroups(line)) groups[cap.Key] = cap.Value;
				m_grid_grps.DataSource = groups.ToList();
			}
		}

		/// <summary>Return the Form for displaying the regex quick help (lazy loaded)</summary>
		public static HelpUI CreateRegexHelpUI(Form parent)
		{
			Debug.Assert(parent != null);
			var ui = new HelpUI(parent, HelpUI.EContent.Html, "Regular Expressions Quick Reference", string.Empty, new Point(1,1) ,new Size(640,480) ,ToolForm.EPin.TopRight);
			ui.Content = Resources.regex_quick_ref;
			ui.RenderContent();
			return ui;
		}

		/// <summary>
		/// Show the pattern UI in a dialog.
		/// If 'patn' is not null, then it is edited in the UI, otherwise a new pattern is created.
		/// Returns the created/edited pattern</summary>
		public static Pattern ShowDialog(IWin32Window owner = null, Pattern patn = null)
		{
			var p = new PatternUI();
			using (var f = p.FormWrap<Form>(title:"Edit Pattern", start_pos:FormStartPosition.CenterParent))
			{
				patn = patn ?? new Pattern();
				p.EditPattern(patn);
				p.Commit += (s,a) => f.Close();
				f.ShowDialog(owner);
				return p.Pattern;
			}
		}

		/// <summary>Lazy create RegexHelpUI</summary>
		private HelpUI RegexHelpUI
		{
			get { return m_dlg_help ?? (m_dlg_help = CreateRegexHelpUI(ParentForm)); }
		}
		private HelpUI m_dlg_help;
		
		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			this.m_check_invert = new System.Windows.Forms.CheckBox();
			this.m_check_ignore_case = new System.Windows.Forms.CheckBox();
			this.m_btn_add = new System.Windows.Forms.Button();
			this.m_il = new System.Windows.Forms.ImageList(this.components);
			this.m_lbl_match = new System.Windows.Forms.Label();
			this.m_edit_match = new System.Windows.Forms.TextBox();
			this.m_edit_test = new Rylogic.Gui.WinForms.RichTextBox();
			this.m_btn_regex_help = new System.Windows.Forms.Button();
			this.m_radio_substring = new System.Windows.Forms.RadioButton();
			this.m_radio_wildcard = new System.Windows.Forms.RadioButton();
			this.m_radio_regex = new System.Windows.Forms.RadioButton();
			this.m_panel_patntype = new System.Windows.Forms.Panel();
			this.m_lbl_match_type = new System.Windows.Forms.Label();
			this.m_split = new System.Windows.Forms.SplitContainer();
			this.m_grid_grps = new Rylogic.Gui.WinForms.DataGridView();
			this.m_lbl_groups = new System.Windows.Forms.Label();
			this.m_check_whole_line = new System.Windows.Forms.CheckBox();
			this.m_panel_flags = new System.Windows.Forms.Panel();
			this.m_panel_patntype.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split)).BeginInit();
			this.m_split.Panel1.SuspendLayout();
			this.m_split.Panel2.SuspendLayout();
			this.m_split.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_grps)).BeginInit();
			this.m_panel_flags.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_check_invert
			// 
			this.m_check_invert.AutoSize = true;
			this.m_check_invert.Location = new System.Drawing.Point(187, 3);
			this.m_check_invert.Name = "m_check_invert";
			this.m_check_invert.Size = new System.Drawing.Size(86, 17);
			this.m_check_invert.TabIndex = 3;
			this.m_check_invert.Text = "&Invert Match";
			this.m_check_invert.UseVisualStyleBackColor = true;
			// 
			// m_check_ignore_case
			// 
			this.m_check_ignore_case.AutoSize = true;
			this.m_check_ignore_case.Location = new System.Drawing.Point(3, 3);
			this.m_check_ignore_case.Name = "m_check_ignore_case";
			this.m_check_ignore_case.Size = new System.Drawing.Size(83, 17);
			this.m_check_ignore_case.TabIndex = 2;
			this.m_check_ignore_case.Text = "Ignore &Case";
			this.m_check_ignore_case.UseVisualStyleBackColor = true;
			// 
			// m_btn_add
			// 
			this.m_btn_add.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_add.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.m_btn_add.ImageKey = "add";
			this.m_btn_add.ImageList = this.m_il;
			this.m_btn_add.Location = new System.Drawing.Point(335, 3);
			this.m_btn_add.Name = "m_btn_add";
			this.m_btn_add.Size = new System.Drawing.Size(46, 46);
			this.m_btn_add.TabIndex = 4;
			this.m_btn_add.UseVisualStyleBackColor = true;
			// 
			// m_lbl_match
			// 
			this.m_lbl_match.AutoSize = true;
			this.m_lbl_match.Location = new System.Drawing.Point(12, 32);
			this.m_lbl_match.Name = "m_lbl_match";
			this.m_lbl_match.Size = new System.Drawing.Size(44, 13);
			this.m_lbl_match.TabIndex = 14;
			this.m_lbl_match.Text = "Pattern:";
			this.m_lbl_match.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_edit_match
			// 
			this.m_edit_match.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_match.Location = new System.Drawing.Point(62, 29);
			this.m_edit_match.Name = "m_edit_match";
			this.m_edit_match.Size = new System.Drawing.Size(246, 20);
			this.m_edit_match.TabIndex = 0;
			// 
			// m_edit_test
			// 
			this.m_edit_test.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_edit_test.CaretLocation = new System.Drawing.Point(0, 0);
			this.m_edit_test.CurrentLineIndex = 0;
			this.m_edit_test.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_edit_test.FirstVisibleLineIndex = 0;
			this.m_edit_test.LineCount = 1;
			this.m_edit_test.Location = new System.Drawing.Point(0, 0);
			this.m_edit_test.Name = "m_edit_test";
			this.m_edit_test.Size = new System.Drawing.Size(224, 110);
			this.m_edit_test.TabIndex = 0;
			this.m_edit_test.Text = "Enter text here to test your pattern";
			// 
			// m_btn_regex_help
			// 
			this.m_btn_regex_help.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_regex_help.Location = new System.Drawing.Point(311, 29);
			this.m_btn_regex_help.Name = "m_btn_regex_help";
			this.m_btn_regex_help.Size = new System.Drawing.Size(22, 21);
			this.m_btn_regex_help.TabIndex = 6;
			this.m_btn_regex_help.Text = "?";
			this.m_btn_regex_help.UseVisualStyleBackColor = true;
			// 
			// m_radio_substring
			// 
			this.m_radio_substring.AutoSize = true;
			this.m_radio_substring.Location = new System.Drawing.Point(3, 3);
			this.m_radio_substring.Name = "m_radio_substring";
			this.m_radio_substring.Size = new System.Drawing.Size(69, 17);
			this.m_radio_substring.TabIndex = 0;
			this.m_radio_substring.TabStop = true;
			this.m_radio_substring.Text = "&Substring";
			this.m_radio_substring.TextAlign = System.Drawing.ContentAlignment.TopRight;
			this.m_radio_substring.UseVisualStyleBackColor = true;
			// 
			// m_radio_wildcard
			// 
			this.m_radio_wildcard.AutoSize = true;
			this.m_radio_wildcard.Location = new System.Drawing.Point(78, 3);
			this.m_radio_wildcard.Name = "m_radio_wildcard";
			this.m_radio_wildcard.Size = new System.Drawing.Size(67, 17);
			this.m_radio_wildcard.TabIndex = 1;
			this.m_radio_wildcard.TabStop = true;
			this.m_radio_wildcard.Text = "Wil&dcard";
			this.m_radio_wildcard.UseVisualStyleBackColor = true;
			// 
			// m_radio_regex
			// 
			this.m_radio_regex.AutoSize = true;
			this.m_radio_regex.Location = new System.Drawing.Point(152, 3);
			this.m_radio_regex.Margin = new System.Windows.Forms.Padding(0);
			this.m_radio_regex.Name = "m_radio_regex";
			this.m_radio_regex.Size = new System.Drawing.Size(116, 17);
			this.m_radio_regex.TabIndex = 2;
			this.m_radio_regex.TabStop = true;
			this.m_radio_regex.Text = "&Regular Expression";
			this.m_radio_regex.UseVisualStyleBackColor = true;
			// 
			// m_panel_patntype
			// 
			this.m_panel_patntype.Controls.Add(this.m_radio_substring);
			this.m_panel_patntype.Controls.Add(this.m_radio_wildcard);
			this.m_panel_patntype.Controls.Add(this.m_radio_regex);
			this.m_panel_patntype.Location = new System.Drawing.Point(62, 3);
			this.m_panel_patntype.Name = "m_panel_patntype";
			this.m_panel_patntype.Size = new System.Drawing.Size(271, 23);
			this.m_panel_patntype.TabIndex = 5;
			// 
			// m_lbl_match_type
			// 
			this.m_lbl_match_type.AutoSize = true;
			this.m_lbl_match_type.Location = new System.Drawing.Point(22, 8);
			this.m_lbl_match_type.Name = "m_lbl_match_type";
			this.m_lbl_match_type.Size = new System.Drawing.Size(34, 13);
			this.m_lbl_match_type.TabIndex = 15;
			this.m_lbl_match_type.Text = "Type:";
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
			this.m_split.Size = new System.Drawing.Size(376, 112);
			this.m_split.SplitterDistance = 226;
			this.m_split.TabIndex = 1;
			// 
			// m_grid_grps
			// 
			this.m_grid_grps.AllowUserToAddRows = false;
			this.m_grid_grps.AllowUserToDeleteRows = false;
			this.m_grid_grps.AllowUserToResizeRows = false;
			this.m_grid_grps.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_grps.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
			this.m_grid_grps.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_grid_grps.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.m_grid_grps.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.m_grid_grps.ColumnHeadersHeight = 20;
			this.m_grid_grps.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.m_grid_grps.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_grps.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.m_grid_grps.Location = new System.Drawing.Point(0, 0);
			this.m_grid_grps.MultiSelect = false;
			this.m_grid_grps.Name = "m_grid_grps";
			this.m_grid_grps.ReadOnly = true;
			this.m_grid_grps.RowHeadersVisible = false;
			this.m_grid_grps.RowTemplate.Height = 18;
			this.m_grid_grps.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_grps.Size = new System.Drawing.Size(144, 110);
			this.m_grid_grps.TabIndex = 0;
			this.m_grid_grps.TabStop = false;
			// 
			// m_lbl_groups
			// 
			this.m_lbl_groups.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_groups.AutoSize = true;
			this.m_lbl_groups.Location = new System.Drawing.Point(298, 58);
			this.m_lbl_groups.Name = "m_lbl_groups";
			this.m_lbl_groups.Size = new System.Drawing.Size(81, 13);
			this.m_lbl_groups.TabIndex = 17;
			this.m_lbl_groups.Text = "Capture Groups";
			this.m_lbl_groups.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_check_whole_line
			// 
			this.m_check_whole_line.AutoSize = true;
			this.m_check_whole_line.Location = new System.Drawing.Point(95, 3);
			this.m_check_whole_line.Name = "m_check_whole_line";
			this.m_check_whole_line.Size = new System.Drawing.Size(80, 17);
			this.m_check_whole_line.TabIndex = 18;
			this.m_check_whole_line.Text = "&Whole Line";
			this.m_check_whole_line.UseVisualStyleBackColor = true;
			// 
			// m_panel_flags
			// 
			this.m_panel_flags.Controls.Add(this.m_check_ignore_case);
			this.m_panel_flags.Controls.Add(this.m_check_whole_line);
			this.m_panel_flags.Controls.Add(this.m_check_invert);
			this.m_panel_flags.Location = new System.Drawing.Point(15, 52);
			this.m_panel_flags.Name = "m_panel_flags";
			this.m_panel_flags.Size = new System.Drawing.Size(277, 21);
			this.m_panel_flags.TabIndex = 19;
			// 
			// PatternUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_panel_flags);
			this.Controls.Add(this.m_lbl_groups);
			this.Controls.Add(this.m_split);
			this.Controls.Add(this.m_lbl_match_type);
			this.Controls.Add(this.m_btn_regex_help);
			this.Controls.Add(this.m_lbl_match);
			this.Controls.Add(this.m_edit_match);
			this.Controls.Add(this.m_btn_add);
			this.Controls.Add(this.m_panel_patntype);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.MinimumSize = new System.Drawing.Size(354, 117);
			this.Name = "PatternUI";
			this.Size = new System.Drawing.Size(382, 190);
			this.m_panel_patntype.ResumeLayout(false);
			this.m_panel_patntype.PerformLayout();
			this.m_split.Panel1.ResumeLayout(false);
			this.m_split.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split)).EndInit();
			this.m_split.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_grps)).EndInit();
			this.m_panel_flags.ResumeLayout(false);
			this.m_panel_flags.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}

	/// <summary>A pattern editor control</summary>
	public interface IPatternUI
	{
		/// <summary>Return the original before any edits were made</summary>
		IPattern Original { get; }

		/// <summary>Return the pattern currently being edited</summary>
		IPattern Pattern { get; }

		/// <summary>True if the pattern currently contained is a new instance, vs editing an existing pattern</summary>
		bool IsNew { get; }

		/// <summary>Set a new pattern for the UI</summary>
		void NewPattern(IPattern pat);

		/// <summary>Select a pattern into the UI for editing</summary>
		void EditPattern(IPattern pat);

		/// <summary>True if the contained pattern is different to the original</summary>
		bool HasUnsavedChanges { get; }

		/// <summary>True if the contained pattern is valid and therefore can be saved</summary>
		bool CommitEnabled { get; }

		/// <summary>Raised when the 'Commit' button is hit and the pattern field contains a valid pattern</summary>
		event EventHandler Commit;

		/// <summary>Access to the test text field</summary>
		string TestText { get; set; }

		/// <summary>Set focus to the primary input field</summary>
		void FocusInput();
	}

	/// <summary>Workaround for the retarded VS designer</summary>
	public class PatternUIImpl :PatternUIBase<Pattern>
	{
		/// <summary>Access to the test text field</summary>
		public override string TestText { get; set; }

		/// <summary>Set focus to the primary input field</summary>
		public override void FocusInput() {}

		/// <summary>Update the UI elements based on the current pattern</summary>
		protected override void UpdateUIInternal() {}
	}

	/// <summary>Base class for editing objects that implement IPattern</summary>
	public abstract class PatternUIBase<TPattern> :UserControl ,IPatternUI where TPattern:class, IPattern, new()
	{
		protected readonly ToolTip m_tt;

		protected PatternUIBase()
		{
			m_tt = new ToolTip();
			m_original = null;
			m_pattern = new TPattern();

			VisibleChanged += (s,a)=>
				{
					if (!Visible) return;
					UpdateUI();
				};

			Disposed += (s,a) =>
				{
					m_tt.Dispose();
				};
		}

		/// <summary>The pattern being edited by this UI</summary>
		[Browsable(false)] public TPattern Pattern { get { return m_pattern; } }
		IPattern IPatternUI.Pattern { get { return Pattern; } }
		protected TPattern m_pattern;

		/// <summary>The original pattern provided to the UI for editing</summary>
		[Browsable(false)] public TPattern Original { get { return m_original; } }
		IPattern IPatternUI.Original { get { return Original; } }
		protected TPattern m_original;

		/// <summary>True if the pattern currently contained is a new instance, vs editing an existing pattern</summary>
		public bool IsNew { get { return ReferenceEquals(m_original,null); } }

		/// <summary>Set a new pattern for the UI</summary>
		public void NewPattern(IPattern pattern)
		{
			m_original = null;
			m_pattern = (TPattern)pattern;
			UpdateUI();
		}

		/// <summary>Select a pattern into the UI for editing</summary>
		public void EditPattern(IPattern pattern)
		{
			m_original = (TPattern)pattern;
			m_pattern  = (TPattern)m_original.Clone();
			UpdateUI();
		}

		/// <summary>True when user activity has changed something in the UI</summary>
		public bool Touched { get; set; }

		/// <summary>True if the pattern contains unsaved changes</summary>
		public bool HasUnsavedChanges
		{
			get
			{
				return (IsNew && m_pattern.Expr.Length != 0 && Touched)
					|| (!IsNew && !Equals(m_original, m_pattern));
			}
		}

		/// <summary>True if the contained pattern is valid and therefore can be saved</summary>
		public bool CommitEnabled
		{
			get { return m_pattern != null && m_pattern.IsValid; }
		}

		/// <summary>Raised when the 'Commit' button is hit and the pattern field contains a valid pattern</summary>
		public event EventHandler Commit;
		protected void RaiseCommitEvent()
		{
			Commit?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Access to the test text field</summary>
		public abstract string TestText { get; set ;}

		/// <summary>Set focus to the primary input field</summary>
		public abstract void FocusInput();

		/// <summary>Prevents reentrant calls to UpdateUI. Yes this is the best way to do it /cry</summary>
		protected bool m_in_update_ui;
		protected void UpdateUI()
		{
			if (m_in_update_ui) return;
			try
			{
				m_in_update_ui = true;
				SuspendLayout();

				UpdateUIInternal();
			}
			finally
			{
				m_in_update_ui = false;
				ResumeLayout();
			}
		}

		/// <summary>Update the UI elements based on the current pattern</summary>
		protected abstract void UpdateUIInternal();
	}
}
