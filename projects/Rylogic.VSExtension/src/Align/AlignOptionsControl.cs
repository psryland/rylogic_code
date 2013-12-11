using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using pr.extn;
using pr.gui;

namespace Rylogic.VSExtension
{
	internal class AlignOptionsControl :UserControl
	{
		private readonly AlignOptions m_options;
		private readonly BindingSource m_bs_groups;
		private readonly BindingSource m_bs_patterns;
		private readonly Dictionary<AlignPattern, EditPatternDlg> m_edit_windows;
		private readonly HintBalloon m_hint_balloon;
		private readonly ToolTip m_tt;
		private SplitContainer m_split;
		private GroupGrid m_grid_groups;
		private Label m_lbl_alignment_group;
		private Label m_lbl_alignment_patterns;
		private Button m_btn_help_groups;
		private Button m_btn_new;
		private Button m_btn_move_down;
		private ImageList m_image_list;
		private Button m_btn_move_up;
		private Button m_btn_del;
		private Button m_btn_help_patterns;
		private Button m_btn_reset;
		private PatternGrid m_grid_patterns;

		public AlignOptionsControl(AlignOptions options)
		{
			InitializeComponent();

			m_options = options;
			m_bs_groups = new BindingSource{DataSource = m_options.Groups};
			m_bs_patterns = new BindingSource{DataSource = null};
			m_edit_windows = new Dictionary<AlignPattern,EditPatternDlg>();
			m_hint_balloon = new HintBalloon{ShowDelay = 0, PreferredTipCorner = HintBalloon.ETipCorner.BottomLeft};
			m_tt = new ToolTip();

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

			SetupGroupsGrid();
			SetupPatternsGrid();
			SetPatternsDataSource();
		}
		protected override void Dispose(bool disposing)
		{
			m_bs_groups.Dispose();
			m_bs_patterns.Dispose();
			m_hint_balloon.Dispose();

			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>Setup the grid of alignment groups</summary>
		private void SetupGroupsGrid()
		{
			m_grid_groups.Data = m_bs_groups;

			// When the selected item in the groups grid changes, update the data source of the pattern grid
			m_bs_groups.PositionChanged += (s,a) =>
				{
					SetPatternsDataSource();
					UpdateButtonStates();
				};

			// Setup add/remove/order buttons
			m_btn_new.ToolTip(m_tt, "Add a new alignment group");
			m_btn_new.Click += (s,a) =>
				{
					m_bs_groups.AddNew();
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
					m_grid_groups.SelectRow(x - 1);
				};
			m_btn_move_down.ToolTip(m_tt, "Move the selected alignment group down in the priority order");
			m_btn_move_down.Click += (s,a) =>
				{
					var x = m_bs_groups.Position;
					if (x >= m_bs_groups.Count - 1) return;
					m_bs_groups.List.Swap(x, x+1);
					m_grid_groups.SelectRow(x + 1);
				};

			// Setup the help button for groups
			m_btn_help_groups.Click += (s,a) =>
				{
					const string tt1 =
						"Each group contains patterns that are all considered equivalent. " +
						"Select a group to show its patterns in the table below. " +
						"The order of groups defines the preferred order when looking for " +
						"potential alignment candidates. Change the order by selecting a group " +
						"and clicking the up/down arrows. 'LeadingSpace' is the number of white " +
						"space characters added in front of the aligned text.";
					m_hint_balloon.Show(m_grid_groups, m_grid_groups.ClientRectangle.TopLeft().Shifted(10,10), tt1, 10000);
				};

			// Setup reset button
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

			UpdateButtonStates();
		}

		/// <summary>Setup the grid of patterns</summary>
		private void SetupPatternsGrid()
		{
			m_grid_patterns.Data                = m_bs_patterns;
			m_grid_patterns.ShowEditPatternDlg  = ShowEditPatternDlg;
			m_grid_patterns.CloseEditPatternDlg = CloseEditPatternDlg;

			// Setup the help button for patterns
			m_btn_help_patterns.Click += (s,a) =>
				{
					const string tt2 =
						"A pattern can be a simple substring or a regular expression. " +
						"The 'Offset' column controls the relative position of matching text " +
						"to the other patterns in the group. The 'MinWidth' column controls " +
						"how much extra padding is added after the matching text.";
					m_hint_balloon.Show(m_grid_patterns, m_grid_patterns.ClientRectangle.TopLeft().Shifted(10,10), tt2, 10000);
				};
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
			EditPatternDlg dlg;
			if (pat != null && m_edit_windows.TryGetValue(pat, out dlg))
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

			dlg = new EditPatternDlg(pat);
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
			dlg.Show(this);
		}

		/// <summary>Close any edit pattern dialogs that reference 'pat'</summary>
		private void CloseEditPatternDlg(AlignPattern pat)
		{
			// If the deleted row is currently being edited, close the editor first
			EditPatternDlg dlg;
			if (m_edit_windows.TryGetValue(pat, out dlg))
				dlg.Close();
		}

		#region Component Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AlignOptionsControl));
			this.m_split = new System.Windows.Forms.SplitContainer();
			this.m_btn_reset = new System.Windows.Forms.Button();
			this.m_btn_move_down = new System.Windows.Forms.Button();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_btn_move_up = new System.Windows.Forms.Button();
			this.m_btn_del = new System.Windows.Forms.Button();
			this.m_btn_new = new System.Windows.Forms.Button();
			this.m_btn_help_groups = new System.Windows.Forms.Button();
			this.m_lbl_alignment_group = new System.Windows.Forms.Label();
			this.m_btn_help_patterns = new System.Windows.Forms.Button();
			this.m_lbl_alignment_patterns = new System.Windows.Forms.Label();
			this.m_grid_groups = new Rylogic.VSExtension.GroupGrid();
			this.m_grid_patterns = new Rylogic.VSExtension.PatternGrid();
			((System.ComponentModel.ISupportInitialize)(this.m_split)).BeginInit();
			this.m_split.Panel1.SuspendLayout();
			this.m_split.Panel2.SuspendLayout();
			this.m_split.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_groups)).BeginInit();
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
			this.m_split.Panel1.Controls.Add(this.m_btn_reset);
			this.m_split.Panel1.Controls.Add(this.m_btn_move_down);
			this.m_split.Panel1.Controls.Add(this.m_btn_move_up);
			this.m_split.Panel1.Controls.Add(this.m_btn_del);
			this.m_split.Panel1.Controls.Add(this.m_btn_new);
			this.m_split.Panel1.Controls.Add(this.m_btn_help_groups);
			this.m_split.Panel1.Controls.Add(this.m_lbl_alignment_group);
			this.m_split.Panel1.Controls.Add(this.m_grid_groups);
			//
			// m_split.Panel2
			//
			this.m_split.Panel2.Controls.Add(this.m_btn_help_patterns);
			this.m_split.Panel2.Controls.Add(this.m_lbl_alignment_patterns);
			this.m_split.Panel2.Controls.Add(this.m_grid_patterns);
			this.m_split.Size = new System.Drawing.Size(445, 288);
			this.m_split.SplitterDistance = 142;
			this.m_split.TabIndex = 1;
			//
			// m_btn_reset
			//
			this.m_btn_reset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_reset.Location = new System.Drawing.Point(312, -1);
			this.m_btn_reset.Name = "m_btn_reset";
			this.m_btn_reset.Size = new System.Drawing.Size(104, 20);
			this.m_btn_reset.TabIndex = 10;
			this.m_btn_reset.Text = "Reset to Defaults";
			this.m_btn_reset.UseVisualStyleBackColor = true;
			//
			// m_btn_move_down
			//
			this.m_btn_move_down.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_move_down.ImageKey = "green_down.png";
			this.m_btn_move_down.ImageList = this.m_image_list;
			this.m_btn_move_down.Location = new System.Drawing.Point(419, 98);
			this.m_btn_move_down.Name = "m_btn_move_down";
			this.m_btn_move_down.Size = new System.Drawing.Size(26, 26);
			this.m_btn_move_down.TabIndex = 9;
			this.m_btn_move_down.UseVisualStyleBackColor = true;
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
			// m_btn_move_up
			//
			this.m_btn_move_up.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_move_up.ImageKey = "green_up.png";
			this.m_btn_move_up.ImageList = this.m_image_list;
			this.m_btn_move_up.Location = new System.Drawing.Point(419, 72);
			this.m_btn_move_up.Name = "m_btn_move_up";
			this.m_btn_move_up.Size = new System.Drawing.Size(26, 26);
			this.m_btn_move_up.TabIndex = 8;
			this.m_btn_move_up.UseVisualStyleBackColor = true;
			//
			// m_btn_del
			//
			this.m_btn_del.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_del.ImageKey = "red_x.png";
			this.m_btn_del.ImageList = this.m_image_list;
			this.m_btn_del.Location = new System.Drawing.Point(419, 46);
			this.m_btn_del.Name = "m_btn_del";
			this.m_btn_del.Size = new System.Drawing.Size(26, 26);
			this.m_btn_del.TabIndex = 7;
			this.m_btn_del.UseVisualStyleBackColor = true;
			//
			// m_btn_new
			//
			this.m_btn_new.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_new.ImageKey = "edit_add.png";
			this.m_btn_new.ImageList = this.m_image_list;
			this.m_btn_new.Location = new System.Drawing.Point(419, 20);
			this.m_btn_new.Name = "m_btn_new";
			this.m_btn_new.Size = new System.Drawing.Size(26, 26);
			this.m_btn_new.TabIndex = 6;
			this.m_btn_new.UseVisualStyleBackColor = true;
			//
			// m_btn_help_groups
			//
			this.m_btn_help_groups.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_help_groups.Location = new System.Drawing.Point(425, -1);
			this.m_btn_help_groups.Name = "m_btn_help_groups";
			this.m_btn_help_groups.Size = new System.Drawing.Size(20, 20);
			this.m_btn_help_groups.TabIndex = 5;
			this.m_btn_help_groups.Text = "?";
			this.m_btn_help_groups.UseVisualStyleBackColor = true;
			//
			// m_lbl_alignment_group
			//
			this.m_lbl_alignment_group.AutoSize = true;
			this.m_lbl_alignment_group.Location = new System.Drawing.Point(0, 3);
			this.m_lbl_alignment_group.Name = "m_lbl_alignment_group";
			this.m_lbl_alignment_group.Size = new System.Drawing.Size(93, 13);
			this.m_lbl_alignment_group.TabIndex = 2;
			this.m_lbl_alignment_group.Text = "Alignment Groups:";
			//
			// m_btn_help_patterns
			//
			this.m_btn_help_patterns.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_help_patterns.Location = new System.Drawing.Point(425, -1);
			this.m_btn_help_patterns.Name = "m_btn_help_patterns";
			this.m_btn_help_patterns.Size = new System.Drawing.Size(20, 20);
			this.m_btn_help_patterns.TabIndex = 6;
			this.m_btn_help_patterns.Text = "?";
			this.m_btn_help_patterns.UseVisualStyleBackColor = true;
			//
			// m_lbl_alignment_patterns
			//
			this.m_lbl_alignment_patterns.AutoSize = true;
			this.m_lbl_alignment_patterns.Location = new System.Drawing.Point(0, 3);
			this.m_lbl_alignment_patterns.Name = "m_lbl_alignment_patterns";
			this.m_lbl_alignment_patterns.Size = new System.Drawing.Size(98, 13);
			this.m_lbl_alignment_patterns.TabIndex = 3;
			this.m_lbl_alignment_patterns.Text = "Alignment Patterns:";
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
			this.m_grid_groups.Location = new System.Drawing.Point(0, 22);
			this.m_grid_groups.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_groups.MultiSelect = false;
			this.m_grid_groups.Name = "m_grid_groups";
			this.m_grid_groups.RowHeadersWidth = 24;
			this.m_grid_groups.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_groups.Size = new System.Drawing.Size(416, 120);
			this.m_grid_groups.TabIndex = 0;
			this.m_grid_groups.VirtualMode = true;
			//
			// m_grid_patterns
			//
			this.m_grid_patterns.AllowUserToAddRows = false;
			this.m_grid_patterns.AllowUserToResizeRows = false;
			this.m_grid_patterns.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_patterns.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_patterns.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_patterns.Location = new System.Drawing.Point(0, 20);
			this.m_grid_patterns.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_patterns.MultiSelect = false;
			this.m_grid_patterns.Name = "m_grid_patterns";
			this.m_grid_patterns.RowHeadersWidth = 24;
			this.m_grid_patterns.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_patterns.Size = new System.Drawing.Size(445, 122);
			this.m_grid_patterns.TabIndex = 0;
			this.m_grid_patterns.VirtualMode = true;
			//
			// AlignOptionsControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.Controls.Add(this.m_split);
			this.Name = "AlignOptionsControl";
			this.Size = new System.Drawing.Size(445, 288);
			this.m_split.Panel1.ResumeLayout(false);
			this.m_split.Panel1.PerformLayout();
			this.m_split.Panel2.ResumeLayout(false);
			this.m_split.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split)).EndInit();
			this.m_split.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_groups)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_patterns)).EndInit();
			this.ResumeLayout(false);
		}

		#endregion
	}
}
