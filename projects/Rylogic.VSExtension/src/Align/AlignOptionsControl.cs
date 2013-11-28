using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;
using Rylogic.VSExtension.Properties;

namespace Rylogic.VSExtension
{
	internal class AlignOptionsControl :UserControl
	{
		private enum EGroupColumns
		{
			Name,
		}
		private enum EPatternColumns
		{
			Active,
			Pattern,
			Offset,
			Edit
		}

		private readonly AlignOptions m_options;
		private readonly BindingSource m_bs_groups;
		private readonly BindingSource m_bs_patterns;
		private readonly Dictionary<AlignPattern, EditPatternDlg> m_edit_windows;
		private readonly HoverScroll m_hover_scroll;
		private readonly HintBalloon m_hint_balloon;
		private readonly DragDrop m_dd;
		private SplitContainer m_split;
		private DataGridView m_grid_groups;
		private Label m_lbl_alignment_group;
		private Label m_lbl_alignment_patterns;
		private Button m_btn_help_groups;
		private Button m_btn_help_patterns;
		private DataGridView m_grid_patterns;

		public AlignOptionsControl(AlignOptions options)
		{
			m_options = options;
			m_bs_groups = new BindingSource{DataSource = m_options.Groups};
			m_bs_patterns = new BindingSource{DataSource = null};
			m_edit_windows = new Dictionary<AlignPattern,EditPatternDlg>();
			m_hover_scroll = new HoverScroll();
			m_hint_balloon = new HintBalloon{PreferredTipCorner = HintBalloon.ETipCorner.BottomRight, ShowDelay = 0};
			m_dd = new DragDrop();

			InitializeComponent();

			SetupGroupsGrid();
			SetupPatternsGrid();
			SetPatternsDataSource();

			Load += (s,a) =>
				{
					if (ParentForm != null)
						m_hover_scroll.WindowHandles.Add(ParentForm.Handle);
				};
		}
		protected override void Dispose(bool disposing)
		{
			m_bs_groups.Dispose();
			m_bs_patterns.Dispose();
			m_hover_scroll.Dispose();
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
			m_grid_groups.AutoGenerateColumns = false;
			m_grid_groups.Columns.Add(new DataGridViewTextBoxColumn{HeaderText = EGroupColumns.Name.ToStringFast(), DataPropertyName = Reflect<AlignGroup>.MemberName(x => x.Name)});
			m_grid_groups.DataSource = m_bs_groups;

			// Setup drag/drop to reorder the groups list
			m_dd.DoDrop += DataGridViewExtensions.DragDrop_DoDropMoveRow;
			m_dd.Attach(m_grid_groups);

			m_bs_groups.CurrentItemChanged += (s,a) => SetPatternsDataSource();

			m_btn_help_groups.Click += (s,a) =>
				{
					const string tt =
						"Each group contains patterns that are all considered equivalent. " +
						"Select a group to show its patterns in the table below. " +
						"The order of groups defines the preferred order when looking for " +
						"potential alignment candidates. Change the order by clicking and " +
						"dragging the row header.";
					m_hint_balloon.Show(m_grid_groups, m_grid_groups.ClientRectangle.TopLeft().Shifted(10,10), tt, 10000);
				};

			m_hover_scroll.WindowHandles.Add(m_grid_groups.Handle);
		}

