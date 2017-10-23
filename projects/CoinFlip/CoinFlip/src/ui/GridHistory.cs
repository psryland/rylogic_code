using System.Drawing;
using System.Windows.Forms;
using pr.common;
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
			ContextMenuStrip = CreateCMenu();
			DataSource = Model.History;
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
					DataSource = Model.History;
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
			var fill = (PositionFill)cell.OwningRow.DataBoundItem;
			if (fill == null)
				return;

			// Format cells
			switch (col.DataPropertyName)
			{
			case nameof(PositionFill.Created):
				{
					a.Value = fill.Created.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
					a.FormattingApplied = true;
					break;
				}
			case nameof(PositionFill.TradeType):
				{
					a.Value =
						fill.TradeType == ETradeType.Q2B ? $"{fill.Pair.Quote}→{fill.Pair.Base} ({fill.TradeType})" :
						fill.TradeType == ETradeType.B2Q ? $"{fill.Pair.Base}→{fill.Pair.Quote} ({fill.TradeType})" :
						"---";
					a.FormattingApplied = true;
					break;
				}
			case nameof(PositionFill.Pair):
				{
					a.Value = fill.Pair.Name;
					a.FormattingApplied = true;
					break;
				}
			case nameof(PositionFill.PriceQ2B):
				{
					a.Value = fill.PriceQ2B.ToString("G8",true);
					a.FormattingApplied = true;
					break;
				}
			case nameof(PositionFill.VolumeIn):
				{
					a.Value = fill.VolumeIn.ToString("G8",true);
					a.FormattingApplied = true;
					break;
				}
			case nameof(PositionFill.VolumeOut):
				{
					a.Value = fill.VolumeOut.ToString("G8",true);
					a.FormattingApplied = true;
					break;
				}
			case nameof(PositionFill.VolumeNett):
				{
					a.Value = fill.VolumeNett.ToString("G8",true);
					a.FormattingApplied = true;
					break;
				}
			case nameof(PositionFill.Commission):
				{
					a.Value = fill.Commission.ToString("G10",true);
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
#if false
	public class GridHistory :TreeBase
	{
		protected override void Dispose(bool disposing)
		{
			DataSource = null;
			base.Dispose(disposing);
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
#endif
}
