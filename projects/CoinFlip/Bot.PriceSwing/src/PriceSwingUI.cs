using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CoinFlip;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Utility;
using ComboBox = Rylogic.Gui.ComboBox;
using DataGridView = Rylogic.Gui.DataGridView;

namespace Bot.PriceSwing
{
	public class PriceSwingUI :UserControl ,IDockable
	{
		#region UI Elements
		private ToolTip m_tt;
		private DataGridView m_grid_trade_record;
		private Label m_lbl_pair;
		private ComboBox m_cb_pair;
		private Label m_lbl_profit;
		private ValueBox m_tb_profit;
		private Label m_lbl_volume_frac;
		private Panel m_panel0;
		private TableLayoutPanel m_table0;
		private ValueBox m_tb_volume_frac;
		#endregion

		public PriceSwingUI(PriceSwing bot)
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

			UpdateActive = true;
		}
		protected override void Dispose(bool disposing)
		{
			UpdateActive = false;
			DockControl = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			UpdateActive = Visible;
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
					m_bot.ActiveChanged -= HandleActiveChanged;
					m_bot.Model.PairsUpdated -= HandlePairsChanged;
					m_bot.PropertyChanged -= HandleBotPropertyChanged;
					m_cb_pair.Value = NoPair;
				}
				m_bot = value;
				if (m_bot != null)
				{
					m_cb_pair.Value = m_bot.Pair?.NameWithExchange ?? NoPair;
					m_bot.PropertyChanged += HandleBotPropertyChanged;
					m_bot.Model.PairsUpdated += HandlePairsChanged;
					m_bot.ActiveChanged += HandleActiveChanged;
				}

