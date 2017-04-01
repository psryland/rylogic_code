using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.common;
using pr.container;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;
using pr.win32;
using Timer = System.Windows.Forms.Timer;

namespace LDraw
{
	public class Model :IDisposable
	{
		public Model(MainUI main_ui)
		{
			Owner        = main_ui;
			View3d       = new View3d();
			IncludePaths = new List<string>();
			ContextIds   = new HashSet<Guid>();
			SavedViews   = new List<SavedView>();
			Scenes       = new BindingListEx<SceneUI>();
			Log          = new LogUI(this);

			// Add the default scene
			CurrentScene = Scenes.Add2(new SceneUI("Scene", this));

			// Apply initial settings
			AutoRefreshSources = Settings.AutoRefresh;
			LinkSceneCameras = Settings.LinkSceneCameras;
		}
		public void Dispose()
		{
			Log = null;
			Scenes = null;
			View3d = null;
		}

		/// <summary>The UI that created this model</summary>
		public MainUI Owner
		{
			get;
			private set;
		}

		/// <summary>App settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get { return Owner.Settings; }
		}

		/// <summary>A view3d context reference that lives for the lifetime of the application</summary>
		public View3d View3d
		{
			[DebuggerStepThrough] get { return m_view3d; }
			private set
			{
				if (m_view3d == value) return;
				if (m_view3d != null)
				{
					View3d.AddFileProgress -= HandleAddFileProgress;
					View3d.OnSourcesChanged -= HandleSourcesChanged;
					View3d.Error -= ReportError;
					Util.Dispose(ref m_view3d);
				}
				m_view3d = value;
				if (m_view3d != null)
				{
					View3d.Error += ReportError;
					View3d.OnSourcesChanged += HandleSourcesChanged;
					View3d.AddFileProgress += HandleAddFileProgress;
				}
			}
		}
		private View3d m_view3d;

		/// <summary>The 3d scene views</summary>
		public BindingListEx<SceneUI> Scenes
		{
			[DebuggerStepThrough] get { return m_scenes; }
			private set
			{
				if (m_scenes == value) return;
				if (m_scenes != null)
				{
					m_scenes.ListChanging -= HandleScenesListChanging;
				}
				m_scenes = value;
				if (m_scenes != null)
				{
					m_scenes.ListChanging += HandleScenesListChanging;
				}
			}
		}
		private BindingListEx<SceneUI> m_scenes;
		private void HandleScenesListChanging(object sender, ListChgEventArgs<SceneUI> e)
		{
			var scene = e.Item;
			switch (e.ChangeType)
			{
			case ListChg.ItemRemoved:
				{
					Owner.DockContainer.Remove(scene);
					scene.Window.MouseNavigating -= HandleMouseNavigating;
					scene.Options.PropertyChanged -= Owner.UpdateUI;
					Util.Dispose(ref scene);
					break;
				}
			case ListChg.ItemAdded:
				{
					// Determine a dock location from the current scene
					var dloc = new DockContainer.DockLocation();
					if (CurrentScene != null)
					{
						var loc = CurrentScene.DockControl.CurrentDockLocation;
						dloc.Address = loc.Address.Concat(CurrentScene.Bounds.Aspect() > 1 ? EDockSite.Right : EDockSite.Bottom).ToArray();
					}
					scene.Options.PropertyChanged += Owner.UpdateUI;
					scene.Window.MouseNavigating += HandleMouseNavigating;
					Owner.DockContainer.Add(scene, dloc);
					break;
				}
			}
		}

		/// <summary>The scene with input focus (or the last to have input focus). Always non-null</summary>
		public SceneUI CurrentScene
		{
			[DebuggerStepThrough] get { return m_current_scene; }
			set
			{
				if (m_current_scene == value) return;
				if (m_current_scene != null)
				{
				}
				Debug.Assert(value != null);
				m_current_scene = value;
				if (m_current_scene != null)
				{
				}
			}
		}
		private SceneUI m_current_scene;

