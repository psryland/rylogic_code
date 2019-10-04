using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Threading;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;
using Rylogic.Utility;
using Util = Rylogic.Utility.Util;

namespace LDraw
{
	public class Model :IDisposable
	{
		public Model(MainUI main_ui)
		{
			Owner        = main_ui;
			Dispatcher   = Dispatcher.CurrentDispatcher;
			View3d       = View3d.Create();
			IncludePaths = new List<string>();
			SavedViews   = new List<SavedView>();
			Scenes       = new BindingListEx<SceneUI>{ PerItem = true };
			Scripts      = new BindingListEx<ScriptUI>{ PerItem = true };
			RPCService   = new GrpcService(this);
			Log          = new LogUI(this);

			// Apply initial settings
			Directory.CreateDirectory(TempScriptsDirectory);
			AutoRefreshSources = Settings.AutoRefresh;
			LinkCameras = Settings.LinkCameras;
		}
		public void Dispose()
		{
			AutoRefreshSources = false;
			CurrentScript = null;
			CurrentScene = null;
			Log = null;
			RPCService = null;
			Scripts = null;
			Scenes = null;
			View3d = null;
		}

		/// <summary>Main thread dispatcher</summary>
		public Dispatcher Dispatcher { get; }

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

		/// <summary>The location for temporary script files</summary>
		public string TempScriptsDirectory
		{
			get { return Path_.CombinePath(MainUI.StartupOptions.UserDataDir, "Temporary Scripts"); }
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

				// Handlers
				void HandleSourcesChanged(object sender, View3d.SourcesChangedEventArgs e)
				{
					// Just prior to reloading sources
					if (e.Before && Settings.UI.ClearErrorLogOnReload)
						Log.Clear();
				}
				void HandleAddFileProgress(object sender, View3d.AddFileProgressEventArgs e) // Worker thread context
				{
					// Ignore if there is no file (i.e. string sources)
					if (!e.Filepath.HasValue())
						return;

					// Marshal to the main thread and update progress
					var complete = e.Complete;
					var progress = new AddFileProgressData(e.ContextId, e.Filepath, e.FileOffset);
					Dispatcher.BeginInvoke(() =>
					{
						// Only update with info from the same file
						if (AddFileProgress != null && AddFileProgress.ContextId != progress.ContextId)
							return;

						// If progress is complete, clear the progress data
						AddFileProgress = !complete ? progress : null;
						Owner.UpdateProgress();
					});
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
					m_scenes.Clear();
					m_scenes.ListChanging -= HandleScenesListChanging;
				}
				m_scenes = value;
				if (m_scenes != null)
				{
					m_scenes.ListChanging += HandleScenesListChanging;
				}

				// Handlers
				void HandleScenesListChanging(object sender, ListChgEventArgs<SceneUI> e)
				{
					var scene = e.Item;
					switch (e.ChangeType)
					{
					case ListChg.ItemRemoved:
						{
							if (CurrentScene == scene)
								CurrentScene = Scenes.Except(scene).FirstOrDefault();
							if (Scripts != null)
								foreach (var script in Scripts.Where(x => x.Scene == scene))
									script.Scene = null;

							Owner.DockContainer.Remove(scene);
							scene.Options.PropertyChanged -= Owner.UpdateUI;
							Util.Dispose(ref scene);
							Owner.UpdateUI();
							break;
						}
					case ListChg.ItemAdded:
						{
							// Determine a dock location from the current scene
							var dloc =
								CurrentScene?.DockControl.CurrentDockLocation ??
								new DockContainer.DockLocation();

							scene.Options.PropertyChanged += Owner.UpdateUI;
							Owner.DockContainer.Add(scene, dloc);
							Owner.UpdateUI();
							break;
						}
					}
				}
			}
		}
		private BindingListEx<SceneUI> m_scenes;

		/// <summary>The script windows</summary>
		public BindingListEx<ScriptUI> Scripts
		{
			get { return m_scripts; }
			private set
			{
				if (m_scripts == value) return;
				if (m_scripts != null)
				{
					Util.DisposeAll(m_scripts);
					m_scripts.Clear();
					m_scripts.ListChanging -= HandleScriptListChanging;
				}
				m_scripts = value;
				if (m_scripts != null)
				{
					m_scripts.ListChanging += HandleScriptListChanging;
				}

				// Handlers
				void HandleScriptListChanging(object sender, ListChgEventArgs<ScriptUI> e)
				{
					var script = e.Item;
					switch (e.ChangeType)
					{
					case ListChg.ItemRemoved:
						{
							if (CurrentScript == script)
								CurrentScript = Scripts.Except(script).FirstOrDefault();

							Owner.DockContainer.Remove(script);
							Util.Dispose(ref script);
							Owner.UpdateUI();
							break;
						}
					case ListChg.ItemAdded:
						{
							// Determine a dock location from the current script
							var dloc =
								CurrentScript?.DockControl.CurrentDockLocation ??
								new DockContainer.DockLocation();

							Owner.DockContainer.Add(script, dloc);
							script.DockControl.IsActiveContent = true;
							Owner.UpdateUI();
							break;
						}
					}
				}
			}
		}
		private BindingListEx<ScriptUI> m_scripts;

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

