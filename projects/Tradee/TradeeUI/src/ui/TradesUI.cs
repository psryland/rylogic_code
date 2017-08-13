using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;
using DataGridView = pr.gui.DataGridView;

namespace Tradee
{
	public class TradesUI :BaseUI
	{
		#region UI Elements
		private ToolStrip m_ts;
		private ToolStripButton m_btn_active_trades;
		private ToolStripButton m_btn_pending_orders;
		private ToolStripButton m_btn_visualising_orders;
		private ToolStripButton m_btn_closed_orders;
		private TreeGridView m_grid;
		#endregion

		public TradesUI(MainModel model)
			:base(model, "Trades")
		{
			InitializeComponent();
			DockControl.DefaultDockLocation = new DockContainer.DockLocation(new[] { EDockSite.Bottom });

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void SetModelCore(MainModel model)
		{
			if (Model != null)
			{
				Model.Positions.Changed -= Invalidate;
				Model.Positions.Trades.ListChanging -= UpdateGrid;
				Model.Positions.Orders.ListChanging -= UpdateGrid;
			}
			base.SetModelCore(model);
			if (Model != null)
			{
				Model.Positions.Trades.ListChanging += UpdateGrid;
				Model.Positions.Orders.ListChanging += UpdateGrid;
				Model.Positions.Changed += Invalidate;
			}
		}

		/// <summary>The collection of trades</summary>
		public BindingSource<Trade> Trades
		{
			get { return Model.Positions.Trades; }
		}

		/// <summary>The collection of orders</summary>
		public BindingSource<Order> Orders
		{
			get { return Model.Positions.Orders; }
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Tool bar
			m_btn_active_trades.Checked = Bit.AllSet(Settings.UI.ShowTrades, Trade.EState.ActivePosition);
			m_btn_active_trades.CheckedChanged += (s,a) =>
			{
				Settings.UI.ShowTrades = Bit.SetBits(Settings.UI.ShowTrades, Trade.EState.ActivePosition, m_btn_active_trades.Checked);
				UpdateGrid();
			};
			m_btn_pending_orders.Checked = Bit.AllSet(Settings.UI.ShowTrades, Trade.EState.PendingOrder);
			m_btn_pending_orders.CheckedChanged += (s,a) =>
			{
				Settings.UI.ShowTrades = Bit.SetBits(Settings.UI.ShowTrades, Trade.EState.PendingOrder, m_btn_pending_orders.Checked);
				UpdateGrid();
			};
			m_btn_visualising_orders.Checked = Bit.AllSet(Settings.UI.ShowTrades, Trade.EState.Visualising);
			m_btn_visualising_orders.CheckedChanged += (s,a) =>
			{
				Settings.UI.ShowTrades = Bit.SetBits(Settings.UI.ShowTrades, Trade.EState.Visualising, m_btn_visualising_orders.Checked);
				UpdateGrid();
			};
			m_btn_closed_orders.Checked = Bit.AllSet(Settings.UI.ShowTrades, Trade.EState.Closed);
			m_btn_closed_orders.CheckedChanged += (s,a) =>
			{
				Settings.UI.ShowTrades = Bit.SetBits(Settings.UI.ShowTrades, Trade.EState.Closed, m_btn_closed_orders.Checked);
				UpdateGrid();
			};
			#endregion

			#region Grid
			m_grid.AutoGenerateColumns = false;
			m_grid.Columns.Add(new TreeGridColumn
			{
				HeaderText = "Symbol/Order Id",
				DataPropertyName = nameof(Trade.SymbolCode),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "State",
				DataPropertyName = nameof(IPosition.State),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Trade Type",
				DataPropertyName = nameof(IPosition.TradeType),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Volume",
				DataPropertyName = nameof(IPosition.Volume),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Stop Loss",
				DataPropertyName = nameof(IPosition.StopLossValue),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Take Profit",
				DataPropertyName = nameof(IPosition.TakeProfitValue),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Entry Time",
				DataPropertyName = nameof(IPosition.EntryTimeUTC),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Gross Profit",
				DataPropertyName = nameof(IPosition.GrossProfit),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Net Profit",
				DataPropertyName = nameof(IPosition.NetProfit),
			});
			m_grid.CellFormatting += HandleCellFormatting;
			m_grid.MouseClick += HandleMouseClick;
			m_grid.CellMouseDoubleClick += HandleCellDoubleClick;
			m_grid.SelectionChanged += HandleOrderSelectionChanged;
			#endregion
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			m_btn_active_trades     .Checked = Bit.AllSet(Settings.UI.ShowTrades, Trade.EState.ActivePosition);
			m_btn_pending_orders    .Checked = Bit.AllSet(Settings.UI.ShowTrades, Trade.EState.PendingOrder);
			m_btn_visualising_orders.Checked = Bit.AllSet(Settings.UI.ShowTrades, Trade.EState.Visualising);
			m_btn_closed_orders     .Checked = Bit.AllSet(Settings.UI.ShowTrades, Trade.EState.Closed);

			m_grid.Invalidate();
		}

		/// <summary>Repopulate the tree grid of trades</summary>
		private void UpdateGrid(object sender = null, EventArgs args = null)
		{
			// Update the tree grid using differences so that expanded/collapsed nodes are preserved
			using (m_grid.SuspendLayout(true))
			{
				var trades = Trades.Where(x => Bit.AllSet(x.State, Settings.UI.ShowTrades)).ToHashSet();

				// Remove any root level nodes that aren't in the trades list
				m_grid.RootNode.Nodes.RemoveIf(x => !trades.Contains(x.DataBoundItem));

				// Update the trades
				foreach (var trade in Trades.Where(x => Bit.AllSet(x.State, Settings.UI.ShowTrades)))
				{
					// Get the node for 'trade'. If the trade is not yet in the tree grid, add it
					var node = m_grid.RootNode.Nodes.FirstOrDefault(x => x.DataBoundItem == trade)
						?? m_grid.RootNode.Nodes.Bind(trade);

					var orders = trade.Orders.Where(x => Bit.AllSet(x.State, Settings.UI.ShowTrades)).ToHashSet();

					// Remove any orders not in the trade
					node.Nodes.RemoveIf(x => !orders.Contains(x.DataBoundItem));

					// Add any orders not yet in the grid
					orders.RemoveWhere(x => node.Nodes.Any(n => n.DataBoundItem == x));
					foreach (var order in orders.Where(x => Bit.AllSet(x.State, Settings.UI.ShowTrades)))
						node.Nodes.Bind(order);
				}
			}
			m_grid.Invalidate();
		}

		/// <summary>Format the grid cell values</summary>
		private void HandleCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (TreeGridView)sender;
			DataGridViewCell cell; DataGridViewColumn col;
			if (!grid.Within(e.ColumnIndex, e.RowIndex, out col, out cell))
				return;

			var pos   = ((TreeGridNode)grid.Rows[e.RowIndex]).DataBoundItem as IPosition;
			var trade = ((TreeGridNode)grid.Rows[e.RowIndex]).DataBoundItem as Trade;
			var order = ((TreeGridNode)grid.Rows[e.RowIndex]).DataBoundItem as Order;

			// Set the background colour (in precedence order)
			if (pos.State.HasFlag(Trade.EState.Closed        )) e.CellStyle.BackColor = Settings.UI.ClosedPositionColour;
			if (pos.State.HasFlag(Trade.EState.Visualising   )) e.CellStyle.BackColor = Settings.UI.VisualisingColour;
			if (pos.State.HasFlag(Trade.EState.PendingOrder  )) e.CellStyle.BackColor = Settings.UI.PendingOrderColour;
			if (pos.State.HasFlag(Trade.EState.ActivePosition)) e.CellStyle.BackColor = Settings.UI.ActivePositionColour;

			switch (col.DataPropertyName)
			{
			default: throw new Exception("Unknown column: {0}".Fmt(col.DataPropertyName));
			case nameof(Trade.SymbolCode):
				{
					if (trade != null) e.Value = trade.SymbolCode;
					if (order != null) e.Value = order.Id;
					break;
				}
			case nameof(IPosition.State):
				{
					e.Value = pos.State;
					break;
				}
			case nameof(IPosition.TradeType):
				{
					e.Value = pos.TradeType;
					break;
				}
			case nameof(IPosition.Volume):
				{
					var info = Misc.KnownSymbols[pos.SymbolCode];
					e.Value = info.FmtString.Fmt(pos.Volume);
					break;
				}
			case nameof(IPosition.StopLossValue):
				{
					var val = Misc.BaseToAcctCurrency(pos.StopLossValue, pos.Instrument.PriceData);
					var pc = 100.0 * val / Model.Acct.Balance;
					e.Value = "{0:N2}% [{1:N2} {2}]".Fmt(pc, -val, Model.Acct.Currency);
					break;
				}
			case nameof(IPosition.TakeProfitValue):
				{
					var val = Misc.BaseToAcctCurrency(pos.TakeProfitValue, pos.Instrument.PriceData);
					var pc  = 100.0 * val / Model.Acct.Balance;
					e.Value = "{0:N2}% [{1:N2} {2}]".Fmt(pc, +val, Model.Acct.Currency);
					break;
				}
			case nameof(IPosition.EntryTimeUTC):
				{
					e.Value = TimeZone.CurrentTimeZone.ToLocalTime(pos.EntryTimeUTC.DateTime).ToString("yyyy-MM-dd HH:mm:ss");
					break;
				}
			case nameof(IPosition.GrossProfit):
				{
					e.Value = "{0:N2} {1}".Fmt(pos.GrossProfit, Model.Acct.Currency);
					e.CellStyle.BackColor = pos.GrossProfit > 0 ? Settings.UI.BullishColour : pos.GrossProfit < 0 ? Settings.UI.BearishColour : System.Drawing.Color.White;
					e.CellStyle.ForeColor = pos.GrossProfit > 0 ? Color.White : pos.GrossProfit < 0 ? Color.Black : System.Drawing.Color.Black;
					break;
				}
			case nameof(IPosition.NetProfit):
				{
					e.Value = "{0:N2} {1}".Fmt(pos.NetProfit, Model.Acct.Currency);
					e.CellStyle.BackColor = pos.NetProfit > 0 ? Settings.UI.BullishColour : pos.NetProfit < 0 ? Settings.UI.BearishColour : System.Drawing.Color.White;
					e.CellStyle.ForeColor = pos.NetProfit > 0 ? Color.White : pos.NetProfit < 0 ? Color.Black : System.Drawing.Color.Black;
					break;
				}
			}
			e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
			e.CellStyle.SelectionBackColor = e.CellStyle.BackColor.Lerp(Color.Black, 0.2f);
		}

