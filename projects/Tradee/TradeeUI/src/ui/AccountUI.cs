using System;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;

namespace Tradee
{
	public class AccountUI :BaseUI
	{
		#region UI Elements
		private TextBox m_tb_total_risk;
		private TextBox m_tb_current_risk;
		private Label m_lbl_position_risk;
		private TextBox m_tb_pending_risk;
		private Label m_lbl_order_risk;
		private Panel m_panel_risk;
		private Panel m_panel_acct;
		private Label m_lbl_balance;
		private TextBox m_tb_equity;
		private Label m_lbl_acct_id;
		private Label m_lbl_equity;
		private TextBox m_tb_acct_id;
		private TextBox m_tb_balance;
		private ToolTip m_tt;
		private Label m_lbl_total_risk;
		#endregion

		public AccountUI(MainModel model)
			:base(model, "Account")
		{
			InitializeComponent();
			DockControl.DefaultDockLocation = new DockContainer.DockLocation(new[] { EDockSite.Left });

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
				Model.Acct.AccountChanged -= UpdateUI;
				Model.Acct.PropertyChanged -= UpdateUI;
			}
			base.SetModelCore(model);
			if (Model != null)
			{
				Model.Acct.PropertyChanged += UpdateUI;
				Model.Acct.AccountChanged += UpdateUI;
			}
		}

		/// <summary>The account being displayed</summary>
		private AccountStatus Acct
		{
			get { return Model.Acct; }
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// Account Id
			m_tb_acct_id.ToolTip(m_tt, "Id of the account being traded");

			// Balance
			m_tb_balance.ToolTip(m_tt, "Current account balance");

			// Equity
			m_tb_equity.ToolTip(m_tt, "Current balance including unrealised profits and losses");

			///// <summary>The free margin of the current account.</summary>
			//public double FreeMargin { get; set; }

			///// <summary>True if the Account is Live, False if it is a Demo</summary>
			//public bool IsLive { get; set; }

			///// <summary>The account leverage</summary>
			//public int Leverage { get; set; }

			///// <summary>Represents the margin of the current account.</summary>
			//public double Margin { get; set; }

			///// <summary>
			///// Represents the margin level of the current account.
			///// Margin level (in %) is calculated using this formula: Equity / Margin * 100.</summary>
			//public double? MarginLevel { get; set; }

			///// <summary>Unrealised gross profit</summary>
			//public double UnrealizedGrossProfit { get; set; }

			///// <summary>Unrealised net profit</summary>
			//public double UnrealizedNetProfit { get; set; }

			// Total risk
			m_tb_total_risk.ToolTip(m_tt, "Total sum of pending and current risk");

			// Current risk
			m_tb_current_risk.ToolTip(m_tt, "Total sum of risk on all active trades");

			// Pending risk
			m_tb_pending_risk.ToolTip(m_tt, "Total sum of risk on all pending trades");
		}

		/// <summary>Update the UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			// Account Id
			m_tb_acct_id.Text = "{0} - {1}".Fmt(Acct.IsLive ? "Live" : "Demo", Acct.AccountId);
			m_tb_acct_id.ForeColor = Color.White;
			m_tb_acct_id.BackColor = Acct.IsLive ? Color.Green : Color.Blue;

			// Balance
			m_tb_balance.Text = "{0:N2} {1}".Fmt(Acct.Balance, Acct.Currency);

			// Equity
			m_tb_equity.Text = "{0:N2} {1}".Fmt(Acct.Equity, Acct.Currency);

			// Total risk
			m_tb_total_risk.Text = "{0:N2}% ({1:N2} {2})".Fmt(Acct.TotalRiskPC, Acct.TotalRisk, Acct.Currency);

			// Current risk
			m_tb_current_risk.Text = "{0:N2}% ({1:N2} {2})".Fmt(Acct.CurrentRiskPC, Acct.CurrentRisk, Acct.Currency);

