using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.extn;
using pr.gui;
using pr.util;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace PaulsBot
{
	public class MainUI :Form
	{
		#region UI Elements
		private DockContainer m_dc;
		private ToolStripContainer m_tsc;
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private ToolStripMenuItem m_menu_file_restart_pipe;
		private ToolStripSeparator m_menu_file_sep0;
		private ToolStripMenuItem m_menu_file_exit;
		#endregion

		/// <summary>The main entry point for the application.</summary>
		[STAThread] static void Main()
		{
			try
			{
				// Force the Msgs.dll to be loaded
				GC.KeepAlive(typeof(TotalRisk));
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

			// Start the pipe receiver
			Srv = new Thread(new ThreadStart(SrvThreadEntry));
		}
		protected override void Dispose(bool disposing)
		{
			Srv = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The background thread for servicing the pipe</summary>
		private Thread Srv
		{
			get { return m_srv; }
			set
			{
				if (m_srv == value) return;
				if (m_srv != null)
				{
					RunServer = false;
					m_srv.Join();
				}
				m_srv = value;
				if (m_srv != null)
				{
					RunServer = true;
					m_srv.Start();
				}
			}
		}
		private Thread m_srv;

		/// <summary>True while the pipe server thread is running</summary>
		private bool RunServer { get; set; }

		/// <summary>Background thread entry point for handling incoming messages</summary>
		private void SrvThreadEntry()
		{
			try
			{
				using (var pipe = new NamedPipeServerStream("PaulsBotPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.None))
				{
					var buf = new byte[1024];
					var xml = new StringBuilder();
					for (; RunServer;)
					{
						// Re-connect the pipe if broken
						if (!pipe.IsConnected)
						{
							Action update_title = () => Text = "PaulsBot - {0}".Fmt(pipe.IsConnected ? "Connected" : "Disconnected");
							BeginInvoke(update_title);
							try { pipe.WaitForConnection(); } catch (IOException) { pipe.Disconnect(); continue; }
							BeginInvoke(update_title);
						}

						// Read a message from the pipe
						xml.Length = 0;
						do
						{
							var len = pipe.Read(buf, 0, buf.Length);
							xml.Append(Encoding.UTF8.GetString(buf, 0, len));
						}
						while (!pipe.IsMessageComplete);

						// De-serialise the message
						if (xml.Length != 0)
						{
							try
							{
								var msg = XElement.Parse(xml.ToString()).ToObject();
								switch (msg.GetType().Name)
								{
								default: throw new Exception("Unknown message type");
								case nameof(TotalRisk): this.BeginInvoke(() => TotalRisk.Raise((TotalRisk)msg)); break;
								}
							}
							catch (Exception)
							{
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.BeginInvoke(() => MsgBox.Show(this, ex.Message, "Pipe Server Thread Exit", MessageBoxButtons.OK, MessageBoxIcon.Error));
			}
		}

		/// <summary>Raised when total risk updates are received</summary>
		public event Action<TotalRisk> TotalRisk;

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			// Menu
			m_menu_file_restart_pipe.Click += (s,a) =>
			{
				Srv = null;
				Srv = new Thread(new ThreadStart(SrvThreadEntry));
			};
			m_menu_file_exit.Click += (s,a) =>
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
			this.m_dc = new pr.gui.DockContainer();
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_restart_pipe = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_sep0 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_dc
			// 
			this.m_dc.ActiveContent = null;
			this.m_dc.ActiveDockable = null;
			this.m_dc.ActivePane = null;
			this.m_dc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dc.Location = new System.Drawing.Point(0, 0);
			this.m_dc.Name = "m_dc";
			this.m_dc.Size = new System.Drawing.Size(520, 548);
			this.m_dc.TabIndex = 2;
			this.m_dc.Text = "dockContainer1";
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Controls.Add(this.m_dc);
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(520, 548);
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
            this.m_menu_file_restart_pipe,
            this.m_menu_file_sep0,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_restart_pipe
			// 
			this.m_menu_file_restart_pipe.Name = "m_menu_file_restart_pipe";
			this.m_menu_file_restart_pipe.Size = new System.Drawing.Size(152, 22);
			this.m_menu_file_restart_pipe.Text = "&Restart Pipe";
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
			this.ClientSize = new System.Drawing.Size(520, 572);
			this.Controls.Add(this.m_tsc);
			this.MainMenuStrip = this.m_menu;
			this.Name = "MainUI";
			this.Text = "Pauls Bot";
			this.m_tsc.ContentPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.PerformLayout();
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
