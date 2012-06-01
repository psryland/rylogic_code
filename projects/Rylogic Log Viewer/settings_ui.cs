using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
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
		public SettingsUI(ETab tab)
		{
			InitializeComponent();
			m_settings = new Settings();
			m_highlights = Highlight.Import(m_settings.HighlightPatterns);
			m_settings.PropertyChanged += (s,a) => UpdateUI();
			m_tabctrl.SelectedIndex = (int)tab;
			m_pattern_hl.Pattern = new Highlight();
			
			// General
			m_check_alternate_line_colour.CheckedChanged += (s,a)=>
				{
					m_settings.AlternateLineColours = m_check_alternate_line_colour.Checked;
				};
			m_btn_selection_fore_colour.Click += (s,a)=>
				{
					m_settings.LineSelectForeColour = PickColour(m_settings.LineSelectForeColour);
				};
			m_btn_selection_back_colour.Click += (s,a)=>
				{
					m_settings.LineSelectBackColour = PickColour(m_settings.LineSelectBackColour);
				};
			m_btn_line1_fore_colour.Click += (s,a)=>
				{
					m_settings.LineForeColour1 = PickColour(m_settings.LineForeColour1);
				};
			m_btn_line1_back_colour.Click += (s,a)=>
				{
					m_settings.LineBackColour1 = PickColour(m_settings.LineBackColour1);
				};
			m_btn_line2_fore_colour.Click += (s,a)=>
				{
					m_settings.LineForeColour2 = PickColour(m_settings.LineForeColour2);
				};
			m_btn_line2_back_colour.Click += (s,a)=>
				{
					m_settings.LineBackColour2 = PickColour(m_settings.LineBackColour2);
				};
			
			// Highlights
			var hl_style = new DataGridViewCellStyle
			{
				Font      = m_settings.Font,
				ForeColor = m_settings.LineForeColour1,
				BackColor = m_settings.LineBackColour1,
			};
			m_grid_highlight.AllowDrop = true;
			m_grid_highlight.VirtualMode = true;
			m_grid_highlight.AutoGenerateColumns = false;
			m_grid_highlight.ColumnCount = m_grid_highlight.RowCount = 0;
			m_grid_highlight.Columns.Add(new DataGridViewCheckBoxColumn{Name = "Active"       ,HeaderText = Resources.Active       ,FillWeight = 3  ,DataPropertyName = "Active" });
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = "Pattern"      ,HeaderText = Resources.Pattern      ,FillWeight = 10 ,DataPropertyName = "Expr"   });
			m_grid_highlight.Columns.Add(new DataGridViewTextBoxColumn {Name = "Highlighting" ,HeaderText = Resources.Highlighting ,FillWeight = 10 ,DataPropertyName = "Example" ,ReadOnly = true ,DefaultCellStyle = hl_style});
			m_grid_highlight.CellValueNeeded += HLCellValueNeeded;
			m_grid_highlight.MouseDown  += HLMouseDown;
			m_grid_highlight.MouseClick += HLMouseClick;
			m_grid_highlight.DragOver  += (s,a)=> HLDragDrop(a, false);
			m_grid_highlight.SelectionChanged += (s,a)=> HLShowSelected();;
			m_pattern_hl.Add += (s,a)=>
				{
					Highlight hl = (Highlight)m_pattern_hl.Pattern;
					m_highlights.Add((Highlight)hl.Clone());
					UpdateUI();
				};
			
			// Save on close
			Closed += (s,a) =>
			{
				m_settings.HighlightPatterns = Highlight.Export(m_highlights);
				m_settings.Save();
			};
			
			UpdateUI();
		}

		/// <summary>DragDrop functionality for highlight grid rows</summary>
		private void HLDragDrop(DragEventArgs args, bool test_can_drop)
		{
			args.Effect = DragDropEffects.None;
			if (!args.Data.GetDataPresent(typeof(Highlight))) return;
			Point pt = m_grid_highlight.PointToClient(new Point(args.X, args.Y));
			var hit = m_grid_highlight.HitTest(pt.X, pt.Y);
			if (hit.Type != DataGridViewHitTestType.RowHeader || hit.RowIndex < 0 || hit.RowIndex >= m_highlights.Count) return;
			args.Effect = args.AllowedEffect;
			if (test_can_drop) return;
			
			// Swap the rows
			Highlight hl = (Highlight)args.Data.GetData(typeof(Highlight));
			int idx1 = m_highlights.IndexOf(hl);
			int idx2 = hit.RowIndex;
			Highlight tmp = m_highlights[idx1];
			m_highlights[idx1] = m_highlights[idx2];
			m_highlights[idx2] = tmp;
			m_grid_highlight.InvalidateRow(idx1);
			m_grid_highlight.InvalidateRow(idx2);
		}

		/// <summary>Provide cells for the grid</summary>
		private void HLCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			if (e.RowIndex < 0 || e.RowIndex >= m_highlights.Count) { e.Value = ""; return; }
			var cell = m_grid_highlight[e.ColumnIndex, e.RowIndex];
			Highlight hl = m_highlights[e.RowIndex];
			switch (m_grid_highlight.Columns[e.ColumnIndex].Name)
			{
			default: e.Value = ""; break;//throw new ApplicationException("Unknown highlight grid column");
			case "Active": e.Value = hl.Active; break;
			case "Pattern": e.Value = hl.Expr; break;
			case "Highlighting":
				e.Value = "Click here to modify highlight";
				cell.Style.BackColor = cell.Style.SelectionBackColor = hl.BackColour;
				cell.Style.ForeColor = cell.Style.SelectionForeColor = hl.ForeColour;
				break;
			}
		}

		/// <summary>Handle clicks on the highlights grid</summary>
		private void HLMouseClick(object sender, MouseEventArgs e)
		{
			var hit = m_grid_highlight.HitTest(e.X, e.Y);
			if (hit.Type == DataGridViewHitTestType.Cell)
			{
				var cell = m_grid_highlight[hit.ColumnIndex, hit.RowIndex];
				Highlight hl = m_highlights[hit.RowIndex];
				switch (m_grid_highlight.Columns[hit.ColumnIndex].Name)
				{
				default: return;
				case "Active":
					hl.Active = (bool)cell.Value == false;
					m_grid_highlight.EndEdit();
					break;
				case "Pattern": m_pattern_hl.Pattern = hl; break;
				case "Highlighting":
					{
						var menu = new ContextMenuStrip();
						menu.Items.Add(new ToolStripMenuItem("Change Foregroud Colour", null, (s,a)=>{ hl.ForeColour = PickColour(hl.ForeColour); }));
						menu.Items.Add(new ToolStripMenuItem("Change Background Colour", null, (s,a)=>{ hl.BackColour = PickColour(hl.BackColour); }));
						menu.Show(m_grid_highlight, e.X, e.Y);
						break;
					}
				}
				UpdateUI();
			}
		}
		
		/// <summary>Handle mouse down on the highlights grid</summary>
		private void HLMouseDown(object sender, MouseEventArgs e)
		{
			var hit = m_grid_highlight.HitTest(e.X, e.Y);
			if (hit.Type == DataGridViewHitTestType.RowHeader && hit.RowIndex >= 0 && hit.RowIndex < m_highlights.Count)
			{
				m_grid_highlight.DoDragDrop(m_highlights[hit.RowIndex], DragDropEffects.Move);
			}
		}

		/// <summary>Show the currently selected highlight in the pattern control</summary>
		private void HLShowSelected()
		{
			if (m_grid_highlight.SelectedRows.Count == 0) return;
			int row = m_grid_highlight.SelectedRows[0].Index;
			m_pattern_hl.Pattern = m_highlights[row];
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
			m_btn_line2_fore_colour.Enabled = m_settings.AlternateLineColours;
			m_btn_line2_back_colour.Enabled = m_settings.AlternateLineColours;
			m_lbl_line2_example.Enabled = m_settings.AlternateLineColours;
			
			m_grid_highlight.RowCount = m_highlights.Count;
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
