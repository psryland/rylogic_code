using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CoinFlip;
using pr.container;
using pr.extn;
using pr.gui;
using pr.util;

namespace Bot.Fishing
{
	public class GridFishing :GridBase
	{
		private DragDrop m_dd;

		public GridFishing(FishFinder ff, string title, string name)
			:base(ff.Model, title, name)
		{
			m_dd = new DragDrop(this);
			m_dd.DoDrop += DataGridView_.DragDrop_DoDropMoveRow;

			AllowDrop = true;
			RowHeadersVisible = true;
			RowHeadersWidth = 20;
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Fisher.PairName),
				HeaderText = "Pair",
				DataPropertyName = nameof(Fisher.PairName),
				FillWeight = 0.7f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Fisher.ExchName0),
				HeaderText = "Ref Exchange",
				DataPropertyName = nameof(Fisher.ExchName0),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Fisher.ExchName1),
				HeaderText = "Target Exchange",
				DataPropertyName = nameof(Fisher.ExchName1),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewImageColumn
			{
				Name = nameof(Fisher.Active),
				HeaderText = "Active",
				DataPropertyName = nameof(Fisher.Active),
				FillWeight = 0.4f,
			});
			ContextMenuStrip = CreateCMenu();
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

			var fisher = Fishers[a.RowIndex];
			switch (col.DataPropertyName) {
			case nameof(Fisher.Active):
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

			var fisher = Fishers[e.RowIndex];
			switch (col.DataPropertyName) {
			case nameof(Fisher.Active):
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

		/// <summary>The bot</summary>
		public FishFinder Bot
		{
			get { return m_bot; }
			private set
			{
				if (m_bot == value) return;
				m_bot = value;
			}
		}
		private FishFinder m_bot;

		/// <summary>Access the source container of fisher instances</summary>
		private BindingSource<Fisher> Fishers
		{
			get { return (BindingSource<Fisher>)DataSource; }
		}

		/// <summary>Create the context menu for the grid</summary>
		private ContextMenuStrip CreateCMenu()
		{
			var cmenu = new ContextMenuStrip();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Go Fish"));
				opt.Click += (s,a) =>
				{
					using (var dlg = new EditFishingUI(Bot))
					{
						if (dlg.ShowDialog(this) != DialogResult.OK) return;
						Fishers.Add(new Fisher(Bot, dlg.FishingData));
					}
				};
			}
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Delete"));
				cmenu.Opening += (s,a) =>
				{
					var doomed = SelectedRows.Cast<DataGridViewRow>().Select(x => (Fisher)x.DataBoundItem).ToHashSet();
					opt.Enabled = doomed.Count != 0 && doomed.All(x => !x.Active);
				};
				opt.Click += (s,a) =>
				{
					var doomed = SelectedRows.Cast<DataGridViewRow>().Select(x => (Fisher)x.DataBoundItem).ToHashSet();
					Fishers.RemoveAll(doomed);
					Util.DisposeAll(doomed);
				};
			}
			cmenu.Items.AddSeparator();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Monitor"));
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = SelectedRows.Count == 1;
				};
				opt.Click += (s,a) =>
				{
					// Add the details UI to the same position as the log
					var fisher = SelectedRows.Cast<DataGridViewRow>().Select(x => (Fisher)x.DataBoundItem).First();
					var logui = DockControl.DockContainer.AllContent.FirstOrDefault(x => x is LogUI ui && ui.Name == "Log");
					var pane = logui?.DockControl.DockPane;
					if (pane != null)
						pane.Content.Add(fisher.DetailsUI.DockControl);
					else
						DockControl.DockContainer.Add(fisher.DetailsUI);
				};
			}
			cmenu.Items.AddSeparator();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Properties"));
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = SelectedRows.Count == 1;
				};
				opt.Click += (s,a) =>
				{
					var fisher = (Fisher)SelectedRows[0].DataBoundItem;
					using (var dlg = new EditFishingUI(Bot, fisher.Settings, fisher.Active))
					{
						if (dlg.ShowDialog(this) != DialogResult.OK) return;
						fisher.Settings = dlg.FishingData;
						Fishers.ResetItem(fisher);
					}
				};
			}
			return cmenu;
		}
	}
}
