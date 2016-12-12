using System;
using System.IO;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gui;
using pr.util;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace LDraw
{
	public class ScriptUI :BaseUI
	{
		#region UI Elements
		private ToolStripContainer m_tsc;
		private ToolStripButton m_btn_render;
		private ImageList m_il_toolbar;
		private MenuStrip m_menu;
		private ToolStripButton m_btn_clear;
		private ToolStripMenuItem m_menu_shortcuts;
		private ToolStripMenuItem m_menu_shortcuts_render;
		private ToolStripMenuItem m_menu_shortcuts_clear;
		private ToolStripButton m_btn_open;
		private ToolStripButton m_btn_save;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStrip m_ts;
		#endregion

		public ScriptUI(Model model, Guid? context_id = null)
			:base(model, "Script")
		{
			InitializeComponent();
			ContextId = context_id ?? Guid.NewGuid();
			Filepath = string.Empty;
			Editor = new ScintillaCtrl();

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Editor = null;
			base.Dispose(disposing);
		}

		/// <summary>The editor control</summary>
		public ScintillaCtrl Editor
		{
			get { return m_editor; }
			private set
			{
				if (m_editor == value) return;
				if (m_editor != null)
				{
					m_tsc.ContentPanel.Controls.Remove(m_editor);
				}
				m_editor = value;
				if (m_editor != null)
				{
					m_editor.Dock = DockStyle.Fill;
					m_editor.InitLdrStyle();

					m_tsc.ContentPanel.Controls.Add(m_editor);
				}
			}
		}
		private ScintillaCtrl m_editor;

		/// <summary>A context Id for objects created by this script</summary>
		public Guid ContextId
		{
			get;
			private set;
		}

		/// <summary>The filepath for this script</summary>
		public string Filepath
		{
			get { return m_filepath; }
			set
			{
				m_filepath = value;
				DockControl.TabText = m_filepath.HasValue() ? Path_.FileTitle(m_filepath) : "Script";
			}
		}
		private string m_filepath;

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			#region Menu
			m_menu_shortcuts_render.Click += (s,a) => m_btn_render.PerformClick();
			m_menu_shortcuts_clear.Click += (s,a) => m_btn_clear.PerformClick();
			m_menu.Visible = false;
			#endregion

			#region Tool bar

			// Open file
			m_btn_open.ToolTipText = "Open a file";
			m_btn_open.Click += (s,a) =>
			{
				LoadFile(null);
			};

			// Save file
			m_btn_open.ToolTipText = "Save this script to file";
			m_btn_open.Click += (s,a) =>
			{
				SaveFile(Filepath);
			};

			// Render scene
			m_btn_render.ToolTipText = "Render this script\r\n[F5]";
			m_btn_render.Click += (s,a) =>
			{
				RenderScript();
			};

			// Clear scene
			m_btn_clear.ToolTipText = "Remove objects created by this script\r\n[Ctrl+D]";
			m_btn_clear.Click += (s,a) =>
			{
				ClearScript();
			};

			#endregion
		}

		/// <summary>Update the state of UI Elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
		}

		/// <summary>Load the script UI from a file</summary>
		public void LoadFile(string filepath)
		{
			// Prompt for a filepath if not given
			if (!filepath.HasValue())
			{
				using (var dlg = new OpenFileDialog { Title = "Load Script", Filter = Util.FileDialogFilter("Script Files", "*.ldr") })
				{
					if (dlg.ShowDialog(Model.Owner) != DialogResult.OK) return;
					filepath = dlg.FileName;
				}
			}

			// Save the filepath
			Filepath = filepath;

			// Load the file into the editor
			Editor.Text = File.ReadAllText(filepath);
			SaveNeeded = false;
		}
		public void LoadFile()
		{
			LoadFile(Filepath);
		}

		/// <summary>Save the script in this editor to a file</summary>
		public void SaveFile(string filepath)
		{
			// Prompt for a filepath if not given
			if (!filepath.HasValue())
			{
				using (var dlg = new SaveFileDialog { Title = "Save Script", Filter = Util.FileDialogFilter("Script Files", "*.ldr") })
				{
					if (dlg.ShowDialog(Model.Owner) != DialogResult.OK) return;
					filepath = dlg.FileName;
				}
			}

			// Save the filepath
			Filepath = filepath;

			// Save the file form the editor
			File.WriteAllText(filepath, Editor.Text);
			SaveNeeded = false;
		}
		public void SaveFile()
		{
			SaveFile(Filepath);
		}

		/// <summary>True if the script has been edited</summary>
		public bool SaveNeeded
		{
			get;
			set;
		}

		/// <summary>Remove any objects associated with this script</summary>
		private void ClearScript()
		{
			Model.ContextIds.Remove(ContextId);

			// Remove any objects previously created by this script
			Model.View3d.DeleteAllObjects(ContextId);
			Model.Window.Invalidate();
		}

		/// <summary>Render the script in this window</summary>
		private void RenderScript()
		{
			ClearScript();

			// Need a View3d method for rendering a string containing a scene
			Model.View3d.LoadScript(Editor.Text, false, true, ContextId, null);
			Model.ContextIds.Add(ContextId);
			Model.Window.Invalidate();
		}

		/// <summary>Handle the script changing</summary>
		private void HandleScriptChanged(object sender, EventArgs e)
		{
			SaveNeeded = true;
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScriptUI));
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_shortcuts = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_shortcuts_render = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_shortcuts_clear = new System.Windows.Forms.ToolStripMenuItem();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_btn_open = new System.Windows.Forms.ToolStripButton();
			this.m_btn_save = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_render = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_clear = new System.Windows.Forms.ToolStripButton();
			this.m_il_toolbar = new System.Windows.Forms.ImageList(this.components);
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.m_ts.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(495, 575);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(495, 630);
			this.m_tsc.TabIndex = 0;
			this.m_tsc.Text = "toolStripContainer1";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_menu);
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts);
			// 
			// m_menu
			// 
			this.m_menu.AutoSize = false;
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_shortcuts});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(495, 24);
			this.m_menu.TabIndex = 1;
			this.m_menu.Text = "Hidden, Used to provide key shortcuts";
			// 
			// m_menu_shortcuts
			// 
			this.m_menu_shortcuts.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_shortcuts_render,
            this.m_menu_shortcuts_clear});
			this.m_menu_shortcuts.Name = "m_menu_shortcuts";
			this.m_menu_shortcuts.Size = new System.Drawing.Size(69, 20);
			this.m_menu_shortcuts.Text = "Shortcuts";
			// 
			// m_menu_shortcuts_render
			// 
			this.m_menu_shortcuts_render.Name = "m_menu_shortcuts_render";
			this.m_menu_shortcuts_render.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.m_menu_shortcuts_render.Size = new System.Drawing.Size(152, 22);
			this.m_menu_shortcuts_render.Text = "&Render";
			// 
			// m_menu_shortcuts_clear
			// 
			this.m_menu_shortcuts_clear.Name = "m_menu_shortcuts_clear";
			this.m_menu_shortcuts_clear.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.m_menu_shortcuts_clear.Size = new System.Drawing.Size(152, 22);
			this.m_menu_shortcuts_clear.Text = "&Clear";
			// 
			// m_ts
			// 
			this.m_ts.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.m_ts.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_btn_open,
            this.m_btn_save,
            this.toolStripSeparator1,
            this.m_btn_render,
            this.toolStripSeparator2,
            this.m_btn_clear});
			this.m_ts.Location = new System.Drawing.Point(3, 24);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(136, 31);
			this.m_ts.TabIndex = 0;
			// 
			// m_btn_open
			// 
			this.m_btn_open.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_open.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_open.Image")));
			this.m_btn_open.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_open.Name = "m_btn_open";
			this.m_btn_open.Size = new System.Drawing.Size(28, 28);
			this.m_btn_open.Text = "Open Script";
			this.m_btn_open.ToolTipText = "Open a script file";
			// 
			// m_btn_save
			// 
			this.m_btn_save.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_save.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_save.Image")));
			this.m_btn_save.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_save.Name = "m_btn_save";
			this.m_btn_save.Size = new System.Drawing.Size(28, 28);
			this.m_btn_save.Text = "Save Script";
			this.m_btn_save.ToolTipText = "Save the current script to a file";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 31);
			// 
			// m_btn_render
			// 
			this.m_btn_render.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_render.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_render.Image")));
			this.m_btn_render.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_render.Name = "m_btn_render";
			this.m_btn_render.Size = new System.Drawing.Size(28, 28);
			this.m_btn_render.Text = "Render";
			this.m_btn_render.ToolTipText = "Render this script";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 31);
			// 
			// m_btn_clear
			// 
			this.m_btn_clear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_clear.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_clear.Image")));
			this.m_btn_clear.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_clear.Name = "m_btn_clear";
			this.m_btn_clear.Size = new System.Drawing.Size(28, 28);
			this.m_btn_clear.Text = "Clear";
			this.m_btn_clear.ToolTipText = "Remove objects, created by this script, from the scene";
			// 
			// m_il_toolbar
			// 
			this.m_il_toolbar.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.m_il_toolbar.ImageSize = new System.Drawing.Size(16, 16);
			this.m_il_toolbar.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// ScriptUI
			// 
			this.Controls.Add(this.m_tsc);
			this.Name = "ScriptUI";
			this.Size = new System.Drawing.Size(495, 630);
			this.m_tsc.ContentPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.PerformLayout();
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			this.m_ts.ResumeLayout(false);
			this.m_ts.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
