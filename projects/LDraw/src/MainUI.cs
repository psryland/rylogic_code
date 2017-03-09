//*************************************************************************************************
// LDraw
// Copyright (c) Rylogic Ltd 2016
//*************************************************************************************************
//#define TRAP_UNHANDLED_EXCEPTIONS
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.scintilla;
using pr.util;
using pr.win32;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace LDraw
{
	public class MainUI :Form
	{
		/// <summary>Recent files history</summary>
		private RecentFiles m_recent_files;

		#region UI Elements
		private ToolStripContainer m_tsc;
		private StatusStrip m_ss;
		private ToolStripStatusLabel m_status;
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private ToolStripSeparator m_sep0;
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
		private ToolStripMenuItem m_menu_nav_camera;
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
		private ToolStripSeparator m_sep_saved_views;
		private ToolStripMenuItem m_menu_nav_saved_views_clear;
		private ToolStripSeparator toolStripSeparator12;
		private ToolStripMenuItem m_menu_rendering;
		private ToolStripMenuItem m_menu_rendering_show_focus;
		private ToolStripMenuItem m_menu_rendering_show_origin;
		private ToolStripMenuItem m_menu_rendering_show_selection;
		private ToolStripMenuItem m_menu_rendering_show_bounds;
		private ToolStripSeparator toolStripSeparator9;
		private ToolStripMenuItem m_menu_rendering_orthographic;
		private ToolStripSeparator toolStripSeparator10;
		private ToolStripMenuItem m_menu_rendering_lighting;
		private ToolStripMenuItem m_menu_window_always_on_top;
		private ToolStripSeparator toolStripSeparator7;
		private ToolStripMenuItem m_menu_window_example_script;
		private ToolStripSeparator toolStripSeparator6;
		private ToolStripMenuItem m_menu_window_about;
		private ToolStripMenuItem m_menu_file_save;
		private ToolStripMenuItem m_menu_rendering_fillmode;
		private ToolStripMenuItem m_menu_rendering_fillmode_solid;
		private ToolStripMenuItem m_menu_rendering_fillmode_wireframe;
		private ToolStripMenuItem m_menu_rendering_fillmode_solidwire;
		private ToolStripMenuItem m_menu_rendering_cullmode;
		private ToolStripMenuItem m_menu_rendering_cullmode_none;
		private ToolStripMenuItem m_menu_rendering_cullmode_back;
		private ToolStripMenuItem m_menu_rendering_cullmode_front;
		private ToolStripMenuItem m_menu_file_edit_script;
		private ToolStripMenuItem m_menu_nav_zoom;
		private ToolStripMenuItem m_menu_nav_zoom_default;
		private ToolStripMenuItem m_menu_nav_zoom_aspect11;
		private ToolStripMenuItem m_menu_nav_zoom_lock_aspect;
		private ToolStripSeparator toolStripSeparator11;
		private ToolStripMenuItem m_menu_nav_reset_on_reload;
		private ToolStripSeparator toolStripSeparator13;
		private ToolStripSeparator toolStripSeparator14;
		private ToolStripMenuItem m_menu_file_options;
		private ToolStripStatusLabel m_lbl_loading;
		private ToolStripProgressBar m_pb_loading;
		private ToolStripMenuItem m_menu_file_save_as;
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
			KeyPreview = true;

			Settings = new Settings(Util.ResolveUserDocumentsPath("Rylogic", "LDraw", "settings.xml"));
			Model = new Model(this);
			DockContainer = new DockContainer();

			SetupUI();
			UpdateUI();

			RestoreWindowPosition();
		}
		protected override void Dispose(bool disposing)
		{
			// The scripts and scene need to be disposed before view3d.
			// The Scene owns the view3d Window and the Model owns the Scene.
			CameraUI = null;
			LightingUI = null;
			DockContainer = null;
			Model = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnInvalidated(InvalidateEventArgs e)
		{
			Model.Scene.Invalidate();
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
		protected override void OnKeyDown(KeyEventArgs e)
		{
			// Cycle through fill modes
			if (e.KeyCode == Keys.W && e.Control)
			{
				Model.Scene.Options.FillMode = Enum<View3d.EFillMode>.Cycle(Model.Scene.Options.FillMode);
				Invalidate();
				e.Handled = true;
			}
			base.OnKeyDown(e);
		}

		/// <summary>App settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get { return m_settings; }
			private set
			{
				if (m_settings == value) return;
				if (m_settings != null)
				{ }
				m_settings = value;
				if (m_settings != null)
				{ }
			}
		}
		private Settings m_settings;

		/// <summary>The app logic</summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return m_impl_model; }
			private set
			{
				if (m_impl_model == value) return;
				if (m_impl_model != null)
				{
					m_impl_model.Scene.Options.PropertyChanged -= UpdateUI;
					Util.Dispose(ref m_impl_model);
				}
				m_impl_model = value;
				if (m_impl_model != null)
				{
					m_impl_model.Scene.Options.PropertyChanged += UpdateUI;
				}
				Update();
			}
		}
		private Model m_impl_model;

		/// <summary>Main application status label</summary>
		public ToolStripStatusLabel Status
		{
			get { return m_status; }
		}

		/// <summary>Lazy created camera properties UI</summary>
		public CameraUI CameraUI
		{
			get { return m_camera_ui ?? (m_camera_ui = new CameraUI(this)); }
			set { Util.Dispose(ref m_camera_ui); }
		}
		private CameraUI m_camera_ui;

		/// <summary>Lazy creating lighting UI</summary>
		public LightingUI LightingUI
		{
			get { return m_lighting_ui ?? (m_lighting_ui = new LightingUI(this)); }
			set { Util.Dispose(ref m_lighting_ui); }
		}
		private LightingUI m_lighting_ui;

		/// <summary>The dock container control</summary>
		private DockContainer DockContainer
		{
			get { return m_dc; }
			set
			{
				if (m_dc == value) return;
				if (m_dc != null)
				{
					// Remove the scene from the dock container before disposing it
					// so that the scene is left for the model to dispose.
					m_dc.ActiveContentChanged -= UpdateUI;
					m_tsc.ContentPanel.Controls.Remove(m_dc);
					m_dc.Remove(Model.Scene);
					m_dc.Remove(Model.Log);
					Util.Dispose(ref m_dc);
				}
				m_dc = value;
				if (m_dc != null)
				{
					m_dc.Dock = DockStyle.Fill;
					m_dc.Options.TabStrip.AlwaysShowTabs = true;
					m_dc.Options.TitleBar.ShowTitleBars = true;
					m_dc.Add(Model.Scene);
					m_dc.Add(Model.Log);
					m_tsc.ContentPanel.Controls.Add(m_dc);
					m_dc.ActiveContentChanged += UpdateUI;
				}
			}
		}
		private DockContainer m_dc;

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
			#region Menu
			#region File Menu
			m_menu_file_new.Click += (s,a) =>
			{
				EditFile(null, false);
			};
			m_menu_file_edit_script.Click += (s,a) =>
			{
				EditFile(null, true);
			};
			m_menu_file_open.Click += (s,a) =>
			{
				OpenFile(null, false);
			};
			m_menu_file_open_additional.Click += (s,a) =>
			{
				OpenFile(null, true);
			};
			m_menu_file_save.Click += (s,a) =>
			{
				SaveFile(false);
			};
			m_menu_file_save_as.Click += (s,a) =>
			{
				SaveFile(true);
			};
			m_menu_file_options.Click += (s,a) =>
			{
				ShowOptions();
			};
			m_menu_file_recent_files.ToolTipText = "Shift+Click to open additionally\r\nCtrl+Click to edit";
			m_recent_files = new RecentFiles(m_menu_file_recent_files, HandleRecentFile);
			m_recent_files.Import(Settings.RecentFiles);
			m_recent_files.RecentListChanged += (s,a) => Settings.RecentFiles = m_recent_files.Export();
			m_menu_file_exit.Click += (s,a) =>
			{
				Close();
			};
			#endregion
			#region Navigation Menu
			m_menu_nav_reset_on_reload.Click += (s,a) =>
			{
				Settings.ResetOnLoad = !Settings.ResetOnLoad;
				UpdateUI();
			};
			m_menu_nav_reset_view_all.Click += (s,a) =>
			{
				Model.ResetView(View3d.ESceneBounds.All);
				Invalidate();
			};
			m_menu_nav_reset_view_selected.Click += (s,a) =>
			{
				Model.ResetView(View3d.ESceneBounds.Selected);
				Invalidate();
			};
			m_menu_nav_reset_view_visible.Click += (s,a) =>
			{
				Model.ResetView(View3d.ESceneBounds.Visible);
				Invalidate();
			};
			m_menu_nav_view_xpos.Click += (s, a) =>
			{
				Model.CamForwardAxis(-v4.XAxis);
				Invalidate();
			};
			m_menu_nav_view_xneg.Click += (s, a) =>
			{
				Model.CamForwardAxis(+v4.XAxis);
				Invalidate();
			};
			m_menu_nav_view_ypos.Click += (s, a) =>
			{
				Model.CamForwardAxis(-v4.YAxis);
				Invalidate();
			};
			m_menu_nav_view_yneg.Click += (s, a) =>
			{
				Model.CamForwardAxis(+v4.YAxis);
				Invalidate();
			};
			m_menu_nav_view_zpos.Click += (s, a) =>
			{
				Model.CamForwardAxis(-v4.ZAxis);
				Invalidate();
			};
			m_menu_nav_view_zneg.Click += (s, a) =>
			{
				Model.CamForwardAxis(+v4.ZAxis);
				Invalidate();
			};
			m_menu_nav_view_xyz.Click += (s, a) =>
			{
				Model.CamForwardAxis(new v4(-0.577350f, -0.577350f, -0.577350f, 0));
				Invalidate();
			};
			m_menu_nav_zoom_default.Click += (s,a) =>
			{
				Model.Scene.AutoRange();
				UpdateUI();
			};
			m_menu_nav_zoom_aspect11.Click += (s,a) =>
			{
				Model.Scene.Aspect = 1.0f;
				UpdateUI();
			};
			m_menu_nav_zoom_lock_aspect.Click += (s,a) =>
			{
				Model.Scene.LockAspect = !Model.Scene.LockAspect;
				UpdateUI();
			};
			m_menu_nav_align_none.Click += (s, a) =>
			{
				AlignCamera(v4.Zero);
			};
			m_menu_nav_align_x.Click += (s, a) =>
			{
				AlignCamera(v4.XAxis);
			};
			m_menu_nav_align_y.Click += (s, a) =>
			{
				AlignCamera(v4.YAxis);
			};
			m_menu_nav_align_z.Click += (s, a) =>
			{
				AlignCamera(v4.ZAxis);
			};
			m_menu_nav_align_current.Click += (s, a) =>
			{
				AlignCamera(Model.Camera.O2W.y);
			};
			m_menu_nav_save_view.Click += (s,a) =>
			{
				Model.SaveView();
				UpdateSavedViewsMenu();
			};
			m_menu_nav_saved_views_clear.Click += (s,a) =>
			{
				Model.SavedViews.Clear();
				UpdateSavedViewsMenu();
			};
			m_menu_nav_camera.Click += (s,a) =>
			{
				ShowCameraUI();
			};
			#endregion
			#region Data Menu
			m_menu_data_clear_scene.Click += (s,a) =>
			{
				ClearScene();
			};
			m_menu_data_auto_refresh.Click += (s,a) =>
			{
				Model.Settings.AutoRefresh = !Model.Settings.AutoRefresh;
				Model.AutoRefreshSources = Model.Settings.AutoRefresh;
				UpdateUI();
			};
			m_menu_data_create_demo_scene.Click += (s,a) =>
			{
				Model.CreateDemoScene();
			};
			m_menu_data_object_manager.Click += (s,a) =>
			{
				Model.Window.ShowObjectManager(true);
			};
			#endregion
			#region Rendering Menu
			m_menu_rendering_show_focus.Click += (s,a) =>
			{
				Model.Window.FocusPointVisible = !Model.Window.FocusPointVisible;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_show_origin.Click += (s,a) =>
			{
				Model.Window.OriginPointVisible = !Model.Window.OriginPointVisible;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_show_selection.Click += (s,a) =>
			{
			};
			m_menu_rendering_show_bounds.Click += (s,a) =>
			{
				Model.Window.BBoxesVisible = !Model.Window.BBoxesVisible;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_fillmode_solid.Click += (s,a) =>
			{
				Model.Scene.Options.FillMode = View3d.EFillMode.Solid;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_fillmode_wireframe.Click += (s,a) =>
			{
				Model.Scene.Options.FillMode = View3d.EFillMode.Wireframe;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_fillmode_solidwire.Click += (s,a) =>
			{
				Model.Scene.Options.FillMode = View3d.EFillMode.SolidWire;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_cullmode_none.Click += (s,a) =>
			{
				Model.Scene.Options.CullMode = View3d.ECullMode.None;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_cullmode_back.Click += (s,a) =>
			{
				Model.Scene.Options.CullMode = View3d.ECullMode.Back;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_cullmode_front.Click += (s,a) =>
			{
				Model.Scene.Options.CullMode = View3d.ECullMode.Front;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_orthographic.Click += (s,a) =>
			{
				Model.Camera.Orthographic = !Model.Camera.Orthographic;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_lighting.Click += (s,a) =>
			{
				ShowLightingUI();
			};
			#endregion
			#region Window menu
			m_menu_window.DropDownItems.Insert(0, m_dc.WindowsMenu());
			m_menu_window_always_on_top.Click += (s,a) =>
			{
				TopMost = !TopMost;
				UpdateUI();
			};
			m_menu_window_example_script.Click += (s,a) =>
			{
				ShowExampleScript();
			};
			m_menu_window_about.Click += (s,a) =>
			{
				new AboutUI().ShowDialog(this);
			};
			#endregion
			#endregion

			#region Progress Bar
			#endregion
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			// Update menu state
			m_menu_file_save.Enabled    = m_dc.ActiveContent?.Owner is ScriptUI;
			m_menu_file_save_as.Enabled = m_dc.ActiveContent?.Owner is ScriptUI;

			// Set check marks next to visible things
			m_menu_nav_reset_on_reload     .Checked = Settings.ResetOnLoad;
			m_menu_nav_zoom_lock_aspect    .Checked = Model.Scene.LockAspect;
			m_menu_data_auto_refresh       .Checked = Model.AutoRefreshSources;
			m_menu_rendering_show_focus    .Checked = Model.Window.FocusPointVisible;
			m_menu_rendering_show_origin   .Checked = Model.Window.OriginPointVisible;
			m_menu_rendering_show_selection.Checked = false;
			m_menu_rendering_show_bounds   .Checked = Model.Window.BBoxesVisible;
			m_menu_rendering_orthographic  .Checked = Model.Camera.Orthographic;

			// Display the next fill mode
			m_menu_rendering_fillmode_solid    .Checked = Model.Scene.Options.FillMode == View3d.EFillMode.Solid;
			m_menu_rendering_fillmode_wireframe.Checked = Model.Scene.Options.FillMode == View3d.EFillMode.Wireframe;
			m_menu_rendering_fillmode_solidwire.Checked = Model.Scene.Options.FillMode == View3d.EFillMode.SolidWire;
			m_menu_rendering_cullmode_none     .Checked = Model.Scene.Options.CullMode == View3d.ECullMode.None;
			m_menu_rendering_cullmode_back     .Checked = Model.Scene.Options.CullMode == View3d.ECullMode.Back;
			m_menu_rendering_cullmode_front    .Checked = Model.Scene.Options.CullMode == View3d.ECullMode.Front;

			m_menu_window_always_on_top.Checked = TopMost;

			// Update file loading progress
			UpdateProgress();
		}

		/// <summary>Create a new file and an editor to edit the file</summary>
		private void EditFile(string filepath, bool prompt)
		{
			if (prompt && !filepath.HasValue())
			{
				using (var dlg = new OpenFileDialog { Title = "Edit Ldr Script file", Filter = Util.FileDialogFilter("Ldr Script","*.ldr") })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					filepath = dlg.FileName;
				}
			}

			// Add a script window
			var script = m_dc.Add2(new ScriptUI(Model), EDockSite.Left);

			// If a filepath is given, load the script UI with the file
			if (filepath.HasValue())
			{
				m_recent_files.Add(filepath);
				script.LoadFile(filepath);
			}
		}

		/// <summary>Add a file source</summary>
		private void OpenFile(string filepath, bool additional)
		{
			if (!filepath.HasValue())
			{
				using (var dlg = new OpenFileDialog { Title = "Open Ldr Script file", Filter = Util.FileDialogFilter("Ldr Script","*.ldr", "Comma Separated Values","*.csv", "Binary Model File","*.p3d", "All Files","*.*") })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					filepath = dlg.FileName;
				}
			}

			// Add the file to the recent file history
			m_recent_files.Add(filepath);

			// Load the file source
			Model.OpenFile(filepath, additional);

			// Set the title to the last loaded file
			if (Model.ContextIds.Count == 0 || !additional)
				Text = "LDraw - {0}".Fmt(filepath);
		}

		/// <summary>Save a script UI to file</summary>
		private void SaveFile(bool save_as)
		{
			// Get the script window to save
			var script = m_dc.ActiveContent.Owner as ScriptUI;
			Debug.Assert(script != null, "Should be able to save if a script isn't selected");
			script.SaveFile(save_as ? null : script.Filepath);
		}

		/// <summary>Display the options dialog</summary>
		private void ShowOptions()
		{
			using (var dlg = new SettingsUI(this, Settings))
				dlg.ShowDialog();
		}

		/// <summary>Clear or script sources</summary>
		private void ClearScene()
		{
			Model.ClearScene();
			Text = "LDraw";
			Invalidate();
		}

		/// <summary>Align the camera to an axis</summary>
		private void AlignCamera(v4 axis)
		{
			m_menu_nav_align_none   .Checked = axis == v4.Zero;
			m_menu_nav_align_x      .Checked = axis == v4.XAxis;
			m_menu_nav_align_y      .Checked = axis == v4.YAxis;
			m_menu_nav_align_z      .Checked = axis == v4.ZAxis;
			m_menu_nav_align_current.Checked = axis == Model.Camera.O2W.y;
			Model.AlignCamera(axis);
			Invalidate();
		}

		/// <summary>Update the sub-menu of saved views</summary>
		private void UpdateSavedViewsMenu()
		{
			// Remove all menu items
			m_menu_nav_saved_views.DropDownItems.Clear();

			// Add the saved views
			int i = 0;
			foreach (var sv in Model.SavedViews)
			{
				var opt = m_menu_nav_saved_views.DropDownItems.Add2(new ToolStripMenuItem(sv.Name));
				opt.ShortcutKeys = Keys.Control | (Keys)('1' + i++);
				opt.Click += (s,a) =>
				{
					sv.Apply(Model.Camera);
					Invalidate();
				};
				opt.MouseDown += (s,a) =>
				{
					if (a.Button != MouseButtons.Right) return;
					var dd = (ToolStripDropDown)opt.GetCurrentParent();
					dd.AutoClose = false;

					var menu = new ContextMenuStrip();
					menu.Items.Add2("Remove", null, (ss,aa) => Model.SavedViews.Remove(sv));
					menu.Closed += (ss,aa) => { dd.AutoClose = true; dd.Close(); };
					menu.Show(dd, opt.Bounds.X + a.X, opt.Bounds.Y + a.Y);
				};
			}

			// Add the separator and clear option
			m_menu_nav_saved_views.DropDownItems.Add(m_sep_saved_views);
			m_menu_nav_saved_views.DropDownItems.Add(m_menu_nav_saved_views_clear);
		}

		/// <summary>Show the camera UI</summary>
		private void ShowCameraUI()
		{
			CameraUI.Show(this);
		}

		/// <summary>Show the lighting UI</summary>
		private void ShowLightingUI()
		{
			LightingUI.Show(this);
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
			if ((ModifierKeys & Keys.Control) != 0)
				EditFile(filepath, false);
			else
				OpenFile(filepath, (ModifierKeys & Keys.Shift) != 0);
		}

		/// <summary>Create a Text window containing the example script</summary>
		private void ShowExampleScript()
		{
			var ui = m_dc.Add2(new ScriptUI(Model), EDockSite.Left);
			ui.Editor.Text = Model.View3d.ExampleScript;
		}

		/// <summary>Update the state of the progress bar</summary>
		public void UpdateProgress()
		{
			var progress = Model.AddFileProgress;

			// Show/Hide the progress indicators
			m_pb_loading .Visible = progress != null;
			m_lbl_loading.Visible = progress != null;

			if (progress != null)
			{
				var finfo = new FileInfo(progress.Filepath);
				m_lbl_loading.Text = progress.Filepath;
				m_pb_loading.ValueFrac(finfo.Length != 0 ? Maths.Clamp((float)progress.FileOffset / finfo.Length, 0f, 1f) : 1f);
			}
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainUI));
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_ss = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_lbl_loading = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_pb_loading = new System.Windows.Forms.ToolStripProgressBar();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_new = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_edit_script = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_open = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open_additional = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_save = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_save_as = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_options = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_recent_files = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep0 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_reset_on_reload = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_nav_reset_view = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_reset_view_all = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_reset_view_selected = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_reset_view_visible = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_xpos = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_xneg = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_ypos = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_yneg = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_zpos = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_zneg = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_view_xyz = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_nav_zoom = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_zoom_default = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_zoom_aspect11 = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_zoom_lock_aspect = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_nav_align = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_align_none = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_align_x = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_align_y = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_align_z = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_align_current = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_nav_save_view = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_nav_saved_views = new System.Windows.Forms.ToolStripMenuItem();
			this.m_sep_saved_views = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_nav_saved_views_clear = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_nav_camera = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_data = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_data_clear_scene = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_data_auto_refresh = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_data_create_demo_scene = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_data_object_manager = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_show_focus = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_show_origin = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_show_selection = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_show_bounds = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_rendering_fillmode = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_fillmode_solid = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_fillmode_wireframe = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_fillmode_solidwire = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_cullmode = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_cullmode_none = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_cullmode_back = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_cullmode_front = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_rendering_orthographic = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_rendering_lighting = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_window = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_window_always_on_top = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_window_example_script = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_window_about = new System.Windows.Forms.ToolStripMenuItem();
			this.m_tsc.BottomToolStripPanel.SuspendLayout();
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
            this.m_status,
            this.m_lbl_loading,
            this.m_pb_loading});
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
			// m_lbl_spacer
			// 
			this.m_lbl_loading.Name = "m_lbl_spacer";
			this.m_lbl_loading.Size = new System.Drawing.Size(389, 17);
			this.m_lbl_loading.Spring = true;
			this.m_lbl_loading.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_pb_loading
			// 
			this.m_pb_loading.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.m_pb_loading.Name = "m_pb_loading";
			this.m_pb_loading.Size = new System.Drawing.Size(200, 16);
			this.m_pb_loading.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
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
            this.m_menu_file_edit_script,
            this.toolStripSeparator8,
            this.m_menu_file_open,
            this.m_menu_file_open_additional,
            this.m_menu_file_save,
            this.m_menu_file_save_as,
            this.toolStripSeparator14,
            this.m_menu_file_options,
            this.toolStripSeparator1,
            this.m_menu_file_recent_files,
            this.m_sep0,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_new
			// 
			this.m_menu_file_new.Name = "m_menu_file_new";
			this.m_menu_file_new.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.m_menu_file_new.Size = new System.Drawing.Size(236, 22);
			this.m_menu_file_new.Text = "&New";
			// 
			// m_menu_file_edit_script
			// 
			this.m_menu_file_edit_script.Name = "m_menu_file_edit_script";
			this.m_menu_file_edit_script.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
			this.m_menu_file_edit_script.Size = new System.Drawing.Size(236, 22);
			this.m_menu_file_edit_script.Text = "&Edit Script";
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(233, 6);
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
			this.m_menu_file_open_additional.Text = "Open A&dditional";
			// 
			// m_menu_file_save
			// 
			this.m_menu_file_save.Name = "m_menu_file_save";
			this.m_menu_file_save.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.m_menu_file_save.Size = new System.Drawing.Size(236, 22);
			this.m_menu_file_save.Text = "&Save";
			// 
			// m_menu_file_save_as
			// 
			this.m_menu_file_save_as.Name = "m_menu_file_save_as";
			this.m_menu_file_save_as.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.m_menu_file_save_as.Size = new System.Drawing.Size(236, 22);
			this.m_menu_file_save_as.Text = "Save &As";
			// 
			// toolStripSeparator14
			// 
			this.toolStripSeparator14.Name = "toolStripSeparator14";
			this.toolStripSeparator14.Size = new System.Drawing.Size(233, 6);
			// 
			// m_menu_file_options
			// 
			this.m_menu_file_options.Name = "m_menu_file_options";
			this.m_menu_file_options.Size = new System.Drawing.Size(236, 22);
			this.m_menu_file_options.Text = "&Options...";
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
			// m_menu_nav
			// 
			this.m_menu_nav.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_nav_reset_on_reload,
            this.toolStripSeparator13,
            this.m_menu_nav_reset_view,
            this.m_menu_nav_view,
            this.toolStripSeparator4,
            this.m_menu_nav_zoom,
            this.toolStripSeparator11,
            this.m_menu_nav_align,
            this.toolStripSeparator5,
            this.m_menu_nav_save_view,
            this.m_menu_nav_saved_views,
            this.toolStripSeparator12,
            this.m_menu_nav_camera});
			this.m_menu_nav.Name = "m_menu_nav";
			this.m_menu_nav.Size = new System.Drawing.Size(77, 20);
			this.m_menu_nav.Text = "&Navigation";
			// 
			// m_menu_nav_reset_on_reload
			// 
			this.m_menu_nav_reset_on_reload.Name = "m_menu_nav_reset_on_reload";
			this.m_menu_nav_reset_on_reload.Size = new System.Drawing.Size(160, 22);
			this.m_menu_nav_reset_on_reload.Text = "Reset on Reload";
			// 
			// toolStripSeparator13
			// 
			this.toolStripSeparator13.Name = "toolStripSeparator13";
			this.toolStripSeparator13.Size = new System.Drawing.Size(157, 6);
			// 
			// m_menu_nav_reset_view
			// 
			this.m_menu_nav_reset_view.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_nav_reset_view_all,
            this.m_menu_nav_reset_view_selected,
            this.m_menu_nav_reset_view_visible});
			this.m_menu_nav_reset_view.Name = "m_menu_nav_reset_view";
			this.m_menu_nav_reset_view.Size = new System.Drawing.Size(160, 22);
			this.m_menu_nav_reset_view.Text = "&Reset View";
			// 
			// m_menu_nav_reset_view_all
			// 
			this.m_menu_nav_reset_view_all.Name = "m_menu_nav_reset_view_all";
			this.m_menu_nav_reset_view_all.Size = new System.Drawing.Size(118, 22);
			this.m_menu_nav_reset_view_all.Text = "&All";
			// 
			// m_menu_nav_reset_view_selected
			// 
			this.m_menu_nav_reset_view_selected.Name = "m_menu_nav_reset_view_selected";
			this.m_menu_nav_reset_view_selected.Size = new System.Drawing.Size(118, 22);
			this.m_menu_nav_reset_view_selected.Text = "&Selected";
			// 
			// m_menu_nav_reset_view_visible
			// 
			this.m_menu_nav_reset_view_visible.Name = "m_menu_nav_reset_view_visible";
			this.m_menu_nav_reset_view_visible.Size = new System.Drawing.Size(118, 22);
			this.m_menu_nav_reset_view_visible.Text = "&Visible";
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
			this.m_menu_nav_view.Size = new System.Drawing.Size(160, 22);
			this.m_menu_nav_view.Text = "&View";
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
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(157, 6);
			// 
			// m_menu_nav_zoom
			// 
			this.m_menu_nav_zoom.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_nav_zoom_default,
            this.m_menu_nav_zoom_aspect11,
            this.m_menu_nav_zoom_lock_aspect});
			this.m_menu_nav_zoom.Name = "m_menu_nav_zoom";
			this.m_menu_nav_zoom.Size = new System.Drawing.Size(160, 22);
			this.m_menu_nav_zoom.Text = "&Zoom";
			// 
			// m_menu_nav_zoom_default
			// 
			this.m_menu_nav_zoom_default.Name = "m_menu_nav_zoom_default";
			this.m_menu_nav_zoom_default.Size = new System.Drawing.Size(138, 22);
			this.m_menu_nav_zoom_default.Text = "&Default";
			// 
			// m_menu_nav_zoom_aspect11
			// 
			this.m_menu_nav_zoom_aspect11.Name = "m_menu_nav_zoom_aspect11";
			this.m_menu_nav_zoom_aspect11.Size = new System.Drawing.Size(138, 22);
			this.m_menu_nav_zoom_aspect11.Text = "Aspect 1:1";
			// 
			// m_menu_nav_zoom_lock_aspect
			// 
			this.m_menu_nav_zoom_lock_aspect.Name = "m_menu_nav_zoom_lock_aspect";
			this.m_menu_nav_zoom_lock_aspect.Size = new System.Drawing.Size(138, 22);
			this.m_menu_nav_zoom_lock_aspect.Text = "Lock Aspect";
			// 
			// toolStripSeparator11
			// 
			this.toolStripSeparator11.Name = "toolStripSeparator11";
			this.toolStripSeparator11.Size = new System.Drawing.Size(157, 6);
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
			this.m_menu_nav_align.Size = new System.Drawing.Size(160, 22);
			this.m_menu_nav_align.Text = "&Align";
			// 
			// m_menu_nav_align_none
			// 
			this.m_menu_nav_align_none.Name = "m_menu_nav_align_none";
			this.m_menu_nav_align_none.Size = new System.Drawing.Size(114, 22);
			this.m_menu_nav_align_none.Text = "&None";
			// 
			// m_menu_nav_align_x
			// 
			this.m_menu_nav_align_x.Name = "m_menu_nav_align_x";
			this.m_menu_nav_align_x.Size = new System.Drawing.Size(114, 22);
			this.m_menu_nav_align_x.Text = "&X";
			// 
			// m_menu_nav_align_y
			// 
			this.m_menu_nav_align_y.Name = "m_menu_nav_align_y";
			this.m_menu_nav_align_y.Size = new System.Drawing.Size(114, 22);
			this.m_menu_nav_align_y.Text = "&Y";
			// 
			// m_menu_nav_align_z
			// 
			this.m_menu_nav_align_z.Name = "m_menu_nav_align_z";
			this.m_menu_nav_align_z.Size = new System.Drawing.Size(114, 22);
			this.m_menu_nav_align_z.Text = "&Z";
			// 
			// m_menu_nav_align_current
			// 
			this.m_menu_nav_align_current.Name = "m_menu_nav_align_current";
			this.m_menu_nav_align_current.Size = new System.Drawing.Size(114, 22);
			this.m_menu_nav_align_current.Text = "&Current";
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(157, 6);
			// 
			// m_menu_nav_save_view
			// 
			this.m_menu_nav_save_view.Name = "m_menu_nav_save_view";
			this.m_menu_nav_save_view.Size = new System.Drawing.Size(160, 22);
			this.m_menu_nav_save_view.Text = "&Save View";
			// 
			// m_menu_nav_saved_views
			// 
			this.m_menu_nav_saved_views.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_sep_saved_views,
            this.m_menu_nav_saved_views_clear});
			this.m_menu_nav_saved_views.Name = "m_menu_nav_saved_views";
			this.m_menu_nav_saved_views.Size = new System.Drawing.Size(160, 22);
			this.m_menu_nav_saved_views.Text = "Save&d Views";
			// 
			// m_sep_saved_views
			// 
			this.m_sep_saved_views.Name = "m_sep_saved_views";
			this.m_sep_saved_views.Size = new System.Drawing.Size(165, 6);
			// 
			// m_menu_nav_saved_views_clear
			// 
			this.m_menu_nav_saved_views_clear.Name = "m_menu_nav_saved_views_clear";
			this.m_menu_nav_saved_views_clear.Size = new System.Drawing.Size(168, 22);
			this.m_menu_nav_saved_views_clear.Text = "&Clear Saved Views";
			// 
			// toolStripSeparator12
			// 
			this.toolStripSeparator12.Name = "toolStripSeparator12";
			this.toolStripSeparator12.Size = new System.Drawing.Size(157, 6);
			// 
			// m_menu_nav_camera
			// 
			this.m_menu_nav_camera.Name = "m_menu_nav_camera";
			this.m_menu_nav_camera.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.M)));
			this.m_menu_nav_camera.Size = new System.Drawing.Size(160, 22);
			this.m_menu_nav_camera.Text = "&Camera";
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
			// m_menu_data_auto_refresh
			// 
			this.m_menu_data_auto_refresh.Name = "m_menu_data_auto_refresh";
			this.m_menu_data_auto_refresh.Size = new System.Drawing.Size(177, 22);
			this.m_menu_data_auto_refresh.Text = "&Auto Refresh";
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
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(174, 6);
			// 
			// m_menu_data_object_manager
			// 
			this.m_menu_data_object_manager.Name = "m_menu_data_object_manager";
			this.m_menu_data_object_manager.Size = new System.Drawing.Size(177, 22);
			this.m_menu_data_object_manager.Text = "&Object Manager";
			// 
			// m_menu_rendering
			// 
			this.m_menu_rendering.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_rendering_show_focus,
            this.m_menu_rendering_show_origin,
            this.m_menu_rendering_show_selection,
            this.m_menu_rendering_show_bounds,
            this.toolStripSeparator9,
            this.m_menu_rendering_fillmode,
            this.m_menu_rendering_cullmode,
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
			// m_menu_rendering_fillmode
			// 
			this.m_menu_rendering_fillmode.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_rendering_fillmode_solid,
            this.m_menu_rendering_fillmode_wireframe,
            this.m_menu_rendering_fillmode_solidwire});
			this.m_menu_rendering_fillmode.Name = "m_menu_rendering_fillmode";
			this.m_menu_rendering_fillmode.Size = new System.Drawing.Size(158, 22);
			this.m_menu_rendering_fillmode.Text = "&Fill Mode";
			// 
			// m_menu_rendering_fillmode_solid
			// 
			this.m_menu_rendering_fillmode_solid.Name = "m_menu_rendering_fillmode_solid";
			this.m_menu_rendering_fillmode_solid.Size = new System.Drawing.Size(174, 22);
			this.m_menu_rendering_fillmode_solid.Text = "&Solid";
			// 
			// m_menu_rendering_fillmode_wireframe
			// 
			this.m_menu_rendering_fillmode_wireframe.Name = "m_menu_rendering_fillmode_wireframe";
			this.m_menu_rendering_fillmode_wireframe.Size = new System.Drawing.Size(174, 22);
			this.m_menu_rendering_fillmode_wireframe.Text = "&Wire Frame";
			// 
			// m_menu_rendering_fillmode_solidwire
			// 
			this.m_menu_rendering_fillmode_solidwire.Name = "m_menu_rendering_fillmode_solidwire";
			this.m_menu_rendering_fillmode_solidwire.Size = new System.Drawing.Size(174, 22);
			this.m_menu_rendering_fillmode_solidwire.Text = "Solid + Wire &Frame";
			// 
			// m_menu_rendering_cullmode
			// 
			this.m_menu_rendering_cullmode.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_rendering_cullmode_none,
            this.m_menu_rendering_cullmode_back,
            this.m_menu_rendering_cullmode_front});
			this.m_menu_rendering_cullmode.Name = "m_menu_rendering_cullmode";
			this.m_menu_rendering_cullmode.Size = new System.Drawing.Size(158, 22);
			this.m_menu_rendering_cullmode.Text = "&Cull Mode";
			// 
			// m_menu_rendering_cullmode_none
			// 
			this.m_menu_rendering_cullmode_none.Name = "m_menu_rendering_cullmode_none";
			this.m_menu_rendering_cullmode_none.Size = new System.Drawing.Size(103, 22);
			this.m_menu_rendering_cullmode_none.Text = "&None";
			// 
			// m_menu_rendering_cullmode_back
			// 
			this.m_menu_rendering_cullmode_back.Name = "m_menu_rendering_cullmode_back";
			this.m_menu_rendering_cullmode_back.Size = new System.Drawing.Size(103, 22);
			this.m_menu_rendering_cullmode_back.Text = "&Back";
			// 
			// m_menu_rendering_cullmode_front
			// 
			this.m_menu_rendering_cullmode_front.Name = "m_menu_rendering_cullmode_front";
			this.m_menu_rendering_cullmode_front.Size = new System.Drawing.Size(103, 22);
			this.m_menu_rendering_cullmode_front.Text = "&Front";
			// 
			// m_menu_rendering_orthographic
			// 
			this.m_menu_rendering_orthographic.Name = "m_menu_rendering_orthographic";
			this.m_menu_rendering_orthographic.Size = new System.Drawing.Size(158, 22);
			this.m_menu_rendering_orthographic.Text = "O&rthographic";
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			this.toolStripSeparator10.Size = new System.Drawing.Size(155, 6);
			// 
			// m_menu_rendering_lighting
			// 
			this.m_menu_rendering_lighting.Name = "m_menu_rendering_lighting";
			this.m_menu_rendering_lighting.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
			this.m_menu_rendering_lighting.Size = new System.Drawing.Size(158, 22);
			this.m_menu_rendering_lighting.Text = "Lighting";
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
			this.m_menu_window_always_on_top.Size = new System.Drawing.Size(151, 22);
			this.m_menu_window_always_on_top.Text = "Always on &Top";
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(148, 6);
			// 
			// m_menu_window_example_script
			// 
			this.m_menu_window_example_script.Name = "m_menu_window_example_script";
			this.m_menu_window_example_script.Size = new System.Drawing.Size(151, 22);
			this.m_menu_window_example_script.Text = "&Example Script";
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(148, 6);
			// 
			// m_menu_window_about
			// 
			this.m_menu_window_about.Name = "m_menu_window_about";
			this.m_menu_window_about.Size = new System.Drawing.Size(151, 22);
			this.m_menu_window_about.Text = "&About";
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
