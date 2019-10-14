using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using LDraw.Dialogs;
using Rylogic.Common;
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
				TabCMenu = (ContextMenu)FindResource("TabCMenu"),
				DestroyOnClose = true,
			};
			Model = model;
			SceneName = name;
			SceneView.Options = Settings.Scene;
			SceneView.Scene.Window.SetLightSource(v4.Origin, Math_.Normalise(new v4(0, 0, -1, 0)), true);
			SceneView.Background = Colour32.LightSteelBlue.ToMediaBrush();

			ShowLightingUI = Command.Create(this, ShowLightingUIInternal);

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
					Util.Dispose(ref m_dock_control!);
				}
				m_dock_control = value;
				if (m_dock_control != null)
				{
					m_dock_control.SavingLayout += HandleSavingLayout;
					m_dock_control.ActiveChanged += HandleSceneActive;
				}

				// Handlers
				void HandleSceneActive(object sender, ActiveContentChangedEventArgs args)
				{
					//	Options.BkColour = args.ContentNew == this ? Color.LightSteelBlue : Color.LightGray;
					//	Invalidate();
				}
				void HandleSavingLayout(object sender, DockContainerSavingLayoutEventArgs args)
				{
					//	args.Node.Add2(nameof(Camera), Camera, false);
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
					m_model.Scenes.Remove(this);
				}
				m_model = value;
				if (m_model != null)
				{
					// Don't add to m_model.Scenes, that's the caller's choice
					m_model.Settings.SettingChange += HandleSettingChange;
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

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
