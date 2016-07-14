using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.extn;
using pr.gui;
using pr.util;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace Tradee
{
	public class MainUI :Form
	{
		/// <summary>A named event so that only one instance of Tradee runs at a time</summary>
		private static EventWaitHandle m_app_running;

		#region UI Elements
		private DockContainer m_dc;
		private ToolStripContainer m_tsc;
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private ToolStripSeparator m_menu_file_sep0;
		private StatusStrip m_ss;
		private ToolStripStatusLabel m_status;
		private ToolStripMenuItem m_menu_file_exit;
		#endregion

		/// <summary>The main entry point for the application.</summary>
		[STAThread] static void Main()
		{
			try
			{
				// Create an event handle to prevent multiple instances
				bool was_created;
				m_app_running = new EventWaitHandle(true, EventResetMode.AutoReset, "TradeeRunningEvent", out was_created);
				if (!was_created)
					return;

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new MainUI());
			}
			catch (Exception ex)
			{
				MsgBox.Show(null, ex.MessageFull(), "Crash!", MessageBoxButtons.OK);
			}
		}
		public MainUI()
		{
			InitializeComponent();

			SetupUI();
			UpdateUI();

			// Create the IPC service host
			RunServer = true;
		}
		protected override void Dispose(bool disposing)
		{
			RunServer = false;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>True while the pipe server thread is running</summary>
		private bool RunServer
		{
			get { return m_srv != null; }
			set
			{
				if (RunServer == value) return;
				if (value)
				{
					// The background thread for servicing the clients
					m_srv = new Thread(new ThreadStart(() =>
					{
						m_srv.Name = "Pipe Server";
						for (;RunServer;)
						{
							try
							{
								//'use a semaphore
								// Wait for the next client connection
								var pipe = new NamedPipeServerStream("TradeePipeIn", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.None) { ReadMode = PipeTransmissionMode.Message };
								pipe.WaitForConnection();
								ThreadPool.QueueUserWorkItem(p =>
								{
									// Read and dispatch the message from the pipe
									try
									{
										var pipe_ = (NamedPipeServerStream)p;
										var obj = new BinaryFormatter().Deserialize(pipe_);
										if (obj != null)
											this.BeginInvoke(() => DispatchMsg(obj));
									}
									catch (Exception ex)
									{
										Debug.WriteLine("De-serialisation failed: {0}".Fmt(ex.MessageFull()));
									}
								}, pipe);
							}
							catch (Exception ex)
							{
								Debug.WriteLine("Pipe server failed: {0}".Fmt(ex.MessageFull()));
							}
						}
					}));
					m_srv.Start();
				}
				else
				{
					var srv = m_srv;
					m_srv = null; // Signal thread exit
					try { new NamedPipeClientStream(".", "TradeePipeIn").Connect(); } catch { } // Connect to wake up from WaitForConnection
					srv.Join();
				}
			}
		}
		private Thread m_srv;

		/// <summary>Handle messages received on the pipe</summary>
		private void DispatchMsg(object obj)
		{
			// Dispatch received messages
			switch (obj.GetType().Name)
			{
			case nameof(HelloMsg):
				m_status.SetStatusMessage(msg:((HelloMsg)obj).Msg, display_time:TimeSpan.FromMilliseconds(500));
				break;
			}
		}

		///// <summary>Raised when total risk updates are received</summary>
		//public event Action<TotalRisk> TotalRisk;

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			// Menu
			m_menu_file_exit.Click += (s, a) =>
			{
				Close();
			};

			// Add components
			m_dc.Add2(new StatusUI(this));
			m_menu.Items.Insert(1, m_dc.WindowsMenu());
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainUI));
			this.m_dc = new pr.gui.DockContainer();
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_sep0 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_ss = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_tsc.BottomToolStripPanel.SuspendLayout();
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.m_ss.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_dc
			// 
			this.m_dc.ActiveContent = null;
			this.m_dc.ActiveDockable = null;
			this.m_dc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dc.Location = new System.Drawing.Point(0, 0);
			this.m_dc.Name = "m_dc";
			this.m_dc.Size = new System.Drawing.Size(520, 526);
			this.m_dc.TabIndex = 2;
			this.m_dc.Text = "dockContainer1";
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
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(520, 526);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(520, 572);
			this.m_tsc.TabIndex = 3;
			this.m_tsc.Text = "toolStripContainer1";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_menu);
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(520, 24);
			this.m_menu.TabIndex = 0;
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_sep0,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_sep0
			// 
			this.m_menu_file_sep0.Name = "m_menu_file_sep0";
			this.m_menu_file_sep0.Size = new System.Drawing.Size(89, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(92, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// m_ss
			// 
			this.m_ss.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ss.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status});
			this.m_ss.Location = new System.Drawing.Point(0, 0);
			this.m_ss.Name = "m_ss";
			this.m_ss.Size = new System.Drawing.Size(520, 22);
			this.m_ss.TabIndex = 0;
			// 
			// m_status
			// 
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(26, 17);
			this.m_status.Text = "Idle";
			// 
			// MainUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(520, 572);
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
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			this.m_ss.ResumeLayout(false);
			this.m_ss.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
