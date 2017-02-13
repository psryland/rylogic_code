//*************************************************************************************************
// CoreCalc
// Copyright (c) Rylogic Ltd 2016
//*************************************************************************************************
//#define TRAP_UNHANDLED_EXCEPTIONS
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.scintilla;
using pr.util;

namespace CoreCalc
{
	public class MainUI :Form
	{
		#region UI Elements
		private TableLayoutPanel m_table0;
		private pr.gui.ToolStripContainer m_tsc;
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private ToolStripMenuItem m_menu_file_exit;
		private DockContainer m_dc;
		private ToolTip m_tt;
		#endregion

		/// <summary>The main entry point for the application.</summary>
		[STAThread] static void Main()
		{
			var unhandled = (Exception)null;
			#if !DEBUG || TRAP_UNHANDLED_EXCEPTIONS
			try {
			#endif

				Debug.WriteLine("{0} is a {1}bit process".Fmt(Application.ExecutablePath, Environment.Is64BitProcess?"64":"32"));
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				#if DEBUG
				pr.util.Util.WaitForDebugger();
				#endif

				// Load dlls
				try { View3d.LoadDll(); }
				catch (DllNotFoundException ex)
				{
					if (Util.IsInDesignMode) return;
					MessageBox.Show(ex.Message);
				}
				try { Sci.LoadDll(); }
				catch (DllNotFoundException ex)
				{
					if (Util.IsInDesignMode) return;
					MessageBox.Show(ex.Message);
				}

				Application.Run(new MainUI());

				// To catch any Disposes in the 'GC Finializer' thread
				GC.Collect();

			#if !DEBUG || TRAP_UNHANDLED_EXCEPTIONS
			} catch (Exception e) { unhandled = e; }
			#endif

			// Report unhandled exceptions
			if (unhandled != null)
			{
				var crash_dump_file = Util.ResolveAppDataPath("Rylogic", "CoreCalc", "crash_report.txt");
				var crash_report = Str.Build(
					"Unhandled exception: ",unhandled.GetType().Name,"\r\n",
					"Message: ",unhandled.MessageFull(),"\r\n",
					"Date: ",DateTimeOffset.Now,"\r\n",
					"Stack:\r\n", unhandled.StackTrace);

				File.WriteAllText(crash_dump_file, crash_report);
				var res = MessageBox.Show(Str.Build(
					"Shutting down due to an unhandled exception.\n",
					unhandled.MessageFull(),"\n\n",
					"A crash report has been generated here:\n",
					crash_dump_file,"\n\n"),
					"Unexpected Shutdown", MessageBoxButtons.OK);
			}
		}
		public MainUI()
		{
			InitializeComponent();
			Model = new Model(this);

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>App logic</summary>
		private Model Model
		{
			get;
			set;
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			m_dc.Add(new CoreOrientationUI(Model), EDockSite.Left);
			m_dc.GetDockSizes().Left = 0.4f;

			m_menu_file_exit.Click += (s,a) =>
			{
				Close();
			};
			m_menu.Items.Add(m_dc.WindowsMenu());
		}

		private void UpdateUI()
		{
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_table0 = new System.Windows.Forms.TableLayoutPanel();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_dc = new pr.gui.DockContainer();
			this.m_table0.SuspendLayout();
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_table0
			// 
			this.m_table0.ColumnCount = 2;
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Controls.Add(this.m_dc, 1, 1);
			this.m_table0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table0.Location = new System.Drawing.Point(0, 0);
			this.m_table0.Name = "m_table0";
			this.m_table0.RowCount = 2;
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Size = new System.Drawing.Size(708, 593);
			this.m_table0.TabIndex = 0;
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Controls.Add(this.m_table0);
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(708, 593);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Margin = new System.Windows.Forms.Padding(0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(708, 617);
			this.m_tsc.TabIndex = 1;
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
			this.m_menu.Size = new System.Drawing.Size(708, 24);
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
			// m_dc
			// 
			this.m_dc.ActiveContent = null;
			this.m_dc.ActiveDockable = null;
			this.m_dc.ActivePane = null;
			this.m_dc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dc.Location = new System.Drawing.Point(0, 0);
			this.m_dc.Margin = new System.Windows.Forms.Padding(0);
			this.m_dc.Name = "m_dc";
			this.m_dc.Size = new System.Drawing.Size(708, 593);
			this.m_dc.TabIndex = 0;
			this.m_dc.Text = "dockContainer1";
			// 
			// MainUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(708, 617);
			this.Controls.Add(this.m_tsc);
			this.MainMenuStrip = this.m_menu;
			this.Name = "MainUI";
			this.Text = "Core Orientation Calculator";
			this.m_table0.ResumeLayout(false);
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
