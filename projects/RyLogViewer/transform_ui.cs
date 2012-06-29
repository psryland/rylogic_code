using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using pr.gfx;
using pr.util;

namespace RyLogViewer
{
	public class TransformUI :UserControl ,IPatternUI
	{
		private enum BtnImageIdx { AddNew = 0, Save = 1 }
		private static class ColumnNames
		{
			public const string Id   = "Id";
			public const string Type = "Type";
			public const string Data = "Data";
		}
		private static readonly Color[] m_bk_colors = new[]
		{
			Color.LightGreen, Color.LightBlue, Color.LightCoral, Color.LightSalmon, Color.Violet, Color.LightSkyBlue,
			Color.Aquamarine, Color.Yellow, Color.Orchid, Color.GreenYellow, Color.PaleGreen, Color.Goldenrod, Color.MediumTurquoise
		};
		
		private readonly ToolTip m_tt;
		private List<string>   m_cap_ids;
		private Transform      m_transform;
		private TextBox        m_edit_match;
		private TextBox        m_edit_replace;
		private DataGridView   m_grid_subs;
		private Label          m_lbl_match;
		private Label          m_lbl_replace;
		private Label          m_lbl_subs;
		private CheckBox       m_check_ignore_case;
		private Button         m_btn_add;
		private Button         m_btn_regex_help;
		private ImageList      m_image_list;
		private SplitContainer m_split_test;
		private RichTextBox    m_edit_test;
		private SplitContainer m_split_subs;
		private Panel          m_group_patntype;
		private RadioButton    m_radio_regex;
		private RadioButton    m_radio_wildcard;
		private RadioButton    m_radio_substring;
		private TextBox m_edit_compiled_regex;
		private Label label1;
		private RichTextBox    m_edit_result;
		
		/// <summary>The pattern being controlled by this UI</summary>
		public Transform Transform { get { return m_transform; } }
		
		/// <summary>The test text to use</summary>
		public string TestText { get { return m_edit_test.Text; } set { m_edit_test.Text = value; } }

		/// <summary>True if the editted pattern is a new instance</summary>
		public bool IsNew { get; private set; }
		
		/// <summary>Raised when the 'Add' button is hit and the pattern field contains a valid pattern</summary>
		public event EventHandler Add;
		
		public TransformUI()
		{
			InitializeComponent();
			m_transform = null;
			m_tt = new ToolTip();
			string tt;
			
			// Add/Update
			m_btn_add.ToolTip(m_tt, "Adds a new transform, or updates an existing transform");
			m_btn_add.Click += (s,a)=>
				{
					if (Add == null) return;
					if (Transform.Match.Length == 0) return;
					Add(this, EventArgs.Empty);
				};

			// Match help
			m_btn_regex_help.ToolTip(m_tt, "Displays a quick help guide for the Match field");
			m_btn_regex_help.Click += (s,a)=>
				{
					ShowMatchHelp();
				};

			// Match
			tt =
				"The format string used to identify rows to transform.\r\n"+
				"Create capture groups using '{' and '}', e.g. {one},{2},{tag},etc\r\n";
			m_lbl_match.ToolTip(m_tt, tt);
			m_edit_match.ToolTip(m_tt, tt);
			m_edit_match.TextChanged += (s,a)=>
				{
					Transform.Match = m_edit_match.Text;
					UpdateUI();
				};

			// Match - Substring
			m_radio_substring.ToolTip(m_tt, "Match any occurance of the pattern as a substring");
			m_radio_substring.Click += (s,a)=>
				{
					if (m_radio_substring.Checked) Transform.PatnType = EPattern.Substring;
					UpdateUI();
				};
			
			// Match - Wildcard
			m_radio_wildcard.ToolTip(m_tt, "Match using wildcards, where '*' matches any number of characters and '?' matches any single character");
			m_radio_wildcard.Click += (s,a)=>
				{
					if (m_radio_wildcard.Checked) Transform.PatnType = EPattern.Wildcard;
					UpdateUI();
				};
			
			// Match - Regex
			m_radio_regex.ToolTip(m_tt, "Match using a regular expression");
			m_radio_regex.Click += (s,a)=>
				{
					if (m_radio_regex.Checked) Transform.PatnType = EPattern.RegularExpression;
					UpdateUI();
				};
			
			// Match - Ignore case
			m_check_ignore_case.ToolTip(m_tt, "Enable to have the template ignore case when matching");
			m_check_ignore_case.Click += (s,a)=>
				{
					Transform.IgnoreCase = m_check_ignore_case.Checked;
					UpdateUI();
				};
			
			// Match - compiled regex
			m_edit_compiled_regex.ToolTip(m_tt, "The regular expression that the Match field is converted into.\r\nVisible here for reference and diagnostic purposes");
			
			// Replace
			tt = "The template that describes the transformed result.\r\nUse the capture groups created in the Match field";
			m_lbl_replace.ToolTip(m_tt, tt);
			m_edit_replace.ToolTip(m_tt, tt);
			m_edit_replace.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					Transform.Replace = m_edit_replace.Text;
					UpdateUI();
				};

