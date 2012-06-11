using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.maths;
using pr.util;
using RyLogViewer.Properties;

namespace RyLogViewer
{
	public partial class SettingsUI :Form
	{
		private readonly Settings        m_settings;    // The app settings changed by this UI
		private readonly List<Highlight> m_highlights;  // The highlight patterns currently in the grid
		private readonly List<Filter>    m_filters;     // The filter patterns currently in the grid
		private readonly ToolTip         m_tt;          // Tooltips
		
		public enum ETab
		{
			General    = 0,
			Highlights = 1,
			Filters    = 2,
		}
		private static class ColumnNames
		{
			public const string Active       = "Active";
			public const string Pattern      = "Pattern";
			public const string Modify       = "Modify";
			public const string Highlighting = "Highlighting";
		}
		
		/// <summary>Returns a bit mask of the settings data that's changed</summary>
		public EWhatsChanged WhatsChanged { get; private set; }
		
		public SettingsUI(ETab tab)
		{
			InitializeComponent();
			m_settings    = new Settings();
			m_highlights  = Highlight.Import(m_settings.HighlightPatterns);
			m_filters     = Filter.Import(m_settings.FilterPatterns);
			m_tt          = new ToolTip();
			
			m_tabctrl.SelectedIndex = (int)tab;
			m_pattern_hl.NewPattern(new Highlight());
			m_pattern_ft.NewPattern(new Filter());
			m_settings.SettingChanged += (s,a) => UpdateUI();
			
			SetupGeneralTab();
			SetupLogViewTab();
			SetupHighlightTab();
			SetupFilterTab();
			
			// Save on close
			Closed += (s,a) =>
			{
				Focus(); // grab focus to ensure all controls persist their state
				m_settings.HighlightPatterns = Highlight.Export(m_highlights);
				m_settings.FilterPatterns    = Filter   .Export(m_filters);
				m_settings.Save();
			};
			
			UpdateUI();
			WhatsChanged = EWhatsChanged.Nothing;
		}
		
