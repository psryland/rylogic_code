using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.util;
using pr.gui;
using pr.extn;
using pr.maths;

namespace Tradee
{
	public class TradesUI :BaseUI
	{
		#region UI Elements
		private DataGridView m_grid;
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

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			m_grid.AutoGenerateColumns = false;
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Symbol",
				DataPropertyName = nameof(Trade.Symbol),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Trade Type",
				DataPropertyName = nameof(Trade.TradeType),
			});
			//m_grid.Columns.Add(new DataGridViewTextBoxColumn
			//{
			//	HeaderText = "Open Time",
			//	DataPropertyName = nameof(Order.OpenTime),
			//});
			m_grid.DataSource = Model.Trades;
			m_grid.MouseClick += HandleMouseClick;
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
		}

		/// <summary>Handle mouse click within the trade grid</summary>
		private void HandleMouseClick(object sender, MouseEventArgs e)
		{
			var hit = m_grid.HitTestEx(e.X, e.Y);
			var trade = m_grid.Rows[hit.RowIndex].DataBoundItem as Trade;

			// Show the context menu on right mouse click
			if (e.Button == MouseButtons.Right)
			{
				ShowContextMenu(hit, trade);
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
					!Bit.AnySet(trade.State, Trade.EState.PendingOrder|Trade.EState.ActivePosition);
				opt.ToolTipText =
					Bit.AnySet(trade.State, Trade.EState.PendingOrder|Trade.EState.ActivePosition) ? "Cannot delete trades with active or pending orders" :
					string.Empty;
				opt.Click += (s,a) =>
				{
					Model.Trades.Remove(trade);
				};
			}
			cmenu.Items.TidySeparators();
			cmenu.Show(m_grid, hit.GridPoint);
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_grid = new System.Windows.Forms.DataGridView();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
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
			this.m_grid.Location = new System.Drawing.Point(0, 0);
			this.m_grid.MultiSelect = false;
			this.m_grid.Name = "m_grid";
			this.m_grid.ReadOnly = true;
			this.m_grid.RowHeadersVisible = false;
			this.m_grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid.Size = new System.Drawing.Size(659, 238);
			this.m_grid.TabIndex = 0;
			// 
			// OrdersUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_grid);
			this.Name = "OrdersUI";
			this.Size = new System.Drawing.Size(659, 238);
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
