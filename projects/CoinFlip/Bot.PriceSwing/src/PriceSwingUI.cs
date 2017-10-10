using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using CoinFlip;
using pr.extn;
using pr.gui;
using pr.util;
using ComboBox = pr.gui.ComboBox;
using DataGridView = pr.gui.DataGridView;

namespace Bot.PriceSwing
{
	public class PriceSwingUI :ToolForm
	{
		#region UI Elements
		private ToolTip m_tt;
		private DataGridView m_dgv_trade_record;
		private Label m_lbl_pair;
		private ComboBox m_cb_pair;
		private Label m_lbl_profit;
		private ValueBox m_tb_profit;
		private Label m_lbl_volume_frac;
		private ValueBox m_tb_volume_frac;
		private Button m_btn_ok;
		#endregion

		public PriceSwingUI(PriceSwing price_swing, Control parent)
			:base(parent, EPin.Centre)
		{
			InitializeComponent();
			HideOnClose = true;
			Bot = price_swing;

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The owning bot instance</summary>
		public PriceSwing Bot
		{
			get { return m_bot; }
			private set
			{
				if (m_bot == value) return;
				if (m_bot != null)
				{
					m_bot.PropertyChanged -= HandleBotPropertyChanged;
				}
				m_bot = value;
				if (m_bot != null)
				{
					m_bot.PropertyChanged += HandleBotPropertyChanged;
				}
			}
		}
		private PriceSwing m_bot;
		private void HandleBotPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(PriceSwing.Pair))
			{
				m_cb_pair.SelectedItem = Bot.Pair;
				UpdateUI();
			}
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Pair Combo
			m_cb_pair.ToolTip(m_tt, "The currency pair to trade, and the exchange to trade on");
			m_cb_pair.DisplayProperty = nameof(TradePair.NameWithExchange);
			m_cb_pair.DataSource = Bot.Model.Pairs;
			m_cb_pair.SelectedItem = Bot.Model.Pairs.FirstOrDefault(x => x.NameWithExchange == Bot.Settings.PairWithExchange);
			m_cb_pair.SelectedIndexChanged += (s,a) =>
			{
				Bot.Pair = (TradePair)m_cb_pair.SelectedItem;
			};
			#endregion

			#region Price Change
			m_tb_profit.ToolTip(m_tt, "The difference in price required to match a previous trade.");
			m_tb_profit.ValueType = typeof(decimal);
			m_tb_profit.Value = (decimal)Bot.Settings.PriceChange;
			m_tb_profit.ValidateText = t => decimal.TryParse(t, out var v) && v > 0;
			m_tb_profit.ValueCommitted += (s,a) =>
			{
				Bot.Settings.PriceChange = ((decimal)m_tb_profit.Value)._(Bot.Pair.RateUnits);
			};
			#endregion

			#region Volume Fraction
			m_tb_volume_frac.ToolTip(m_tt, "The percentage of available balance to use when creating new trades.");
			m_tb_volume_frac.ValueType = typeof(decimal);
			m_tb_volume_frac.Value = Bot.Settings.VolumeFrac * 100m;
			m_tb_volume_frac.ValidateText = t => decimal.TryParse(t, out var v) && v > 0 && v <= 100;
			m_tb_volume_frac.ValueCommitted += (s,a) =>
			{
				Bot.Settings.VolumeFrac = (decimal)m_tb_volume_frac.Value * 0.01m;
			};
			#endregion

