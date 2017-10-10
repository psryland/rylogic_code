using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public class GridCoins :GridBase
	{
		public GridCoins(Model model, string title, string name)
			:base(model, title, name)
		{
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Currency",
				Name = nameof(Settings.CoinData.Symbol),
				DataPropertyName = nameof(Settings.CoinData.Symbol),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.7f,
				ReadOnly = true,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Value",
				Name = nameof(ColumnNames.Value),
				DataPropertyName = nameof(ColumnNames.Value),
				DefaultCellStyle = new DataGridViewCellStyle{ Format = "C" },
				FillWeight = 0.8f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Available",
				Name = nameof(ColumnNames.Available),
				DataPropertyName = nameof(ColumnNames.Available),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Total",
				Name = nameof(ColumnNames.Total),
				DataPropertyName = nameof(ColumnNames.Total),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Balance",
				Name = nameof(ColumnNames.Balance),
				DataPropertyName = nameof(ColumnNames.Balance),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewImageColumn
			{
				HeaderText = "Flip",
				Name = nameof(Settings.CoinData.OfInterest),
				DataPropertyName = nameof(Settings.CoinData.OfInterest),
				FillWeight = 0.4f,
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
			case nameof(Settings.CoinData.OfInterest):
				{
					cd.OfInterest = !cd.OfInterest;
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
			case nameof(ColumnNames.Value):
				{
					// Find the maximum price for the available exchanges
					a.Value =
						Model.Settings.ShowLivePrices && Model.LivePriceAvailable(cd.Symbol)
						? Model.MaxLiveValue(cd.Symbol).ToString("C")
						: "unknown";
					a.FormattingApplied = true;
					break;
				}
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
			case nameof(ColumnNames.Balance):
				{
					var value = Model.MaxLiveValue(cd.Symbol);
					var total = Model.SumOfTotal(cd.Symbol);
					a.Value = (total * value).ToString("C");
					a.FormattingApplied = true;
					break;
				}
			case nameof(Settings.CoinData.OfInterest):
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
					using (var prompt = new PromptUI { Title = "Currency Symbol", PromptText = string.Empty })
					{
						if (prompt.ShowDialog(this) != DialogResult.OK) return;
						var sym = ((string)prompt.Value).ToUpperInvariant();
						Model.Coins.Add(new Settings.CoinData(sym, 1m));
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
					var doomed = SelectedRows.Cast<DataGridViewRow>().Select(x => (Settings.CoinData)x.DataBoundItem).ToHashSet();
					Model.Coins.RemoveAll(doomed);
				};
			}
			{
				cmenu.Items.AddSeparator();
			}
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Live Value Conversion"));
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = SelectedRows.Count == 1;
				};
				opt.Click += (s,a) =>
				{
					var meta = SelectedRows.Cast<DataGridViewRow>().Select(x => (Settings.CoinData)x.DataBoundItem).First();
					using (var dlg = new PromptUI{ Title = "Live Price Conversion", PromptText = "Set the symbols used to convert to a live price value (comma separated)", Value = meta.LivePriceSymbols })
					{
						if (dlg.ShowDialog(this) != DialogResult.OK) return;
						meta.LivePriceSymbols = (string)dlg.Value;
					}
				};
			}
			return cmenu;
		}

		public static class ColumnNames
		{
			public const int Value = 0;
			public const int Total = 0;
			public const int Available = 0;
			public const int Balance = 0;
		}
	}
}
