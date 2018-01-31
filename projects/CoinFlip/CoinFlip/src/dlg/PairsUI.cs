using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Utility;
using ComboBox = Rylogic.Gui.ComboBox;
using DataGridView = Rylogic.Gui.DataGridView;

namespace CoinFlip
{
	public class PairsUI :ToolForm
	{
		#region UI Elements
		private TableLayoutPanel m_table0;
		private TableLayoutPanel m_table1;
		private DataGridView m_grid_sells;
		private DataGridView m_grid_buys;
		private Panel m_panel0;
		private TextBox m_tb_exchange1;
		private TextBox m_tb_exchange0;
		private Label m_lbl_exchanges;
		private Label m_lbl_pair;
		private Label m_lbl_quote_exchange;
		private Label m_lbl_buy_orders;
		private Label m_lbl_sell_orders;
		private Label m_lbl_exch;
		private ComboBox m_cb_exchange;
		private ComboBox m_cb_pair;
		#endregion

		public PairsUI(Model model, Control parent)
			:base(parent, EPin.Centre)
		{
			InitializeComponent();
			Model = model;
			Sells = new BindingSource<OrderBook.Offer>();
			Buys = new BindingSource<OrderBook.Offer>();
			Pairs = new BindingSource<TradePair>{ DataSource = new BindingListEx<TradePair>() };
			Exchanges = new BindingSource<Exchange>{};

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Exchanges = null;
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
				{
					m_model.PairsUpdated -= HandlePairsUpdated;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.PairsUpdated += HandlePairsUpdated;
				}

				// Handlers
				void HandlePairsUpdated(object sender, EventArgs e)
				{
					Exchanges?.ResetBindings();
				}
			}
		}
		private Model m_model;

		/// <summary>Binding source of exchanges</summary>
		private BindingSource<Exchange> Exchanges
		{
			get { return m_exchanges; }
			set
			{
				if (m_exchanges == value) return;
				if (m_exchanges != null)
				{
					m_exchanges.PositionChanged -= HandleSelectedExchangeChanged;
					m_exchanges.ListChanging -= HandleExchangesListChanging;
					m_exchanges.DataSource = null;
				}
				m_exchanges = value;
				if (m_exchanges != null)
				{
					m_exchanges.DataSource = m_model.Exchanges;
					m_exchanges.ListChanging += HandleExchangesListChanging;
					m_exchanges.PositionChanged += HandleSelectedExchangeChanged;
					UpdatePairs();
				}

				// Handlers
				void HandleExchangesListChanging(object sender, ListChgEventArgs<Exchange> e)
				{
					switch (e.ChangeType)
					{
					case ListChg.Reset:
					case ListChg.ItemAdded:
					case ListChg.ItemRemoved:
						UpdatePairs();
						break;
					}
				}
				void HandleSelectedExchangeChanged(object sender, PositionChgEventArgs e)
				{
					UpdatePairs();
				}
				void UpdatePairs()
				{
					var pair = Pairs?.Current;
					Pairs.Assign(Model.Pairs.Where(x => x.Exchange == Exchanges.Current));
					if (pair != null)
					{
						// Select the same pair on the new exchange, if available
						var idx = Pairs.IndexOf(x => x.CurrencyPair == pair.CurrencyPair);
						if (idx >= 0) Pairs.Position = idx;
					}
				}
			}
		}
		private BindingSource<Exchange> m_exchanges;

