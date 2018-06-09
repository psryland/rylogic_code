using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	public class GridPositions :GridBase
	{
		private readonly string[] m_dummy_row;
		public GridPositions(Model model, string title, string name)
			:base(model, title, name)
		{
			m_dummy_row = new string[1];
			ReadOnly = true;
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Order Id",
				Name = nameof(Order.OrderId),
				DataPropertyName = nameof(Order.OrderId),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Date",
				Name = nameof(Order.Created),
				DataPropertyName = nameof(Order.Created),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Type",
				Name = nameof(Order.TradeType),
				DataPropertyName = nameof(Order.TradeType),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Pair",
				Name = nameof(Order.Pair),
				DataPropertyName = nameof(Order.Pair),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.5f,
				Visible = false,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Price",
				Name = nameof(Order.PriceQ2B),
				DataPropertyName = nameof(Order.PriceQ2B),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Live Price",
				Name = nameof(ColumnNames.LivePrice),
				DataPropertyName = nameof(ColumnNames.LivePrice),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Distance (Index)",
				Name = nameof(ColumnNames.PriceDist),
				DataPropertyName = nameof(ColumnNames.PriceDist),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Volume (Base)",
				Name = nameof(Order.VolumeBase),
				DataPropertyName = nameof(Order.VolumeBase),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Volume (Quote)",
				Name = nameof(Order.VolumeQuote),
				DataPropertyName = nameof(Order.VolumeQuote),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Remaining (%)",
				Name = nameof(Order.RemainingBase),
				DataPropertyName = nameof(Order.RemainingBase),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			DataSource = Model.Positions;
		}
		protected override void SetModelCore(Model model)
		{
			if (Model != null)
			{
				Model.SimRunningChanged -= HandleSimRunning;
			}
			base.SetModelCore(model);
			if (Model != null)
			{
				Model.SimRunningChanged += HandleSimRunning;
			}

			// Handlers
			void HandleSimRunning(object sender, PrePostEventArgs e)
			{
				// Disable the data source while the simulation is running, for performance reasons
				if (e.Before && !Model.SimRunning)
				{
					DataSource = m_dummy_row;
				}
				if (e.After && !Model.SimRunning)
				{
					DataSource = Model.Positions;
				}
			}
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Right)
			{
				var hit = this.HitTestEx(e.Location);
				if (hit.Type == DataGridView_.HitTestInfo.EType.ColumnHeader || hit.Type == DataGridView_.HitTestInfo.EType.ColumnDivider)
				{
					this.ColumnVisibilityContextMenu(hit.GridPoint);
				}
				else
				{
					var pos = SelectedRows.Cast<DataGridViewRow>().Select(x => (Order)x.DataBoundItem).SingleOrDefault();
					var cmenu = new ContextMenuStrip();
					{
						var opt = cmenu.Items.Add2(new ToolStripMenuItem("Cancel Order"));
						opt.Enabled = pos != null;
						opt.Click += (s, a) =>
						{
							pos?.CancelOrder();
						};
					}
					if (!Model.AllowTrades)
					{
						// Cancel the order and create a fake history entry to simulate the fake order being filled
						var opt = cmenu.Items.Add2(new ToolStripMenuItem("\"Fill\" order"));
						opt.Visible = pos != null && pos.Fake;
						opt.Click += (s, a) =>
						{
							pos?.FillFakeOrder();
						};
					}
					cmenu.Items.AddSeparator();
					{
						var opt = cmenu.Items.Add2(new ToolStripMenuItem("Modify"));
						opt.Enabled = pos != null;
						opt.Click += (s, a) =>
						{
							Model.EditTrade(pos);
						};
					}
					cmenu.Show(this, hit.GridPoint);
				}
			}
		}
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs a)
		{
			base.OnCellFormatting(a);
			if (!this.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col, out DataGridViewCell cell))
				return;

			// Display a message while the simulation is running
			if (DataSource == m_dummy_row)
			{
				a.Value = a.ColumnIndex == 0 && a.RowIndex == 0 ? "... Simulation Running ..." : string.Empty;
				a.FormattingApplied = true;
				return;
			}

			// Can happen during the change over from dummy row back to a data source
			var pos= (Order)cell.OwningRow.DataBoundItem;
			if (pos== null)
				return;

			// Format cells
			switch (col.Name)
			{
			case nameof(Order.Created):
				{
					a.Value = pos.Created?.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
					a.FormattingApplied = true;
					break;
				}
			case nameof(Order.TradeType):
				{
					a.Value =
						pos.TradeType == ETradeType.Q2B ? $"{pos.Pair.Quote}→{pos.Pair.Base} ({pos.TradeType})" :
						pos.TradeType == ETradeType.B2Q ? $"{pos.Pair.Base}→{pos.Pair.Quote} ({pos.TradeType})" :
						"---";
					a.FormattingApplied = true;
					break;
				}
			case nameof(Order.PriceQ2B):
				{
					a.Value = pos.PriceQ2B.ToString("G8",true);
					a.FormattingApplied = true;
					break;
				}
			case nameof(ColumnNames.LivePrice):
				{
					a.Value = pos.Pair.SpotPrice(pos.TradeType).ToString("G8");
					a.FormattingApplied = true;
					break;
				}
			case nameof(ColumnNames.PriceDist):
				{
					var spot_b2q = pos.Pair.SpotPrice(ETradeType.B2Q);
					var spot_q2b = pos.Pair.SpotPrice(ETradeType.Q2B);
					var dist =
						pos.TradeType == ETradeType.B2Q && spot_b2q != null ? (pos.PriceQ2B - spot_b2q.Value) :
						pos.TradeType == ETradeType.Q2B && spot_q2b != null ? (spot_q2b.Value - pos.PriceQ2B) :
						(decimal?)0m;

					a.Value = dist != null ? $"{dist:G8} ({pos.Pair.OrderBookIndex(pos.TradeType, pos.PriceQ2B)})" : "---";
					a.FormattingApplied = true;
					break;
				}
			case nameof(Order.VolumeBase):
				{
					a.Value = pos.VolumeBase.ToString("G8", true);
					a.FormattingApplied = true;
					break;
				}
			case nameof(Order.VolumeQuote):
				{
					a.Value = pos.VolumeQuote.ToString("G8",true);
					a.FormattingApplied = true;
					break;
				}
			case nameof(Order.RemainingBase):
				{
					a.Value = $"{100m * Math_.Div((decimal)pos.RemainingBase, (decimal)pos.VolumeBase, 0m):G4} %";
					a.FormattingApplied = true;
					break;
				}
			}
			a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
			a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
		}
		protected override void OnCellDoubleClick(DataGridViewCellEventArgs a)
		{
			base.OnCellDoubleClick(a);
			if (!this.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col, out DataGridViewCell cell) || DataSource == m_dummy_row)
				return;

			// Can happen during the change over from dummy row back to a data source
			var pos = (Order)cell.OwningRow.DataBoundItem;
			if (pos== null)
				return;

			// Launch an editor for the trade
			Model.EditTrade(pos);
		}

		public static class ColumnNames
		{
			public const int LivePrice = 0;
			public const int PriceDist = 0;
		}
	}
}
