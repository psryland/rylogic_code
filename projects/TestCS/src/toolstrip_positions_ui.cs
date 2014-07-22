using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.extn;

namespace TestCS
{
	public class ToolStripPositionsUI :Form
	{
		private static ToolStripLocations m_locations = null;
		
		public ToolStripPositionsUI()
		{
			InitializeComponent();
			foreach (var ts in Controls.OfType<ToolStrip>())
				ts.Dock = DockStyle.Right;

			m_menu_save.Click += (s,a) =>
				{
					m_locations = m_toolstripcont.SaveLocations();
				};
			m_menu_load.Click += (s,a) =>
				{
					if (m_locations == null) return;
					m_toolstripcont.LoadLocations(m_locations);
				};

			if (m_locations != null)
				m_toolstripcont.LoadLocations(m_locations);
		}

		private ToolStripContainer m_toolstripcont;
		private MenuStrip m_menu;
		private ToolStripMenuItem menuToolStripMenuItem;
		private ToolStrip toolStrip3;
		private ToolStripLabel m_toolstrip3;
		private ToolStripButton toolStripButton3;
		private ToolStripComboBox toolStripComboBox1;
		private ToolStrip toolStrip1;
		private ToolStripLabel m_toolstrip1;
		private ToolStripButton toolStripButton1;
		private ToolStrip toolStrip2;
		private ToolStripLabel m_toolstrip2;
		private ToolStripButton toolStripButton2;
		private ToolStripMenuItem m_menu_save;
		private ToolStripMenuItem m_menu_load;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ToolStripPositionsUI));
			this.m_toolstripcont = new System.Windows.Forms.ToolStripContainer();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.menuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripComboBox1 = new System.Windows.Forms.ToolStripComboBox();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.m_toolstrip1 = new System.Windows.Forms.ToolStripLabel();
			this.toolStrip2 = new System.Windows.Forms.ToolStrip();
			this.m_toolstrip2 = new System.Windows.Forms.ToolStripLabel();
			this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
			this.toolStrip3 = new System.Windows.Forms.ToolStrip();
			this.m_toolstrip3 = new System.Windows.Forms.ToolStripLabel();
			this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
			this.m_menu_save = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_load = new System.Windows.Forms.ToolStripMenuItem();
			this.m_toolstripcont.LeftToolStripPanel.SuspendLayout();
			this.m_toolstripcont.TopToolStripPanel.SuspendLayout();
			this.m_toolstripcont.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.toolStrip2.SuspendLayout();
			this.toolStrip3.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_toolstripcont
			// 
			// 
			// m_toolstripcont.ContentPanel
			// 
			this.m_toolstripcont.ContentPanel.Size = new System.Drawing.Size(226, 184);
			this.m_toolstripcont.Dock = System.Windows.Forms.DockStyle.Fill;
			// 
			// m_toolstripcont.LeftToolStripPanel
			// 
			this.m_toolstripcont.LeftToolStripPanel.Controls.Add(this.toolStrip3);
			this.m_toolstripcont.Location = new System.Drawing.Point(0, 0);
			this.m_toolstripcont.Name = "m_toolstripcont";
			this.m_toolstripcont.Size = new System.Drawing.Size(284, 261);
			this.m_toolstripcont.TabIndex = 0;
			this.m_toolstripcont.Text = "toolStripContainer1";
			// 
			// m_toolstripcont.TopToolStripPanel
			// 
			this.m_toolstripcont.TopToolStripPanel.Controls.Add(this.m_menu);
			this.m_toolstripcont.TopToolStripPanel.Controls.Add(this.toolStrip1);
			this.m_toolstripcont.TopToolStripPanel.Controls.Add(this.toolStrip2);
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuToolStripMenuItem,
            this.toolStripComboBox1});
			this.m_menu.Location = new System.Drawing.Point(3, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(273, 27);
			this.m_menu.Stretch = false;
			this.m_menu.TabIndex = 0;
			this.m_menu.Text = "menuStrip1";
			// 
			// menuToolStripMenuItem
			// 
			this.menuToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_save,
            this.m_menu_load});
			this.menuToolStripMenuItem.Name = "menuToolStripMenuItem";
			this.menuToolStripMenuItem.Size = new System.Drawing.Size(50, 23);
			this.menuToolStripMenuItem.Text = "Menu";
			// 
			// toolStripComboBox1
			// 
			this.toolStripComboBox1.Name = "toolStripComboBox1";
			this.toolStripComboBox1.Size = new System.Drawing.Size(121, 23);
			// 
			// toolStrip1
			// 
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_toolstrip1,
            this.toolStripButton1});
			this.toolStrip1.Location = new System.Drawing.Point(3, 27);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(92, 25);
			this.toolStrip1.TabIndex = 1;
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
			this.toolStripButton1.Text = "toolStripButton1";
			// 
			// m_toolstrip1
			// 
			this.m_toolstrip1.Name = "m_toolstrip1";
			this.m_toolstrip1.Size = new System.Drawing.Size(57, 22);
			this.m_toolstrip1.Text = "toolstrip1";
			// 
			// toolStrip2
			// 
			this.toolStrip2.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_toolstrip2,
            this.toolStripButton2});
			this.toolStrip2.Location = new System.Drawing.Point(95, 27);
			this.toolStrip2.Name = "toolStrip2";
			this.toolStrip2.Size = new System.Drawing.Size(92, 25);
			this.toolStrip2.TabIndex = 2;
			// 
			// m_toolstrip2
			// 
			this.m_toolstrip2.Name = "m_toolstrip2";
			this.m_toolstrip2.Size = new System.Drawing.Size(57, 22);
			this.m_toolstrip2.Text = "toolstrip2";
			// 
			// toolStripButton2
			// 
			this.toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
			this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton2.Name = "toolStripButton2";
			this.toolStripButton2.Size = new System.Drawing.Size(23, 22);
			this.toolStripButton2.Text = "toolStripButton2";
			// 
			// toolStrip3
			// 
			this.toolStrip3.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_toolstrip3,
            this.toolStripButton3});
			this.toolStrip3.Location = new System.Drawing.Point(0, 66);
			this.toolStrip3.Name = "toolStrip3";
			this.toolStrip3.Size = new System.Drawing.Size(58, 52);
			this.toolStrip3.TabIndex = 3;
			// 
			// m_toolstrip3
			// 
			this.m_toolstrip3.Name = "m_toolstrip3";
			this.m_toolstrip3.Size = new System.Drawing.Size(56, 15);
			this.m_toolstrip3.Text = "toolstrip3";
			// 
			// toolStripButton3
			// 
			this.toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton3.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton3.Image")));
			this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton3.Name = "toolStripButton3";
			this.toolStripButton3.Size = new System.Drawing.Size(56, 20);
			this.toolStripButton3.Text = "toolStripButton2";
			// 
			// m_menu_save
			// 
			this.m_menu_save.Name = "m_menu_save";
			this.m_menu_save.Size = new System.Drawing.Size(164, 22);
			this.m_menu_save.Text = "Save Positions";
			// 
			// m_menu_load
			// 
			this.m_menu_load.Name = "m_menu_load";
			this.m_menu_load.Size = new System.Drawing.Size(164, 22);
			this.m_menu_load.Text = "Restore Positions";
			// 
			// ToolStripPositionsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.Controls.Add(this.m_toolstripcont);
			this.MainMenuStrip = this.m_menu;
			this.Name = "ToolStripPositionsUI";
			this.Text = "toolstrip_positions_ui";
			this.m_toolstripcont.LeftToolStripPanel.ResumeLayout(false);
			this.m_toolstripcont.LeftToolStripPanel.PerformLayout();
			this.m_toolstripcont.TopToolStripPanel.ResumeLayout(false);
			this.m_toolstripcont.TopToolStripPanel.PerformLayout();
			this.m_toolstripcont.ResumeLayout(false);
			this.m_toolstripcont.PerformLayout();
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.toolStrip2.ResumeLayout(false);
			this.toolStrip2.PerformLayout();
			this.toolStrip3.ResumeLayout(false);
			this.toolStrip3.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion
	}
}
