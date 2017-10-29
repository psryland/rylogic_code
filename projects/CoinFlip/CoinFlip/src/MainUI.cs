using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using pr.common;
using pr.container;
using pr.db;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.scintilla;
using pr.util;
using pr.win32;
using ToolStripContainer = pr.gui.ToolStripContainer;
using Timer = System.Windows.Forms.Timer;
using System.Linq;

namespace CoinFlip
{
	public class MainUI :Form
	{
		#region UI Elements
		private ToolStripContainer m_tsc;
		private StatusStrip m_ss;
		private ToolStripStatusLabel m_status;
		private GridExchanges m_grid_exchanges;
		private GridCoins m_grid_coins;
		private GridBalances m_grid_balances;
		private GridPositions m_grid_positions;
		private GridHistory m_grid_history;
		private GridBots m_grid_bots;
		private GridArbitrage m_grid_arbitrage;
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private ToolStripMenuItem m_menu_file_exit;
		private DockContainer m_dc;
		private ToolStrip m_ts;
		private ToolStripButton m_chk_allow_trades;
		private ToolStripMenuItem toolsToolStripMenuItem;
		private ToolStripMenuItem m_menu_tools_show_pairs;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripLabel m_lbl_nett_worth;
		private ToolStripTextBox m_tb_nett_worth;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripButton m_chk_live;
		private ToolStrip m_ts_backtesting;
		private ToolStripButton m_chk_back_testing;
		private ToolStripButton m_btn_backtesting_run;
		private ToolStripTrackBar m_trk_sim_time;
		private LogUI m_win_log;
		private ToolTip m_tt;
		private System.Windows.Forms.ToolStripComboBox m_cb_sim_timeframe;
		private ToolStripMenuItem m_menu_file_new_chart;
		private ToolStripSeparator toolStripSeparator5;
		private ToolStripSeparator toolStripSeparator6;
		private ToolStripMenuItem m_menu_tools_back_testing;
		private ToolStripMenuItem m_menu_debug;
		private ToolStripMenuItem m_menu_debug_invalidate_chart;
		private ToolStripMenuItem m_menu_debug_test;
		private ToolStripButton m_btn_backtesting_reset;
		private ToolStripButton m_btn_backtesting_step1;
		private ToolStripButton m_btn_backtesting_run_to_trade;
		private ToolStripMenuItem m_menu_tools_update_pairs;
		private ToolStripTextBox m_tb_sim_time;
		private LogUI m_log;
		#endregion

		public MainUI()
		{
			try
			{
				InitializeComponent();
				CreateHandle();

				// Ensure the app data directory exists
				Directory.CreateDirectory(Misc.ResolveUserPath());
				var settings = new Settings(Misc.ResolveUserPath("settings.xml"));

				// Get the user details
				var user = Misc.LogIn(this, settings);
				Text = $"{Application.ProductName} - {user.Username}";

				Model = new Model(this, settings, user);
				PairsUI = new PairsUI(Model, this);
				Charts = new List<ChartUI>();

				SetupUI();
				UpdateUI();

				RestoreWindowPosition();
				UpdateActive = true;
			}
			catch (Exception)
			{
				Dispose();
				throw;
			}
		}
		protected override void Dispose(bool disposing)
		{
			UpdateActive = false;
			PairsUI = null;
			Charts = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
			Model = null;
		}
		protected override void OnResizeEnd(EventArgs e)
		{
			base.OnResizeEnd(e);
			Settings.UI.WindowPosition = Bounds;
			Settings.UI.UILayout = m_dc.SaveLayout();
		}
		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (m.Msg == Win32.WM_SYSCOMMAND)
			{
				var wp = (uint)m.WParam & 0xFFF0;
				if (wp == Win32.SC_MAXIMIZE || wp == Win32.SC_RESTORE || wp == Win32.SC_MINIMIZE)
					Settings.UI.WindowMaximised = WindowState == FormWindowState.Maximized;
			}
		}
		protected override void OnClosing(CancelEventArgs e)
		{
			// Save the layout and window position
			Settings.UI.UILayout = m_dc.SaveLayout();

			// Form shutdown is a PITA when using async methods.
			// Disable the form while we wait for shutdown to be allowed
			Enabled = false;
			if (!Model.Shutdown.IsCancellationRequested)
			{
				e.Cancel = true;
				Model.Shutdown.Cancel();
				Close();
				return;
			}
			base.OnClosing(e);
		}

