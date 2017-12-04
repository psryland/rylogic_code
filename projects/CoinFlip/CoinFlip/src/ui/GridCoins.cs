using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;

namespace CoinFlip
{
	public class GridCoins :GridBase
	{
		public GridCoins(Model model, string title, string name)
			:base(model, title, name)
		{
			AllowDrop = true;
			RowHeadersVisible = true;
			RowHeadersWidth = 16;
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
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Auto Trade Limit",
				Name = nameof(ColumnNames.AutoTradeLimit),
				DataPropertyName = nameof(ColumnNames.AutoTradeLimit),
				FillWeight = 0.5f,
				Visible = false,
			});
			Columns.Add(new DataGridViewImageColumn
			{
				HeaderText = "Flip",
				Name = nameof(Settings.CoinData.OfInterest),
				DataPropertyName = nameof(Settings.CoinData.OfInterest),
				FillWeight = 0.4f,
			});
			DataSource = Model.Coins;

			var dd = new DragDrop(this);
			dd.DoDrop += DataGridView_.DragDrop_DoDropMoveRow;
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			DataGridView_.DragDrop_DragRow(this, e);
		}
		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);

			var hit = this.HitTestEx(e.Location);
			if (e.Button == MouseButtons.Left)
			{
				if (hit.Type == DataGridView_.HitTestInfo.EType.Cell &&
					hit.Column.DataPropertyName == nameof(Settings.CoinData.OfInterest))
				{
					// Toggle 'OfInterest' for the clicked coin
					var cd = Model.Coins[hit.RowIndex];
					cd.OfInterest = !cd.OfInterest;
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				// Display a context menu
				if (hit.Type == DataGridView_.HitTestInfo.EType.ColumnHeader)
				{
					this.ColumnVisibilityContextMenu(e.Location);
				}
				else if (hit.Type == DataGridView_.HitTestInfo.EType.Cell)
				{
					var cmenu = new ContextMenuStrip();
					{// Add Coin
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
					{// Delete Coin
						var opt = cmenu.Items.Add2(new ToolStripMenuItem("Delete"));
						cmenu.Opening += (s,a) =>
						{
							opt.Enabled = this.SelectedRowCount(1) != 0;
						};
						opt.Click += (s,a) =>
						{
							var doomed = SelectedRows.Cast<DataGridViewRow>().Select(x => (Settings.CoinData)x.DataBoundItem).ToHashSet();
							Model.Coins.RemoveAll(doomed);
						};
					}
					cmenu.Items.AddSeparator();
					{// Live value
						var opt = cmenu.Items.Add2(new ToolStripMenuItem("Live Value Conversion"));
						opt.Enabled = SelectedRows.Count == 1;
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
					cmenu.Show(this, e.Location);
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
						Model.Settings.ShowLivePrices == false ? cd.AssignedValue.ToString("C") :
						Model.LivePriceAvailable(cd.Symbol) ? Model.MaxLiveValue(cd.Symbol).ToString("C") :
						"---";
					a.FormattingApplied = true;
					break;
				}
			case nameof(ColumnNames.Available):
				{
					a.Value = Model.SumOfAvailable(cd.Symbol).ToString("F8");
					a.FormattingApplied = true;
					break;
				}
			case nameof(ColumnNames.Total):
				{
					a.Value = Model.SumOfTotal(cd.Symbol).ToString("F8");
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
			case nameof(ColumnNames.AutoTradeLimit):
				{
					a.Value = cd.AutoTradingLimit.ToString("G");
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

		public static class ColumnNames
		{
			public const int Currency       = 0;
			public const int Value          = 1;
			public const int Available      = 2;
			public const int Total          = 3;
			public const int Balance        = 4;
			public const int AutoTradeLimit = 5;
			public const int Flip           = 6;
		}
	}
}