		/// <summary>The error log</summary>
		public LogUI Log
		{
			[DebuggerStepThrough] get { return m_log; }
			private set
			{
				if (m_log == value) return;
				if (m_log != null)
				{
					Owner.DockContainer.Remove(m_log);
					Util.Dispose(ref m_log);
				}
				m_log = value;
				if (m_log != null)
				{
					Owner.DockContainer.Add(m_log);
				}
			}
		}
		private LogUI m_log;

		/// <summary>Application include paths</summary>
		public List<string> IncludePaths
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Context Ids of loaded script sources</summary>
		public HashSet<Guid> ContextIds
		{
			get;
			private set;
		}

		/// <summary>Saved camera positions</summary>
		public List<SavedView> SavedViews
		{
			get;
			private set;
		}

		/// <summary>Get/Set whether mouse navigation effects all scenes or just the active one</summary>
		public bool LinkSceneCameras
		{
			get { return m_link_scene_cameras; }
			set
			{
				if (m_link_scene_cameras == value) return;
				Settings.LinkSceneCameras = value;
				m_link_scene_cameras = value;
			}
		}
		private bool m_link_scene_cameras;

		/// <summary>Return the settings for a scene with the given name</summary>
		public SceneSettings SceneSettings(string name)
		{
			var ss = Settings.Scenes.FirstOrDefault(x => x.Name == name);
			if (ss != null)
				return ss;

			ss = new SceneSettings(name);
			Settings.Scenes = Util.AddToHistoryList(Settings.Scenes, ss, 20, cmp:(l,r) => l.Name == r.Name);
			return ss;
		}

		/// <summary>Handler for errors generated by View3d</summary>
		public void ReportError(object sender, View3d.ErrorEventArgs arg)
		{
			// Handle background threads reporting errors
			if (Owner.InvokeRequired) Owner.BeginInvoke(() => ReportError(sender, arg));
			else Log.AddErrorMessage(arg.Message);
		}

		/// <summary>Save the current camera position and view</summary>
		public void SaveView()
		{
			// Prompt for a name
			using (var dlg = new PromptForm { Title = "View Name:", Value = "View{0}".Fmt(SavedViews.Count + 1) })
			{
				dlg.ValueCtrl.SelectAll();
				if (dlg.ShowDialog(Owner) != DialogResult.OK) return;
				SavedViews.Add(new SavedView(dlg.Value, CurrentScene.Camera));
			}
		}

		/// <summary>Add a new scene</summary>
		public void AddNewScene()
		{
			var highest = int_.TryParse(Scenes.Max(x => x.SceneName.SubstringRegex(@"Scene(\d*)").FirstOrDefault()));
			var name = highest != null ? "Scene{0}".Fmt(highest.Value + 1) : "Scene";
			Scenes.Add(new SceneUI(name, this));
		}

		/// <summary>Clear all scenes</summary>
		public void ClearAllScenes()
		{
			// Remove and delete all objects (excluding focus points, selection boxes, etc)
			foreach (var id in ContextIds)
				View3d.DeleteAllObjects(id);

			// Remove all script sources
			View3d.ClearScriptSources();

			// Reset the list script source objects
			ContextIds.Clear();
		}

		/// <summary>Add a demo scene to the scene</summary>
		public void CreateDemoScene()
		{
			var scene = Scenes.Add2(new SceneUI("Demo", this));
			var id = scene.Window.CreateDemoScene();
			scene.ContextIds.Add(id);
		}

		/// <summary>Enable/Disable auto refreshing of script sources</summary>
		public bool AutoRefreshSources
		{
			get { return m_timer_refresh != null; }
			set
			{
				if (AutoRefreshSources == value) return;
				if (m_timer_refresh != null)
				{
					m_timer_refresh.Tick -= View3d.CheckForChangedSources;
					Util.Dispose(ref m_timer_refresh);
				}
				m_timer_refresh = value ? new Timer { Interval = 500, Enabled = true } : null;
				if (m_timer_refresh != null)
				{
					m_timer_refresh.Tick += View3d.CheckForChangedSources;
				}
			}
		}
		private Timer m_timer_refresh;

