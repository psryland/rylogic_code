using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using LDraw.Dialogs;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Gui.WPF.ChartDetail;
using Rylogic.Maths;

namespace LDraw.UI
{
	public partial class SceneUI :IView3dCMenu, IChartCMenu
	{
		// Notes:
		//  - Don't sign up to events on 'm_owner' because that causes leaked references.
		//    When the context menu gets replaced. Instead, have the owner call
		//    'NotifyPropertyChanged'.

		private void InitCommands()
		{
			ClearScene = Command.Create(this, ClearSceneInternal);
			ShowAnimationUI = Command.Create(this, ShowAnimationUIInternal);
			ShowLightingUI = Command.Create(this, ShowLightingUIInternal);
			LinkCamera = Command.Create(this, LinkCameraInternal);
		}

		/// <inheritdoc/>
		public Command ClearScene { get; private set; } = null!;
		private void ClearSceneInternal()
		{
			SceneView.Scene.RemoveAllObjects();
			SceneView.Invalidate();
		}

		/// <inheritdoc/>
		public bool OriginPointVisible
		{
			get => SceneState.Chart.OriginPointVisible;
			set => SceneState.Chart.OriginPointVisible = value;
		}
		public ICommand ToggleOriginPoint => SceneView.ToggleOriginPoint;

		/// <inheritdoc/>
		public bool FocusPointVisible
		{
			get => SceneState.Chart.FocusPointVisible;
			set => SceneState.Chart.FocusPointVisible = value;
		}
		public ICommand ToggleFocusPoint => SceneView.ToggleFocusPoint;

		/// <summary>Selection box</summary>
		public bool SelectionBoxVisible
		{
			get => SceneView.Scene.Window.SelectionBoxVisible;
			set => SceneView.Scene.Window.SelectionBoxVisible = value;
		}
		public ICommand ToggleSelectionBox => SceneView.Scene.ToggleSelectionBox;

		/// <inheritdoc/>
		public bool ShowCrossHair
		{
			get => SceneView.ShowCrossHair;
			set => SceneView.ShowCrossHair = value;
		}
		public ICommand ToggleShowCrossHair => SceneView.ToggleShowCrossHair;

		/// <inheritdoc/>
		public bool ShowValueAtPointer
		{
			get => SceneView.ShowValueAtPointer;
			set => SceneView.ShowValueAtPointer = value;
		}
		public ICommand ToggleShowValueAtPointer => SceneView.ToggleShowValueAtPointer;

		/// <inheritdoc/>
		public bool ShowAxes
		{
			get => SceneState.Chart.ShowAxes;
			set => SceneState.Chart.ShowAxes = value;
		}
		public ICommand ToggleAxes => SceneView.ToggleAxes;

		/// <inheritdoc/>
		public bool ShowGridLines
		{
			get => SceneState.Chart.ShowGridLines;
			set => SceneState.Chart.ShowGridLines = value;
		}
		public ICommand ToggleGridLines => SceneView.ToggleGridLines;

		/// <inheritdoc/>
		public bool Orthographic
		{
			get => SceneState.Chart.Orthographic;
			set => SceneState.Chart.Orthographic = value;
		}
		public ICommand ToggleOrthographic => SceneView.ToggleOrthographic;

		/// <inheritdoc/>
		public bool Antialiasing
		{
			get => SceneState.Chart.Antialiasing;
			set => SceneState.Chart.Antialiasing = value;
		}
		public ICommand ToggleAntialiasing => SceneView.ToggleAntialiasing;

		/// <inheritdoc/>
		public bool LockAspect
		{
			get => SceneView.LockAspect;
			set => SceneView.LockAspect = value;
		}
		public ICommand ToggleLockAspect => SceneView.ToggleLockAspect;

		/// <inheritdoc/>
		public bool MouseCentredZoom
		{
			get => SceneState.Chart.MouseCentredZoom;
			set => SceneState.Chart.MouseCentredZoom = value;
		}
		public ICommand ToggleMouseCentredZoom => SceneView.ToggleMouseCentredZoom;

		/// <inheritdoc/>
		public Colour32 BackgroundColour
		{
			get => SceneState.Chart.BackgroundColour;
			set => SceneState.Chart.BackgroundColour = value;
		}
		public ICommand SetBackgroundColour => SceneView.SetBackgroundColour;

		/// <inheritdoc/>
		public ICommand ShowMeasureToolUI => SceneView.ShowMeasureToolUI;

		/// <inheritdoc/>
		public ICommand ShowAnimationUI { get; private set; } = null!;
		private void ShowAnimationUIInternal()
		{
			AnimationUI = !AnimationUI;
		}

