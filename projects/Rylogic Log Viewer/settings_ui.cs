using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using Rylogic_Log_Viewer.Properties;

namespace Rylogic_Log_Viewer
{
	internal partial class SettingsUI :Form
	{
		public enum ETab
		{
			General    = 0,
			Highlights = 1,
			Filters    = 2,
		}
		
		private readonly Settings        m_settings;    // The app settings changed by this UI
		private readonly List<Highlight> m_highlights;  // The highlight patterns
		private readonly List<Filter>    m_filters;     // The filter patterns
		public SettingsUI(ETab tab)
		{
			InitializeComponent();
			m_settings = new Settings();
			m_highlights = Highlight.Import(m_settings.HighlightPatterns);
			m_filters = Filter.Import(m_settings.FilterPatterns);
			m_settings.PropertyChanged += (s,a) => UpdateUI();
			m_tabctrl.SelectedIndex = (int)tab;
			m_pattern_hl.NewPattern(new Highlight());
			m_pattern_ft.NewPattern(new Filter());
			
			// General
			m_check_save_screen_loc.CheckedChanged += (s,a)=>
				{
					m_settings.RestoreScreenLoc = m_check_save_screen_loc.Checked;
				};
			m_check_load_last_file.CheckedChanged += (s,a)=>
				{
					m_settings.LoadLastFile = m_check_load_last_file.Checked;
				};
			m_lbl_selection_example.MouseClick += (s,a)=>
				{
					PickColours(m_lbl_selection_example, a.X, a.Y,
						(o,e) => m_settings.LineSelectForeColour = PickColour(m_settings.LineSelectForeColour),
						(o,e) => m_settings.LineSelectBackColour = PickColour(m_settings.LineSelectBackColour));
				};
			m_lbl_line1_example.MouseClick += (s,a)=>
				{
					PickColours(m_lbl_line1_example, a.X, a.Y,
						(o,e) => m_settings.LineForeColour1 = PickColour(m_settings.LineForeColour1),
						(o,e) => m_settings.LineBackColour1 = PickColour(m_settings.LineBackColour1));
				};
			m_lbl_line2_example.MouseClick += (s,a)=>
				{
					PickColours(m_lbl_line2_example, a.X, a.Y,
						(o,e) => m_settings.LineForeColour2 = PickColour(m_settings.LineForeColour2),
						(o,e) => m_settings.LineBackColour2 = PickColour(m_settings.LineBackColour2));
				};
			m_check_alternate_line_colour.CheckedChanged += (s,a)=>
				{
					m_settings.AlternateLineColours = m_check_alternate_line_colour.Checked;
				};
			
			// Highlights
			m_pattern_hl.Add += (s,a)=>
				{
					Highlight hl = (Highlight)m_pattern_hl.Pattern.Clone();
					m_highlights.Add(hl);
					UpdateUI();
				};
			var hl_style = new DataGridViewCellStyle
			{
				Font      = m_settings.Font,
				ForeColor = m_settings.LineForeColour1,
				BackColor = m_settings.LineBackColour1,
			};
			m_grid_highlight.AllowDrop           = true;
			m_grid_highlight.VirtualMode         = true;
			m_grid_highlight.AutoGenerateColumns = false;
			m_grid_highlight.ColumnCount         = m_grid_highlight.RowCount = 0;
			m_grid_highlight.Columns.Add(new DataGridViewCheckBoxColumn{Name = "Active"       ,HeaderText = Resources.Active       ,FillWeight = 25  });
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = "Pattern"      ,HeaderText = Resources.Pattern      ,FillWeight = 100 ,ReadOnly = true });
			m_grid_highlight.Columns.Add(new DataGridViewCheckBoxColumn{Name = "FullColumn"   ,HeaderText = Resources.BinaryMatch  ,FillWeight = 25  });
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = "Highlighting" ,HeaderText = Resources.Highlighting ,FillWeight = 100 ,ReadOnly = true ,DefaultCellStyle = hl_style});
			m_grid_highlight.Columns.Add(new DataGridViewButtonColumn  {Name = "Modify"       ,HeaderText = Resources.Edit         ,FillWeight = 20  });
			m_grid_highlight.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_highlight.KeyDown          += DataGridView_Extensions.Copy;
			m_grid_highlight.KeyDown          += DataGridView_Extensions.SelectAll;
			m_grid_highlight.UserDeletedRow   += (s,a)=> OnDeleteRow(m_grid_highlight, m_highlights);
			m_grid_highlight.MouseDown        += (s,a)=> OnMouseDown(m_grid_highlight, m_highlights, a);
			m_grid_highlight.DragOver         += (s,a)=> DoDragDrop(m_grid_highlight, m_highlights, a, false);
			//m_grid_highlight.SelectionChanged += (s,a)=> OnShowSelected(m_grid_highlight, m_highlights, m_pattern_hl);
			m_grid_highlight.CellValueNeeded  += (s,a)=> OnCellValueNeeded(m_grid_highlight, m_highlights, a);
			m_grid_highlight.MouseClick       += (s,a)=> OnMouseClick(m_grid_highlight, m_highlights, m_pattern_hl, a);
			
			// Filters
			m_pattern_ft.Add += (s,a)=>
				{
					Filter ft = (Filter)m_pattern_ft.Pattern.Clone();
					m_filters.Add(ft);
					UpdateUI();
				};
			m_grid_filter.AllowDrop           = true;
			m_grid_filter.VirtualMode         = true;
			m_grid_filter.AutoGenerateColumns = false;
			m_grid_filter.ColumnCount         = m_grid_filter.RowCount = 0;
			m_grid_filter.Columns.Add(new DataGridViewCheckBoxColumn{Name = "Active"       ,HeaderText = Resources.Active       ,FillWeight = 25  });
			m_grid_filter.Columns.Add(new DataGridViewTextBoxColumn {Name = "Pattern"      ,HeaderText = Resources.Pattern      ,FillWeight = 100 ,ReadOnly = true });
			m_grid_filter.Columns.Add(new DataGridViewButtonColumn  {Name = "Modify"       ,HeaderText = Resources.Edit         ,FillWeight = 20  });
			m_grid_filter.ClipboardCopyMode   = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_filter.KeyDown            += DataGridView_Extensions.Copy;
			m_grid_filter.KeyDown            += DataGridView_Extensions.SelectAll;
			m_grid_filter.UserDeletedRow     += (s,a)=> OnDeleteRow(m_grid_filter, m_filters);
			m_grid_filter.MouseDown          += (s,a)=> OnMouseDown(m_grid_filter, m_filters, a);
			m_grid_filter.DragOver           += (s,a)=> DoDragDrop(m_grid_filter, m_filters, a, false);
			//m_grid_filter.SelectionChanged   += (s,a)=> OnShowSelected(m_grid_filter, m_filters, m_pattern_ft);
			m_grid_filter.CellValueNeeded    += (s,a)=> OnCellValueNeeded(m_grid_filter, m_filters, a);
			m_grid_filter.MouseClick         += (s,a)=> OnMouseClick(m_grid_filter, m_filters, m_pattern_ft, a);
			
			// Save on close
			Closed += (s,a) =>
			{
				m_settings.HighlightPatterns = Highlight.Export(m_highlights);
				m_settings.FilterPatterns = Filter.Export(m_filters);
				m_settings.Save();
			};
			
			UpdateUI();
		}

		/// <summary>Delete a pattern</summary>
		private static void OnDeleteRow<T>(DataGridView grid, List<T> patterns)
		{
			if (grid.RowCount == 0) patterns.Clear();
			else
			{
				int selected = grid.FirstSelectedRowIndex();
				if (selected != -1) patterns.RemoveAt(selected);
			}
		}

		/// <summary>DragDrop functionality for grid rows</summary>
		private static void DoDragDrop<T>(DataGridView grid, List<T> patterns, DragEventArgs args, bool test_can_drop)
		{
			args.Effect = DragDropEffects.None;
			if (!args.Data.GetDataPresent(typeof(T))) return;
			Point pt = grid.PointToClient(new Point(args.X, args.Y));
			var hit = grid.HitTest(pt.X, pt.Y);
			if (hit.Type != DataGridViewHitTestType.RowHeader || hit.RowIndex < 0 || hit.RowIndex >= patterns.Count) return;
			args.Effect = args.AllowedEffect;
			if (test_can_drop) return;
			
			// Swap the rows
			T pat = (T)args.Data.GetData(typeof(T));
			int idx1 = patterns.IndexOf(pat);
			int idx2 = hit.RowIndex;
			T tmp = patterns[idx1];
			patterns[idx1] = patterns[idx2];
			patterns[idx2] = tmp;
			grid.InvalidateRow(idx1);
			grid.InvalidateRow(idx2);
		}

		/// <summary>Handle mouse down on the patterns grid</summary>
		private static void OnMouseDown<T>(DataGridView grid, List<T> patterns, MouseEventArgs e)
		{
			var hit = grid.HitTest(e.X, e.Y);
			if (hit.Type == DataGridViewHitTestType.RowHeader && hit.RowIndex >= 0 && hit.RowIndex < patterns.Count)
			{
				grid.DoDragDrop(patterns[hit.RowIndex], DragDropEffects.Move);
			}
		}

		///// <summary>Show the currently selected highlight in the pattern control</summary>
		//private static void OnShowSelected<T>(DataGridView grid, List<T> patterns, PatternUI ctrl) where T:Pattern
		//{
		//    if (grid.SelectedRows.Count == 0) return;
		//    int row = grid.SelectedRows[0].Index;
		//    ctrl.Pattern = patterns[row];
		//}

		/// <summary>Provide cells for the grid</summary>
		private static void OnCellValueNeeded<T>(DataGridView grid, List<T> patterns, DataGridViewCellValueEventArgs e) where T:Pattern
		{
			if (e.RowIndex < 0 || e.RowIndex >= patterns.Count) { e.Value = ""; return; }
			var cell = grid[e.ColumnIndex, e.RowIndex];
			T pat = patterns[e.RowIndex];
			Highlight hl = pat as Highlight;
			
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: e.Value = ""; break;//throw new ApplicationException("Unknown highlight grid column");
			case "Active": e.Value = pat.Active; break;
			case "Pattern": e.Value = pat.Expr; break;
			case "FullColumn":
				e.Value = hl != null ? hl.FullColumn : false;
				break;
			case "Highlighting":
				e.Value = "Click here to modify highlight";
				cell.Style.BackColor = cell.Style.SelectionBackColor = hl != null ? hl.BackColour : Color.White;
				cell.Style.ForeColor = cell.Style.SelectionForeColor = hl != null ? hl.ForeColour : Color.White;
				break;
			case "Modify":
				e.Value = "...";
				break;
			}
		}

		/// <summary>Handle clicks on the highlights grid</summary>
		private void OnMouseClick<T>(DataGridView grid, List<T> patterns, PatternUI ctrl, MouseEventArgs e) where T:Pattern
		{
			var hit = grid.HitTest(e.X, e.Y);
			if (hit.Type == DataGridViewHitTestType.Cell)
			{
				var cell = grid[hit.ColumnIndex, hit.RowIndex];
				T pat = patterns[hit.RowIndex];
				Highlight hl = pat as Highlight;
				
				switch (grid.Columns[hit.ColumnIndex].Name)
				{
				default: return;
				case "Active":
					pat.Active = (bool)cell.Value == false;
					grid.EndEdit();
					break;
				case "Pattern":
					ctrl.EditPattern(pat);
					break;
				case "FullColumn":
					if (hl != null)
					{
						hl.FullColumn = (bool)cell.Value == false;
						grid.EndEdit();
					}
					break;
				case "Highlighting":
					if (hl != null)
					{
						PickColours(grid, e.X, e.Y,
							(s,a) => { hl.ForeColour = PickColour(hl.ForeColour); grid.InvalidateCell(cell); },
							(s,a) => { hl.BackColour = PickColour(hl.BackColour); grid.InvalidateCell(cell); });
					}
					break;
				}
				UpdateUI();
			}
		}
		
		/// <summary>Helper for create a text colour pick menu</summary>
		private static void PickColours(Control ctrl, int x, int y, EventHandler pick_fore, EventHandler pick_back)
		{
			var menu = new ContextMenuStrip();
			menu.Items.Add(new ToolStripMenuItem(Resources.ChangeForeColour, null, pick_fore));
			menu.Items.Add(new ToolStripMenuItem(Resources.ChangeBackColour, null, pick_back));
			menu.Show(ctrl, x, y);
		}

		
		/// <summary>Update the UI state based on current settings</summary>
		private void UpdateUI()
		{
			SuspendLayout();
			m_check_alternate_line_colour.Checked = m_settings.AlternateLineColours;
			m_lbl_selection_example.BackColor = m_settings.LineSelectBackColour;
			m_lbl_selection_example.ForeColor = m_settings.LineSelectForeColour;
			m_lbl_line1_example.BackColor = m_settings.LineBackColour1;
			m_lbl_line1_example.ForeColor = m_settings.LineForeColour1;
			m_lbl_line2_example.BackColor = m_settings.LineBackColour2;
			m_lbl_line2_example.ForeColor = m_settings.LineForeColour2;
			m_lbl_line2_colours.Enabled = m_settings.AlternateLineColours;
			m_lbl_line2_example.Enabled = m_settings.AlternateLineColours;
			
			int selected = m_grid_highlight.FirstSelectedRowIndex();
			m_grid_highlight.RowCount = m_highlights.Count;
			m_grid_highlight.SelectRow(selected);
			
			selected = m_grid_filter.FirstSelectedRowIndex();
			m_grid_filter.RowCount = m_filters.Count;
			m_grid_filter.SelectRow(selected);
			
			ResumeLayout();
		}

		/// <summary>Colour picker helper</summary>
		private Color PickColour(Color current)
		{
			var d = new ColorDialog{AllowFullOpen = true, AnyColor = true, Color = current};
			return d.ShowDialog(this) == DialogResult.OK ? d.Color : current;
		}
	}
}
