using System.Linq;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	public class GridFishing :GridBase
	{
		public GridFishing(Model model, string title, string name)
			:base(model, title, name)
		{
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Fishing.Pair0),
				HeaderText = "Pair",
				DataPropertyName = nameof(Fishing.Pair0),
				FillWeight = 0.7f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Fishing.Exch0),
				HeaderText = "Ref Exchange",
				DataPropertyName = nameof(Fishing.Exch0),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Fishing.Exch1),
				HeaderText = "Target Exchange",
				DataPropertyName = nameof(Fishing.Exch1),
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
			case nameof(Fishing.Pair0):
				{
					e.Value = fisher.Pair0.Name;
					e.FormattingApplied = true;
					break;
				}
			case nameof(Fishing.Exch0):
				{
					e.Value = fisher.Exch0.Name;
					e.FormattingApplied = true;
					break;
				}
			case nameof(Fishing.Exch1):
				{
					e.Value = fisher.Exch1.Name;
					e.FormattingApplied = true;
					break;
				}
			case nameof(Fishing.Active):
				{
					e.Value = fisher.Active ? Res.Active : Res.Inactive;
					break;
				}
			}
		}

		/// <summary>Create the context menu for the grid</summary>
		private ContextMenuStrip CreateCMenu()
		{
			var cmenu = new ContextMenuStrip();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Go Fish"));
				opt.Click += (s,a) =>
				{
					using (var dlg = new NewFishingUI(Model))
					{
						if (dlg.ShowDialog(this) != DialogResult.OK) return;
						Model.Fishing.Add(dlg.Fishing);
					}
				};
			}
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Delete"));
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = SelectedRows.Count != 0;
				};
				opt.Click += (s,a) =>
				{
					var doomed = SelectedRows.Cast<DataGridViewRow>().Select(x => (Fishing)x.DataBoundItem).ToHashSet();
					Model.Fishing.RemoveAll(doomed);
					Util.DisposeAll(doomed);
				};
			}
			return cmenu;
		}
	}
}
