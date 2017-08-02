using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.extn;
using pr.gui;

namespace CoinFlip
{
	public class GridCoins :GridBase
	{
		public GridCoins(Model model, string title, string name)
			:base(model, title, name)
		{
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(CoinData.Symbol),
				HeaderText = "Currency",
				DataPropertyName = nameof(CoinData.Symbol),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.5f,
				ReadOnly = true,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(CoinData.Value),
				HeaderText = "Value",
				DataPropertyName = nameof(CoinData.Value),
				FillWeight = 0.6f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(ColumnNames.Available),
				HeaderText = "Available",
				DataPropertyName = nameof(ColumnNames.Available),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(ColumnNames.Total),
				HeaderText = "Total",
				DataPropertyName = nameof(ColumnNames.Total),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewImageColumn
			{
				Name = nameof(CoinData.OfInterest),
				HeaderText = "Flip",
				DataPropertyName = nameof(CoinData.OfInterest),
				FillWeight = 0.3f,
			});
			ContextMenuStrip = CreateCMenu();
			DataSource = Model.Coins;
		}
		protected override void OnCellClick(DataGridViewCellEventArgs a)
		{
			base.OnCellClick(a);
			if (!this.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col))
				return;

			var cd = Model.Coins[a.RowIndex];
			switch (col.DataPropertyName) {
			case nameof(CoinData.OfInterest):
				{
					cd.OfInterest = !cd.OfInterest;
					Model.RebuildLoops = true;
					break;
				}
			}
		}
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs a)
		{
			base.OnCellFormatting(a);
			if (!this.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col))
				return;

			var cd = Model.Coins[a.RowIndex];
			switch (col.DataPropertyName) {
			case nameof(ColumnNames.Available):
				{
					a.Value = Model.SumOfAvailable(cd.Symbol).ToString();
					a.FormattingApplied = true;
					break;
				}
			case nameof(ColumnNames.Total):
				{
					a.Value = Model.SumOfTotal(cd.Symbol).ToString();
					a.FormattingApplied = true;
					break;
				}
			case nameof(CoinData.OfInterest):
				{
					a.Value = cd.OfInterest ? Res.Active : Res.Inactive;
					break;
				}
			}

			a.CellStyle.BackColor = Color.White;
			a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
			a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
		}

		/// <summary>Create the context menu for the grid</summary>
		private ContextMenuStrip CreateCMenu()
		{
			var cmenu = new ContextMenuStrip();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Add Coin"));
				opt.Click += (s,a) =>
				{
					using (var prompt = new PromptForm { Title = "Currency Symbol", PromptText = string.Empty })
					{
						if (prompt.ShowDialog(this) != DialogResult.OK) return;
						var sym = prompt.Value.ToUpperInvariant();
						Model.Coins.Add(new CoinData(sym, 1m));
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
					var doomed = SelectedRows.Cast<DataGridViewRow>().Select(x => (CoinData)x.DataBoundItem).ToHashSet();
					Model.Coins.RemoveAll(doomed);
				};
			}
			return cmenu;
		}

		public static class ColumnNames
		{
			public const int Total = 0;
			public const int Available = 0;
		}
	}
}
