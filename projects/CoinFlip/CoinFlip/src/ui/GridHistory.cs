using System;
using System.Windows.Forms;
using pr.common;
using pr.container;
using pr.extn;
using pr.gui;

namespace CoinFlip
{
	public class GridHistory :GridBase
	{
		private readonly string[] m_dummy_row;
		public GridHistory(Model model, string title, string name)
			:base(model, title,name)
		{
			m_dummy_row = new string[1];
			ReadOnly = true;
			VirtualMode = true;
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Order/Trade Id",
				Name = nameof(PositionFill.OrderId),
				DataPropertyName = nameof(PositionFill.OrderId),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.6f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Date",
				Name = nameof(PositionFill.Created),
				DataPropertyName = nameof(PositionFill.Created),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Type",
				Name = nameof(PositionFill.TradeType),
				DataPropertyName = nameof(PositionFill.TradeType),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Pair",
				Name = nameof(PositionFill.Pair),
				DataPropertyName = nameof(PositionFill.Pair),
				FillWeight = 0.5f,
				Visible = false,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Price",
				Name = nameof(PositionFill.PriceQ2B),
				DataPropertyName = nameof(PositionFill.PriceQ2B),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Volume In",
				Name = nameof(PositionFill.VolumeIn),
				DataPropertyName = nameof(PositionFill.VolumeIn),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Volume Out",
				Name = nameof(PositionFill.VolumeOut),
				DataPropertyName = nameof(PositionFill.VolumeOut),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Volume Nett",
				Name = nameof(PositionFill.VolumeNett),
				DataPropertyName = nameof(PositionFill.VolumeNett),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Commission",
				Name = nameof(PositionFill.Commission),
				DataPropertyName = nameof(PositionFill.Commission),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Trade Count",
				Name = nameof(PositionFill.TradeCount),
				DataPropertyName = nameof(PositionFill.TradeCount),
				FillWeight = 0.1f,
			});
		}
		protected override void SetModelCore(Model model)
		{
			if (Model != null)
			{
				Model.History.ListChanging -= HandleHistoryListChanging;
			}
			base.SetModelCore(model);
			if (Model != null)
			{
				Model.History.ListChanging += HandleHistoryListChanging;
			}

			// Handlers
			void HandleHistoryListChanging(object sender, ListChgEventArgs<PositionFill> e)
			{
				switch (e.ChangeType)
				{
				case ListChg.Reset:
				case ListChg.ItemAdded:
				case ListChg.ItemRemoved:
					RowCount = Model.History.Count;
					break;
				}
			}
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Right)
			{
				// Show a item context menu
				var hit = this.HitTestEx(e.X, e.Y);
				if (hit.Type == DataGridView_.HitTestInfo.EType.ColumnHeader)
					DataGridView_.ColumnVisibilityContextMenu(this, e.Location);
				if (hit.Type == DataGridView_.HitTestInfo.EType.Cell)
					CreateCMenu().Show(this, e.X, e.Y);
			}
		}
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			base.OnCellFormatting(e);
			DataGridView_.HalfBrightSelection(this, e);
		}
		protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
		{
			base.OnCellValueNeeded(e);
			if (this.Within(e.ColumnIndex, e.RowIndex, out DataGridViewColumn col) &&
				e.RowIndex.Within(0, Model.History.Count))
			{
				// Display a message while the simulation is running
				if (Model.SimRunning)
				{
					e.Value = e.ColumnIndex == 0 && e.RowIndex == 0 ? "... Simulation Running ..." : string.Empty;
					return;
				}

				// Otherwise provide the cell values
				var fill = Model.History[e.RowIndex];
				switch (col.DataPropertyName)
				{
				default: throw new Exception("Unknown column");
				case nameof(PositionFill.OrderId):
					{
						e.Value = fill.OrderId.ToString();
						break;
					}
				case nameof(PositionFill.Created):
					{
						e.Value = fill.Created.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
						break;
					}
				case nameof(PositionFill.TradeType):
					{
						e.Value =
							fill.TradeType == ETradeType.Q2B ? $"{fill.Pair.Quote}→{fill.Pair.Base} ({fill.TradeType})" :
							fill.TradeType == ETradeType.B2Q ? $"{fill.Pair.Base}→{fill.Pair.Quote} ({fill.TradeType})" :
							"---";
						break;
					}
				case nameof(PositionFill.Pair):
					{
						e.Value = fill.Pair.Name;
						break;
					}
				case nameof(PositionFill.PriceQ2B):
					{
						e.Value = fill.PriceQ2B.ToString("G8",true);
						break;
					}
				case nameof(PositionFill.VolumeIn):
					{
						e.Value = fill.VolumeIn.ToString("G8",true);
						break;
					}
				case nameof(PositionFill.VolumeOut):
					{
						e.Value = fill.VolumeOut.ToString("G8",true);
						break;
					}
				case nameof(PositionFill.VolumeNett):
					{
						e.Value = fill.VolumeNett.ToString("G8",true);
						break;
					}
				case nameof(PositionFill.Commission):
					{
						e.Value = fill.Commission.ToString("G10",true);
						break;
					}
				case nameof(PositionFill.TradeCount):
					{
						e.Value = fill.TradeCount.ToString();
						break;
					}
				}
			}
		}

		/// <summary>Create the context menu for the grid</summary>
		private ContextMenuStrip CreateCMenu()
		{
			var cmenu = new ContextMenuStrip();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Show Trades"));
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = SelectedRows.Count == 1;
				};
				opt.Click += (s,a) =>
				{
					MsgBox.Show(this, "TODO");
					//var pos = SelectedRows.Cast<DataGridViewRow>().Select(x => (Position)x.DataBoundItem).First();
					//pos.CancelOrder();
				};
			}
			return cmenu;
		}
	}
}
