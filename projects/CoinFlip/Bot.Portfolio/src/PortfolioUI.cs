using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Maths;
using Rylogic.Utility;
using ComboBox = Rylogic.Gui.ComboBox;
using DataGridView = Rylogic.Gui.DataGridView;

namespace Bot.Portfolio
{
	public class PortfolioUI :UserControl ,IDockable
	{
		#region UI Elements
		private Panel m_panel0;
		private DataGridView m_grid_portfolio;
		private ComboBox m_cb_exchange;
		private Label m_lbl_exchange;
		private DataGridView m_grid_status;
		private ToolTip m_tt;
		#endregion

		public PortfolioUI(Portfolio bot)
		{
			InitializeComponent();
			Currencies = new BindingSource<Portfolio.Folio>();
			Bot = bot;

			// Support for dock container controls
			DockControl = new DockControl(this, bot.Name)
			{
				TabText = bot.Name,
				DefaultDockLocation = new[]{ EDockSite.Bottom },
			};

			SetupUI();

			UpdateTimer = true;
		}
		protected override void Dispose(bool disposing)
		{
			UpdateTimer = false;
			DockControl = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The owning bot instance</summary>
		public Portfolio Bot
		{
			get { return m_bot; }
			private set
			{
				if (m_bot == value) return;
				if (m_bot != null)
				{
					m_bot.ActiveChanged -= HandleActiveChanged;
					m_bot.PropertyChanged -= HandleBotPropertyChanged;
					Currencies.DataSource = null;
				}
				m_bot = value;
				if (m_bot != null)
				{
					Currencies.DataSource = m_bot.Currencies;
					m_bot.PropertyChanged += HandleBotPropertyChanged;
					m_bot.ActiveChanged += HandleActiveChanged;
				}

				// Handlers
				void HandleActiveChanged(object sender, EventArgs e)
				{
					//UpdateUI();
				}
				void HandleBotPropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					//if (e.PropertyName == nameof(Portfolio.Settings))
					//{
					//	m_grid_portfolio.Invalidate();
					//	m_grid_status.Invalidate();
					//}
				}
			}
		}
		private Portfolio m_bot;

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

		/// <summary>The currencies that make up the portfolio</summary>
		private BindingSource<Portfolio.Folio> Currencies { get; set; }

		/// <summary>UI update timer</summary>
		private bool UpdateTimer
		{
			get { return m_update_timer != null; }
			set
			{
				if (UpdateTimer == value) return;
				if (m_update_timer != null)
				{
					m_update_timer.Tick -= HandlerTick;
					m_update_timer.Stop();
				}
				m_update_timer = value ? new Timer() : null;
				if (m_update_timer != null)
				{
					m_update_timer.Interval = 200;
					m_update_timer.Tick += HandlerTick;
					m_update_timer.Start();
				}

				// Handlers
				void HandlerTick(object sender, EventArgs e)
				{
					m_grid_status.InvalidateColumn(0);
				}
			}
		}
		private Timer m_update_timer;

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			void PopulateExchangeCombo(object sender = null, EventArgs args = null)
			{
				m_cb_exchange.Items.Clear();
				m_cb_exchange.Items.Add(None);
				m_cb_exchange.Items.AddRange(Bot.Model.TradingExchanges.Select(x => x.Name).OrderBy(x => x));
				m_cb_exchange.SelectedItem = Bot.Settings.Exchange;
			}

			#region Exchange Combo
			PopulateExchangeCombo();
			m_cb_exchange.ToolTip(m_tt, "The exchange to trade on");
			m_cb_exchange.DropDown += PopulateExchangeCombo;
			m_cb_exchange.DropDownClosed += (s,a) =>
			{
				// Update the settings
				Bot.Settings.Exchange = (string)m_cb_exchange.SelectedItem;
				m_grid_portfolio.Invalidate();
			};
			#endregion

