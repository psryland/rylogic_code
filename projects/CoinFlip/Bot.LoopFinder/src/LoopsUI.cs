using System;
using System.Drawing;
using System.Windows.Forms;
using CoinFlip;
using pr.extn;
using pr.gui;
using pr.util;
using DataGridView = pr.gui.DataGridView;

namespace Bot.LoopFinder
{
	public class LoopsUI :ToolForm
	{
		#region UI Elements
		private DataGridView m_grid_loops;
		#endregion

		public LoopsUI(LoopFinder loop_finder, Control parent)
			:base(parent, EPin.Centre)
		{
			InitializeComponent();
			DoubleBuffered = true;
			Bot = loop_finder;
			HideOnClose = true;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Bot = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected async override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			if (Visible)
				await Bot.RebuildLoopsAsync();
		}

		/// <summary>App logic</summary>
		public LoopFinder Bot
		{
			get { return m_bot; }
			private set
			{
				if (m_bot == value) return;
				if (m_bot != null)
				{
					m_bot.Model.MarketDataChanging -= HandleMarketDataChanging;
				}
				m_bot = value;
				if (m_bot != null)
				{
					m_bot.Model.MarketDataChanging += HandleMarketDataChanging;
				}
			}
		}
		private LoopFinder m_bot;

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			#region Loops Grid
			{
				m_grid_loops.DoubleBuffered(true);
				m_grid_loops.AutoGenerateColumns = false;
				m_grid_loops.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Loop.Description),
					HeaderText = "Loops",
					DataPropertyName = nameof(Loop.Description),
					SortMode = DataGridViewColumnSortMode.Programmatic,
					FillWeight = 3,
				});
				m_grid_loops.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Loop.ProfitRatioFwd),
					HeaderText = "Profit Ratio Forward",
					DataPropertyName = nameof(Loop.ProfitRatioFwd),
					SortMode = DataGridViewColumnSortMode.Programmatic,
					DefaultCellStyle = new DataGridViewCellStyle{ Format = "G4" },
					FillWeight = 1,
				});
				m_grid_loops.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Loop.ProfitRatioBck),
					HeaderText = "Profit Ratio Backward",
					DataPropertyName = nameof(Loop.ProfitRatioBck),
					SortMode = DataGridViewColumnSortMode.Programmatic,
					DefaultCellStyle = new DataGridViewCellStyle{ Format = "G4" },
					FillWeight = 1,
				});
				m_grid_loops.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Loop.Tradeability),
					HeaderText = "Trade-ability",
					DataPropertyName = nameof(Loop.Tradeability),
					SortMode = DataGridViewColumnSortMode.Programmatic,
					ToolTipText = "Reasons this trade could not be executed (i.e. lack of funds, which currencies, etc)",
					FillWeight = 1,
				});
				m_grid_loops.CellFormatting += (s,a) =>
				{
					a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
					a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
				};
				m_grid_loops.CellPainting += (s,a) =>
				{
					if (!m_grid_loops.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col, out DataGridViewCell cell)) return;
					var loop = Bot.Loops[a.RowIndex];

					switch (col.Name) {
					default: a.Handled = false; break;
					case nameof(Loop.Description):
						{
							var x = (float)a.CellBounds.Left;
							var y = (float)a.CellBounds.Top;
							var arrow_sz = a.Graphics.MeasureString("→", a.CellStyle.Font);
							a.PaintBackground(a.ClipBounds, cell.Selected);

							Action<Coin, bool> WriteCoin = (Coin coin, bool arrow) =>
							{
								var sz = a.Graphics.MeasureString(coin.Symbol, a.CellStyle.Font);
								using (var bsh = new SolidBrush(coin.Exchange.Colour))
								{
									a.Graphics.DrawString(coin.Symbol, a.CellStyle.Font, bsh  , new PointF(x, y + (a.CellBounds.Height - sz.Height) / 2)); x += sz.Width;
									if (!arrow) return;
									a.Graphics.DrawString("→", a.CellStyle.Font, Brushes.Black, new PointF(x, y + (a.CellBounds.Height - arrow_sz.Height) / 2)); x += arrow_sz.Width;
								}
							};

							var i = loop.Pairs.Count + 1;
							foreach (var coin in loop.EnumCoins(loop.Direction))
								WriteCoin(coin, --i != 0);

							a.Handled = true;
							break;
						}
					}
				};
				m_grid_loops.RowTemplate.Height = 28;
				m_grid_loops.DataSource = Bot.Loops;
			}
			#endregion
		}

		/// <summary>Handle new market data</summary>
		private void HandleMarketDataChanging(object sender, MarketDataChangingEventArgs e)
		{
			if (!e.Done) return;
			m_grid_loops.Invalidate();
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
			this.m_grid_loops.Size = new System.Drawing.Size(559, 407);
			this.m_grid_loops.TabIndex = 0;
			// 
			// LoopsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(559, 407);
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
