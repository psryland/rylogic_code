using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoinFlip.Properties;
using pr.container;
using pr.extn;
using pr.gui;
using pr.util;
using ToolStripContainer = pr.gui.ToolStripContainer;

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
		private GridFishing m_grid_fishing;
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private ToolStripMenuItem m_menu_file_exit;
		private DockContainer m_dc;
		private ToolStrip m_ts;
		private ToolStripButton m_chk_run;
		private ToolStripButton m_chk_allow_trades;
		private ToolStripMenuItem toolsToolStripMenuItem;
		private ToolStripMenuItem m_menu_tools_show_pairs;
		private ToolStripMenuItem m_menu_tools_show_loops;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripLabel m_lbl_nett_worth;
		private ToolStripTextBox m_tb_nett_worth;
		private ToolStripMenuItem m_menu_tools_test;
		private ToolStripLabel m_lbl_token_total;
		private ToolStripTextBox m_tb_token_total;
		private ToolStripSeparator toolStripSeparator1;
		private LogUI m_log;
		#endregion

		public MainUI()
		{
			InitializeComponent();

			Model = new Model(this);
			LoopsUI = new LoopsUI(Model, this);
			PairsUI = new PairsUI(Model, this);

			SetupUI();
			UpdateUI();

			RestoreWindowPosition();
		}
		protected override void Dispose(bool disposing)
		{
			LoopsUI = null;
			PairsUI = null;
			Model = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnResizeEnd(EventArgs e)
		{
			base.OnResizeEnd(e);
			Settings.UI.WindowPosition = Bounds;
			Settings.UI.UILayout = m_dc.SaveLayout();
		}
		protected async override void OnClosing(CancelEventArgs e)
		{
			// Disable 'Run' mode
			Model.RunLoopFinder = false;

			// Save the layout and window position
			Settings.UI.UILayout = m_dc.SaveLayout();

			// Form shutdown is a PITA when using async methods.
			// Disable the form while we wait for shutdown to be allowed
			Enabled = false;
			if (Model.Running)
			{
				e.Cancel = true;
				await Model.ShutdownAsync();
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
					m_model.Pairs.ListChanging     -= HandlePairsChanging;
					m_model.Coins.ListChanging     -= HandleCoinsListChanging;
					m_model.Exchanges.ListChanging -= HandleExchangesChanging;
					m_model.MarketDataChanging     -= HandleMarketDataChanging;
					m_model.AllowTradesChanged     -= UpdateUI;
					m_model.RunChanged             -= UpdateUI;
					m_grid_coins.DataSource         = null;
					m_grid_exchanges.DataSource     = null;
					m_grid_balances.DataSource      = null;
					m_grid_positions.DataSource     = null;
					m_grid_history.DataSource       = null;
					Util.Dispose(ref m_model);
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.MarketDataChanging     += HandleMarketDataChanging;
					m_model.Exchanges.ListChanging += HandleExchangesChanging;
					m_model.Coins.ListChanging     += HandleCoinsListChanging;
					m_model.Pairs.ListChanging     += HandlePairsChanging;
					m_model.AllowTradesChanged     += UpdateUI;
					m_model.RunChanged             += UpdateUI;
				}
			}
		}
		private Model m_model;

		/// <summary>The UI for displaying the loops</summary>
		public LoopsUI LoopsUI
		{
			get { return m_loops_ui; }
			private set
			{
				if (m_loops_ui == value) return;
				Util.Dispose(ref m_loops_ui);
				m_loops_ui = value;
			}
		}
		private LoopsUI m_loops_ui;

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

		/// <summary>Wire up the UI</summary>
		private void SetupUI()
		{
			#region Menu
			{
				m_menu_tools_show_pairs.Click += (s,a) =>
				{
					PairsUI.Show();
				};
				m_menu_tools_show_loops.Click += (s,a) =>
				{
					LoopsUI.Show();
				};
				m_menu_tools_test.Visible = Util.IsDebug;
				m_menu_tools_test.Click += (s,a) =>
				{
					Model.Test();
				};
				m_menu_file_exit.Click += (s,a) =>
				{
					Close();
				};
				m_menu.Items.Add(m_dc.WindowsMenu());
			}
			#endregion

			#region Tool bar
			{
				m_chk_run.CheckedChanged += (s,a) =>
				{
					Model.RunLoopFinder = m_chk_run.Checked;
					m_chk_run.Text = m_chk_run.Checked ? "Stop" : "Start";
					m_chk_run.Image = m_chk_run.Checked ? Resources.power_blue : Resources.power_gray;
					m_chk_run.BackColor = m_chk_run.Checked ? Color.LightGreen : SystemColors.Control;
				};
				m_chk_allow_trades.CheckedChanged += (s,a) =>
				{
					Model.AllowTrades = m_chk_allow_trades.Checked;
					m_chk_allow_trades.Text = m_chk_allow_trades.Checked ? "Disable Trading" : "Enable Trading";
					m_chk_allow_trades.BackColor = m_chk_allow_trades.Checked ? Color.LightGreen : SystemColors.Control;
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
				m_grid_fishing = new GridFishing(Model, "Fishing", nameof(m_grid_fishing));
			}
			#endregion

			#region Log control
			{
				m_log = new LogUI("Log", nameof(m_log));
				m_log.LogFilepath = ((LogToFile)Model.Log.LogCB).Filepath;
				m_log.LogEntryPattern = new Regex(@"^(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*)"
					,RegexOptions.Singleline|RegexOptions.Multiline|RegexOptions.CultureInvariant|RegexOptions.Compiled);
			}
			#endregion

			#region Dock Container

			// Add content
			m_dc.Add(m_grid_positions, EDockSite.Centre);
			m_dc.Add(m_grid_history, EDockSite.Centre);
			m_dc.Add(m_grid_exchanges, EDockSite.Top);
			m_dc.Add(m_log, EDockSite.Bottom);
			m_dc.Add(m_grid_coins, EDockSite.Left);
			m_dc.Add(m_grid_balances, EDockSite.Bottom);
			m_dc.Add(m_grid_fishing, new DockContainer.DockLocation(auto_hide:EDockSite.Right));

			// Load layout
			if (Settings.UI.UILayout != null)
			{
				try { m_dc.LoadLayout(Settings.UI.UILayout); }
				catch (Exception ex) { Model.Log.Write(ELogLevel.Error, ex, "Failed to restore UI layout"); }
			}
			#endregion
		}

		/// <summary>Handle the model properties changing</summary>
		private void UpdateUI(object sender = null, EventArgs e = null)
		{
			m_chk_allow_trades.Checked = Model.AllowTrades;
			m_chk_run.Checked = Model.RunLoopFinder;
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

		/// <summary>Model heart beat event handler</summary>
		private void HandleMarketDataChanging(object sender, MarketDataChangingEventArgs e)
		{
			if (!e.Done)
			{
			}
			else
			{
				// Invalidate the live price data 
				m_grid_exchanges.InvalidateColumn(m_grid_exchanges.Columns[nameof(Exchange.NettWorth)].Index);
				m_grid_balances .InvalidateColumn(m_grid_balances.Columns[nameof(Balance.Available)].Index);
				m_grid_positions.InvalidateColumn(m_grid_positions.Columns[nameof(GridPositions.ColumnNames.LivePrice)].Index);
				m_grid_positions.InvalidateColumn(m_grid_positions.Columns[nameof(GridPositions.ColumnNames.PriceDist)].Index);
				m_grid_coins    .InvalidateColumn(m_grid_coins.Columns[nameof(GridCoins.ColumnNames.Total)].Index);

				// Update the nett worth value
				m_tb_nett_worth.Text = Model.NettWorth.ToString("c");

				// Update the sum of tokens
				m_tb_token_total.Text = Model.TokenTotal.ToString("G6");
			}
		}

		/// <summary>Coins of interest changed</summary>
		private void HandleCoinsListChanging(object sender, ListChgEventArgs<CoinData> e)
		{
			m_grid_coins.Invalidate();
			m_grid_balances.Invalidate();
		}

		/// <summary>Exchanges changed</summary>
		private void HandleExchangesChanging(object sender, ListChgEventArgs<Exchange> e)
		{
			m_grid_exchanges.Invalidate();
			m_grid_balances.Invalidate();
			m_grid_positions.Invalidate();
		}

		/// <summary>Available pairs changed</summary>
		private void HandlePairsChanging(object sender, EventArgs e)
		{
			m_grid_exchanges.InvalidateColumn(m_grid_exchanges.Columns[nameof(Exchange.CoinsAvailable)].Index);
			m_grid_exchanges.InvalidateColumn(m_grid_exchanges.Columns[nameof(Exchange.PairsAvailable)].Index);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainUI));
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_ss = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_dc = new pr.gui.DockContainer();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_show_pairs = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_show_loops = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_test = new System.Windows.Forms.ToolStripMenuItem();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_chk_run = new System.Windows.Forms.ToolStripButton();
			this.m_chk_allow_trades = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_lbl_nett_worth = new System.Windows.Forms.ToolStripLabel();
			this.m_tb_nett_worth = new System.Windows.Forms.ToolStripTextBox();
			this.m_lbl_token_total = new System.Windows.Forms.ToolStripLabel();
			this.m_tb_token_total = new System.Windows.Forms.ToolStripTextBox();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_tsc.BottomToolStripPanel.SuspendLayout();
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_ss.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.m_ts.SuspendLayout();
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
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(674, 567);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Margin = new System.Windows.Forms.Padding(0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(674, 638);
			this.m_tsc.TabIndex = 2;
			this.m_tsc.Text = "tsc";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_menu);
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts);
			// 
			// m_ss
			// 
			this.m_ss.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ss.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status});
			this.m_ss.Location = new System.Drawing.Point(0, 0);
			this.m_ss.Name = "m_ss";
			this.m_ss.Size = new System.Drawing.Size(674, 22);
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
			this.m_dc.Size = new System.Drawing.Size(658, 567);
			this.m_dc.TabIndex = 4;
			this.m_dc.Text = "dockContainer1";
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file,
            this.toolsToolStripMenuItem});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(674, 24);
			this.m_menu.TabIndex = 0;
			this.m_menu.Text = "menuStrip1";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(92, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// toolsToolStripMenuItem
			// 
			this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_tools_show_pairs,
            this.m_menu_tools_show_loops,
            this.m_menu_tools_test});
			this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
			this.toolsToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
			this.toolsToolStripMenuItem.Text = "&Tools";
			// 
			// m_menu_tools_show_pairs
			// 
			this.m_menu_tools_show_pairs.Name = "m_menu_tools_show_pairs";
			this.m_menu_tools_show_pairs.Size = new System.Drawing.Size(138, 22);
			this.m_menu_tools_show_pairs.Text = "&Show Pairs";
			// 
			// m_menu_tools_show_loops
			// 
			this.m_menu_tools_show_loops.Name = "m_menu_tools_show_loops";
			this.m_menu_tools_show_loops.Size = new System.Drawing.Size(138, 22);
			this.m_menu_tools_show_loops.Text = "&Show Loops";
			// 
			// m_menu_tools_test
			// 
			this.m_menu_tools_test.Name = "m_menu_tools_test";
			this.m_menu_tools_test.Size = new System.Drawing.Size(138, 22);
			this.m_menu_tools_test.Text = "&TEST";
			// 
			// m_ts
			// 
			this.m_ts.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_chk_allow_trades,
            this.toolStripSeparator2,
            this.m_lbl_nett_worth,
            this.m_tb_nett_worth,
            this.m_lbl_token_total,
            this.m_tb_token_total,
            this.toolStripSeparator1,
            this.m_chk_run});
			this.m_ts.Location = new System.Drawing.Point(3, 24);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(595, 25);
			this.m_ts.TabIndex = 1;
			// 
			// m_chk_run
			// 
			this.m_chk_run.CheckOnClick = true;
			this.m_chk_run.Image = global::CoinFlip.Properties.Resources.power_gray;
			this.m_chk_run.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_chk_run.Name = "m_chk_run";
			this.m_chk_run.Size = new System.Drawing.Size(91, 22);
			this.m_chk_run.Text = "Trade Loops";
			// 
			// m_chk_allow_trades
			// 
			this.m_chk_allow_trades.CheckOnClick = true;
			this.m_chk_allow_trades.Image = global::CoinFlip.Properties.Resources.money;
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
			// m_lbl_token_total
			// 
			this.m_lbl_token_total.Name = "m_lbl_token_total";
			this.m_lbl_token_total.Size = new System.Drawing.Size(71, 22);
			this.m_lbl_token_total.Text = "Token Total:";
			// 
			// m_tb_token_total
			// 
			this.m_tb_token_total.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tb_token_total.Name = "m_tb_token_total";
			this.m_tb_token_total.ReadOnly = true;
			this.m_tb_token_total.Size = new System.Drawing.Size(100, 25);
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
			this.ClientSize = new System.Drawing.Size(674, 638);
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
			this.ResumeLayout(false);

		}
		#endregion
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