		/// <summary>App settings</summary>
		public Settings Settings
		{
			get { return Model.Settings; }
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.AllowTradesChanging     -= UpdateUI;
					m_model.BackTestingChanging     -= HandleBackTestingChanged;
					m_model.SimRunningChanged       -= UpdateUI;
					m_model.Settings.SettingChanged -= HandleSettingsChanged;
					m_model.Pairs.ListChanging      -= HandlePairsChanging;
					m_model.Coins.ListChanging      -= HandleCoinsListChanging;
					m_model.Exchanges.ListChanging  -= HandleExchangesChanging;
					m_model.OnAddToUI               -= HandleAddToUI;
					m_model.OnEditTrade             -= HandleEditTrade;
					Util.Dispose(ref m_model);
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.OnEditTrade             += HandleEditTrade;
					m_model.OnAddToUI               += HandleAddToUI;
					m_model.Exchanges.ListChanging  += HandleExchangesChanging;
					m_model.Coins.ListChanging      += HandleCoinsListChanging;
					m_model.Pairs.ListChanging      += HandlePairsChanging;
					m_model.Settings.SettingChanged += HandleSettingsChanged;
					m_model.SimRunningChanged       += UpdateUI;
					m_model.BackTestingChanging     += HandleBackTestingChanged;
					m_model.AllowTradesChanging     += UpdateUI;
				}