		/// <summary>Setup the grid of patterns</summary>
		private void SetupPatternsGrid()
		{
			m_grid_patterns.AllowDrop = true;
			m_grid_patterns.VirtualMode = true;
			m_grid_patterns.AutoGenerateColumns = false;
			m_grid_patterns.AllowUserToAddRows = false;
			m_grid_patterns.Columns.Add(new DataGridViewImageColumn   {Tag = EPatternColumns.Active  ,HeaderText = EPatternColumns.Active .ToStringFast() ,FillWeight = 25  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});
			m_grid_patterns.Columns.Add(new DataGridViewTextBoxColumn {Tag = EPatternColumns.Pattern ,HeaderText = EPatternColumns.Pattern.ToStringFast() ,FillWeight = 100 ,ReadOnly = true });
			m_grid_patterns.Columns.Add(new DataGridViewTextBoxColumn {Tag = EPatternColumns.Offset  ,HeaderText = EPatternColumns.Offset .ToStringFast() ,FillWeight = 20 });
			m_grid_patterns.Columns.Add(new DataGridViewImageColumn   {Tag = EPatternColumns.Edit    ,HeaderText = EPatternColumns.Edit   .ToStringFast() ,FillWeight = 15  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});

			m_grid_patterns.KeyDown               += DataGridViewExtensions.Copy;
			m_grid_patterns.KeyDown               += DataGridViewExtensions.SelectAll;
			m_grid_patterns.CellValueNeeded       += OnCellValueNeeded;
			m_grid_patterns.CellValidating        += OnCellValidating;
			m_grid_patterns.CellValuePushed       += OnCellValuePushed;
			m_grid_patterns.CellFormatting        += OnCellFormatting;
			m_grid_patterns.CellToolTipTextNeeded += OnCellToolTipNeeded;
			m_grid_patterns.CellClick             += OnCellClick;
			m_grid_patterns.CellDoubleClick       += OnCellDoubleClick;
			m_grid_patterns.MouseDown             += OnMouseDown;
			m_grid_patterns.UserDeletingRow       += OnDeletingRow;
			m_grid_patterns.RowCount               = m_bs_patterns.Count;
			m_grid_patterns.AllowUserToAddRows     = true;

			// Clear the 'X' null image
			foreach (var col in m_grid_patterns.Columns.OfType<DataGridViewImageColumn>())
				col.DefaultCellStyle.NullValue = null;

			// Changes in the patterns data source updates the grid
			m_bs_patterns.ListChanged += (s,a) =>
				{
					//m_options.SaveSettingsToStorage();
					m_grid_patterns.RowCount = m_bs_patterns.Count + (m_grid_patterns.AllowUserToAddRows ? 1 : 0);
					m_grid_patterns.Refresh();
				};

			m_btn_help_patterns.Click += (s,a) =>
				{
					const string tt =
						"A pattern can be a simple substring or a regular expression. " +
						"The 'Offset' column controls the relative position of matching text " +
						"to the other patterns in the group.";
					m_hint_balloon.Show(m_grid_patterns, m_grid_patterns.ClientRectangle.TopLeft().Shifted(10,10), tt, 10000);
				};

			m_hover_scroll.WindowHandles.Add(m_grid_patterns.Handle);
		}

		/// <summary>Update the data source for the patterns grid to be the current group patterns</summary>
		private void SetPatternsDataSource()
		{
			var grp = (AlignGroup)m_bs_groups.CurrentOrDefault();
			m_bs_patterns.DataSource = null;
			if (grp != null)
			{
				m_bs_patterns.DataSource = grp.Patterns;
			}
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

			dlg = new EditPatternDlg(pat);
			dlg.Closed += (s,a) =>
				{
					m_edit_windows.Remove(dlg.Pattern);
				};
			dlg.Commit += (s,a) =>
				{
					var ui = (PatternUI)s;
					var list = (BindingList<AlignPattern>)m_bs_patterns.DataSource;
					if (ui.IsNew) list.Insert(0, (AlignPattern)ui.Pattern);
					else          list.Replace((AlignPattern)ui.Original, (AlignPattern)ui.Pattern);
					m_edit_windows.Remove((AlignPattern)ui.Original);
					dlg.Close();
				};
			dlg.Show(this);
			m_edit_windows.Add(pat, dlg);
		}

		/// <summary>Supplies cell values for the pattern grid</summary>
		private void OnCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex    < 0 || e.RowIndex    >= m_bs_patterns.Count) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
			// Note m_bs_patterns.Count != grid.RowCount, because of the 'insert new row' row

