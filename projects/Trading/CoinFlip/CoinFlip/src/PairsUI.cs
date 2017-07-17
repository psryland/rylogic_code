using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.gui;
using pr.util;

namespace CoinFlip
{
	public class PairsUI :ToolForm
	{
		#region UI Elements
		private TableLayoutPanel m_table0;
		private TableLayoutPanel m_table1;
		private pr.gui.DataGridView m_grid_sells;
		private pr.gui.DataGridView m_grid_buys;
		private Panel m_panel0;
		private TextBox m_tb_exchange1;
		private TextBox m_tb_exchange0;
		private Label m_lbl_exchanges;
		private Label m_lbl_pair;
		private Label m_lbl_quote_exchange;
		private Label m_lbl_buy_orders;
		private Label m_lbl_sell_orders;
		private pr.gui.ComboBox m_cb_pair;
		#endregion

		public PairsUI(Model model, Control parent)
			:base(parent, EPin.Centre)
		{
			InitializeComponent();
			Model = model;

			Pairs = new BindingSource<TradePair>();
			Sells = new BindingSource<Order>();
			Buys = new BindingSource<Order>();

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Pairs = null;
			Buys = null;
			Sells = null;
			Model = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{}
				m_model = value;
				if (m_model != null)
				{}
			}
		}
		private Model m_model;

		/// <summary>Binding source of pairs</summary>
		private BindingSource<TradePair> Pairs
		{
			get { return m_pairs; }
			set
			{
				if (m_pairs == value) return;
				if (m_pairs != null)
				{
					m_pairs.PositionChanged -= HandleCurrentPairChanged;
					m_pairs.DataSource = null;
				}
				m_pairs = value;
				if (m_pairs != null)
				{
					m_pairs.DataSource = Model.Pairs;
					m_pairs.PositionChanged += HandleCurrentPairChanged;
				}
			}
		}
		private BindingSource<TradePair> m_pairs;

		/// <summary>Binding source for Sell(Ask) orders</summary>
		private BindingSource<Order> Sells
		{
			get;
			set;
		}

