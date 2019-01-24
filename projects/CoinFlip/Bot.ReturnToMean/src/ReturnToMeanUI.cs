using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using CoinFlip;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Utility;
using ComboBox = Rylogic.Gui.WinForms.ComboBox;
using Util = Rylogic.Utility.Util;

namespace Bot.ReturnToMean
{
	public class ReturnToMeanUI :UserControl ,IDockable
	{
		#region UI Elements
		private Panel m_panel0;
		private ComboBox m_cb_pair;
		private ToolTip m_tt;
		private ValueBox m_tb_average_holding;
		private Label m_lbl_mean_holding;
		private ComboBox m_cb_exchange;
		private Label m_lbl_exchange;
		private ValueBox m_tb_holding_change;
		private Label m_lbl_holding_change;
		private ComboBox m_cb_coins;
		private Label m_lbl_hold_currency;
		private Label m_lbl_trade_pair;
		#endregion

		public ReturnToMeanUI(ReturnToMean bot)
		{
			InitializeComponent();
			Bot = bot;

			// Support for dock container controls
			DockControl = new DockControl(this, bot.Name)
			{
				TabText = bot.Name,
				DefaultDockLocation = new[]{ EDockSite.Bottom },
			};

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			DockControl = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The owning bot instance</summary>
		public ReturnToMean Bot
		{
			get { return m_bot; }
			private set
			{
				if (m_bot == value) return;
				if (m_bot != null)
				{
					m_bot.ActiveChanged -= HandleActiveChanged;
					m_bot.PropertyChanged -= HandleBotPropertyChanged;
					m_cb_pair.Value = None;
				}
				m_bot = value;
				if (m_bot != null)
				{
					m_cb_pair.Value = m_bot.Pair?.NameWithExchange ?? None;
					m_bot.PropertyChanged += HandleBotPropertyChanged;
					m_bot.ActiveChanged += HandleActiveChanged;
				}

				// Handlers
				void HandleActiveChanged(object sender, EventArgs e)
				{
					UpdateUI();
				}
				void HandleBotPropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					if (e.PropertyName == nameof(ReturnToMean.Pair))
					{
						m_cb_pair.SelectedItem = Bot.Pair?.NameWithExchange ?? None;
						UpdateUI();
					}
				}
			}
		}
		private ReturnToMean m_bot;

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockControl DockControl
		{
			[DebuggerStepThrough] get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				Util.Dispose(ref m_dock_control);
				m_dock_control = value;
			}
		}
		private DockControl m_dock_control;

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// Populate functions
			void PopulateExchangeCombo(object sender = null, EventArgs args = null)
			{
				using (m_cb_exchange.PreserveSelectedItem())
				{
					m_cb_exchange.Items.Clear();
					m_cb_exchange.Items.Add(None);
					m_cb_exchange.Items.AddRange(Bot.Model.TradingExchanges.Select(x => x.Name).OrderBy(x => x));
				}
			}
			void PopulatePairsCombo(object sender = null, EventArgs args = null)
			{
				using (m_cb_pair.PreserveSelectedItem())
				{
					m_cb_pair.Items.Clear();
					m_cb_pair.Items.Add(None);
					if (Bot.Exchange != null)
						m_cb_pair.Items.AddRange(Bot.Exchange.Pairs.Values.Select(x => x.Name).OrderBy(x => x));
				}
			}
			void PopulateCoinsCombo(object sender = null, EventArgs args = null)
			{
				using (m_cb_coins.PreserveSelectedItem())
				{
					m_cb_coins.Items.Clear();
					m_cb_coins.Items.Add(None);
					if (Bot.Pair != null)
						m_cb_coins.Items.AddRange(Bot.Pair.Coins.Select(x => x.Symbol).OrderBy(x => x));
				}
			}
			void PopulateHoldings(object sender = null, EventArgs args = null)
			{
				//m_tb_average_holding.TryCommitValue();
				//m_tb_holding_change.TryCommitValue();
			}

			#region Exchange Combo
			PopulateExchangeCombo();
			m_cb_exchange.ToolTip(m_tt, "The exchange to trade on");
			m_cb_exchange.ValueChanged += PopulatePairsCombo;
			m_cb_exchange.DropDown += PopulateExchangeCombo;
			m_cb_exchange.DropDownClosed += (s,a) =>
			{
				// Update the settings
				Bot.Settings.Exchange = (string)m_cb_exchange.SelectedItem;
				Bot.Settings.Pair = Bot.Pair?.Name ?? None;
				Bot.Settings.Currency = Bot.Coin?.Symbol ?? None;
				UpdateUI();
			};
			#endregion

