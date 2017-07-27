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
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.TimeStamp),
				HeaderText = "Date",
				DataPropertyName = nameof(Position.TimeStamp),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.TradeType),
				HeaderText = "Type",
				DataPropertyName = nameof(Position.TradeType),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.Pair),
				HeaderText = "Pair",
				DataPropertyName = nameof(Position.Pair),
				FillWeight = 0.5f,
				Visible = false,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.Price),
				HeaderText = "Price",
				DataPropertyName = nameof(Position.Price),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.VolumeBase),
				HeaderText = "Volume (Base)",
				DataPropertyName = nameof(Position.VolumeBase),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Position.VolumeQuote),
				HeaderText = "Volume (Quote)",
				DataPropertyName = nameof(Position.VolumeQuote),
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
			DataGridViewEx.ColumnVisibility(this, e);
		}
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs a)
		{
			base.OnCellFormatting(a);
			if (!this.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col, out DataGridViewCell cell))
				return;

			var node  = (TreeGridNode)Rows[a.RowIndex];
			var fill  = node.DataBoundItem as PositionFill;
			var trade = node.DataBoundItem as Position;
			var tt    = fill?.TradeType ?? trade.TradeType;
			var pair  = fill?.Pair      ?? trade.Pair;

			switch (col.DataPropertyName)
			{
			case nameof(PositionFill.OrderId):
				{
					a.Value = fill?.OrderId ?? trade.TradeId;
					break;
				}
			case nameof(Position.TimeStamp):
				{
					a.Value = fill != null
						? fill.Trades.Values.Min(x => x.TimeStamp)
						: trade.TimeStamp;
					break;
				}
			case nameof(Position.TradeType):
				{
					a.Value = "{0}→{1} ({2})".Fmt(
						tt == ETradeType.Q2B ? pair.Quote : pair.Base ,
						tt == ETradeType.Q2B ? pair.Base  : pair.Quote,
						tt);
					a.FormattingApplied = true;
					break;
				}
			case nameof(Position.Pair):
				{
					a.Value = pair.Name;
					a.FormattingApplied = true;
					break;
				}
			case nameof(Position.Price):
				{
					var price = 0m;
					if (fill != null && fill.Trades.Count != 0)
					{
						price = tt == ETradeType.B2Q
							? fill.Trades.Values.Max(x => (decimal)x.Price)
							: fill.Trades.Values.Min(x => (decimal)x.Price);
					}
					else if (trade != null)
					{
						price = trade.Price;
					}

					a.Value = "{0:G8} {1}".Fmt(price, pair.RateUnits);
					a.FormattingApplied = true;
					break;
				}
			case nameof(Position.VolumeBase):
				{
					var vol = fill != null
						? fill.Trades.Values.Sum(x => (decimal)x.VolumeBase)
						: (decimal)trade.VolumeBase;

					a.Value = "{0:G8} {1}".Fmt(vol, pair.Base);
					a.FormattingApplied = true;
					break;
				}
			case nameof(Position.VolumeQuote):
				{
					var vol = fill != null
						? fill.Trades.Values.Sum(x => (decimal)x.VolumeQuote)
						: (decimal)trade.VolumeQuote;

					a.Value = "{0:G8} {1}".Fmt(vol, pair.Quote);
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