		/// <summary>Binding source for Buy(Bid) orders</summary>
		private BindingSource<Order> Buys
		{
			get;
			set;
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			#region Pair Selection
			{
				m_cb_pair.DataSource = Pairs;
				m_cb_pair.DisplayProperty = nameof(TradePair.NameWithExchange);
			}
			#endregion

			#region Sells Grid
			m_grid_sells.Name = "Sell Orders";
			m_grid_sells.AutoGenerateColumns = false;
			m_grid_sells.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Order.Price),
				HeaderText = "Price",
				DataPropertyName = nameof(Order.Price),
			});
			m_grid_sells.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Order.VolumeBase),
				HeaderText = "Volume",
				DataPropertyName = nameof(Order.VolumeBase),
			});
			m_grid_sells.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Order.VolumeQuote),
				HeaderText = "Volume",
				DataPropertyName = nameof(Order.VolumeQuote),
			});
			m_grid_sells.CellFormatting += (s,a) =>
			{
				a.CellStyle.SelectionBackColor = a.CellStyle.BackColor;
				a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
			};
			m_grid_sells.DataSource = Sells;
			#endregion

			#region Buys Grid
			m_grid_buys.Name = "Buy Orders";
			m_grid_buys.AutoGenerateColumns = false;
			m_grid_buys.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Order.Price),
				HeaderText = "Price",
				DataPropertyName = nameof(Order.Price),
			});
			m_grid_buys.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Order.VolumeBase),
				HeaderText = "Volume",
				DataPropertyName = nameof(Order.VolumeBase),
			});
			m_grid_buys.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Order.VolumeQuote),
				HeaderText = "Volume",
				DataPropertyName = nameof(Order.VolumeQuote),
			});
			m_grid_buys.CellFormatting += (s,a) =>
			{
				a.CellStyle.SelectionBackColor = a.CellStyle.BackColor;
				a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
			};
			m_grid_buys.DataSource = Buys;
			#endregion
		}

		/// <summary>Handle the selected pair changing</summary>
		private void HandleCurrentPairChanged(object sender, PositionChgEventArgs e)
		{
			var pair = Pairs.Current;
			if (pair != null)
			{
				m_tb_exchange0.Text = pair.Base.Exchange.Name;
				m_tb_exchange1.Text = pair.Quote.Exchange.Name;

				m_grid_sells.Columns[nameof(Order.VolumeBase )].HeaderText = "Volume ({0})".Fmt(pair.Base);
				m_grid_sells.Columns[nameof(Order.VolumeQuote)].HeaderText = "Volume ({0})".Fmt(pair.Quote);

				m_grid_buys.Columns[nameof(Order.VolumeBase )].HeaderText = "Volume ({0})".Fmt(pair.Base);
				m_grid_buys.Columns[nameof(Order.VolumeQuote)].HeaderText = "Volume ({0})".Fmt(pair.Quote);

				Sells.DataSource = pair.Ask.Orders;
				Buys.DataSource = pair.Bid.Orders;
			}
			else
			{
				m_tb_exchange0.Text = string.Empty;
				m_tb_exchange1.Text = string.Empty;

				m_grid_sells.Columns[nameof(Order.VolumeBase )].HeaderText = "Volume (Base)";
				m_grid_sells.Columns[nameof(Order.VolumeQuote)].HeaderText = "Volume (Quote)";

				m_grid_buys.Columns[nameof(Order.VolumeBase )].HeaderText = "Volume (Base)";
				m_grid_buys.Columns[nameof(Order.VolumeQuote)].HeaderText = "Volume (Quote)";

				Sells.DataSource = null;
				Buys.DataSource = null;
			}
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PairsUI));
			this.m_table0 = new System.Windows.Forms.TableLayoutPanel();
			this.m_table1 = new System.Windows.Forms.TableLayoutPanel();
			this.m_grid_buys = new pr.gui.DataGridView();
			this.m_grid_sells = new pr.gui.DataGridView();
			this.m_panel0 = new System.Windows.Forms.Panel();
			this.m_lbl_quote_exchange = new System.Windows.Forms.Label();
			this.m_tb_exchange1 = new System.Windows.Forms.TextBox();
			this.m_tb_exchange0 = new System.Windows.Forms.TextBox();
			this.m_lbl_exchanges = new System.Windows.Forms.Label();
			this.m_lbl_pair = new System.Windows.Forms.Label();
			this.m_cb_pair = new pr.gui.ComboBox();
			this.m_lbl_sell_orders = new System.Windows.Forms.Label();
			this.m_lbl_buy_orders = new System.Windows.Forms.Label();
			this.m_table0.SuspendLayout();
			this.m_table1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_buys)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_sells)).BeginInit();
			this.m_panel0.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_table0
			// 
			this.m_table0.ColumnCount = 1;
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Controls.Add(this.m_table1, 0, 1);
			this.m_table0.Controls.Add(this.m_panel0, 0, 0);
			this.m_table0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table0.Location = new System.Drawing.Point(0, 0);
			this.m_table0.Name = "m_table0";
			this.m_table0.RowCount = 2;
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 53F));
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.m_table0.Size = new System.Drawing.Size(521, 438);
			this.m_table0.TabIndex = 0;
			// 
			// m_table1
			// 
			this.m_table1.ColumnCount = 2;
			this.m_table1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table1.Controls.Add(this.m_lbl_buy_orders, 1, 0);
			this.m_table1.Controls.Add(this.m_grid_buys, 1, 1);
			this.m_table1.Controls.Add(this.m_grid_sells, 0, 1);
			this.m_table1.Controls.Add(this.m_lbl_sell_orders, 0, 0);
			this.m_table1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table1.Location = new System.Drawing.Point(3, 56);
			this.m_table1.Name = "m_table1";
			this.m_table1.RowCount = 2;
			this.m_table1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.m_table1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table1.Size = new System.Drawing.Size(515, 379);
			this.m_table1.TabIndex = 0;
			// 
			// m_grid_buys
			// 
			this.m_grid_buys.AllowUserToAddRows = false;
			this.m_grid_buys.AllowUserToDeleteRows = false;
			this.m_grid_buys.AllowUserToResizeRows = false;
			dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_grid_buys.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
			this.m_grid_buys.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_buys.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid_buys.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_buys.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_buys.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_buys.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.m_grid_buys.Location = new System.Drawing.Point(265, 28);
			this.m_grid_buys.Margin = new System.Windows.Forms.Padding(8);
			this.m_grid_buys.MultiSelect = false;
			this.m_grid_buys.Name = "m_grid_buys";
			this.m_grid_buys.ReadOnly = true;
			this.m_grid_buys.RowHeadersVisible = false;
			this.m_grid_buys.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_buys.Size = new System.Drawing.Size(242, 343);
			this.m_grid_buys.TabIndex = 1;
			// 
			// m_grid_sells
			// 
			this.m_grid_sells.AllowUserToAddRows = false;
			this.m_grid_sells.AllowUserToDeleteRows = false;
			this.m_grid_sells.AllowUserToResizeRows = false;
			dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_grid_sells.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle2;
			this.m_grid_sells.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_sells.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid_sells.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_sells.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_sells.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_sells.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.m_grid_sells.Location = new System.Drawing.Point(8, 28);
			this.m_grid_sells.Margin = new System.Windows.Forms.Padding(8);
			this.m_grid_sells.MultiSelect = false;
			this.m_grid_sells.Name = "m_grid_sells";
			this.m_grid_sells.ReadOnly = true;
			this.m_grid_sells.RowHeadersVisible = false;
			this.m_grid_sells.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_sells.Size = new System.Drawing.Size(241, 343);
			this.m_grid_sells.TabIndex = 0;
			// 
			// m_panel0
			// 
			this.m_panel0.Controls.Add(this.m_lbl_quote_exchange);
			this.m_panel0.Controls.Add(this.m_tb_exchange1);
			this.m_panel0.Controls.Add(this.m_tb_exchange0);
			this.m_panel0.Controls.Add(this.m_lbl_exchanges);
			this.m_panel0.Controls.Add(this.m_lbl_pair);
			this.m_panel0.Controls.Add(this.m_cb_pair);
			this.m_panel0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panel0.Location = new System.Drawing.Point(0, 0);
			this.m_panel0.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel0.Name = "m_panel0";
			this.m_panel0.Size = new System.Drawing.Size(521, 53);
			this.m_panel0.TabIndex = 2;
			// 
			// m_lbl_quote_exchange
			// 
			this.m_lbl_quote_exchange.AutoSize = true;
			this.m_lbl_quote_exchange.Location = new System.Drawing.Point(172, 29);
			this.m_lbl_quote_exchange.Name = "m_lbl_quote_exchange";
			this.m_lbl_quote_exchange.Size = new System.Drawing.Size(90, 13);
			this.m_lbl_quote_exchange.TabIndex = 4;
			this.m_lbl_quote_exchange.Text = "Quote Exchange:";
			// 
			// m_tb_exchange1
			// 
			this.m_tb_exchange1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_exchange1.Location = new System.Drawing.Point(264, 27);
			this.m_tb_exchange1.Name = "m_tb_exchange1";
			this.m_tb_exchange1.ReadOnly = true;
			this.m_tb_exchange1.Size = new System.Drawing.Size(160, 20);
			this.m_tb_exchange1.TabIndex = 5;
			// 
			// m_tb_exchange0
			// 
			this.m_tb_exchange0.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_exchange0.Location = new System.Drawing.Point(264, 6);
			this.m_tb_exchange0.Name = "m_tb_exchange0";
			this.m_tb_exchange0.ReadOnly = true;
			this.m_tb_exchange0.Size = new System.Drawing.Size(160, 20);
			this.m_tb_exchange0.TabIndex = 4;
			// 
			// m_lbl_exchanges
			// 
			this.m_lbl_exchanges.AutoSize = true;
			this.m_lbl_exchanges.Location = new System.Drawing.Point(172, 8);
			this.m_lbl_exchanges.Name = "m_lbl_exchanges";
			this.m_lbl_exchanges.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_exchanges.TabIndex = 3;
			this.m_lbl_exchanges.Text = "Base Exchange:";
			// 
			// m_lbl_pair
			// 
			this.m_lbl_pair.AutoSize = true;
			this.m_lbl_pair.Location = new System.Drawing.Point(3, 15);
			this.m_lbl_pair.Name = "m_lbl_pair";
			this.m_lbl_pair.Size = new System.Drawing.Size(28, 13);
			this.m_lbl_pair.TabIndex = 2;
			this.m_lbl_pair.Text = "Pair:";
			// 
			// m_cb_pair
			// 
			this.m_cb_pair.BackColor = System.Drawing.Color.White;
			this.m_cb_pair.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_pair.BackColorValid = System.Drawing.Color.White;
			this.m_cb_pair.DisplayProperty = null;
			this.m_cb_pair.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_pair.ForeColor = System.Drawing.Color.Black;
			this.m_cb_pair.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_pair.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_pair.FormattingEnabled = true;
			this.m_cb_pair.Location = new System.Drawing.Point(35, 12);
			this.m_cb_pair.Name = "m_cb_pair";
			this.m_cb_pair.PreserveSelectionThruFocusChange = false;
			this.m_cb_pair.Size = new System.Drawing.Size(121, 21);
			this.m_cb_pair.TabIndex = 1;
			this.m_cb_pair.UseValidityColours = true;
			this.m_cb_pair.Value = null;
			// 
			// m_lbl_sell_orders
			// 
			this.m_lbl_sell_orders.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.m_lbl_sell_orders.AutoSize = true;
			this.m_lbl_sell_orders.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_sell_orders.Location = new System.Drawing.Point(3, 3);
			this.m_lbl_sell_orders.Name = "m_lbl_sell_orders";
			this.m_lbl_sell_orders.Size = new System.Drawing.Size(69, 13);
			this.m_lbl_sell_orders.TabIndex = 2;
			this.m_lbl_sell_orders.Text = "Sell Orders";
			// 
			// m_lbl_buy_orders
			// 
			this.m_lbl_buy_orders.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.m_lbl_buy_orders.AutoSize = true;
			this.m_lbl_buy_orders.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_buy_orders.Location = new System.Drawing.Point(260, 3);
			this.m_lbl_buy_orders.Name = "m_lbl_buy_orders";
			this.m_lbl_buy_orders.Size = new System.Drawing.Size(69, 13);
			this.m_lbl_buy_orders.TabIndex = 3;
			this.m_lbl_buy_orders.Text = "Buy Orders";
			// 
			// PairsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(521, 438);
			this.Controls.Add(this.m_table0);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "PairsUI";
			this.Text = "Trading Pair Details";
			this.m_table0.ResumeLayout(false);
			this.m_table1.ResumeLayout(false);
			this.m_table1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_buys)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_sells)).EndInit();
			this.m_panel0.ResumeLayout(false);
			this.m_panel0.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