			#region Portfolio Grid
			m_grid_portfolio.AllowDrop = true;
			m_grid_portfolio.AutoGenerateColumns = false;
			m_grid_portfolio.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Currency",
				Name = EColPortfolio.Currency.ToString(),
				DataPropertyName = nameof(Portfolio.Folio.Currency),
				ReadOnly = true,
			});
			m_grid_portfolio.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Weighting",
				Name = EColPortfolio.Weight.ToString(),
				DataPropertyName = nameof(Portfolio.Folio.Weight),
			});
			m_grid_portfolio.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Normalised",
				Name = EColPortfolio.Normalised.ToString(),
				DataPropertyName = EColPortfolio.Normalised.ToString(),
				ReadOnly = true,
			});
			m_grid_portfolio.CellFormatting += (s,a) =>
			{
				if (!m_grid_portfolio.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col, out DataGridViewCell cell) ||
					!a.RowIndex.Within(0, Bot.Currencies.Count))
					return;

				var folio = Bot.Currencies[a.RowIndex];

				// Highlight currencies that aren't available on the selected exchange
				if (Bot.Exchange.Coins[folio.Currency] == null)
					a.CellStyle.BackColor = Color.Red;

				// Normalised column
				if (col.DataPropertyName == EColPortfolio.Normalised.ToString())
				{
					a.Value = Math_.Div(folio.Weight, Bot.Currencies.Sum(x => x.Weight), 0).ToString();
					a.FormattingApplied = true;
				}

				// Half bright selection
				DataGridView_.HalfBrightSelection(s,a);
			};
			m_grid_portfolio.CellValueChanged += (s,a) =>
			{
				if (a.ColumnIndex != (int)EColPortfolio.Weight) return;
				m_grid_portfolio.InvalidateColumn((int)EColPortfolio.Normalised);
			};
			m_grid_portfolio.MouseDown += DataGridView_.DragDrop_DragRow;
			m_grid_portfolio.ContextMenuStrip = CreateCMenu();
			m_grid_portfolio.DataSource = Currencies;
			ContextMenuStrip CreateCMenu()
			{
				var cmenu = new ContextMenuStrip();
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Add"));
					opt.Click += (s,a) =>
					{
						Bot.AddCurrency();
						m_grid_portfolio.Invalidate();
					};
				}
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Delete"));
					cmenu.Opening += (s,a) =>
					{
						opt.Enabled = m_grid_portfolio.SelectedRowCount(1) != 0;
					};
					opt.Click += (s,a) =>
					{
						var doomed = m_grid_portfolio.SelectedRows
							.Cast<DataGridViewRow>()
							.Select(x => ((Portfolio.Folio)x.DataBoundItem).Currency)
							.ToList();
						foreach (var sym in doomed)
							Bot.RemoveCurrency(sym);

						m_grid_portfolio.CurrentCell = null;
						m_grid_portfolio.Invalidate();
					};
				}
				return cmenu;
			}
			#endregion

			#region Portfolio Status Grid
			m_grid_status.AutoGenerateColumns = false;
			m_grid_status.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Current Weighting",
				Name = EColStatus.CurrentWeighting.ToString(),
				DataPropertyName = EColStatus.CurrentWeighting.ToString(),
			});
			m_grid_status.CellFormatting += (s,a) =>
			{
				if (!m_grid_status.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col) ||
					!a.RowIndex.Within(0, Bot.Currencies.Count))
					return;

				var folio = Bot.Currencies[a.RowIndex];

				// Currency weighting
				if (col.DataPropertyName == EColStatus.CurrentWeighting.ToString())
				{
					var val = (decimal)folio.Coin.ValueOf(folio.Coin.Balances[Bot.Fund].Total);
					var tot = Bot.Currencies.Sum(x => (decimal)x.Coin.ValueOf(x.Coin.Balances[Bot.Fund].Total));
					a.Value = Math_.Div(val, tot, 0).ToString("G3");
					a.FormattingApplied = true;
				}

				DataGridView_.HalfBrightSelection(s,a);
			};
			m_grid_status.DataSource = Currencies;
			#endregion

			var dd = new DragDrop(m_grid_portfolio);
			dd.DoDrop += DataGridView_.DragDrop_DoDropMoveRow;
		}

		private const string None = "---";
		private enum EColPortfolio { Currency, Weight, Normalised, };
		private enum EColStatus { CurrentWeighting, };

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_panel0 = new System.Windows.Forms.Panel();
			this.m_grid_portfolio = new Rylogic.Gui.DataGridView();
			this.m_cb_exchange = new Rylogic.Gui.ComboBox();
			this.m_lbl_exchange = new System.Windows.Forms.Label();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_grid_status = new Rylogic.Gui.DataGridView();
			this.m_panel0.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_portfolio)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_status)).BeginInit();
			this.SuspendLayout();
			// 
			// m_panel0
			// 
			this.m_panel0.Controls.Add(this.m_grid_status);
			this.m_panel0.Controls.Add(this.m_grid_portfolio);
			this.m_panel0.Controls.Add(this.m_cb_exchange);
			this.m_panel0.Controls.Add(this.m_lbl_exchange);
			this.m_panel0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panel0.Location = new System.Drawing.Point(0, 0);
			this.m_panel0.Name = "m_panel0";
			this.m_panel0.Size = new System.Drawing.Size(676, 306);
			this.m_panel0.TabIndex = 0;
			// 
			// m_grid_portfolio
			// 
			this.m_grid_portfolio.AllowUserToAddRows = false;
			this.m_grid_portfolio.AllowUserToDeleteRows = false;
			this.m_grid_portfolio.AllowUserToResizeRows = false;
			this.m_grid_portfolio.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.m_grid_portfolio.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_portfolio.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid_portfolio.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_portfolio.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_portfolio.Location = new System.Drawing.Point(6, 31);
			this.m_grid_portfolio.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_portfolio.Name = "m_grid_portfolio";
			this.m_grid_portfolio.RowHeadersWidth = 20;
			this.m_grid_portfolio.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_portfolio.Size = new System.Drawing.Size(275, 269);
			this.m_grid_portfolio.TabIndex = 0;
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
			this.m_cb_exchange.Location = new System.Drawing.Point(67, 7);
			this.m_cb_exchange.Name = "m_cb_exchange";
			this.m_cb_exchange.PreserveSelectionThruFocusChange = false;
			this.m_cb_exchange.Size = new System.Drawing.Size(143, 21);
			this.m_cb_exchange.TabIndex = 7;
			this.m_cb_exchange.UseValidityColours = true;
			this.m_cb_exchange.Value = null;
			// 
			// m_lbl_exchange
			// 
			this.m_lbl_exchange.AutoSize = true;
			this.m_lbl_exchange.Location = new System.Drawing.Point(3, 10);
			this.m_lbl_exchange.Name = "m_lbl_exchange";
			this.m_lbl_exchange.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_exchange.TabIndex = 6;
			this.m_lbl_exchange.Text = "Exchange:";
			// 
			// m_grid_status
			// 
			this.m_grid_status.AllowUserToAddRows = false;
			this.m_grid_status.AllowUserToDeleteRows = false;
			this.m_grid_status.AllowUserToResizeRows = false;
			this.m_grid_status.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.m_grid_status.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_status.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid_status.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_status.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_status.Location = new System.Drawing.Point(284, 31);
			this.m_grid_status.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_status.Name = "m_grid_status";
			this.m_grid_status.ReadOnly = true;
			this.m_grid_status.RowHeadersVisible = false;
			this.m_grid_status.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_status.Size = new System.Drawing.Size(261, 269);
			this.m_grid_status.TabIndex = 8;
			// 
			// PortfolioUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_panel0);
			this.Name = "PortfolioUI";
			this.Size = new System.Drawing.Size(676, 306);
			this.m_panel0.ResumeLayout(false);
			this.m_panel0.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_portfolio)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_status)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
