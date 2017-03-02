using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using cAlgo.API;
using pr.extn;
using pr.util;

namespace Rylobot
{
	public class RylobotUI :Form
	{
		#region UI Elements
		private Panel m_panel_risk;
		private NumericUpDown m_spinner_max_concurrent_trades;
		private Label m_lbl_risk_pc_per_trade;
		private Label m_lbl_total_risk_pc;
		private NumericUpDown m_spinner_risk_pc_total;
		private Label m_lbl_position_risk;
		private TextBox m_tb_pending_risk;
		private Label m_lbl_total_risk;
		private Label m_lbl_order_risk;
		private TextBox m_tb_total_risk;
		private TextBox m_tb_current_risk;
		private ToolTip m_tt;
		private System.Windows.Forms.Timer m_timer;
		#endregion

		public RylobotUI(Robot robit)
		{
			InitializeComponent();
			Model = new RylobotModel(robit);

			m_timer.Interval = 500;
			m_timer.Tick += (s,a) => Model.Step();
			m_timer.Enabled = true;
		}
		protected override void Dispose(bool disposing)
		{
			Model = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>App settings</summary>
		public Settings Settings
		{
			get { return Model.Settings; }
		}

		/// <summary>Application logic</summary>
		public RylobotModel Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.Acct.PropertyChanged -= UpdateUI;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Acct.PropertyChanged += UpdateUI;
				}
			}
		}
		private RylobotModel m_model;

		/// <summary>Set up controls</summary>
		private void SetupUI()
		{
			// Total risk
			m_tb_total_risk.ToolTip(m_tt, "Total sum of pending and current risk");

			// Current risk
			m_tb_current_risk.ToolTip(m_tt, "Total sum of risk on all active trades");

			// Pending risk
			m_tb_pending_risk.ToolTip(m_tt, "Total sum of risk on all pending trades");

			// Risk total
			m_spinner_risk_pc_total.ToolTip(m_tt, "The percentage of the account balance to have risked at any point in time");
			m_spinner_risk_pc_total.Minimum = 0;
			m_spinner_risk_pc_total.Maximum = 100;
			//m_spinner_risk_pc_total.Value = (decimal)(Settings.Trade.RiskFracTotal * 100.0);
			//m_spinner_risk_pc_total.ValueChanged += (s,a) =>
			//{
			//	if (m_updating_ui != 0) return;
			//	Settings.Trade.RiskFracTotal = (double)m_spinner_risk_pc_total.Value;
			//	UpdateUI();
			//};

			// Max concurrent trades
			m_spinner_max_concurrent_trades.ToolTip(m_tt, "The maximum number of trades to allow at any one time");
			m_spinner_max_concurrent_trades.Minimum = 1;
			m_spinner_max_concurrent_trades.Maximum = 100;
			//m_spinner_max_concurrent_trades.Value = Settings.Trade.MaxConcurrentTrades;
			//m_spinner_max_concurrent_trades.ValueChanged += (s,a) =>
			//{
			//	if (m_updating_ui != 0) return;
			//	Settings.Trade.MaxConcurrentTrades = (int)m_spinner_max_concurrent_trades.Value;
			//	UpdateUI();
			//};
		}

		/// <summary>Update the UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			using (Scope.Create(() => ++m_updating_ui, () => --m_updating_ui))
			{
				var acct = Model.Acct;

				//// Account Id
				//m_tb_acct_id.Text = "{0} - {1}".Fmt(Acct.IsLive ? "Live" : "Demo", Acct.AccountId);
				//m_tb_acct_id.ForeColor = Color.White;
				//m_tb_acct_id.BackColor = Acct.IsLive ? Color.Green : Color.Blue;

				//// Balance
				//m_tb_balance.Text = "{0:N2} {1}".Fmt(Acct.Balance, Acct.Currency);

				//// Equity
				//m_tb_equity.Text = "{0:N2} {1}".Fmt(Acct.Equity, Acct.Currency);

				// Total risk
				m_tb_total_risk.Text = "{0:N2}% ({1:N2} {2})".Fmt(acct.TotalRiskPC, acct.TotalRisk, acct.Currency);

				// Current risk
				m_tb_current_risk.Text = "{0:N2}% ({1:N2} {2})".Fmt(acct.CurrentRiskPC, acct.CurrentRisk, acct.Currency);

				// Pending risk
				m_tb_pending_risk.Text = "{0:N2}% ({1:N2} {2})".Fmt(acct.PendingRiskPC, acct.PendingRisk, acct.Currency);

				// Total risk percent
				//m_spinner_risk_pc_total.Value = (decimal)(Settings.Trade.RiskFracTotal * 100.0);

				// Max concurrent trades
				//m_spinner_max_concurrent_trades.Value = Settings.Trade.MaxConcurrentTrades;
			}
		}
		private int m_updating_ui;

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_timer = new System.Windows.Forms.Timer(this.components);
			this.m_panel_risk = new System.Windows.Forms.Panel();
			this.m_spinner_max_concurrent_trades = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_risk_pc_per_trade = new System.Windows.Forms.Label();
			this.m_lbl_total_risk_pc = new System.Windows.Forms.Label();
			this.m_spinner_risk_pc_total = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_position_risk = new System.Windows.Forms.Label();
			this.m_tb_pending_risk = new System.Windows.Forms.TextBox();
			this.m_lbl_total_risk = new System.Windows.Forms.Label();
			this.m_lbl_order_risk = new System.Windows.Forms.Label();
			this.m_tb_total_risk = new System.Windows.Forms.TextBox();
			this.m_tb_current_risk = new System.Windows.Forms.TextBox();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_panel_risk.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_max_concurrent_trades)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_risk_pc_total)).BeginInit();
			this.SuspendLayout();
			// 
			// m_panel_risk
			// 
			this.m_panel_risk.AutoScroll = true;
			this.m_panel_risk.BackColor = System.Drawing.SystemColors.Window;
			this.m_panel_risk.Controls.Add(this.m_spinner_max_concurrent_trades);
			this.m_panel_risk.Controls.Add(this.m_lbl_risk_pc_per_trade);
			this.m_panel_risk.Controls.Add(this.m_lbl_total_risk_pc);
			this.m_panel_risk.Controls.Add(this.m_spinner_risk_pc_total);
			this.m_panel_risk.Controls.Add(this.m_lbl_position_risk);
			this.m_panel_risk.Controls.Add(this.m_tb_pending_risk);
			this.m_panel_risk.Controls.Add(this.m_lbl_total_risk);
			this.m_panel_risk.Controls.Add(this.m_lbl_order_risk);
			this.m_panel_risk.Controls.Add(this.m_tb_total_risk);
			this.m_panel_risk.Controls.Add(this.m_tb_current_risk);
			this.m_panel_risk.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_panel_risk.Location = new System.Drawing.Point(0, 0);
			this.m_panel_risk.Name = "m_panel_risk";
			this.m_panel_risk.Size = new System.Drawing.Size(201, 137);
			this.m_panel_risk.TabIndex = 9;
			// 
			// m_spinner_max_concurrent_trades
			// 
			this.m_spinner_max_concurrent_trades.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_spinner_max_concurrent_trades.Location = new System.Drawing.Point(138, 108);
			this.m_spinner_max_concurrent_trades.Name = "m_spinner_max_concurrent_trades";
			this.m_spinner_max_concurrent_trades.Size = new System.Drawing.Size(60, 20);
			this.m_spinner_max_concurrent_trades.TabIndex = 11;
			// 
			// m_lbl_risk_pc_per_trade
			// 
			this.m_lbl_risk_pc_per_trade.AutoSize = true;
			this.m_lbl_risk_pc_per_trade.Location = new System.Drawing.Point(11, 110);
			this.m_lbl_risk_pc_per_trade.Name = "m_lbl_risk_pc_per_trade";
			this.m_lbl_risk_pc_per_trade.Size = new System.Drawing.Size(121, 13);
			this.m_lbl_risk_pc_per_trade.TabIndex = 10;
			this.m_lbl_risk_pc_per_trade.Text = "Max Concurrent Trades:";
			// 
			// m_lbl_total_risk_pc
			// 
			this.m_lbl_total_risk_pc.AutoSize = true;
			this.m_lbl_total_risk_pc.Location = new System.Drawing.Point(35, 84);
			this.m_lbl_total_risk_pc.Name = "m_lbl_total_risk_pc";
			this.m_lbl_total_risk_pc.Size = new System.Drawing.Size(98, 13);
			this.m_lbl_total_risk_pc.TabIndex = 9;
			this.m_lbl_total_risk_pc.Text = "Total Risk Percent:";
			// 
			// m_spinner_risk_pc_total
			// 
			this.m_spinner_risk_pc_total.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_spinner_risk_pc_total.DecimalPlaces = 1;
			this.m_spinner_risk_pc_total.Location = new System.Drawing.Point(138, 82);
			this.m_spinner_risk_pc_total.Name = "m_spinner_risk_pc_total";
			this.m_spinner_risk_pc_total.Size = new System.Drawing.Size(60, 20);
			this.m_spinner_risk_pc_total.TabIndex = 8;
			// 
			// m_lbl_position_risk
			// 
			this.m_lbl_position_risk.AutoSize = true;
			this.m_lbl_position_risk.Location = new System.Drawing.Point(3, 33);
			this.m_lbl_position_risk.Name = "m_lbl_position_risk";
			this.m_lbl_position_risk.Size = new System.Drawing.Size(68, 13);
			this.m_lbl_position_risk.TabIndex = 4;
			this.m_lbl_position_risk.Text = "Current Risk:";
			// 
			// m_tb_pending_risk
			// 
			this.m_tb_pending_risk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_pending_risk.Location = new System.Drawing.Point(73, 56);
			this.m_tb_pending_risk.Name = "m_tb_pending_risk";
			this.m_tb_pending_risk.ReadOnly = true;
			this.m_tb_pending_risk.Size = new System.Drawing.Size(125, 20);
			this.m_tb_pending_risk.TabIndex = 7;
			// 
			// m_lbl_total_risk
			// 
			this.m_lbl_total_risk.AutoSize = true;
			this.m_lbl_total_risk.Location = new System.Drawing.Point(3, 7);
			this.m_lbl_total_risk.Name = "m_lbl_total_risk";
			this.m_lbl_total_risk.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_total_risk.TabIndex = 2;
			this.m_lbl_total_risk.Text = "Total Risk:";
			// 
			// m_lbl_order_risk
			// 
			this.m_lbl_order_risk.AutoSize = true;
			this.m_lbl_order_risk.Location = new System.Drawing.Point(0, 59);
			this.m_lbl_order_risk.Name = "m_lbl_order_risk";
			this.m_lbl_order_risk.Size = new System.Drawing.Size(73, 13);
			this.m_lbl_order_risk.TabIndex = 6;
			this.m_lbl_order_risk.Text = "Pending Risk:";
			// 
			// m_tb_total_risk
			// 
			this.m_tb_total_risk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_total_risk.Location = new System.Drawing.Point(73, 4);
			this.m_tb_total_risk.Name = "m_tb_total_risk";
			this.m_tb_total_risk.ReadOnly = true;
			this.m_tb_total_risk.Size = new System.Drawing.Size(125, 20);
			this.m_tb_total_risk.TabIndex = 3;
			// 
			// m_tb_current_risk
			// 
			this.m_tb_current_risk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_current_risk.Location = new System.Drawing.Point(73, 30);
			this.m_tb_current_risk.Name = "m_tb_current_risk";
			this.m_tb_current_risk.ReadOnly = true;
			this.m_tb_current_risk.Size = new System.Drawing.Size(125, 20);
			this.m_tb_current_risk.TabIndex = 5;
			// 
			// RylobotUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(201, 206);
			this.Controls.Add(this.m_panel_risk);
			this.Name = "RylobotUI";
			this.ShowIcon = false;
			this.Text = "Rylobot";
			this.m_panel_risk.ResumeLayout(false);
			this.m_panel_risk.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_max_concurrent_trades)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_risk_pc_total)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
