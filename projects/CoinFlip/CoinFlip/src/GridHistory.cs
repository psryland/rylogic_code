using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.gui;

namespace CoinFlip
{
	public class GridHistory :TreeBase
	{
		public GridHistory(Model model, string title, string name)
			:base(model, title, name)
		{
			ReadOnly = true;
			Columns.Add(new TreeGridColumn
			{
				Name = nameof(PositionFill.OrderId),
				HeaderText = "Order/Trade Id",
				DataPropertyName = nameof(PositionFill.OrderId),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.6f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Historic.Created),
				HeaderText = "Date",
				DataPropertyName = nameof(Historic.Created),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Historic.TradeType),
				HeaderText = "Type",
				DataPropertyName = nameof(Historic.TradeType),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Historic.Pair),
				HeaderText = "Pair",
				DataPropertyName = nameof(Historic.Pair),
				FillWeight = 0.5f,
				Visible = false,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Historic.PriceQ2B),
				HeaderText = "Price",
				DataPropertyName = nameof(Historic.PriceQ2B),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Historic.VolumeIn),
				HeaderText = "Volume In",
				DataPropertyName = nameof(Historic.VolumeIn),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Historic.VolumeOut),
				HeaderText = "Volume Out",
				DataPropertyName = nameof(Historic.VolumeOut),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Historic.VolumeNett),
				HeaderText = "Volume Nett",
				DataPropertyName = nameof(Historic.VolumeNett),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Historic.Commission),
				HeaderText = "Commission",
				DataPropertyName = nameof(Historic.Commission),
				FillWeight = 1.0f,
			});
			DataSource = Model.History;
		}
		protected override void Dispose(bool disposing)
		{
			DataSource = null;
			base.Dispose(disposing);
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			DataGridView_.ColumnVisibility(this, e);
		}
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs a)
		{
			base.OnCellFormatting(a);
			if (!this.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col, out DataGridViewCell cell))
				return;

			var node  = (TreeGridNode)Rows[a.RowIndex];
			var fill  = node.DataBoundItem as PositionFill;
			var trade = node.DataBoundItem as Historic;
			var tt    = fill?.TradeType ?? trade.TradeType;
			var pair  = fill?.Pair      ?? trade.Pair;

			switch (col.DataPropertyName)
			{
			case nameof(PositionFill.OrderId):
				{
					a.Value = fill?.OrderId ?? trade?.TradeId ?? 0UL;
					break;
				}
			case nameof(Historic.Created):
				{
					var ts = fill?.Created ?? trade?.Created ?? DateTimeOffset.MinValue;
					a.Value = ts.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
					a.FormattingApplied = true;
					break;
				}
			case nameof(Historic.TradeType):
				{
					a.Value = "{0}→{1} ({2})".Fmt(
						tt == ETradeType.Q2B ? pair.Quote : pair.Base ,
						tt == ETradeType.Q2B ? pair.Base  : pair.Quote,
						tt);
					a.FormattingApplied = true;
					break;
				}
			case nameof(Historic.Pair):
				{
					a.Value = pair.Name;
					a.FormattingApplied = true;
					break;
				}
			case nameof(Historic.PriceQ2B):
				{
					var price = fill?.PriceQ2B ?? trade?.PriceQ2B ?? 0m;
					a.Value = $"{price.ToString("G8")} {pair.RateUnits}";
					a.FormattingApplied = true;
					break;
				}
			case nameof(Historic.VolumeIn):
				{
					var vol = fill?.VolumeIn ?? trade?.VolumeIn ?? 0m;
					var coin = fill?.CoinIn ?? trade?.CoinIn ?? string.Empty;
					a.Value = $"{vol.ToString("G8")} {coin}";
					a.FormattingApplied = true;
					break;
				}
			case nameof(Historic.VolumeOut):
				{
					var vol = fill?.VolumeOut ?? trade?.VolumeOut ?? 0m;
					var coin = fill?.CoinOut ?? trade?.CoinOut ?? string.Empty;
					a.Value = $"{vol.ToString("G8")} {coin}";
					a.FormattingApplied = true;
					break;
				}
			case nameof(Historic.VolumeNett):
				{
					var vol = fill?.VolumeNett ?? trade?.VolumeNett ?? 0m;
					var coin = fill?.CoinOut ?? trade?.CoinOut ?? string.Empty;
					a.Value = $"{vol.ToString("G8")} {coin}";
					a.FormattingApplied = true;
					break;
				}
			case nameof(Historic.Commission):
				{
					var fees = fill?.Commission ?? trade?.Commission ?? 0m;
					var coin = fill?.CoinOut ?? trade?.CoinOut ?? string.Empty;
					a.Value = $"{fees.ToString("G8")} {coin}";
					a.FormattingApplied = true;
					break;
				}
			}
			a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
			a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
		}

		/// <summary>The data source for the tree</summary>
		public new BindingSource<PositionFill> DataSource
		{
			get { return m_data_source; }
			set
			{
				if (m_data_source == value) return;
				if (m_data_source != null)
				{
					m_data_source.ListChanging -= HandleListChanging;
				}
				m_data_source = value;
				if (m_data_source != null)
				{
					m_data_source.ListChanging += HandleListChanging;
				}
			}
		}
		private BindingSource<PositionFill> m_data_source;

		/// <summary>Update the tree grid when the source data changes</summary>
		private void HandleListChanging(object sender, ListChgEventArgs<PositionFill> e)
		{
			switch (e.ChangeType)
			{
			case ListChg.PreReset:
				{
					Nodes.Clear();
					break;
				}
			case ListChg.Reset:
				{
					foreach (var fill in DataSource)
					{
						var node = Nodes.Bind(fill);
						foreach (var trade in fill.Trades.Values)
							node.Nodes.Bind(trade);
					}
					break;
				}
			case ListChg.ItemAdded:
				{
					var node = Nodes.Bind(e.Item);
					foreach (var trade in e.Item.Trades.Values)
						node.Nodes.Bind(trade);

					break;
				}
			case ListChg.ItemPreRemove:
				{
					Nodes.RemoveIf(x => x.DataBoundItem == e.Item);
					break;
				}
			case ListChg.ItemPreReset:
				{
					var node = Nodes.First(x => x.DataBoundItem == e.Item);
					node.Nodes.Clear();
					break;
				}
			case ListChg.ItemReset:
				{
					var node = Nodes.First(x => x.DataBoundItem == e.Item);
					foreach (var trade in e.Item.Trades.Values)
						node.Nodes.Bind(trade);
					break;
				}
			}
		}
	}
}