			#region Pair Combo
			PopulatePairsCombo();
			m_cb_pair.ToolTip(m_tt, "The currency pair to trade");
			m_cb_pair.ValueChanged += PopulateCoinsCombo;
			m_cb_pair.DropDown += PopulatePairsCombo;
			m_cb_pair.DropDownClosed += (s,a) =>
			{
				// Update the settings
				Bot.Settings.Pair = (string)m_cb_pair.SelectedItem;
				Bot.Settings.Currency = Bot.Coin?.Symbol ?? None;
				UpdateUI();
			};
			#endregion

			#region Hold Currency
			PopulateCoinsCombo();
			m_cb_coins.ToolTip(m_tt, "The currency to hold");
			m_cb_coins.ValueChanged += PopulateHoldings;
			m_cb_coins.DropDown += PopulateCoinsCombo;
			m_cb_coins.DropDownClosed += (s,a) =>
			{
				// Update the settings
				Bot.Settings.Currency = (string)m_cb_coins.SelectedItem;
				UpdateUI();
			};
			#endregion

			#region Base Holding
			m_tb_average_holding.ToolTip(m_tt, "The amount to hold when price is at the average price level.\r\n(Use '%' to assign a percentage of the current balance)");
			m_tb_average_holding.ValueType = typeof(decimal);
			m_tb_average_holding.ValidateText = t =>
			{
				if (Bot.Coin == null) return false;
				var avail = Bot.Coin.Balances[Bot.Fund].Available;
				var vol = Misc.InterpretVolume(t, Bot.Coin, avail);
				return vol != null && vol.Value.WithinInclusive(0, avail);
			};
			m_tb_average_holding.TextToValue = t =>
			{
				if (Bot.Coin == null) return 0m;
				var avail = Bot.Coin.Balances[Bot.Fund].Available;
				var vol = Misc.InterpretVolume(t, Bot.Coin, avail);
				return (decimal)vol.Value;
			};
			m_tb_average_holding.ValueToText = v =>
			{
				if (Bot.Coin == null) return None;
				if ((decimal)v == 0m) return "0"; // 0 is currency agnostic
				return ((decimal)v)._(Bot.Coin).ToString("G8",true);
			};
			m_tb_average_holding.ValueCommitted += (s,a) =>
			{
				if (Bot.Coin == null) return;
				Bot.Settings.AverageHolding = (decimal)a.Value;
			};
			#endregion