			#region Trade Record Grid
			m_dgv_trade_record.ToolTip(m_tt, "Earlier trades. The bot is waiting for the price to move so that matched trades can be made resulting in profit");
			m_dgv_trade_record.AutoGenerateColumns = false;
			m_dgv_trade_record.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Trade Type",
				Name = nameof(TradeRecord.TradeType),
				DataPropertyName = nameof(TradeRecord.TradeType),
				FillWeight = 1.0f,
			});
			m_dgv_trade_record.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Price",
				Name = nameof(TradeRecord.PriceQ2B),
				DataPropertyName = nameof(TradeRecord.PriceQ2B),
				FillWeight = 1.0f,
			});
			m_dgv_trade_record.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Volume In",
				Name = nameof(TradeRecord.VolumeIn),
				DataPropertyName = nameof(TradeRecord.VolumeIn),
				FillWeight = 1.0f,
			});
			m_dgv_trade_record.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Volume Out",
				Name = nameof(TradeRecord.VolumeOut),
				DataPropertyName = nameof(TradeRecord.VolumeOut),
				FillWeight = 1.0f,
			});
			m_dgv_trade_record.CellFormatting += (s,a) =>
			{
				if (!m_dgv_trade_record.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col))
					return;

				var pair = Bot.Pair;
				var rec = Bot.TradeRecords[a.RowIndex];
				switch (col.Name)
				{
				case nameof(TradeRecord.TradeType):
					{
						a.Value = rec.TradeType == ETradeType.Q2B
							? $"{pair.Quote}→{pair.Base} ({rec.TradeType})"
							: $"{pair.Base}→{pair.Quote} ({rec.TradeType})";
						a.FormattingApplied = true;
						break;
					}
				case nameof(TradeRecord.PriceQ2B):
					{
						a.Value = rec.Price.ToString("G8", include_units:true);
						a.FormattingApplied = true;
						break;
					}
				case nameof(TradeRecord.VolumeIn):
					{
						a.Value = rec.VolumeIn.ToString("G8", include_units:true);
						a.FormattingApplied = true;
						break;
					}
				case nameof(TradeRecord.VolumeOut):
					{
						a.Value = rec.VolumeOut.ToString("G8", include_units:true);
						a.FormattingApplied = true;
						break;
					}
				}
			};
			m_dgv_trade_record.ColumnWidthChanged += DataGridView_.FitColumnsToDisplayWidth;
			m_dgv_trade_record.SizeChanged += DataGridView_.FitColumnsToDisplayWidth;
			m_dgv_trade_record.DataSource = Bot.TradeRecords;
			m_dgv_trade_record.SetGridColumnSizes(DataGridView_.EColumnSizeOptions.FitToDisplayWidth);
			#endregion
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_dgv_trade_record = new pr.gui.DataGridView();
			this.m_lbl_pair = new System.Windows.Forms.Label();
			this.m_cb_pair = new pr.gui.ComboBox();
			this.m_lbl_profit = new System.Windows.Forms.Label();
			this.m_tb_profit = new pr.gui.ValueBox();
			this.m_lbl_volume_frac = new System.Windows.Forms.Label();
			this.m_tb_volume_frac = new pr.gui.ValueBox();
			((System.ComponentModel.ISupportInitialize)(this.m_dgv_trade_record)).BeginInit();
			this.SuspendLayout();
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(452, 326);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 2;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_dgv_trade_record
			// 
			this.m_dgv_trade_record.AllowUserToAddRows = false;
			this.m_dgv_trade_record.AllowUserToDeleteRows = false;
			this.m_dgv_trade_record.AllowUserToOrderColumns = true;
			this.m_dgv_trade_record.AllowUserToResizeRows = false;
			this.m_dgv_trade_record.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_dgv_trade_record.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_dgv_trade_record.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_dgv_trade_record.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_dgv_trade_record.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_dgv_trade_record.Location = new System.Drawing.Point(12, 53);
			this.m_dgv_trade_record.Margin = new System.Windows.Forms.Padding(0);
			this.m_dgv_trade_record.Name = "m_dgv_trade_record";
			this.m_dgv_trade_record.ReadOnly = true;
			this.m_dgv_trade_record.RowHeadersVisible = false;
			this.m_dgv_trade_record.RowHeadersWidth = 28;
			this.m_dgv_trade_record.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_dgv_trade_record.Size = new System.Drawing.Size(515, 267);
			this.m_dgv_trade_record.TabIndex = 3;
			// 
			// m_lbl_pair
			// 
			this.m_lbl_pair.AutoSize = true;
			this.m_lbl_pair.Location = new System.Drawing.Point(24, 9);
			this.m_lbl_pair.Name = "m_lbl_pair";
			this.m_lbl_pair.Size = new System.Drawing.Size(59, 13);
			this.m_lbl_pair.TabIndex = 5;
			this.m_lbl_pair.Text = "Trade Pair:";
			// 
			// m_cb_pair
			// 
			this.m_cb_pair.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_pair.BackColorValid = System.Drawing.Color.White;
			this.m_cb_pair.CommitValueOnFocusLost = true;
			this.m_cb_pair.DisplayProperty = null;
			this.m_cb_pair.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_pair.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_pair.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_pair.FormattingEnabled = true;
			this.m_cb_pair.Location = new System.Drawing.Point(89, 6);
			this.m_cb_pair.Name = "m_cb_pair";
			this.m_cb_pair.PreserveSelectionThruFocusChange = false;
			this.m_cb_pair.Size = new System.Drawing.Size(204, 21);
			this.m_cb_pair.TabIndex = 6;
			this.m_cb_pair.UseValidityColours = true;
			this.m_cb_pair.Value = null;
			// 
			// m_lbl_profit
			// 
			this.m_lbl_profit.AutoSize = true;
			this.m_lbl_profit.Location = new System.Drawing.Point(9, 33);
			this.m_lbl_profit.Name = "m_lbl_profit";
			this.m_lbl_profit.Size = new System.Drawing.Size(74, 13);
			this.m_lbl_profit.TabIndex = 7;
			this.m_lbl_profit.Text = "Price Change:";
			// 
			// m_tb_profit
			// 
			this.m_tb_profit.BackColor = System.Drawing.Color.White;
			this.m_tb_profit.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_profit.BackColorValid = System.Drawing.Color.White;
			this.m_tb_profit.CommitValueOnFocusLost = true;
			this.m_tb_profit.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_profit.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_profit.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_profit.Location = new System.Drawing.Point(89, 30);
			this.m_tb_profit.Name = "m_tb_profit";
			this.m_tb_profit.Size = new System.Drawing.Size(68, 20);
			this.m_tb_profit.TabIndex = 8;
			this.m_tb_profit.UseValidityColours = true;
			this.m_tb_profit.Value = null;
			// 
			// m_lbl_volume_frac
			// 
			this.m_lbl_volume_frac.AutoSize = true;
			this.m_lbl_volume_frac.Location = new System.Drawing.Point(163, 33);
			this.m_lbl_volume_frac.Name = "m_lbl_volume_frac";
			this.m_lbl_volume_frac.Size = new System.Drawing.Size(62, 13);
			this.m_lbl_volume_frac.TabIndex = 9;
			this.m_lbl_volume_frac.Text = "Volume (%):";
			// 
			// m_tb_volume_frac
			// 
			this.m_tb_volume_frac.BackColor = System.Drawing.Color.White;
			this.m_tb_volume_frac.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_volume_frac.BackColorValid = System.Drawing.Color.White;
			this.m_tb_volume_frac.CommitValueOnFocusLost = true;
			this.m_tb_volume_frac.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_volume_frac.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_volume_frac.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_volume_frac.Location = new System.Drawing.Point(231, 30);
			this.m_tb_volume_frac.Name = "m_tb_volume_frac";
			this.m_tb_volume_frac.Size = new System.Drawing.Size(62, 20);
			this.m_tb_volume_frac.TabIndex = 10;
			this.m_tb_volume_frac.UseValidityColours = true;
			this.m_tb_volume_frac.Value = null;
			// 
			// PriceSwingUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(539, 361);
			this.Controls.Add(this.m_tb_volume_frac);
			this.Controls.Add(this.m_lbl_volume_frac);
			this.Controls.Add(this.m_tb_profit);
			this.Controls.Add(this.m_lbl_profit);
			this.Controls.Add(this.m_cb_pair);
			this.Controls.Add(this.m_lbl_pair);
			this.Controls.Add(this.m_dgv_trade_record);
			this.Controls.Add(this.m_btn_ok);
			this.MinimumSize = new System.Drawing.Size(322, 341);
			this.Name = "PriceSwingUI";
			this.Text = "Price Swing Bot";
			((System.ComponentModel.ISupportInitialize)(this.m_dgv_trade_record)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