			// Substitutions
			m_grid_subs.VirtualMode = true;
			m_grid_subs.AutoGenerateColumns = false;
			m_grid_subs.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Id   ,HeaderText = "Id"   ,FillWeight = 20 ,ReadOnly = true});
			m_grid_subs.Columns.Add(new DataGridViewComboBoxColumn{Name = ColumnNames.Type ,HeaderText = "Name" ,FillWeight = 30 ,DataSource = Transform.SubLoader.TxfmSubs, DisplayMember = "Name", FlatStyle=FlatStyle.Flat});
			m_grid_subs.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Data ,HeaderText = "Data" ,FillWeight = 50 ,ReadOnly = true});
			m_grid_subs.CurrentCellDirtyStateChanged += (s,a) => m_grid_subs.CommitEdit(DataGridViewDataErrorContexts.Commit);
			m_grid_subs.CellValueNeeded  += CellValueNeeded;
			m_grid_subs.CellValuePushed  += CellValuePushed;
			m_grid_subs.CellPainting     += CellPainting;
			m_grid_subs.CellClick        += CellClick;
			
			// Test text
			m_edit_test.ToolTip(m_tt, "Enter text here on which to test your pattern.");
			m_edit_test.TextChanged += (s,a)=>
				{
					UpdateUI();
				};

			// Result text
			m_edit_result.ToolTip(m_tt, "Shows the result of applying the transform to the text in the test area above");
		}

		/// <summary>Get the cell value from the transform</summary>
		private void CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			e.Value = string.Empty;
			DataGridView grid = (DataGridView)sender;
			if (m_cap_ids == null || e.RowIndex < 0 || e.RowIndex >= m_cap_ids.Count) return;
			if (!Transform.Subs.ContainsKey(m_cap_ids[e.RowIndex])) return;
			
			ITxfmSub sub = Transform.Subs[m_cap_ids[e.RowIndex]];
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default:               e.Value = string.Empty; break;
			case ColumnNames.Id:   e.Value = sub.Id;   break;
			case ColumnNames.Type: e.Value = sub.Name; break;
			case ColumnNames.Data: e.Value = sub.ConfigSummary; break;
			}
		}

		/// <summary>Handle cell values changed</summary>
		private void CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
		{
			DataGridView grid = (DataGridView)sender;
			if (m_cap_ids == null || e.RowIndex < 0 || e.RowIndex >= m_cap_ids.Count) return;
			if (!Transform.Subs.ContainsKey(m_cap_ids[e.RowIndex])) return;

			ITxfmSub sub = Transform.Subs[m_cap_ids[e.RowIndex]];
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			case ColumnNames.Type:
				if (!e.Value.Equals(sub.Name))
				{
					var new_sub = Transform.SubLoader.Create(sub.Id, (string)e.Value);
					Transform.Subs[m_cap_ids[e.RowIndex]] = new_sub;
					grid.Invalidate();
				}
				break;
			}
			UpdateUI();
		}
		
		/// <summary>Paint the backgrounds to show capture groups</summary>
		private void CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			e.Handled = false;
			DataGridView grid = (DataGridView)sender;
			if (e.RowIndex < 0 || e.RowIndex >= Transform.Subs.Count) return;
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			case ColumnNames.Id:
				e.CellStyle.BackColor = m_bk_colors[e.RowIndex % m_bk_colors.Length];
				e.CellStyle.SelectionBackColor = Gfx.Blend(e.CellStyle.BackColor, Color.Black, 0.2f);
				break;
			}
		}

		/// <summary>Handle clicks on cells</summary>
		private void CellClick(object sender, DataGridViewCellEventArgs e)
		{
			DataGridView grid = (DataGridView)sender;
			if (e.RowIndex < 0 || e.RowIndex >= Transform.Subs.Count) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
			ITxfmSub sub = Transform.Subs[m_cap_ids[e.RowIndex]];
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: return;
			case ColumnNames.Data:
				sub.Config(this);
				grid.Invalidate();
				break;
			}
		}

		/// <summary>Select 'tx' as a new transform</summary>
		public void NewPattern(IPattern tx)
		{
			IsNew = true;
			m_transform = (Transform)tx;
			m_btn_add.ImageIndex = (int)BtnImageIdx.AddNew;
			m_btn_add.ToolTip(m_tt, "Add this new transform");
			UpdateUI();
		}

		/// <summary>Select a pattern into the UI for editting</summary>
		public void EditPattern(IPattern tx)
		{
			IsNew = false;
			m_transform = (Transform)tx;
			m_btn_add.ImageIndex = (int)BtnImageIdx.Save;
			m_btn_add.ToolTip(m_tt, "Finish editing this transform");
			UpdateUI();
		}
		
		/// <summary>Prevents reentrant calls to UpdateUI. Yes this is the best way to do it /cry</summary>
		private bool m_in_update_ui;
		private void UpdateUI()
		{
			if (m_in_update_ui) return;
			try
			{
				m_in_update_ui = true;
				SuspendLayout();
			
				m_cap_ids = Transform.CaptureIds;
				m_grid_subs.RowCount = m_cap_ids.Count;
			
				m_btn_add.Enabled           = Transform.IsValid && Transform.Match.Length != 0;
				m_edit_match.Text           = Transform.Match;
				m_edit_compiled_regex.Text  = Transform.RegexString;
				m_edit_replace.Text         = Transform.Replace;
				m_radio_substring.Checked   = Transform.PatnType == EPattern.Substring;
				m_radio_wildcard.Checked    = Transform.PatnType == EPattern.Wildcard;
				m_radio_regex.Checked       = Transform.PatnType == EPattern.RegularExpression;
				m_check_ignore_case.Checked = Transform.IgnoreCase;
				
				if (Transform.IsValid)
				{
					// Preserve the current carot position
					int start  = m_edit_test.SelectionStart;
					int length = m_edit_test.SelectionLength;
			
					// Reset the highlighting
					m_edit_test.SelectAll();
					m_edit_test.SelectionBackColor = Color.White;
			
					string[] lines = m_edit_test.Lines;
			
					// Apply the transform to each line in the test text
					m_edit_result.Clear();
					for (int i = 0, iend = lines.Length; i != iend; ++i)
					{
						m_edit_result.Select(m_edit_result.TextLength, 0);
						if (i != 0) m_edit_result.SelectedText = Environment.NewLine;
						if (!Transform.IsMatch(lines[i]))
						{
							m_edit_result.SelectedText = lines[i];
						}
						else
						{
							int starti = m_edit_test.GetFirstCharIndexFromLine(i);
							int startj = m_edit_result.TextLength;
					
							List<Transform.Capture> src_caps, dst_caps;
							string result = Transform.Txfm(lines[i], out src_caps, out dst_caps);
							m_edit_result.SelectedText = result;

							// Highlight the capture groups in the test text and the result
							int j = 0; foreach (var s in src_caps)
							{
								m_edit_test.Select(starti + s.Span.Index, s.Span.Count);
								m_edit_test.SelectionBackColor = m_bk_colors[j++ % m_bk_colors.Length];
							}
							j = 0; foreach (var s in dst_caps)
							{
								m_edit_result.Select(startj + s.Span.Index, s.Span.Count);
								m_edit_result.SelectionBackColor = m_bk_colors[j++ % m_bk_colors.Length];
							}
						}
					}
			
					// Restore the selection
					m_edit_test.Select(start, length);
				}
			}
			finally
			{
				m_in_update_ui = false;
				ResumeLayout();
			}
		}

		/// <summary>Display a quick help for the match field syntax</summary>
		private void ShowMatchHelp()
		{
			HelpUI.Show(this
				,"RyLogViewer.docs.TransformQuickRef.html"
				,"Transform Help"
				,PointToScreen(Location) + new Size(Width, 0)
				,new Size(640,480));
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
			this.m_btn_regex_help = new System.Windows.Forms.Button();
			this.m_btn_add = new System.Windows.Forms.Button();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_edit_test = new System.Windows.Forms.RichTextBox();
			this.m_edit_result = new System.Windows.Forms.RichTextBox();
			this.m_lbl_replace = new System.Windows.Forms.Label();
			this.m_edit_replace = new System.Windows.Forms.TextBox();
			this.m_check_ignore_case = new System.Windows.Forms.CheckBox();
			this.m_split_test = new System.Windows.Forms.SplitContainer();
			this.m_lbl_match = new System.Windows.Forms.Label();
			this.m_edit_match = new System.Windows.Forms.TextBox();
			this.m_grid_subs = new System.Windows.Forms.DataGridView();
			this.m_lbl_subs = new System.Windows.Forms.Label();
			this.m_split_subs = new System.Windows.Forms.SplitContainer();
			this.m_group_patntype = new System.Windows.Forms.Panel();
			this.m_radio_regex = new System.Windows.Forms.RadioButton();
			this.m_radio_wildcard = new System.Windows.Forms.RadioButton();
			this.m_radio_substring = new System.Windows.Forms.RadioButton();
			this.m_edit_compiled_regex = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_split_test)).BeginInit();
			this.m_split_test.Panel1.SuspendLayout();
			this.m_split_test.Panel2.SuspendLayout();
			this.m_split_test.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_subs)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_split_subs)).BeginInit();
			this.m_split_subs.Panel1.SuspendLayout();
			this.m_split_subs.Panel2.SuspendLayout();
			this.m_split_subs.SuspendLayout();
			this.m_group_patntype.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_btn_regex_help
			// 
			this.m_btn_regex_help.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_regex_help.Location = new System.Drawing.Point(403, 7);
			this.m_btn_regex_help.Name = "m_btn_regex_help";
			this.m_btn_regex_help.Size = new System.Drawing.Size(22, 21);
			this.m_btn_regex_help.TabIndex = 10;
			this.m_btn_regex_help.Text = "?";
			this.m_btn_regex_help.UseVisualStyleBackColor = true;
			// 
			// m_btn_add
			// 
			this.m_btn_add.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_add.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.m_btn_add.ImageIndex = 0;
			this.m_btn_add.ImageList = this.m_image_list;
			this.m_btn_add.Location = new System.Drawing.Point(431, 6);
			this.m_btn_add.Name = "m_btn_add";
			this.m_btn_add.Size = new System.Drawing.Size(46, 46);
			this.m_btn_add.TabIndex = 11;
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
			this.m_edit_test.Size = new System.Drawing.Size(473, 55);
			this.m_edit_test.TabIndex = 0;
			this.m_edit_test.Text = "Enter text here to test your pattern\n    Enter text here to test your pattern on\n" +
    "** Enter text here to test your pattern **";
			// 
			// m_edit_result
			// 
			this.m_edit_result.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_edit_result.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_edit_result.Location = new System.Drawing.Point(0, 0);
			this.m_edit_result.Name = "m_edit_result";
			this.m_edit_result.ReadOnly = true;
			this.m_edit_result.Size = new System.Drawing.Size(473, 64);
			this.m_edit_result.TabIndex = 0;
			this.m_edit_result.Text = "This is the resulting text after replacement";
			// 
			// m_lbl_replace
			// 
			this.m_lbl_replace.AutoSize = true;
			this.m_lbl_replace.Location = new System.Drawing.Point(24, 81);
			this.m_lbl_replace.Name = "m_lbl_replace";
			this.m_lbl_replace.Size = new System.Drawing.Size(50, 13);
			this.m_lbl_replace.TabIndex = 29;
			this.m_lbl_replace.Text = "Replace:";
			this.m_lbl_replace.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_edit_replace
			// 
			this.m_edit_replace.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_replace.Location = new System.Drawing.Point(74, 78);
			this.m_edit_replace.Name = "m_edit_replace";
			this.m_edit_replace.Size = new System.Drawing.Size(323, 20);
			this.m_edit_replace.TabIndex = 7;
			this.m_edit_replace.Text = "This is the result of your {2} {1} on {0}";
			// 
			// m_check_ignore_case
			// 
			this.m_check_ignore_case.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_check_ignore_case.AutoSize = true;
			this.m_check_ignore_case.Location = new System.Drawing.Point(347, 33);
			this.m_check_ignore_case.Name = "m_check_ignore_case";
			this.m_check_ignore_case.Size = new System.Drawing.Size(83, 17);
			this.m_check_ignore_case.TabIndex = 2;
			this.m_check_ignore_case.Text = "Ignore Case";
			this.m_check_ignore_case.UseVisualStyleBackColor = true;
			// 
			// m_split_test
			// 
			this.m_split_test.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_split_test.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_split_test.Location = new System.Drawing.Point(0, 0);
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
			this.m_split_test.Size = new System.Drawing.Size(475, 127);
			this.m_split_test.SplitterDistance = 57;
			this.m_split_test.TabIndex = 8;
			// 
			// m_lbl_match
			// 
			this.m_lbl_match.AutoSize = true;
			this.m_lbl_match.Location = new System.Drawing.Point(34, 11);
			this.m_lbl_match.Name = "m_lbl_match";
			this.m_lbl_match.Size = new System.Drawing.Size(40, 13);
			this.m_lbl_match.TabIndex = 39;
			this.m_lbl_match.Text = "Match:";
			this.m_lbl_match.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_edit_match
			// 
			this.m_edit_match.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_match.Location = new System.Drawing.Point(74, 8);
			this.m_edit_match.Name = "m_edit_match";
			this.m_edit_match.Size = new System.Drawing.Size(323, 20);
			this.m_edit_match.TabIndex = 0;
			this.m_edit_match.Text = "Enter {0} here to {1} your {2}";
			// 
			// m_grid_subs
			// 
			this.m_grid_subs.AllowUserToAddRows = false;
			this.m_grid_subs.AllowUserToDeleteRows = false;
			this.m_grid_subs.AllowUserToResizeRows = false;
			this.m_grid_subs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_subs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_subs.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_grid_subs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_subs.Location = new System.Drawing.Point(73, 0);
			this.m_grid_subs.MultiSelect = false;
			this.m_grid_subs.Name = "m_grid_subs";
			this.m_grid_subs.RowHeadersVisible = false;
			this.m_grid_subs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_subs.Size = new System.Drawing.Size(402, 118);
			this.m_grid_subs.TabIndex = 47;
			// 
			// m_lbl_subs
			// 
			this.m_lbl_subs.AutoSize = true;
			this.m_lbl_subs.Location = new System.Drawing.Point(3, 0);
			this.m_lbl_subs.Name = "m_lbl_subs";
			this.m_lbl_subs.Size = new System.Drawing.Size(70, 13);
			this.m_lbl_subs.TabIndex = 48;
			this.m_lbl_subs.Text = "Substitutions:";
			this.m_lbl_subs.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_split_subs
			// 
			this.m_split_subs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_split_subs.Location = new System.Drawing.Point(3, 103);
			this.m_split_subs.Name = "m_split_subs";
			this.m_split_subs.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split_subs.Panel1
			// 
			this.m_split_subs.Panel1.Controls.Add(this.m_lbl_subs);
			this.m_split_subs.Panel1.Controls.Add(this.m_grid_subs);
			// 
			// m_split_subs.Panel2
			// 
			this.m_split_subs.Panel2.Controls.Add(this.m_split_test);
			this.m_split_subs.Size = new System.Drawing.Size(475, 252);
			this.m_split_subs.SplitterDistance = 118;
			this.m_split_subs.TabIndex = 49;
			// 
			// m_group_patntype
			// 
			this.m_group_patntype.Controls.Add(this.m_radio_regex);
			this.m_group_patntype.Controls.Add(this.m_radio_wildcard);
			this.m_group_patntype.Controls.Add(this.m_radio_substring);
			this.m_group_patntype.Location = new System.Drawing.Point(76, 29);
			this.m_group_patntype.Name = "m_group_patntype";
			this.m_group_patntype.Size = new System.Drawing.Size(268, 23);
			this.m_group_patntype.TabIndex = 50;
			// 
			// m_radio_regex
			// 
			this.m_radio_regex.AutoSize = true;
			this.m_radio_regex.Location = new System.Drawing.Point(149, 3);
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
			this.m_radio_wildcard.Location = new System.Drawing.Point(76, 3);
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
			this.m_radio_substring.Location = new System.Drawing.Point(3, 3);
			this.m_radio_substring.Name = "m_radio_substring";
			this.m_radio_substring.Size = new System.Drawing.Size(69, 17);
			this.m_radio_substring.TabIndex = 1;
			this.m_radio_substring.TabStop = true;
			this.m_radio_substring.Text = "Substring";
			this.m_radio_substring.UseVisualStyleBackColor = true;
			// 
			// m_edit_compiled_regex
			// 
			this.m_edit_compiled_regex.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_compiled_regex.Location = new System.Drawing.Point(74, 55);
			this.m_edit_compiled_regex.Name = "m_edit_compiled_regex";
			this.m_edit_compiled_regex.ReadOnly = true;
			this.m_edit_compiled_regex.Size = new System.Drawing.Size(323, 20);
			this.m_edit_compiled_regex.TabIndex = 51;
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.AutoSize = true;
			this.label1.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
			this.label1.Location = new System.Drawing.Point(400, 58);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(63, 13);
			this.label1.TabIndex = 52;
			this.label1.Text = "Eqv. Regex";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// TransformUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_edit_compiled_regex);
			this.Controls.Add(this.m_group_patntype);
			this.Controls.Add(this.m_split_subs);
			this.Controls.Add(this.m_edit_match);
			this.Controls.Add(this.m_check_ignore_case);
			this.Controls.Add(this.m_edit_replace);
			this.Controls.Add(this.m_btn_regex_help);
			this.Controls.Add(this.m_btn_add);
			this.Controls.Add(this.m_lbl_match);
			this.Controls.Add(this.m_lbl_replace);
			this.DoubleBuffered = true;
			this.Margin = new System.Windows.Forms.Padding(0);
			this.MinimumSize = new System.Drawing.Size(400, 176);
			this.Name = "TransformUI";
			this.Size = new System.Drawing.Size(481, 355);
			this.m_split_test.Panel1.ResumeLayout(false);
			this.m_split_test.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split_test)).EndInit();
			this.m_split_test.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_subs)).EndInit();
			this.m_split_subs.Panel1.ResumeLayout(false);
			this.m_split_subs.Panel1.PerformLayout();
			this.m_split_subs.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split_subs)).EndInit();
			this.m_split_subs.ResumeLayout(false);
			this.m_group_patntype.ResumeLayout(false);
			this.m_group_patntype.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

	}
}
