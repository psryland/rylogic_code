//*************************************************************************************************
// LDraw
// Copyright (c) Rylogic Ltd 2016
//*************************************************************************************************
//#define TRAP_UNHANDLED_EXCEPTIONS
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.util;
using pr.win32;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace LDraw
{
	public class MainUI :Form
	{
		#region UI Elements
		private ToolStripContainer m_tsc;
		private StatusStrip m_ss;
		private ToolStripStatusLabel m_status;
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private ToolStripSeparator m_sep0;
		private DockContainer m_dc;
		private ToolStripMenuItem m_menu_file_open;
		private ToolStripMenuItem m_menu_file_open_additional;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripMenuItem m_menu_file_recent_files;
		private ToolStripMenuItem m_menu_file_exit;
		private ToolStripMenuItem m_menu_data;
		private ToolStripMenuItem m_menu_data_create_demo_scene;
		private ToolStripMenuItem m_menu_data_clear_scene;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripMenuItem m_menu_nav;
		private ToolStripMenuItem m_menu_nav_reset_view;
		private ToolStripMenuItem m_menu_nav_view;
		private ToolStripMenuItem m_menu_nav_set_focus_position;
		private ToolStripMenuItem m_menu_nav_set_camera_position;
		private ToolStripSeparator toolStripSeparator4;
		private ToolStripMenuItem m_menu_nav_align;
		private ToolStripSeparator toolStripSeparator5;
		private ToolStripMenuItem m_menu_nav_save_view;
		private ToolStripMenuItem m_menu_nav_saved_views;
		private ToolStripMenuItem m_menu_data_auto_refresh;
		private ToolStripSeparator toolStripSeparator3;
		private ToolStripMenuItem m_menu_data_object_manager;
		private ToolStripMenuItem m_menu_window;
		private ToolStripMenuItem m_menu_file_new;
		private ToolStripSeparator toolStripSeparator8;
		private ToolStripMenuItem m_menu_nav_reset_view_all;
		private ToolStripMenuItem m_menu_nav_reset_view_selected;
		private ToolStripMenuItem m_menu_nav_reset_view_visible;
		private ToolStripMenuItem m_menu_nav_view_xpos;
		private ToolStripMenuItem m_menu_nav_view_xneg;
		private ToolStripMenuItem m_menu_nav_view_ypos;
		private ToolStripMenuItem m_menu_nav_view_yneg;
		private ToolStripMenuItem m_menu_nav_view_zpos;
		private ToolStripMenuItem m_menu_nav_view_zneg;
		private ToolStripMenuItem m_menu_nav_view_xyz;
		private ToolStripMenuItem m_menu_nav_align_none;
		private ToolStripMenuItem m_menu_nav_align_x;
		private ToolStripMenuItem m_menu_nav_align_y;
		private ToolStripMenuItem m_menu_nav_align_z;
		private ToolStripMenuItem m_menu_nav_align_current;
		private ToolStripSeparator toolStripSeparator11;
		private ToolStripMenuItem m_menu_nav_saved_views_clear;
		private ToolStripSeparator toolStripSeparator12;
		private ToolStripMenuItem m_menu_rendering;
		private ToolStripMenuItem m_menu_rendering_show_focus;
		private ToolStripMenuItem m_menu_rendering_show_origin;
		private ToolStripMenuItem m_menu_rendering_show_selection;
		private ToolStripMenuItem m_menu_rendering_show_bounds;
		private ToolStripSeparator toolStripSeparator9;
		private ToolStripMenuItem m_menu_rendering_wireframe;
		private ToolStripMenuItem m_menu_rendering_orthographic;
		private ToolStripSeparator toolStripSeparator10;
		private ToolStripMenuItem m_menu_rendering_lighting;
		private ToolStripMenuItem m_menu_window_always_on_top;
		private ToolStripSeparator toolStripSeparator7;
		private ToolStripMenuItem m_menu_window_example_script;
		private ToolStripSeparator toolStripSeparator6;
		private ToolStripMenuItem m_menu_window_about;
		#endregion

		/// <summary>Recent files history</summary>
		private RecentFiles m_recent_files;

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
				var crash_dump_file = Util.ResolveAppDataPath("Rylogic", "LDraw", "crash_report.txt");
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

			Settings = new Settings(Util.ResolveUserDocumentsPath("Rylogic", "LDraw", "settings.xml"));
			View3d = new View3d();
			Model = new Model(this);

			SetupUI();
			UpdateUI();

			RestoreWindowPosition();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_dc);
			Util.Dispose(ref components);
			base.Dispose(disposing);
			Model = null;
			View3d = null;
		}
		protected override void OnInvalidated(InvalidateEventArgs e)
		{
			Scene.Invalidate();
			base.OnInvalidated(e);
		}
		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			Settings.UI.WindowPosition = Bounds;
			Settings.UI.UILayout = m_dc.SaveLayout();
			Settings.Save();
			base.OnFormClosed(e);
		}
		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (m.Msg == Win32.WM_SYSCOMMAND)
			{
				var wp = (uint)m.WParam & 0xFFF0;
				if (wp == Win32.SC_MAXIMIZE || wp == Win32.SC_RESTORE || wp == Win32.SC_MINIMIZE)
					Settings.UI.WindowMaximised = WindowState == FormWindowState.Maximized;
			}
		}

		/// <summary>App settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The app logic</summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return m_impl_model; }
			private set
			{
				if (m_impl_model == value) return;
				if (m_impl_model != null)
				{
					Util.Dispose(ref m_impl_model);
				}
				m_impl_model = value;
				if (m_impl_model != null)
				{
				}
				Update();
			}
		}
		private Model m_impl_model;

		/// <summary>A view3d context reference that lives for the lifetime of the application</summary>
		public View3d View3d
		{
			[DebuggerStepThrough] get { return m_view3d; }
			set
			{
				if (m_view3d == value) return;
				Util.Dispose(ref m_view3d);
				m_view3d = value;
			}
		}
		private View3d m_view3d;

		/// <summary>The 3d scene</summary>
		public ChartControl Scene
		{
			get;
			private set;
		}

		/// <summary>Main application status label</summary>
		public ToolStripStatusLabel Status
		{
			get { return m_status; }
		}

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
			#region Menu
			// File Menu
			m_menu_file_new.Click += (s,a) =>
			{
				NewFile(null);
			};
			m_menu_file_open.Click += (s,a) =>
			{
				OpenFile(null, false);
			};
			m_menu_file_open_additional.Click += (s,a) =>
			{
				OpenFile(null, true);
			};
			m_menu_file_exit.Click += (s,a) =>
			{
				Close();
			};
			m_recent_files = new RecentFiles(m_menu_file_recent_files, HandleRecentFile);
			m_recent_files.Import(Settings.RecentFiles);
			m_recent_files.RecentListChanged += (s,a) => Settings.RecentFiles = m_recent_files.Export();

			// Navigation Menu

			// Data Menu
			m_menu_data_clear_scene.Click+= (s,a) =>
			{
				Model.ClearScene();
				Scene.Invalidate();
			};
			m_menu_data_create_demo_scene.Click += (s,a) =>
			{
				Model.CreateDemoScene();
				Scene.Invalidate();
			};

			// Rendering Menu

			// Window menu
			#endregion

			#region Scene
			Scene = new ChartControl(string.Empty, Settings.Scene);
			Scene.Name                   = "Scene";
			Scene.BorderStyle            = BorderStyle.FixedSingle;
			Scene.Dock                   = DockStyle.Fill;
			Scene.DefaultMouseControl    = true;
			Scene.Options.LockAspect     = 1.0f;
			Scene.Options.NavigationMode = ChartControl.ENavMode.Scene3D;
			Model.Window = Scene.Chart.Window;
			#endregion

			#region Dock Container

			m_menu_window.DropDownItems.Insert(0, m_dc.WindowsMenu());
			m_dc.Options.TabStrip.AlwaysShowTabs = true;
			m_dc.Options.TitleBar.ShowTitleBars = false;
			var dc_scene = m_dc.Add2(new Dockable(Scene, "Scene"));
			
			#endregion
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
		}

		/// <summary>Create a new file and an editor to edit the file</summary>
		private void NewFile(string filepath)
		{
			m_dc.Add2(new ScriptUI(Model));

			// Create a new script file
			Model.NewFile(filepath);
		}

		/// <summary>Add a file source</summary>
		private void OpenFile(string filepath, bool additional)
		{
			if (!filepath.HasValue())
			{
				using (var dlg = new OpenFileDialog { Title = "Open Ldr Script file"})
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					filepath = dlg.FileName;
				}
			}

			// Add the file to the recent file history
			m_recent_files.Add(filepath);

			// Load the file source
			Model.OpenFile(filepath, additional);
		}

		/// <summary>Restore the last window position</summary>
		private void RestoreWindowPosition()
		{
			if (Settings.UI.WindowPosition.IsEmpty)
			{
				StartPosition = FormStartPosition.WindowsDefaultLocation;
			}
			else
			{
				StartPosition = FormStartPosition.Manual;
				Bounds = Util.OnScreen(Settings.UI.WindowPosition);
				WindowState = Settings.UI.WindowMaximised ? FormWindowState.Maximized : FormWindowState.Normal;
			}
		}

		/// <summary>Recent file selected</summary>
		private void HandleRecentFile(string filepath)
		{
			OpenFile(filepath, (ModifierKeys & Keys.Shift) != 0);
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
			this.m_menu_file_open = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open_additional = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_recent_files = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep0 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_data = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_data_clear_scene = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_data_create_demo_scene = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_data_auto_refresh = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_data_object_manager = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_nav = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_reset_view = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_set_focus_position = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_set_camera_position = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_nav_align = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_nav_save_view = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_saved_views = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_window = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_window_always_on_top = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_window_about = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_window_example_script = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_new = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_rendering = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_show_focus = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_show_origin = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_show_selection = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_show_bounds = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_rendering_wireframe = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_rendering_orthographic = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_lighting = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_align_x = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_align_y = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_align_z = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_align_current = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_align_none = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_xpos = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_xneg = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_ypos = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_yneg = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_zpos = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_zneg = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_xyz = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_reset_view_all = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_reset_view_selected = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_reset_view_visible = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_saved_views_clear = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
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
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(663, 543);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(663, 589);
			this.m_tsc.TabIndex = 0;
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
			this.m_ss.Size = new System.Drawing.Size(663, 22);
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
			this.m_dc.Size = new System.Drawing.Size(663, 543);
			this.m_dc.TabIndex = 0;
			this.m_dc.Text = "pr.gui.DockContainer";
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file,
            this.m_menu_nav,
            this.m_menu_data,
            this.m_menu_rendering,
            this.m_menu_window});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(663, 24);
			this.m_menu.TabIndex = 0;
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_new,
            this.toolStripSeparator8,
            this.m_menu_file_open,
            this.m_menu_file_open_additional,
            this.toolStripSeparator1,
            this.m_menu_file_recent_files,
            this.m_sep0,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_open
			// 
			this.m_menu_file_open.Name = "m_menu_file_open";
			this.m_menu_file_open.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.m_menu_file_open.Size = new System.Drawing.Size(236, 22);
			this.m_menu_file_open.Text = "&Open";
			// 
			// m_menu_file_open_additional
			// 
			this.m_menu_file_open_additional.Name = "m_menu_file_open_additional";
			this.m_menu_file_open_additional.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.O)));
			this.m_menu_file_open_additional.Size = new System.Drawing.Size(236, 22);
			this.m_menu_file_open_additional.Text = "Open &Additional";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(233, 6);
			// 
			// m_menu_file_recent_files
			// 
			this.m_menu_file_recent_files.Name = "m_menu_file_recent_files";
			this.m_menu_file_recent_files.Size = new System.Drawing.Size(236, 22);
			this.m_menu_file_recent_files.Text = "&Recent Files";
			// 
			// m_sep0
			// 
			this.m_sep0.Name = "m_sep0";
			this.m_sep0.Size = new System.Drawing.Size(233, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(236, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// m_menu_data
			// 
			this.m_menu_data.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_data_clear_scene,
            this.m_menu_data_auto_refresh,
            this.toolStripSeparator2,
            this.m_menu_data_create_demo_scene,
            this.toolStripSeparator3,
            this.m_menu_data_object_manager});
			this.m_menu_data.Name = "m_menu_data";
			this.m_menu_data.Size = new System.Drawing.Size(43, 20);
			this.m_menu_data.Text = "&Data";
			// 
			// m_menu_data_clear_scene
			// 
			this.m_menu_data_clear_scene.Name = "m_menu_data_clear_scene";
			this.m_menu_data_clear_scene.Size = new System.Drawing.Size(177, 22);
			this.m_menu_data_clear_scene.Text = "&Clear Scene";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(174, 6);
			// 
			// m_menu_data_create_demo_scene
			// 
			this.m_menu_data_create_demo_scene.Name = "m_menu_data_create_demo_scene";
			this.m_menu_data_create_demo_scene.Size = new System.Drawing.Size(177, 22);
			this.m_menu_data_create_demo_scene.Text = "&Create Demo Scene";
			// 
			// m_menu_data_auto_refresh
			// 
			this.m_menu_data_auto_refresh.Name = "m_menu_data_auto_refresh";
			this.m_menu_data_auto_refresh.Size = new System.Drawing.Size(177, 22);
			this.m_menu_data_auto_refresh.Text = "&Auto Refresh";
			// 
			// m_menu_data_object_manager
			// 
			this.m_menu_data_object_manager.Name = "m_menu_data_object_manager";
			this.m_menu_data_object_manager.Size = new System.Drawing.Size(177, 22);
			this.m_menu_data_object_manager.Text = "&Object Manager";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(174, 6);
			// 
			// m_menu_nav
			// 
			this.m_menu_nav.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_nav_reset_view,
            this.m_menu_nav_view,
            this.toolStripSeparator4,
            this.m_menu_nav_align,
            this.toolStripSeparator5,
            this.m_menu_nav_save_view,
            this.m_menu_nav_saved_views,
            this.toolStripSeparator12,
            this.m_menu_nav_set_camera_position,
            this.m_menu_nav_set_focus_position});
			this.m_menu_nav.Name = "m_menu_nav";
			this.m_menu_nav.Size = new System.Drawing.Size(77, 20);
			this.m_menu_nav.Text = "&Navigation";
			// 
			// m_menu_nav_reset_view
			// 
			this.m_menu_nav_reset_view.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_nav_reset_view_all,
            this.m_menu_nav_reset_view_selected,
            this.m_menu_nav_reset_view_visible});
			this.m_menu_nav_reset_view.Name = "m_menu_nav_reset_view";
			this.m_menu_nav_reset_view.Size = new System.Drawing.Size(222, 22);
			this.m_menu_nav_reset_view.Text = "&Reset View";
			// 
			// m_menu_nav_view
			// 
			this.m_menu_nav_view.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_nav_view_xpos,
            this.m_menu_nav_view_xneg,
            this.m_menu_nav_view_ypos,
            this.m_menu_nav_view_yneg,
            this.m_menu_nav_view_zpos,
            this.m_menu_nav_view_zneg,
            this.m_menu_nav_view_xyz});
			this.m_menu_nav_view.Name = "m_menu_nav_view";
			this.m_menu_nav_view.Size = new System.Drawing.Size(222, 22);
			this.m_menu_nav_view.Text = "&View";
			// 
			// m_menu_nav_set_focus_position
			// 
			this.m_menu_nav_set_focus_position.Name = "m_menu_nav_set_focus_position";
			this.m_menu_nav_set_focus_position.Size = new System.Drawing.Size(222, 22);
			this.m_menu_nav_set_focus_position.Text = "Set &Focus Position";
			// 
			// m_menu_nav_set_camera_position
			// 
			this.m_menu_nav_set_camera_position.Name = "m_menu_nav_set_camera_position";
			this.m_menu_nav_set_camera_position.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.m_menu_nav_set_camera_position.Size = new System.Drawing.Size(222, 22);
			this.m_menu_nav_set_camera_position.Text = "Set Camera &Position";
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(219, 6);
			// 
			// m_menu_nav_align
			// 
			this.m_menu_nav_align.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_nav_align_none,
            this.m_menu_nav_align_x,
            this.m_menu_nav_align_y,
            this.m_menu_nav_align_z,
            this.m_menu_nav_align_current});
			this.m_menu_nav_align.Name = "m_menu_nav_align";
			this.m_menu_nav_align.Size = new System.Drawing.Size(222, 22);
			this.m_menu_nav_align.Text = "&Align";
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(219, 6);
			// 
			// m_menu_nav_save_view
			// 
			this.m_menu_nav_save_view.Name = "m_menu_nav_save_view";
			this.m_menu_nav_save_view.Size = new System.Drawing.Size(222, 22);
			this.m_menu_nav_save_view.Text = "&Save View";
			// 
			// m_menu_nav_saved_views
			// 
			this.m_menu_nav_saved_views.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator11,
            this.m_menu_nav_saved_views_clear});
			this.m_menu_nav_saved_views.Name = "m_menu_nav_saved_views";
			this.m_menu_nav_saved_views.Size = new System.Drawing.Size(222, 22);
			this.m_menu_nav_saved_views.Text = "Save&d Views";
			// 
			// m_menu_window
			// 
			this.m_menu_window.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_window_always_on_top,
            this.toolStripSeparator7,
            this.m_menu_window_example_script,
            this.toolStripSeparator6,
            this.m_menu_window_about});
			this.m_menu_window.Name = "m_menu_window";
			this.m_menu_window.Size = new System.Drawing.Size(63, 20);
			this.m_menu_window.Text = "&Window";
			// 
			// m_menu_window_always_on_top
			// 
			this.m_menu_window_always_on_top.Name = "m_menu_window_always_on_top";
			this.m_menu_window_always_on_top.Size = new System.Drawing.Size(152, 22);
			this.m_menu_window_always_on_top.Text = "Always on &Top";
			// 
			// m_menu_window_about
			// 
			this.m_menu_window_about.Name = "m_menu_window_about";
			this.m_menu_window_about.Size = new System.Drawing.Size(152, 22);
			this.m_menu_window_about.Text = "&About";
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(149, 6);
			// 
			// m_menu_window_example_script
			// 
			this.m_menu_window_example_script.Name = "m_menu_window_example_script";
			this.m_menu_window_example_script.Size = new System.Drawing.Size(152, 22);
			this.m_menu_window_example_script.Text = "&Example Script";
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(149, 6);
			// 
			// m_menu_file_new
			// 
			this.m_menu_file_new.Name = "m_menu_file_new";
			this.m_menu_file_new.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.m_menu_file_new.Size = new System.Drawing.Size(236, 22);
			this.m_menu_file_new.Text = "&New";
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(233, 6);
			// 
			// m_menu_rendering
			// 
			this.m_menu_rendering.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_rendering_show_focus,
            this.m_menu_rendering_show_origin,
            this.m_menu_rendering_show_selection,
            this.m_menu_rendering_show_bounds,
            this.toolStripSeparator9,
            this.m_menu_rendering_wireframe,
            this.m_menu_rendering_orthographic,
            this.toolStripSeparator10,
            this.m_menu_rendering_lighting});
			this.m_menu_rendering.Name = "m_menu_rendering";
			this.m_menu_rendering.Size = new System.Drawing.Size(73, 20);
			this.m_menu_rendering.Text = "&Rendering";
			// 
			// m_menu_rendering_show_focus
			// 
			this.m_menu_rendering_show_focus.Name = "m_menu_rendering_show_focus";
			this.m_menu_rendering_show_focus.Size = new System.Drawing.Size(158, 22);
			this.m_menu_rendering_show_focus.Text = "Show &Focus";
			// 
			// m_menu_rendering_show_origin
			// 
			this.m_menu_rendering_show_origin.Name = "m_menu_rendering_show_origin";
			this.m_menu_rendering_show_origin.Size = new System.Drawing.Size(158, 22);
			this.m_menu_rendering_show_origin.Text = "Show &Origin";
			// 
			// m_menu_rendering_show_selection
			// 
			this.m_menu_rendering_show_selection.Name = "m_menu_rendering_show_selection";
			this.m_menu_rendering_show_selection.Size = new System.Drawing.Size(158, 22);
			this.m_menu_rendering_show_selection.Text = "Show &Selection";
			// 
			// m_menu_rendering_show_bounds
			// 
			this.m_menu_rendering_show_bounds.Name = "m_menu_rendering_show_bounds";
			this.m_menu_rendering_show_bounds.Size = new System.Drawing.Size(158, 22);
			this.m_menu_rendering_show_bounds.Text = "Show &Bounds";
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(155, 6);
			// 
			// m_menu_rendering_wireframe
			// 
			this.m_menu_rendering_wireframe.Name = "m_menu_rendering_wireframe";
			this.m_menu_rendering_wireframe.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.m_menu_rendering_wireframe.Size = new System.Drawing.Size(158, 22);
			this.m_menu_rendering_wireframe.Text = "Sol&id";
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			this.toolStripSeparator10.Size = new System.Drawing.Size(155, 6);
			// 
			// m_menu_rendering_orthographic
			// 
			this.m_menu_rendering_orthographic.Name = "m_menu_rendering_orthographic";
			this.m_menu_rendering_orthographic.Size = new System.Drawing.Size(158, 22);
			this.m_menu_rendering_orthographic.Text = "O&rthographic";
			// 
			// m_menu_rendering_lighting
			// 
			this.m_menu_rendering_lighting.Name = "m_menu_rendering_lighting";
			this.m_menu_rendering_lighting.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
			this.m_menu_rendering_lighting.Size = new System.Drawing.Size(158, 22);
			this.m_menu_rendering_lighting.Text = "Lighting";
			// 
			// m_menu_nav_align_x
			// 
			this.m_menu_nav_align_x.Name = "m_menu_nav_align_x";
			this.m_menu_nav_align_x.Size = new System.Drawing.Size(152, 22);
			this.m_menu_nav_align_x.Text = "&X";
			// 
			// m_menu_nav_align_y
			// 
			this.m_menu_nav_align_y.Name = "m_menu_nav_align_y";
			this.m_menu_nav_align_y.Size = new System.Drawing.Size(152, 22);
			this.m_menu_nav_align_y.Text = "&Y";
			// 
			// m_menu_nav_align_z
			// 
			this.m_menu_nav_align_z.Name = "m_menu_nav_align_z";
			this.m_menu_nav_align_z.Size = new System.Drawing.Size(152, 22);
			this.m_menu_nav_align_z.Text = "&Z";
			// 
			// m_menu_nav_align_current
			// 
			this.m_menu_nav_align_current.Name = "m_menu_nav_align_current";
			this.m_menu_nav_align_current.Size = new System.Drawing.Size(152, 22);
			this.m_menu_nav_align_current.Text = "&Current";
			// 
			// m_menu_nav_align_none
			// 
			this.m_menu_nav_align_none.Name = "m_menu_nav_align_none";
			this.m_menu_nav_align_none.Size = new System.Drawing.Size(152, 22);
			this.m_menu_nav_align_none.Text = "&None";
			// 
			// m_menu_nav_view_xpos
			// 
			this.m_menu_nav_view_xpos.Name = "m_menu_nav_view_xpos";
			this.m_menu_nav_view_xpos.Size = new System.Drawing.Size(211, 22);
			this.m_menu_nav_view_xpos.Text = "Axis +X (&Right Side)";
			// 
			// m_menu_nav_view_xneg
			// 
			this.m_menu_nav_view_xneg.Name = "m_menu_nav_view_xneg";
			this.m_menu_nav_view_xneg.Size = new System.Drawing.Size(211, 22);
			this.m_menu_nav_view_xneg.Text = "Axis -X (&Left Side)";
			// 
			// m_menu_nav_view_ypos
			// 
			this.m_menu_nav_view_ypos.Name = "m_menu_nav_view_ypos";
			this.m_menu_nav_view_ypos.Size = new System.Drawing.Size(211, 22);
			this.m_menu_nav_view_ypos.Text = "Axis +Y (&Top)";
			// 
			// m_menu_nav_view_yneg
			// 
			this.m_menu_nav_view_yneg.Name = "m_menu_nav_view_yneg";
			this.m_menu_nav_view_yneg.Size = new System.Drawing.Size(211, 22);
			this.m_menu_nav_view_yneg.Text = "Axis -Y (&Bottom)";
			// 
			// m_menu_nav_view_zpos
			// 
			this.m_menu_nav_view_zpos.Name = "m_menu_nav_view_zpos";
			this.m_menu_nav_view_zpos.Size = new System.Drawing.Size(211, 22);
			this.m_menu_nav_view_zpos.Text = "Axis +Z (&Front)";
			// 
			// m_menu_nav_view_zneg
			// 
			this.m_menu_nav_view_zneg.Name = "m_menu_nav_view_zneg";
			this.m_menu_nav_view_zneg.Size = new System.Drawing.Size(211, 22);
			this.m_menu_nav_view_zneg.Text = "Axis -Z (Bac&k)";
			// 
			// m_menu_nav_view_xyz
			// 
			this.m_menu_nav_view_xyz.Name = "m_menu_nav_view_xyz";
			this.m_menu_nav_view_xyz.Size = new System.Drawing.Size(211, 22);
			this.m_menu_nav_view_xyz.Text = "Axis -X -Y -Z (&Perspective)";
			// 
			// m_menu_nav_reset_view_all
			// 
			this.m_menu_nav_reset_view_all.Name = "m_menu_nav_reset_view_all";
			this.m_menu_nav_reset_view_all.Size = new System.Drawing.Size(152, 22);
			this.m_menu_nav_reset_view_all.Text = "&All";
			// 
			// m_menu_nav_reset_view_selected
			// 
			this.m_menu_nav_reset_view_selected.Name = "m_menu_nav_reset_view_selected";
			this.m_menu_nav_reset_view_selected.Size = new System.Drawing.Size(152, 22);
			this.m_menu_nav_reset_view_selected.Text = "&Selected";
			// 
			// m_menu_nav_reset_view_visible
			// 
			this.m_menu_nav_reset_view_visible.Name = "m_menu_nav_reset_view_visible";
			this.m_menu_nav_reset_view_visible.Size = new System.Drawing.Size(152, 22);
			this.m_menu_nav_reset_view_visible.Text = "&Visible";
			// 
			// m_menu_nav_saved_views_clear
			// 
			this.m_menu_nav_saved_views_clear.Name = "m_menu_nav_saved_views_clear";
			this.m_menu_nav_saved_views_clear.Size = new System.Drawing.Size(168, 22);
			this.m_menu_nav_saved_views_clear.Text = "&Clear Saved Views";
			// 
			// toolStripSeparator11
			// 
			this.toolStripSeparator11.Name = "toolStripSeparator11";
			this.toolStripSeparator11.Size = new System.Drawing.Size(165, 6);
			// 
			// toolStripSeparator12
			// 
			this.toolStripSeparator12.Name = "toolStripSeparator12";
			this.toolStripSeparator12.Size = new System.Drawing.Size(219, 6);
			// 
			// MainUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(663, 589);
			this.Controls.Add(this.m_tsc);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.m_menu;
			this.Name = "MainUI";
			this.Text = "LDraw";
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