		/// <summary>The scene with input focus (or the last to have input focus)</summary>
		public SceneUI CurrentScene
		{
			[DebuggerStepThrough] get { return m_current_scene; }
			set
			{
				if (m_current_scene == value) return;
				if (m_current_scene != null)
				{
					m_current_scene.Window.MouseNavigating  -= HandleMouseNavigating;
					m_current_scene.ChartMoved              -= HandleChartMoved;
					m_current_scene.CrossHairMoved          -= HandleCrossHairMoved;
				}
				m_current_scene = value;
				if (m_current_scene != null)
				{
					m_current_scene.Window.MouseNavigating  += HandleMouseNavigating;
					m_current_scene.ChartMoved              += HandleChartMoved;
					m_current_scene.CrossHairMoved          += HandleCrossHairMoved;

					LinkSceneAxes();
					LinkSceneCrossHairs();
				}

				// Handlers
				void HandleMouseNavigating(object sender, View3d.Window.MouseNavigateEventArgs e)
				{
					LinkSceneCameras(e);
				}
				void HandleChartMoved(object sender, ChartControl.ChartMovedEventArgs e)
				{
					LinkSceneAxes();
				}
				void HandleCrossHairMoved(object sender, EventArgs e)
				{
					LinkSceneCrossHairs();
				}
			}
		}
		private SceneUI m_current_scene;

		/// <summary>The script with input focus (or the last to have input focus)</summary>
		public ScriptUI CurrentScript
		{
			get { return m_current_script; }
			set
			{
				if (m_current_script == value) return;
				m_current_script = value;
			}
		}
		private ScriptUI m_current_script;

		/// <summary>Application include paths</summary>
		public List<string> IncludePaths
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Saved camera positions</summary>
		public List<SavedView> SavedViews
		{
			get;
			private set;
		}

		/// <summary>Get/Set whether mouse navigation effects all scenes or just the active one</summary>
		public ELinkCameras LinkCameras
		{
			get { return m_link_cameras; }
			set
			{
				if (m_link_cameras == value) return;
				Settings.LinkCameras = value;
				m_link_cameras = value;
			}
		}
		private ELinkCameras m_link_cameras;

		/// <summary>Get/Set whether the X/Y axes of the scenes are linked</summary>
		public ELinkAxes LinkAxes
		{
			get { return m_link_axes; }
			set
			{
				if (m_link_axes == value) return;
				Settings.LinkAxes = value;
				m_link_axes = value;
				LinkSceneAxes();
			}
		}
		private ELinkAxes m_link_axes;

		/// <summary>Return the settings for a scene with the given name</summary>
		public SceneSettings SceneSettings(string name, SceneSettings def = null)
		{
			var ss = Settings.Scenes.FirstOrDefault(x => x.Name == name);
			if (ss != null)
				return ss;

			ss = def != null ? new SceneSettings(name, def) : new SceneSettings(name);
			Settings.Scenes = Util.AddToHistoryList(Settings.Scenes, ss, 20, cmp:(l,r) => l.Name == r.Name);
			return ss;
		}

		/// <summary>Handler for errors generated by View3d</summary>
		public void ReportError(object sender, MessageEventArgs arg)
		{
			// Handle background threads reporting errors
			if (Owner.InvokeRequired) Dispatcher.BeginInvoke(() => ReportError(sender, arg));
			else Log.AddErrorMessage(arg.Message);
		}

		/// <summary>Save the current camera position and view</summary>
		public void SaveView()
		{
			if (CurrentScene == null)
				throw new Exception("No scene to save view");

			// Prompt for a name
			using (var dlg = new PromptUI { Title = "View Name:", Value = $"View{SavedViews.Count+1}" })
			{
				dlg.ValueCtrl.SelectAll();
				if (dlg.ShowDialog(Owner) != DialogResult.OK) return;
				SavedViews.Add(new SavedView((string)dlg.Value, CurrentScene.Camera));
			}
		}

		/// <summary>Add a new scene</summary>
		public SceneUI AddNewScene(string name = SceneUI.DefaultName)
		{
			if (name == SceneUI.DefaultName && Scenes.Count >= 1)
			{
				var highest = int_.TryParse(Scenes.Max(x => x.SceneName.SubstringRegex($@"{SceneUI.DefaultName}(\d*)").FirstOrDefault())) ?? 1;
				name = $"{SceneUI.DefaultName}{highest + 1}";
			}
			return Scenes.Add2(new SceneUI(name, this));
		}

		/// <summary>Add a new script</summary>
		public ScriptUI AddNewScript(string name = ScriptUI.DefaultName, string filepath = null)
		{
			if (name == ScriptUI.DefaultName && Scripts.Count >= 1)
			{
				var highest = int_.TryParse(Scripts.Max(x => x.ScriptName.SubstringRegex($@"{ScriptUI.DefaultName}(\d*)").FirstOrDefault())) ?? 1;
				name = $"{ScriptUI.DefaultName}{highest + 1}";
			}
			return Scripts.Add2(new ScriptUI(name, this, filepath));
		}

