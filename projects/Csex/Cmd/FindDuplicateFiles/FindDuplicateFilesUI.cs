using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace Csex
{
	public class FindDuplicateFilesUI :Form
	{
		private Model m_model;

		public FindDuplicateFilesUI()
		{
			InitializeComponent();
			m_model = new Model(this);

			SetupMenu();
			SetupDockContent();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Hook up the menu handlers</summary>
		private void SetupMenu()
		{
			m_menu_file_search_dirs.Click += (s,a) =>
				{
					ChooseDirectories();
				};
			m_menu_file_exit.Click += (s,a) =>
				{
					Close();
				};
		}

		/// <summary>Hook up the dock panels</summary>
		private void SetupDockContent()
		{
			m_dock.Add(new MainControls(m_model), EDockSite.Left);
			m_dock.Add(new DuplicateResults(m_model), EDockSite.Bottom);
		}

		/// <summary>Select the directories to look in for duplicate files</summary>
		private void ChooseDirectories()
		{
			using (var dlg = new SelectDirectoriesUI(m_model.Settings.SearchPaths))
			{
				dlg.ShowDialog();
				m_model.Settings.SearchPaths = dlg.Paths.Select(x => x.Text).ToArray();
			}
		}

		///// <summary>Wrap the given control in a dockable form</summary>
		//private static DockContent MakeDockable<TCtrl>(TCtrl ctrl, string title, DockPanel dock, ToolStripMenuItem windows_menu) where TCtrl:Control
		//{
		//	var form = new DockContent(ctrl.GetType().ToString())
		//	{
		//		Icon = null,
		//		Text = title,
		//		ShowIcon = false,
		//		ShowInTaskbar = true,
		//		HideOnClose = true,
		//		MaximizeBox = true,
		//		MinimizeBox = true,
		//		ControlBox = true,
		//	};
		//	ctrl.Dock = DockStyle.Fill;
		//	form.Controls.Add(ctrl);
		//	//form.DockStateChanged += (s,a) => settings.Save();

		//	if (windows_menu != null)
		//	{
		//		var menu_entry = new ToolStripMenuItem(form.Text, null, (s,a) =>
		//			{
		//				// Ensure the window is onscreen
		//				if (form.IsFloat)
		//				{
		//					var area = Screen.GetWorkingArea(((ToolStripMenuItem)s).OwnerItem.Owner);
		//					var b = form.FloatPane.Parent.Bounds;
		//					var pt = form.FloatPane.Parent.Location;
		//					if (b.Right  > area.Right ) pt.X -= b.Right  - area.Right;
		//					if (b.Bottom > area.Bottom) pt.Y -= b.Bottom - area.Bottom;
		//					if (b.Left   < area.Left  ) pt.X += area.Left - b.Left;
		//					if (b.Top    < area.Top   ) pt.Y += area.Top  - b.Top;
		//					form.FloatPane.Parent.Location = pt;
		//				}

		//				form.Show(dock);
		//				form.BringToFront();
		//			});
		//		windows_menu.DropDownItems.Add(menu_entry);
		//		windows_menu.DropDownOpening += (s,a) => menu_entry.Checked = form.IsActivated;
		//	}

		//	return form;
		//}

		private ToolStripContainer m_tsc;
		private StatusStrip m_ss;
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private ToolStripMenuItem m_menu_file_search_dirs;
		private ToolStripSeparator m_menu_file_sep0;
		private ToolStripMenuItem m_menu_file_exit;
		private ToolStripStatusLabel m_status;
		private ToolStripMenuItem m_menu_window;
		private DockContainer m_dock;

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources         = new System.ComponentModel.ComponentResourceManager(typeof(FindDuplicateFilesUI));
			this.m_tsc                                                       = new ToolStripContainer();
			this.m_ss                                                        = new System.Windows.Forms.StatusStrip();
			this.m_status                                                    = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_dock                                                      = new DockContainer();
			this.m_menu                                                      = new System.Windows.Forms.MenuStrip();
			this.m_menu_file                                                 = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_search_dirs                                     = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_sep0                                            = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit                                            = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_window                                               = new System.Windows.Forms.ToolStripMenuItem();
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
			this.m_tsc.ContentPanel.Controls.Add(this.m_dock);
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(738, 596);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Margin = new System.Windows.Forms.Padding(0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(738, 642);
			this.m_tsc.TabIndex = 0;
			this.m_tsc.Text = "tsc";
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
			this.m_ss.Size = new System.Drawing.Size(738, 22);
			this.m_ss.TabIndex = 0;
			// 
			// m_status
			// 
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(26, 17);
			this.m_status.Text = "Idle";
			// 
			// m_dock
			// 
			this.m_dock.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_dock.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dock.Location = new System.Drawing.Point(0, 0);
			this.m_dock.Margin = new System.Windows.Forms.Padding(0);
			this.m_dock.Name = "m_dock";
			this.m_dock.Size = new System.Drawing.Size(738, 596);
			this.m_dock.TabIndex = 0;
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file,
            this.m_menu_window});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(738, 24);
			this.m_menu.TabIndex = 0;
			this.m_menu.Text = "menuStrip1";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_search_dirs,
            this.m_menu_file_sep0,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_search_dirs
			// 
			this.m_menu_file_search_dirs.Name = "m_menu_file_search_dirs";
			this.m_menu_file_search_dirs.Size = new System.Drawing.Size(168, 22);
			this.m_menu_file_search_dirs.Text = "Search Directories";
			// 
			// m_menu_file_sep0
			// 
			this.m_menu_file_sep0.Name = "m_menu_file_sep0";
			this.m_menu_file_sep0.Size = new System.Drawing.Size(165, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(168, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// m_menu_window
			// 
			this.m_menu_window.Name = "m_menu_window";
			this.m_menu_window.Size = new System.Drawing.Size(63, 20);
			this.m_menu_window.Text = "&Window";
			// 
			// FindDuplicateFilesUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(738, 642);
			this.Controls.Add(this.m_tsc);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.m_menu;
			this.Name = "FindDuplicateFilesUI";
			this.Text = "Find Duplicate Files";
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
