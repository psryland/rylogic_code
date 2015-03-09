using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using RichTextBox = pr.gui.RichTextBox;

namespace RyLogViewer
{
	public class TransformUI :TransformUIImpl ,IPatternUI
	{
		private static class ColumnNames
		{
			public const string Tag   = "Tag";
			public const string Value = "Value";
			public const string Type  = "Type";
			public const string Cfg  = "Cfg";
		}

		private readonly Image NullImage = new Bitmap(1,1);

		private List<KeyValuePair<string,string>> m_caps;
		private HelpUI           m_dlg_help;
		private TextBox          m_edit_match;
		private TextBox          m_edit_replace;
		private DataGridView     m_grid_subs;
		private Label            m_lbl_match;
		private Label            m_lbl_replace;
		private CheckBox         m_check_ignore_case;
		private Button           m_btn_add;
		private Button           m_btn_regex_help;
		private ImageList        m_image_list;
		private SplitContainer   m_split_test;
		private RichTextBox      m_edit_test;
		private SplitContainer   m_split_subs;
		private RadioButton      m_radio_regex;
		private RadioButton      m_radio_wildcard;
		private RadioButton      m_radio_substring;
		private TextBox          m_edit_eqv_regex;
		private Label            m_lbl_matchtype;
		private Button           m_btn_show_eqv_regex;
		private Label            m_lbl_eqv_regex;
		private Panel            m_panel_btns;
		private Panel            m_panel_match_type;
		private Panel            m_panel_match;
		private TableLayoutPanel m_table;
		private Panel            m_panel_replace;
		private Panel            m_panel_eqv_regex;
		private RichTextBox      m_edit_result;

		public TransformUI()
		{
			InitializeComponent();
			m_caps = new List<KeyValuePair<string, string>>();

			// Add/Update
			m_btn_add.ToolTip(m_tt, "Adds a new transform, or updates an existing transform");
			m_btn_add.Click += (s,a)=>
				{
					if (!CommitEnabled) return;
					RaiseCommitEvent();
				};

			// Match help
			m_btn_regex_help.ToolTip(m_tt, "Displays a quick help guide for the Match field");
			m_btn_regex_help.Click += (s,a)=>
				{
					MatchFieldHelpUI.Display();
				};

			// Toggle equivalent regex
			m_btn_show_eqv_regex.ToolTip(m_tt, "Toggle the visibility of the equivalent regular expression field");
			m_btn_show_eqv_regex.Click += (s,a)=>
				{
					m_panel_eqv_regex.Visible = !m_panel_eqv_regex.Visible;
					UpdateUI();
				};

			// Match (tooltip set in UpdateUI())
			m_edit_match.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					Pattern.Expr = m_edit_match.Text;
					Touched = true;
					UpdateUI();
				};

			// Match - Substring
			m_radio_substring.ToolTip(m_tt, "Match any occurrence of the pattern as a substring");
			m_radio_substring.Click += (s,a)=>
				{
					if (m_radio_substring.Checked) Pattern.PatnType = EPattern.Substring;
					UpdateUI();
				};

			// Match - Wildcard
			m_radio_wildcard.ToolTip(m_tt, "Match using wildcards, where '*' matches any number of characters and '?' matches any single character");
			m_radio_wildcard.Click += (s,a)=>
				{
					if (m_radio_wildcard.Checked) Pattern.PatnType = EPattern.Wildcard;
					UpdateUI();
				};

			// Match - Regex
			m_radio_regex.ToolTip(m_tt, "Match using a regular expression");
			m_radio_regex.Click += (s,a)=>
				{
					if (m_radio_regex.Checked) Pattern.PatnType = EPattern.RegularExpression;
					UpdateUI();
				};

			// Match - Ignore case
			m_check_ignore_case.ToolTip(m_tt, "Enable to have the template ignore case when matching");
			m_check_ignore_case.Click += (s,a)=>
				{
					Pattern.IgnoreCase = m_check_ignore_case.Checked;
					Touched = true;
					UpdateUI();
				};

			// Match - compiled regex
			m_edit_eqv_regex.ToolTip(m_tt, "The regular expression that the Match field is converted into.\r\nVisible here for reference and diagnostic purposes");

			// Replace (tooltip set in UpdateUI())
			m_edit_replace.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					Pattern.Replace = m_edit_replace.Text;
					Touched = true;
					UpdateUI();
				};

