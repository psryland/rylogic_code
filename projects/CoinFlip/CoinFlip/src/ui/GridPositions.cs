using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

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
				Name = nameof(Position.OrderId),
				DataPropertyName = nameof(Position.OrderId),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Date",
				Name = nameof(Position.Created),
				DataPropertyName = nameof(Position.Created),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Type",
				Name = nameof(Position.TradeType),
				DataPropertyName = nameof(Position.TradeType),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Pair",
				Name = nameof(Position.Pair),
				DataPropertyName = nameof(Position.Pair),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.5f,
				Visible = false,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Price",
				Name = nameof(Position.PriceQ2B),
				DataPropertyName = nameof(Position.PriceQ2B),
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
				Name = nameof(Position.VolumeBase),
				DataPropertyName = nameof(Position.VolumeBase),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Volume (Quote)",
				Name = nameof(Position.VolumeQuote),
				DataPropertyName = nameof(Position.VolumeQuote),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Remaining (%)",
				Name = nameof(Position.RemainingBase),
				DataPropertyName = nameof(Position.RemainingBase),
				SortMode = DataGridViewColumnSortMode.NotSortable,
				FillWeight = 1.0f,
			});
			ContextMenuStrip = CreateCMenu();
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
			DataGridView_.ColumnVisibility(this, e);
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
			var pos= (Position)cell.OwningRow.DataBoundItem;
			if (pos== null)
				return;

			// Format cells
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
					a.Value =
						pos.TradeType == ETradeType.Q2B ? $"{pos.Pair.Quote}→{pos.Pair.Base} ({pos.TradeType})" :
						pos.TradeType == ETradeType.B2Q ? $"{pos.Pair.Base}→{pos.Pair.Quote} ({pos.TradeType})" :
						"---";
					a.FormattingApplied = true;
					break;
				}
			case nameof(Position.PriceQ2B):
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
			case nameof(Position.VolumeBase):
				{
					a.Value = pos.VolumeBase.ToString("G8", true);
					a.FormattingApplied = true;
					break;
				}
			case nameof(Position.VolumeQuote):
				{
					a.Value = pos.VolumeQuote.ToString("G8",true);
					a.FormattingApplied = true;
					break;
				}
			case nameof(Position.RemainingBase):
				{
					a.Value = $"{100m * Maths.Div((decimal)pos.RemainingBase, (decimal)pos.VolumeBase, 0m):G4} %";
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
			var pos = (Position)cell.OwningRow.DataBoundItem;
			if (pos== null)
				return;

			// Launch an editor for the trade
			Model.EditTrade(pos);
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
				opt.Click += (s,a) =>
				{
					var pos = SelectedRows.Cast<DataGridViewRow>().Select(x => (Position)x.DataBoundItem).First();
					pos.CancelOrder();
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
				opt.Click += (s,a) =>
				{
					// Cancel the order and create a fake history entry to simulate the fake order being filled
					var pos = SelectedRows.Cast<DataGridViewRow>().Select(x => (Position)x.DataBoundItem).First();
					pos.FillFakeOrder();
				};
			}
			cmenu.Items.AddSeparator();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Modify"));
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = SelectedRows.Count == 1;
				};
				opt.Click += (s,a) =>
				{
					var pos = SelectedRows.Cast<DataGridViewRow>().Select(x => (Position)x.DataBoundItem).First();
					Model.EditTrade(pos);
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