		/// <inheritdoc/>
		public ICommand ShowLightingUI { get; private set; } = null!;
		private void ShowLightingUIInternal()
		{
			if (m_lighting_ui == null)
			{
				m_lighting_ui = new View3dLightingUI(Window.GetWindow(this), SceneView.Scene.Window);
				m_lighting_ui.Closed += delegate { m_lighting_ui = null; };
				m_lighting_ui.Show();
			}
			m_lighting_ui.Focus();
		}
		private View3dLightingUI? m_lighting_ui;

		/// <inheritdoc/>
		public ICommand ShowObjectManagerUI => SceneView.Scene.ShowObjectManagerUI;

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
		public ICommand AutoRangeView => SceneView.AutoRangeView;

		/// <inheritdoc/>
		public ICommand DoAspect11 => SceneView.DoAspect11;

		/// <inheritdoc/>
		public bool CanLinkCamera => Model.Scenes.Count > 1;
		public ICommand LinkCamera { get; private set; } = null!;
		private void LinkCameraInternal()
		{
			if (m_link_cameras_ui == null)
			{
				m_link_cameras_ui = new LinkCamerasUI(Window.GetWindow(this), Model);
				m_link_cameras_ui.Closed += delegate { m_link_cameras_ui = null; };
				m_link_cameras_ui.Show();
			}
			m_link_cameras_ui.Source = new SceneWrapper(this);
			m_link_cameras_ui.Focus();
		}
		private static LinkCamerasUI? m_link_cameras_ui;

		/// <inheritdoc/>
		public EAlignDirection AlignDirection
		{
			get => SceneState.AlignDirection;
			set => SceneState.AlignDirection = value;
		}

		/// <inheritdoc/>
		public EViewPreset ViewPreset
		{
			get => SceneState.ViewPreset;
			set => SceneState.ViewPreset = value;
		}

		/// <inheritdoc/>
		public View3d.EFillMode FillMode
		{
			get => SceneState.Chart.FillMode;
			set => SceneState.Chart.FillMode = value;
		}

		/// <inheritdoc/>
		public View3d.ECullMode CullMode
		{
			get => SceneState.Chart.CullMode;
			set => SceneState.Chart.CullMode = value;
		}

		/// <summary>Saved views</summary>
		public ICollectionView SavedViews => SceneView.Scene.SavedViews;
		public ICommand ApplySavedView => SceneView.Scene.ApplySavedView;
		public ICommand SaveCurrentView => SceneView.Scene.SaveCurrentView;
		public ICommand RemoveSavedView => SceneView.Scene.RemoveSavedView;

		/// <inheritdoc/>
		public ChartControl.ENavMode NavigationMode
		{
			get => SceneState.Chart.NavigationMode;
			set => SceneState.Chart.NavigationMode = value;
		}

		/// <inheritdoc/>
		public bool BBoxesVisible
		{
			get => SceneView.Scene.Window.Diag.BBoxesVisible;
			set => SceneView.Scene.Window.Diag.BBoxesVisible = value;
		}
		public ICommand ToggleBBoxesVisible => SceneView.Scene.ToggleBBoxesVisible;

		/// <inheritdoc/>
		public float NormalsLength
		{
			get => SceneView.Scene.Window.Diag.NormalsLength;
			set => SceneView.Scene.Window.Diag.NormalsLength = value;
		}

		/// <inheritdoc/>
		public Colour32 NormalsColour
		{
			get => SceneView.Scene.Window.Diag.NormalsColour;
			set => SceneView.Scene.Window.Diag.NormalsColour = value;
		}
		public ICommand SetNormalsColour => SceneView.Scene.SetNormalsColour;

		/// <inheritdoc/>
		public float FillModePointsSize
		{
			get => SceneView.Scene.Window.Diag.FillModePointsSize.x;
			set => SceneView.Scene.Window.Diag.FillModePointsSize = new v2(value, value);
		}
	}

	public class SceneAxisCMenu :IChartAxisCMenu
	{
		private readonly AxisPanel m_axis_panel;
		public SceneAxisCMenu(SceneUI owner, ChartControl.EAxis axis)
		{
			m_axis_panel = owner.SceneView.GetAxisPanel(axis);
		}

		/// <inheritdoc/>
		public bool AllowScroll
		{
			get => m_axis_panel.AllowScroll;
			set => m_axis_panel.AllowScroll = value;
		}
		public ICommand ToggleScrollLock => m_axis_panel.ToggleScrollLock;

		/// <inheritdoc/>
		public bool AllowZoom
		{
			get => m_axis_panel.AllowZoom;
			set => m_axis_panel.AllowZoom = value;
		}
		public ICommand ToggleZoomLock => m_axis_panel.ToggleZoomLock;

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
