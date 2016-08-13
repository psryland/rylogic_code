using System;
using System.Drawing;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;
using Tradee.Properties;

namespace Tradee
{
	public class InstrumentsUI :BaseUI
	{
		#region UI Elements
		private SplitContainer m_split;
		private DataGridView m_grid_fav;
		private DataGridView m_grid_all;
		#endregion
		private DragDrop m_dd;

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
			if (Model != null)
			{
				Model.MarketData.DataChanged -= UpdateUI;
			}
			base.SetModelCore(model);
			if (Model != null)
			{
				Model.MarketData.DataChanged += UpdateUI;
			}
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
			// Set up the favourites grid and the all instruments grid exactly the same
			foreach (var grid in new[] {m_grid_all, m_grid_fav})
			{
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridViewTextBoxColumn
				{
					HeaderText = "Instrument",
					DataPropertyName = nameof(Instrument.SymbolCode),
					FillWeight = 1f,
				});
				grid.Columns.Add(new DataGridViewTextBoxColumn
				{
					HeaderText = "Spread",
					DataPropertyName = nameof(PriceData.Spread),
					FillWeight = 1f,
				});
				grid.Columns.Add(new DataGridViewTextBoxColumn
				{
					HeaderText = "Last Updated",
					DataPropertyName = nameof(Instrument.LastUpdatedLocal),
					FillWeight = 2f,
				});
				grid.CellFormatting += HandleGridCellFormatting;
				grid.CellMouseClick += HandleGridCellClick;
				grid.CellMouseDoubleClick += HandleCellDoubleClick;
			}

			// Set the data sources
			m_grid_all.DataSource = Model.MarketData.Instruments;
			m_grid_fav.DataSource = Favourites;
			m_grid_all.ColumnFilters(true).Enabled = true;

			// Support drag and drop from the 'all' to the favourites list
			m_dd = new DragDrop(m_grid_fav);
			m_dd.DoDrop += (s,a,m) =>
			{
				if (!a.Data.GetDataPresent(typeof(DataGridViewEx.DragDropData)))
					return false;

				// Set the drop effect to indicate what will happen if the item is dropped here
				a.Effect = DragDropEffects.Copy;

				// 'mode' == 'DragDrop.EDrop.Drop' when the item is actually dropped
				if (m != pr.util.DragDrop.EDrop.Drop)
					return true;

				var ddd   = (DataGridViewEx.DragDropData)a.Data.GetData(typeof(DataGridViewEx.DragDropData));
				var instr = ddd.Row.DataBoundItem as Instrument;
				var pt    = m_grid_fav.PointToClient(new Point(a.X, a.Y));
				var hit   = m_grid_fav.HitTestEx(pt);
				Favourites.Insert(hit.RowIndex, instr);
				return true;
			};

			m_grid_fav.AllowDrop = true;
			m_grid_all.MouseDown += DataGridViewEx.DragDrop_DragRow;
		}

		/// <summary>Update the UI</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			Invalidate(true);
		}

		/// <summary>Set the display style of the grid cells</summary>
		private void HandleGridCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (!grid.Within(e.ColumnIndex, e.RowIndex))
				return;

			var col = grid.Columns[e.ColumnIndex];
			var row = grid.Rows[e.RowIndex];
			var item = (Instrument)row.DataBoundItem;
			switch (col.DataPropertyName)
			{
			default: throw new Exception("Unknown column");
			case nameof(Instrument.SymbolCode):
				{
					// Todo
					// Colour the cell based on bullish/bearish market
					break;
				}
			case nameof(PriceData.Spread):
				{
					e.Value = "{0:N1}".Fmt(item.PriceData.SpreadPips);
					break;
				}
			case nameof(Instrument.LastUpdatedLocal):
				{
					e.Value = item.LastUpdatedUTC != DateTimeOffset.MinValue ? item.LastUpdatedLocal.ToString("yyyy/MM/dd HH:mm:ss") : "never";
					e.FormattingApplied = true;
					break;
				}
			}

			e.CellStyle.ForeColor = Color.Blue.Lerp(Color.DarkGray, (float)Maths.Clamp(Maths.Frac(0.0, (Model.UtcNow - item.LastUpdatedUTC).TotalSeconds, 10.0), 0.0, 1.0));
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
				Model.ShowChart(instr.SymbolCode);
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
						Model.ShowChart(instr.SymbolCode);
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
			this.m_grid_fav = new System.Windows.Forms.DataGridView();
			this.m_grid_all = new System.Windows.Forms.DataGridView();
			((System.ComponentModel.ISupportInitialize)(this.m_split)).BeginInit();
			this.m_split.Panel1.SuspendLayout();
			this.m_split.Panel2.SuspendLayout();
			this.m_split.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_fav)).BeginInit();
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
			this.m_split.Panel1.Controls.Add(this.m_grid_fav);
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
			this.m_grid_fav.AllowUserToAddRows = false;
			this.m_grid_fav.AllowUserToDeleteRows = false;
			this.m_grid_fav.AllowUserToResizeRows = false;
			this.m_grid_fav.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_fav.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_grid_fav.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_fav.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_fav.Location = new System.Drawing.Point(0, 0);
			this.m_grid_fav.Name = "m_grid_favs";
			this.m_grid_fav.ReadOnly = true;
			this.m_grid_fav.RowHeadersVisible = false;
			this.m_grid_fav.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_fav.Size = new System.Drawing.Size(238, 283);
			this.m_grid_fav.TabIndex = 0;
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
			((System.ComponentModel.ISupportInitialize)(this.m_grid_fav)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_all)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
