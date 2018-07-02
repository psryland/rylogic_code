using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Utility;

namespace Rylogic.VSExtension
{
	public class DonateUI : Form
	{
		#region UI Elements
		private PictureBox m_pic_qr;
		private Button m_btn_copy;
		private Button m_btn_ok;
		private Label m_lbl_desc;
		private PictureBox m_pic_logo;
		private Label m_lbl_status;
		private Button m_btn_prev;
		private ImageList m_il_buttons;
		private Button m_btn_next;
		private TableLayoutPanel m_table;
		private Panel m_panel_buttons;
		private Panel m_panel_address;
		private Panel m_panel_logo;
		private TextBox m_tb_address;
		#endregion

		public DonateUI(Control owner)
		{
			InitializeComponent();
			ShowIcon = false;
			ShowInTaskbar = false;
			MinimizeBox = false;
			MaximizeBox = false;

			// Initialise the available currencies
			AvailableCoins = new List<CoinData>();
			AvailableCoins.Add(new CoinData(Resources.bitcoin_logo, Resources.bitcoin_qr, "1PA5tuo9dBmsi3bHXi6J5mfAstheR5sr7Y", null));
			AvailableCoins.Add(new CoinData(Resources.ethereum_logo, Resources.ethereum_qr, "924721a628770dde8e54e2753289bd559dd13738", null));
			AvailableCoins.Add(new CoinData(Resources.paypal_donate_logo, null, null, PaypalDonateLink));
			CurrencyIndex = 0;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Get the currently selected currency</summary>
		private CoinData Coin
		{
			get { return AvailableCoins[CurrencyIndex]; }
		}

		/// <summary>The available currencies</summary>
		private List<CoinData> AvailableCoins { get; set; }

		/// <summary>The currently displayed currency</summary>
		private int CurrencyIndex
		{
			get { return m_currency_index; }
			set
			{
				if (m_currency_index == value) return;
				m_currency_index = Math.Min(Math.Max(value, 0), AvailableCoins.Count-1);
				var coin = AvailableCoins[m_currency_index];

				m_pic_logo.Image = coin.Logo;
				m_panel_logo.Visible = coin.Logo != null;

				m_pic_qr.Image = coin.QRCode;
				m_pic_qr.Visible = coin.QRCode != null;

				m_tb_address.Text = coin.Address;
				m_panel_address.Visible = coin.Address != null;

				m_lbl_status.Visible = false;

				ClientSize = m_table.PreferredSize;
			}
		}
		private int m_currency_index = -1;

		/// <summary>A link for making a PayPal donation</summary>
		private string PaypalDonateLink
		{
			get
			{
				var business    = "accounts@rylogic.co.nz"; // your paypal email
				var description = "Donation";               // '%20' represents a space. remember HTML!
				var country     = "NZ";                     // AU  , US, etc.
				var currency    = "NZD";                    // AUD , USD, etc.
				return "https://www.paypal.com/cgi-bin/webscr" +
					"?cmd=" + "_donations" +
					"&business=" + business +
					"&lc=" + country +
					"&item_name=" + description +
					"&currency_code=" + currency +
					"&bn=" + "PP%2dDonationsBF";
			}
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			m_pic_logo.Click += (s, a) =>
			{
				if (Coin.Link == null) return;
				Process.Start(Coin.Link);
			};
			m_btn_next.Click += (s, a) =>
			{
				CurrencyIndex = (CurrencyIndex + AvailableCoins.Count + 1) % AvailableCoins.Count;
			};
			m_btn_prev.Click += (s, a) =>
			{
				CurrencyIndex = (CurrencyIndex + AvailableCoins.Count - 1) % AvailableCoins.Count;
			};
			m_btn_copy.Click += (s, a) =>
			{
				Clipboard.SetData(DataFormats.Text, m_tb_address.Text);
				m_lbl_status.Visible = true;
			};
		}

		/// <summary>Donation currency</summary>
		private class CoinData
		{
			public CoinData(Image logo, Image qr_code, string address, string link)
			{
				Logo = logo;
				QRCode = qr_code;
				Address = address;
				Link = link;
			}

			public Image Logo { get; private set; }
			public Image QRCode { get; private set; }
			public string Address { get; private set; }
			public string Link { get; private set; }
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DonateUI));
			this.m_tb_address = new System.Windows.Forms.TextBox();
			this.m_btn_copy = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_lbl_desc = new System.Windows.Forms.Label();
			this.m_lbl_status = new System.Windows.Forms.Label();
			this.m_il_buttons = new System.Windows.Forms.ImageList(this.components);
			this.m_btn_next = new System.Windows.Forms.Button();
			this.m_btn_prev = new System.Windows.Forms.Button();
			this.m_pic_logo = new System.Windows.Forms.PictureBox();
			this.m_pic_qr = new System.Windows.Forms.PictureBox();
			this.m_table = new System.Windows.Forms.TableLayoutPanel();
			this.m_panel_buttons = new System.Windows.Forms.Panel();
			this.m_panel_address = new System.Windows.Forms.Panel();
			this.m_panel_logo = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.m_pic_logo)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_pic_qr)).BeginInit();
			this.m_table.SuspendLayout();
			this.m_panel_buttons.SuspendLayout();
			this.m_panel_address.SuspendLayout();
			this.m_panel_logo.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tb_address
			// 
			this.m_tb_address.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_address.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_tb_address.Location = new System.Drawing.Point(24, 3);
			this.m_tb_address.Name = "m_tb_address";
			this.m_tb_address.ReadOnly = true;
			this.m_tb_address.Size = new System.Drawing.Size(276, 23);
			this.m_tb_address.TabIndex = 1;
			this.m_tb_address.Text = "1PA5tuo9dBmsi3bHXi6J5mfAstheR5sr7Y";
			// 
			// m_btn_copy
			// 
			this.m_btn_copy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_copy.Location = new System.Drawing.Point(306, 3);
			this.m_btn_copy.Name = "m_btn_copy";
			this.m_btn_copy.Size = new System.Drawing.Size(47, 23);
			this.m_btn_copy.TabIndex = 2;
			this.m_btn_copy.Text = "Copy";
			this.m_btn_copy.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(270, 14);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 3;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_lbl_desc
			// 
			this.m_lbl_desc.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.m_lbl_desc.AutoSize = true;
			this.m_lbl_desc.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_desc.Location = new System.Drawing.Point(37, 0);
			this.m_lbl_desc.Name = "m_lbl_desc";
			this.m_lbl_desc.Size = new System.Drawing.Size(292, 48);
			this.m_lbl_desc.TabIndex = 4;
			this.m_lbl_desc.Text = "Say thanks with a donation!\r\nAny donations gratefully received.";
			this.m_lbl_desc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_status
			// 
			this.m_lbl_status.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.m_lbl_status.AutoSize = true;
			this.m_lbl_status.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
			this.m_lbl_status.Location = new System.Drawing.Point(131, 19);
			this.m_lbl_status.Name = "m_lbl_status";
			this.m_lbl_status.Size = new System.Drawing.Size(83, 13);
			this.m_lbl_status.TabIndex = 6;
			this.m_lbl_status.Text = "Address copied.";
			this.m_lbl_status.Visible = false;
			// 
			// m_il_buttons
			// 
			this.m_il_buttons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_il_buttons.ImageStream")));
			this.m_il_buttons.TransparentColor = System.Drawing.Color.Transparent;
			this.m_il_buttons.Images.SetKeyName(0, "green_up.png");
			this.m_il_buttons.Images.SetKeyName(1, "green_down.png");
			// 
			// m_btn_next
			// 
			this.m_btn_next.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_next.ImageKey = "green_down.png";
			this.m_btn_next.ImageList = this.m_il_buttons;
			this.m_btn_next.Location = new System.Drawing.Point(338, 38);
			this.m_btn_next.Name = "m_btn_next";
			this.m_btn_next.Size = new System.Drawing.Size(26, 29);
			this.m_btn_next.TabIndex = 8;
			this.m_btn_next.UseVisualStyleBackColor = true;
			// 
			// m_btn_prev
			// 
			this.m_btn_prev.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_prev.ImageKey = "green_up.png";
			this.m_btn_prev.ImageList = this.m_il_buttons;
			this.m_btn_prev.Location = new System.Drawing.Point(338, 3);
			this.m_btn_prev.Name = "m_btn_prev";
			this.m_btn_prev.Size = new System.Drawing.Size(26, 29);
			this.m_btn_prev.TabIndex = 7;
			this.m_btn_prev.UseVisualStyleBackColor = true;
			// 
			// m_pic_logo
			// 
			this.m_pic_logo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_pic_logo.Location = new System.Drawing.Point(3, 3);
			this.m_pic_logo.Name = "m_pic_logo";
			this.m_pic_logo.Size = new System.Drawing.Size(329, 78);
			this.m_pic_logo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.m_pic_logo.TabIndex = 5;
			this.m_pic_logo.TabStop = false;
			// 
			// m_pic_qr
			// 
			this.m_pic_qr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_pic_qr.Location = new System.Drawing.Point(3, 136);
			this.m_pic_qr.Name = "m_pic_qr";
			this.m_pic_qr.Size = new System.Drawing.Size(361, 133);
			this.m_pic_qr.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.m_pic_qr.TabIndex = 0;
			this.m_pic_qr.TabStop = false;
			// 
			// m_table
			// 
			this.m_table.ColumnCount = 1;
			this.m_table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table.Controls.Add(this.m_panel_buttons, 0, 5);
			this.m_table.Controls.Add(this.m_panel_address, 0, 3);
			this.m_table.Controls.Add(this.m_panel_logo, 0, 1);
			this.m_table.Controls.Add(this.m_pic_qr, 0, 2);
			this.m_table.Controls.Add(this.m_lbl_desc, 0, 0);
			this.m_table.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table.Location = new System.Drawing.Point(0, 0);
			this.m_table.Name = "m_table";
			this.m_table.RowCount = 6;
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.m_table.Size = new System.Drawing.Size(367, 364);
			this.m_table.TabIndex = 9;
			// 
			// m_panel_buttons
			// 
			this.m_panel_buttons.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_buttons.Controls.Add(this.m_btn_ok);
			this.m_panel_buttons.Controls.Add(this.m_lbl_status);
			this.m_panel_buttons.Location = new System.Drawing.Point(3, 308);
			this.m_panel_buttons.Name = "m_panel_buttons";
			this.m_panel_buttons.Size = new System.Drawing.Size(361, 53);
			this.m_panel_buttons.TabIndex = 10;
			// 
			// m_panel_address
			// 
			this.m_panel_address.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_address.Controls.Add(this.m_tb_address);
			this.m_panel_address.Controls.Add(this.m_btn_copy);
			this.m_panel_address.Location = new System.Drawing.Point(0, 272);
			this.m_panel_address.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_address.Name = "m_panel_address";
			this.m_panel_address.Size = new System.Drawing.Size(367, 30);
			this.m_panel_address.TabIndex = 10;
			// 
			// m_panel_logo
			// 
			this.m_panel_logo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_logo.Controls.Add(this.m_btn_prev);
			this.m_panel_logo.Controls.Add(this.m_pic_logo);
			this.m_panel_logo.Controls.Add(this.m_btn_next);
			this.m_panel_logo.Location = new System.Drawing.Point(0, 48);
			this.m_panel_logo.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_logo.Name = "m_panel_logo";
			this.m_panel_logo.Size = new System.Drawing.Size(367, 85);
			this.m_panel_logo.TabIndex = 11;
			// 
			// DonateUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(367, 364);
			this.Controls.Add(this.m_table);
			this.MinimumSize = new System.Drawing.Size(383, 251);
			this.Name = "DonateUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Donation";
			((System.ComponentModel.ISupportInitialize)(this.m_pic_logo)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_pic_qr)).EndInit();
			this.m_table.ResumeLayout(false);
			this.m_table.PerformLayout();
			this.m_panel_buttons.ResumeLayout(false);
			this.m_panel_buttons.PerformLayout();
			this.m_panel_address.ResumeLayout(false);
			this.m_panel_address.PerformLayout();
			this.m_panel_logo.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
