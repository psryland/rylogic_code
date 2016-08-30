using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.util;

namespace Tradee
{
	public partial class EditOrderUI :Form
	{
		#region UI Elements
		private Label m_lbl_edit_order;
		private RadioButton m_radio_pending;
		private RadioButton m_radio_immediate_order;
		private ComboBox m_cb_percent_of_balance;
		private Label m_lbl_percentage_balance;
		private Button m_btn_make_order;
		private pr.gui.CheckedGroupBox m_chkgrp_stop_loss;
		#endregion

		public EditOrderUI(Order order)
		{
			InitializeComponent();
			Order = order;
			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The order being created/modified</summary>
		private Order Order { get; set; }

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI()
		{
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditOrderUI));
			this.m_lbl_edit_order = new System.Windows.Forms.Label();
			this.m_radio_pending = new System.Windows.Forms.RadioButton();
			this.m_radio_immediate_order = new System.Windows.Forms.RadioButton();
			this.m_cb_percent_of_balance = new System.Windows.Forms.ComboBox();
			this.m_lbl_percentage_balance = new System.Windows.Forms.Label();
			this.m_btn_make_order = new System.Windows.Forms.Button();
			this.m_chkgrp_stop_loss = new pr.gui.CheckedGroupBox();
			this.m_chkgrp_stop_loss.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_lbl_edit_order
			// 
			this.m_lbl_edit_order.AutoSize = true;
			this.m_lbl_edit_order.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_edit_order.ForeColor = System.Drawing.Color.DarkRed;
			this.m_lbl_edit_order.Location = new System.Drawing.Point(12, 9);
			this.m_lbl_edit_order.Name = "m_lbl_edit_order";
			this.m_lbl_edit_order.Size = new System.Drawing.Size(178, 74);
			this.m_lbl_edit_order.TabIndex = 1;
			this.m_lbl_edit_order.Text = "Edit Market\r\nOrder";
			// 
			// m_radio_pending
			// 
			this.m_radio_pending.Appearance = System.Windows.Forms.Appearance.Button;
			this.m_radio_pending.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_radio_pending.Location = new System.Drawing.Point(359, 12);
			this.m_radio_pending.Name = "m_radio_pending";
			this.m_radio_pending.Size = new System.Drawing.Size(157, 61);
			this.m_radio_pending.TabIndex = 2;
			this.m_radio_pending.TabStop = true;
			this.m_radio_pending.Text = "Pending Order";
			this.m_radio_pending.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.m_radio_pending.UseVisualStyleBackColor = true;
			// 
			// m_radio_immediate_order
			// 
			this.m_radio_immediate_order.Appearance = System.Windows.Forms.Appearance.Button;
			this.m_radio_immediate_order.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_radio_immediate_order.Location = new System.Drawing.Point(196, 12);
			this.m_radio_immediate_order.Name = "m_radio_immediate_order";
			this.m_radio_immediate_order.Size = new System.Drawing.Size(157, 61);
			this.m_radio_immediate_order.TabIndex = 3;
			this.m_radio_immediate_order.TabStop = true;
			this.m_radio_immediate_order.Text = "Immediate Order";
			this.m_radio_immediate_order.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.m_radio_immediate_order.UseVisualStyleBackColor = true;
			// 
			// m_cb_percent_of_balance
			// 
			this.m_cb_percent_of_balance.FormattingEnabled = true;
			this.m_cb_percent_of_balance.Location = new System.Drawing.Point(123, 26);
			this.m_cb_percent_of_balance.Name = "m_cb_percent_of_balance";
			this.m_cb_percent_of_balance.Size = new System.Drawing.Size(121, 21);
			this.m_cb_percent_of_balance.TabIndex = 4;
			// 
			// m_lbl_percentage_balance
			// 
			this.m_lbl_percentage_balance.AutoSize = true;
			this.m_lbl_percentage_balance.Location = new System.Drawing.Point(6, 29);
			this.m_lbl_percentage_balance.Name = "m_lbl_percentage_balance";
			this.m_lbl_percentage_balance.Size = new System.Drawing.Size(101, 13);
			this.m_lbl_percentage_balance.TabIndex = 5;
			this.m_lbl_percentage_balance.Text = "Percent of Balance:";
			// 
			// m_btn_make_order
			// 
			this.m_btn_make_order.Location = new System.Drawing.Point(389, 382);
			this.m_btn_make_order.Name = "m_btn_make_order";
			this.m_btn_make_order.Size = new System.Drawing.Size(127, 36);
			this.m_btn_make_order.TabIndex = 6;
			this.m_btn_make_order.Text = "Make Order";
			this.m_btn_make_order.UseVisualStyleBackColor = true;
			// 
			// m_chkgrp_stop_loss
			// 
			this.m_chkgrp_stop_loss.Controls.Add(this.m_lbl_percentage_balance);
			this.m_chkgrp_stop_loss.Controls.Add(this.m_cb_percent_of_balance);
			this.m_chkgrp_stop_loss.Enabled = false;
			this.m_chkgrp_stop_loss.Location = new System.Drawing.Point(12, 86);
			this.m_chkgrp_stop_loss.Name = "m_chkgrp_stop_loss";
			this.m_chkgrp_stop_loss.Size = new System.Drawing.Size(276, 122);
			this.m_chkgrp_stop_loss.TabIndex = 8;
			this.m_chkgrp_stop_loss.TabStop = false;
			this.m_chkgrp_stop_loss.Text = "Stop Loss";
			// 
			// EditOrderUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(538, 430);
			this.Controls.Add(this.m_chkgrp_stop_loss);
			this.Controls.Add(this.m_btn_make_order);
			this.Controls.Add(this.m_radio_immediate_order);
			this.Controls.Add(this.m_radio_pending);
			this.Controls.Add(this.m_lbl_edit_order);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "EditOrderUI";
			this.Text = "Edit Order";
			this.m_chkgrp_stop_loss.ResumeLayout(false);
			this.m_chkgrp_stop_loss.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
