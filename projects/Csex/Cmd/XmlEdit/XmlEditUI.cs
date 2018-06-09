using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Utility;
using ToolStripContainer = Rylogic.Gui.ToolStripContainer;

namespace Csex
{
	public class XmlEditUI :Form
	{
		#region UI Elements
		private ToolStripContainer m_tsc;
		private DockContainer m_dock;
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private ToolStripMenuItem m_menu_file_open;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripMenuItem m_menu_file_save;
		private ToolStripMenuItem m_menu_file_exit;
		#endregion

		public XmlEditUI()
		{
			InitializeComponent();

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_dock);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
			m_menu_file_open.Click += (s,a) =>
			{
				OpenFile();
			};
			m_menu_file_exit.Click += (s,a) =>
			{
				Close();
			};

			m_menu.Items.Add(m_dock.WindowsMenu());
		}

		/// <summary>Open the XML file 'filename'</summary>
		public void OpenFile(string filepath = null)
		{
			try
			{
				if (!filepath.HasValue())
				{
					using (var fd = new OpenFileDialog { Title = "Open an XML File", Filter = Util.FileDialogFilter("Extensible Mark-Up Language", "*.xml") })
					{
						if (fd.ShowDialog(this) != DialogResult.OK) return;
						filepath = fd.FileName;
					}
				}

				var file = new XmlTree(filepath);
				m_dock.Add(file, EDockSite.Centre);
			}
			catch (Exception ex)
			{
				MsgBox.Show(this, $"Failed to load XML file {filepath}\r\n{ex.Message}", "Load XML File Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(XmlEditUI));
			this.m_tsc = new Rylogic.Gui.ToolStripContainer();
			this.m_dock = new Rylogic.Gui.DockContainer();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_save = new System.Windows.Forms.ToolStripMenuItem();
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Controls.Add(this.m_dock);
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(627, 588);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Margin = new System.Windows.Forms.Padding(0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(627, 612);
			this.m_tsc.TabIndex = 2;
			this.m_tsc.Text = "toolStripContainer1";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_menu);
			// 
			// m_dock
			// 
			this.m_dock.ActiveContent = null;
			this.m_dock.ActivePane = null;
			this.m_dock.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dock.Location = new System.Drawing.Point(0, 0);
			this.m_dock.Name = "m_dock";
			this.m_dock.Size = new System.Drawing.Size(627, 588);
			this.m_dock.TabIndex = 1;
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(627, 24);
			this.m_menu.TabIndex = 0;
			this.m_menu.Text = "menuStrip1";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_open,
            this.m_menu_file_save,
            this.toolStripSeparator1,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_open
			// 
			this.m_menu_file_open.Name = "m_menu_file_open";
			this.m_menu_file_open.Size = new System.Drawing.Size(152, 22);
			this.m_menu_file_open.Text = "&Open";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(152, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// m_menu_file_save
			// 
			this.m_menu_file_save.Name = "m_menu_file_save";
			this.m_menu_file_save.Size = new System.Drawing.Size(152, 22);
			this.m_menu_file_save.Text = "&Save";
			// 
			// XmlEditUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(627, 612);
			this.Controls.Add(this.m_tsc);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.m_menu;
			this.Name = "XmlEditUI";
			this.Text = "Xml Edit";
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
