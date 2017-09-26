using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;

namespace CoinFlip
{
	public class GridFishing :GridBase
	{
		private DragDrop m_dd;
		public GridFishing(Model model, string title, string name)
			:base(model, title, name)
		{
			m_dd = new DragDrop(this);
			m_dd.DoDrop += DataGridView_.DragDrop_DoDropMoveRow;

			AllowDrop = true;
			RowHeadersVisible = true;
			RowHeadersWidth = 20;
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Fishing.PairName),
				HeaderText = "Pair",
				DataPropertyName = nameof(Fishing.PairName),
				FillWeight = 0.7f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Fishing.ExchName0),
				HeaderText = "Ref Exchange",
				DataPropertyName = nameof(Fishing.ExchName0),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Fishing.ExchName1),
				HeaderText = "Target Exchange",
				DataPropertyName = nameof(Fishing.ExchName1),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewImageColumn
			{
				Name = nameof(Fishing.Active),
				HeaderText = "Active",
				DataPropertyName = nameof(Fishing.Active),
				FillWeight = 0.4f,
			});
			ContextMenuStrip = CreateCMenu();
			DataSource = Model.Fishing;
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			DataGridView_.DragDrop_DragRow(this, e);
		}
		protected override void OnCellClick(DataGridViewCellEventArgs a)
		{
			base.OnCellClick(a);
			if (!this.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col))
				return;

			var fisher = Model.Fishing[a.RowIndex];
			switch (col.DataPropertyName) {
			case nameof(Fishing.Active):
				{
					fisher.Active = !fisher.Active;
					break;
				}
			}
		}
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			base.OnCellFormatting(e);
			if (!this.Within(e.ColumnIndex, e.RowIndex, out DataGridViewColumn col))
				return;

			var fisher = Model.Fishing[e.RowIndex];
			switch (col.DataPropertyName) {
			case nameof(Fishing.Active):
				{
					e.Value = fisher.Active ? Res.Active : Res.Inactive;
					break;
				}
			}

			e.CellStyle.ForeColor = Color.Black;
			e.CellStyle.BackColor = fisher.Settings.Valid ? Color.White : Color.LightSalmon;
			e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
			e.CellStyle.SelectionBackColor = e.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
		}

		/// <summary>Create the context menu for the grid</summary>
		private ContextMenuStrip CreateCMenu()
		{
			var cmenu = new ContextMenuStrip();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Go Fish"));
				opt.Click += (s,a) =>
				{
					using (var dlg = new EditFishingUI(Model))
					{
						if (dlg.ShowDialog(this) != DialogResult.OK) return;
						Model.Fishing.Add(new Fishing(Model, dlg.FishingData));
					}
				};
			}
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Delete"));
				cmenu.Opening += (s,a) =>
				{
					var doomed = SelectedRows.Cast<DataGridViewRow>().Select(x => (Fishing)x.DataBoundItem).ToHashSet();
					opt.Enabled = doomed.Count != 0 && doomed.All(x => !x.Active);
				};
				opt.Click += (s,a) =>
				{
					var doomed = SelectedRows.Cast<DataGridViewRow>().Select(x => (Fishing)x.DataBoundItem).ToHashSet();
					Model.Fishing.RemoveAll(doomed);
					Util.DisposeAll(doomed);
				};
			}
			{
				cmenu.Items.AddSeparator();
			}
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Monitor"));
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = SelectedRows.Count == 1;
				};
				opt.Click += (s,a) =>
				{
					// Add the details UI to the same position as the log
					var fisher = SelectedRows.Cast<DataGridViewRow>().Select(x => (Fishing)x.DataBoundItem).First();
					var logui = DockControl.DockContainer.AllContent.FirstOrDefault(x => x is LogUI ui && ui.Name == "Log");
					var pane = logui?.DockControl.DockPane;
					if (pane != null)
						pane.Content.Add(fisher.DetailsUI.DockControl);
					else
						DockControl.DockContainer.Add(fisher.DetailsUI);
				};
			}
			{
				cmenu.Items.AddSeparator();
			}
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Properties"));
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = SelectedRows.Count == 1;
				};
				opt.Click += (s,a) =>
				{
					var fisher = (Fishing)SelectedRows[0].DataBoundItem;
					using (var dlg = new EditFishingUI(Model, fisher.Settings, fisher.Active))
					{
						if (dlg.ShowDialog(this) != DialogResult.OK) return;
						fisher.Settings = dlg.FishingData;
						Model.Fishing.ResetItem(fisher);
					}
				};
			}
			return cmenu;
		}
	}
}