		/// <summary>Clear all scenes</summary>
		public void ClearAllScenes()
		{
			// Reset the context ids lists in each scene
			foreach (var scene in Scenes)
				scene.Clear(delete_objects:true);
		}

		/// <summary>Add a demo scene to the scene</summary>
		public void CreateDemoScene()
		{
			var scene = AddNewScene("Demo");
			scene.Window.CreateDemoScene();
			scene.DockControl.IsActiveContent = true;
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
					m_timer_refresh.Stop();
				}
				m_timer_refresh = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Normal, View3d.CheckForChangedSources, Dispatcher.CurrentDispatcher) : null;
				if (m_timer_refresh != null)
				{
					m_timer_refresh.Start();
				}
			}
		}
		private DispatcherTimer m_timer_refresh;

		/// <summary>Apply the current scene axes to all other scenes if linked</summary>
		private void LinkSceneAxes()
		{
			if (LinkAxes == ELinkAxes.None)
				return;

			// Set the axes ranges in the other scenes
			var scene = CurrentScene;
			if (scene != null)
			{
				foreach (var scn in Scenes.Except(scene).ToArray())
				{
					var update = false;
					if (LinkAxes.HasFlag(ELinkAxes.XAxis))
					{
						scn.XAxis.Range = scene.XAxis.Range;
						update = true;
					}
					if (LinkAxes.HasFlag(ELinkAxes.YAxis))
					{
						scn.YAxis.Range = scene.YAxis.Range;
						update = true;
					}
					if (update)
					{
						scn.SetCameraFromRange();
						scn.Invalidate();
						scn.Update();
					}
				}
			}
		}

		/// <summary>Apply navigation operations to all scenes</summary>
		private void LinkSceneCameras(View3d.Window.MouseNavigateEventArgs e)
		{
			if (LinkCameras == ELinkCameras.None)
				return;

			// Replicate navigation commands in the other scenes
			var scene = CurrentScene;
			if (scene != null)
			{
				foreach (var scn in Scenes.Except(scene).ToArray())
				{
					// Use the camera lock to limit motion
					using (Scope.Create(() => scn.Camera.LockMask, m => scn.Camera.LockMask = m))
					{
						var lock_mask = View3d.ECameraLockMask.All;
						if (LinkCameras.HasFlag(ELinkCameras.LeftRight) && !LinkAxes.HasFlag(ELinkAxes.XAxis)) lock_mask = Bit.SetBits(lock_mask, View3d.ECameraLockMask.TransX, false);
						if (LinkCameras.HasFlag(ELinkCameras.UpDown   ) && !LinkAxes.HasFlag(ELinkAxes.YAxis)) lock_mask = Bit.SetBits(lock_mask, View3d.ECameraLockMask.TransY, false);
						if (LinkCameras.HasFlag(ELinkCameras.InOut    )) lock_mask = Bit.SetBits(lock_mask, View3d.ECameraLockMask.TransZ|View3d.ECameraLockMask.Zoom, false);
						if (LinkCameras.HasFlag(ELinkCameras.Rotate   )) lock_mask = Bit.SetBits(lock_mask, View3d.ECameraLockMask.RotX|View3d.ECameraLockMask.RotY|View3d.ECameraLockMask.RotZ, false);
						scn.Camera.LockMask = lock_mask;

						// Replicate the navigation command
						if (!e.ZNavigation)
							scn.Window.MouseNavigate(e.Point, e.Btns, e.NavOp, e.NavBegOrEnd);
						else
							scn.Window.MouseNavigateZ(e.Point, e.Btns, e.Delta, e.AlongRay);

						scn.SetRangeFromCamera();
						scn.Invalidate();
						scn.Update();
					}
				}
			}
		}

		/// <summary>Synchronise the cross hairs in all scenes</summary>
		private void LinkSceneCrossHairs()
		{
			// Position the cross hairs in other scenes
			var scene = CurrentScene;
			if (scene != null)
			{
				foreach (var scn in Scenes.Except(scene).ToArray())
				{
					scn.CrossHairVisible = scene.CrossHairVisible;
					if (scn.CrossHairVisible)
						scn.CrossHairLocation = scene.CrossHairLocation;
				}
			}
		}

		/// <summary>The RPC API for LDraw</summary>
		public GrpcService RPCService
		{
			get { return m_rpc;  }
			private set
			{
				if (m_rpc == value) return;
				Util.Dispose(ref m_rpc);
				m_rpc = value;
			}
		}
		private GrpcService m_rpc;

		/// <summary>File loading progress data</summary>
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

		/// <summary>Saved camera views</summary>
		public class SavedView
		{
			public SavedView(string name, View3d.Camera camera)
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

			public void Apply(View3d.Camera camera)
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
