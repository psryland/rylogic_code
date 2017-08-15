using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public class GridPositions :GridBase
	{
		public GridPositions(Model model, string title, string name)
			:base(model, title, name)
		{
			ReadOnly = true;
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.OrderId),
				HeaderText = "Order Id",
				DataPropertyName = nameof(Position.OrderId),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.Created),
				HeaderText = "Date",
				DataPropertyName = nameof(Position.Created),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.TradeType),
				HeaderText = "Type",
				DataPropertyName = nameof(Position.TradeType),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.Pair),
				HeaderText = "Pair",
				DataPropertyName = nameof(Position.Pair),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.5f,
				Visible = false,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.Price),
				HeaderText = "Price",
				DataPropertyName = nameof(Position.Price),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(ColumnNames.LivePrice),
				HeaderText = "Live Price",
				DataPropertyName = nameof(ColumnNames.LivePrice),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(ColumnNames.PriceDist),
				HeaderText = "Distance (Index)",
				DataPropertyName = nameof(ColumnNames.PriceDist),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.VolumeBase),
				HeaderText = "Volume (Base)",
				DataPropertyName = nameof(Position.VolumeBase),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.VolumeQuote),
				HeaderText = "Volume (Quote)",
				DataPropertyName = nameof(Position.VolumeQuote),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.Remaining),
				HeaderText = "Remaining (%)",
				DataPropertyName = nameof(Position.Remaining),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			ContextMenuStrip = CreateCMenu();
			DataSource = Model.Positions;
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			DataGridView_.ColumnVisibility(this, e);
		}
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs a)
		{
			base.OnCellFormatting(a);

			if (!this.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col)) return;
			if (!a.RowIndex.Within(0, Model.Positions.Count)) return;
			var pos = Model.Positions[a.RowIndex];

			switch (col.Name)
			{
			case nameof(Position.Created):
				{
					a.Value = pos.Created?.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
					a.FormattingApplied = true;
					break;
				}
			case nameof(Position.TradeType):
				{
					a.Value = "{0}→{1} ({2})".Fmt(
						pos.TradeType == ETradeType.Q2B ? pos.Pair.Quote : pos.Pair.Base ,
						pos.TradeType == ETradeType.Q2B ? pos.Pair.Base  : pos.Pair.Quote,
						pos.TradeType);
					a.FormattingApplied = true;
					break;
				}
			case nameof(Position.Price):
				{
					a.Value = "{0:G8} {1}".Fmt((decimal)pos.Price, pos.Pair.RateUnits);
					a.FormattingApplied = true;
					break;
				}
			case nameof(ColumnNames.LivePrice):
				{
					a.Value = pos.Pair.CurrentPrice(pos.TradeType).ToString("G8");
					a.FormattingApplied = true;
					break;
				}
			case nameof(ColumnNames.PriceDist):
				{
					a.Value = "{0:G8} ({1})".Fmt(
						pos.TradeType == ETradeType.B2Q ? (decimal)(pos.Price - pos.Pair.CurrentPrice(ETradeType.B2Q)) :
						pos.TradeType == ETradeType.Q2B ? (decimal)(pos.Pair.CurrentPrice(ETradeType.Q2B) - pos.Price) :
						0m,
						pos.Pair.OrderBookIndex(pos.TradeType, pos.Price));
					a.FormattingApplied = true;
					break;
				}
			case nameof(Position.VolumeBase):
				{
					a.Value = "{0:G8} {1}".Fmt((decimal)pos.VolumeBase, pos.Pair.Base);
					a.FormattingApplied = true;
					break;
				}
			case nameof(Position.VolumeQuote):
				{
					a.Value = "{0:G8} {1}".Fmt((decimal)pos.VolumeQuote, pos.Pair.Quote);
					a.FormattingApplied = true;
					break;
				}
			case nameof(Position.Remaining):
				{
					var pc = 100m * Maths.Div((decimal)pos.Remaining, (decimal)pos.VolumeBase, 0m);
					a.Value = "{0:G4} %".Fmt((decimal)pc);
					a.FormattingApplied = true;
					break;
				}
			}
			a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
			a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
		}

		/// <summary>Create the context menu for the grid</summary>
		private ContextMenuStrip CreateCMenu()
		{
			var cmenu = new ContextMenuStrip();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Cancel Order"));
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = SelectedRows.Count == 1;
				};
				opt.Click += async (s,a) =>
				{
					var pos = SelectedRows.Cast<DataGridViewRow>().Select(x => (Position)x.DataBoundItem).First();
					await pos.CancelOrder();
				};
			}
			if (!Model.AllowTrades)
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("\"Fill\" order"));
				cmenu.Opening += (s,a) =>
				{
					var pos = SelectedRows.Cast<DataGridViewRow>().Select(x => (Position)x.DataBoundItem).FirstOrDefault();
					opt.Visible = pos != null && pos.Fake;
				};
				opt.Click += async (s,a) =>
				{
					// Cancel the order and create a fake history entry to simulate the fake order being filled
					var pos = SelectedRows.Cast<DataGridViewRow>().Select(x => (Position)x.DataBoundItem).First();
					await pos.FillFakeOrder();
				};
			}
			return cmenu;
		}

		public static class ColumnNames
		{
			public const int LivePrice = 0;
			public const int PriceDist = 0;
		}
	}
}
