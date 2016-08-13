using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.util;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace Tradee
{
	public class MainUI :Form
	{
		#region Entry Point
		[STAThread] static void Main()
		{
			try
			{
				// Create an event handle to prevent multiple instances
				bool was_created;
				using (var app_running = new EventWaitHandle(true, EventResetMode.AutoReset, "TradeeRunningEvent", out was_created))
				{
					// Only run the app if we created the event handle
					if (was_created)
					{
						// Ensure required dlls are loaded
						View3d.LoadDll();
						Sci.LoadDll();
						Sqlite.LoadDll();

						Application.EnableVisualStyles();
						Application.SetCompatibleTextRenderingDefault(false);
						Application.Run(new MainUI());
					}
				}
			}
			catch (Exception ex)
			{
				MsgBox.Show(null, ex.MessageFull(), "Crash!", MessageBoxButtons.OK);
			}
		}
		#endregion

		#region UI Elements
		private DockContainer m_dc;
		private ToolStripContainer m_tsc;
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private StatusStrip m_ss;
		private ToolStripStatusLabel m_status;
		private ToolStripMenuItem m_menu_file_exit;
		private System.Windows.Forms.Timer m_timer;
		private ToolStripMenuItem m_menu_tools;
		private ToolStripMenuItem m_menu_tools_simulate;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripMenuItem m_menu_tools_req_acct_update;
		private ToolStripMenuItem m_menu_tools_req_trade_history;
		#endregion

		/// <summary>A view3d context reference the lives for the lifetime of the application</summary>
		private View3d m_view3d;

		public MainUI()
		{
			InitializeComponent();

			Settings = new Settings(@"Settings.xml"){AutoSaveOnChanges = true};
			Model = new MainModel(this, Settings);
			m_view3d = new View3d();

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			m_dc.Dispose();
			Model = null;
			Util.Dispose(ref m_sim_ui);
			Util.Dispose(ref m_view3d);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			Settings.UI.UILayout = m_dc.SaveLayout();
			Settings.Save();
			base.OnFormClosed(e);
		}

		/// <summary>The app logic</summary>
		public MainModel Model
		{
			get { return m_impl_model; }
			private set
			{
				if (m_impl_model == value) return;
				if (m_impl_model != null)
				{
					m_impl_model.MarketData.InstrumentRemoved -= HandleInstrumentRemoved;
					m_impl_model.ConnectionChanged -= UpdateUI;
					Util.Dispose(ref m_impl_model);
				}
				m_impl_model = value;
				if (m_impl_model != null)
				{
					m_impl_model.ConnectionChanged += UpdateUI;
					m_impl_model.MarketData.InstrumentRemoved += HandleInstrumentRemoved;
				}
				Update();
			}
		}
		private MainModel m_impl_model;

		/// <summary>App settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Main application status label</summary>
		public ToolStripStatusLabel Status
		{
			get { return m_status; }
		}

		/// <summary>The currently open charts</summary>
		public IEnumerable<ChartUI> Charts
		{
			get { return m_dc.AllContent.OfType<ChartUI>(); }
		}

		/// <summary>Add a new, or show an existing, chart in the UI</summary>
		public void ShowChart(string sym)
		{
			var chart = Charts.FirstOrDefault(x => x.SymbolCode == sym);
			if (chart == null) chart = m_dc.Add2(new ChartUI(Model, Model.MarketData[sym]));
			m_dc.ActiveDockable = chart;
		}

		/// <summary>Open the simulator</summary>
		private void ShowSimulateUI()
		{
			m_sim_ui = m_sim_ui ?? new SimulationUI(this, Model);
			m_sim_ui.Show(this);
		}
		private SimulationUI m_sim_ui;

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			#region Menu
			m_menu_file_exit.Click += (s, a) =>
			{
				Close();
			};
			m_menu_tools_simulate.Click += (s,a) =>
			{
				ShowSimulateUI();
			};
			m_menu_tools_req_acct_update.Click += (s,a) =>
			{
				Model.RefreshAcctStatus();
			};
			m_menu_tools_req_trade_history.Click += (s,a) =>
			{
				Model.RefreshTradeHistory();
			};
			#endregion

			#region Timer
			m_timer.Interval = 1000;
			m_timer.Tick += UpdateUI;
			#endregion

			// Add components
			m_dc.Options.TabStrip.AlwaysShowTabs = true;
			m_dc.Add2(new AccountUI(Model));
			m_dc.Add2(new AlarmClockUI(Model));
			m_dc.Add2(new TradesUI(Model));
			m_dc.Add2(new InstrumentsUI(Model));
			m_menu.Items.Add(m_dc.WindowsMenu());

			// Restore the UI layout
			if (Settings.UI.UILayout != null)
			{
				try
				{
					// Load the layout, create charts on demand
					m_dc.LoadLayout(Settings.UI.UILayout, name =>
					{
						var sym = ChartUI.ToSymbolCode(name);
						var instr = Model.MarketData.GetOrCreateInstrument(sym);
						return new ChartUI(Model, instr).DockControl;
					});
				}
				catch (Exception ex) { Debug.WriteLine("Failed to restore UI Layout: {0}".Fmt(ex.Message)); }
			}
			//else
			//{
			//	var sz = m_dc.GetDockSizes(EDockSite.Centre);
			//	sz.Bottom = (int)(m_dc.Width * 0.33f);
			//}
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			var connected = Model != null && Model.IsConnected;

			// Update the app title
			Text = "Tradee - {0}".Fmt(connected ? "Connected" : "Disconnected");

			// Update menu item state
			m_menu_tools_req_acct_update.Enabled = connected;
			m_menu_tools_req_trade_history.Enabled = connected;
		}

		/// <summary>Close charts that reference the out-going instrument</summary>
		private void HandleInstrumentRemoved(object sender, DataEventArgs e)
		{
			// Remove any charts
			var charts = m_dc.AllContent.OfType<ChartUI>().Where(x => x.Instrument == e.Instrument).ToArray();
			foreach (var chart in charts)
			{
				chart.DockControl.DockPane = null;
				chart.Dispose();
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
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_simulate = new System.Windows.Forms.ToolStripMenuItem();
			this.m_timer = new System.Windows.Forms.Timer(this.components);
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_tools_req_acct_update = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_req_trade_history = new System.Windows.Forms.ToolStripMenuItem();
			this.m_tsc.BottomToolStripPanel.SuspendLayout();
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_ss.SuspendLayout();
			this.m_menu.SuspendLayout();
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
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(906, 628);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(906, 674);
			this.m_tsc.TabIndex = 3;
			this.m_tsc.Text = "toolStripContainer1";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_menu);
			// 
			// m_ss
			// 
			this.m_ss.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ss.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status});
			this.m_ss.Location = new System.Drawing.Point(0, 0);
			this.m_ss.Name = "m_ss";
			this.m_ss.Size = new System.Drawing.Size(906, 22);
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
			this.m_dc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dc.Location = new System.Drawing.Point(0, 0);
			this.m_dc.Name = "m_dc";
			this.m_dc.Size = new System.Drawing.Size(906, 628);
			this.m_dc.TabIndex = 2;
			this.m_dc.Text = "dockContainer1";
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file,
            this.m_menu_tools});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(906, 24);
			this.m_menu.TabIndex = 0;
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
			this.m_menu_file_exit.Size = new System.Drawing.Size(152, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// m_menu_tools
			// 
			this.m_menu_tools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_tools_simulate,
            this.toolStripSeparator1,
            this.m_menu_tools_req_acct_update,
            this.m_menu_tools_req_trade_history});
			this.m_menu_tools.Name = "m_menu_tools";
			this.m_menu_tools.Size = new System.Drawing.Size(47, 20);
			this.m_menu_tools.Text = "&Tools";
			// 
			// m_menu_tools_simulate
			// 
			this.m_menu_tools_simulate.Name = "m_menu_tools_simulate";
			this.m_menu_tools_simulate.Size = new System.Drawing.Size(205, 22);
			this.m_menu_tools_simulate.Text = "&Simulate";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(202, 6);
			// 
			// m_menu_tools_req_acct_update
			// 
			this.m_menu_tools_req_acct_update.Name = "m_menu_tools_req_acct_update";
			this.m_menu_tools_req_acct_update.Size = new System.Drawing.Size(205, 22);
			this.m_menu_tools_req_acct_update.Text = "Request &Account Update";
			// 
			// m_menu_tools_req_trade_history
			// 
			this.m_menu_tools_req_trade_history.Name = "m_menu_tools_req_trade_history";
			this.m_menu_tools_req_trade_history.Size = new System.Drawing.Size(205, 22);
			this.m_menu_tools_req_trade_history.Text = "Request Trade &History";
			// 
			// MainUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(906, 674);
			this.Controls.Add(this.m_tsc);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.m_menu;
			this.Name = "MainUI";
			this.Text = "Tradee";
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
			this.ResumeLayout(false);

		}
		#endregion
	}
}
