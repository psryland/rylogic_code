using System;
using System.Windows.Forms;
using pr.util;
using pr.gui;
using pr.extn;
using pr.maths;
using pr.container;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Threading;

namespace Tradee
{
	public class TradesUI :BaseUI
	{
		#region UI Elements
		private ToolStrip m_ts;
		private ToolStripButton m_btn_aggregate;
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
				Model.Positions.Trades.ListChanging -= SignalUpdateGrid;
				Model.Positions.Orders.ListChanging -= SignalUpdateGrid;
			}
			base.SetModelCore(model);
			if (Model != null)
			{
				Model.Positions.Trades.ListChanging += SignalUpdateGrid;
				Model.Positions.Orders.ListChanging += SignalUpdateGrid;
				Model.Positions.Changed += Invalidate;
			}
		}

		/// <summary>The collection of trades</summary>
		public BindingSource<Trade> Trades
		{
			get { return Model.Positions.Trades; }
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Tool bar
			m_btn_aggregate.CheckedChanged += (s,a) =>
			{
				Settings.UI.AggregateTrades = m_btn_aggregate.Checked;
				UpdateUI();
			}; 
			#endregion

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
				DataPropertyName = nameof(Order.StopLossRel),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Take Profit",
				DataPropertyName = nameof(Order.TakeProfitRel),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Entry Time",
				DataPropertyName = nameof(Order.EntryTimeUTC),
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
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			m_grid.Invalidate();
		}

		/// <summary>Repopulate the tree grid of trades</summary>
		private void UpdateGrid()
		{
			m_sig_updated_grid = false;

			// Update the tree grid using differences so that expanded/collapsed nodes are preserved
			using (m_grid.SuspendLayout(true))
			{
				var trades = Trades.ToHashSet();

				// Remove any root level nodes that aren't in the trades list
				m_grid.RootNode.Nodes.RemoveIf(x => !trades.Contains(x.DataBoundItem));

				// Update the trades
				foreach (var trade in Trades)
				{
					// Get the node for 'trade'. If the trade is not yet in the tree grid, add it
					var node = m_grid.RootNode.Nodes.FirstOrDefault(x => trades.Contains(x.DataBoundItem));
					if (node == null)
						node = m_grid.RootNode.Nodes.Bind(trade);

					var orders = trade.Orders.ToHashSet();

					// Remove any orders not in the trade
					node.Nodes.RemoveIf(x => !orders.Contains(x.DataBoundItem));

					// Add any orders not yet in the grid
					orders.RemoveWhere(x => node.Nodes.Any(n => n.DataBoundItem == x));
					foreach (var order in orders)
						node.Nodes.Bind(order);
				}
			}
			m_grid.Invalidate();
		}
		private void SignalUpdateGrid(object sender = null, EventArgs args = null)
		{
			if (m_sig_updated_grid) return;
			m_sig_updated_grid = true;
			Dispatcher.CurrentDispatcher.BeginInvoke(UpdateGrid);
		}
		private bool m_sig_updated_grid;

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
					e.Value = "{0} {1}".Fmt(pos.Volume, Model.Acct.Currency);
					break;
				}
			case nameof(Order.StopLossRel):
				{
					if (trade != null) e.Value = string.Empty;
					if (order != null) e.Value = "{0:N2} {1}".Fmt(order.StopLossRel * order.Volume, Model.Acct.Currency);
					break;
				}
			case nameof(Order.TakeProfitRel):
				{
					if (trade != null) e.Value = string.Empty;
					if (order != null) e.Value = "{0:N2} {1}".Fmt(order.TakeProfitRel * order.Volume, Model.Acct.Currency);
					break;
				}
			case nameof(Order.EntryTimeUTC):
				{
					if (trade != null) e.Value = string.Empty;
					if (order != null) e.Value = TimeZone.CurrentTimeZone.ToLocalTime(order.EntryTimeUTC.DateTime).ToString("yyyy-MM-dd HH:mm:ss");
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
				var trade = node.DataBoundItem as Trade;
				if (trade != null)
					trade.Orders.ForEach(x => sel.Add(x));

				var order = node.DataBoundItem as Order;
				if (order != null)
					sel.Add(order);
			}

			// Set the highlighted state for all selected orders on all charts
			foreach (var order_gfx in Model.Charts.SelectMany(x => x.Elements).OfType<OrderChartElement>())
				order_gfx.Highlighted = sel.Contains(order_gfx.Order);
		}

		/// <summary>Handle mouse click within the trade grid</summary>
		private void HandleMouseClick(object sender, MouseEventArgs e)
		{
			var hit = m_grid.HitTestEx(e.X, e.Y);
			var trade = hit.RowIndex >= 0 && hit.RowIndex < m_grid.RowCount ? m_grid.Rows[hit.RowIndex].DataBoundItem as Trade : null;

			// Show the context menu on right mouse click
			if (e.Button == MouseButtons.Right)
			{
				ShowContextMenu(hit, trade);
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
		private void ShowContextMenu(DataGridViewEx.HitTestInfo hit, Trade trade)
		{
			var cmenu = new ContextMenuStrip();
			if (trade != null)
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Delete Trade"));
				opt.Enabled =
					trade != null &&
					!Bit.AnySet(trade.State, Trade.EState.PendingOrder|Trade.EState.ActivePosition);
				opt.ToolTipText =
					trade == null ? "No trade selected" :
					Bit.AnySet(trade.State, Trade.EState.PendingOrder|Trade.EState.ActivePosition) ? "Cannot delete trades with active or pending orders" :
					string.Empty;
				opt.Click += (s,a) =>
				{
					Trades.Remove(trade);
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
			this.m_btn_aggregate = new System.Windows.Forms.ToolStripButton();
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
            this.m_btn_aggregate});
			this.m_ts.Location = new System.Drawing.Point(0, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(659, 31);
			this.m_ts.Stretch = true;
			this.m_ts.TabIndex = 1;
			this.m_ts.Text = "toolStrip1";
			// 
			// m_btn_aggregate
			// 
			this.m_btn_aggregate.CheckOnClick = true;
			this.m_btn_aggregate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_aggregate.Image = global::Tradee.Properties.Resources.aggregate;
			this.m_btn_aggregate.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_aggregate.Name = "m_btn_aggregate";
			this.m_btn_aggregate.Size = new System.Drawing.Size(28, 28);
			this.m_btn_aggregate.Text = "Aggregate";
			this.m_btn_aggregate.ToolTipText = "Aggregate by instrument";
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