				// Handlers
				void HandleSettingsChanged(object sender, SettingChangedEventArgs e)
				{
					switch (e.Key)
					{
					case nameof(Settings.ShowLivePrices):
						{
							m_chk_live.Checked = Settings.ShowLivePrices;
							break;
						}
					case nameof(Settings.BackTestingSettings.TimeFrame):
						{
							m_cb_sim_timeframe.SelectedItem = Settings.BackTesting.TimeFrame;
							break;
						}
					case nameof(Settings.BackTestingSettings.Steps):
						{
							m_trk_sim_time.TrackBar.ValueClamped(m_trk_sim_time.TrackBar.Maximum - Settings.BackTesting.Steps);
							break;
						}
					case nameof(Settings.BackTestingSettings.MaxSteps):
						{
							m_trk_sim_time.TrackBar.Set(Settings.BackTesting.MaxSteps - Settings.BackTesting.Steps, 0, Settings.BackTesting.MaxSteps);
							break;
						}
					}
				}
				void HandleBackTestingChanged(object sender, EventArgs e)
				{
					m_tb_sim_time.Visible = Model.BackTesting;
					UpdateUI();
				}
				void HandleCoinsListChanging(object sender, ListChgEventArgs<Settings.CoinData> e)
				{
					m_grid_coins.Invalidate();
					m_grid_balances.Invalidate();
				}
				void HandleExchangesChanging(object sender, ListChgEventArgs<Exchange> e)
				{
					m_grid_exchanges.Invalidate();
					m_grid_balances.Invalidate();
					m_grid_positions.Invalidate();
				}
				void HandlePairsChanging(object sender, EventArgs e)
				{
					m_grid_exchanges.InvalidateColumn(m_grid_exchanges.Columns[nameof(Exchange.CoinsAvailable)].Index);
					m_grid_exchanges.InvalidateColumn(m_grid_exchanges.Columns[nameof(Exchange.PairsAvailable)].Index);
				}
				void HandleAddToUI(object sender, AddToUIEventArgs e)
				{
					if (e.Dockable != null)
						m_dc.Add(e.Dockable, e.DockLocation);

					if (e.Toolbar != null)
						m_tsc.TopToolStripPanel.Controls.Add(e.Toolbar);
				}
				void HandleEditTrade(object sender, EditTradeEventArgs e)
				{
					// Find a chart for the pair that is being edited, add one if not found
					var chart = Charts.FirstOrDefault(x => x.Instrument?.Pair == e.Trade.Pair);
					if (chart == null)
					{
						chart = NewChart();
						chart.SetInstrument(e.Trade.Pair, ETimeFrame.Hour1);
						chart.DockControl.IsActiveContent = true;
					}

					// Create an edit order UI that places the trade when OK'd
					var ui = new EditOrderUI(e.Trade, chart, e.ExistingOrderId);
					ui.FormClosed += (s,a) => ui.Dispose();

					// Show the create/modify order UI
					ui.Show(this);
				}
			}
		}
		private Model m_model;

		/// <summary>UI refresh timer</summary>
		public bool UpdateActive
		{
			get { return m_timer != null; }
			set
			{
				if (UpdateActive == value) return;
				if (UpdateActive)
				{
					m_timer.Stop();
					m_timer.Tick -= HandleTick;
					Util.Dispose(ref m_timer);
				}
				m_timer = value ? new Timer() : null;
				if (UpdateActive)
				{
					m_timer.Interval = 200;
					m_timer.Tick += HandleTick;
					m_timer.Start();
				}

				// Handlers
				void HandleTick(object sender, EventArgs args)
				{
					// Invalidate the live price data 
					m_grid_exchanges.InvalidateColumn(m_grid_exchanges.Columns[nameof(Exchange.NettWorth)].Index);
					m_grid_balances .InvalidateColumn(m_grid_balances.Columns[nameof(Balance.Available)].Index);
					m_grid_balances .InvalidateColumn(m_grid_balances.Columns[nameof(Balance.Total)].Index);
					m_grid_balances .InvalidateColumn(m_grid_balances.Columns[nameof(Balance.Value)].Index);
					m_grid_positions.InvalidateColumn(m_grid_positions.Columns[nameof(GridPositions.ColumnNames.LivePrice)].Index);
					m_grid_positions.InvalidateColumn(m_grid_positions.Columns[nameof(GridPositions.ColumnNames.PriceDist)].Index);
					m_grid_coins    .InvalidateColumn(m_grid_coins.Columns[nameof(GridCoins.ColumnNames.Total)].Index);
					m_grid_coins    .InvalidateColumn(m_grid_coins.Columns[nameof(GridCoins.ColumnNames.Available)].Index);
					m_grid_coins    .InvalidateColumn(m_grid_coins.Columns[nameof(GridCoins.ColumnNames.Value)].Index);
					m_grid_coins    .InvalidateColumn(m_grid_coins.Columns[nameof(GridCoins.ColumnNames.Balance)].Index);
					m_grid_arbitrage.Invalidate();

					// Update the back-testing clock
					if (Model.BackTesting)
						m_tb_sim_time.Text = Model.Simulation.Clock.ToString("yyyy-MM-dd ddd HH:mm:ss");

					// Update the nett worth value
					m_tb_nett_worth.Text = Model.NettWorth.ToString("c");
				}
			}
		}
		private Timer m_timer;

		/// <summary>The UI for displaying info about a particular pair</summary>
		public PairsUI PairsUI
		{
			get { return m_pairs_ui; }
			private set
			{
				if (m_pairs_ui == value) return;
				Util.Dispose(ref m_pairs_ui);
				m_pairs_ui = value;
			}
		}
		private PairsUI m_pairs_ui;

		/// <summary>Chart instances</summary>
		public List<ChartUI> Charts
		{
			get { return m_charts; }
			private set
			{
				if (m_charts == value) return;
				Util.DisposeAll(m_charts);
				m_charts = value;
			}
		}
		private List<ChartUI> m_charts;

		/// <summary>Wire up the UI</summary>
		private void SetupUI()
		{
			#region Menu
			{
				m_menu_file_new_chart.Click += (s,a) =>
				{
					NewChart();
				};
				m_menu_file_exit.Click += (s,a) =>
				{
					Close();
				};
				m_menu_tools_update_pairs.Click += (s,a) =>
				{
					Model.TriggerPairsUpdate();
				};
				m_menu_tools_show_pairs.Click += (s,a) =>
				{
					PairsUI.Show();
				};
				m_menu_tools_back_testing.Click += (s,a) =>
				{
					ShowSimUI();
				};
				m_menu_debug.Visible = Util.IsDebug;
				m_menu_debug_invalidate_chart.Click += (s,a) =>
				{
					foreach (var chart in Charts)
						chart.InvalidateCandleGfx();
				};
				m_menu_debug_test.Click += (s,a) =>
				{
					Model.Test();
				};
				m_menu.Items.Add(m_dc.WindowsMenu());
			}
			#endregion

			#region Trading Tool bar
			{
				m_chk_allow_trades.CheckedChanged += (s,a) =>
				{
					Model.SetAllowTrades(m_chk_allow_trades.Checked);
					m_chk_allow_trades.Text = m_chk_allow_trades.Checked ? "Disable Trading" : "Enable Trading";
					m_chk_allow_trades.BackColor = m_chk_allow_trades.Checked ? Color.LightGreen : SystemColors.Control;
				};
				m_chk_live.Checked = Settings.ShowLivePrices;
				m_chk_live.CheckedChanged += (s,a) =>
				{
					Settings.ShowLivePrices = m_chk_live.Checked;
				};
			}
			#endregion

			#region Back Testing Tool bar
			{
				m_chk_back_testing.ToolTip(m_tt, "Enable/Disable back-testing mode");
				m_chk_back_testing.CheckedChanged += (s,a) =>
				{
					Model.BackTesting = m_chk_back_testing.Checked;
					UpdateUI();
				};

				m_btn_backtesting_reset.ToolTip(m_tt, "Reset to the starting time");
				m_btn_backtesting_reset.Click += (s,a) =>
				{
					if (Model.Simulation == null) return;
					Model.Simulation.Reset();
					foreach (var chart in Charts)
						chart.AutoRange();
					UpdateUI();
				};

				m_btn_backtesting_step1.ToolTip(m_tt, "Step forward by one candle, then pause");
				m_btn_backtesting_step1.Click += (s,a) =>
				{
					if (Model.Simulation == null) return;
					Model.Simulation.StepOne();
					UpdateUI();
				};

				m_btn_backtesting_run_to_trade.ToolTip(m_tt, "Run up to the next submitted or filled trade");
				m_btn_backtesting_run_to_trade.Click += (s,a) =>
				{
					if (Model.Simulation == null) return;
					Model.Simulation.RunToTrade();
					UpdateUI();
				};

				m_btn_backtesting_run.ToolTip(m_tt, "Start/Stop back-testing");
				m_btn_backtesting_run.Click += (s,a) =>
				{
					if (Model.Simulation == null) return;
					if (Model.Simulation.Running)
						Model.Simulation.Pause();
					else
						Model.Simulation.Run();
					UpdateUI();
				};

				var max_steps = Settings.BackTesting.MaxSteps;
				m_trk_sim_time.ToolTip(m_tt, "Set the starting point to run the back testing from");
				m_trk_sim_time.TrackBar.Set(Maths.Clamp(max_steps - Settings.BackTesting.Steps, 0, max_steps), 0, max_steps);
				m_trk_sim_time.TrackBar.ValueChanged += (s,a) =>
				{
					var steps = m_trk_sim_time.TrackBar.Maximum - m_trk_sim_time.TrackBar.Value;
					var size = (ETimeFrame)m_cb_sim_timeframe.ComboBox.SelectedItem;
					Model.Simulation.SetStartTime(size, steps);
					UpdateUI();
				};

				m_cb_sim_timeframe.ToolTip(m_tt, "Set the time step size used for back testing");
				m_cb_sim_timeframe.ComboBox.DataSource = Enum<ETimeFrame>.Values;
				m_cb_sim_timeframe.ComboBox.SelectedItem = Settings.BackTesting.TimeFrame;
				m_cb_sim_timeframe.ComboBox.SelectedIndexChanged += (s,a) =>
				{
					var steps = m_trk_sim_time.TrackBar.Maximum - m_trk_sim_time.TrackBar.Value;
					var size = (ETimeFrame)m_cb_sim_timeframe.ComboBox.SelectedItem;
					Model.Simulation.SetStartTime(size, steps);
					UpdateUI();
				};
			}
			#endregion

			#region Grids
			{
				m_grid_coins = new GridCoins(Model, "Coins", nameof(m_grid_coins));
				m_grid_exchanges = new GridExchanges(Model, "Exchanges", nameof(m_grid_exchanges));
				m_grid_balances = new GridBalances(Model, "Balances", nameof(m_grid_balances));
				m_grid_positions = new GridPositions(Model, "Positions", nameof(m_grid_positions));
				m_grid_history = new GridHistory(Model, "History", nameof(m_grid_history));
				m_grid_bots = new GridBots(Model, "Bots", nameof(m_grid_bots));
				m_grid_arbitrage = new GridArbitrage(Model, "Arbitrage", nameof(m_grid_arbitrage));
			}
			#endregion

			#region Log control
			{
				m_win_log = new LogUI("Wins!", nameof(m_win_log))
				{
					LogFilepath = ((LogToFile)Model.WinLog.LogCB).Filepath,
					LogEntryPattern = Misc.LogEntryPattern,
					PopOutOnNewMessages = false,
				};
				m_log = new LogUI("Log", nameof(m_log))
				{
					LogFilepath = ((LogToFile)Model.Log.LogCB).Filepath,
					LogEntryPattern = Misc.LogEntryPattern,
					PopOutOnNewMessages = false,
				};
				m_log.Highlighting.AddRange(Misc.LogHighlighting);
				m_log.Formatting += Misc.LogFormatting;
			}
			#endregion

			#region Dock Container

			// Add content
			NewChart();
			m_dc.Add(m_log, EDockSite.Centre);
			m_dc.Add(m_win_log, EDockSite.Centre);
			m_dc.Add(m_grid_positions, EDockSite.Bottom);
			m_dc.Add(m_grid_history, EDockSite.Bottom);
			m_dc.Add(m_grid_exchanges, EDockSite.Left);
			m_dc.Add(m_grid_coins, EDockSite.Left);
			m_dc.Add(m_grid_balances, EDockSite.Left, EDockSite.Bottom);
			m_dc.Add(m_grid_arbitrage, EDockSite.Left, EDockSite.Bottom);
			m_dc.Add(m_grid_bots, EDockSite.Right);

			// Load layout
			if (Settings.UI.UILayout != null)
			{
				try { m_dc.LoadLayout(Settings.UI.UILayout); }
				catch (Exception ex) { Model.Log.Write(ELogLevel.Error, ex, "Failed to restore UI layout"); }
			}

			// Clean up
			m_dc.Options.DisposeContent = true;
			#endregion

			// Persist tool bar locations
			m_tsc.AutoPersistLocations(Settings.UI.ToolStripLayout, loc => Settings.UI.ToolStripLayout = loc);
		}

		/// <summary>Handle the model properties changing</summary>
		private void UpdateUI(object sender = null, EventArgs e = null)
		{
			var back_testing = Model.BackTesting;
			var allow_trades = Model.AllowTrades;
			var sim_running =  Model.Simulation?.Running ?? false;

			// Enable
			m_chk_allow_trades.Enabled             = !back_testing;
			m_chk_back_testing.Enabled             = !allow_trades && !sim_running;
			m_menu_tools_back_testing.Enabled      = back_testing;
			m_btn_backtesting_run.Enabled          = back_testing;
			m_btn_backtesting_reset.Enabled        = back_testing && !sim_running;
			m_btn_backtesting_step1.Enabled        = back_testing && !sim_running;
			m_btn_backtesting_run_to_trade.Enabled = back_testing && !sim_running;
			m_trk_sim_time.Enabled                 = back_testing;
			m_cb_sim_timeframe.Enabled             = back_testing && !sim_running;

			// Appearance
			m_chk_allow_trades.Checked = allow_trades;
			m_chk_back_testing.Checked = back_testing;
			m_btn_backtesting_run.Image = sim_running ? Res.Pause : Res.Play;
		}

		/// <summary>Create a new chart instance</summary>
		private ChartUI NewChart()
		{
			var chart = Charts.Add2(new ChartUI(Model));
			m_dc.Add(chart, EDockSite.Centre);
			chart.Closed += (s,a) =>
			{
				Charts.Remove(chart);
				m_dc.Remove(chart);
				Util.Dispose(chart);
			};
			return chart;
		}

		/// <summary>Display a dialog for configuring the simulation</summary>
		public void ShowSimUI()
		{
			using (var dlg = new SimulationUI(this, Settings))
				dlg.ShowDialog(this);
		}

		/// <summary>Restore the last window position</summary>
		private void RestoreWindowPosition()
		{
			if (Settings.UI.WindowPosition.IsEmpty)
			{
				StartPosition = FormStartPosition.WindowsDefaultLocation;
			}
			else
			{
				// Ensure the Bounds are at least partially within the desktop
				StartPosition  = FormStartPosition.Manual;
				Bounds         = Util.OnScreen(Settings.UI.WindowPosition);;
				WindowState    = Settings.UI.WindowMaximised ? FormWindowState.Maximized : FormWindowState.Normal;
			}
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainUI));
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_ss = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_dc = new pr.gui.DockContainer();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_new_chart = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_update_pairs = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_show_pairs = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_tools_back_testing = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_debug = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_debug_invalidate_chart = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_debug_test = new System.Windows.Forms.ToolStripMenuItem();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_chk_allow_trades = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_lbl_nett_worth = new System.Windows.Forms.ToolStripLabel();
			this.m_tb_nett_worth = new System.Windows.Forms.ToolStripTextBox();
			this.m_chk_live = new System.Windows.Forms.ToolStripButton();
			this.m_ts_backtesting = new System.Windows.Forms.ToolStrip();
			this.m_chk_back_testing = new System.Windows.Forms.ToolStripButton();
			this.m_btn_backtesting_reset = new System.Windows.Forms.ToolStripButton();
			this.m_btn_backtesting_step1 = new System.Windows.Forms.ToolStripButton();
			this.m_btn_backtesting_run_to_trade = new System.Windows.Forms.ToolStripButton();
			this.m_btn_backtesting_run = new System.Windows.Forms.ToolStripButton();
			this.m_trk_sim_time = new pr.gui.ToolStripTrackBar();
			this.m_cb_sim_timeframe = new System.Windows.Forms.ToolStripComboBox();
			this.m_tb_sim_time = new System.Windows.Forms.ToolStripTextBox();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_tsc.BottomToolStripPanel.SuspendLayout();
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_ss.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.m_ts.SuspendLayout();
			this.m_ts_backtesting.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tsc
			// 
			// 
			// m_tsc.BottomToolStripPanel
			// 
			this.m_tsc.BottomToolStripPanel.Controls.Add(this.m_ss);
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Controls.Add(this.m_dc);
			this.m_tsc.ContentPanel.Margin = new System.Windows.Forms.Padding(0);
			this.m_tsc.ContentPanel.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(1025, 622);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Margin = new System.Windows.Forms.Padding(0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(1025, 718);
			this.m_tsc.TabIndex = 2;
			this.m_tsc.Text = "tsc";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_menu);
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts);
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts_backtesting);
			// 
			// m_ss
			// 
			this.m_ss.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ss.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status});
			this.m_ss.Location = new System.Drawing.Point(0, 0);
			this.m_ss.Name = "m_ss";
			this.m_ss.Size = new System.Drawing.Size(1025, 22);
			this.m_ss.TabIndex = 0;
			// 
			// m_status
			// 
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(26, 17);
			this.m_status.Text = "Idle";
			// 
			// m_dc
			// 
			this.m_dc.ActiveContent = null;
			this.m_dc.ActiveDockable = null;
			this.m_dc.ActivePane = null;
			this.m_dc.BackColor = System.Drawing.SystemColors.AppWorkspace;
			this.m_dc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dc.Location = new System.Drawing.Point(8, 0);
			this.m_dc.Name = "m_dc";
			this.m_dc.Size = new System.Drawing.Size(1009, 622);
			this.m_dc.TabIndex = 4;
			this.m_dc.Text = "dockContainer1";
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file,
            this.toolsToolStripMenuItem,
            this.m_menu_debug});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(1025, 24);
			this.m_menu.TabIndex = 0;
			this.m_menu.Text = "menuStrip1";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_new_chart,
            this.toolStripSeparator5,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_new_chart
			// 
			this.m_menu_file_new_chart.Name = "m_menu_file_new_chart";
			this.m_menu_file_new_chart.Size = new System.Drawing.Size(130, 22);
			this.m_menu_file_new_chart.Text = "New &Chart";
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(127, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(130, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// toolsToolStripMenuItem
			// 
			this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_tools_update_pairs,
            this.m_menu_tools_show_pairs,
            this.toolStripSeparator6,
            this.m_menu_tools_back_testing});
			this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
			this.toolsToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
			this.toolsToolStripMenuItem.Text = "&Tools";
			// 
			// m_menu_tools_update_pairs
			// 
			this.m_menu_tools_update_pairs.Name = "m_menu_tools_update_pairs";
			this.m_menu_tools_update_pairs.Size = new System.Drawing.Size(149, 22);
			this.m_menu_tools_update_pairs.Text = "Update &Pairs";
			// 
			// m_menu_tools_show_pairs
			// 
			this.m_menu_tools_show_pairs.Name = "m_menu_tools_show_pairs";
			this.m_menu_tools_show_pairs.Size = new System.Drawing.Size(149, 22);
			this.m_menu_tools_show_pairs.Text = "&Show Pairs";
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(146, 6);
			// 
			// m_menu_tools_back_testing
			// 
			this.m_menu_tools_back_testing.Name = "m_menu_tools_back_testing";
			this.m_menu_tools_back_testing.Size = new System.Drawing.Size(149, 22);
			this.m_menu_tools_back_testing.Text = "&Back Testing...";
			// 
			// m_menu_debug
			// 
			this.m_menu_debug.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_debug_invalidate_chart,
            this.m_menu_debug_test});
			this.m_menu_debug.Name = "m_menu_debug";
			this.m_menu_debug.Size = new System.Drawing.Size(54, 20);
			this.m_menu_debug.Text = "&Debug";
			// 
			// m_menu_debug_invalidate_chart
			// 
			this.m_menu_debug_invalidate_chart.Name = "m_menu_debug_invalidate_chart";
			this.m_menu_debug_invalidate_chart.Size = new System.Drawing.Size(157, 22);
			this.m_menu_debug_invalidate_chart.Text = "Invalidate Chart";
			// 
			// m_menu_debug_test
			// 
			this.m_menu_debug_test.Name = "m_menu_debug_test";
			this.m_menu_debug_test.Size = new System.Drawing.Size(157, 22);
			this.m_menu_debug_test.Text = "Test";
			// 
			// m_ts
			// 
			this.m_ts.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_chk_allow_trades,
            this.toolStripSeparator2,
            this.m_lbl_nett_worth,
            this.m_tb_nett_worth,
            this.m_chk_live});
			this.m_ts.Location = new System.Drawing.Point(3, 24);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(326, 25);
			this.m_ts.TabIndex = 1;
			// 
			// m_chk_allow_trades
			// 
			this.m_chk_allow_trades.CheckOnClick = true;
			this.m_chk_allow_trades.Image = ((System.Drawing.Image)(resources.GetObject("m_chk_allow_trades.Image")));
			this.m_chk_allow_trades.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_chk_allow_trades.Name = "m_chk_allow_trades";
			this.m_chk_allow_trades.Size = new System.Drawing.Size(105, 22);
			this.m_chk_allow_trades.Text = "Enable Trading";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// m_lbl_nett_worth
			// 
			this.m_lbl_nett_worth.Name = "m_lbl_nett_worth";
			this.m_lbl_nett_worth.Size = new System.Drawing.Size(69, 22);
			this.m_lbl_nett_worth.Text = "Nett Worth:";
			// 
			// m_tb_nett_worth
			// 
			this.m_tb_nett_worth.Name = "m_tb_nett_worth";
			this.m_tb_nett_worth.ReadOnly = true;
			this.m_tb_nett_worth.Size = new System.Drawing.Size(100, 25);
			// 
			// m_chk_live
			// 
			this.m_chk_live.CheckOnClick = true;
			this.m_chk_live.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.m_chk_live.Image = ((System.Drawing.Image)(resources.GetObject("m_chk_live.Image")));
			this.m_chk_live.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_chk_live.Name = "m_chk_live";
			this.m_chk_live.Size = new System.Drawing.Size(32, 22);
			this.m_chk_live.Text = "Live";
			// 
			// m_ts_backtesting
			// 
			this.m_ts_backtesting.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts_backtesting.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_chk_back_testing,
            this.m_cb_sim_timeframe,
            this.m_btn_backtesting_reset,
            this.m_btn_backtesting_step1,
            this.m_btn_backtesting_run_to_trade,
            this.m_btn_backtesting_run,
            this.m_trk_sim_time,
            this.m_tb_sim_time});
			this.m_ts_backtesting.Location = new System.Drawing.Point(3, 49);
			this.m_ts_backtesting.Name = "m_ts_backtesting";
			this.m_ts_backtesting.Size = new System.Drawing.Size(652, 25);
			this.m_ts_backtesting.TabIndex = 2;
			// 
			// m_chk_back_testing
			// 
			this.m_chk_back_testing.CheckOnClick = true;
			this.m_chk_back_testing.Image = ((System.Drawing.Image)(resources.GetObject("m_chk_back_testing.Image")));
			this.m_chk_back_testing.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_chk_back_testing.Name = "m_chk_back_testing";
			this.m_chk_back_testing.Size = new System.Drawing.Size(93, 22);
			this.m_chk_back_testing.Text = "Back Testing";
			// 
			// m_btn_backtesting_reset
			// 
			this.m_btn_backtesting_reset.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_backtesting_reset.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_backtesting_reset.Image")));
			this.m_btn_backtesting_reset.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_backtesting_reset.Name = "m_btn_backtesting_reset";
			this.m_btn_backtesting_reset.Size = new System.Drawing.Size(23, 22);
			this.m_btn_backtesting_reset.Text = "toolStripButton1";
			// 
			// m_btn_backtesting_step1
			// 
			this.m_btn_backtesting_step1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_backtesting_step1.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_backtesting_step1.Image")));
			this.m_btn_backtesting_step1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_backtesting_step1.Name = "m_btn_backtesting_step1";
			this.m_btn_backtesting_step1.Size = new System.Drawing.Size(23, 22);
			this.m_btn_backtesting_step1.Text = "Step One";
			this.m_btn_backtesting_step1.ToolTipText = "Step forward by one candle then pause";
			// 
			// m_btn_backtesting_run_to_trade
			// 
			this.m_btn_backtesting_run_to_trade.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_backtesting_run_to_trade.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_backtesting_run_to_trade.Image")));
			this.m_btn_backtesting_run_to_trade.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_backtesting_run_to_trade.Name = "m_btn_backtesting_run_to_trade";
			this.m_btn_backtesting_run_to_trade.Size = new System.Drawing.Size(23, 22);
			this.m_btn_backtesting_run_to_trade.Text = "Run to Trade";
			// 
			// m_btn_backtesting_run
			// 
			this.m_btn_backtesting_run.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_backtesting_run.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_backtesting_run.Image")));
			this.m_btn_backtesting_run.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_backtesting_run.Name = "m_btn_backtesting_run";
			this.m_btn_backtesting_run.Size = new System.Drawing.Size(23, 22);
			this.m_btn_backtesting_run.Text = "Run";
			// 
			// m_trk_sim_time
			// 
			this.m_trk_sim_time.Name = "m_trk_sim_time";
			this.m_trk_sim_time.Size = new System.Drawing.Size(200, 22);
			this.m_trk_sim_time.Value = 0;
			// 
			// m_cb_sim_timeframe
			// 
			this.m_cb_sim_timeframe.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_sim_timeframe.Name = "m_cb_sim_timeframe";
			this.m_cb_sim_timeframe.Size = new System.Drawing.Size(80, 25);
			// 
			// m_tb_sim_time
			// 
			this.m_tb_sim_time.BackColor = System.Drawing.SystemColors.Control;
			this.m_tb_sim_time.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tb_sim_time.Name = "m_tb_sim_time";
			this.m_tb_sim_time.Size = new System.Drawing.Size(140, 25);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// MainUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1025, 718);
			this.Controls.Add(this.m_tsc);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.m_menu;
			this.Name = "MainUI";
			this.Text = "Coin Flip";
			this.m_tsc.BottomToolStripPanel.ResumeLayout(false);
			this.m_tsc.BottomToolStripPanel.PerformLayout();
			this.m_tsc.ContentPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.PerformLayout();
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			this.m_ss.ResumeLayout(false);
			this.m_ss.PerformLayout();
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			this.m_ts.ResumeLayout(false);
			this.m_ts.PerformLayout();
			this.m_ts_backtesting.ResumeLayout(false);
			this.m_ts_backtesting.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>The main entry point for the application.</summary>
		[STAThread] static void Main()
		{
			try
			{
				// Create an event handle to prevent multiple instances
				Debug.WriteLine("ClipFlip is running as a {0}bit process".Fmt(Environment.Is64BitProcess ? 64 : 32));
				using (var app_running = new EventWaitHandle(true, EventResetMode.AutoReset, "CoinFlip", out var was_created))
				{
					// Only run the app if we created the event handle
					if (was_created || Util.IsDebug)
					{
						// Ensure required dlls are loaded
						Sci.LoadDll();
						Sqlite.LoadDll();
						View3d.LoadDll();

						Application.EnableVisualStyles();
						Application.SetCompatibleTextRenderingDefault(false);

						// A view3d context reference the lives for the lifetime of the application
						using (var view3d = new View3d(bgra_compatibility: true))
						using (var ui = new MainUI())
							Application.Run(ui);
					}
					else
					{
						// Find the order instance and bring it to the front
						//todo
					}
				}
			}
			catch (OperationCanceledException) {}
			#if !DEBUG
			catch (Exception ex)
			{
				MsgBox.Show(null, ex.MessageFull(), "Crash!", MessageBoxButtons.OK);
			}
			#endif
		}
	}

	#region Helper Structs
	public class ListElementProxy<T>
	{
		private readonly List<T> m_list;
		private readonly int m_index;
		public ListElementProxy(List<T> list, int index)
		{
			m_list = list;
			m_index = index;
		}
		public T Value
		{
			get { return m_list[m_index]; }
			set { m_list[m_index] = value; }
		}
	}
#endregion
}