		/// <summary>Binding source of pairs</summary>
		private BindingSource<TradePair> Pairs
		{
			get { return m_pairs; }
			set
			{
				if (m_pairs == value) return;
				if (m_pairs != null)
				{
					m_pairs.PositionChanged -= HandleSelectedPairChanged;
				}
				m_pairs = value;
				if (m_pairs != null)
				{
					m_pairs.PositionChanged += HandleSelectedPairChanged;
				}

				// Handlers
				void HandleSelectedPairChanged(object sender, PositionChgEventArgs e)
				{
					var pair = Pairs.Current;
					if (pair != null)
					{
						m_tb_exchange0.Text = pair.Base.Exchange.Name;
						m_tb_exchange1.Text = pair.Quote.Exchange.Name;

						m_grid_sells.Columns[nameof(OrderBook.Offer.VolumeBase )].HeaderText = $"Volume ({pair.Base})";
						m_grid_sells.Columns[nameof(OrderBook.Offer.VolumeQuote)].HeaderText = $"Volume ({pair.Quote})";

						m_grid_buys.Columns[nameof(OrderBook.Offer.VolumeBase )].HeaderText = $"Volume ({pair.Base})";
						m_grid_buys.Columns[nameof(OrderBook.Offer.VolumeQuote)].HeaderText = $"Volume ({pair.Quote})";

						Sells.DataSource = pair.Q2B.Orders;
						Buys.DataSource = pair.B2Q.Orders;
					}
					else
					{
						m_tb_exchange0.Text = string.Empty;
						m_tb_exchange1.Text = string.Empty;

						m_grid_sells.Columns[nameof(OrderBook.Offer.VolumeBase )].HeaderText = "Volume (Base)";
						m_grid_sells.Columns[nameof(OrderBook.Offer.VolumeQuote)].HeaderText = "Volume (Quote)";

						m_grid_buys.Columns[nameof(OrderBook.Offer.VolumeBase )].HeaderText = "Volume (Base)";
						m_grid_buys.Columns[nameof(OrderBook.Offer.VolumeQuote)].HeaderText = "Volume (Quote)";

						Sells.DataSource = null;
						Buys.DataSource = null;
					}
				}
			}
		}
		private BindingSource<TradePair> m_pairs;

		/// <summary>Binding source for Sell(Ask) orders</summary>
		private BindingSource<OrderBook.Offer> Sells
		{
			get;
			set;
		}

