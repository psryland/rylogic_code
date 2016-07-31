using System;
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
		private ToolStripSeparator m_menu_file_sep0;
		private StatusStrip m_ss;
		private ToolStripStatusLabel m_status;
		private ToolStripMenuItem m_menu_file_dump;
		private ToolStripMenuItem m_menu_file_test_msg;
		private ToolStripMenuItem m_menu_file_exit;
		#endregion

		public MainUI()
		{
			InitializeComponent();

			Settings = new Settings(@"Settings.xml"){AutoSaveOnChanges = true};
			Model = new MainModel(this, Settings);

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			m_dc.Dispose();
			Model = null;
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
					Util.Dispose(ref m_impl_model);
				}
				m_impl_model = value;
				if (m_impl_model != null)
				{
					m_impl_model.MarketData.InstrumentRemoved += HandleInstrumentRemoved;
				}
				Update();
			}
		}
		private MainModel m_impl_model;

		/// <summary>App settings</summary>
		public Settings Settings
		{
			get;
			private set;
		}

		/// <summary>Main application status label</summary>
		public ToolStripStatusLabel Status
		{
			get { return m_status; }
		}

		/// <summary>Add a chart to the dock container</summary>
		public void AddChart(ChartUI chart)
		{
			m_dc.Add(chart);
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			#region Menu
			m_menu_file_test_msg.Click += (s,a) =>
			{
				Model.TradeDataSource.Post(new OutMsg.TestMsg { Text = "Hi From Tradee" });
			};
			m_menu_file_dump.Click += (s,a) =>
			{
				Model.Dump();
			};
			m_menu_file_exit.Click += (s, a) =>
			{
				Close();
			};
			#endregion

			// Add components
			m_dc.Add2(new StatusUI(Model));
			m_dc.Add2(new AlarmClockUI(Model));
			m_dc.Add2(new TradesUI(Model));
			m_dc.Add2(new InstrumentsUI(Model));
			m_menu.Items.Add(m_dc.WindowsMenu());

			// Restore the UI layout
			if (Settings.UI.UILayout != null)
			{
				try { m_dc.LoadLayout(Settings.UI.UILayout); }
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
			if (Model == null)
				return;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainUI));
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_ss = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_dc = new pr.gui.DockContainer();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_test_msg = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_dump = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_sep0 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
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
            this.m_menu_file});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(906, 24);
			this.m_menu.TabIndex = 0;
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_test_msg,
            this.m_menu_file_dump,
            this.m_menu_file_sep0,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_test_msg
			// 
			this.m_menu_file_test_msg.Name = "m_menu_file_test_msg";
			this.m_menu_file_test_msg.Size = new System.Drawing.Size(152, 22);
			this.m_menu_file_test_msg.Text = "&Test Msg";
			// 
			// m_menu_file_dump
			// 
			this.m_menu_file_dump.Name = "m_menu_file_dump";
			this.m_menu_file_dump.Size = new System.Drawing.Size(152, 22);
			this.m_menu_file_dump.Text = "&Dump";
			// 
			// m_menu_file_sep0
			// 
			this.m_menu_file_sep0.Name = "m_menu_file_sep0";
			this.m_menu_file_sep0.Size = new System.Drawing.Size(149, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(152, 22);
			this.m_menu_file_exit.Text = "E&xit";
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
