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
			SceneView = m_scene;
			SceneName = name;
			SceneState = Settings.SceneState.get(name);
			SceneView.Background = Colour32.LightSteelBlue.ToMediaBrush();
			SceneView.Scene.Window.SetLightSource(v4.Origin, Math_.Normalise(new v4(0, 0, -1, 0)), true);
			SceneView.Scene.Window.OnRendering += HandleSceneRendering;
			Links = new List<ChartLink>();

			RenameScene = Command.Create(this, RenameSceneInternal);
			CloseScene = Command.Create(this, CloseSceneInternal);

			InitCMenus();
			PopulateOtherScenes();
			DataContext = this;
		}
		public void Dispose()
		{
			Model = null!;
			SceneView = null!;
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
				void HandleSceneActive(object? sender, ActiveContentChangedEventArgs args)
				{
				}
				void HandleLoadingLayout(object? sender, DockContainerLoadingLayoutEventArgs e)
				{
				}
				void HandleSavingLayout(object? sender, DockContainerSavingLayoutEventArgs e)
				{
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
					m_model.Scenes.CollectionChanged -= HandleScenesCollectionChanged;
					m_model.Scenes.Remove(this);
				}
				m_model = value;
				if (m_model != null)
				{
					// Don't add to m_model.Scenes, that's the caller's choice
					m_model.Scenes.CollectionChanged += HandleScenesCollectionChanged;
				}

				// Handlers
				void HandleScenesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					PopulateOtherScenes();
					CleanupLinks();
				}
			}
		}
		private Model m_model = null!;

		/// <summary>Add settings</summary>
		public SettingsData Settings => Model.Settings;
		
		/// <summary>Scene state settings</summary>
		public SceneStateData SceneState
		{
			get => m_scene_state;
			private set
			{
				if (m_scene_state == value) return;
				if (m_scene_state != null)
				{
					SceneView.Scene.Window.OnSettingsChanged -= HandleSceneSettingChanged;
					m_scene_state.SettingChange -= HandleSettingChange;
					SceneView.Options = new ChartControl.OptionsData();
				}
				m_scene_state = value;
				if (m_scene_state != null)
				{
					SceneView.Options = m_scene_state.Chart;
					m_scene_state.SettingChange += HandleSettingChange;
					SceneView.Scene.Window.OnSettingsChanged += HandleSceneSettingChanged;
				}

				// Handlers
				void HandleSettingChange(object? sender, SettingChangeEventArgs e)
				{
					// Handle notification that a SceneState value has changed
					if (e.Before) return;
					switch (e.Key)
					{
					case nameof(SceneStateData.Chart):
						{
							SceneView.Options = SceneState.Chart;
							break;
						}
					case nameof(SceneStateData.AlignDirection):
						{
							SceneView.Scene.AlignDirection = SceneState.AlignDirection;
							break;
						}
					case nameof(SceneStateData.ViewPreset):
						{
							SceneView.Scene.ViewPreset = SceneState.ViewPreset;
							break;
						}
					}
				}
				void HandleSceneSettingChanged(object? sender, View3d.SettingChangeEventArgs e)
				{
					// Handle notification that a View3d.Window property has changed
					var cmenu = SceneView.Scene.ContextMenu?.DataContext as IView3dCMenu;

					// Persist settings
					if (Bit.AllSet(e.Setting, View3d.ESettings.Camera))
					{
						if (Bit.AllSet(e.Setting, View3d.ESettings.Camera_Orthographic))
						{
							SceneState.Chart.Orthographic = SceneView.Camera.Orthographic;
							cmenu?.NotifyPropertyChanged(nameof(IView3dCMenu.Orthographic));
						}
						if (Bit.AllSet(e.Setting, View3d.ESettings.Camera_AlignAxis))
						{
							SceneState.AlignDirection = AlignDirection_.FromAxis(SceneView.Camera.AlignAxis);
							cmenu?.NotifyPropertyChanged(nameof(IView3dCMenu.AlignDirection));
						}
					}
				}
			}
		}
		private SceneStateData m_scene_state = null!;

		/// <summary>The 3d part of the scene (i.e. the chart control)</summary>
		public ChartControl SceneView
		{
			get => m_scene_view;
			set
			{
				if (m_scene_view == value) return;
				if (m_scene_view != null)
				{
					m_scene_view.PropertyChanged -= HandlePropertyChanged;
				}
				m_scene_view = value;
				if (m_scene_view != null)
				{
					m_scene_view.PropertyChanged += HandlePropertyChanged;
				}

				// Handler
				void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					// Forward property changed from the ChartControl.
					// Probably should only forward properties of IChartCMenu and IView3dCMenu.
					NotifyPropertyChanged(e.PropertyName);
				}
			}
		}
		private ChartControl m_scene_view = null!;

		/// <summary>The camera focus point</summary>
		public v4 FocusPoint
		{
			get => SceneView.Camera.FocusPoint;
			set
			{
				if (FocusPoint == value) return;
				SceneView.Camera.FocusPoint = value;
				NotifyPropertyChanged(nameof(FocusPoint));
				SceneView.Invalidate();
			}
		}

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

		/// <summary>The links to other scenes</summary>
		public List<ChartLink> Links { get; }
		public void CleanupLinks()
		{
			Links.RemoveAll(x => x.Source == null || x.Target == null || (x.CamLink == ELinkCameras.None && x.AxisLink == ELinkAxes.None));
		}

		/// <summary>Set up the context menus</summary>
		private void InitCMenus()
		{
			SceneView.Scene.ContextMenu = this.FindCMenu("LDrawCMenu", this);
			SceneView.XAxisPanel.ContextMenu = this.FindCMenu("LDrawAxisCMenu", SceneView.GetAxisPanel(ChartControl.EAxis.XAxis));
			SceneView.YAxisPanel.ContextMenu = this.FindCMenu("LDrawAxisCMenu", SceneView.GetAxisPanel(ChartControl.EAxis.YAxis));
		}

		/// <summary>The data context for View3d menu items</summary>
		public IView3dCMenu View3dCMenuContext => SceneView.Scene.View3dCMenuContext;

		/// <summary>The data context for View3d menu items</summary>
		public IChartCMenu ChartCMenuContext => SceneView.Scene.ChartCMenuContext;

		/// <summary>True if the animation UI is visible</summary>
		public bool AnimationUI
		{
			get => m_show_anim_ui;
			set
			{
				if (m_show_anim_ui == value) return;
				m_show_anim_ui = value;
				NotifyPropertyChanged(nameof(AnimationUI));
			}
		}
		private bool m_show_anim_ui;

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

		/// <summary>Close and remove this scene</summary>
		public Command CloseScene { get; }
		private void CloseSceneInternal()
		{
			Model.Scenes.Remove(this);
			Dispose();
		}

		/// <summary>Event from the view3d window during rendering</summary>
		private void HandleSceneRendering(object? sender, EventArgs e)
		{
			NotifyPropertyChanged(nameof(FocusPoint));
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