		/// <summary>Hook up events for the general tab</summary>
		private void SetupGeneralTab()
		{
			// Load last file on startup
			m_check_load_last_file.ToolTip(m_tt, "Automatically load the last loaded file on startup");
			m_check_load_last_file.CheckedChanged += (s,a)=>
				{
					m_settings.LoadLastFile = m_check_load_last_file.Checked;
					WhatsChanged |= EWhatsChanged.StartupOptions;
				};
			
			// Restore window position on startup
			m_check_save_screen_loc.ToolTip(m_tt, "Restore the window to its last position on startup");
			m_check_save_screen_loc.CheckedChanged += (s,a)=>
				{
					m_settings.RestoreScreenLoc = m_check_save_screen_loc.Checked;
					WhatsChanged |= EWhatsChanged.StartupOptions;
				};
			
			// Show tip of the day on startup
			m_check_show_totd.ToolTip(m_tt, "Show the 'Tip of the Day' dialog on startup");
			m_check_show_totd.CheckedChanged += (s,a)=>
				{
					m_settings.ShowTOTD = m_check_show_totd.Checked;
					WhatsChanged |= EWhatsChanged.StartupOptions;
				};

			// Line endings
			m_edit_line_ends.ToolTip(m_tt, "Set the line ending characters to expect in the log data.\r\nUse 'CR' for carriage return, 'LF' for line feed.\r\nLeave blank to auto detect");
			m_edit_line_ends.Text = m_settings.RowDelimiter;
			m_edit_line_ends.TextChanged += (s,a)=>
			{
				m_settings.RowDelimiter = m_edit_line_ends.Text;
				WhatsChanged |= EWhatsChanged.FileParsing;
			};
			
			// Column delimiters
			m_edit_col_delims.ToolTip(m_tt, "Set the characters that separate columns in the log data.\r\nUse 'TAB' for a tab character.\r\nLeave blank for no column delimiters");
			m_edit_col_delims.Text = m_settings.ColDelimiter;
			m_edit_col_delims.TextChanged += (s,a)=>
				{
					m_settings.ColDelimiter = m_edit_col_delims.Text;
					WhatsChanged |= EWhatsChanged.FileParsing;
				};
			
			// Include blank lines
			m_check_ignore_blank_lines.ToolTip(m_tt, "Ignore blank lines when loading the log file");
			m_check_ignore_blank_lines.Checked = m_settings.IgnoreBlankLines;
			m_check_ignore_blank_lines.CheckedChanged += (s,a)=>
				{
					m_settings.IgnoreBlankLines = m_check_ignore_blank_lines.Checked;
					WhatsChanged |= EWhatsChanged.FileParsing;
				};
			
			// Open at end
			m_check_open_at_end.ToolTip(m_tt, "If checked, opens files showing the end of the file.\r\nIf unchecked opens files at the beginning");
			m_check_open_at_end.Checked = m_settings.OpenAtEnd;
			m_check_open_at_end.CheckedChanged += (s,a)=>
				{
					m_settings.OpenAtEnd = m_check_open_at_end.Checked;
					WhatsChanged |= EWhatsChanged.FileOpenOptions;
				};
			
			// File changes additive
			m_check_file_changes_additive.ToolTip(m_tt, "Assume all changes to the watched file are additive only\r\nIf checked, reloading of changed files will not invalidate existing cached data");
			m_check_file_changes_additive.Checked = m_settings.FileChangesAdditive;
			m_check_file_changes_additive.CheckedChanged += (s,a)=>
				{
					m_settings.FileChangesAdditive = m_check_file_changes_additive.Checked;
					WhatsChanged |= EWhatsChanged.FileParsing;
				};


			// File buf size
			m_spinner_file_buf_size.ToolTip(m_tt, "The size (in KB) of the cached portion of the log file");
			m_spinner_file_buf_size.Minimum = 1;
			m_spinner_file_buf_size.Maximum = 100000;
			m_spinner_file_buf_size.Value = Maths.Clamp(m_settings.FileBufSize / 1024, (int)m_spinner_file_buf_size.Minimum, (int)m_spinner_file_buf_size.Maximum);
			m_spinner_file_buf_size.ValueChanged += (s,a)=>
				{
					m_settings.FileBufSize = (int)m_spinner_file_buf_size.Value * 1024;
					WhatsChanged |= EWhatsChanged.FileParsing;
				};

		}
		