				// Handlers
				void HandleActiveChanged(object sender, EventArgs e)
				{
					UpdateUI();
				}
				void HandleBotPropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					if (e.PropertyName == nameof(PriceSwing.Pair))
					{
						m_cb_pair.SelectedItem = Bot.Pair?.NameWithExchange ?? NoPair;
						UpdateUI();
					}
				}
				void HandlePairsChanged(object sender, EventArgs e)
				{
					PopulatePairs();
				}
			}
		}
		private PriceSwing m_bot;

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

		/// <summary>A timer for UI updating independent of the bot</summary>
		public bool UpdateActive
		{
			get { return m_update_timer != null; }
			set
			{
				if (UpdateActive == value) return;
				if (UpdateActive)
				{
					m_update_timer.Stop();
					m_update_timer.Tick -= HandleTick;
					Util.Dispose(ref m_update_timer);
				}
				m_update_timer = value ? new Timer() : null;
				if (UpdateActive)
				{
					m_update_timer.Tick += HandleTick;
					m_update_timer.Interval = 100;
					m_update_timer.Start();
				}

				// Handlers
				void HandleTick(object sender, EventArgs args)
				{
					var idx = m_grid_trade_record.Columns[nameof(ColumnNames.Distance)].Index;
					m_grid_trade_record.InvalidateColumn(idx);
				}
			}
		}
		private Timer m_update_timer;

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Pair Combo
			PopulatePairs();
			m_cb_pair.ToolTip(m_tt, "The currency pair to trade, and the exchange to trade on");
			m_cb_pair.SelectedIndexChanged += (s,a) =>
			{
				var name = (string)m_cb_pair.SelectedItem;
				var pair = Bot.Model.Pairs.FirstOrDefault(x => x.NameWithExchange == name);
				Bot.Pair = pair;
			};
			#endregion

			#region Price Change
			m_tb_profit.ToolTip(m_tt, "The percentage change in price required to match a previous trade.");
			m_tb_profit.ValueType = typeof(double);
			m_tb_profit.Value = Bot.Settings.PriceChangePC;
			m_tb_profit.ValidateText = t => double.TryParse(t, out var v) && v > 0 && v <= 100;
			m_tb_profit.ValueCommitted += (s,a) =>
			{
				Bot.Settings.PriceChangePC = (double)m_tb_profit.Value;
			};
			#endregion

			#region Volume Fraction
			m_tb_volume_frac.ToolTip(m_tt, "The percentage of available balance to use when creating new trades.");
			m_tb_volume_frac.ValueType = typeof(double);
			m_tb_volume_frac.Value = Bot.Settings.VolumePC;
			m_tb_volume_frac.ValidateText = t => double.TryParse(t, out var v) && v > 0 && v <= 100;
			m_tb_volume_frac.ValueCommitted += (s,a) =>
			{
				Bot.Settings.VolumePC = (double)m_tb_volume_frac.Value;
			};
			#endregion

			#region Trade Record Grid
			m_grid_trade_record.ToolTip(m_tt, "Earlier trades. The bot is waiting for the price to move so that matched trades can be made resulting in profit");
			m_grid_trade_record.AutoGenerateColumns = false;
			m_grid_trade_record.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Trade Type",
				Name = nameof(TradeRecord.TradeType),
				DataPropertyName = nameof(TradeRecord.TradeType),
				FillWeight = 1.0f,
			});
			m_grid_trade_record.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Price",
				Name = nameof(TradeRecord.PriceQ2B),
				DataPropertyName = nameof(TradeRecord.PriceQ2B),
				FillWeight = 1.0f,
			});
			m_grid_trade_record.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Distance",
				Name = nameof(ColumnNames.Distance),
				DataPropertyName = nameof(ColumnNames.Distance),
				FillWeight = 1.0f,
			});
			m_grid_trade_record.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Volume In",
				Name = nameof(TradeRecord.VolumeIn),
				DataPropertyName = nameof(TradeRecord.VolumeIn),
				FillWeight = 1.0f,
			});
			m_grid_trade_record.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Volume Out",
				Name = nameof(TradeRecord.VolumeOut),
				DataPropertyName = nameof(TradeRecord.VolumeOut),
				FillWeight = 1.0f,
			});
			m_grid_trade_record.CellFormatting += (s,a) =>
			{
				if (!m_grid_trade_record.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col))
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
						a.Value = rec.PriceQ2B.ToString("G8", include_units:true);
						a.FormattingApplied = true;
						break;
					}
				case nameof(ColumnNames.Distance):
					{
						var spot = pair.SpotPrice(rec.TradeType.Opposite());
						if (spot != null)
						{
							var dist =
								rec.TradeType == ETradeType.Q2B ? spot.Value - rec.PriceQ2B :
								rec.TradeType == ETradeType.B2Q ? rec.PriceQ2B - spot.Value :
								0m._();
							var pc = 100 * Math.Abs(dist) / (decimal)rec.PriceQ2B;
							a.CellStyle.ForeColor = dist >= 0 ? Color.Green : Color.Red;
							a.Value = $"{dist.ToString("G8",false)} ({pc:G5}%)";
							a.FormattingApplied = true;
						}
						else
						{
							a.Value = "---";
							a.FormattingApplied = true;
						}
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
				a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
				a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
			};
			m_grid_trade_record.ColumnWidthChanged += DataGridView_.FitColumnsToDisplayWidth;
			m_grid_trade_record.SizeChanged += DataGridView_.FitColumnsToDisplayWidth;
			m_grid_trade_record.SetGridColumnSizes(DataGridView_.EColumnSizeOptions.FitToDisplayWidth);
			m_grid_trade_record.DataSource = Bot.TradeRecords;
			#endregion
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			m_cb_pair.Enabled = !Bot.Active;
		}

		/// <summary>Update the combo with the available pairs</summary>
		private void PopulatePairs()
		{
			// Populate the options
			m_cb_pair.Items.Clear();
			m_cb_pair.Items.Add(NoPair);
			foreach (var pair in Bot.Model.Pairs)
				m_cb_pair.Items.Add(pair.NameWithExchange);

			// Select the current pair
			m_cb_pair.SelectedItem = Bot.Settings.PairWithExchange;
		}

		private const string NoPair = "---";
		private static class ColumnNames
		{
			public const int Distance = 0;
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_grid_trade_record = new Rylogic.Gui.DataGridView();
			this.m_lbl_pair = new System.Windows.Forms.Label();
			this.m_cb_pair = new Rylogic.Gui.ComboBox();
			this.m_lbl_profit = new System.Windows.Forms.Label();
			this.m_tb_profit = new Rylogic.Gui.ValueBox();
			this.m_lbl_volume_frac = new System.Windows.Forms.Label();
			this.m_tb_volume_frac = new Rylogic.Gui.ValueBox();
			this.m_panel0 = new System.Windows.Forms.Panel();
			this.m_table0 = new System.Windows.Forms.TableLayoutPanel();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_trade_record)).BeginInit();
			this.m_panel0.SuspendLayout();
			this.m_table0.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_dgv_trade_record
			// 
			this.m_grid_trade_record.AllowUserToAddRows = false;
			this.m_grid_trade_record.AllowUserToDeleteRows = false;
			this.m_grid_trade_record.AllowUserToOrderColumns = true;
			this.m_grid_trade_record.AllowUserToResizeRows = false;
			this.m_grid_trade_record.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_trade_record.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_grid_trade_record.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid_trade_record.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_trade_record.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_trade_record.Location = new System.Drawing.Point(0, 30);
			this.m_grid_trade_record.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_trade_record.Name = "m_dgv_trade_record";
			this.m_grid_trade_record.ReadOnly = true;
			this.m_grid_trade_record.RowHeadersVisible = false;
			this.m_grid_trade_record.RowHeadersWidth = 28;
			this.m_grid_trade_record.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_trade_record.Size = new System.Drawing.Size(699, 167);
			this.m_grid_trade_record.TabIndex = 3;
			// 
			// m_lbl_pair
			// 
			this.m_lbl_pair.AutoSize = true;
			this.m_lbl_pair.Location = new System.Drawing.Point(2, 8);
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
			this.m_cb_pair.Location = new System.Drawing.Point(67, 5);
			this.m_cb_pair.Name = "m_cb_pair";
			this.m_cb_pair.PreserveSelectionThruFocusChange = false;
			this.m_cb_pair.Size = new System.Drawing.Size(112, 21);
			this.m_cb_pair.TabIndex = 6;
			this.m_cb_pair.UseValidityColours = true;
			this.m_cb_pair.Value = null;
			// 
			// m_lbl_profit
			// 
			this.m_lbl_profit.AutoSize = true;
			this.m_lbl_profit.Location = new System.Drawing.Point(185, 8);
			this.m_lbl_profit.Name = "m_lbl_profit";
			this.m_lbl_profit.Size = new System.Drawing.Size(91, 13);
			this.m_lbl_profit.TabIndex = 7;
			this.m_lbl_profit.Text = "Price Change (%):";
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
			this.m_tb_profit.Location = new System.Drawing.Point(282, 5);
			this.m_tb_profit.Name = "m_tb_profit";
			this.m_tb_profit.Size = new System.Drawing.Size(62, 20);
			this.m_tb_profit.TabIndex = 8;
			this.m_tb_profit.UseValidityColours = true;
			this.m_tb_profit.Value = null;
			// 
			// m_lbl_volume_frac
			// 
			this.m_lbl_volume_frac.AutoSize = true;
			this.m_lbl_volume_frac.Location = new System.Drawing.Point(350, 8);
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
			this.m_tb_volume_frac.Location = new System.Drawing.Point(418, 5);
			this.m_tb_volume_frac.Name = "m_tb_volume_frac";
			this.m_tb_volume_frac.Size = new System.Drawing.Size(62, 20);
			this.m_tb_volume_frac.TabIndex = 10;
			this.m_tb_volume_frac.UseValidityColours = true;
			this.m_tb_volume_frac.Value = null;
			// 
			// m_panel0
			// 
			this.m_panel0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel0.Controls.Add(this.m_lbl_pair);
			this.m_panel0.Controls.Add(this.m_cb_pair);
			this.m_panel0.Controls.Add(this.m_tb_volume_frac);
			this.m_panel0.Controls.Add(this.m_lbl_profit);
			this.m_panel0.Controls.Add(this.m_lbl_volume_frac);
			this.m_panel0.Controls.Add(this.m_tb_profit);
			this.m_panel0.Location = new System.Drawing.Point(0, 0);
			this.m_panel0.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel0.MinimumSize = new System.Drawing.Size(486, 0);
			this.m_panel0.Name = "m_panel0";
			this.m_panel0.Size = new System.Drawing.Size(699, 30);
			this.m_panel0.TabIndex = 11;
			// 
			// m_table0
			// 
			this.m_table0.ColumnCount = 1;
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Controls.Add(this.m_panel0, 0, 0);
			this.m_table0.Controls.Add(this.m_grid_trade_record, 0, 1);
			this.m_table0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table0.Location = new System.Drawing.Point(0, 0);
			this.m_table0.Margin = new System.Windows.Forms.Padding(0);
			this.m_table0.MinimumSize = new System.Drawing.Size(486, 100);
			this.m_table0.Name = "m_table0";
			this.m_table0.RowCount = 2;
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Size = new System.Drawing.Size(699, 197);
			this.m_table0.TabIndex = 12;
			// 
			// PriceSwingUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_table0);
			this.Name = "PriceSwingUI";
			this.Size = new System.Drawing.Size(699, 197);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_trade_record)).EndInit();
			this.m_panel0.ResumeLayout(false);
			this.m_panel0.PerformLayout();
			this.m_table0.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
