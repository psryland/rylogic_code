using System;
using System.Drawing;
using System.Windows.Forms;
using pr.util;

namespace RyLogViewer
{
	public class TransformUI :UserControl ,IPatternUI
	{
		enum BtnImageIdx { AddNew = 0, Save = 1 }
		
		private readonly ToolTip m_tt;
		private readonly BindingSource m_subs;
		private Transform      m_transform;
		private TextBox        m_edit_match;
		private TextBox        m_edit_replace;
		private DataGridView   m_grid_subs;
		private Label          m_lbl_match;
		private Label          m_lbl_replace;
		private Label          m_lbl_subs;
		private Label          m_lbl_match_desc;
		private Label          m_lbl_replace_desc;
		private CheckBox       m_check_ignore_case;
		public  Button         m_btn_add;
		private Button         m_btn_regex_help;
		private ImageList      m_image_list;
		private SplitContainer m_split_test;
		private RichTextBox    m_edit_test;
		private RichTextBox    m_edit_result;
		
		/// <summary>The pattern being controlled by this UI</summary>
		public Transform Transform { get { return m_transform; } }
		
		/// <summary>True if the editted pattern is a new instance</summary>
		public bool IsNew { get; private set; }
		
		/// <summary>Raised when the 'Add' button is hit and the pattern field contains a valid pattern</summary>
		public event EventHandler Add;
		
		public TransformUI()
		{
			InitializeComponent();
			m_transform = null;
			m_tt = new ToolTip();
			m_subs = new BindingSource{DataSource = null};
			string tt;

			// Add/Update
			m_btn_add.ToolTip(m_tt, "Adds a new transform, or updates an existing transform");
			m_btn_add.Click += (s,a)=>
				{
					if (Add == null) return;
					if (Transform.Match.Length == 0) return;
					Add(this, EventArgs.Empty);
				};

			// Regex help
			m_btn_regex_help.ToolTip(m_tt, "Displays a quick help guide for regular expressions");
			m_btn_regex_help.Click += (s,a)=>
				{
					Misc.ShowQuickHelp(this);
				};

			// Match
			tt = "The template used to identify rows to transform.\r\nCreate capture patterns using '{#}', e.g. {0},{1},etc";
			m_lbl_match.ToolTip(m_tt, tt);
			m_edit_match.ToolTip(m_tt, tt);
			m_edit_match.TextChanged += (s,a)=>
				{
					Transform.Match = m_edit_match.Text;
					UpdateUI();
				};

			// Match - Ignore case
			m_check_ignore_case.ToolTip(m_tt, "Enable to have the template ignore case when matching");
			m_check_ignore_case.Click += (s,a)=>
				{
					Transform.IgnoreCase = m_check_ignore_case.Checked;
					UpdateUI();
				};

			// Replace
			tt = "The format that describes the transformed result.\r\nUse '{#}' to include capture patterns, e.g. {0},{1},etc";
			m_lbl_replace.ToolTip(m_tt, tt);
			m_edit_replace.ToolTip(m_tt, tt);
			m_edit_replace.TextChanged += (s,a)=>
				{
					Transform.Replace = m_edit_replace.Text;
					UpdateUI();
				};

			// Substitutions
			m_grid_subs.AutoGenerateColumns = false;
			m_grid_subs.Columns.Add(new DataGridViewTextBoxColumn {HeaderText = "Id"   ,FillWeight = 20 ,DataPropertyName = "Id"    });
			m_grid_subs.Columns.Add(new DataGridViewComboBoxColumn{HeaderText = "Type" ,FillWeight = 50 ,DataPropertyName = "Type"  ,DataSource = Enum.GetNames(typeof(Transform.Sub.EType))});
			m_grid_subs.Columns.Add(new DataGridViewTextBoxColumn {HeaderText = "Data" ,FillWeight = 50 ,DataPropertyName = "Type"  });
			m_grid_subs.DataSource = m_subs;
			m_grid_subs.CellPainting += CellPainting;

			// Test text
			m_edit_test.ToolTip(m_tt, "A area for testing your pattern. Add any text you like here");
			m_edit_test.TextChanged += (s,a)=>
				{
					UpdateUI();
				};

			// Result text
			m_edit_result.ToolTip(m_tt, "Shows the result of applying the transform to the text in the test area");
		}