		/// <summary>Hook up events for the log view tab</summary>
		private void SetupLogViewTab()
		{
			// Selection colour
			m_lbl_selection_example.ToolTip(m_tt, "Set the selection foreground and back colours in the log view");
			m_lbl_selection_example.MouseClick += (s,a)=>
				{
					PickColours(m_lbl_selection_example, a.X, a.Y,
						(o,e) => m_settings.LineSelectForeColour = PickColour(m_settings.LineSelectForeColour),
						(o,e) => m_settings.LineSelectBackColour = PickColour(m_settings.LineSelectBackColour));
					WhatsChanged |= EWhatsChanged.Rendering;
				};
			
			// Log text colour
			m_lbl_line1_example.ToolTip(m_tt, "Set the foreground and background colours in the log view");
			m_lbl_line1_example.MouseClick += (s,a)=>
				{
					PickColours(m_lbl_line1_example, a.X, a.Y,
						(o,e) => m_settings.LineForeColour1 = PickColour(m_settings.LineForeColour1),
						(o,e) => m_settings.LineBackColour1 = PickColour(m_settings.LineBackColour1));
					WhatsChanged |= EWhatsChanged.Rendering;
				};
			
			// Alt log text colour
			m_lbl_line2_example.ToolTip(m_tt, "Set the foreground and background colours for odd numbered rows in the log view");
			m_lbl_line2_example.MouseClick += (s,a)=>
				{
					PickColours(m_lbl_line2_example, a.X, a.Y,
						(o,e) => m_settings.LineForeColour2 = PickColour(m_settings.LineForeColour2),
						(o,e) => m_settings.LineBackColour2 = PickColour(m_settings.LineBackColour2));
					WhatsChanged |= EWhatsChanged.Rendering;
				};
			
			// Enable alt line colours
			m_check_alternate_line_colour.ToolTip(m_tt, "Enable alternating colours in the log view");
			m_check_alternate_line_colour.CheckedChanged += (s,a)=>
				{
					m_settings.AlternateLineColours = m_check_alternate_line_colour.Checked;
					WhatsChanged |= EWhatsChanged.Rendering;
				};
			
			// Row height
			m_spinner_row_height.ToolTip(m_tt, "The height of each row in the log view");
			m_spinner_row_height.Minimum = 1;
			m_spinner_row_height.Maximum = 200;
			m_spinner_row_height.Value = Maths.Clamp(m_settings.RowHeight, (int)m_spinner_row_height.Minimum, (int)m_spinner_row_height.Maximum);
			m_spinner_row_height.ValueChanged += (s,a)=>
				{
					m_settings.RowHeight = (int)m_spinner_row_height.Value;
					WhatsChanged |= EWhatsChanged.Rendering;
				};
			
			// File scroll width
			m_spinner_file_scroll_width.ToolTip(m_tt, "The width of the left file position scroll bar");
			m_spinner_file_scroll_width.Minimum = 16;
			m_spinner_file_scroll_width.Maximum = 200;
			m_spinner_file_scroll_width.Value = Maths.Clamp(m_settings.FileScrollWidth, (int)m_spinner_file_scroll_width.Minimum, (int)m_spinner_file_scroll_width.Maximum);
			m_spinner_file_scroll_width.ValueChanged += (s,a)=>
				{
					m_settings.FileScrollWidth = (int)m_spinner_file_scroll_width.Value;
					WhatsChanged |= EWhatsChanged.Rendering;
				};
			
			// Font 
			m_text_font.ToolTip(m_tt, "The font used to display the log file data");
			m_text_font.Text = string.Format("{0}, {1}pt" ,m_settings.Font.Name ,m_settings.Font.Size);
			m_text_font.Font = m_settings.Font;
			
			// Font button
			m_btn_change_font.ToolTip(m_tt, "Change the log view font");
			m_btn_change_font.Click += (s,a)=>
				{
					var dg = new FontDialog{Font = m_settings.Font};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					m_text_font.Font = dg.Font;
					m_settings.Font = dg.Font;
				};
		}
		
