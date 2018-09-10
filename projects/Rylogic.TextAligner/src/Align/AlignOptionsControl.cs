using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Util = Rylogic.Utility.Util;

namespace Rylogic.TextAligner
{
	internal class AlignOptionsControl :UserControl
	{
		private readonly AlignOptions m_options;
		private readonly BindingSource m_bs_groups;
		private readonly BindingSource m_bs_patterns;
		private readonly Dictionary<AlignPattern, EditPatternUI> m_edit_windows;
		
		#region UI Elements
		private SplitContainer m_split;
		private TableLayoutPanel m_table1;
		private TableLayoutPanel m_table2;
		private FlowLayoutPanel m_flow;
		private Panel m_panel;
		private GroupGrid m_grid_groups;
		private PatternGrid m_grid_patterns;
		private Label m_lbl_alignment_group;
		private Label m_lbl_alignment_patterns;
		private Button m_btn_help_groups;
		private Button m_btn_new;
		private Button m_btn_move_down;
		private Button m_btn_move_up;
		private Button m_btn_del;
		private Button m_btn_reset;
		private ImageList m_image_list;
		private Button m_btn_donate;
		private ToolTip m_tt;
		#endregion

		public AlignOptionsControl(AlignOptions options)
		{
			InitializeComponent();

			m_options = options;
			m_bs_groups = new BindingSource{DataSource = m_options.Groups};
			m_bs_patterns = new BindingSource{DataSource = null};
			m_edit_windows = new Dictionary<AlignPattern,EditPatternUI>();

			options.Deactivating += (s,a) =>
			{
				if (m_grid_groups.IsCurrentCellInEditMode  ) { m_grid_groups  .EndEdit(); a.Cancel = true; }
				if (m_grid_patterns.IsCurrentCellInEditMode) { m_grid_patterns.EndEdit(); a.Cancel = true; }
			};

			options.Saving += (s,a) =>
			{
				// Grab focus to take grids out of edit mode
				Focus();
			};

			SetupUI();

			SetPatternsDataSource();
			UpdateButtonStates();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(m_bs_groups);
			Util.Dispose(m_bs_patterns);
			components.Dispose();
			base.Dispose(disposing);
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// Set up the grid of alignment groups
			m_grid_groups.Data = m_bs_groups;

			// When the selected item in the groups grid changes, update the data source of the pattern grid
			m_bs_groups.PositionChanged += (s,a) =>
			{
				SetPatternsDataSource();
				UpdateButtonStates();
			};

			// Set up add/remove/order buttons
			m_btn_new.ToolTip(m_tt, "Add a new alignment group");
			m_btn_new.Click += (s,a) =>
			{
				m_bs_groups.AddNew();
				m_grid_groups.FirstDisplayedScrollingRowIndex = m_grid_groups.RowCount - m_grid_groups.DisplayedRowCount(false);
				m_grid_groups.SelectSingleRow(m_grid_groups.RowCount - 1);
				m_grid_groups.BeginEdit(true);
			};
			m_btn_del.ToolTip(m_tt, "Delete the selected alignment group");
			m_btn_del.Click += (s,a) =>
			{
				m_bs_groups.RemoveCurrent();
			};
			m_btn_move_up.ToolTip(m_tt, "Move the selected alignment group up in the priority order");
			m_btn_move_up.Click += (s,a) =>
			{
				var x = m_bs_groups.Position;
				if (x <= 0) return;
				m_bs_groups.List.Swap(x, x-1);
				m_grid_groups.SelectSingleRow(x - 1);
			};
			m_btn_move_down.ToolTip(m_tt, "Move the selected alignment group down in the priority order");
			m_btn_move_down.Click += (s,a) =>
			{
				var x = m_bs_groups.Position;
				if (x >= m_bs_groups.Count - 1) return;
				m_bs_groups.List.Swap(x, x+1);
				m_grid_groups.SelectSingleRow(x + 1);
			};

			// Set up the donate button
			m_btn_donate.ToolTip(m_tt, "Buy me a beer!");
			m_btn_donate.Click += (s, a) =>
			{
				ShowDonate();
			};

			// Set up the help button
			m_btn_help_groups.ToolTip(m_tt, "Display a window containing help information");
			m_btn_help_groups.Click += (s, a) =>
			{
				ShowHelp();
			};

			// Set up reset button
			m_btn_reset.ToolTip(m_tt, "Resets the alignment groups to the defaults");
			m_btn_reset.Click += (s,a) =>
			{
				var r = MsgBox.Show(this, "Reset all alignment groups/patterns to defaults?", "Reset to Default", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
				if (r != DialogResult.OK) return;
				m_options.ResetSettings();
				m_options.SaveSettingsToStorage();
				m_bs_groups.ResetBindings(false);
				m_bs_patterns.ResetBindings(false);
			};

			// Set up the grid of patterns
			m_grid_patterns.Data = m_bs_patterns;
			m_grid_patterns.ShowEditPatternDlg = ShowEditPatternDlg;
			m_grid_patterns.CloseEditPatternDlg = CloseEditPatternDlg;
		}

		/// <summary>Update the enabled states of the group buttons</summary>
		private void UpdateButtonStates()
		{
			var pos = m_bs_groups.Position;
			m_btn_move_up   .Enabled = pos > 0;
			m_btn_move_down .Enabled = pos < m_bs_groups.Count - 1;
			m_btn_del       .Enabled = m_bs_groups.CurrentOrDefault() != null;
		}

		/// <summary>Update the data source for the patterns grid to be the current group patterns</summary>
		private void SetPatternsDataSource()
		{
			m_bs_patterns.DataSource = null;

			var grp = (AlignGroup)m_bs_groups.CurrentOrDefault();
			if (grp != null)
				m_bs_patterns.DataSource = grp.Patterns;

			m_grid_patterns.Refresh();
		}

		/// <summary>Display an instance of the edit pattern dialog</summary>
		private void ShowEditPatternDlg(AlignPattern pat)
		{
			if (pat != null && m_edit_windows.TryGetValue(pat, out var dlg))
			{
				dlg.Visible = true;
				dlg.BringToFront();
				dlg.Focus();
				return;
			}
			if (pat == null)
			{
				pat = new AlignPattern();
				m_bs_patterns.Add(pat);
			}

			dlg = new EditPatternUI(pat)
			{
				StartPosition = FormStartPosition.CenterScreen,
			};
			dlg.Shown += (s,a) =>
			{
				m_edit_windows.Add(pat, dlg);
			};
			dlg.Closed += (s,a) =>
			{
				m_edit_windows.Remove(pat);
				SetPatternsDataSource();
			};
			dlg.Commit += (s,a) =>
			{
				var ui = (PatternUI)s;
				Debug.Assert(!ui.IsNew);
				var list = (BindingList<AlignPattern>)m_bs_patterns.DataSource;
				list.Replace((AlignPattern)ui.Original, (AlignPattern)ui.Pattern);
				dlg.Close();
			};
			dlg.Show();
		}

		/// <summary>Close any edit pattern dialogs that reference 'pat'</summary>
		private void CloseEditPatternDlg(AlignPattern pat)
		{
			// If the deleted row is currently being edited, close the editor first
			if (m_edit_windows.TryGetValue(pat, out var dlg))
				dlg.Close();
		}

		/// <summary>Display a help dialog</summary>
		private void ShowHelp()
		{
			if (_help_ui == null)
			{
				var text = new Gui.WinForms.RichTextBox
				{
					Multiline = true,
					Dock = DockStyle.Fill,
					ReadOnly = true,
					Rtf = Resources.help,
					BackColor = SystemColors.Control,
				};
				_help_ui = new Form
				{
					Text = "Alignment Help",
					ShowIcon = false,
					BackColor = SystemColors.Control,
					Padding = new Padding(8),
					Size = new Size(520, 400),
					ShowInTaskbar = false,
					StartPosition = FormStartPosition.CenterParent,
				};
				_help_ui.FormClosed += (s, a) =>
				{
					Util.Dispose(ref _help_ui);
				};
				_help_ui.Controls.Add(text);
			}
			_help_ui.Show(TopLevelControl);
		}
		private Form _help_ui;

		/// <summary>Show the donate button</summary>
		private void ShowDonate()
		{
			using (var dlg = new DonateUI(this))
				dlg.ShowDialog();
		}
		
		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AlignOptionsControl));
			this.m_split = new System.Windows.Forms.SplitContainer();
			this.m_table1 = new System.Windows.Forms.TableLayoutPanel();
			this.m_grid_groups = new Rylogic.TextAligner.GroupGrid();
			this.m_flow = new System.Windows.Forms.FlowLayoutPanel();
			this.m_btn_new = new System.Windows.Forms.Button();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_btn_del = new System.Windows.Forms.Button();
			this.m_btn_move_up = new System.Windows.Forms.Button();
			this.m_btn_move_down = new System.Windows.Forms.Button();
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_btn_donate = new System.Windows.Forms.Button();
			this.m_btn_help_groups = new System.Windows.Forms.Button();
			this.m_btn_reset = new System.Windows.Forms.Button();
			this.m_lbl_alignment_group = new System.Windows.Forms.Label();
			this.m_table2 = new System.Windows.Forms.TableLayoutPanel();
			this.m_grid_patterns = new Rylogic.TextAligner.PatternGrid();
			this.m_lbl_alignment_patterns = new System.Windows.Forms.Label();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.m_split)).BeginInit();
			this.m_split.Panel1.SuspendLayout();
			this.m_split.Panel2.SuspendLayout();
			this.m_split.SuspendLayout();
			this.m_table1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_groups)).BeginInit();
			this.m_flow.SuspendLayout();
			this.m_panel.SuspendLayout();
			this.m_table2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_patterns)).BeginInit();
			this.SuspendLayout();
			// 
			// m_split
			// 
			this.m_split.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split.Location = new System.Drawing.Point(0, 0);
			this.m_split.Name = "m_split";
			this.m_split.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split.Panel1
			// 
			this.m_split.Panel1.AutoScroll = true;
			this.m_split.Panel1.Controls.Add(this.m_table1);
			// 
			// m_split.Panel2
			// 
			this.m_split.Panel2.Controls.Add(this.m_table2);
			this.m_split.Size = new System.Drawing.Size(655, 563);
			this.m_split.SplitterDistance = 277;
			this.m_split.TabIndex = 1;
			// 
			// m_table1
			// 
			this.m_table1.ColumnCount = 2;
			this.m_table1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.m_table1.Controls.Add(this.m_grid_groups, 0, 1);
			this.m_table1.Controls.Add(this.m_flow, 1, 1);
			this.m_table1.Controls.Add(this.m_panel, 0, 0);
			this.m_table1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table1.Location = new System.Drawing.Point(0, 0);
			this.m_table1.Name = "m_table1";
			this.m_table1.RowCount = 2;
			this.m_table1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table1.Size = new System.Drawing.Size(655, 277);
			this.m_table1.TabIndex = 11;
			// 
			// m_grid_groups
			// 
			this.m_grid_groups.AllowUserToAddRows = false;
			this.m_grid_groups.AllowUserToResizeRows = false;
			this.m_grid_groups.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_groups.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_groups.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_groups.Data = null;
			this.m_grid_groups.Location = new System.Drawing.Point(0, 23);
			this.m_grid_groups.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_groups.MultiSelect = false;
			this.m_grid_groups.Name = "m_grid_groups";
			this.m_grid_groups.RowHeadersWidth = 24;
			this.m_grid_groups.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_groups.Size = new System.Drawing.Size(621, 254);
			this.m_grid_groups.TabIndex = 0;
			this.m_grid_groups.VirtualMode = true;
			// 
			// m_flow
			// 
			this.m_flow.Controls.Add(this.m_btn_new);
			this.m_flow.Controls.Add(this.m_btn_del);
			this.m_flow.Controls.Add(this.m_btn_move_up);
			this.m_flow.Controls.Add(this.m_btn_move_down);
			this.m_flow.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_flow.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.m_flow.Location = new System.Drawing.Point(621, 23);
			this.m_flow.Margin = new System.Windows.Forms.Padding(0);
			this.m_flow.Name = "m_flow";
			this.m_flow.Size = new System.Drawing.Size(34, 254);
			this.m_flow.TabIndex = 1;
			// 
			// m_btn_new
			// 
			this.m_btn_new.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_new.ImageKey = "edit_add.png";
			this.m_btn_new.ImageList = this.m_image_list;
			this.m_btn_new.Location = new System.Drawing.Point(3, 3);
			this.m_btn_new.Name = "m_btn_new";
			this.m_btn_new.Size = new System.Drawing.Size(26, 26);
			this.m_btn_new.TabIndex = 6;
			this.m_btn_new.UseVisualStyleBackColor = true;
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "edit_add.png");
			this.m_image_list.Images.SetKeyName(1, "red_x.png");
			this.m_image_list.Images.SetKeyName(2, "green_up.png");
			this.m_image_list.Images.SetKeyName(3, "green_down.png");
			this.m_image_list.Images.SetKeyName(4, "green_tick.png");
			this.m_image_list.Images.SetKeyName(5, "gray_cross.png");
			this.m_image_list.Images.SetKeyName(6, "pencil.png");
			// 
			// m_btn_del
			// 
			this.m_btn_del.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_del.ImageKey = "red_x.png";
			this.m_btn_del.ImageList = this.m_image_list;
			this.m_btn_del.Location = new System.Drawing.Point(3, 35);
			this.m_btn_del.Name = "m_btn_del";
			this.m_btn_del.Size = new System.Drawing.Size(26, 26);
			this.m_btn_del.TabIndex = 7;
			this.m_btn_del.UseVisualStyleBackColor = true;
			// 
			// m_btn_move_up
			// 
			this.m_btn_move_up.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_move_up.ImageKey = "green_up.png";
			this.m_btn_move_up.ImageList = this.m_image_list;
			this.m_btn_move_up.Location = new System.Drawing.Point(3, 67);
			this.m_btn_move_up.Name = "m_btn_move_up";
			this.m_btn_move_up.Size = new System.Drawing.Size(26, 29);
			this.m_btn_move_up.TabIndex = 8;
			this.m_btn_move_up.UseVisualStyleBackColor = true;
			// 
			// m_btn_move_down
			// 
			this.m_btn_move_down.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_move_down.ImageKey = "green_down.png";
			this.m_btn_move_down.ImageList = this.m_image_list;
			this.m_btn_move_down.Location = new System.Drawing.Point(3, 102);
			this.m_btn_move_down.Name = "m_btn_move_down";
			this.m_btn_move_down.Size = new System.Drawing.Size(26, 30);
			this.m_btn_move_down.TabIndex = 9;
			this.m_btn_move_down.UseVisualStyleBackColor = true;
			// 
			// m_panel
			// 
			this.m_panel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel.Controls.Add(this.m_btn_donate);
			this.m_panel.Controls.Add(this.m_btn_help_groups);
			this.m_panel.Controls.Add(this.m_btn_reset);
			this.m_panel.Controls.Add(this.m_lbl_alignment_group);
			this.m_panel.Location = new System.Drawing.Point(0, 0);
			this.m_panel.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(621, 23);
			this.m_panel.TabIndex = 2;
			// 
			// m_btn_donate
			// 
			this.m_btn_donate.BackColor = System.Drawing.Color.LightGreen;
			this.m_btn_donate.Dock = System.Windows.Forms.DockStyle.Right;
			this.m_btn_donate.Location = new System.Drawing.Point(385, 0);
			this.m_btn_donate.Name = "m_btn_donate";
			this.m_btn_donate.Size = new System.Drawing.Size(66, 23);
			this.m_btn_donate.TabIndex = 11;
			this.m_btn_donate.Text = "Donate";
			this.m_btn_donate.UseVisualStyleBackColor = false;
			// 
			// m_btn_help_groups
			// 
			this.m_btn_help_groups.Dock = System.Windows.Forms.DockStyle.Right;
			this.m_btn_help_groups.Location = new System.Drawing.Point(451, 0);
			this.m_btn_help_groups.Name = "m_btn_help_groups";
			this.m_btn_help_groups.Size = new System.Drawing.Size(66, 23);
			this.m_btn_help_groups.TabIndex = 5;
			this.m_btn_help_groups.Text = "Help";
			this.m_btn_help_groups.UseVisualStyleBackColor = true;
			// 
			// m_btn_reset
			// 
			this.m_btn_reset.Dock = System.Windows.Forms.DockStyle.Right;
			this.m_btn_reset.Location = new System.Drawing.Point(517, 0);
			this.m_btn_reset.Name = "m_btn_reset";
			this.m_btn_reset.Size = new System.Drawing.Size(104, 23);
			this.m_btn_reset.TabIndex = 10;
			this.m_btn_reset.Text = "Reset to Defaults";
			this.m_btn_reset.UseVisualStyleBackColor = true;
			// 
			// m_lbl_alignment_group
			// 
			this.m_lbl_alignment_group.Dock = System.Windows.Forms.DockStyle.Left;
			this.m_lbl_alignment_group.Location = new System.Drawing.Point(0, 0);
			this.m_lbl_alignment_group.Name = "m_lbl_alignment_group";
			this.m_lbl_alignment_group.Size = new System.Drawing.Size(93, 23);
			this.m_lbl_alignment_group.TabIndex = 2;
			this.m_lbl_alignment_group.Text = "Alignment Groups:";
			this.m_lbl_alignment_group.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// m_table2
			// 
			this.m_table2.ColumnCount = 1;
			this.m_table2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table2.Controls.Add(this.m_grid_patterns, 0, 1);
			this.m_table2.Controls.Add(this.m_lbl_alignment_patterns, 0, 0);
			this.m_table2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table2.Location = new System.Drawing.Point(0, 0);
			this.m_table2.Name = "m_table2";
			this.m_table2.RowCount = 2;
			this.m_table2.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table2.Size = new System.Drawing.Size(655, 282);
			this.m_table2.TabIndex = 7;
			// 
			// m_grid_patterns
			// 
			this.m_grid_patterns.AllowUserToResizeRows = false;
			this.m_grid_patterns.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_patterns.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_patterns.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_patterns.Location = new System.Drawing.Point(0, 21);
			this.m_grid_patterns.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_patterns.MultiSelect = false;
			this.m_grid_patterns.Name = "m_grid_patterns";
			this.m_grid_patterns.RowHeadersWidth = 24;
			this.m_grid_patterns.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_patterns.Size = new System.Drawing.Size(655, 261);
			this.m_grid_patterns.TabIndex = 0;
			this.m_grid_patterns.VirtualMode = true;
			// 
			// m_lbl_alignment_patterns
			// 
			this.m_lbl_alignment_patterns.Location = new System.Drawing.Point(3, 0);
			this.m_lbl_alignment_patterns.Name = "m_lbl_alignment_patterns";
			this.m_lbl_alignment_patterns.Size = new System.Drawing.Size(98, 21);
			this.m_lbl_alignment_patterns.TabIndex = 3;
			this.m_lbl_alignment_patterns.Text = "Alignment Patterns:";
			this.m_lbl_alignment_patterns.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// AlignOptionsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.Controls.Add(this.m_split);
			this.Name = "AlignOptionsControl";
			this.Size = new System.Drawing.Size(655, 563);
			this.m_split.Panel1.ResumeLayout(false);
			this.m_split.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split)).EndInit();
			this.m_split.ResumeLayout(false);
			this.m_table1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_groups)).EndInit();
			this.m_flow.ResumeLayout(false);
			this.m_panel.ResumeLayout(false);
			this.m_table2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_patterns)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