		private void CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			// Highlight the edit field backgrounds to show valid expressions
			//m_edit_match  .BackColor = Transform.Match  .ExprValid ? Color.LightGreen : Color.LightSalmon;
			
			e.Handled = false;
		}

		/// <summary>Select 'tx' as a new transform</summary>
		public void NewPattern(IPattern tx)
		{
			IsNew = true;
			m_transform = (Transform)tx;
			m_subs.DataSource = m_transform.Subs;
			m_btn_add.ImageIndex = (int)BtnImageIdx.AddNew;
			m_btn_add.ToolTip(m_tt, "Add this new transform");
			UpdateUI();
		}

		/// <summary>Select a pattern into the UI for editting</summary>
		public void EditPattern(IPattern tx)
		{
			IsNew = false;
			m_transform = (Transform)tx;
			m_subs.DataSource = m_transform.Subs;
			m_btn_add.ImageIndex = (int)BtnImageIdx.Save;
			m_btn_add.ToolTip(m_tt, "Finish editing this transform");
			UpdateUI();
		}
	
		private void UpdateUI()
		{
			SuspendLayout();
			
			m_btn_add.Enabled           = Transform.IsValid && Transform.Match.Length != 0;
			m_edit_match.Text           = Transform.Match;
			m_edit_replace.Text         = Transform.Replace;
			m_check_ignore_case.Checked = Transform.IgnoreCase;
			
			bool ismatch = Transform.IsMatch(m_edit_test.Text);
			m_edit_result.BackColor = ismatch ? Color.LightGreen : Color.LightSalmon;
			m_edit_result.Text = ismatch
				? Transform.Txfm(m_edit_test.Text)
				: "The Match field does not match the test text";
			
			// Preserve the current carot position
			int start = m_edit_test.SelectionStart;
			int length = m_edit_test.SelectionLength;
			
			// Reset the highlighting
			m_edit_test.SelectAll();
			m_edit_test.SelectionBackColor = Color.White;
			
			// Highlight the captured groups in the test text and the result
			foreach (var s in Transform.Captures(m_edit_test.Text))
			{
				m_edit_test.SelectionStart     = s.Value.Span.Index;
				m_edit_test.SelectionLength    = s.Value.Span.Count;
				m_edit_test.SelectionBackColor = Color.LightBlue;
			}

			//foreach (var r in Pattern.Match(m_edit_test.Text))
			//{
			//    m_edit_test.SelectionStart     = (int)r.First;
			//    m_edit_test.SelectionLength    = (int)r.Count;
			//    m_edit_test.SelectionBackColor = Color.LightBlue;
			//}
			m_edit_test.SelectionStart  = start;
			m_edit_test.SelectionLength = length;
			
			ResumeLayout();
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
			this.m_lbl_match_desc = new System.Windows.Forms.Label();
			this.m_lbl_replace_desc = new System.Windows.Forms.Label();
			this.m_grid_subs = new System.Windows.Forms.DataGridView();
			this.m_lbl_subs = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_split_test)).BeginInit();
			this.m_split_test.Panel1.SuspendLayout();
			this.m_split_test.Panel2.SuspendLayout();
			this.m_split_test.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_subs)).BeginInit();
			this.SuspendLayout();
			// 
			// m_btn_regex_help
			// 
			this.m_btn_regex_help.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_regex_help.Location = new System.Drawing.Point(374, 55);
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
			this.m_btn_add.Location = new System.Drawing.Point(350, 6);
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
			this.m_edit_test.Size = new System.Drawing.Size(391, 44);
			this.m_edit_test.TabIndex = 0;
			this.m_edit_test.Text = "Enter text here to test your pattern";
			// 
			// m_edit_result
			// 
			this.m_edit_result.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_edit_result.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_edit_result.Location = new System.Drawing.Point(0, 0);
			this.m_edit_result.Name = "m_edit_result";
			this.m_edit_result.ReadOnly = true;
			this.m_edit_result.Size = new System.Drawing.Size(391, 53);
			this.m_edit_result.TabIndex = 0;
			this.m_edit_result.Text = "This is the resulting text after replacement";
			// 
			// m_lbl_replace
			// 
			this.m_lbl_replace.AutoSize = true;
			this.m_lbl_replace.Location = new System.Drawing.Point(24, 63);
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
			this.m_edit_replace.Location = new System.Drawing.Point(74, 60);
			this.m_edit_replace.Name = "m_edit_replace";
			this.m_edit_replace.Size = new System.Drawing.Size(270, 20);
			this.m_edit_replace.TabIndex = 7;
			this.m_edit_replace.Text = "This is the result of your {2} {1} on {0}";
			// 
			// m_check_ignore_case
			// 
			this.m_check_ignore_case.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_check_ignore_case.AutoSize = true;
			this.m_check_ignore_case.Location = new System.Drawing.Point(261, 40);
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
			this.m_split_test.Location = new System.Drawing.Point(3, 187);
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
			this.m_split_test.Size = new System.Drawing.Size(393, 105);
			this.m_split_test.SplitterDistance = 46;
			this.m_split_test.TabIndex = 8;
			// 
			// m_lbl_match
			// 
			this.m_lbl_match.AutoSize = true;
			this.m_lbl_match.Location = new System.Drawing.Point(34, 23);
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
			this.m_edit_match.Location = new System.Drawing.Point(74, 20);
			this.m_edit_match.Name = "m_edit_match";
			this.m_edit_match.Size = new System.Drawing.Size(270, 20);
			this.m_edit_match.TabIndex = 0;
			this.m_edit_match.Text = "Enter {0} here to {1} your {2}";
			// 
			// m_lbl_match_desc
			// 
			this.m_lbl_match_desc.AutoSize = true;
			this.m_lbl_match_desc.Location = new System.Drawing.Point(71, 4);
			this.m_lbl_match_desc.Name = "m_lbl_match_desc";
			this.m_lbl_match_desc.Size = new System.Drawing.Size(179, 13);
			this.m_lbl_match_desc.TabIndex = 41;
			this.m_lbl_match_desc.Text = "Find rows that match this template... ";
			// 
			// m_lbl_replace_desc
			// 
			this.m_lbl_replace_desc.AutoSize = true;
			this.m_lbl_replace_desc.Location = new System.Drawing.Point(71, 44);
			this.m_lbl_replace_desc.Name = "m_lbl_replace_desc";
			this.m_lbl_replace_desc.Size = new System.Drawing.Size(151, 13);
			this.m_lbl_replace_desc.TabIndex = 46;
			this.m_lbl_replace_desc.Text = "...and replace using this format";
			// 
			// m_grid_subs
			// 
			this.m_grid_subs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_subs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_subs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_subs.Location = new System.Drawing.Point(74, 86);
			this.m_grid_subs.Name = "m_grid_subs";
			this.m_grid_subs.RowHeadersVisible = false;
			this.m_grid_subs.Size = new System.Drawing.Size(321, 97);
			this.m_grid_subs.TabIndex = 47;
			// 
			// m_lbl_subs
			// 
			this.m_lbl_subs.AutoSize = true;
			this.m_lbl_subs.Location = new System.Drawing.Point(4, 86);
			this.m_lbl_subs.Name = "m_lbl_subs";
			this.m_lbl_subs.Size = new System.Drawing.Size(70, 13);
			this.m_lbl_subs.TabIndex = 48;
			this.m_lbl_subs.Text = "Substitutions:";
			this.m_lbl_subs.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// TransformUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_grid_subs);
			this.Controls.Add(this.m_split_test);
			this.Controls.Add(this.m_lbl_replace_desc);
			this.Controls.Add(this.m_lbl_match_desc);
			this.Controls.Add(this.m_edit_match);
			this.Controls.Add(this.m_check_ignore_case);
			this.Controls.Add(this.m_edit_replace);
			this.Controls.Add(this.m_btn_regex_help);
			this.Controls.Add(this.m_btn_add);
			this.Controls.Add(this.m_lbl_subs);
			this.Controls.Add(this.m_lbl_match);
			this.Controls.Add(this.m_lbl_replace);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.MinimumSize = new System.Drawing.Size(400, 240);
			this.Name = "TransformUI";
			this.Size = new System.Drawing.Size(400, 295);
			this.m_split_test.Panel1.ResumeLayout(false);
			this.m_split_test.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split_test)).EndInit();
			this.m_split_test.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_subs)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

	}
}