		/// <summary>Handle notification that the script sources are about to be reloaded</summary>
		private void HandleSourcesChanged(object sender, View3d.SourcesChangedEventArgs e)
		{
			// Just prior to reloading sources
			if (e.Before && Settings.UI.ClearErrorLogOnReload)
				Log.Clear();
		}

		/// <summary>Handle progress updates during file parsing</summary>
		private void HandleAddFileProgress(object sender, View3d.AddFileProgressEventArgs e)
		{
			// Warning: called from a background thread context

			// Ignore if there is no file (i.e. string sources)
			if (!e.Filepath.HasValue())
				return;

			// Marshal to the main thread and update progress
			var complete = e.Complete;
			var progress = new AddFileProgressData(e.ContextId, e.Filepath, e.FileOffset);
			Owner.BeginInvoke(() =>
			{
				// Only update with info from the same file
				if (AddFileProgress != null && AddFileProgress.ContextId != progress.ContextId)
					return;

				// If progress is complete, clear the progress data
				AddFileProgress = !complete ? progress : null;
				Owner.UpdateProgress();
			});
		}

		/// <summary>Handle mouse navigation</summary>
		private void HandleMouseNavigating(object sender, View3d.Window.MouseNavigateEventArgs e)
		{
			if (!LinkSceneCameras)
				return;

			if (m_in_handle_mouse_navigating != 0) return;
			using (Scope.Create(() => ++m_in_handle_mouse_navigating, () => --m_in_handle_mouse_navigating))
			{
				foreach (var scene in Scenes.Where(x => x.Window != sender))
				{
					// Replicate navigation commands in the other scenes
					if (!e.ZNavigation)
						scene.Window.MouseNavigate(e.Point, e.NavOp, e.NavBegOrEnd);
					else
						scene.Window.MouseNavigateZ(e.Point, e.Delta, e.AlongRay);

					scene.Invalidate();
				}
			}
		}
		private int m_in_handle_mouse_navigating;

		public AddFileProgressData AddFileProgress { get; private set; }
		public class AddFileProgressData
		{
			public AddFileProgressData(Guid context_id, string filepath, long file_offset)
			{
				ContextId  = context_id;
				Filepath   = filepath;
				FileOffset = file_offset;
			}

			/// <summary>The context id for the 'AddFile' group</summary>
			public Guid ContextId{ get; private set; }

			/// <summary>The file being parsed</summary>
			public string Filepath { get; private set; }

			/// <summary>How far through the file being parsed we are</summary>
			public long FileOffset { get; private set; }
		}

		public class SavedView
		{
			public SavedView(string name, View3d.CameraControls camera)
			{
				Name         = name;
				C2W          = camera.O2W;
				FocusDist    = camera.FocusDist;
				AlignAxis    = camera.AlignAxis;
				Aspect       = camera.Aspect;
				FovX         = camera.FovX;
				FovY         = camera.FovY;
				Orthographic = camera.Orthographic;
			}

			public string Name         { get; set; }
			public m4x4   C2W          { get; private set; }
			public float  FocusDist    { get; private set; }
			public v4     AlignAxis    { get; private set; }
			public float  Aspect       { get; private set; }
			public float  FovX         { get; private set; }
			public float  FovY         { get; private set; }
			public bool   Orthographic { get; private set; }

			public void Apply(View3d.CameraControls camera)
			{
				camera.FocusDist    = FocusDist;
				camera.AlignAxis    = AlignAxis;
				camera.Aspect       = Aspect;
				camera.FovX         = FovX;
				camera.FovY         = FovY;
				camera.Orthographic = Orthographic;
				camera.O2W          = C2W;
				camera.Commit();
			}
		}
	}
}
