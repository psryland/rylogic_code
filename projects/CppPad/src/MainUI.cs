using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using pr.common;
using pr.container;
using pr.extn;
using pr.gui;
using pr.util;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace CppPad
{
	public class MainUI :Form
	{
		#region UI Elements
		private DockContainer m_dock;
		private ToolStripContainer m_tsc;
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private ToolStripSeparator m_menu_file_sep0;
		private ToolStripMenuItem m_menu_file_exit;
		private ToolStripMenuItem m_menu_file_recent_files;
		private ToolStripSeparator m_menu_file_sep1;
		private ToolStripMenuItem m_menu_file_new_file;
		private ToolStripSeparator m_menu_file_sep2;
		private ToolStripMenuItem m_menu_file_open_project;
		private ToolStripMenuItem m_menu_file_save;
		private ToolStripMenuItem m_menu_file_saveas;
		private ToolStripMenuItem m_menu_build;
		private ToolStripMenuItem m_menu_build_compile;
		private ToolStripMenuItem m_menu_file_new_project;
		private ToolStripMenuItem m_menu_file_recent_projects;
		private RecentFiles m_recent_projects;
		private ToolStripSeparator m_menu_file_sep3;
		private ToolStripMenuItem m_menu_file_save_all;
		private ToolStripSeparator m_menu_file_sep4;
		private ToolStripMenuItem m_menu_file_close_file;
		private StatusStrip m_ss;
		private ToolStripStatusLabel m_status;
		private ToolStripMenuItem m_menu_file_close_all;
		private ToolStripMenuItem m_menu_file_open_file;
		private RecentFiles m_recent_files;
		#endregion

		/// <summary>The main entry point for the application.</summary>
		[STAThread] static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainUI());
		}
		public MainUI()
		{
			Sci.LoadDll(".\\lib\\$(platform)\\$(config)");
			InitializeComponent();

			Settings = new Settings(Path_.CombinePath(Util.AppDirectory, "settings.xml"));
			Model    = new Model(Settings, this);

			SetupUI();

			if (Path_.DirExists(Settings.LastProject))
				LoadProject(Settings.LastProject);
			else
				NewProject();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
			Model = null;
		}

		/// <summary>App settings</summary>
		public Settings Settings { get; private set; }

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_impl_model; }
			private set
			{
				if (m_impl_model == value) return;
				if (m_impl_model != null) Util.Dispose(ref m_impl_model);
				m_impl_model = value;
			}
		}
		private Model m_impl_model;

		/// <summary>The status label</summary>
		public ToolStripStatusLabel Status
		{
			get { return m_status; }
		}

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
			#region Menu
			m_menu_file.DropDownOpening += (s,a) =>
			{
				m_menu_file_new_file.Enabled   = Path_.DirExists(Model.ProjectDirectory);
				m_menu_file_open_file.Enabled  = Path_.DirExists(Model.ProjectDirectory);
				m_menu_file_save.Enabled       = Model.Editors.Current?.SaveNeeded ?? false;
				m_menu_file_close_file.Enabled = Model.Editors.Current != null;
				m_menu_file_close_all.Enabled  = Model.Editors.Count != 0;
			};
			m_menu_file_new_project.Click += (s,a) =>
			{
				NewProject();
			};
			m_menu_file_new_file.Click += (s,a) =>
			{
				NewFile(null);
			};
			m_menu_file_open_project.Click += (s,a) =>
			{
				LoadProject(null);
			};
			m_menu_file_open_file.Click += (s,a) =>
			{
				LoadFile(null);
			};
			m_menu_file_save.Click += (s,a) =>
			{
				Model.Editors.Current?.Save(string.Empty);
			};
			m_menu_file_saveas.Click += (s,a) =>
			{
				Model.Editors.Current?.Save(null);
			};
			m_menu_file_save_all.Click += (s,a) =>
			{
				Model.SaveAll();
			};
			m_menu_file_close_file.Click += (s,a) =>
			{
				Model.Editors.Current?.Dispose();
			};
			m_menu_file_close_all.Click += (s,a) =>
			{
				Model.CloseAll();
			};
			m_menu_file_exit.Click += (s,a) =>
			{
				Close();
			};
			m_menu_build_compile.Click += (s,a) =>
			{
				BuildAndRun();
			};
			#endregion

			// Recent projects
			m_recent_projects = new RecentFiles(m_menu_file_recent_projects, p => LoadProject(p));
			m_recent_projects.Import(Settings.RecentProjects);
			m_recent_projects.RecentListChanged += (s,a) => Settings.RecentProjects = m_recent_projects.Export();

			// Recent files
			m_recent_files = new RecentFiles(m_menu_file_recent_files, f => LoadFile(f));
			m_recent_files.Import(Settings.RecentFiles);
			m_recent_files.RecentListChanged += (s,a) => Settings.RecentFiles = m_recent_files.Export();

			// Dockable windows
			m_menu.Items.Insert(2, m_dock.WindowsMenu());

			// Add the dockable windows
			m_dock.Add2(Model.BuildOutput, EDockSite.Bottom);
			m_dock.Add2(Model.ProgOutput, EDockSite.Bottom);

			// Customise the dock container
			m_dock.ActiveContentChanged += UpdateUI;
			var main_pane = m_dock.GetPane(EDockSite.Centre);
			main_pane.TitleCtrl.Visible = false;
			main_pane.TabStripCtrl.StripLocation = EDockSite.Top;
			main_pane.TabStripCtrl.TabStripOpts.AlwaysShowTabs = true;

			// Restore the UI layout
			if (Settings.UILayout != null)
			{
				try { m_dock.LoadLayout(Settings.UILayout); }
				catch (Exception ex) { Debug.WriteLine("Failed to restore UI Layout: {0}".Fmt(ex.Message)); }
			}
			else
			{
				var sz = m_dock.GetDockSizes();
				sz.Bottom = (int)(m_dock.Height * 0.2f);
			}
		}

		/// <summary>Create a new folder to contain source files for a new project</summary>
		private void NewProject()
		{
			// Create a new folder in the temp directory as the new project
			var proj_dir = Path.ChangeExtension(Path.GetTempFileName(), string.Empty).TrimEnd('.');

			// Load the project
			LoadProject(proj_dir, prompt_to_create:false);

			// Create a default file in the project directory
			NewFile("main.cpp");
		}

		/// <summary>Create a source file with name 'filename' (auto added to Model.ProjectDirectory)</summary>
		private void NewFile(string filename)
		{
			try
			{
				// A project directory must be set
				if (!Path_.DirExists(Model.ProjectDirectory))
					throw new Exception("No project directory set");

				// If no file name is given, prompt for one
				if (filename == null)
				{
					var dlg = new PromptForm { Title = "New Source File", PromptText =  "Name:", InputType = PromptForm.EInputType.Filename };
					using (dlg)
					{
						if (dlg.ShowDialog(Owner) != DialogResult.OK) return;
						filename = dlg.Value;
					}
				}

				// Create a blank file
				var filepath = Path_.CombinePath(Model.ProjectDirectory, filename);
				using (new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Read)) { }

				// Load it into the project
				LoadFile(filepath);
			}
			catch (Exception ex)
			{
				m_status.SetStatusMessage(msg:"Create file failed: {0}".Fmt(ex.Message), fr_color:Color.Red, display_time:TimeSpan.FromSeconds(5));
			}
		}

		/// <summary>Update a project directory</summary>
		private void LoadProject(string proj_dir, bool prompt_to_create = true)
		{
			try
			{
				// If no project directory is given, prompt for one
				if (proj_dir == null)
				{
					using (var dlg = new OpenFolderUI { Title = "Select a project directory" })
					{
						if (dlg.ShowDialog(Owner) != DialogResult.OK) return;
						proj_dir = dlg.SelectedPath;
					}
				}

				// If the project directory doesn't exist, prompt to create it
				if (!Path_.DirExists(proj_dir))
				{
					if (prompt_to_create)
					{
						var r = MsgBox.Show(Owner, "Create a directory for this project?", "Project Directory doesn't Exist", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
						if (r != DialogResult.OK) return;
					}
					Directory.CreateDirectory(proj_dir);
				}

				// Add to recent
				m_recent_projects.Add(proj_dir);
				Settings.LastProject = proj_dir;

				// Close all currently open files
				Model.CloseAll();

				// Set the new project directory
				Model.ProjectDirectory = proj_dir;

				{// Load main.cpp|main.c from the project directory if it exists
					var main = Path_.CombinePath(Model.ProjectDirectory, "main.cpp");
					if (Path_.FileExists(main)) LoadFile(main);
				} {
					var main = Path_.CombinePath(Model.ProjectDirectory, "main.c");
					if (Path_.FileExists(main)) LoadFile(main);
				}

				// Update the UI
				UpdateUI();
			}
			catch (Exception ex)
			{
				m_status.SetStatusMessage(msg:"Load project failed: {0}".Fmt(ex.Message), fr_color:Color.Red, display_time:TimeSpan.FromSeconds(5));
			}
		}

		/// <summary>Load a file</summary>
		private void LoadFile(string filepath)
		{
			try
			{
				// Prompt for a file if none given
				if (filepath == null)
				{
					using (var dlg = new OpenFileDialog { Title = "Open a Source File", InitialDirectory = Model.ProjectDirectory, Filter = Model.CodeFileFilter })
					{
						if (dlg.ShowDialog(Owner) != DialogResult.OK) return;
						filepath = dlg.FileName;
					}
				}

				// If the file is not within the project directory, make a copy of it
				if (!Path_.IsSubPath(Model.ProjectDirectory, filepath))
				{
					var old = filepath;
					var nue = Path_.CombinePath(Model.ProjectDirectory, Path_.FileName(filepath));
					Path_.ShellCopy(old, nue);
					filepath = nue;
				}

				// Add to recent
				m_recent_files.Add(filepath);

				// Check whether the file is already open
				var existing = Model.Editors.FirstOrDefault(x => Path_.Compare(x.Filepath, filepath) == 0);
				if (existing != null)
				{
					// Make the selected file the active one
					m_dock.ActiveDockable = existing;
				}
				else
				{
					// Add an editor for this file
					var editor = Model.Editors.Add2(new EditorUI(filepath, Model));
					editor.Disposed += (s,a) => Model.Editors.Remove(editor);
					m_dock.Add2(editor, EDockSite.Centre);
				}

				UpdateUI();
			}
			catch (Exception ex)
			{
				m_status.SetStatusMessage(msg:"Load file failed: {0}".Fmt(ex.Message), fr_color:Color.Red, display_time:TimeSpan.FromSeconds(5));
			}
		}

		/// <summary>Build and run the current project</summary>
		private void BuildAndRun()
		{
			try
			{
				Model.BuildAndRun();
			}
			catch (Exception ex)
			{
				// Show exceptions as status messages
				m_status.SetStatusMessage(msg:ex.Message, bold:true, fr_color:Color.Red, display_time:TimeSpan.FromSeconds(5));
			}
		}

		/// <summary>Update the UI</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			if (Model == null)
				return;

			// Update the app title
			Text = "Cpp Pad - {0}".Fmt(Settings.LastProject);

			//// Enable menu items
			//m_menu_file_new_project.Enabled = true;
			//m_menu_file_new_file.Enabled = Path_.DirExists(Model.ProjectDirectory);
			//m_menu_file_open_project.Enabled = true;
			//m_menu_file_save.Enabled = Model.Editors.Current?.SaveNeeded ?? false;
			//m_menu_file_saveas.Enabled = true;
			//m_menu_file_save_all.Enabled = true;
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainUI));
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_ss = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_dock = new pr.gui.DockContainer();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_new_project = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_new_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_sep2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_open_project = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_sep3 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_save = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_saveas = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_save_all = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_sep4 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_close_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_close_all = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_sep0 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_recent_projects = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_recent_files = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_sep1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_build = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_build_compile = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open_file = new System.Windows.Forms.ToolStripMenuItem();
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
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(807, 689);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(807, 735);
			this.m_tsc.TabIndex = 1;
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
			this.m_ss.Size = new System.Drawing.Size(807, 22);
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
			this.m_dock.ActiveContent = null;
			this.m_dock.ActiveDockable = null;
			this.m_dock.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dock.Location = new System.Drawing.Point(0, 0);
			this.m_dock.Name = "m_dock";
			this.m_dock.Size = new System.Drawing.Size(807, 689);
			this.m_dock.TabIndex = 0;
			this.m_dock.Text = "dockContainer1";
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file,
            this.m_menu_build});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(807, 24);
			this.m_menu.TabIndex = 0;
			this.m_menu.Text = "menuStrip1";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_new_project,
            this.m_menu_file_new_file,
            this.m_menu_file_sep2,
            this.m_menu_file_open_project,
            this.m_menu_file_open_file,
            this.m_menu_file_sep3,
            this.m_menu_file_save,
            this.m_menu_file_saveas,
            this.m_menu_file_save_all,
            this.m_menu_file_sep4,
            this.m_menu_file_close_file,
            this.m_menu_file_close_all,
            this.m_menu_file_sep0,
            this.m_menu_file_recent_projects,
            this.m_menu_file_recent_files,
            this.m_menu_file_sep1,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_new_project
			// 
			this.m_menu_file_new_project.Name = "m_menu_file_new_project";
			this.m_menu_file_new_project.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.m_menu_file_new_project.Size = new System.Drawing.Size(199, 22);
			this.m_menu_file_new_project.Text = "&New Project";
			// 
			// m_menu_file_new_file
			// 
			this.m_menu_file_new_file.Name = "m_menu_file_new_file";
			this.m_menu_file_new_file.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.N)));
			this.m_menu_file_new_file.Size = new System.Drawing.Size(199, 22);
			this.m_menu_file_new_file.Text = "&New File";
			// 
			// m_menu_file_sep2
			// 
			this.m_menu_file_sep2.Name = "m_menu_file_sep2";
			this.m_menu_file_sep2.Size = new System.Drawing.Size(196, 6);
			// 
			// m_menu_file_open_project
			// 
			this.m_menu_file_open_project.Name = "m_menu_file_open_project";
			this.m_menu_file_open_project.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.m_menu_file_open_project.Size = new System.Drawing.Size(199, 22);
			this.m_menu_file_open_project.Text = "&Open Project";
			// 
			// m_menu_file_sep3
			// 
			this.m_menu_file_sep3.Name = "m_menu_file_sep3";
			this.m_menu_file_sep3.Size = new System.Drawing.Size(196, 6);
			// 
			// m_menu_file_save
			// 
			this.m_menu_file_save.Name = "m_menu_file_save";
			this.m_menu_file_save.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.m_menu_file_save.Size = new System.Drawing.Size(199, 22);
			this.m_menu_file_save.Text = "&Save";
			// 
			// m_menu_file_saveas
			// 
			this.m_menu_file_saveas.Name = "m_menu_file_saveas";
			this.m_menu_file_saveas.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.m_menu_file_saveas.Size = new System.Drawing.Size(199, 22);
			this.m_menu_file_saveas.Text = "Save &As";
			// 
			// m_menu_file_save_all
			// 
			this.m_menu_file_save_all.Name = "m_menu_file_save_all";
			this.m_menu_file_save_all.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
			this.m_menu_file_save_all.Size = new System.Drawing.Size(199, 22);
			this.m_menu_file_save_all.Text = "Save A&ll";
			// 
			// m_menu_file_sep4
			// 
			this.m_menu_file_sep4.Name = "m_menu_file_sep4";
			this.m_menu_file_sep4.Size = new System.Drawing.Size(196, 6);
			// 
			// m_menu_file_close
			// 
			this.m_menu_file_close_file.Name = "m_menu_file_close";
			this.m_menu_file_close_file.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.m_menu_file_close_file.Size = new System.Drawing.Size(199, 22);
			this.m_menu_file_close_file.Text = "&Close";
			// 
			// m_menu_file_close_all
			// 
			this.m_menu_file_close_all.Name = "m_menu_file_close_all";
			this.m_menu_file_close_all.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.W)));
			this.m_menu_file_close_all.Size = new System.Drawing.Size(199, 22);
			this.m_menu_file_close_all.Text = "Close All";
			// 
			// m_menu_file_sep0
			// 
			this.m_menu_file_sep0.Name = "m_menu_file_sep0";
			this.m_menu_file_sep0.Size = new System.Drawing.Size(196, 6);
			// 
			// m_menu_file_recent_projects
			// 
			this.m_menu_file_recent_projects.Name = "m_menu_file_recent_projects";
			this.m_menu_file_recent_projects.Size = new System.Drawing.Size(199, 22);
			this.m_menu_file_recent_projects.Text = "&Recent Projects";
			// 
			// m_menu_file_recent_files
			// 
			this.m_menu_file_recent_files.Name = "m_menu_file_recent_files";
			this.m_menu_file_recent_files.Size = new System.Drawing.Size(199, 22);
			this.m_menu_file_recent_files.Text = "&Recent Files";
			// 
			// m_menu_file_sep1
			// 
			this.m_menu_file_sep1.Name = "m_menu_file_sep1";
			this.m_menu_file_sep1.Size = new System.Drawing.Size(196, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(199, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// m_menu_build
			// 
			this.m_menu_build.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_build_compile});
			this.m_menu_build.Name = "m_menu_build";
			this.m_menu_build.Size = new System.Drawing.Size(46, 20);
			this.m_menu_build.Text = "&Build";
			// 
			// m_menu_build_compile
			// 
			this.m_menu_build_compile.Name = "m_menu_build_compile";
			this.m_menu_build_compile.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.m_menu_build_compile.Size = new System.Drawing.Size(138, 22);
			this.m_menu_build_compile.Text = "&Compile";
			// 
			// m_menu_file_open_file
			// 
			this.m_menu_file_open_file.Name = "m_menu_file_open_file";
			this.m_menu_file_open_file.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.O)));
			this.m_menu_file_open_file.Size = new System.Drawing.Size(199, 22);
			this.m_menu_file_open_file.Text = "&Open File";
			// 
			// MainUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(807, 735);
			this.Controls.Add(this.m_tsc);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.m_menu;
			this.Name = "MainUI";
			this.Text = "Cpp Pad";
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