		/// <summary>Binding source for Buy(Bid) orders</summary>
		private BindingSource<OrderBook.Offer> Buys
		{
			get;
			set;
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			#region Selection
			{
				// Exchange
				m_cb_exchange.DataSource = Exchanges;
				m_cb_exchange.DisplayProperty = nameof(Exchange.Name);
				m_cb_exchange.DropDown += ComboBox_.DropDownWidthAutoSize;

				// Pairs
				m_cb_pair.DataSource = Pairs;
				m_cb_pair.DisplayProperty = nameof(TradePair.NameWithExchange);
				m_cb_pair.DropDown += ComboBox_.DropDownWidthAutoSize;
			}
			#endregion

			#region Buys Grid
			m_grid_buys.Name = "Buy Orders";
			m_grid_buys.AutoGenerateColumns = false;
			m_grid_buys.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(OrderBook.Offer.VolumeQuote),
				HeaderText = "Volume",
				DataPropertyName = nameof(OrderBook.Offer.VolumeQuote),
			});
			m_grid_buys.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(OrderBook.Offer.VolumeBase),
				HeaderText = "Volume",
				DataPropertyName = nameof(OrderBook.Offer.VolumeBase),
			});
			m_grid_buys.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(OrderBook.Offer.Price),
				HeaderText = "Price",
				DataPropertyName = nameof(OrderBook.Offer.Price),
			});
			m_grid_buys.CellFormatting += (s,a) =>
			{
				a.CellStyle.SelectionBackColor = a.CellStyle.BackColor;
				a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
			};
			m_grid_buys.DataSource = Buys;
			#endregion

			#region Sells Grid
			m_grid_sells.Name = "Sell Orders";
			m_grid_sells.AutoGenerateColumns = false;
			m_grid_sells.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(OrderBook.Offer.Price),
				HeaderText = "Price",
				DataPropertyName = nameof(OrderBook.Offer.Price),
			});
			m_grid_sells.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(OrderBook.Offer.VolumeBase),
				HeaderText = "Volume",
				DataPropertyName = nameof(OrderBook.Offer.VolumeBase),
			});
			m_grid_sells.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(OrderBook.Offer.VolumeQuote),
				HeaderText = "Volume",
				DataPropertyName = nameof(OrderBook.Offer.VolumeQuote),
			});
			m_grid_sells.CellFormatting += (s,a) =>
			{
				a.CellStyle.SelectionBackColor = a.CellStyle.BackColor;
				a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
			};
			m_grid_sells.DataSource = Sells;
			#endregion
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
			this.m_lbl_buy_orders = new System.Windows.Forms.Label();
			this.m_grid_buys = new Rylogic.Gui.DataGridView();
			this.m_grid_sells = new Rylogic.Gui.DataGridView();
			this.m_lbl_sell_orders = new System.Windows.Forms.Label();
			this.m_panel0 = new System.Windows.Forms.Panel();
			this.m_lbl_exch = new System.Windows.Forms.Label();
			this.m_cb_exchange = new Rylogic.Gui.ComboBox();
			this.m_lbl_quote_exchange = new System.Windows.Forms.Label();
			this.m_tb_exchange1 = new System.Windows.Forms.TextBox();
			this.m_tb_exchange0 = new System.Windows.Forms.TextBox();
			this.m_lbl_exchanges = new System.Windows.Forms.Label();
			this.m_lbl_pair = new System.Windows.Forms.Label();
			this.m_cb_pair = new Rylogic.Gui.ComboBox();
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
			this.m_table0.Size = new System.Drawing.Size(529, 385);
			this.m_table0.TabIndex = 0;
			// 
			// m_table1
			// 
			this.m_table1.ColumnCount = 2;
			this.m_table1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table1.Controls.Add(this.m_lbl_buy_orders, 0, 0);
			this.m_table1.Controls.Add(this.m_grid_buys, 0, 1);
			this.m_table1.Controls.Add(this.m_grid_sells, 1, 1);
			this.m_table1.Controls.Add(this.m_lbl_sell_orders, 1, 0);
			this.m_table1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table1.Location = new System.Drawing.Point(3, 56);
			this.m_table1.Name = "m_table1";
			this.m_table1.RowCount = 2;
			this.m_table1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.m_table1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table1.Size = new System.Drawing.Size(523, 326);
			this.m_table1.TabIndex = 0;
			// 
			// m_lbl_buy_orders
			// 
			this.m_lbl_buy_orders.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.m_lbl_buy_orders.AutoSize = true;
			this.m_lbl_buy_orders.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_buy_orders.Location = new System.Drawing.Point(3, 3);
			this.m_lbl_buy_orders.Name = "m_lbl_buy_orders";
			this.m_lbl_buy_orders.Size = new System.Drawing.Size(69, 13);
			this.m_lbl_buy_orders.TabIndex = 3;
			this.m_lbl_buy_orders.Text = "Buy Orders";
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
			this.m_grid_buys.Location = new System.Drawing.Point(8, 28);
			this.m_grid_buys.Margin = new System.Windows.Forms.Padding(8);
			this.m_grid_buys.MultiSelect = false;
			this.m_grid_buys.Name = "m_grid_buys";
			this.m_grid_buys.ReadOnly = true;
			this.m_grid_buys.RowHeadersVisible = false;
			this.m_grid_buys.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_buys.Size = new System.Drawing.Size(245, 290);
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
			this.m_grid_sells.Location = new System.Drawing.Point(269, 28);
			this.m_grid_sells.Margin = new System.Windows.Forms.Padding(8);
			this.m_grid_sells.MultiSelect = false;
			this.m_grid_sells.Name = "m_grid_sells";
			this.m_grid_sells.ReadOnly = true;
			this.m_grid_sells.RowHeadersVisible = false;
			this.m_grid_sells.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_sells.Size = new System.Drawing.Size(246, 290);
			this.m_grid_sells.TabIndex = 0;
			// 
			// m_lbl_sell_orders
			// 
			this.m_lbl_sell_orders.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.m_lbl_sell_orders.AutoSize = true;
			this.m_lbl_sell_orders.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_sell_orders.Location = new System.Drawing.Point(264, 3);
			this.m_lbl_sell_orders.Name = "m_lbl_sell_orders";
			this.m_lbl_sell_orders.Size = new System.Drawing.Size(69, 13);
			this.m_lbl_sell_orders.TabIndex = 2;
			this.m_lbl_sell_orders.Text = "Sell Orders";
			// 
			// m_panel0
			// 
			this.m_panel0.Controls.Add(this.m_lbl_exch);
			this.m_panel0.Controls.Add(this.m_cb_exchange);
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
			this.m_panel0.Size = new System.Drawing.Size(529, 53);
			this.m_panel0.TabIndex = 2;
			// 
			// m_lbl_exch
			// 
			this.m_lbl_exch.AutoSize = true;
			this.m_lbl_exch.Location = new System.Drawing.Point(7, 9);
			this.m_lbl_exch.Name = "m_lbl_exch";
			this.m_lbl_exch.Size = new System.Drawing.Size(34, 13);
			this.m_lbl_exch.TabIndex = 7;
			this.m_lbl_exch.Text = "Exch:";
			// 
			// m_cb_exchange
			// 
			this.m_cb_exchange.BackColor = System.Drawing.Color.White;
			this.m_cb_exchange.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_exchange.BackColorValid = System.Drawing.Color.White;
			this.m_cb_exchange.CommitValueOnFocusLost = true;
			this.m_cb_exchange.DisplayProperty = null;
			this.m_cb_exchange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_exchange.ForeColor = System.Drawing.Color.Black;
			this.m_cb_exchange.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_exchange.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_exchange.FormattingEnabled = true;
			this.m_cb_exchange.Location = new System.Drawing.Point(45, 6);
			this.m_cb_exchange.Name = "m_cb_exchange";
			this.m_cb_exchange.PreserveSelectionThruFocusChange = false;
			this.m_cb_exchange.Size = new System.Drawing.Size(121, 21);
			this.m_cb_exchange.TabIndex = 6;
			this.m_cb_exchange.UseValidityColours = true;
			this.m_cb_exchange.Value = null;
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
			this.m_tb_exchange1.Size = new System.Drawing.Size(120, 20);
			this.m_tb_exchange1.TabIndex = 5;
			// 
			// m_tb_exchange0
			// 
			this.m_tb_exchange0.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_exchange0.Location = new System.Drawing.Point(264, 6);
			this.m_tb_exchange0.Name = "m_tb_exchange0";
			this.m_tb_exchange0.ReadOnly = true;
			this.m_tb_exchange0.Size = new System.Drawing.Size(120, 20);
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
			this.m_lbl_pair.Location = new System.Drawing.Point(13, 30);
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
			this.m_cb_pair.CommitValueOnFocusLost = true;
			this.m_cb_pair.DisplayProperty = null;
			this.m_cb_pair.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_pair.ForeColor = System.Drawing.Color.Black;
			this.m_cb_pair.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_pair.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_pair.FormattingEnabled = true;
			this.m_cb_pair.Location = new System.Drawing.Point(45, 27);
			this.m_cb_pair.Name = "m_cb_pair";
			this.m_cb_pair.PreserveSelectionThruFocusChange = false;
			this.m_cb_pair.Size = new System.Drawing.Size(121, 21);
			this.m_cb_pair.TabIndex = 1;
			this.m_cb_pair.UseValidityColours = true;
			this.m_cb_pair.Value = null;
			// 
			// PairsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(529, 385);
			this.Controls.Add(this.m_table0);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(412, 154);
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
