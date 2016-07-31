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
using pr.container;
using pr.extn;
using Tradee.Properties;

namespace Tradee
{
	public class InstrumentsUI :BaseUI
	{
		#region UI Elements
		private SplitContainer m_split;
		private DataGridView m_grid_favs;
		private DataGridView m_grid_all;
		#endregion

		public InstrumentsUI(MainModel model)
			:base(model, "Instruments")
		{
			InitializeComponent();
			DockControl.DefaultDockLocation = new DockContainer.DockLocation(auto_hide:EDockSite.Right);
			Favourites = new BindingSource<Instrument> { DataSource = new BindingListEx<Instrument>() };

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void SetModelCore(MainModel model)
		{
			base.SetModelCore(model);
		}

		/// <summary>The collection of favourite instruments</summary>
		public BindingSource<Instrument> Favourites
		{
			get;
			private set;
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			// Favourites grid
			m_grid_favs.AutoGenerateColumns = false;
			m_grid_favs.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Symbol Code",
				DataPropertyName = nameof(Instrument.SymbolCode),
			});
			m_grid_favs.DataSource = Favourites;
			m_grid_favs.CellMouseClick += HandleGridCellClick;
			m_grid_favs.CellMouseDoubleClick += HandleCellDoubleClick;

			// All instruments
			m_grid_all.AutoGenerateColumns = false;
			m_grid_all.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Symbol Code",
				DataPropertyName = nameof(Instrument.SymbolCode),
			});
			m_grid_all.DataSource = Model.MarketData.Instruments;
			m_grid_all.CellMouseClick += HandleGridCellClick;
			m_grid_all.CellMouseDoubleClick += HandleCellDoubleClick;
		}

		/// <summary>Handle a click within a grid</summary>
		private void HandleGridCellClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			var grid = (DataGridView)sender;
			var col = (DataGridViewColumn)null;
			if (!grid.Within(e.ColumnIndex, e.RowIndex, out col))
				return;

			var instr = (Instrument)grid.Rows[e.RowIndex].DataBoundItem;
			grid.CurrentCell = grid[e.ColumnIndex, e.RowIndex];

			if (e.Button == MouseButtons.Right)
			{
				var pt = Control_.MapPoint(null, this, MousePosition);
				ShowContextMenu(instr, pt);
			}
		}

		/// <summary>Handle a double click in a grid</summary>
		private void HandleCellDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			var grid = (DataGridView)sender;
			var col = (DataGridViewColumn)null;
			if (!grid.Within(e.ColumnIndex, e.RowIndex, out col))
				return;

			if (e.Button == MouseButtons.Left)
			{
				var instr = (Instrument)grid.Rows[e.RowIndex].DataBoundItem;
				Model.AddChart(instr);
			}
		}

		/// <summary>Grid context menu</summary>
		private void ShowContextMenu(Instrument instr, Point pt)
		{
			var cmenu = new ContextMenuStrip();
			using (cmenu.SuspendLayout(true))
			{
				{// Add Chart
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Add Chart", Resources.graph));
					opt.Enabled = instr != null;
					opt.ToolTipText = "Open a chart for this symbol";
					opt.Click += (s,a) =>
					{
						Model.AddChart(instr);
					};
				}
				{// Clear Cached Data
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Purge Cached Data", Resources.recycle));
					opt.Enabled = instr != null;
					opt.ToolTipText = "Flush the cached data for this symbol";
					opt.Click += (s,a) =>
					{
						var r = MsgBox.Show(this, "Purge cache price data for {0}?".Fmt(instr.SymbolCode), "Purge Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
						if (r == DialogResult.Yes)
							Model.MarketData.PurgeCachedInstrument(instr.SymbolCode);
					};
				}
			}
			cmenu.Show(this, pt);
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_split = new System.Windows.Forms.SplitContainer();
			this.m_grid_favs = new System.Windows.Forms.DataGridView();
			this.m_grid_all = new System.Windows.Forms.DataGridView();
			((System.ComponentModel.ISupportInitialize)(this.m_split)).BeginInit();
			this.m_split.Panel1.SuspendLayout();
			this.m_split.Panel2.SuspendLayout();
			this.m_split.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_favs)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_all)).BeginInit();
			this.SuspendLayout();
			// 
			// m_split
			// 
			this.m_split.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split.Location = new System.Drawing.Point(0, 0);
			this.m_split.Name = "m_split";
			this.m_split.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split.Panel1
			// 
			this.m_split.Panel1.Controls.Add(this.m_grid_favs);
			// 
			// m_split.Panel2
			// 
			this.m_split.Panel2.Controls.Add(this.m_grid_all);
			this.m_split.Size = new System.Drawing.Size(238, 599);
			this.m_split.SplitterDistance = 283;
			this.m_split.TabIndex = 0;
			// 
			// m_grid_favs
			// 
			this.m_grid_favs.AllowUserToAddRows = false;
			this.m_grid_favs.AllowUserToDeleteRows = false;
			this.m_grid_favs.AllowUserToResizeRows = false;
			this.m_grid_favs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_favs.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_grid_favs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_favs.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_favs.Location = new System.Drawing.Point(0, 0);
			this.m_grid_favs.Name = "m_grid_favs";
			this.m_grid_favs.ReadOnly = true;
			this.m_grid_favs.RowHeadersVisible = false;
			this.m_grid_favs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_favs.Size = new System.Drawing.Size(238, 283);
			this.m_grid_favs.TabIndex = 0;
			// 
			// m_grid_all
			// 
			this.m_grid_all.AllowUserToAddRows = false;
			this.m_grid_all.AllowUserToDeleteRows = false;
			this.m_grid_all.AllowUserToResizeRows = false;
			this.m_grid_all.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_all.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_grid_all.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_all.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_all.Location = new System.Drawing.Point(0, 0);
			this.m_grid_all.Name = "m_grid_all";
			this.m_grid_all.ReadOnly = true;
			this.m_grid_all.RowHeadersVisible = false;
			this.m_grid_all.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_all.Size = new System.Drawing.Size(238, 312);
			this.m_grid_all.TabIndex = 1;
			// 
			// InstrumentsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_split);
			this.Name = "InstrumentsUI";
			this.Size = new System.Drawing.Size(238, 599);
			this.m_split.Panel1.ResumeLayout(false);
			this.m_split.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split)).EndInit();
			this.m_split.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_favs)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_all)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
