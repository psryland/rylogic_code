using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public partial class View3dControl
	{
		// Notes:
		//  - CMenu howto? Look in IView3dCMenu.cs
		//  - If adding a new context menu option, you'll need to do:
		//       - Add a MenuItem resource to 'ContextMenus.xaml'
		//       - Add any binding variables to 'IView3dCMenu'
		//       - Implement the functionality in the owner, and create forwarding
		//         properties here.
		//       - Find the appropriate 'HandleSettingChanged' or 'NotifyPropertyChanged'
		//         method to add a case to, and call the 'NotifyPropertyChanged' on this
		//         object via:
		//             if (ContextMenu.DataContext is IView3dCMenu cmenu)
		//                  cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.<prop_name>))
		//
		//  - Don't sign up to events on 'm_owner' because that causes leaked references.
		//    When the context menu gets replaced. Instead, have the owner call
		//    'NotifyPropertyChanged'.
		private void InitCommands()
		{
			// Tools Menu
			ToggleOriginPoint = Command.Create(this, ToggleOriginPointInternal);
			ToggleFocusPoint = Command.Create(this, ToggleFocusPointInternal);
			ToggleBBoxesVisible = Command.Create(this, ToggleBBoxesVisibleInternal);
			ToggleSelectionBox = Command.Create(this, ToggleSelectionBoxInternal);
			ShowMeasureToolUI = Command.Create(this, ShowMeasureToolInternal);

			// Camera Menu
			AutoRangeView = Command.Create(this, AutoRangeViewInternal);
			ToggleOrthographic = Command.Create(this, ToggleOrthographicInternal);
			ApplySavedView = Command.Create(this, ApplySavedViewInternal);
			SaveCurrentView = Command.Create(this, SaveCurrentViewInternal);
			RemoveSavedView = Command.Create(this, RemoveSavedViewInternal);
			
			// Renderering Menu
			SetBackgroundColour = Command.Create(this, SetBackgroundColourInternal);
			ToggleAntialiasing = Command.Create(this, ToggleAntialiasingInternal);
			ShowAnimationUI = Command.Create(this, ShowAnimationUIInternal);
			ShowLightingUI = Command.Create(this, ShowLightingUIInternal);

			// Diagnostics
			ToggleShowNormals = Command.Create(this, ToggleShowNormalsInternal);
			SetNormalsColour = Command.Create(this, SetNormalsColourInternal);

			// Objects Menu
			ShowObjectManagerUI = Command.Create(this, ShowObjectManagerInternal);
		}

		/// <inheritdoc/>
		public IView3dCMenu View3dCMenuContext => this;

		/// <inheritdoc/>
		public bool OriginPointVisible
		{
			get => Window.OriginPointVisible;
			set => Window.OriginPointVisible = value;
		}
		public ICommand ToggleOriginPoint { get; private set; } = null!;
		private void ToggleOriginPointInternal()
		{
			OriginPointVisible = !OriginPointVisible;
			Invalidate();
		}

		/// <inheritdoc/>
		public bool FocusPointVisible
		{
			get => Window.FocusPointVisible;
			set => Window.FocusPointVisible = value;
		}
		public ICommand ToggleFocusPoint { get; private set; } = null!;
		private void ToggleFocusPointInternal()
		{
			FocusPointVisible = !FocusPointVisible;
			Invalidate();
		}

		/// <inheritdoc/>
		public bool BBoxesVisible
		{
			get => Window.Diag.BBoxesVisible;
			set => Window.Diag.BBoxesVisible = value;
		}
		public ICommand ToggleBBoxesVisible { get; private set; } = null!;
		private void ToggleBBoxesVisibleInternal()
		{
			BBoxesVisible = !BBoxesVisible;
			Invalidate();
		}

		/// <inheritdoc/>
		public bool SelectionBoxVisible
		{
			get => Window.SelectionBoxVisible;
			set => Window.SelectionBoxVisible = value;
		}
		public ICommand ToggleSelectionBox { get; private set; } = null!;
		private void ToggleSelectionBoxInternal()
		{
			SelectionBoxVisible = !SelectionBoxVisible;
			Invalidate();
		}

		/// <inheritdoc/>
		public View3d.ESceneBounds AutoRangeBounds
		{
			get => m_AutoRangeBounds;
			set
			{
				if (m_AutoRangeBounds == value) return;
				m_AutoRangeBounds = value;
				NotifyPropertyChanged(nameof(AutoRangeBounds));
			}
		}
		private View3d.ESceneBounds m_AutoRangeBounds;
		public ICommand AutoRangeView { get; private set; } = null!;
		private void AutoRangeViewInternal(object? parameter)
		{
			var bounds = parameter is View3d.ESceneBounds b ? b : View3d.ESceneBounds.All;
			Camera.ResetView(Window.SceneBounds(bounds));
			Invalidate();
		}

		/// <inheritdoc/>
		public bool Orthographic
		{
			get => Camera.Orthographic;
			set => Camera.Orthographic = value;
		}
		public ICommand ToggleOrthographic { get; private set; } = null!;
		private void ToggleOrthographicInternal()
		{
			Orthographic = !Orthographic;
			Invalidate();
		}

		/// <inheritdoc/>
		public EAlignDirection AlignDirection
		{
			get => m_align_direction;
			set
			{
				if (m_align_direction == value) return;
				m_align_direction = value;
				Camera.AlignAxis = AlignDirection_.ToAxis(m_align_direction);
				Invalidate();
			}
		}
		private EAlignDirection m_align_direction;

		/// <inheritdoc/>
		public EViewPreset ViewPreset
		{
			get => m_view_preset;
			set
			{
				if (m_view_preset == value) return;
				m_view_preset = value;
				if (m_view_preset != EViewPreset.Current)
				{
					var pos = Camera.FocusPoint;
					Camera.ResetView(ViewPreset_.ToForward(m_view_preset));
					Camera.FocusPoint = pos;
					Invalidate();
				}
			}
		}
		private EViewPreset m_view_preset;

		/// <inheritdoc/>
		public ICollectionView SavedViews { get; private set; } = new ListCollectionView(new ObservableCollection<SavedView>());
		private ObservableCollection<SavedView> SavedViewsList => (ObservableCollection<SavedView>)SavedViews.SourceCollection;

		/// <inheritdoc/>
		public ICommand ApplySavedView { get; private set; } = null!;
		private void ApplySavedViewInternal()
		{
			if (SavedViews.CurrentItem is SavedView view)
			{
				view.Apply(Camera);
				Invalidate();
			}
		}

		/// <inheritdoc/>
		public ICommand SaveCurrentView { get; private set; } = null!;
		private void SaveCurrentViewInternal()
		{
			// Generate a name for the saved view
			var name = $"View {++m_saved_view_id}";
			for (; SavedViewsList.Any(x => string.Compare(x.Name, name) == 0); name = $"View {++m_saved_view_id}") { }

			var ui = new PromptUI
			{
				Owner = System.Windows.Window.GetWindow(this),
				Title = "Saved View Name",
				Prompt = "Label this view",
				ShowWrapCheckbox = false,
				Value = name,
			};
			if (ui.ShowDialog() == true)
			{
				SavedViewsList.Add(new SavedView(ui.Value, Camera));
			}
		}
		private int m_saved_view_id;

		/// <inheritdoc/>
		public ICommand RemoveSavedView { get; private set; } = null!;
		private void RemoveSavedViewInternal()
		{
			if (SavedViews.CurrentItem is SavedView view)
				SavedViewsList.Remove(view);
		}

		/// <inheritdoc/>
		public Colour32 BackgroundColour
		{
			get => (Colour32)GetValue(BackgroundColourProperty);
			set => SetValue(BackgroundColourProperty, value);
		}
		private void BackgroundColour_Changed(Colour32 new_value)
		{
			Window.BackgroundColour = new_value;
			NotifyPropertyChanged(nameof(BackgroundColour));
		}
		public static readonly DependencyProperty BackgroundColourProperty = Gui_.DPRegister<View3dControl>(nameof(BackgroundColour), def: Colour32.LightGray);
		public ICommand SetBackgroundColour { get; private set; } = null!;
		private void SetBackgroundColourInternal()
		{
			var bg = BackgroundColour;
			var dlg = new ColourPickerUI
			{
				Title = "Background Colour",
				Owner = System.Windows.Window.GetWindow(this),
				Colour = bg,
			};
			dlg.ColorChanged += (s, a) =>
			{
				BackgroundColour = a.Colour;
			};
			if (dlg.ShowDialog() == true)
				BackgroundColour = dlg.Colour;
			else
				BackgroundColour = bg;
		}
		public Brush BackgroundColourBrush => BackgroundColour.ToMediaBrush();

		/// <inheritdoc/>
		public bool Antialiasing
		{
			get => MultiSampling != 1;
			set => MultiSampling = value ? 4 : 1;
		}
		public ICommand ToggleAntialiasing { get; private set; } = null!;
		private void ToggleAntialiasingInternal()
		{
			Antialiasing = !Antialiasing;
			Invalidate();
		}

		/// <inheritdoc/>
		public View3d.EFillMode FillMode
		{
			get => Window.FillMode;
			set => Window.FillMode = value;
		}

		/// <inheritdoc/>
		public View3d.ECullMode CullMode
		{
			get => Window.CullMode;
			set => Window.CullMode = value;
		}

		/// <summary>Show/Hide normals for all objects</summary>
		public bool ShowNormals
		{
			get
			{
				var normals_visible = false;
				Window.EnumObjects(obj =>
				{
					normals_visible |= obj.ShowNormals;
					return true;
				});
				return normals_visible;
			}
			set
			{
				Window.EnumObjects(obj =>
				{
					obj.ShowNormals = value;
					return true;
				});
				NotifyPropertyChanged(nameof(ShowNormals));
				Invalidate();
			}
		}
		public ICommand ToggleShowNormals { get; private set; } = null!;
		private void ToggleShowNormalsInternal()
		{
			ShowNormals = !ShowNormals;
		}

		/// <inheritdoc/>
		public float NormalsLength
		{
			get => Window.Diag.NormalsLength;
			set => Window.Diag.NormalsLength = value;
		}

		/// <inheritdoc/>
		public Colour32 NormalsColour
		{
			get => Window.Diag.NormalsColour;
			set => Window.Diag.NormalsColour = value;
		}
		public ICommand SetNormalsColour { get; private set; } = null!;
		private void SetNormalsColourInternal()
		{
			var col = NormalsColour;
			var dlg = new ColourPickerUI
			{
				Title = "Normals Colour",
				Owner = System.Windows.Window.GetWindow(this),
				Colour = col,
			};
			dlg.ColorChanged += (s, a) =>
			{
				NormalsColour = a.Colour;
			};
			if (dlg.ShowDialog() == true)
				NormalsColour = dlg.Colour;
			else
				NormalsColour = col;
		}

		/// <inheritdoc/>
		public float FillModePointsSize
		{
			get => Window.Diag.FillModePointsSize.x;
			set => Window.Diag.FillModePointsSize = new Maths.v2(value, value);
		}

		/// <inheritdoc/>
		public ICommand ShowAnimationUI { get; private set; } = null!;
		private void ShowAnimationUIInternal()
		{
			if (m_animation_ui == null)
			{
				m_animation_ui = new Window
				{
					WindowStyle = WindowStyle.ToolWindow,
					WindowStartupLocation = WindowStartupLocation.CenterOwner,
					Owner = System.Windows.Window.GetWindow(this),
					Icon = System.Windows.Window.GetWindow(this)?.Icon,
					Title = "Animation Controls",
					SizeToContent = SizeToContent.WidthAndHeight,
					ResizeMode = ResizeMode.CanResizeWithGrip,
					ShowInTaskbar = false,
				};
				m_animation_ui.Content = new View3dAnimControls
				{
					ViewWindow = Window
				};
				m_animation_ui.Closed += delegate { m_animation_ui = null; };
				m_animation_ui.Show();
			}
			m_animation_ui.Focus();
		}
		private Window? m_animation_ui;

		/// <inheritdoc/>
		public ICommand ShowLightingUI { get; private set; } = null!;
		private void ShowLightingUIInternal()
		{
			if (m_lighting_ui == null)
			{
				var light = new View3d.Light(Window.LightProperties);
				light.PropertyChanged += (s, a) =>
				{
					if (m_lighting_ui == null) return;
					Window.LightProperties = m_lighting_ui.Light;
					Invalidate();
				};

				m_lighting_ui = new View3dLightingUI(System.Windows.Window.GetWindow(this), Window);
				m_lighting_ui.Closed += delegate { m_lighting_ui = null; };
				m_lighting_ui.Light = light;
				m_lighting_ui.Show();
			}
			m_lighting_ui.Focus();
		}
		private View3dLightingUI? m_lighting_ui;

		/// <inheritdoc/>
		public ICommand ShowMeasureToolUI { get; private set; } = null!;
		private void ShowMeasureToolInternal()
		{
			if (m_measurement_ui == null)
			{
				m_measurement_ui = new View3dMeasurementUI(this);
				m_measurement_ui.Closed += (s, a) => { m_measurement_ui = null; Invalidate(); };
				m_measurement_ui.Show();
			}
			m_measurement_ui.Focus();
		}
		private View3dMeasurementUI? m_measurement_ui;

		/// <inheritdoc/>
		public ICommand ShowObjectManagerUI { get; private set; } = null!;
		private void ShowObjectManagerInternal()
		{
			if (m_object_manager_ui == null)
			{
				m_object_manager_ui = new View3dObjectManagerUI(System.Windows.Window.GetWindow(this), Window);
				m_object_manager_ui.Closed += delegate { m_object_manager_ui = null; };
				m_object_manager_ui.Show();
			}
			m_object_manager_ui.Focus();
		}
		private View3dObjectManagerUI? m_object_manager_ui;
	}
}