		/// <summary>Hook up events for the highlights tab</summary>
		private void SetupHighlightTab()
		{
			var hl_style = new DataGridViewCellStyle
			{
				Font      = m_settings.Font,
				ForeColor = m_settings.LineForeColour1,
				BackColor = m_settings.LineBackColour1,
			};

			// Highlight grid
			m_grid_highlight.AllowDrop           = true;
			m_grid_highlight.VirtualMode         = true;
			m_grid_highlight.AutoGenerateColumns = false;
			m_grid_highlight.ColumnCount         = m_grid_highlight.RowCount = 0;
			m_grid_highlight.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Active       ,HeaderText = Resources.Active       ,FillWeight = 25  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Pattern      ,HeaderText = Resources.Pattern      ,FillWeight = 100 ,ReadOnly = true });
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Highlighting ,HeaderText = Resources.Highlighting ,FillWeight = 100 ,ReadOnly = true ,DefaultCellStyle = hl_style});
			m_grid_highlight.Columns.Add(new DataGridViewButtonColumn  {Name = ColumnNames.Modify       ,HeaderText = Resources.Edit         ,FillWeight = 15  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader});
			m_grid_highlight.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_highlight.KeyDown          += DataGridView_Extensions.Copy;
			m_grid_highlight.KeyDown          += DataGridView_Extensions.SelectAll;
			m_grid_highlight.UserDeletedRow   += (s,a)=> OnDeleteRow(m_grid_highlight, m_highlights);
			m_grid_highlight.MouseDown        += (s,a)=> OnMouseDown(m_grid_highlight, m_highlights, a);
			m_grid_highlight.DragOver         += (s,a)=> DoDragDrop(m_grid_highlight, m_highlights, a, false);
			m_grid_highlight.CellValueNeeded  += (s,a)=> OnCellValueNeeded(m_grid_highlight, m_highlights, a);
			m_grid_highlight.CellClick        += (s,a)=> OnCellClick(m_grid_highlight, m_highlights, m_pattern_hl, a);
			m_grid_highlight.CellFormatting   += (s,a)=> OnCellFormatting(m_grid_highlight, m_highlights, a);
			m_grid_highlight.DataError        += (s,a)=> a.Cancel = true;
			
			// Highlight pattern
			m_pattern_hl.Add += (s,a)=>
				{
					WhatsChanged |= EWhatsChanged.Rendering;
					if (m_pattern_hl.IsNew) m_highlights.Add((Highlight)m_pattern_hl.Pattern);
					m_pattern_hl.NewPattern(new Highlight());
					UpdateUI();
				};
			
			// Highlight pattern sets
			m_pattern_set_hl.Init(m_settings, m_highlights);
			m_pattern_set_hl.CurrentSetChanged += (s,a)=>
				{
					UpdateUI();
				};
		}

		/// <summary>Hook up events for the filters tab</summary>
		private void SetupFilterTab()
		{
			m_grid_filter.AllowDrop           = true;
			m_grid_filter.VirtualMode         = true;
			m_grid_filter.AutoGenerateColumns = false;
			m_grid_filter.ColumnCount         = m_grid_filter.RowCount = 0;
			m_grid_filter.Columns.Add(new DataGridViewImageColumn   {Name = ColumnNames.Active  ,HeaderText = Resources.Active  ,FillWeight = 25  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});
			m_grid_filter.Columns.Add(new DataGridViewTextBoxColumn {Name = ColumnNames.Pattern ,HeaderText = Resources.Pattern ,FillWeight = 100 ,ReadOnly = true });
			m_grid_filter.Columns.Add(new DataGridViewButtonColumn  {Name = ColumnNames.Modify  ,HeaderText = Resources.Edit    ,FillWeight = 15  ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader});
			m_grid_filter.ClipboardCopyMode   = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid_filter.KeyDown            += DataGridView_Extensions.Copy;
			m_grid_filter.KeyDown            += DataGridView_Extensions.SelectAll;
			m_grid_filter.UserDeletedRow     += (s,a)=> OnDeleteRow(m_grid_filter, m_filters);
			m_grid_filter.MouseDown          += (s,a)=> OnMouseDown(m_grid_filter, m_filters, a);
			m_grid_filter.DragOver           += (s,a)=> DoDragDrop(m_grid_filter, m_filters, a, false);
			m_grid_filter.CellValueNeeded    += (s,a)=> OnCellValueNeeded(m_grid_filter, m_filters, a);
			m_grid_filter.CellClick          += (s,a)=> OnCellClick(m_grid_filter, m_filters, m_pattern_ft, a);
			m_grid_filter.CellFormatting     += (s,a)=> OnCellFormatting(m_grid_filter, m_filters, a);
			m_grid_filter.DataError          += (s,a)=> a.Cancel = true;
			
			// Filter pattern
			m_pattern_ft.Add += (s,a)=>
				{
					WhatsChanged |= EWhatsChanged.FileParsing;
					if (m_pattern_ft.IsNew) m_filters.Add((Filter)m_pattern_ft.Pattern);
					m_pattern_ft.NewPattern(new Filter());
					UpdateUI();
				};
			
			// Filter pattern sets
			m_pattern_set_ft.Init(m_settings, m_filters);
			m_pattern_set_ft.CurrentSetChanged += (s,a)=>
				{
					UpdateUI();
				};
		}
		
		/// <summary>Delete a pattern</summary>
		private void OnDeleteRow<T>(DataGridView grid, List<T> patterns)
		{
			WhatsChanged |= grid == m_grid_highlight
				? EWhatsChanged.Rendering
				: EWhatsChanged.FileParsing;
			
			if (grid.RowCount == 0) patterns.Clear();
			else
			{
				int selected = grid.FirstSelectedRowIndex();
				if (selected != -1) patterns.RemoveAt(selected);
			}
		}

		/// <summary>DragDrop functionality for grid rows</summary>
		private void DoDragDrop<T>(DataGridView grid, List<T> patterns, DragEventArgs args, bool test_can_drop)
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
			
			// Changing the order can effect the highlight order, and possibly the filtering(?)
			WhatsChanged |= grid == m_grid_highlight
				? EWhatsChanged.Rendering
				: EWhatsChanged.FileParsing;
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

		/// <summary>Provide cells for the grid</summary>
		private static void OnCellValueNeeded<T>(DataGridView grid, List<T> patterns, DataGridViewCellValueEventArgs e) where T:Pattern
		{
			if (e.RowIndex < 0 || e.RowIndex >= patterns.Count) { e.Value = ""; return; }
			var cell = grid[e.ColumnIndex, e.RowIndex];
			T pat = patterns[e.RowIndex];
			Highlight hl = pat as Highlight;
			
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: e.Value = string.Empty; break;
			case ColumnNames.Pattern:
				e.Value = pat.Expr;
				break;
			case ColumnNames.Highlighting:
				e.Value = Resources.ClickToModifyHighlight;
				cell.Style.BackColor = cell.Style.SelectionBackColor = hl != null ? hl.BackColour : Color.White;
				cell.Style.ForeColor = cell.Style.SelectionForeColor = hl != null ? hl.ForeColour : Color.White;
				break;
			case ColumnNames.Modify:
				e.Value = "...";
				break;
			}
		}

		/// <summary>Handle cell clicks</summary>
		private void OnCellClick<T>(DataGridView grid, List<T> patterns, PatternUI ctrl, DataGridViewCellEventArgs e) where T:Pattern
		{
			if (e.RowIndex    < 0 || e.RowIndex    >= grid.RowCount   ) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
			T pat = patterns[e.RowIndex];
			Highlight hl = pat as Highlight;
			
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: return;
			case ColumnNames.Active:
				pat.Active = !pat.Active;
				WhatsChanged |= grid == m_grid_highlight
					? EWhatsChanged.Rendering
					: EWhatsChanged.FileParsing;
				break;
			case ColumnNames.Highlighting:
				if (hl != null)
				{
					WhatsChanged |= EWhatsChanged.Rendering;
					PickColours(grid, MousePosition.X, MousePosition.Y,
						(s,a) => { hl.ForeColour = PickColour(hl.ForeColour); grid.InvalidateCell(e.ColumnIndex, e.RowIndex); },
						(s,a) => { hl.BackColour = PickColour(hl.BackColour); grid.InvalidateCell(e.ColumnIndex, e.RowIndex); });
				}
				break;
			case ColumnNames.Modify:
				ctrl.EditPattern(patterns[e.RowIndex]);
				WhatsChanged |= grid == m_grid_highlight
					? EWhatsChanged.Rendering
					: EWhatsChanged.FileParsing;
				break;
			}
		}
		
		/// <summary>Cell formatting...</summary>
		private static void OnCellFormatting<T>(DataGridView grid, List<T> patterns, DataGridViewCellFormattingEventArgs e) where T:Pattern
		{
			if (e.RowIndex    < 0 || e.RowIndex    >= grid.RowCount   ) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
			T pat = patterns[e.RowIndex];
			
			switch (grid.Columns[e.ColumnIndex].Name)
			{
			default: return;
			case ColumnNames.Active:
				e.Value = pat.Active ? Resources.pattern_active : Resources.pattern_inactive;
				break;
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
			m_lbl_line2_example.Enabled = m_settings.AlternateLineColours;
			
			int selected = m_grid_highlight.FirstSelectedRowIndex();
			m_grid_highlight.CurrentCell = null;
			m_grid_highlight.RowCount = 0;
			m_grid_highlight.RowCount = m_highlights.Count;
			m_grid_highlight.SelectRow(selected);
			
			selected = m_grid_filter.FirstSelectedRowIndex();
			m_grid_filter.CurrentCell = null;
			m_grid_filter.RowCount = 0;
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
