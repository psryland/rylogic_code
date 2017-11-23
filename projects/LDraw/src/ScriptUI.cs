using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.gui;
using pr.util;
using pr.win32;
using ToolStripComboBox = pr.gui.ToolStripComboBox;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace LDraw
{
	public class ScriptUI :BaseUI
	{
		// Notes:
		//  - A 'temporary script' is one whose filepath is in the 'Model.TempScriptDirectory'
		//  - 'New' creates a temporary script
		//  - On startup, all temporary files in the user data folder are opened.
		//  - If a temporary script is closed, it is removed from the temp script directory.
		//  - If a temporary file is saved by the user it is moved to the new location and added to recent files.
		public const string DefaultName = "Script";

		#region UI Elements
		private ToolStripContainer m_tsc;
		private ToolStripButton m_btn_render;
		private ImageList m_il_toolbar;
		private ToolStripButton m_btn_clear;
		private ToolStripButton m_btn_save;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripSeparator toolStripSeparator3;
		private ToolStripComboBox m_cb_scene;
		private ToolStrip m_ts;
		#endregion

		public ScriptUI(string name, Model model, string filepath = null)
			:base(model, name)
		{
			InitializeComponent();
			ContextId = Guid.NewGuid();
			Filepath = string.Empty;
			Editor = new ScintillaCtrl();
			ScriptName = name;
			DockControl.TabCMenu = CreateTabCMenu();

			SetupUI();

			// If a filepath is given, load the script with the file.
			// If not, then this is a temporary script. Create a filepath
			// in the temporary scripts folder.
			if (!filepath.HasValue())
				filepath = Path_.CombinePath(Model.TempScriptsDirectory, $"Script_{Guid.NewGuid()}.ldr");

			// Load the script file if it exists
			Filepath = filepath;
			if (Path_.FileExists(Filepath))
				LoadFile(Filepath);
		}
		public ScriptUI(string name, Model model, XElement user_data)
			:this(name, model, user_data.Element(nameof(Filepath)).As<string>())
		{}
		protected override void Dispose(bool disposing)
		{
			Scene = null;
			Editor = null;
			base.Dispose(disposing);
		}
		protected override void OnSavingLayout(DockContainerSavingLayoutEventArgs args)
		{
			args.Node.Add2(nameof(Filepath), Filepath, false);
			base.OnSavingLayout(args);
		}
		protected override bool ProcessKeyPreview(ref Message m)
		{
			switch (Win32.ToVKey(m.WParam))
			{
			case Keys.F5:
				m_btn_render.PerformClick();
				break;
			case Keys.F7:
				Scene?.AutoRange();
				break;
			case Keys.ControlKey | Keys.D:
				m_btn_clear.PerformClick();
				break;
			}
			return base.ProcessKeyPreview(ref m);
		}

		/// <summary>The name of this script</summary>
		public string ScriptName
		{
			get { return m_name; }
			set
			{
				if (m_name == value) return;
				m_name = value;
				DockControl.TabText = m_name;

				// Invalidate anything looking at this script
				Model.Scripts.ResetItem(this, optional:true);
			}
		}
		private string m_name;

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
					m_editor.TextChanged -= HandleScriptChanged;
				}
				m_editor = value;
				if (m_editor != null)
				{
					m_editor.Dock = DockStyle.Fill;
					m_editor.InitLdrStyle();

					m_editor.TextChanged += HandleScriptChanged;
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

		/// <summary>The scene that this script renders to</summary>
		public SceneUI Scene
		{
			get { return m_scene; }
			set
			{
				if (m_scene == value) return;
				if (m_scene != null)
				{
					RemoveScriptObjects();
					m_btn_render.Enabled = false;
					m_btn_clear.Enabled = false;
				}
				m_scene = value;
				if (m_scene != null)
				{
					m_btn_clear.Enabled = true;
					m_btn_render.Enabled = true;
					RenderScript();
				}
			}
		}
		private SceneUI m_scene;

		/// <summary>The filepath for this script</summary>
		public string Filepath
		{
			get { return m_filepath; }
			set
			{
				// Update to the new filepath
				var old = m_filepath;
				m_filepath = value;

				// Update the tab text if it hasn't been changed by the user
				if (DockControl.TabText == DefaultName ||
					DockControl.TabText == Path_.FileTitle(old))
				{
					// Use the file title, unless this is a temporary script
					DockControl.TabText = !IsTempScript ? Path_.FileTitle(m_filepath) : DefaultName;
				}
			}
		}
		private string m_filepath;

		/// <summary>True if this is a temporary script</summary>
		public bool IsTempScript
		{
			get { return Filepath.HasValue() && Path_.IsSubPath(Model.TempScriptsDirectory, Filepath); }
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			#region Tool bar

			// Save file
			m_btn_save.ToolTipText = "Save this script to file";
			m_btn_save.Click += (s,a) =>
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
				RemoveScriptObjects();
			};

			// Binding source for scene selection
			var scenes = new BindingSource<SceneUI>{ DataSource = Model.Scenes };
			scenes.CurrentChanged += (s,a) => Scene = scenes.Current;

			// Render to scene
			m_cb_scene.ToolTipText = "The scene to render to";
			m_cb_scene.ComboBox.DataSource = scenes;
			m_cb_scene.ComboBox.DisplayMember = nameof(SceneUI.SceneName);

			// Initialise to the current scene
			Scene = scenes.Current;

			#endregion
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
			get { return m_save_needed; }
			set
			{
				if (m_save_needed == value) return;
				m_save_needed = value;

				// Add/Remove a '*' from the tab
				DockControl.TabText = DockControl.TabText.TrimEnd('*') + (m_save_needed ? "*" : string.Empty);

				// Enable/Disable buttons
				m_btn_save.Enabled = m_save_needed;
			}
		}
		private bool m_save_needed;

		/// <summary>Remove any objects associated with this script</summary>
		private void RemoveScriptObjects()
		{
			// Remove the objects from the associated scene
			if (Scene != null)
			{
				Scene.Window.RemoveObjects(new [] { ContextId }, 1, 0);
				Scene.Invalidate();
			}

			// Remove any objects previously created by this script
			Model.View3d.DeleteObjects(new [] { ContextId }, 1, 0);
		}

		/// <summary>Render the script in this window</summary>
		private void RenderScript()
		{
			// Save the script first
			if (SaveNeeded)
				SaveFile();

			// Remove any objects from last time we rendered this script
			RemoveScriptObjects();

			// Parse the script, adding objects to the view3d context
			Model.View3d.LoadScript(Editor.Text, false, ContextId, null);

			// Add the script content to the selected scene
			if (Scene != null)
			{
				Scene.Window.AddObjects(new [] { ContextId }, 1, 0);
				if (Model.Settings.ResetOnLoad)
					Scene.AutoRange();
				else
					Scene.Invalidate();
			}
		}

		/// <summary>Handle the script changing</summary>
		private void HandleScriptChanged(object sender, EventArgs e)
		{
			SaveNeeded = true;
		}

		/// <summary>Create a context menu for the tab</summary>
		private ContextMenuStrip CreateTabCMenu()
		{
			var cmenu = new ContextMenuStrip();
			using (cmenu.SuspendLayout(true))
			{
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Rename"));
					opt.Click += (s,a) =>
					{
						using (var dlg = new PromptUI { Title = "Rename", PromptText = "Enter a name for the script", Value = DockControl.TabText })
						{
							dlg.ShowDialog(this);
							ScriptName = (string)dlg.Value;
						}
					};
				}
				cmenu.Items.AddSeparator();
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Close"));
					opt.Click += (s,a) =>
					{
						// When a temporary script is closed, remove the file from disk
						if (IsTempScript)
							File.Delete(Filepath);

						DockControl.DockPane = null;
						Model.Scripts.Remove(this);
					};
				}
			}
			return cmenu;
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScriptUI));
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_btn_save = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_render = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_btn_clear = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.m_cb_scene = new pr.gui.ToolStripComboBox();
			this.m_il_toolbar = new System.Windows.Forms.ImageList(this.components);
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_ts.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(495, 599);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(495, 630);
			this.m_tsc.TabIndex = 0;
			this.m_tsc.Text = "toolStripContainer1";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts);
			// 
			// m_ts
			// 
			this.m_ts.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.m_ts.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_btn_save,
            this.toolStripSeparator1,
            this.m_btn_render,
            this.toolStripSeparator2,
            this.m_btn_clear,
            this.toolStripSeparator3,
            this.m_cb_scene});
			this.m_ts.Location = new System.Drawing.Point(3, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(245, 31);
			this.m_ts.TabIndex = 0;
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
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(6, 31);
			// 
			// m_cb_scene
			// 
			this.m_cb_scene.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_scene.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this.m_cb_scene.Name = "m_cb_scene";
			this.m_cb_scene.SelectedIndex = -1;
			this.m_cb_scene.SelectedItem = null;
			this.m_cb_scene.SelectedText = "";
			this.m_cb_scene.Size = new System.Drawing.Size(100, 28);
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
			this.m_tsc.TopToolStripPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.PerformLayout();
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			this.m_ts.ResumeLayout(false);
			this.m_ts.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
