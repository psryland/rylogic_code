using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.gui;
using pr.util;
using DataGridView = pr.gui.DataGridView;

namespace CoinFlip
{
	public class LoopsUI :ToolForm
	{
		#region UI Elements
		private DataGridView m_grid_loops;
		#endregion

		public LoopsUI(Model model, Control parent)
			:base(parent, EPin.Centre)
		{
			InitializeComponent();
			Model = model;
			HideOnClose = true;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Model = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.MarketDataChanged -= HandleMarketDataChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.MarketDataChanged += HandleMarketDataChanged;
				}
			}
		}
		private Model m_model;

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			#region Loops Grid
			{
				m_grid_loops.AutoGenerateColumns = false;
				m_grid_loops.Columns.Add(new DataGridViewTextBoxColumn
				{
					HeaderText = "Loops",
					DataPropertyName = nameof(Loop.LoopDescription),
				});
				m_grid_loops.Columns.Add(new DataGridViewTextBoxColumn
				{
					HeaderText = "Profit Ratio",
					DataPropertyName = nameof(Loop.ProfitRatio),
				});
				m_grid_loops.DataSource = Model.Loops;
			}
			#endregion
		}

		/// <summary>Handle new market data</summary>
		private void HandleMarketDataChanged(object sender, EventArgs e)
		{
			m_grid_loops.InvalidateColumn(1);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoopsUI));
			this.m_grid_loops = new pr.gui.DataGridView();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_loops)).BeginInit();
			this.SuspendLayout();
			// 
			// m_grid_loops
			// 
			this.m_grid_loops.AllowUserToAddRows = false;
			this.m_grid_loops.AllowUserToDeleteRows = false;
			this.m_grid_loops.AllowUserToResizeRows = false;
			this.m_grid_loops.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_loops.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_loops.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_loops.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_loops.Location = new System.Drawing.Point(0, 0);
			this.m_grid_loops.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_loops.Name = "m_grid_loops";
			this.m_grid_loops.ReadOnly = true;
			this.m_grid_loops.RowHeadersVisible = false;
			this.m_grid_loops.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_loops.Size = new System.Drawing.Size(280, 401);
			this.m_grid_loops.TabIndex = 0;
			// 
			// LoopsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(280, 401);
			this.Controls.Add(this.m_grid_loops);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "LoopsUI";
			this.Text = "Trading Pair Loops";
			((System.ComponentModel.ISupportInitialize)(this.m_grid_loops)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
