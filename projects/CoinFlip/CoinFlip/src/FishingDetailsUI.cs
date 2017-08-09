using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using pr.attrib;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public class FishingDetailsUI :UserControl, IDockable
	{
		#region UI Elements
		private LogUI m_log_ui;
		private TableLayoutPanel m_table0;
		private Panel m_panel0;
		private Label m_lbl_profit_ratio;
		private Label m_lbl_b2q_match;
		private TextBox m_tb_b2q_match_price;
		private TextBox m_tb_b2q_bait_price;
		private Label m_lbl_b2q_bait_price;
		private TextBox m_tb_b2q_match_trade;
		private Label m_lbl_b2q_match_trade;
		private Label m_lbl_b2q_bait_trade;
		private TextBox m_tb_b2q_bait_trade;
		private Panel panel1;
		private Label m_lbl_q2b_bait_trade;
		private TextBox m_tb_q2b_bait_trade;
		private TextBox m_tb_q2b_match_trade;
		private Label m_lbl_q2b_match_trade;
		private TextBox m_tb_q2b_bait_price;
		private Label m_lbl_q2b_bait_price;
		private TextBox m_tb_q2b_match_price;
		private Label m_lbl_q2b_match_price;
		private Label m_lbl_q2b_profit_ratio;
		private TextBox m_tb_q2b_profit_ratio;
		private TextBox m_tb_b2q_status;
		private TextBox m_tb_q2b_status;
		private TextBox m_tb_q2b_order_book_index;
		private Label m_lbl_q2b_order_book_index;
		private Label m_lbl_b2q_order_book_index;
		private TextBox m_tb_b2q_order_book_index;
		private TextBox m_tb_b2q_profit_ratio;
		#endregion

		public FishingDetailsUI(Fishing fisher)
		{
			InitializeComponent();
			Fisher = fisher;

			// Support for dock container controls
			DockControl = new DockControl(this, Fisher.Name)
			{
				TabText             = Fisher.Name,
				DefaultDockLocation = new DockContainer.DockLocation(address:new[]{EDockSite.Bottom}),
			};

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Fisher = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The fisher instance whose details are displayed</summary>
		public Fishing Fisher
		{
			get { return m_fisher; }
			private set
			{
				if (m_fisher == value) return;
				if (m_fisher != null)
					m_fisher.Updated -= UpdateUI;
				m_fisher = value;
				if (m_fisher != null)
					m_fisher.Updated += UpdateUI;
			}
		}
		private Fishing m_fisher;

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockControl DockControl
		{
			[DebuggerStepThrough] get { return m_impl_dock_control; }
			private set
			{
				if (m_impl_dock_control == value) return;
				if (m_impl_dock_control != null)
				{
					Util.Dispose(ref m_impl_dock_control);
				}
				m_impl_dock_control = value;
				if (m_impl_dock_control != null)
				{
				}
			}
		}
		private DockControl m_impl_dock_control;

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// Log
			m_log_ui.LogFilepath = (Fisher.Log.LogCB as LogToFile).Filepath;
			m_log_ui.LogEntryPattern = Misc.LogEntryPattern;
			m_log_ui.Highlighting.AddRange(Misc.LogHighlighting);
			m_log_ui.PopOutOnNewMessages = false;

			// Remove the borders
			foreach (var tb in Controls.OfType(typeof(TextBox)).Cast<TextBox>())
				tb.BorderStyle = BorderStyle.None;
		}

		/// <summary>Update UI values</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			#region B2Q
			{
				var fisher = Fisher.BaitB2Q;
				if (fisher != null)
				{
					var price0 = fisher.Trade0.PriceQ2B;
					var price1 = fisher.Trade1.PriceQ2B;
					var ratio = Math.Abs(1m - Maths.Div(price1, price0));

					m_tb_b2q_match_trade.Text      = $"{fisher.Trade0.Description}";
					m_tb_b2q_bait_trade.Text       = $"{fisher.Trade1.Description}";
					m_tb_b2q_match_price.Text      = $"{price0.ToString("G6")} {fisher.Trade0.Pair.RateUnits}";
					m_tb_b2q_bait_price.Text       = $"{price1.ToString("G6")} {fisher.Trade1.Pair.RateUnits}";
					m_tb_b2q_profit_ratio.Text     = $"{100m*ratio:G4}%";
					m_tb_b2q_order_book_index.Text = $"{fisher.Trade1.OrderBookIndex}";
					m_tb_b2q_status.Text           = $"{fisher.Result}";
					m_tb_b2q_status.BackColor      = Color_.FromArgb(fisher.Result.Assoc<uint>("Color"));
				}
				else
				{
					m_tb_b2q_match_trade.Text      = string.Empty;
					m_tb_b2q_bait_trade.Text       = string.Empty;
					m_tb_b2q_match_price.Text      = string.Empty;
					m_tb_b2q_bait_price.Text       = string.Empty;
					m_tb_b2q_profit_ratio.Text     = string.Empty;
					m_tb_b2q_order_book_index.Text = string.Empty;
					m_tb_b2q_status.Text           = "No Trade";
					m_tb_b2q_status.BackColor      = Color_.FromArgb(Fishing.FishingTrade.EResult.Unknown.Assoc<uint>("Color"));
				}
			}
			#endregion
			#region Q2B
			{
				var fisher = Fisher.BaitQ2B;
				if (fisher != null)
				{
					var price0 = fisher.Trade0.PriceQ2B;
					var price1 = fisher.Trade1.PriceQ2B;
					var ratio = Math.Abs(1m - Maths.Div(price1, price0));

					m_tb_q2b_match_trade.Text      = $"{fisher.Trade0.Description}";
					m_tb_q2b_bait_trade.Text       = $"{fisher.Trade1.Description}";
					m_tb_q2b_match_price.Text      = $"{price0.ToString("G6")} {fisher.Trade0.Pair.RateUnits}";
					m_tb_q2b_bait_price.Text       = $"{price1.ToString("G6")} {fisher.Trade1.Pair.RateUnits}";
					m_tb_q2b_profit_ratio.Text     = $"{100m*ratio:G4}%";
					m_tb_q2b_order_book_index.Text = $"{fisher.Trade1.OrderBookIndex}";
					m_tb_q2b_status.Text           = $"{fisher.Result}";
					m_tb_q2b_status.BackColor      = Color_.FromArgb(fisher.Result.Assoc<uint>("Color"));
				}
				else
				{
					m_tb_q2b_match_trade.Text      = string.Empty;
					m_tb_q2b_bait_trade.Text       = string.Empty;
					m_tb_q2b_match_price.Text      = string.Empty;
					m_tb_q2b_bait_price.Text       = string.Empty;
					m_tb_q2b_profit_ratio.Text     = string.Empty;
					m_tb_q2b_order_book_index.Text = string.Empty;
					m_tb_q2b_status.Text           = "No Trade";
					m_tb_q2b_status.BackColor      = Color_.FromArgb(Fishing.FishingTrade.EResult.Unknown.Assoc<uint>("Color"));
				}
			}
			#endregion
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_log_ui = new pr.gui.LogUI();
			this.m_table0 = new System.Windows.Forms.TableLayoutPanel();
			this.panel1 = new System.Windows.Forms.Panel();
			this.m_tb_q2b_status = new System.Windows.Forms.TextBox();
			this.m_lbl_q2b_bait_trade = new System.Windows.Forms.Label();
			this.m_tb_q2b_bait_trade = new System.Windows.Forms.TextBox();
			this.m_tb_q2b_match_trade = new System.Windows.Forms.TextBox();
			this.m_lbl_q2b_match_trade = new System.Windows.Forms.Label();
			this.m_tb_q2b_bait_price = new System.Windows.Forms.TextBox();
			this.m_lbl_q2b_bait_price = new System.Windows.Forms.Label();
			this.m_tb_q2b_match_price = new System.Windows.Forms.TextBox();
			this.m_lbl_q2b_match_price = new System.Windows.Forms.Label();
			this.m_lbl_q2b_profit_ratio = new System.Windows.Forms.Label();
			this.m_tb_q2b_profit_ratio = new System.Windows.Forms.TextBox();
			this.m_panel0 = new System.Windows.Forms.Panel();
			this.m_tb_b2q_status = new System.Windows.Forms.TextBox();
			this.m_lbl_b2q_bait_trade = new System.Windows.Forms.Label();
			this.m_tb_b2q_bait_trade = new System.Windows.Forms.TextBox();
			this.m_tb_b2q_match_trade = new System.Windows.Forms.TextBox();
			this.m_lbl_b2q_match_trade = new System.Windows.Forms.Label();
			this.m_tb_b2q_bait_price = new System.Windows.Forms.TextBox();
			this.m_lbl_b2q_bait_price = new System.Windows.Forms.Label();
			this.m_tb_b2q_match_price = new System.Windows.Forms.TextBox();
			this.m_lbl_b2q_match = new System.Windows.Forms.Label();
			this.m_lbl_profit_ratio = new System.Windows.Forms.Label();
			this.m_tb_b2q_profit_ratio = new System.Windows.Forms.TextBox();
			this.m_tb_b2q_order_book_index = new System.Windows.Forms.TextBox();
			this.m_lbl_b2q_order_book_index = new System.Windows.Forms.Label();
			this.m_lbl_q2b_order_book_index = new System.Windows.Forms.Label();
			this.m_tb_q2b_order_book_index = new System.Windows.Forms.TextBox();
			this.m_table0.SuspendLayout();
			this.panel1.SuspendLayout();
			this.m_panel0.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_log_ui
			// 
			this.m_table0.SetColumnSpan(this.m_log_ui, 2);
			this.m_log_ui.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_log_ui.Location = new System.Drawing.Point(3, 170);
			this.m_log_ui.Name = "m_log_ui";
			this.m_log_ui.PopOutOnNewMessages = true;
			this.m_log_ui.Size = new System.Drawing.Size(643, 325);
			this.m_log_ui.TabIndex = 0;
			this.m_log_ui.Title = "Log";
			// 
			// m_table0
			// 
			this.m_table0.ColumnCount = 2;
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table0.Controls.Add(this.panel1, 1, 0);
			this.m_table0.Controls.Add(this.m_panel0, 0, 0);
			this.m_table0.Controls.Add(this.m_log_ui, 0, 1);
			this.m_table0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table0.Location = new System.Drawing.Point(0, 0);
			this.m_table0.Name = "m_table0";
			this.m_table0.RowCount = 2;
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.m_table0.Size = new System.Drawing.Size(649, 498);
			this.m_table0.TabIndex = 1;
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.BackColor = System.Drawing.SystemColors.Window;
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.m_tb_q2b_order_book_index);
			this.panel1.Controls.Add(this.m_lbl_q2b_order_book_index);
			this.panel1.Controls.Add(this.m_tb_q2b_status);
			this.panel1.Controls.Add(this.m_lbl_q2b_bait_trade);
			this.panel1.Controls.Add(this.m_tb_q2b_bait_trade);
			this.panel1.Controls.Add(this.m_tb_q2b_match_trade);
			this.panel1.Controls.Add(this.m_lbl_q2b_match_trade);
			this.panel1.Controls.Add(this.m_tb_q2b_bait_price);
			this.panel1.Controls.Add(this.m_lbl_q2b_bait_price);
			this.panel1.Controls.Add(this.m_tb_q2b_match_price);
			this.panel1.Controls.Add(this.m_lbl_q2b_match_price);
			this.panel1.Controls.Add(this.m_lbl_q2b_profit_ratio);
			this.panel1.Controls.Add(this.m_tb_q2b_profit_ratio);
			this.panel1.Location = new System.Drawing.Point(324, 0);
			this.panel1.Margin = new System.Windows.Forms.Padding(0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(325, 167);
			this.panel1.TabIndex = 1;
			// 
			// m_tb_q2b_status
			// 
			this.m_tb_q2b_status.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_q2b_status.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_q2b_status.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_q2b_status.Location = new System.Drawing.Point(77, 142);
			this.m_tb_q2b_status.Name = "m_tb_q2b_status";
			this.m_tb_q2b_status.ReadOnly = true;
			this.m_tb_q2b_status.Size = new System.Drawing.Size(237, 20);
			this.m_tb_q2b_status.TabIndex = 11;
			this.m_tb_q2b_status.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// m_lbl_q2b_bait_trade
			// 
			this.m_lbl_q2b_bait_trade.AutoSize = true;
			this.m_lbl_q2b_bait_trade.Location = new System.Drawing.Point(12, 32);
			this.m_lbl_q2b_bait_trade.Name = "m_lbl_q2b_bait_trade";
			this.m_lbl_q2b_bait_trade.Size = new System.Drawing.Size(59, 13);
			this.m_lbl_q2b_bait_trade.TabIndex = 9;
			this.m_lbl_q2b_bait_trade.Text = "Bait Trade:";
			// 
			// m_tb_q2b_bait_trade
			// 
			this.m_tb_q2b_bait_trade.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_q2b_bait_trade.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_q2b_bait_trade.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_q2b_bait_trade.Location = new System.Drawing.Point(77, 30);
			this.m_tb_q2b_bait_trade.Name = "m_tb_q2b_bait_trade";
			this.m_tb_q2b_bait_trade.ReadOnly = true;
			this.m_tb_q2b_bait_trade.Size = new System.Drawing.Size(237, 20);
			this.m_tb_q2b_bait_trade.TabIndex = 8;
			// 
			// m_tb_q2b_match_trade
			// 
			this.m_tb_q2b_match_trade.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_q2b_match_trade.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_q2b_match_trade.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_q2b_match_trade.Location = new System.Drawing.Point(77, 4);
			this.m_tb_q2b_match_trade.Name = "m_tb_q2b_match_trade";
			this.m_tb_q2b_match_trade.ReadOnly = true;
			this.m_tb_q2b_match_trade.Size = new System.Drawing.Size(237, 20);
			this.m_tb_q2b_match_trade.TabIndex = 7;
			// 
			// m_lbl_q2b_match_trade
			// 
			this.m_lbl_q2b_match_trade.AutoSize = true;
			this.m_lbl_q2b_match_trade.Location = new System.Drawing.Point(0, 6);
			this.m_lbl_q2b_match_trade.Name = "m_lbl_q2b_match_trade";
			this.m_lbl_q2b_match_trade.Size = new System.Drawing.Size(71, 13);
			this.m_lbl_q2b_match_trade.TabIndex = 6;
			this.m_lbl_q2b_match_trade.Text = "Match Trade:";
			// 
			// m_tb_q2b_bait_price
			// 
			this.m_tb_q2b_bait_price.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_q2b_bait_price.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_q2b_bait_price.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_q2b_bait_price.Location = new System.Drawing.Point(77, 91);
			this.m_tb_q2b_bait_price.Name = "m_tb_q2b_bait_price";
			this.m_tb_q2b_bait_price.ReadOnly = true;
			this.m_tb_q2b_bait_price.Size = new System.Drawing.Size(237, 20);
			this.m_tb_q2b_bait_price.TabIndex = 5;
			// 
			// m_lbl_q2b_bait_price
			// 
			this.m_lbl_q2b_bait_price.AutoSize = true;
			this.m_lbl_q2b_bait_price.Location = new System.Drawing.Point(16, 93);
			this.m_lbl_q2b_bait_price.Name = "m_lbl_q2b_bait_price";
			this.m_lbl_q2b_bait_price.Size = new System.Drawing.Size(55, 13);
			this.m_lbl_q2b_bait_price.TabIndex = 4;
			this.m_lbl_q2b_bait_price.Text = "Bait Price:";
			// 
			// m_tb_q2b_match_price
			// 
			this.m_tb_q2b_match_price.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_q2b_match_price.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_q2b_match_price.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_q2b_match_price.Location = new System.Drawing.Point(77, 65);
			this.m_tb_q2b_match_price.Name = "m_tb_q2b_match_price";
			this.m_tb_q2b_match_price.ReadOnly = true;
			this.m_tb_q2b_match_price.Size = new System.Drawing.Size(237, 20);
			this.m_tb_q2b_match_price.TabIndex = 3;
			// 
			// m_lbl_q2b_match_price
			// 
			this.m_lbl_q2b_match_price.AutoSize = true;
			this.m_lbl_q2b_match_price.Location = new System.Drawing.Point(4, 67);
			this.m_lbl_q2b_match_price.Name = "m_lbl_q2b_match_price";
			this.m_lbl_q2b_match_price.Size = new System.Drawing.Size(67, 13);
			this.m_lbl_q2b_match_price.TabIndex = 2;
			this.m_lbl_q2b_match_price.Text = "Match Price:";
			// 
			// m_lbl_q2b_profit_ratio
			// 
			this.m_lbl_q2b_profit_ratio.AutoSize = true;
			this.m_lbl_q2b_profit_ratio.Location = new System.Drawing.Point(9, 119);
			this.m_lbl_q2b_profit_ratio.Name = "m_lbl_q2b_profit_ratio";
			this.m_lbl_q2b_profit_ratio.Size = new System.Drawing.Size(62, 13);
			this.m_lbl_q2b_profit_ratio.TabIndex = 1;
			this.m_lbl_q2b_profit_ratio.Text = "Profit Ratio:";
			// 
			// m_tb_q2b_profit_ratio
			// 
			this.m_tb_q2b_profit_ratio.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_q2b_profit_ratio.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_q2b_profit_ratio.Location = new System.Drawing.Point(77, 117);
			this.m_tb_q2b_profit_ratio.Name = "m_tb_q2b_profit_ratio";
			this.m_tb_q2b_profit_ratio.ReadOnly = true;
			this.m_tb_q2b_profit_ratio.Size = new System.Drawing.Size(77, 20);
			this.m_tb_q2b_profit_ratio.TabIndex = 0;
			// 
			// m_panel0
			// 
			this.m_panel0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel0.BackColor = System.Drawing.SystemColors.Window;
			this.m_panel0.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_panel0.Controls.Add(this.m_lbl_b2q_order_book_index);
			this.m_panel0.Controls.Add(this.m_tb_b2q_order_book_index);
			this.m_panel0.Controls.Add(this.m_tb_b2q_status);
			this.m_panel0.Controls.Add(this.m_lbl_b2q_bait_trade);
			this.m_panel0.Controls.Add(this.m_tb_b2q_bait_trade);
			this.m_panel0.Controls.Add(this.m_tb_b2q_match_trade);
			this.m_panel0.Controls.Add(this.m_lbl_b2q_match_trade);
			this.m_panel0.Controls.Add(this.m_tb_b2q_bait_price);
			this.m_panel0.Controls.Add(this.m_lbl_b2q_bait_price);
			this.m_panel0.Controls.Add(this.m_tb_b2q_match_price);
			this.m_panel0.Controls.Add(this.m_lbl_b2q_match);
			this.m_panel0.Controls.Add(this.m_lbl_profit_ratio);
			this.m_panel0.Controls.Add(this.m_tb_b2q_profit_ratio);
			this.m_panel0.Location = new System.Drawing.Point(0, 0);
			this.m_panel0.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel0.Name = "m_panel0";
			this.m_panel0.Size = new System.Drawing.Size(324, 167);
			this.m_panel0.TabIndex = 0;
			// 
			// m_tb_b2q_status
			// 
			this.m_tb_b2q_status.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_b2q_status.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_b2q_status.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_b2q_status.Location = new System.Drawing.Point(77, 142);
			this.m_tb_b2q_status.Name = "m_tb_b2q_status";
			this.m_tb_b2q_status.ReadOnly = true;
			this.m_tb_b2q_status.Size = new System.Drawing.Size(236, 20);
			this.m_tb_b2q_status.TabIndex = 10;
			this.m_tb_b2q_status.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// m_lbl_b2q_bait_trade
			// 
			this.m_lbl_b2q_bait_trade.AutoSize = true;
			this.m_lbl_b2q_bait_trade.Location = new System.Drawing.Point(12, 32);
			this.m_lbl_b2q_bait_trade.Name = "m_lbl_b2q_bait_trade";
			this.m_lbl_b2q_bait_trade.Size = new System.Drawing.Size(59, 13);
			this.m_lbl_b2q_bait_trade.TabIndex = 9;
			this.m_lbl_b2q_bait_trade.Text = "Bait Trade:";
			// 
			// m_tb_b2q_bait_trade
			// 
			this.m_tb_b2q_bait_trade.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_b2q_bait_trade.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_b2q_bait_trade.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_b2q_bait_trade.Location = new System.Drawing.Point(77, 30);
			this.m_tb_b2q_bait_trade.Name = "m_tb_b2q_bait_trade";
			this.m_tb_b2q_bait_trade.ReadOnly = true;
			this.m_tb_b2q_bait_trade.Size = new System.Drawing.Size(236, 20);
			this.m_tb_b2q_bait_trade.TabIndex = 8;
			// 
			// m_tb_b2q_match_trade
			// 
			this.m_tb_b2q_match_trade.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_b2q_match_trade.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_b2q_match_trade.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_b2q_match_trade.Location = new System.Drawing.Point(77, 4);
			this.m_tb_b2q_match_trade.Name = "m_tb_b2q_match_trade";
			this.m_tb_b2q_match_trade.ReadOnly = true;
			this.m_tb_b2q_match_trade.Size = new System.Drawing.Size(236, 20);
			this.m_tb_b2q_match_trade.TabIndex = 7;
			// 
			// m_lbl_b2q_match_trade
			// 
			this.m_lbl_b2q_match_trade.AutoSize = true;
			this.m_lbl_b2q_match_trade.Location = new System.Drawing.Point(0, 6);
			this.m_lbl_b2q_match_trade.Name = "m_lbl_b2q_match_trade";
			this.m_lbl_b2q_match_trade.Size = new System.Drawing.Size(71, 13);
			this.m_lbl_b2q_match_trade.TabIndex = 6;
			this.m_lbl_b2q_match_trade.Text = "Match Trade:";
			// 
			// m_tb_b2q_bait_price
			// 
			this.m_tb_b2q_bait_price.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_b2q_bait_price.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_b2q_bait_price.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_b2q_bait_price.Location = new System.Drawing.Point(77, 91);
			this.m_tb_b2q_bait_price.Name = "m_tb_b2q_bait_price";
			this.m_tb_b2q_bait_price.ReadOnly = true;
			this.m_tb_b2q_bait_price.Size = new System.Drawing.Size(236, 20);
			this.m_tb_b2q_bait_price.TabIndex = 5;
			// 
			// m_lbl_b2q_bait_price
			// 
			this.m_lbl_b2q_bait_price.AutoSize = true;
			this.m_lbl_b2q_bait_price.Location = new System.Drawing.Point(16, 93);
			this.m_lbl_b2q_bait_price.Name = "m_lbl_b2q_bait_price";
			this.m_lbl_b2q_bait_price.Size = new System.Drawing.Size(55, 13);
			this.m_lbl_b2q_bait_price.TabIndex = 4;
			this.m_lbl_b2q_bait_price.Text = "Bait Price:";
			// 
			// m_tb_b2q_match_price
			// 
			this.m_tb_b2q_match_price.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_b2q_match_price.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_b2q_match_price.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_b2q_match_price.Location = new System.Drawing.Point(77, 65);
			this.m_tb_b2q_match_price.Name = "m_tb_b2q_match_price";
			this.m_tb_b2q_match_price.ReadOnly = true;
			this.m_tb_b2q_match_price.Size = new System.Drawing.Size(236, 20);
			this.m_tb_b2q_match_price.TabIndex = 3;
			// 
			// m_lbl_b2q_match
			// 
			this.m_lbl_b2q_match.AutoSize = true;
			this.m_lbl_b2q_match.Location = new System.Drawing.Point(4, 67);
			this.m_lbl_b2q_match.Name = "m_lbl_b2q_match";
			this.m_lbl_b2q_match.Size = new System.Drawing.Size(67, 13);
			this.m_lbl_b2q_match.TabIndex = 2;
			this.m_lbl_b2q_match.Text = "Match Price:";
			// 
			// m_lbl_profit_ratio
			// 
			this.m_lbl_profit_ratio.AutoSize = true;
			this.m_lbl_profit_ratio.Location = new System.Drawing.Point(9, 119);
			this.m_lbl_profit_ratio.Name = "m_lbl_profit_ratio";
			this.m_lbl_profit_ratio.Size = new System.Drawing.Size(62, 13);
			this.m_lbl_profit_ratio.TabIndex = 1;
			this.m_lbl_profit_ratio.Text = "Profit Ratio:";
			// 
			// m_tb_b2q_profit_ratio
			// 
			this.m_tb_b2q_profit_ratio.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_b2q_profit_ratio.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_b2q_profit_ratio.Location = new System.Drawing.Point(77, 117);
			this.m_tb_b2q_profit_ratio.Name = "m_tb_b2q_profit_ratio";
			this.m_tb_b2q_profit_ratio.ReadOnly = true;
			this.m_tb_b2q_profit_ratio.Size = new System.Drawing.Size(77, 20);
			this.m_tb_b2q_profit_ratio.TabIndex = 0;
			// 
			// m_tb_b2q_order_book_index
			// 
			this.m_tb_b2q_order_book_index.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_b2q_order_book_index.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_b2q_order_book_index.Location = new System.Drawing.Point(259, 116);
			this.m_tb_b2q_order_book_index.Name = "m_tb_b2q_order_book_index";
			this.m_tb_b2q_order_book_index.ReadOnly = true;
			this.m_tb_b2q_order_book_index.Size = new System.Drawing.Size(54, 20);
			this.m_tb_b2q_order_book_index.TabIndex = 11;
			// 
			// m_lbl_b2q_order_book_index
			// 
			this.m_lbl_b2q_order_book_index.AutoSize = true;
			this.m_lbl_b2q_order_book_index.Location = new System.Drawing.Point(160, 119);
			this.m_lbl_b2q_order_book_index.Name = "m_lbl_b2q_order_book_index";
			this.m_lbl_b2q_order_book_index.Size = new System.Drawing.Size(93, 13);
			this.m_lbl_b2q_order_book_index.TabIndex = 12;
			this.m_lbl_b2q_order_book_index.Text = "Order Book Index:";
			// 
			// m_lbl_q2b_order_book_index
			// 
			this.m_lbl_q2b_order_book_index.AutoSize = true;
			this.m_lbl_q2b_order_book_index.Location = new System.Drawing.Point(160, 119);
			this.m_lbl_q2b_order_book_index.Name = "m_lbl_q2b_order_book_index";
			this.m_lbl_q2b_order_book_index.Size = new System.Drawing.Size(93, 13);
			this.m_lbl_q2b_order_book_index.TabIndex = 13;
			this.m_lbl_q2b_order_book_index.Text = "Order Book Index:";
			// 
			// m_tb_q2b_order_book_index
			// 
			this.m_tb_q2b_order_book_index.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_q2b_order_book_index.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_q2b_order_book_index.Location = new System.Drawing.Point(259, 116);
			this.m_tb_q2b_order_book_index.Name = "m_tb_q2b_order_book_index";
			this.m_tb_q2b_order_book_index.ReadOnly = true;
			this.m_tb_q2b_order_book_index.Size = new System.Drawing.Size(54, 20);
			this.m_tb_q2b_order_book_index.TabIndex = 14;
			// 
			// FishingDetailsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_table0);
			this.Name = "FishingDetailsUI";
			this.Size = new System.Drawing.Size(649, 498);
			this.m_table0.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.m_panel0.ResumeLayout(false);
			this.m_panel0.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
