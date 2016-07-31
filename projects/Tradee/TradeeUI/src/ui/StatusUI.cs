using System.ComponentModel;
using System.Windows.Forms;
using pr.util;
using pr.gui;
using pr.extn;
using System;

namespace Tradee
{
	public class StatusUI :BaseUI
	{
		#region UI Elements
		private TextBox m_tb_total_risk;
		private TextBox m_tb_position_risk;
		private Label m_lbl_position_risk;
		private TextBox m_tb_order_risk;
		private Label m_lbl_order_risk;
		private Panel m_panel_risk;
		private Label m_lbl_total_risk;
		#endregion

		public StatusUI(MainModel model)
			:base(model, "Status")
		{
			InitializeComponent();
			DockControl.DefaultDockLocation = new DockContainer.DockLocation(new[] { EDockSite.Left });
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}


		///// <summary>Handle total risk updates</summary>
		//private void HandleTotalRisk(TotalRisk risk)
		//{
		//	m_tb_total_risk   .Text = "{0} {1:N2}".Fmt(risk.CurrencySymbol, risk.Total);
		//	m_tb_position_risk.Text = "{0} {1:N2}".Fmt(risk.CurrencySymbol, risk.PositionRisk);
		//	m_tb_order_risk   .Text = "{0} {1:N2}".Fmt(risk.CurrencySymbol, risk.OrderRisk);
		//}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_tb_total_risk = new System.Windows.Forms.TextBox();
			this.m_lbl_total_risk = new System.Windows.Forms.Label();
			this.m_tb_position_risk = new System.Windows.Forms.TextBox();
			this.m_lbl_position_risk = new System.Windows.Forms.Label();
			this.m_tb_order_risk = new System.Windows.Forms.TextBox();
			this.m_lbl_order_risk = new System.Windows.Forms.Label();
			this.m_panel_risk = new System.Windows.Forms.Panel();
			this.m_panel_risk.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tb_total_risk
			// 
			this.m_tb_total_risk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_total_risk.Location = new System.Drawing.Point(132, 4);
			this.m_tb_total_risk.Name = "m_tb_total_risk";
			this.m_tb_total_risk.ReadOnly = true;
			this.m_tb_total_risk.Size = new System.Drawing.Size(42, 20);
			this.m_tb_total_risk.TabIndex = 3;
			// 
			// m_lbl_total_risk
			// 
			this.m_lbl_total_risk.AutoSize = true;
			this.m_lbl_total_risk.Location = new System.Drawing.Point(72, 7);
			this.m_lbl_total_risk.Name = "m_lbl_total_risk";
			this.m_lbl_total_risk.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_total_risk.TabIndex = 2;
			this.m_lbl_total_risk.Text = "Total Risk:";
			// 
			// m_tb_position_risk
			// 
			this.m_tb_position_risk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_position_risk.Location = new System.Drawing.Point(132, 30);
			this.m_tb_position_risk.Name = "m_tb_position_risk";
			this.m_tb_position_risk.ReadOnly = true;
			this.m_tb_position_risk.Size = new System.Drawing.Size(42, 20);
			this.m_tb_position_risk.TabIndex = 5;
			// 
			// m_lbl_position_risk
			// 
			this.m_lbl_position_risk.AutoSize = true;
			this.m_lbl_position_risk.Location = new System.Drawing.Point(3, 33);
			this.m_lbl_position_risk.Name = "m_lbl_position_risk";
			this.m_lbl_position_risk.Size = new System.Drawing.Size(126, 13);
			this.m_lbl_position_risk.TabIndex = 4;
			this.m_lbl_position_risk.Text = "Risk on current positions:";
			// 
			// m_tb_order_risk
			// 
			this.m_tb_order_risk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_order_risk.Location = new System.Drawing.Point(132, 56);
			this.m_tb_order_risk.Name = "m_tb_order_risk";
			this.m_tb_order_risk.ReadOnly = true;
			this.m_tb_order_risk.Size = new System.Drawing.Size(42, 20);
			this.m_tb_order_risk.TabIndex = 7;
			// 
			// m_lbl_order_risk
			// 
			this.m_lbl_order_risk.AutoSize = true;
			this.m_lbl_order_risk.Location = new System.Drawing.Point(3, 59);
			this.m_lbl_order_risk.Name = "m_lbl_order_risk";
			this.m_lbl_order_risk.Size = new System.Drawing.Size(119, 13);
			this.m_lbl_order_risk.TabIndex = 6;
			this.m_lbl_order_risk.Text = "Risk on pending orders:";
			// 
			// m_panel_risk
			// 
			this.m_panel_risk.AutoScroll = true;
			this.m_panel_risk.Controls.Add(this.m_lbl_position_risk);
			this.m_panel_risk.Controls.Add(this.m_tb_order_risk);
			this.m_panel_risk.Controls.Add(this.m_lbl_total_risk);
			this.m_panel_risk.Controls.Add(this.m_lbl_order_risk);
			this.m_panel_risk.Controls.Add(this.m_tb_total_risk);
			this.m_panel_risk.Controls.Add(this.m_tb_position_risk);
			this.m_panel_risk.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_panel_risk.Location = new System.Drawing.Point(0, 0);
			this.m_panel_risk.Name = "m_panel_risk";
			this.m_panel_risk.Size = new System.Drawing.Size(177, 91);
			this.m_panel_risk.TabIndex = 8;
			// 
			// StatusUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoScroll = true;
			this.Controls.Add(this.m_panel_risk);
			this.MinimumSize = new System.Drawing.Size(177, 0);
			this.Name = "StatusUI";
			this.Size = new System.Drawing.Size(177, 151);
			this.m_panel_risk.ResumeLayout(false);
			this.m_panel_risk.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