			var pat = (AlignPattern)m_bs_patterns[e.RowIndex];
			switch ((EPatternColumns)grid.Columns[e.ColumnIndex].Tag)
			{
			default:
				e.Value = string.Empty;
				break;
			case EPatternColumns.Pattern:
				var val = pat.ToString();
				if (string.IsNullOrEmpty(val)) val = "<blank>";
				if (string.IsNullOrWhiteSpace(val)) val = "'"+val+"'";
				if (pat.Invert) val = "not " + val;
				e.Value = val;
				break;
			case EPatternColumns.Offset:
				e.Value = pat.Offset;
				break;
			}
		}

		/// <summary>Validates cell data before it is pushed</summary>
		private void OnCellValidating(object sender, DataGridViewCellValidatingEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex    < 0 || e.RowIndex    >= m_bs_patterns.Count) return;
			if (e.RowIndex    < 0 || e.RowIndex    >= grid.RowCount) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;

			switch ((EPatternColumns)grid.Columns[e.ColumnIndex].Tag)
			{
			case EPatternColumns.Offset:
				int ofs;
				if (!int.TryParse(e.FormattedValue.ToString(), out ofs))
				{
					e.Cancel = true;
					grid.Rows[e.RowIndex].ErrorText = "Offset must be a positive or negative integer";
				}
				break;
			}
		}

		/// <summary>Update a cell value after editing</summary>
		private void OnCellValuePushed(object sender, DataGridViewCellValueEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex    < 0 || e.RowIndex    >= m_bs_patterns.Count) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;

			var pat = (AlignPattern)m_bs_patterns[e.RowIndex];

			switch ((EPatternColumns)grid.Columns[e.ColumnIndex].Tag)
			{
			case EPatternColumns.Offset:
				pat.Offset = int.Parse(e.Value.ToString()); break;
			}
		}

		/// <summary>Cell formatting...</summary>
		private void OnCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex    < 0 || e.RowIndex    >= grid.RowCount) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;

			if (e.RowIndex == m_bs_patterns.Count)
			{
				e.Value = null;
			}
			else
			{
				var pat = (AlignPattern)m_bs_patterns[e.RowIndex];
				switch ((EPatternColumns)grid.Columns[e.ColumnIndex].Tag)
				{
				default: return;
				case EPatternColumns.Active:
					e.Value = pat.Active ? Resources.green_tick : Resources.gray_cross;
					break;
				case EPatternColumns.Edit:
					e.Value = Resources.pencil;
					break;
				}
			}
		}

		/// <summary>Cell tool tips</summary>
		private void OnCellToolTipNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex    < 0 || e.RowIndex    >= m_bs_patterns.Count) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;

			var pat = (AlignPattern)m_bs_patterns[e.RowIndex];

			switch ((EPatternColumns)grid.Columns[e.ColumnIndex].Tag)
			{
			default: return;
			case EPatternColumns.Pattern:
				e.ToolTipText= pat.Comment;
				break;
			}
		}

		/// <summary>Handle cell clicks</summary>
		private void OnCellClick(object sender, DataGridViewCellEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex    < 0 || e.RowIndex    >= m_bs_patterns.Count) return;
			if (e.RowIndex    < 0 || e.RowIndex    >= grid.RowCount   ) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;

			var pat = (AlignPattern)m_bs_patterns[e.RowIndex];

			switch ((EPatternColumns)grid.Columns[e.ColumnIndex].Tag)
			{
			default: return;
			case EPatternColumns.Active:
				pat.Active = !pat.Active;
				break;
			case EPatternColumns.Edit:
				ShowEditPatternDlg(pat);
				break;
			}
		}

		/// <summary>Double click edits the pattern</summary>
		private void OnCellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.RowIndex    < 0 || e.RowIndex    >= grid.RowCount   ) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;

			if ((EPatternColumns)e.ColumnIndex == EPatternColumns.Pattern)
			{
				var pat = e.RowIndex < m_bs_patterns.Count ? (AlignPattern)m_bs_patterns[e.RowIndex] : null;
				ShowEditPatternDlg(pat);
			}
		}

		/// <summary>Handle mouse down on the patterns grid</summary>
		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			var grid = (DataGridView)sender;
			var hit = grid.HitTest(e.X, e.Y);
			if (hit.RowIndex    < 0 || hit.RowIndex    >= m_bs_patterns.Count) return;
			if (hit.RowIndex    < 0 || hit.RowIndex    >= grid.RowCount) return;

			if (e.Button == MouseButtons.Left && hit.Type == DataGridViewHitTestType.RowHeader)
			{
				grid.DoDragDrop(grid.Rows[hit.RowIndex], DragDropEffects.Move);
			}
			if (e.Button == MouseButtons.Right && hit.Type == DataGridViewHitTestType.Cell)
			{
				var pat = (AlignPattern)m_bs_patterns[hit.RowIndex];
				var menu = new ContextMenuStrip();
				{
					var opt = new ToolStripLabel("Set Comment", null, false, (s,a) =>
						{
							var dlg = new PromptForm{Title = "Set Comment", Value = pat.Comment};
							if (dlg.ShowDialog(this) != DialogResult.OK) return;
							pat.Comment = dlg.Value;
						});
					menu.Items.Add(opt);
				}
			}
		}

		/// <summary>Delete a pattern</summary>
		private void OnDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
		{
			// Note, don't update the grid here or it causes an ArgumentOutOfRange exception.
			// Other stuff must be using the grid row that will be deleted.
			var pat = (AlignPattern)m_bs_patterns[e.Row.Index];

			// If the deleted row is currently being edited, close the editor first
			EditPatternDlg dlg;
			if (m_edit_windows.TryGetValue(pat, out dlg))
				dlg.Close();

			m_bs_patterns.RemoveAt(e.Row.Index);
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
			this.m_split = new System.Windows.Forms.SplitContainer();
			this.m_btn_help_groups = new System.Windows.Forms.Button();
			this.m_lbl_alignment_group = new System.Windows.Forms.Label();
			this.m_grid_groups = new System.Windows.Forms.DataGridView();
			this.m_btn_help_patterns = new System.Windows.Forms.Button();
			this.m_lbl_alignment_patterns = new System.Windows.Forms.Label();
			this.m_grid_patterns = new System.Windows.Forms.DataGridView();
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
			this.m_split.Panel1.Controls.Add(this.m_btn_help_groups);
			this.m_split.Panel1.Controls.Add(this.m_lbl_alignment_group);
			this.m_split.Panel1.Controls.Add(this.m_grid_groups);
			//
			// m_split.Panel2
			//
			this.m_split.Panel2.Controls.Add(this.m_btn_help_patterns);
			this.m_split.Panel2.Controls.Add(this.m_lbl_alignment_patterns);
			this.m_split.Panel2.Controls.Add(this.m_grid_patterns);
			this.m_split.Size = new System.Drawing.Size(369, 387);
			this.m_split.SplitterDistance = 192;
			this.m_split.TabIndex = 1;
			//
			// m_btn_help_groups
			//
			this.m_btn_help_groups.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_help_groups.Location = new System.Drawing.Point(350, 0);
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
			// m_grid_groups
			//
			this.m_grid_groups.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_groups.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_groups.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_groups.Location = new System.Drawing.Point(0, 20);
			this.m_grid_groups.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_groups.Name = "m_grid_groups";
			this.m_grid_groups.RowHeadersWidth = 24;
			this.m_grid_groups.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_groups.Size = new System.Drawing.Size(369, 172);
			this.m_grid_groups.TabIndex = 0;
			//
			// m_btn_help_patterns
			//
			this.m_btn_help_patterns.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_help_patterns.Location = new System.Drawing.Point(350, 0);
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
			// m_grid_patterns
			//
			this.m_grid_patterns.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_patterns.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_patterns.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_patterns.Location = new System.Drawing.Point(0, 20);
			this.m_grid_patterns.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_patterns.Name = "m_grid_patterns";
			this.m_grid_patterns.RowHeadersWidth = 24;
			this.m_grid_patterns.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_patterns.Size = new System.Drawing.Size(369, 171);
			this.m_grid_patterns.TabIndex = 0;
			//
			// AlignOptionsControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.Controls.Add(this.m_split);
			this.Name = "AlignOptionsControl";
			this.Size = new System.Drawing.Size(369, 387);
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