		/// <summary>Handle trades or orders being selected in the grid</summary>
		private void HandleOrderSelectionChanged(object sender, EventArgs e)
		{
			// Get the set of selected orders
			var sel = new HashSet<Order>();
			foreach (var node in m_grid.SelectedRows.Cast<TreeGridNode>())
			{
				// Only add specifically selected orders, selecting the trade 'deselects' everything
				var order = node.DataBoundItem as Order;
				if (order != null)
					sel.Add(order);
			}

			// Set the highlighted state for all selected orders on all charts
			foreach (var order_gfx in Model.Charts.SelectMany(x => x.Elements).OfType<OrderChartElement>())
				order_gfx.Selected = sel.Contains(order_gfx.Order);
		}

		/// <summary>Handle mouse click within the trade grid</summary>
		private void HandleMouseClick(object sender, MouseEventArgs e)
		{
			var hit = m_grid.HitTestEx(e.X, e.Y);
			if (!m_grid.Within(hit.ColumnIndex, hit.RowIndex))
				return;

			// Ensure the clicked row is selected
			if (!m_grid.Rows[hit.RowIndex].Selected)
			{
				m_grid.ClearSelection();
				m_grid.Rows[hit.RowIndex].Selected = true;
			}

			// Get the data bound item
			var trade = ((TreeGridNode)m_grid.Rows[hit.RowIndex]).DataBoundItem as Trade;
			var order = ((TreeGridNode)m_grid.Rows[hit.RowIndex]).DataBoundItem as Order;

			// Show the context menu on right mouse click
			if (e.Button == MouseButtons.Right)
			{
				ShowContextMenu(hit, trade, order);
			}
		}

