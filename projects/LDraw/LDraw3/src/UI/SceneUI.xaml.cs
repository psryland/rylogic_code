using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LDraw.Dialogs;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace LDraw.UI
{
	public sealed partial class SceneUI :UserControl, IDockable, IDisposable, INotifyPropertyChanged
	{
		public SceneUI(Model model, string name)
		{
			InitializeComponent();
			DockControl = new DockControl(this, name)
			{
				ShowTitle = false,
				TabText = name,
				TabCMenu = this.FindCMenu("TabCMenu", this),
				DestroyOnClose = true,
			};
			OtherScenes = new ListCollectionView(new List<SceneWrapper>());
			Model = model;
			SceneName = name;
			SceneView.Options = Settings.Scene;
			SceneView.Scene.Window.SetLightSource(v4.Origin, Math_.Normalise(new v4(0, 0, -1, 0)), true);
			SceneView.Background = Colour32.LightSteelBlue.ToMediaBrush();

			RenameScene = Command.Create(this, RenameSceneInternal);
			ClearScene = Command.Create(this, ClearSceneInternal);
			LinkCamera = Command.Create(this, LinkCameraInternal);
			ShowLightingUI = Command.Create(this, ShowLightingUIInternal);
			CloseScene = Command.Create(this, CloseSceneInternal);

			InitCMenus();
			PopulateOtherScenes();
			DataContext = this;
		}
		public void Dispose()
		{
			Model = null!;
			DockControl = null!;
			m_scene.Dispose();
		}

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get => m_dock_control;
			private set
			{
				if (m_dock_control == value) return;
				if (m_dock_control != null)
				{
					m_dock_control.ActiveChanged -= HandleSceneActive;
					m_dock_control.SavingLayout -= HandleSavingLayout;
					m_dock_control.LoadingLayout -= HandleLoadingLayout;
					Util.Dispose(ref m_dock_control!);
				}
				m_dock_control = value;
				if (m_dock_control != null)
				{
					m_dock_control.LoadingLayout += HandleLoadingLayout;
					m_dock_control.SavingLayout += HandleSavingLayout;
					m_dock_control.ActiveChanged += HandleSceneActive;
				}

				// Handlers
				void HandleSceneActive(object sender, ActiveContentChangedEventArgs args)
				{
					//	Options.BkColour = args.ContentNew == this ? Color.LightSteelBlue : Color.LightGray;
					//	Invalidate();
				}
				void HandleLoadingLayout(object sender, DockContainerLoadingLayoutEventArgs e)
				{
					var camera_xml = e.UserData.Element(nameof(SceneView.Camera));
					if (camera_xml != null)
						SceneView.Camera.Load(camera_xml);
				}
				void HandleSavingLayout(object sender, DockContainerSavingLayoutEventArgs e)
				{
					e.Node.Add2(nameof(SceneView.Camera), SceneView.Camera, false);
				}
			}
		}
		private DockControl m_dock_control = null!;

		/// <summary>App logic</summary>
		public Model Model
		{
			get => m_model;
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.Settings.SettingChange -= HandleSettingChange;
					m_model.Scenes.CollectionChanged -= HandleScenesCollectionChanged;
					m_model.Scenes.Remove(this);
				}
				m_model = value;
				if (m_model != null)
				{
					// Don't add to m_model.Scenes, that's the caller's choice
					m_model.Settings.SettingChange += HandleSettingChange;
					m_model.Scenes.CollectionChanged += HandleScenesCollectionChanged;
				}

				// Handlers
				void HandleSettingChange(object sender, SettingChangeEventArgs e)
				{
					if (e.Before) return;
					switch (e.Key)
					{
					case nameof(SettingsData.Scene):
						{
							SceneView.Options = Settings.Scene;
							break;
						}
					}
				}
				void HandleScenesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					PopulateOtherScenes();
				}
			}
		}
		private Model m_model = null!;

		/// <summary>Add settings</summary>
		public SettingsData Settings => Model.Settings;

		/// <summary>The 3d part of the scene (i.e. the chart control)</summary>
		public ChartControl SceneView => m_scene;

		/// <summary>The name assigned to this scene UI</summary>
		public string SceneName
		{
			get => m_scene_name;
			set
			{
				if (m_scene_name == value) return;
				m_scene_name = value;
				DockControl.TabText = m_scene_name;
				NotifyPropertyChanged(nameof(SceneName));
			}
		}
		private string m_scene_name = null!;

		/// <summary>True if this view should auto range on new data</summary>
		public bool AutoRange
		{
			get { return (bool)GetValue(AutoRangeProperty); }
			set { SetValue(AutoRangeProperty, value); }
		}
		public static readonly DependencyProperty AutoRangeProperty = Gui_.DPRegister<SceneUI>(nameof(AutoRange));

		/// <summary>Other available scenes</summary>
		public ICollectionView OtherScenes { get; }
		private void PopulateOtherScenes()
		{
			var list = (List<SceneWrapper>)OtherScenes.SourceCollection;
			list.Sync(Model.Scenes.Except(this).Select(x => new SceneWrapper(x)).Prepend(SceneWrapper.NullScene));
			if (OtherScenes.CurrentItem == null) OtherScenes.MoveCurrentToFirst();
			OtherScenes.Refresh();
			NotifyPropertyChanged(nameof(OtherScenes));
		}

		/// <summary>Set up the context menus</summary>
		private void InitCMenus()
		{
			SceneView.Scene.ContextMenu = this.FindCMenu("LDrawCMenu", new SceneCMenu(this));
			SceneView.XAxisPanel.ContextMenu = this.FindCMenu("LDrawAxisCMenu", new SceneAxisCMenu(this, ChartControl.EAxis.XAxis));
			SceneView.YAxisPanel.ContextMenu = this.FindCMenu("LDrawAxisCMenu", new SceneAxisCMenu(this, ChartControl.EAxis.YAxis));
		}

		/// <summary>Rename the scene tab</summary>
		public Command RenameScene { get; }
		private void RenameSceneInternal()
		{
			var prompt = new PromptUI(Window.GetWindow(this))
			{
				Title = "Rename Scene",
                Prompt = "Set the label for this scene",
                Value = SceneName,
                ShowWrapCheckbox = false,
			};
            if (prompt.ShowDialog() == true)
            {
                SceneName = prompt.Value;
            }
		}

		/// <summary>Remove all objects from the scene</summary>
		public Command ClearScene { get; }
		private void ClearSceneInternal()
		{
			SceneView.Scene.RemoveAllObjects();
			SceneView.Invalidate();
		}

		/// <summary>Link the camera for this scene to the camera of another scene</summary>
		public Command LinkCamera { get; }
		private void LinkCameraInternal()
		{

		}

		/// <summary>Show the lighting dialog</summary>
		public Command ShowLightingUI { get; }
		private void ShowLightingUIInternal()
		{
			if (m_lighting_ui == null)
			{
				m_lighting_ui = new LightingUI(App.Current.MainWindow, SceneView.Scene.Window);
				m_lighting_ui.Closed += delegate { m_lighting_ui = null; };
				m_lighting_ui.Show();
			}
			m_lighting_ui.Focus();
		}
		private LightingUI? m_lighting_ui;

		/// <summary>Close and remove this scene</summary>
		public Command CloseScene { get; }
		private void CloseSceneInternal()
		{
			Model.Scenes.Remove(this);
			Dispose();
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