			// Pending risk
			m_tb_pending_risk.Text = "{0:N2}% ({1:N2} {2})".Fmt(Acct.PendingRiskPC, Acct.PendingRisk, Acct.Currency);
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_tb_total_risk = new System.Windows.Forms.TextBox();
			this.m_lbl_total_risk = new System.Windows.Forms.Label();
			this.m_tb_current_risk = new System.Windows.Forms.TextBox();
			this.m_lbl_position_risk = new System.Windows.Forms.Label();
			this.m_tb_pending_risk = new System.Windows.Forms.TextBox();
			this.m_lbl_order_risk = new System.Windows.Forms.Label();
			this.m_panel_risk = new System.Windows.Forms.Panel();
			this.m_panel_acct = new System.Windows.Forms.Panel();
			this.m_lbl_balance = new System.Windows.Forms.Label();
			this.m_tb_equity = new System.Windows.Forms.TextBox();
			this.m_lbl_acct_id = new System.Windows.Forms.Label();
			this.m_lbl_equity = new System.Windows.Forms.Label();
			this.m_tb_acct_id = new System.Windows.Forms.TextBox();
			this.m_tb_balance = new System.Windows.Forms.TextBox();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_panel_risk.SuspendLayout();
			this.m_panel_acct.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tb_total_risk
			// 
			this.m_tb_total_risk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_total_risk.Location = new System.Drawing.Point(73, 4);
			this.m_tb_total_risk.Name = "m_tb_total_risk";
			this.m_tb_total_risk.ReadOnly = true;
			this.m_tb_total_risk.Size = new System.Drawing.Size(162, 20);
			this.m_tb_total_risk.TabIndex = 3;
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
			// m_tb_position_risk
			// 
			this.m_tb_current_risk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_current_risk.Location = new System.Drawing.Point(73, 30);
			this.m_tb_current_risk.Name = "m_tb_position_risk";
			this.m_tb_current_risk.ReadOnly = true;
			this.m_tb_current_risk.Size = new System.Drawing.Size(162, 20);
			this.m_tb_current_risk.TabIndex = 5;
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
			// m_tb_order_risk
			// 
			this.m_tb_pending_risk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_pending_risk.Location = new System.Drawing.Point(73, 56);
			this.m_tb_pending_risk.Name = "m_tb_order_risk";
			this.m_tb_pending_risk.ReadOnly = true;
			this.m_tb_pending_risk.Size = new System.Drawing.Size(162, 20);
			this.m_tb_pending_risk.TabIndex = 7;
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
			// m_panel_risk
			// 
			this.m_panel_risk.AutoScroll = true;
			this.m_panel_risk.BackColor = System.Drawing.SystemColors.Window;
			this.m_panel_risk.Controls.Add(this.m_lbl_position_risk);
			this.m_panel_risk.Controls.Add(this.m_tb_pending_risk);
			this.m_panel_risk.Controls.Add(this.m_lbl_total_risk);
			this.m_panel_risk.Controls.Add(this.m_lbl_order_risk);
			this.m_panel_risk.Controls.Add(this.m_tb_total_risk);
			this.m_panel_risk.Controls.Add(this.m_tb_current_risk);
			this.m_panel_risk.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_panel_risk.Location = new System.Drawing.Point(0, 121);
			this.m_panel_risk.Name = "m_panel_risk";
			this.m_panel_risk.Size = new System.Drawing.Size(238, 91);
			this.m_panel_risk.TabIndex = 8;
			// 
			// m_panel_acct
			// 
			this.m_panel_acct.AutoScroll = true;
			this.m_panel_acct.BackColor = System.Drawing.SystemColors.Window;
			this.m_panel_acct.Controls.Add(this.m_lbl_balance);
			this.m_panel_acct.Controls.Add(this.m_tb_equity);
			this.m_panel_acct.Controls.Add(this.m_lbl_acct_id);
			this.m_panel_acct.Controls.Add(this.m_lbl_equity);
			this.m_panel_acct.Controls.Add(this.m_tb_acct_id);
			this.m_panel_acct.Controls.Add(this.m_tb_balance);
			this.m_panel_acct.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_panel_acct.Location = new System.Drawing.Point(0, 0);
			this.m_panel_acct.Name = "m_panel_acct";
			this.m_panel_acct.Size = new System.Drawing.Size(238, 121);
			this.m_panel_acct.TabIndex = 9;
			// 
			// m_lbl_balance
			// 
			this.m_lbl_balance.AutoSize = true;
			this.m_lbl_balance.Location = new System.Drawing.Point(18, 33);
			this.m_lbl_balance.Name = "m_lbl_balance";
			this.m_lbl_balance.Size = new System.Drawing.Size(49, 13);
			this.m_lbl_balance.TabIndex = 4;
			this.m_lbl_balance.Text = "Balance:";
			// 
			// m_tb_equity
			// 
			this.m_tb_equity.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_equity.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_equity.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_equity.Location = new System.Drawing.Point(73, 56);
			this.m_tb_equity.Name = "m_tb_equity";
			this.m_tb_equity.ReadOnly = true;
			this.m_tb_equity.Size = new System.Drawing.Size(162, 20);
			this.m_tb_equity.TabIndex = 7;
			// 
			// m_lbl_acct_id
			// 
			this.m_lbl_acct_id.AutoSize = true;
			this.m_lbl_acct_id.Location = new System.Drawing.Point(3, 7);
			this.m_lbl_acct_id.Name = "m_lbl_acct_id";
			this.m_lbl_acct_id.Size = new System.Drawing.Size(64, 13);
			this.m_lbl_acct_id.TabIndex = 2;
			this.m_lbl_acct_id.Text = "Account ID:";
			// 
			// m_lbl_equity
			// 
			this.m_lbl_equity.AutoSize = true;
			this.m_lbl_equity.Location = new System.Drawing.Point(28, 58);
			this.m_lbl_equity.Name = "m_lbl_equity";
			this.m_lbl_equity.Size = new System.Drawing.Size(39, 13);
			this.m_lbl_equity.TabIndex = 6;
			this.m_lbl_equity.Text = "Equity:";
			// 
			// m_tb_acct_id
			// 
			this.m_tb_acct_id.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_acct_id.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_acct_id.Location = new System.Drawing.Point(73, 4);
			this.m_tb_acct_id.Name = "m_tb_acct_id";
			this.m_tb_acct_id.ReadOnly = true;
			this.m_tb_acct_id.Size = new System.Drawing.Size(162, 20);
			this.m_tb_acct_id.TabIndex = 3;
			// 
			// m_tb_balance
			// 
			this.m_tb_balance.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_balance.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_balance.Location = new System.Drawing.Point(73, 30);
			this.m_tb_balance.Name = "m_tb_balance";
			this.m_tb_balance.ReadOnly = true;
			this.m_tb_balance.Size = new System.Drawing.Size(162, 20);
			this.m_tb_balance.TabIndex = 5;
			// 
			// AccountUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoScroll = true;
			this.Controls.Add(this.m_panel_risk);
			this.Controls.Add(this.m_panel_acct);
			this.MinimumSize = new System.Drawing.Size(177, 0);
			this.Name = "AccountUI";
			this.Size = new System.Drawing.Size(238, 356);
			this.m_panel_risk.ResumeLayout(false);
			this.m_panel_risk.PerformLayout();
			this.m_panel_acct.ResumeLayout(false);
			this.m_panel_acct.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