			var subs = new BindingSource{DataSource = Transform.Substitutors.Select(x => new TransSubWrapper(x)), AllowNew = false};

			// Substitutions
			m_grid_subs.VirtualMode = true;
			m_grid_subs.AutoGenerateColumns = false;
			m_grid_subs.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Tag   ,HeaderText = "Tag"       ,FillWeight = 11.5f ,ReadOnly = true ,ToolTipText = "The identifier of the capture group"});
			m_grid_subs.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Value ,HeaderText = "Value"     ,FillWeight = 37.6f ,ReadOnly = true ,ToolTipText = "The value of the capture group when applied to the current line of text in the text area"});
			m_grid_subs.Columns.Add(new DataGridViewComboBoxColumn{Name = ColumnNames.Type  ,HeaderText = "Transform" ,FillWeight = 25.8f ,DataSource = subs ,FlatStyle=FlatStyle.Flat, ToolTipText = "The type of text transform to apply to this capture group"});
			m_grid_subs.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Cfg   ,HeaderText = ""          ,FillWeight =  5.0f ,ImageLayout = DataGridViewImageCellLayout.Zoom, ToolTipText = "Displays a pencil icon if the text transform can be configured.\r\nClicking will display the configuration dialog"});
			m_grid_subs.DataError += (s,a) => {};//Debug.Assert(false, "Data error in subs grid: {0}".Fmt(a.Exception.MessageFull()));
			m_grid_subs.CurrentCellDirtyStateChanged += (s,a) => m_grid_subs.CommitEdit(DataGridViewDataErrorContexts.Commit);
			m_grid_subs.CellValueNeeded  += CellValueNeeded;
			m_grid_subs.CellValuePushed  += CellValuePushed;
			m_grid_subs.CellPainting     += CellPainting;
			m_grid_subs.CellClick        += CellClick;
			m_grid_subs.CellDoubleClick  += CellDoubleClick;

			// Test text
			m_edit_test.ToolTip(m_tt, "Enter text here on which to test your pattern.");
			m_edit_test.Text = PatternUI.DefaultTestText;
			m_edit_test.TextChanged += (s,a)=>
				{
					UpdateUI();
				};
			int last_selected_line = -1;
			m_edit_test.SelectionChanged += (s,a) =>
				{
					if (MouseButtons != MouseButtons.None || ModifierKeys != Keys.None) return;
					var idx = m_edit_test.GetLineFromCharIndex(m_edit_test.SelectionStart);
					if (last_selected_line != idx) last_selected_line = idx; else return;
					UpdateUI();
				};

			// Result text
			m_edit_result.ToolTip(m_tt,
				"Shows the result of applying the transform to the text in the test area above\r\n" +
				"Transforms only replace the portion of the input text that they match.\r\n" +
				"If you are trying to replace the whole line, your pattern needs to match the whole line");
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

		/// <summary>Get the cell value from the transform</summary>
		private void CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			e.Value = string.Empty;
			var grid = (DataGridView)sender;
			if (e.RowIndex < 0 || e.RowIndex >= m_caps.Count) return;
			var col = grid.Columns[e.ColumnIndex];
			var cap = m_caps[e.RowIndex];

			ITransformSubstitution sub = Pattern.Subs.TryGetValue(cap.Key, out sub) ? sub : null;
			switch (col.Name)
			{
			default:
				Debug.Assert(false, "Unknown column name");
				break;
			case ColumnNames.Tag:
				e.Value = cap.Key;
				break;
			case ColumnNames.Value:
				e.Value = cap.Value;
				break;
			case ColumnNames.Type:
				if (sub != null)
				{
					var subs = ((BindingSource)((DataGridViewComboBoxColumn)col).DataSource).List.Cast<TransSubWrapper>();
					e.Value = subs.FirstOrDefault(x => x.Sub.Guid == sub.Guid);
					grid[e.ColumnIndex,e.RowIndex].ToolTipText = sub.ConfigSummary;
				}
				else
				{
					e.Value = null;
					grid[e.ColumnIndex,e.RowIndex].ToolTipText = null;
				}
				break;
			case ColumnNames.Cfg:
				e.Value = sub != null && sub.Configurable ? Resources.pencil : NullImage;
				break;
			}
		}

		/// <summary>Handle cell values changed</summary>
		private void CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex < 0 || e.RowIndex >= m_caps.Count) return;
			var col = grid.Columns[e.ColumnIndex];
			var cap = m_caps[e.RowIndex];

			ITransformSubstitution sub_cur = Pattern.Subs.TryGetValue(cap.Key, out sub_cur) ? sub_cur : null;
			switch (col.Name)
			{
			case ColumnNames.Type:
				{
					var cur = (TransSubWrapper)((BindingSource)((DataGridViewComboBoxColumn)col).DataSource).Current;
					var sub_new = cur != null ? cur.Sub : null;
					if (sub_new != null && (sub_cur == null || !sub_new.Guid.Equals(sub_cur.Guid)))
					{
						Pattern.Subs[cap.Key] = (ITransformSubstitution)Activator.CreateInstance(sub_new.GetType());
						grid.Invalidate();
					}
				}
				break;
			}
			UpdateUI();
		}

		/// <summary>Paint the backgrounds to show capture groups</summary>
		private void CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			e.Handled = false;
			var grid = (DataGridView)sender;
			if (e.RowIndex < 0 || e.RowIndex >= Pattern.Subs.Count) return;
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			case ColumnNames.Tag:
				e.CellStyle.BackColor = Constants.BkColors[e.RowIndex % Constants.BkColors.Length];
				e.CellStyle.SelectionBackColor = Gfx.Blend(e.CellStyle.BackColor, Color.Black, 0.2f);
				break;
			}
		}

		/// <summary>Handle clicks on cells</summary>
		private void CellClick(object sender, DataGridViewCellEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex < 0 || e.RowIndex >= Pattern.Subs.Count) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
			var cap = m_caps[e.RowIndex];

			ITransformSubstitution sub = Pattern.Subs.TryGetValue(cap.Key, out sub) ? sub : null;
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: return;
			case ColumnNames.Cfg:
				if (sub != null) sub.ShowConfigUI(ParentForm);
				UpdateUI();
				break;
			}
		}

		/// <summary>Handle double clicks on cells</summary>
		private void CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex < 0 || e.RowIndex >= Pattern.Subs.Count) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
			var cap = m_caps[e.RowIndex];

			ITransformSubstitution sub = Pattern.Subs.TryGetValue(cap.Key, out sub) ? sub : null;
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: return;
			case ColumnNames.Tag:
				if (sub != null)
					m_edit_replace.SelectedText = "{"+cap.Key+"}";
				break;
			}
		}

		/// <summary>Prevents re-entrant calls to UpdateUI. Yes this is the best way to do it /cry</summary>
		protected override void UpdateUIInternal()
		{
			m_edit_match.Text           = Pattern.Expr;
			m_edit_eqv_regex.Text       = Pattern.RegexString;
			m_edit_replace.Text         = Pattern.Replace;
			m_radio_substring.Checked   = Pattern.PatnType == EPattern.Substring;
			m_radio_wildcard.Checked    = Pattern.PatnType == EPattern.Wildcard;
			m_radio_regex.Checked       = Pattern.PatnType == EPattern.RegularExpression;
			m_check_ignore_case.Checked = Pattern.IgnoreCase;

			m_btn_add.ToolTip(m_tt, IsNew ? "Add this new pattern" : "Save changes to this pattern");
			m_btn_add.ImageIndex = (int)(IsNew ? EBtnImageIdx.AddNew : EBtnImageIdx.Save);
			m_btn_add.Enabled = CommitEnabled;

			// Show/Hide the eqv regex
			m_table.Height = m_table.PreferredSize.Height;
			m_split_subs.Top = m_table.Bottom;
			m_split_subs.Height = m_split_subs.Parent.Height - m_split_subs.Top - 3;

			// Highlight the match/replace fields if in error
			var ex0 = Pattern.ValidateExpr();
			string tt0 = ex0 == null
				? "The pattern used to identify rows to transform.\r\n" +
					(Pattern.PatnType == EPattern.RegularExpression
						? "Capture groups are defined using the usual regular expression syntax for capture groups e.g. (.*),(<tag>?.*),etc\r\n"
						: "Create capture groups using '{' and '}', e.g. {one},{2},{tag},etc\r\n")
				: "Invalid match pattern - " + ex0.Message;
			var ex1 = Pattern.ValidateReplace();
			string tt1 = ex1 == null
				? "The template for the transformed result.\r\nUse the capture groups created in the Match field"
				: "Invalid replace template - " + ex1.Message;
			m_lbl_match   .ToolTip(m_tt, tt0);
			m_edit_match  .ToolTip(m_tt, tt0);
			m_lbl_replace .ToolTip(m_tt, tt1);
			m_edit_replace.ToolTip(m_tt, tt1);
			m_edit_match  .BackColor = Misc.FieldBkColor(ex0 == null);
			m_edit_replace.BackColor = Misc.FieldBkColor(ex1 == null);

			// Apply the transform to the test text if not in error
			if (ex0 == null && ex1 == null)
			{
				string[] lines = m_edit_test.Lines;

				// Preserve the current carot position
				using (m_edit_test.SelectionScope())
				{
					// Reset the highlighting
					m_edit_test.SelectAll();
					m_edit_test.SelectionBackColor = Color.White;

					// Apply the transform to each line in the test text
					m_edit_result.Clear();
					for (int i = 0, iend = lines.Length; i != iend; ++i)
					{
						m_edit_result.Select(m_edit_result.TextLength, 0);
						if (i != 0) m_edit_result.SelectedText = Environment.NewLine;
						if (!Pattern.IsMatch(lines[i]))
						{
							m_edit_result.SelectedText = lines[i];
						}
						else
						{
							int starti = m_edit_test.GetFirstCharIndexFromLine(i);
							int startj = m_edit_result.TextLength;

							List<Transform.Capture> src_caps, dst_caps;
							string result = Pattern.Txfm(lines[i], out src_caps, out dst_caps);
							m_edit_result.SelectedText = result;

							// Highlight the capture groups in the test text and the result
							int j = 0; foreach (var s in src_caps)
							{
								m_edit_test.Select(starti + s.Span.Begini, s.Span.Sizei);
								m_edit_test.SelectionBackColor = Constants.BkColors[j++ % Constants.BkColors.Length];
							}
							j = 0; foreach (var s in dst_caps)
							{
								m_edit_result.Select(startj + s.Span.Begini, s.Span.Sizei);
								m_edit_result.SelectionBackColor = Constants.BkColors[j++ % Constants.BkColors.Length];
							}
						}
					}
				}

				// Updates the caps data based on the line that the cursor's in
				var line_index = m_edit_test.GetLineFromCharIndex(m_edit_test.SelectionStart);
				var line = line_index >= 0 && line_index < lines.Length ? lines[line_index] : string.Empty;
				var groups = new Dictionary<string, string>();
				foreach (var name in Pattern.CaptureGroupNames) groups[name] = string.Empty;
				foreach (var cap in Pattern.CaptureGroups(line)) groups[cap.Key] = cap.Value;
				m_caps = groups.ToList();
			}

			m_grid_subs.RowCount = m_caps.Count;
			m_grid_subs.Refresh();
		}

		/// <summary>Return the Form for displaying the quick help for the match field syntax (lazy loaded)</summary>
		private HelpUI MatchFieldHelpUI
		{
			get
			{
				Debug.Assert(ParentForm != null);
				return m_dlg_help ?? (m_dlg_help = HelpUI.From(ParentForm, HelpUI.EContent.Html, "Transform Help", Resources.transform_quick_ref, new Point(1,1), new Size(640,480), ToolForm.EPin.TopRight));
			}
		}

		#region Component Designer generated code

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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TransformUI));
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			this.m_btn_regex_help = new System.Windows.Forms.Button();
			this.m_btn_add = new System.Windows.Forms.Button();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_edit_test = new RichTextBox();
			this.m_edit_result = new RichTextBox();
			this.m_lbl_replace = new System.Windows.Forms.Label();
			this.m_edit_replace = new System.Windows.Forms.TextBox();
			this.m_check_ignore_case = new System.Windows.Forms.CheckBox();
			this.m_split_test = new System.Windows.Forms.SplitContainer();
			this.m_lbl_match = new System.Windows.Forms.Label();
			this.m_edit_match = new System.Windows.Forms.TextBox();
			this.m_grid_subs = new DataGridView();
			this.m_split_subs = new System.Windows.Forms.SplitContainer();
			this.m_radio_regex = new System.Windows.Forms.RadioButton();
			this.m_radio_wildcard = new System.Windows.Forms.RadioButton();
			this.m_radio_substring = new System.Windows.Forms.RadioButton();
			this.m_edit_eqv_regex = new System.Windows.Forms.TextBox();
			this.m_lbl_matchtype = new System.Windows.Forms.Label();
			this.m_btn_show_eqv_regex = new System.Windows.Forms.Button();
			this.m_lbl_eqv_regex = new System.Windows.Forms.Label();
			this.m_panel_btns = new System.Windows.Forms.Panel();
			this.m_panel_match_type = new System.Windows.Forms.Panel();
			this.m_panel_match = new System.Windows.Forms.Panel();
			this.m_table = new System.Windows.Forms.TableLayoutPanel();
			this.m_panel_replace = new System.Windows.Forms.Panel();
			this.m_panel_eqv_regex = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.m_split_test)).BeginInit();
			this.m_split_test.Panel1.SuspendLayout();
			this.m_split_test.Panel2.SuspendLayout();
			this.m_split_test.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_subs)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_split_subs)).BeginInit();
			this.m_split_subs.Panel1.SuspendLayout();
			this.m_split_subs.Panel2.SuspendLayout();
			this.m_split_subs.SuspendLayout();
			this.m_panel_btns.SuspendLayout();
			this.m_panel_match_type.SuspendLayout();
			this.m_panel_match.SuspendLayout();
			this.m_table.SuspendLayout();
			this.m_panel_replace.SuspendLayout();
			this.m_panel_eqv_regex.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btn_regex_help
			//
			this.m_btn_regex_help.Location = new System.Drawing.Point(3, 3);
			this.m_btn_regex_help.Name = "m_btn_regex_help";
			this.m_btn_regex_help.Size = new System.Drawing.Size(22, 21);
			this.m_btn_regex_help.TabIndex = 3;
			this.m_btn_regex_help.Text = "?";
			this.m_btn_regex_help.UseVisualStyleBackColor = true;
			//
			// m_btn_add
			//
			this.m_btn_add.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.m_btn_add.ImageIndex = 0;
			this.m_btn_add.ImageList = this.m_image_list;
			this.m_btn_add.Location = new System.Drawing.Point(26, 3);
			this.m_btn_add.Name = "m_btn_add";
			this.m_btn_add.Size = new System.Drawing.Size(46, 46);
			this.m_btn_add.TabIndex = 5;
			this.m_btn_add.UseVisualStyleBackColor = true;
			//
			// m_image_list
			//
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "edit_add.png");
			this.m_image_list.Images.SetKeyName(1, "edit_save.png");
			//
			// m_edit_test
			//
			this.m_edit_test.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_edit_test.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_edit_test.Location = new System.Drawing.Point(0, 0);
			this.m_edit_test.Name = "m_edit_test";
			this.m_edit_test.Size = new System.Drawing.Size(202, 55);
			this.m_edit_test.TabIndex = 2;
			this.m_edit_test.Text = "Enter text here to test your pattern";
			//
			// m_edit_result
			//
			this.m_edit_result.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_edit_result.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_edit_result.Location = new System.Drawing.Point(0, 0);
			this.m_edit_result.Name = "m_edit_result";
			this.m_edit_result.ReadOnly = true;
			this.m_edit_result.Size = new System.Drawing.Size(202, 75);
			this.m_edit_result.TabIndex = 0;
			this.m_edit_result.Text = "This is the resulting text after replacement";
			//
			// m_lbl_replace
			//
			this.m_lbl_replace.Location = new System.Drawing.Point(3, 1);
			this.m_lbl_replace.Name = "m_lbl_replace";
			this.m_lbl_replace.Size = new System.Drawing.Size(70, 17);
			this.m_lbl_replace.TabIndex = 29;
			this.m_lbl_replace.Text = "Replace:";
			this.m_lbl_replace.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			//
			// m_edit_replace
			//
			this.m_edit_replace.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_replace.Location = new System.Drawing.Point(76, 0);
			this.m_edit_replace.Name = "m_edit_replace";
			this.m_edit_replace.Size = new System.Drawing.Size(340, 20);
			this.m_edit_replace.TabIndex = 1;
			//
			// m_check_ignore_case
			//
			this.m_check_ignore_case.AutoSize = true;
			this.m_check_ignore_case.Location = new System.Drawing.Point(333, 2);
			this.m_check_ignore_case.Name = "m_check_ignore_case";
			this.m_check_ignore_case.Size = new System.Drawing.Size(83, 17);
			this.m_check_ignore_case.TabIndex = 3;
			this.m_check_ignore_case.Text = "Ignore Case";
			this.m_check_ignore_case.UseVisualStyleBackColor = true;
			//
			// m_split_test
			//
			this.m_split_test.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_split_test.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split_test.Location = new System.Drawing.Point(0, 0);
			this.m_split_test.Margin = new System.Windows.Forms.Padding(0);
			this.m_split_test.Name = "m_split_test";
			this.m_split_test.Orientation = System.Windows.Forms.Orientation.Horizontal;
			//
			// m_split_test.Panel1
			//
			this.m_split_test.Panel1.Controls.Add(this.m_edit_test);
			//
			// m_split_test.Panel2
			//
			this.m_split_test.Panel2.Controls.Add(this.m_edit_result);
			this.m_split_test.Size = new System.Drawing.Size(204, 138);
			this.m_split_test.SplitterDistance = 57;
			this.m_split_test.TabIndex = 0;
			//
			// m_lbl_match
			//
			this.m_lbl_match.Location = new System.Drawing.Point(3, 3);
			this.m_lbl_match.Name = "m_lbl_match";
			this.m_lbl_match.Size = new System.Drawing.Size(70, 17);
			this.m_lbl_match.TabIndex = 39;
			this.m_lbl_match.Text = "Pattern:";
			this.m_lbl_match.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			//
			// m_edit_match
			//
			this.m_edit_match.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_match.Location = new System.Drawing.Point(76, 2);
			this.m_edit_match.Name = "m_edit_match";
			this.m_edit_match.Size = new System.Drawing.Size(340, 20);
			this.m_edit_match.TabIndex = 0;
			//
			// m_grid_subs
			//
			this.m_grid_subs.AllowUserToAddRows = false;
			this.m_grid_subs.AllowUserToDeleteRows = false;
			this.m_grid_subs.AllowUserToResizeRows = false;
			this.m_grid_subs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_subs.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.m_grid_subs.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.m_grid_subs.ColumnHeadersHeight = 20;
			this.m_grid_subs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.m_grid_subs.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_subs.Location = new System.Drawing.Point(0, 0);
			this.m_grid_subs.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_subs.MultiSelect = false;
			this.m_grid_subs.Name = "m_grid_subs";
			this.m_grid_subs.RowHeadersVisible = false;
			this.m_grid_subs.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this.m_grid_subs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_subs.Size = new System.Drawing.Size(284, 138);
			this.m_grid_subs.TabIndex = 0;
			//
			// m_split_subs
			//
			this.m_split_subs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_split_subs.Location = new System.Drawing.Point(3, 99);
			this.m_split_subs.Margin = new System.Windows.Forms.Padding(0);
			this.m_split_subs.Name = "m_split_subs";
			//
			// m_split_subs.Panel1
			//
			this.m_split_subs.Panel1.Controls.Add(this.m_split_test);
			//
			// m_split_subs.Panel2
			//
			this.m_split_subs.Panel2.Controls.Add(this.m_grid_subs);
			this.m_split_subs.Size = new System.Drawing.Size(492, 138);
			this.m_split_subs.SplitterDistance = 204;
			this.m_split_subs.TabIndex = 49;
			//
			// m_radio_regex
			//
			this.m_radio_regex.AutoSize = true;
			this.m_radio_regex.Location = new System.Drawing.Point(218, 1);
			this.m_radio_regex.Name = "m_radio_regex";
			this.m_radio_regex.Size = new System.Drawing.Size(116, 17);
			this.m_radio_regex.TabIndex = 3;
			this.m_radio_regex.TabStop = true;
			this.m_radio_regex.Text = "Regular Expression";
			this.m_radio_regex.UseVisualStyleBackColor = true;
			//
			// m_radio_wildcard
			//
			this.m_radio_wildcard.AutoSize = true;
			this.m_radio_wildcard.Location = new System.Drawing.Point(151, 1);
			this.m_radio_wildcard.Name = "m_radio_wildcard";
			this.m_radio_wildcard.Size = new System.Drawing.Size(67, 17);
			this.m_radio_wildcard.TabIndex = 2;
			this.m_radio_wildcard.TabStop = true;
			this.m_radio_wildcard.Text = "Wildcard";
			this.m_radio_wildcard.UseVisualStyleBackColor = true;
			//
			// m_radio_substring
			//
			this.m_radio_substring.AutoSize = true;
			this.m_radio_substring.Location = new System.Drawing.Point(80, 1);
			this.m_radio_substring.Name = "m_radio_substring";
			this.m_radio_substring.Size = new System.Drawing.Size(69, 17);
			this.m_radio_substring.TabIndex = 1;
			this.m_radio_substring.TabStop = true;
			this.m_radio_substring.Text = "Substring";
			this.m_radio_substring.UseVisualStyleBackColor = true;
			//
			// m_edit_eqv_regex
			//
			this.m_edit_eqv_regex.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_eqv_regex.Location = new System.Drawing.Point(76, 0);
			this.m_edit_eqv_regex.Name = "m_edit_eqv_regex";
			this.m_edit_eqv_regex.ReadOnly = true;
			this.m_edit_eqv_regex.Size = new System.Drawing.Size(340, 20);
			this.m_edit_eqv_regex.TabIndex = 51;
			//
			// m_lbl_matchtype
			//
			this.m_lbl_matchtype.Location = new System.Drawing.Point(3, 0);
			this.m_lbl_matchtype.Name = "m_lbl_matchtype";
			this.m_lbl_matchtype.Size = new System.Drawing.Size(71, 18);
			this.m_lbl_matchtype.TabIndex = 53;
			this.m_lbl_matchtype.Text = "Pattern Type:";
			this.m_lbl_matchtype.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			//
			// m_btn_show_eqv_regex
			//
			this.m_btn_show_eqv_regex.Location = new System.Drawing.Point(3, 27);
			this.m_btn_show_eqv_regex.Name = "m_btn_show_eqv_regex";
			this.m_btn_show_eqv_regex.Size = new System.Drawing.Size(22, 21);
			this.m_btn_show_eqv_regex.TabIndex = 4;
			this.m_btn_show_eqv_regex.Text = "V";
			this.m_btn_show_eqv_regex.UseVisualStyleBackColor = true;
			//
			// m_lbl_eqv_regex
			//
			this.m_lbl_eqv_regex.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
			this.m_lbl_eqv_regex.Location = new System.Drawing.Point(3, 1);
			this.m_lbl_eqv_regex.Name = "m_lbl_eqv_regex";
			this.m_lbl_eqv_regex.Size = new System.Drawing.Size(70, 17);
			this.m_lbl_eqv_regex.TabIndex = 56;
			this.m_lbl_eqv_regex.Text = "Eqv. Regex:";
			this.m_lbl_eqv_regex.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			//
			// m_panel_btns
			//
			this.m_panel_btns.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_btns.Controls.Add(this.m_btn_add);
			this.m_panel_btns.Controls.Add(this.m_btn_show_eqv_regex);
			this.m_panel_btns.Controls.Add(this.m_btn_regex_help);
			this.m_panel_btns.Location = new System.Drawing.Point(422, 2);
			this.m_panel_btns.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_btns.Name = "m_panel_btns";
			this.m_panel_btns.Size = new System.Drawing.Size(76, 52);
			this.m_panel_btns.TabIndex = 63;
			//
			// m_panel_match_type
			//
			this.m_panel_match_type.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_match_type.AutoSize = true;
			this.m_panel_match_type.Controls.Add(this.m_check_ignore_case);
			this.m_panel_match_type.Controls.Add(this.m_radio_regex);
			this.m_panel_match_type.Controls.Add(this.m_radio_wildcard);
			this.m_panel_match_type.Controls.Add(this.m_radio_substring);
			this.m_panel_match_type.Controls.Add(this.m_lbl_matchtype);
			this.m_panel_match_type.Location = new System.Drawing.Point(0, 0);
			this.m_panel_match_type.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_match_type.Name = "m_panel_match_type";
			this.m_panel_match_type.Size = new System.Drawing.Size(419, 22);
			this.m_panel_match_type.TabIndex = 5;
			//
			// m_panel_match
			//
			this.m_panel_match.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_match.AutoSize = true;
			this.m_panel_match.Controls.Add(this.m_edit_match);
			this.m_panel_match.Controls.Add(this.m_lbl_match);
			this.m_panel_match.Location = new System.Drawing.Point(0, 22);
			this.m_panel_match.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_match.Name = "m_panel_match";
			this.m_panel_match.Size = new System.Drawing.Size(419, 25);
			this.m_panel_match.TabIndex = 66;
			//
			// m_table
			//
			this.m_table.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_table.AutoSize = true;
			this.m_table.ColumnCount = 1;
			this.m_table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table.Controls.Add(this.m_panel_replace, 0, 3);
			this.m_table.Controls.Add(this.m_panel_eqv_regex, 0, 2);
			this.m_table.Controls.Add(this.m_panel_match, 0, 1);
			this.m_table.Controls.Add(this.m_panel_match_type, 0, 0);
			this.m_table.Location = new System.Drawing.Point(3, 3);
			this.m_table.Name = "m_table";
			this.m_table.RowCount = 4;
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.Size = new System.Drawing.Size(419, 93);
			this.m_table.TabIndex = 66;
			//
			// m_panel_replace
			//
			this.m_panel_replace.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_replace.AutoSize = true;
			this.m_panel_replace.Controls.Add(this.m_edit_replace);
			this.m_panel_replace.Controls.Add(this.m_lbl_replace);
			this.m_panel_replace.Location = new System.Drawing.Point(0, 70);
			this.m_panel_replace.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_replace.Name = "m_panel_replace";
			this.m_panel_replace.Size = new System.Drawing.Size(419, 23);
			this.m_panel_replace.TabIndex = 67;
			//
			// m_panel_eqv_regex
			//
			this.m_panel_eqv_regex.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_eqv_regex.AutoSize = true;
			this.m_panel_eqv_regex.Controls.Add(this.m_edit_eqv_regex);
			this.m_panel_eqv_regex.Controls.Add(this.m_lbl_eqv_regex);
			this.m_panel_eqv_regex.Location = new System.Drawing.Point(0, 47);
			this.m_panel_eqv_regex.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_eqv_regex.Name = "m_panel_eqv_regex";
			this.m_panel_eqv_regex.Size = new System.Drawing.Size(419, 23);
			this.m_panel_eqv_regex.TabIndex = 67;
			this.m_panel_eqv_regex.Visible = false;
			//
			// TransformUI
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_table);
			this.Controls.Add(this.m_split_subs);
			this.Controls.Add(this.m_panel_btns);
			this.DoubleBuffered = true;
			this.Margin = new System.Windows.Forms.Padding(0);
			this.MinimumSize = new System.Drawing.Size(498, 212);
			this.Name = "TransformUI";
			this.Size = new System.Drawing.Size(498, 237);
			this.m_split_test.Panel1.ResumeLayout(false);
			this.m_split_test.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split_test)).EndInit();
			this.m_split_test.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_subs)).EndInit();
			this.m_split_subs.Panel1.ResumeLayout(false);
			this.m_split_subs.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split_subs)).EndInit();
			this.m_split_subs.ResumeLayout(false);
			this.m_panel_btns.ResumeLayout(false);
			this.m_panel_match_type.ResumeLayout(false);
			this.m_panel_match_type.PerformLayout();
			this.m_panel_match.ResumeLayout(false);
			this.m_panel_match.PerformLayout();
			this.m_table.ResumeLayout(false);
			this.m_table.PerformLayout();
			this.m_panel_replace.ResumeLayout(false);
			this.m_panel_replace.PerformLayout();
			this.m_panel_eqv_regex.ResumeLayout(false);
			this.m_panel_eqv_regex.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion
	}

	/// <summary>Workaround for the retarded VS designer</summary>
	public class TransformUIImpl :PatternUIBase<Transform>
	{
		/// <summary>Access to the test text field</summary>
		public override string TestText { get; set; }

		/// <summary>Set focus to the primary input field</summary>
		public override void FocusInput() {}

		/// <summary>Update the UI elements based on the current pattern</summary>
		protected override void UpdateUIInternal() {}
	}

	/// <summary>Wrapper so that ITransformSubstitution can be displayed in a DGV combo box</summary>
	public class TransSubWrapper
	{
		public ITransformSubstitution Sub { get; private set; }
		public override string ToString() { return Sub.DropDownName; }
		public TransSubWrapper(ITransformSubstitution sub) { Sub = sub; }
	}
}