			#region Holding Change
			m_tb_holding_change.ToolTip(m_tt, "The amount to increase or decrease the holding by.\r\n(Use '%' to assign a percentage of the current balance)");
			m_tb_holding_change.ValueType = typeof(decimal);
			m_tb_holding_change.ValidateText = t =>
			{
				if (Bot.Coin == null) return false;
				var avail = Bot.Coin.Balances[Bot.Fund].Available;
				var vol = Misc.InterpretVolume(t, Bot.Coin, avail);
				return
					vol != null &&
					(Bot.Settings.AverageHolding + vol.Value).WithinInclusive(0, avail) &&
					(Bot.Settings.AverageHolding - vol.Value).WithinInclusive(0, avail);
			};
			m_tb_holding_change.TextToValue = t =>
			{
				if (Bot.Coin == null) return 0m;
				var avail = Bot.Coin.Balances[Bot.Fund].Available;
				var vol = Misc.InterpretVolume(t, Bot.Coin, avail);
				return (decimal)vol.Value;
			};
			m_tb_holding_change.ValueToText = v =>
			{
				if (Bot.Coin == null) return None;
				if ((decimal)v == 0m) return "0"; // 0 is currency agnostic
				return ((decimal)v)._(Bot.Coin).ToString("G8",true);
			};
			m_tb_holding_change.ValueCommitted += (s,a) =>
			{
				if (Bot.Coin == null) return;
				Bot.Settings.HoldingChange = (decimal)a.Value;
			};
			#endregion
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			if (m_in_update_ui != 0) return;
			using (Scope.Create(() => ++m_in_update_ui, () => --m_in_update_ui))
			{
				m_cb_exchange.Enabled        = !Bot.Active;
				m_cb_pair.Enabled            = !Bot.Active;
				m_cb_coins.Enabled           = !Bot.Active;
				m_tb_average_holding.Enabled = !Bot.Active && Bot.Coin != null;
				m_tb_holding_change.Enabled  = !Bot.Active && Bot.Coin != null;

				m_cb_exchange.Value        = Bot.Settings.Exchange;
				m_cb_pair.Value            = Bot.Settings.Pair;
				m_cb_coins.Value           = Bot.Settings.Currency;
				m_tb_average_holding.Value = Bot.Settings.AverageHolding;
				m_tb_holding_change.Value  = Bot.Settings.HoldingChange;
			}
		}
		private int m_in_update_ui;

		private const string None = "---";

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_panel0 = new System.Windows.Forms.Panel();
			this.m_tb_holding_change = new Rylogic.Gui.WinForms.ValueBox();
			this.m_lbl_holding_change = new System.Windows.Forms.Label();
			this.m_cb_exchange = new Rylogic.Gui.WinForms.ComboBox();
			this.m_lbl_exchange = new System.Windows.Forms.Label();
			this.m_tb_average_holding = new Rylogic.Gui.WinForms.ValueBox();
			this.m_lbl_mean_holding = new System.Windows.Forms.Label();
			this.m_cb_pair = new Rylogic.Gui.WinForms.ComboBox();
			this.m_lbl_trade_pair = new System.Windows.Forms.Label();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_cb_coins = new Rylogic.Gui.WinForms.ComboBox();
			this.m_lbl_hold_currency = new System.Windows.Forms.Label();
			this.m_panel0.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_panel0
			// 
			this.m_panel0.Controls.Add(this.m_cb_coins);
			this.m_panel0.Controls.Add(this.m_lbl_hold_currency);
			this.m_panel0.Controls.Add(this.m_tb_holding_change);
			this.m_panel0.Controls.Add(this.m_lbl_holding_change);
			this.m_panel0.Controls.Add(this.m_cb_exchange);
			this.m_panel0.Controls.Add(this.m_lbl_exchange);
			this.m_panel0.Controls.Add(this.m_tb_average_holding);
			this.m_panel0.Controls.Add(this.m_lbl_mean_holding);
			this.m_panel0.Controls.Add(this.m_cb_pair);
			this.m_panel0.Controls.Add(this.m_lbl_trade_pair);
			this.m_panel0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panel0.Location = new System.Drawing.Point(0, 0);
			this.m_panel0.Name = "m_panel0";
			this.m_panel0.Size = new System.Drawing.Size(421, 192);
			this.m_panel0.TabIndex = 0;
			// 
			// m_tb_holding_change
			// 
			this.m_tb_holding_change.BackColor = System.Drawing.Color.White;
			this.m_tb_holding_change.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_holding_change.BackColorValid = System.Drawing.Color.White;
			this.m_tb_holding_change.CommitValueOnFocusLost = true;
			this.m_tb_holding_change.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_holding_change.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_holding_change.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_holding_change.Location = new System.Drawing.Point(98, 117);
			this.m_tb_holding_change.Name = "m_tb_holding_change";
			this.m_tb_holding_change.Size = new System.Drawing.Size(143, 20);
			this.m_tb_holding_change.TabIndex = 7;
			this.m_tb_holding_change.UseValidityColours = true;
			this.m_tb_holding_change.Value = null;
			// 
			// m_lbl_holding_change
			// 
			this.m_lbl_holding_change.AutoSize = true;
			this.m_lbl_holding_change.Location = new System.Drawing.Point(6, 120);
			this.m_lbl_holding_change.Name = "m_lbl_holding_change";
			this.m_lbl_holding_change.Size = new System.Drawing.Size(86, 13);
			this.m_lbl_holding_change.TabIndex = 6;
			this.m_lbl_holding_change.Text = "Holding Change:";
			// 
			// m_cb_exchange
			// 
			this.m_cb_exchange.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_exchange.BackColorValid = System.Drawing.Color.White;
			this.m_cb_exchange.CommitValueOnFocusLost = true;
			this.m_cb_exchange.DisplayProperty = null;
			this.m_cb_exchange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_exchange.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_exchange.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_exchange.FormattingEnabled = true;
			this.m_cb_exchange.Location = new System.Drawing.Point(98, 9);
			this.m_cb_exchange.Name = "m_cb_exchange";
			this.m_cb_exchange.PreserveSelectionThruFocusChange = false;
			this.m_cb_exchange.Size = new System.Drawing.Size(143, 21);
			this.m_cb_exchange.TabIndex = 5;
			this.m_cb_exchange.UseValidityColours = true;
			this.m_cb_exchange.Value = null;
			// 
			// m_lbl_exchange
			// 
			this.m_lbl_exchange.AutoSize = true;
			this.m_lbl_exchange.Location = new System.Drawing.Point(34, 12);
			this.m_lbl_exchange.Name = "m_lbl_exchange";
			this.m_lbl_exchange.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_exchange.TabIndex = 4;
			this.m_lbl_exchange.Text = "Exchange:";
			// 
			// m_tb_average_holding
			// 
			this.m_tb_average_holding.BackColor = System.Drawing.Color.White;
			this.m_tb_average_holding.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_average_holding.BackColorValid = System.Drawing.Color.White;
			this.m_tb_average_holding.CommitValueOnFocusLost = true;
			this.m_tb_average_holding.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_average_holding.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_average_holding.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_average_holding.Location = new System.Drawing.Point(98, 91);
			this.m_tb_average_holding.Name = "m_tb_average_holding";
			this.m_tb_average_holding.Size = new System.Drawing.Size(143, 20);
			this.m_tb_average_holding.TabIndex = 3;
			this.m_tb_average_holding.UseValidityColours = true;
			this.m_tb_average_holding.Value = null;
			// 
			// m_lbl_mean_holding
			// 
			this.m_lbl_mean_holding.AutoSize = true;
			this.m_lbl_mean_holding.Location = new System.Drawing.Point(3, 94);
			this.m_lbl_mean_holding.Name = "m_lbl_mean_holding";
			this.m_lbl_mean_holding.Size = new System.Drawing.Size(89, 13);
			this.m_lbl_mean_holding.TabIndex = 2;
			this.m_lbl_mean_holding.Text = "Average Holding:";
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
			this.m_cb_pair.Location = new System.Drawing.Point(98, 36);
			this.m_cb_pair.Name = "m_cb_pair";
			this.m_cb_pair.PreserveSelectionThruFocusChange = false;
			this.m_cb_pair.Size = new System.Drawing.Size(143, 21);
			this.m_cb_pair.TabIndex = 1;
			this.m_cb_pair.UseValidityColours = true;
			this.m_cb_pair.Value = null;
			// 
			// m_lbl_trade_pair
			// 
			this.m_lbl_trade_pair.AutoSize = true;
			this.m_lbl_trade_pair.Location = new System.Drawing.Point(33, 39);
			this.m_lbl_trade_pair.Name = "m_lbl_trade_pair";
			this.m_lbl_trade_pair.Size = new System.Drawing.Size(59, 13);
			this.m_lbl_trade_pair.TabIndex = 0;
			this.m_lbl_trade_pair.Text = "Trade Pair:";
			// 
			// m_cb_hold_currency
			// 
			this.m_cb_coins.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_coins.BackColorValid = System.Drawing.Color.White;
			this.m_cb_coins.CommitValueOnFocusLost = true;
			this.m_cb_coins.DisplayProperty = null;
			this.m_cb_coins.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_coins.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_coins.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_coins.FormattingEnabled = true;
			this.m_cb_coins.Location = new System.Drawing.Point(98, 63);
			this.m_cb_coins.Name = "m_cb_hold_currency";
			this.m_cb_coins.PreserveSelectionThruFocusChange = false;
			this.m_cb_coins.Size = new System.Drawing.Size(143, 21);
			this.m_cb_coins.TabIndex = 9;
			this.m_cb_coins.UseValidityColours = true;
			this.m_cb_coins.Value = null;
			// 
			// m_lbl_hold_currency
			// 
			this.m_lbl_hold_currency.AutoSize = true;
			this.m_lbl_hold_currency.Location = new System.Drawing.Point(15, 66);
			this.m_lbl_hold_currency.Name = "m_lbl_hold_currency";
			this.m_lbl_hold_currency.Size = new System.Drawing.Size(77, 13);
			this.m_lbl_hold_currency.TabIndex = 8;
			this.m_lbl_hold_currency.Text = "Hold Currency:";
			// 
			// ReturnToMeanUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_panel0);
			this.Name = "ReturnToMeanUI";
			this.Size = new System.Drawing.Size(421, 192);
			this.m_panel0.ResumeLayout(false);
			this.m_panel0.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
