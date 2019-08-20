//*************************************************************************************************
// LDraw
// Copyright (c) Rylogic Ltd 2016
//*************************************************************************************************
//#define TRAP_UNHANDLED_EXCEPTIONS
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WinForms;
using Rylogic.Interop.Win32;
using Rylogic.Maths;
using Rylogic.Scintilla;
using Rylogic.Utility;
using ToolStripContainer = Rylogic.Gui.WinForms.ToolStripContainer;

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
		private ToolStripMenuItem m_menu_data_clear_all_scenes;
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
		private ToolStripStatusLabel m_lbl_pointer_location;
		private ToolStripMenuItem m_menu_window_new_scene;
		private ToolStripSeparator toolStripSeparator15;
		private ToolStrip m_ts_multiview;
		private ToolStripButton m_btn_link_camera_left_right;
		private ToolStripLabel m_lbl_link_cameras;
		private ToolStripButton m_btn_link_camera_up_down;
		private ToolStripButton m_btn_link_camera_in_out;
		private ToolStripButton m_btn_link_camera_rotate;
		private ToolStripSeparator toolStripSeparator16;
		private ToolStripLabel m_lbl_link_axes;
		private ToolStripButton m_btn_link_xaxis;
		private ToolStripButton m_btn_link_yaxis;
		private ToolStripMenuItem m_menu_data_clear_scene;
		private ToolStripSeparator toolStripSeparator17;
		private ToolStrip m_ts_anim_controls;
		private ToolStripButton m_btn_reset;
		private ToolStripLabel m_lbl_anim_controls;
		private ToolStripButton m_btn_run;
		private ToolStripButton m_btn_step_bck;
		private ToolStripButton m_btn_step_fwd;
		private ToolStripLabel m_lbl_step_rate;
		private ToolStripLabel m_lbl_anim_clock;
		private ToolStripTextBox m_tb_clock;
		private ToolStripSeparator toolStripSeparator18;
		private ToolStripMenuItem m_menu_rendering_anim_controls;
		private ToolStripTrackBar m_tr_speed;
		private ToolStrip m_ts_tools;
		private ToolStripLabel m_lbl_tools;
		private ToolStripButton m_btn_measure;
		private ToolStripSeparator toolStripSeparator19;
		private ToolStripSeparator toolStripSeparator8;
		private ToolStripMenuItem m_menu_file_save_as;
		#endregion

		/// <summary>The main entry point for the application.</summary>
		[STAThread] static void Main(string[] args)
		{
			var unhandled = (Exception)null;
			#if !DEBUG || TRAP_UNHANDLED_EXCEPTIONS
			try {
			#endif

				Debug.WriteLine($"{Application.ExecutablePath} is a {(Environment.Is64BitProcess?"64":"32")}bit process");
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				// Parse the command line
				Exception err = null;
				try { StartupOptions = new StartupOptions(args); }
				catch (Exception ex) { err = ex; }

				// If there was an error display the error message
				if (err != null)
				{
					MsgBox.Show(null,
						"There is an error in the startup options provided.\r\n"+
						$"{err.Message}"
						, "Command Line Error"
						, MessageBoxButtons.OK
						, MessageBoxIcon.Error);
					Environment.ExitCode = 1;
					return;
				}

				// If they just want help displayed...
				if (StartupOptions.ShowHelp)
				{
					MsgBox.Show(null,
						CmdLine.Help,
						Application.ProductName,
						MessageBoxButtons.OK,
						MessageBoxIcon.Information);
					Environment.ExitCode = 0;
					return;
				}

				// Otherwise show the app
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
				var crash_report =
					$"Unhandled exception: {unhandled.GetType().Name}\r\n"+
					$"Message: {unhandled.MessageFull()}\r\n"+
					$"Date: {DateTimeOffset.Now}\r\n"+
					$"Stack:\r\n{unhandled.StackTrace}";

				File.WriteAllText(crash_dump_file, crash_report);
				var res = MessageBox.Show(
					$"Shutting down due to an unhandled exception.\n"+
					$"{unhandled.MessageFull()}\n\n"+
					$"A crash report has been generated here:\n"+
					$"{crash_dump_file}\n\n",
					"Unexpected Shutdown", MessageBoxButtons.OK);
			}
		}
		public MainUI()
		{
			#if DEBUG
			Util.WaitForDebugger();
			#endif

			// Load dlls
			try { View3d.LoadDll(); }
			catch (DllNotFoundException ex)
			{
				if (this.IsInDesignMode()) return;
				MessageBox.Show(ex.Message);
			}
			try { Sci.LoadDll(); }
			catch (DllNotFoundException ex)
			{
				if (this.IsInDesignMode()) return;
				MessageBox.Show(ex.Message);
			}

			InitializeComponent();
			KeyPreview = true;

			Xml_.Config.SupportWinFormsTypes();
			Settings = new Settings(StartupOptions.SettingsPath){ ReadOnly = true };
			DockContainer = new DockContainer();
			Model = new Model(this);
			AnimTimer = new Timer{ Interval = 1, Enabled = false };

			SetupUI();
			UpdateUI();

			RestoreWindowPosition();
			Settings.ReadOnly = false;
		}
		protected override void Dispose(bool disposing)
		{
			// The scripts and scene need to be disposed before view3d.
			// The Scene owns the view3d Window and the Model owns the Scene.
			// This is why 'Model' is disposed before 'DockContainer'
			Model = null;
			DockContainer = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
			AnimTimer = null;
		}
		protected override void OnInvalidated(InvalidateEventArgs e)
		{
			foreach (var scene in Model.Scenes)
				scene.Invalidate();

			base.OnInvalidated(e);
		}
		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			// Stop the RPC service
			Model.RPCService.Active = false;

			// Save the window position and size
			Settings.UI.WindowPosition = Bounds;
			Settings.UI.UILayout = m_dc.SaveLayout();
			Settings.Save();

			base.OnFormClosed(e);
		}
		protected override void OnShown(EventArgs e)
		{
			// Create a default scene if one has not been created from the settings
			if (Model.Scenes.Count == 0)
				Model.CurrentScene = Model.AddNewScene();

			// Load files from the command line
			foreach (var file in StartupOptions.FilesToLoad)
				OpenFile(file, true);

			// Start the RPC service
			//Model.RPCService.Active = true;

			base.OnShown(e);
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
			if (e.KeyCode == Keys.W && e.Control && Model.CurrentScene is SceneUI scene)
			{
				scene.Options.FillMode = Enum<View3d.EFillMode>.Cycle(scene.Options.FillMode);
				scene.Invalidate();
				e.Handled = true;
			}
			base.OnKeyDown(e);
		}

		/// <summary>Start up options</summary>
		public static StartupOptions StartupOptions { get; private set; }

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
				Util.Dispose(ref m_impl_model);
				m_impl_model = value;
				Update();
			}
		}
		private Model m_impl_model;

		/// <summary>Main application status label</summary>
		public ToolStripStatusLabel Status
		{
			get { return m_status; }
		}

		/// <summary>The pointer location status label</summary>
		public ToolStripStatusLabel PointerLocationStatus
		{
			get { return m_lbl_pointer_location; }
		}

		/// <summary>The dock container control</summary>
		public DockContainer DockContainer
		{
			get { return m_dc; }
			set
			{
				if (m_dc == value) return;
				if (m_dc != null)
				{
					// Remove the scene from the dock container before disposing it
					// so that the scene is left for the model to dispose.
					m_dc.ActiveContentChanged -= HandleActiveContentChanged;
					m_tsc.ContentPanel.Controls.Remove(m_dc);
					Util.Dispose(ref m_dc);
				}
				m_dc = value;
				if (m_dc != null)
				{
					m_dc.Dock = DockStyle.Fill;
					m_dc.Options.TabStrip.AlwaysShowTabs = true;
					m_dc.Options.TitleBar.ShowTitleBars = true;
					m_tsc.ContentPanel.Controls.Add(m_dc);
					m_dc.ActiveContentChanged += HandleActiveContentChanged;
				}

				// Handlers
				void HandleActiveContentChanged(object sender, ActiveContentChangedEventArgs e)
				{
					if (Model == null) return;
					if (e.ContentNew is SceneUI scene)
						Model.CurrentScene = scene;

					Settings.UI.UILayout = m_dc.SaveLayout();
					UpdateUI();
				}
			}
		}
		private DockContainer m_dc;

		/// <summary>Animation timer</summary>
		public Timer AnimTimer
		{
			get { return m_anim_timer; }
			private set
			{
				if (m_anim_timer == value) return;
				if (m_anim_timer != null)
				{
					m_anim_timer.Tick -= HandleAnimTimerTick;
				}
				m_anim_timer = value;
				if (m_anim_timer != null)
				{
					m_anim_timer.Tick += HandleAnimTimerTick;
				}
			}
		}
		private Timer m_anim_timer;
		private void HandleAnimTimerTick(object sender, EventArgs e)
		{
			if (m_anim_steps > 0)
			{
				if (m_anim_steps != int.MaxValue)
					--m_anim_steps;

				// Advance the clock
				AnimClock += m_anim_dir * (float)Math_.Sqr(2.0f * m_tr_speed.TrackBar.ValueFrac()) * 0.05f;
			}

			// Update the animation time in each scene
			foreach (var scene in Model.Scenes)
			{
				// Note: forcing a render in here doesn't prevent jerkiness during mouse operations
				scene.Window.AnimTime = AnimClock;
				scene.Window.Invalidate();
			}

			// Update the clock value
			m_tb_clock.Text = $"{AnimClock:N1}";
			AnimTimer.Enabled = m_anim_steps != 0;
		}

		/// <summary>The animation time</summary>
		public float AnimClock { get; set; }
		private int m_anim_steps;
		private int m_anim_dir;

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
			#region Dock Container
			if (Settings.UI.UILayout != null)
			{
				// Restore the layout
				m_dc.LoadLayout(Settings.UI.UILayout, (name,ty,udat) =>
				{
					// Restore the scenes
					if (ty == typeof(SceneUI).FullName)
						return Model.Scenes.Add2(new SceneUI(name, Model, udat)).DockControl;

					// Restore the scripts
					if (ty == typeof(ScriptUI).FullName)
						return Model.Scripts.Add2(new ScriptUI(name, Model, udat)).DockControl;

					return null;
				});
			}
			m_dc.ActiveContent = Model.Scenes.FirstOrDefault()?.DockControl;
			#endregion

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
				Model.CurrentScene.ResetView(View3d.ESceneBounds.All);
				Invalidate();
			};
			m_menu_nav_reset_view_selected.Click += (s,a) =>
			{
				Model.CurrentScene.ResetView(View3d.ESceneBounds.Selected);
				Invalidate();
			};
			m_menu_nav_reset_view_visible.Click += (s,a) =>
			{
				Model.CurrentScene.ResetView(View3d.ESceneBounds.Visible);
				Invalidate();
			};
			m_menu_nav_view_xpos.Click += (s, a) =>
			{
				Model.CurrentScene.CamForwardAxis(-v4.XAxis);
				Invalidate();
			};
			m_menu_nav_view_xneg.Click += (s, a) =>
			{
				Model.CurrentScene.CamForwardAxis(+v4.XAxis);
				Invalidate();
			};
			m_menu_nav_view_ypos.Click += (s, a) =>
			{
				Model.CurrentScene.CamForwardAxis(-v4.YAxis);
				Invalidate();
			};
			m_menu_nav_view_yneg.Click += (s, a) =>
			{
				Model.CurrentScene.CamForwardAxis(+v4.YAxis);
				Invalidate();
			};
			m_menu_nav_view_zpos.Click += (s, a) =>
			{
				Model.CurrentScene.CamForwardAxis(-v4.ZAxis);
				Invalidate();
			};
			m_menu_nav_view_zneg.Click += (s, a) =>
			{
				Model.CurrentScene.CamForwardAxis(+v4.ZAxis);
				Invalidate();
			};
			m_menu_nav_view_xyz.Click += (s, a) =>
			{
				Model.CurrentScene.CamForwardAxis(new v4(-0.577350f, -0.577350f, -0.577350f, 0));
				Invalidate();
			};
			m_menu_nav_zoom_default.Click += (s,a) =>
			{
				Model.CurrentScene.AutoRange();
				UpdateUI();
			};
			m_menu_nav_zoom_aspect11.Click += (s,a) =>
			{
				Model.CurrentScene.Aspect = 1.0f;
				UpdateUI();
			};
			m_menu_nav_zoom_lock_aspect.Click += (s,a) =>
			{
				Model.CurrentScene.LockAspect = !Model.CurrentScene.LockAspect;
				UpdateUI();
			};
			m_menu_nav_align.DropDownOpening += (s,a) =>
			{
				var axis = Model.CurrentScene?.Camera.AlignAxis ?? v4.Zero;
				m_menu_nav_align_none   .Checked = axis == v4.Zero;
				m_menu_nav_align_x      .Checked = axis == v4.XAxis;
				m_menu_nav_align_y      .Checked = axis == v4.YAxis;
				m_menu_nav_align_z      .Checked = axis == v4.ZAxis;
				m_menu_nav_align_current.Checked = axis != v4.Zero && axis != v4.XAxis && axis != v4.YAxis && axis != v4.ZAxis;
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
				AlignCamera(Model.CurrentScene.Camera.O2W.y);
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
			m_menu_data_auto_refresh.Click += (s,a) =>
			{
				Model.Settings.AutoRefresh = !Model.Settings.AutoRefresh;
				Model.AutoRefreshSources = Model.Settings.AutoRefresh;
				UpdateUI();
			};
			m_menu_data_clear_all_scenes.Click += (s,a) =>
			{
				ClearAllScenes();
			};
			m_menu_data_clear_scene.Click += (s,a) =>
			{
				Model.CurrentScene?.Clear(delete_objects:true);
			};
			m_menu_data_create_demo_scene.Click += (s,a) =>
			{
				Model.CreateDemoScene();
			};
			m_menu_data_object_manager.Click += (s,a) =>
			{
				Model.CurrentScene?.ShowObjectManager(true);
			};
			#endregion
			#region Rendering Menu
			m_menu_rendering_show_focus.Click += (s,a) =>
			{
				Model.CurrentScene.FocusPointVisible = !Model.CurrentScene.FocusPointVisible;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_show_origin.Click += (s,a) =>
			{
				Model.CurrentScene.OriginPointVisible = !Model.CurrentScene.OriginPointVisible;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_show_selection.Click += (s,a) =>
			{
				Model.CurrentScene.SelectionBoxVisible = !Model.CurrentScene.SelectionBoxVisible;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_show_bounds.Click += (s,a) =>
			{
				Model.CurrentScene.BBoxesVisible = !Model.CurrentScene.BBoxesVisible;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_fillmode_solid.Click += (s,a) =>
			{
				Model.CurrentScene.Options.FillMode = View3d.EFillMode.Solid;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_fillmode_wireframe.Click += (s,a) =>
			{
				Model.CurrentScene.Options.FillMode = View3d.EFillMode.Wireframe;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_fillmode_solidwire.Click += (s,a) =>
			{
				Model.CurrentScene.Options.FillMode = View3d.EFillMode.SolidWire;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_cullmode_none.Click += (s,a) =>
			{
				Model.CurrentScene.Options.CullMode = View3d.ECullMode.None;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_cullmode_back.Click += (s,a) =>
			{
				Model.CurrentScene.Options.CullMode = View3d.ECullMode.Back;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_cullmode_front.Click += (s,a) =>
			{
				Model.CurrentScene.Options.CullMode = View3d.ECullMode.Front;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_orthographic.Click += (s,a) =>
			{
				Model.CurrentScene.Camera.Orthographic = !Model.CurrentScene.Camera.Orthographic;
				Invalidate();
				UpdateUI();
			};
			m_menu_rendering_anim_controls.Click += (s,a) =>
			{
				m_ts_anim_controls.Visible = !m_ts_anim_controls.Visible;
				m_menu_rendering_anim_controls.Checked = m_ts_anim_controls.Visible;
			};
			m_menu_rendering_lighting.Click += (s,a) =>
			{
				ShowLightingUI();
			};
			#endregion
			#region Window menu
			m_menu_window_new_scene.Click += (s,a) =>
			{
				Model.AddNewScene();
			};
			m_menu_window.DropDownItems.Insert(1, m_dc.WindowsMenu());
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

			#region Link Cameras Tool bar
			m_btn_link_camera_left_right.Checked = Model.LinkCameras.HasFlag(ELinkCameras.LeftRight);
			m_btn_link_camera_left_right.CheckedChanged += (s,a) =>
			{
				Model.LinkCameras = Bit.SetBits(Model.LinkCameras, ELinkCameras.LeftRight, m_btn_link_camera_left_right.Checked);
			};
			m_btn_link_camera_up_down.Checked = Model.LinkCameras.HasFlag(ELinkCameras.UpDown);
			m_btn_link_camera_up_down.CheckedChanged += (s,a) =>
			{
				Model.LinkCameras = Bit.SetBits(Model.LinkCameras, ELinkCameras.UpDown, m_btn_link_camera_up_down.Checked);
			};
			m_btn_link_camera_in_out.Checked = Model.LinkCameras.HasFlag(ELinkCameras.InOut);
			m_btn_link_camera_in_out.CheckedChanged += (s,a) =>
			{
				Model.LinkCameras = Bit.SetBits(Model.LinkCameras, ELinkCameras.InOut, m_btn_link_camera_in_out.Checked);
			};
			m_btn_link_camera_rotate.Checked = Model.LinkCameras.HasFlag(ELinkCameras.Rotate);
			m_btn_link_camera_rotate.CheckedChanged += (s,a) =>
			{
				Model.LinkCameras = Bit.SetBits(Model.LinkCameras, ELinkCameras.Rotate, m_btn_link_camera_rotate.Checked);
			};

			m_btn_link_xaxis.Checked = Model.LinkAxes.HasFlag(ELinkAxes.XAxis);
			m_btn_link_xaxis.CheckedChanged += (s,a) =>
			{
				Model.LinkAxes = Bit.SetBits(Model.LinkAxes, ELinkAxes.XAxis, m_btn_link_xaxis.Checked);
			};
			m_btn_link_yaxis.Checked = Model.LinkAxes.HasFlag(ELinkAxes.YAxis);
			m_btn_link_yaxis.CheckedChanged += (s,a) =>
			{
				Model.LinkAxes = Bit.SetBits(Model.LinkAxes, ELinkAxes.YAxis, m_btn_link_yaxis.Checked);
			};
			#endregion

			#region Animation Controls Tool bar

			// Hide until needed
			m_ts_anim_controls.Visible = false;

			// Animation control buttons
			m_btn_reset.Image = Res.ImgStop;
			m_btn_reset.Click += (s,a) =>
			{
				AnimClock = 0;
				m_anim_steps = 0;
				m_btn_run.Image = Res.ImgPlay;
				AnimTimer.Enabled = true;
			};
			m_btn_step_bck.Image = Res.ImgStepBck;
			m_btn_step_bck.Click += (s,a) =>
			{
				m_anim_dir = -1;
				m_anim_steps = 1;
				m_btn_run.Image = Res.ImgPlay;
				AnimTimer.Enabled = true;
			};
			m_btn_step_fwd.Image = Res.ImgStepFwd;
			m_btn_step_fwd.Click += (s,a) =>
			{
				m_anim_dir = +1;
				m_anim_steps = 1;
				m_btn_run.Image = Res.ImgPlay;
				AnimTimer.Enabled = true;
			};
			m_btn_run.Image = Res.ImgPlay;
			m_btn_run.Click += (s,a) =>
			{
				// Toggle between play/pause
				if (m_btn_run.Image == Res.ImgPlay)
				{
					m_anim_dir = +1;
					m_anim_steps = int.MaxValue;
					m_btn_run.Image = Res.ImgPause;
					AnimTimer.Enabled = true;
				}
				else if (m_btn_run.Image == Res.ImgPause)
				{
					m_anim_dir = +1;
					m_anim_steps = 0;
					m_btn_run.Image = Res.ImgPlay;
					AnimTimer.Enabled = true;
				}
			};

			// Step rate
			m_tr_speed.TrackBar.Set(50, 0, 100);
			#endregion

			#region Tools tool bar

			// Measure tool
			m_btn_measure.ToolTipText = "Open the measurement tool";
			m_btn_measure.Click += (s,a) =>
			{
				if (Model.CurrentScene != null)
					Model.CurrentScene.ShowMeasurementUI = true;
			};

			#endregion

			#region Progress Bar
			m_pb_loading.Alignment = ToolStripItemAlignment.Right;
			#endregion

			#region Pointer Location
			m_lbl_pointer_location.Alignment = ToolStripItemAlignment.Right;
			#endregion
		}

		/// <summary>Update UI elements</summary>
		public void UpdateUI(object sender = null, EventArgs args = null)
		{
			if (Model == null)
				return;

			var scene         = Model.CurrentScene;
			var has_scene     = scene != null;
			var scene_active  = m_dc.ActiveContent?.Owner is SceneUI;
			var script_active = m_dc.ActiveContent?.Owner is ScriptUI;

			// Enable/Disable menu items
			{
				m_menu_file_new            .Enabled = true;
				m_menu_file_edit_script    .Enabled = true;
				m_menu_file_open           .Enabled = has_scene;
				m_menu_file_open_additional.Enabled = has_scene;
				m_menu_file_save           .Enabled = script_active;
				m_menu_file_save_as        .Enabled = script_active;
				m_menu_file_options        .Enabled = true;
				m_menu_file_recent_files   .Enabled = scene_active;
				m_menu_file_exit           .Enabled = true;

				m_menu_nav_reset_on_reload    .Enabled = true;
				m_menu_nav_reset_view_all     .Enabled = scene_active;
				m_menu_nav_reset_view_selected.Enabled = scene_active;
				m_menu_nav_reset_view_visible .Enabled = scene_active;
				m_menu_nav_view_xpos          .Enabled = scene_active;
				m_menu_nav_view_xneg          .Enabled = scene_active;
				m_menu_nav_view_ypos          .Enabled = scene_active;
				m_menu_nav_view_yneg          .Enabled = scene_active;
				m_menu_nav_view_zpos          .Enabled = scene_active;
				m_menu_nav_view_zneg          .Enabled = scene_active;
				m_menu_nav_view_xyz           .Enabled = scene_active;
				m_menu_nav_zoom_default       .Enabled = scene_active;
				m_menu_nav_zoom_aspect11      .Enabled = scene_active;
				m_menu_nav_zoom_lock_aspect   .Enabled = scene_active;
				m_menu_nav_align_none         .Enabled = scene_active;
				m_menu_nav_align_x            .Enabled = scene_active;
				m_menu_nav_align_y            .Enabled = scene_active;
				m_menu_nav_align_z            .Enabled = scene_active;
				m_menu_nav_align_current      .Enabled = scene_active;
				m_menu_nav_save_view          .Enabled = scene_active;
				m_menu_nav_saved_views_clear  .Enabled = true;
				m_menu_nav_camera             .Enabled = scene_active;

				m_menu_data_auto_refresh     .Enabled = true;
				m_menu_data_clear_all_scenes .Enabled = true;
				m_menu_data_clear_scene      .Enabled = scene_active;
				m_menu_data_create_demo_scene.Enabled = scene_active;
				m_menu_data_object_manager   .Enabled = scene_active;

				m_menu_rendering_show_focus        .Enabled = scene_active;
				m_menu_rendering_show_origin       .Enabled = scene_active;
				m_menu_rendering_show_selection    .Enabled = scene_active;
				m_menu_rendering_show_bounds       .Enabled = scene_active;
				m_menu_rendering_fillmode_solid    .Enabled = scene_active;
				m_menu_rendering_fillmode_wireframe.Enabled = scene_active;
				m_menu_rendering_fillmode_solidwire.Enabled = scene_active;
				m_menu_rendering_cullmode_none     .Enabled = scene_active;
				m_menu_rendering_cullmode_back     .Enabled = scene_active;
				m_menu_rendering_cullmode_front    .Enabled = scene_active;
				m_menu_rendering_orthographic      .Enabled = scene_active;
				m_menu_rendering_anim_controls     .Enabled = true;
				m_menu_rendering_lighting          .Enabled = scene_active;
			}

			// Set check marks next to visible things
			{
				m_menu_nav_reset_on_reload     .Checked = Settings.ResetOnLoad;
				m_menu_nav_zoom_lock_aspect    .Checked = scene?.LockAspect ?? false;
				m_menu_data_auto_refresh       .Checked = Model.AutoRefreshSources;
				m_menu_rendering_show_focus    .Checked = scene?.FocusPointVisible ?? false;
				m_menu_rendering_show_origin   .Checked = scene?.OriginPointVisible ?? false;
				m_menu_rendering_show_selection.Checked = scene?.SelectionBoxVisible ?? false;
				m_menu_rendering_show_bounds   .Checked = scene?.BBoxesVisible ?? false;
				m_menu_rendering_orthographic  .Checked = scene?.Camera.Orthographic ?? false;

				// Display the next fill mode
				m_menu_rendering_fillmode_solid    .Checked = scene?.Options.FillMode == View3d.EFillMode.Solid;
				m_menu_rendering_fillmode_wireframe.Checked = scene?.Options.FillMode == View3d.EFillMode.Wireframe;
				m_menu_rendering_fillmode_solidwire.Checked = scene?.Options.FillMode == View3d.EFillMode.SolidWire;
				m_menu_rendering_cullmode_none     .Checked = scene?.Options.CullMode == View3d.ECullMode.None;
				m_menu_rendering_cullmode_back     .Checked = scene?.Options.CullMode == View3d.ECullMode.Back;
				m_menu_rendering_cullmode_front    .Checked = scene?.Options.CullMode == View3d.ECullMode.Front;

				// Always on top
				m_menu_window_always_on_top.Checked = TopMost;
			}

			// Display the multi scene tool-bar when there are multiple scenes
			m_ts_multiview.Visible = Model.Scenes.Count > 1;

			// Animation
			m_tb_clock.Text = $"{AnimClock:N1}";

			// Set the title to show the last filepath of the active scene
			if (scene_active && scene.LastFilepath.HasValue())
				Text = $"LDraw - {scene.LastFilepath}";
			else
				Text = $"LDraw";

			// Update file loading progress
			UpdateProgress();
		}

		/// <summary>Create a new file and an editor to edit the file</summary>
		private void EditFile(string filepath, bool prompt)
		{
			// Prompt for a filepath if this is an 'open' operation
			if (prompt && !filepath.HasValue())
			{
				using (var dlg = new OpenFileDialog { Title = "Edit Ldr Script file", Filter = Util.FileDialogFilter("Ldr Script","*.ldr") })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					filepath = dlg.FileName;
				}
			}

			// Add a new script window
			var script = Model.AddNewScript(filepath);

			// If the script is not a temporary script, add to the recent files
			if (!script.IsTempScript)
				m_recent_files.Add(script.Filepath);
		}

		/// <summary>Add a file source</summary>
		private void OpenFile(string filepath, bool additional)
		{
			if (!filepath.HasValue())
			{
				var filter = Util.FileDialogFilter(
					"Supported Files"          , "*.ldr", "*.p3d", "*.3ds", "*.stl", "*.csv",
					"Ldr Script"               , "*.ldr",
					"Binary Model File"        , "*.p3d",
					"3D Studio Max Model File" , "*.3ds",
					"STL CAD Model File"       , "*.stl",
					"Comma Separated Values"   , "*.csv",
					"All Files"                , "*.*");

				using (var dlg = new OpenFileDialog { Title = "Open Ldr Script file", Filter = filter })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					filepath = dlg.FileName;
				}
			}

			// Add the file to the recent file history
			m_recent_files.Add(filepath);

			// Load the file source
			Model.CurrentScene.OpenFile(filepath, additional);

			UpdateUI();
		}

		/// <summary>Save a script UI to file</summary>
		private void SaveFile(bool save_as)
		{
			// Get the script to save (the active content)
			var script = m_dc.ActiveContent.Owner as ScriptUI;
			Debug.Assert(script != null, "Should not be able to save if a script isn't selected");

			// Write the script to disk at it's current location
			script.SaveFile();

			// If this is a 'save as' or the script is a temporary script,
			// prompt for a save location and copy the script to that location
			if (save_as || script.IsTempScript)
			{
				// Get the save location
				var filepath = (string)null;
				using (var dlg = new SaveFileDialog { Title = "Save Script", Filter = Util.FileDialogFilter("Script Files", "*.ldr") })
				{
					// Don't allow saving to the temporary script folder
					dlg.FileOk += (s,a) => a.Cancel = Path_.IsSubPath(Model.TempScriptsDirectory, dlg.FileName);
					if (dlg.ShowDialog(Model.Owner) != DialogResult.OK) return;
					filepath = dlg.FileName;
				}

				// Move the script to the new location
				if (Path_.FileExists(filepath)) File.Delete(filepath);
				File.Move(script.Filepath, filepath);

				// Update the script with the new location
				script.Filepath = filepath;
			}

			// If the script is not a temporary script, add to the recent files
			if (!script.IsTempScript)
				m_recent_files.Add(script.Filepath);
		}

		/// <summary>Display the options dialog</summary>
		private void ShowOptions()
		{
			using (var dlg = new SettingsUI(this, Settings))
				dlg.ShowDialog();
		}

		/// <summary>Clear all script sources</summary>
		private void ClearAllScenes()
		{
			Model.ClearAllScenes();
			Text = "LDraw";
			Invalidate();
		}

		/// <summary>Align the camera to an axis</summary>
		private void AlignCamera(v4 axis)
		{
			Model.CurrentScene.AlignCamera(axis);
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
				opt.DropDownOpening += (s,a) =>
				{
					opt.Enabled = Model.CurrentScene != null;
				};
				opt.Click += (s,a) =>
				{
					sv.Apply(Model.CurrentScene.Camera);
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
			Model.CurrentScene.CameraUI.Show(this);
		}

		/// <summary>Show the lighting UI</summary>
		private void ShowLightingUI()
		{
			Model.CurrentScene.LightingUI.Show(this);
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
				Bounds = WinFormsUtil.OnScreen(Settings.UI.WindowPosition);
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
			var ui = Model.AddNewScript("Example");
			ui.Editor.Text = Model.View3d.ExampleScript;
		}

		/// <summary>Update the state of the progress bar</summary>
		public void UpdateProgress()
		{
			if (Model == null) return;
			var progress = Model.AddFileProgress;

			// Show/Hide the progress indicators
			m_lbl_loading.Text = progress?.Filepath ?? string.Empty;
			m_pb_loading.Visible = progress != null;
			if (progress != null)
			{
				var finfo = new FileInfo(progress.Filepath);
				m_pb_loading.ValueFrac(finfo.Length != 0 ? Math_.Clamp((float)progress.FileOffset / finfo.Length, 0f, 1f) : 1f);
			}
		}

		private static class Res
		{
			public readonly static Image ImgStop    = Resources.media_stop;
			public readonly static Image ImgPlay    = Resources.media_play;
			public readonly static Image ImgStepBck = Resources.media_step_back;
			public readonly static Image ImgStepFwd = Resources.media_step_forward;
			public readonly static Image ImgPause   = Resources.media_pause;
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainUI));
			this.m_tsc = new Rylogic.Gui.WinForms.ToolStripContainer();
			this.m_ss = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_lbl_loading = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_pb_loading = new System.Windows.Forms.ToolStripProgressBar();
			this.m_lbl_pointer_location = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_new = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_edit_script = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator19 = new System.Windows.Forms.ToolStripSeparator();
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
			this.m_menu_data_auto_refresh = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator17 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_data_clear_all_scenes = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_data_clear_scene = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
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
			this.toolStripSeparator18 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_rendering_anim_controls = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_rendering_lighting = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_window = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_window_new_scene = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator15 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_window_always_on_top = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_window_example_script = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_window_about = new System.Windows.Forms.ToolStripMenuItem();
			this.m_ts_anim_controls = new System.Windows.Forms.ToolStrip();
			this.m_lbl_anim_controls = new System.Windows.Forms.ToolStripLabel();
			this.m_btn_reset = new System.Windows.Forms.ToolStripButton();
			this.m_btn_step_bck = new System.Windows.Forms.ToolStripButton();
			this.m_btn_step_fwd = new System.Windows.Forms.ToolStripButton();
			this.m_btn_run = new System.Windows.Forms.ToolStripButton();
			this.m_lbl_step_rate = new System.Windows.Forms.ToolStripLabel();
			this.m_tr_speed = new Rylogic.Gui.WinForms.ToolStripTrackBar();
			this.m_lbl_anim_clock = new System.Windows.Forms.ToolStripLabel();
			this.m_tb_clock = new System.Windows.Forms.ToolStripTextBox();
			this.m_ts_multiview = new System.Windows.Forms.ToolStrip();
			this.m_lbl_link_cameras = new System.Windows.Forms.ToolStripLabel();
			this.m_btn_link_camera_left_right = new System.Windows.Forms.ToolStripButton();
			this.m_btn_link_camera_up_down = new System.Windows.Forms.ToolStripButton();
			this.m_btn_link_camera_in_out = new System.Windows.Forms.ToolStripButton();
			this.m_btn_link_camera_rotate = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator16 = new System.Windows.Forms.ToolStripSeparator();
			this.m_lbl_link_axes = new System.Windows.Forms.ToolStripLabel();
			this.m_btn_link_xaxis = new System.Windows.Forms.ToolStripButton();
			this.m_btn_link_yaxis = new System.Windows.Forms.ToolStripButton();
			this.m_ts_tools = new System.Windows.Forms.ToolStrip();
			this.m_lbl_tools = new System.Windows.Forms.ToolStripLabel();
			this.m_btn_measure = new System.Windows.Forms.ToolStripButton();
			this.m_tsc.BottomToolStripPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_ss.SuspendLayout();
			this.m_menu.SuspendLayout();
			this.m_ts_anim_controls.SuspendLayout();
			this.m_ts_multiview.SuspendLayout();
			this.m_ts_tools.SuspendLayout();
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
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(663, 464);
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
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts_anim_controls);
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts_multiview);
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts_tools);
			// 
			// m_ss
			// 
			this.m_ss.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ss.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status,
            this.m_lbl_loading,
            this.m_pb_loading,
            this.m_lbl_pointer_location});
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
			// m_lbl_loading
			// 
			this.m_lbl_loading.Name = "m_lbl_loading";
			this.m_lbl_loading.Size = new System.Drawing.Size(398, 17);
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
			// m_lbl_pointer_location
			// 
			this.m_lbl_pointer_location.Name = "m_lbl_pointer_location";
			this.m_lbl_pointer_location.Size = new System.Drawing.Size(22, 17);
			this.m_lbl_pointer_location.Text = "0,0";
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
            this.toolStripSeparator19,
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
			// toolStripSeparator19
			// 
			this.toolStripSeparator19.Name = "toolStripSeparator19";
			this.toolStripSeparator19.Size = new System.Drawing.Size(233, 6);
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
            this.m_menu_data_auto_refresh,
            this.toolStripSeparator17,
            this.m_menu_data_clear_all_scenes,
            this.m_menu_data_clear_scene,
            this.toolStripSeparator8,
            this.m_menu_data_create_demo_scene,
            this.toolStripSeparator3,
            this.m_menu_data_object_manager});
			this.m_menu_data.Name = "m_menu_data";
			this.m_menu_data.Size = new System.Drawing.Size(43, 20);
			this.m_menu_data.Text = "&Data";
			// 
			// m_menu_data_auto_refresh
			// 
			this.m_menu_data_auto_refresh.Name = "m_menu_data_auto_refresh";
			this.m_menu_data_auto_refresh.Size = new System.Drawing.Size(180, 22);
			this.m_menu_data_auto_refresh.Text = "&Auto Refresh";
			// 
			// toolStripSeparator17
			// 
			this.toolStripSeparator17.Name = "toolStripSeparator17";
			this.toolStripSeparator17.Size = new System.Drawing.Size(177, 6);
			// 
			// m_menu_data_clear_all_scenes
			// 
			this.m_menu_data_clear_all_scenes.Name = "m_menu_data_clear_all_scenes";
			this.m_menu_data_clear_all_scenes.Size = new System.Drawing.Size(180, 22);
			this.m_menu_data_clear_all_scenes.Text = "&Clear All Scenes";
			// 
			// m_menu_data_clear_scene
			// 
			this.m_menu_data_clear_scene.Name = "m_menu_data_clear_scene";
			this.m_menu_data_clear_scene.Size = new System.Drawing.Size(180, 22);
			this.m_menu_data_clear_scene.Text = "&Clear Scene";
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(177, 6);
			// 
			// m_menu_data_create_demo_scene
			// 
			this.m_menu_data_create_demo_scene.Name = "m_menu_data_create_demo_scene";
			this.m_menu_data_create_demo_scene.Size = new System.Drawing.Size(180, 22);
			this.m_menu_data_create_demo_scene.Text = "&Create Demo Scene";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(177, 6);
			// 
			// m_menu_data_object_manager
			// 
			this.m_menu_data_object_manager.Name = "m_menu_data_object_manager";
			this.m_menu_data_object_manager.Size = new System.Drawing.Size(180, 22);
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
            this.toolStripSeparator18,
            this.m_menu_rendering_anim_controls,
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
			// toolStripSeparator18
			// 
			this.toolStripSeparator18.Name = "toolStripSeparator18";
			this.toolStripSeparator18.Size = new System.Drawing.Size(155, 6);
			// 
			// m_menu_rendering_anim_controls
			// 
			this.m_menu_rendering_anim_controls.Name = "m_menu_rendering_anim_controls";
			this.m_menu_rendering_anim_controls.Size = new System.Drawing.Size(158, 22);
			this.m_menu_rendering_anim_controls.Text = "&Animation";
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
            this.m_menu_window_new_scene,
            this.toolStripSeparator15,
            this.m_menu_window_always_on_top,
            this.toolStripSeparator7,
            this.m_menu_window_example_script,
            this.toolStripSeparator6,
            this.m_menu_window_about});
			this.m_menu_window.Name = "m_menu_window";
			this.m_menu_window.Size = new System.Drawing.Size(63, 20);
			this.m_menu_window.Text = "&Window";
			// 
			// m_menu_window_new_scene
			// 
			this.m_menu_window_new_scene.Name = "m_menu_window_new_scene";
			this.m_menu_window_new_scene.Size = new System.Drawing.Size(151, 22);
			this.m_menu_window_new_scene.Text = "&New Scene";
			// 
			// toolStripSeparator15
			// 
			this.toolStripSeparator15.Name = "toolStripSeparator15";
			this.toolStripSeparator15.Size = new System.Drawing.Size(148, 6);
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
			// m_ts_anim_controls
			// 
			this.m_ts_anim_controls.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts_anim_controls.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.m_ts_anim_controls.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_lbl_anim_controls,
            this.m_btn_reset,
            this.m_btn_step_bck,
            this.m_btn_step_fwd,
            this.m_btn_run,
            this.m_lbl_step_rate,
            this.m_tr_speed,
            this.m_lbl_anim_clock,
            this.m_tb_clock});
			this.m_ts_anim_controls.Location = new System.Drawing.Point(3, 24);
			this.m_ts_anim_controls.Name = "m_ts_anim_controls";
			this.m_ts_anim_controls.Size = new System.Drawing.Size(385, 27);
			this.m_ts_anim_controls.TabIndex = 2;
			// 
			// m_lbl_anim_controls
			// 
			this.m_lbl_anim_controls.Name = "m_lbl_anim_controls";
			this.m_lbl_anim_controls.Size = new System.Drawing.Size(66, 24);
			this.m_lbl_anim_controls.Text = "Animation:";
			// 
			// m_btn_reset
			// 
			this.m_btn_reset.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_reset.Image = global::LDraw.Resources.media_stop;
			this.m_btn_reset.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_reset.Name = "m_btn_reset";
			this.m_btn_reset.Size = new System.Drawing.Size(24, 24);
			this.m_btn_reset.Text = "Stop";
			this.m_btn_reset.ToolTipText = "Stop and reset the animation clock to zero";
			// 
			// m_btn_step_bck
			// 
			this.m_btn_step_bck.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_step_bck.Image = global::LDraw.Resources.media_step_back;
			this.m_btn_step_bck.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_step_bck.Name = "m_btn_step_bck";
			this.m_btn_step_bck.Size = new System.Drawing.Size(24, 24);
			this.m_btn_step_bck.Text = "Step Back";
			this.m_btn_step_bck.ToolTipText = "Step backwards one frame";
			// 
			// m_btn_step_fwd
			// 
			this.m_btn_step_fwd.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_step_fwd.Image = global::LDraw.Resources.media_step_forward;
			this.m_btn_step_fwd.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_step_fwd.Name = "m_btn_step_fwd";
			this.m_btn_step_fwd.Size = new System.Drawing.Size(24, 24);
			this.m_btn_step_fwd.Text = "Step Forward";
			this.m_btn_step_fwd.ToolTipText = "Step forward by one frame";
			// 
			// m_btn_run
			// 
			this.m_btn_run.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_run.Image = global::LDraw.Resources.media_play;
			this.m_btn_run.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_run.Name = "m_btn_run";
			this.m_btn_run.Size = new System.Drawing.Size(24, 24);
			this.m_btn_run.Text = "Run";
			this.m_btn_run.ToolTipText = "Run the animations";
			// 
			// m_lbl_step_rate
			// 
			this.m_lbl_step_rate.Name = "m_lbl_step_rate";
			this.m_lbl_step_rate.Size = new System.Drawing.Size(42, 24);
			this.m_lbl_step_rate.Text = "Speed:";
			// 
			// m_tr_speed
			// 
			this.m_tr_speed.Name = "m_tr_speed";
			this.m_tr_speed.Size = new System.Drawing.Size(80, 24);
			this.m_tr_speed.Value = 0;
			// 
			// m_lbl_anim_clock
			// 
			this.m_lbl_anim_clock.Name = "m_lbl_anim_clock";
			this.m_lbl_anim_clock.Size = new System.Drawing.Size(37, 24);
			this.m_lbl_anim_clock.Text = "Time:";
			// 
			// m_tb_clock
			// 
			this.m_tb_clock.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_clock.Name = "m_tb_clock";
			this.m_tb_clock.ReadOnly = true;
			this.m_tb_clock.Size = new System.Drawing.Size(50, 27);
			this.m_tb_clock.Text = "100.0";
			// 
			// m_ts_multiview
			// 
			this.m_ts_multiview.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts_multiview.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.m_ts_multiview.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_lbl_link_cameras,
            this.m_btn_link_camera_left_right,
            this.m_btn_link_camera_up_down,
            this.m_btn_link_camera_in_out,
            this.m_btn_link_camera_rotate,
            this.toolStripSeparator16,
            this.m_lbl_link_axes,
            this.m_btn_link_xaxis,
            this.m_btn_link_yaxis});
			this.m_ts_multiview.Location = new System.Drawing.Point(3, 51);
			this.m_ts_multiview.Name = "m_ts_multiview";
			this.m_ts_multiview.Size = new System.Drawing.Size(302, 27);
			this.m_ts_multiview.TabIndex = 1;
			// 
			// m_lbl_link_cameras
			// 
			this.m_lbl_link_cameras.Name = "m_lbl_link_cameras";
			this.m_lbl_link_cameras.Size = new System.Drawing.Size(81, 24);
			this.m_lbl_link_cameras.Text = "Link Cameras:";
			// 
			// m_btn_link_camera_left_right
			// 
			this.m_btn_link_camera_left_right.CheckOnClick = true;
			this.m_btn_link_camera_left_right.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_link_camera_left_right.Image = global::LDraw.Resources.green_left_right;
			this.m_btn_link_camera_left_right.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_link_camera_left_right.Name = "m_btn_link_camera_left_right";
			this.m_btn_link_camera_left_right.Size = new System.Drawing.Size(24, 24);
			this.m_btn_link_camera_left_right.ToolTipText = "Apply camera navigation to all views";
			// 
			// m_btn_link_camera_up_down
			// 
			this.m_btn_link_camera_up_down.CheckOnClick = true;
			this.m_btn_link_camera_up_down.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_link_camera_up_down.Image = global::LDraw.Resources.green_up_down;
			this.m_btn_link_camera_up_down.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_link_camera_up_down.Name = "m_btn_link_camera_up_down";
			this.m_btn_link_camera_up_down.Size = new System.Drawing.Size(24, 24);
			// 
			// m_btn_link_camera_in_out
			// 
			this.m_btn_link_camera_in_out.CheckOnClick = true;
			this.m_btn_link_camera_in_out.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_link_camera_in_out.Image = global::LDraw.Resources.green_in_out;
			this.m_btn_link_camera_in_out.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_link_camera_in_out.Name = "m_btn_link_camera_in_out";
			this.m_btn_link_camera_in_out.Size = new System.Drawing.Size(24, 24);
			// 
			// m_btn_link_camera_rotate
			// 
			this.m_btn_link_camera_rotate.CheckOnClick = true;
			this.m_btn_link_camera_rotate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_link_camera_rotate.Image = global::LDraw.Resources.green_rotate;
			this.m_btn_link_camera_rotate.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_link_camera_rotate.Name = "m_btn_link_camera_rotate";
			this.m_btn_link_camera_rotate.Size = new System.Drawing.Size(24, 24);
			// 
			// toolStripSeparator16
			// 
			this.toolStripSeparator16.Name = "toolStripSeparator16";
			this.toolStripSeparator16.Size = new System.Drawing.Size(6, 27);
			// 
			// m_lbl_link_axes
			// 
			this.m_lbl_link_axes.Name = "m_lbl_link_axes";
			this.m_lbl_link_axes.Size = new System.Drawing.Size(59, 24);
			this.m_lbl_link_axes.Text = "Link Axes:";
			// 
			// m_btn_link_xaxis
			// 
			this.m_btn_link_xaxis.CheckOnClick = true;
			this.m_btn_link_xaxis.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_link_xaxis.Image = global::LDraw.Resources.XAxis;
			this.m_btn_link_xaxis.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_link_xaxis.Name = "m_btn_link_xaxis";
			this.m_btn_link_xaxis.Size = new System.Drawing.Size(24, 24);
			this.m_btn_link_xaxis.Text = "X";
			// 
			// m_btn_link_yaxis
			// 
			this.m_btn_link_yaxis.CheckOnClick = true;
			this.m_btn_link_yaxis.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_link_yaxis.Image = global::LDraw.Resources.YAxis;
			this.m_btn_link_yaxis.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_link_yaxis.Name = "m_btn_link_yaxis";
			this.m_btn_link_yaxis.Size = new System.Drawing.Size(24, 24);
			this.m_btn_link_yaxis.Text = "toolStripButton2";
			// 
			// m_ts_tools
			// 
			this.m_ts_tools.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts_tools.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_lbl_tools,
            this.m_btn_measure});
			this.m_ts_tools.Location = new System.Drawing.Point(3, 78);
			this.m_ts_tools.Name = "m_ts_tools";
			this.m_ts_tools.Size = new System.Drawing.Size(73, 25);
			this.m_ts_tools.TabIndex = 3;
			// 
			// m_lbl_tools
			// 
			this.m_lbl_tools.Name = "m_lbl_tools";
			this.m_lbl_tools.Size = new System.Drawing.Size(38, 22);
			this.m_lbl_tools.Text = "Tools:";
			// 
			// m_btn_measure
			// 
			this.m_btn_measure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_measure.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_measure.Image")));
			this.m_btn_measure.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_measure.Name = "m_btn_measure";
			this.m_btn_measure.Size = new System.Drawing.Size(23, 22);
			this.m_btn_measure.Text = "Measure Tool";
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
			this.m_ts_anim_controls.ResumeLayout(false);
			this.m_ts_anim_controls.PerformLayout();
			this.m_ts_multiview.ResumeLayout(false);
			this.m_ts_multiview.PerformLayout();
			this.m_ts_tools.ResumeLayout(false);
			this.m_ts_tools.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