		/// <summary>Handle double click within the grid</summary>
		private void HandleCellDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (!grid.Within(e.ColumnIndex, e.RowIndex)) return;
			var row = (TreeGridNode)grid.Rows[e.RowIndex];

			// Open or select a chart for the associated instrument
			if (e.Button == MouseButtons.Left)
			{
				var pos = row.DataBoundItem as IPosition;
				Model.ShowChart(pos.SymbolCode);
			}
		}

		/// <summary>Display the trades grid context menu</summary>
		private void ShowContextMenu(DataGridView_.HitTestInfo hit, Trade trade, Order order)
		{
			var cmenu = new ContextMenuStrip();
			if (trade != null)
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Delete Trade"));
				opt.Enabled =
					!Bit.AnySet(trade.State, Trade.EState.PendingOrder|Trade.EState.ActivePosition);
				opt.ToolTipText =
					Bit.AnySet(trade.State, Trade.EState.PendingOrder|Trade.EState.ActivePosition) ? "Cannot delete trades with active or pending orders" :
					string.Empty;
				opt.Click += (s,a) =>
				{
					Orders.RemoveAll(trade.Orders);
					Trades.Remove(trade);
				};
			}
			if (order != null)
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Delete Order"));
				opt.Enabled =
					!Bit.AnySet(order.State, Trade.EState.PendingOrder|Trade.EState.ActivePosition);
				opt.ToolTipText =
					Bit.AnySet(order.State, Trade.EState.PendingOrder|Trade.EState.ActivePosition) ? "Cannot delete trades with active or pending orders" :
					string.Empty;
				opt.Click += (s,a) =>
				{
					Orders.Remove(order);
				};
			}
			cmenu.Items.TidySeparators();
			cmenu.Show(m_grid, hit.GridPoint);
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_grid = new pr.gui.TreeGridView();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_btn_active_trades = new System.Windows.Forms.ToolStripButton();
			this.m_btn_pending_orders = new System.Windows.Forms.ToolStripButton();
			this.m_btn_visualising_orders = new System.Windows.Forms.ToolStripButton();
			this.m_btn_closed_orders = new System.Windows.Forms.ToolStripButton();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.m_ts.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_grid
			// 
			this.m_grid.AllowUserToAddRows = false;
			this.m_grid.AllowUserToDeleteRows = false;
			this.m_grid.AllowUserToOrderColumns = true;
			this.m_grid.AllowUserToResizeRows = false;
			this.m_grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.m_grid.ImageList = null;
			this.m_grid.Location = new System.Drawing.Point(0, 31);
			this.m_grid.MultiSelect = false;
			this.m_grid.Name = "m_grid";
			this.m_grid.NodeCount = 0;
			this.m_grid.ReadOnly = true;
			this.m_grid.RowHeadersVisible = false;
			this.m_grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid.ShowLines = true;
			this.m_grid.Size = new System.Drawing.Size(659, 207);
			this.m_grid.TabIndex = 0;
			this.m_grid.VirtualNodes = false;
			// 
			// m_ts
			// 
			this.m_ts.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.m_ts.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_btn_active_trades,
            this.m_btn_pending_orders,
            this.m_btn_visualising_orders,
            this.m_btn_closed_orders});
			this.m_ts.Location = new System.Drawing.Point(0, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(659, 31);
			this.m_ts.Stretch = true;
			this.m_ts.TabIndex = 1;
			this.m_ts.Text = "toolStrip1";
			// 
			// m_btn_active_trades
			// 
			this.m_btn_active_trades.CheckOnClick = true;
			this.m_btn_active_trades.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_active_trades.Image = global::Tradee.Properties.Resources.active_orders;
			this.m_btn_active_trades.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_active_trades.Name = "m_btn_active_trades";
			this.m_btn_active_trades.Size = new System.Drawing.Size(28, 28);
			this.m_btn_active_trades.Text = "toolStripButton1";
			// 
			// m_btn_pending_orders
			// 
			this.m_btn_pending_orders.CheckOnClick = true;
			this.m_btn_pending_orders.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_pending_orders.Image = global::Tradee.Properties.Resources.pending_orders;
			this.m_btn_pending_orders.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_pending_orders.Name = "m_btn_pending_orders";
			this.m_btn_pending_orders.Size = new System.Drawing.Size(28, 28);
			this.m_btn_pending_orders.Text = "toolStripButton1";
			// 
			// m_btn_visualising_orders
			// 
			this.m_btn_visualising_orders.CheckOnClick = true;
			this.m_btn_visualising_orders.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_visualising_orders.Image = global::Tradee.Properties.Resources.visualising_orders;
			this.m_btn_visualising_orders.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_visualising_orders.Name = "m_btn_visualising_orders";
			this.m_btn_visualising_orders.Size = new System.Drawing.Size(28, 28);
			this.m_btn_visualising_orders.Text = "toolStripButton1";
			// 
			// m_btn_closed_orders
			// 
			this.m_btn_closed_orders.CheckOnClick = true;
			this.m_btn_closed_orders.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_closed_orders.Image = global::Tradee.Properties.Resources.closed_orders;
			this.m_btn_closed_orders.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_closed_orders.Name = "m_btn_closed_orders";
			this.m_btn_closed_orders.Size = new System.Drawing.Size(28, 28);
			this.m_btn_closed_orders.Text = "toolStripButton1";
			// 
			// TradesUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_grid);
			this.Controls.Add(this.m_ts);
			this.Name = "TradesUI";
			this.Size = new System.Drawing.Size(659, 238);
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.m_ts.ResumeLayout(false);
			this.m_ts.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
