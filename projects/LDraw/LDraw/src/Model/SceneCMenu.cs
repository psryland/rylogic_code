using System;
using System.ComponentModel;
using System.Windows.Input;
using LDraw.UI;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Gui.WPF.ChartDetail;
using Rylogic.Maths;

namespace LDraw
{
	public class SceneCMenu :IView3dCMenu, IChartCMenu
	{
		// Notes:
		//  - Don't sign up to events on 'm_owner' because that causes leaked references.
		//    When the context menu gets replaced. Instead, have the owner call
		//    'NotifyPropertyChanged'.

		private readonly SceneUI m_owner;
		public SceneCMenu(SceneUI owner)
		{
			m_owner = owner;
		}

		/// <summary>Allow property changed to be triggered externally</summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary></summary>
		public ICommand ClearScene
		{
			get => m_owner.ClearScene;
		}
		public ICommand ResetView
		{
			get => m_owner.SceneView.Scene.ResetView;
		}

		/// <inheritdoc/>
		public bool OriginPointVisible
		{
			get => m_owner.SceneState.Chart.OriginPointVisible;
			set => m_owner.SceneState.Chart.OriginPointVisible = value;
		}
		public ICommand ToggleOriginPoint
		{
			get => m_owner.SceneView.ToggleOriginPoint;
		}

		/// <inheritdoc/>
		public bool FocusPointVisible
		{
			get => m_owner.SceneState.Chart.FocusPointVisible;
			set => m_owner.SceneState.Chart.FocusPointVisible = value;
		}
		public ICommand ToggleFocusPoint
		{
			get => m_owner.SceneView.ToggleFocusPoint;
		}

		/// <summary>Selection box</summary>
		public bool SelectionBoxVisible
		{
			get => m_owner.SceneView.Scene.Window.SelectionBoxVisible;
			set => m_owner.SceneView.Scene.Window.SelectionBoxVisible = value;
		}
		public ICommand ToggleSelectionBox
		{
			get => m_owner.SceneView.Scene.ToggleSelectionBox;
		}

		/// <inheritdoc/>
		public bool ShowCrossHair
		{
			get => m_owner.SceneView.ShowCrossHair;
			set => m_owner.SceneView.ShowCrossHair = value;
		}
		public ICommand ToggleShowCrossHair
		{
			get => m_owner.SceneView.ToggleShowCrossHair;
		}

		/// <inheritdoc/>
		public bool ShowValueAtPointer
		{
			get => m_owner.SceneView.ShowValueAtPointer;
			set => m_owner.SceneView.ShowValueAtPointer = value;
		}
		public ICommand ToggleShowValueAtPointer
		{
			get =>m_owner.SceneView.ToggleShowValue;
		}

		/// <inheritdoc/>
		public bool ShowAxes
		{
			get => m_owner.SceneState.Chart.ShowAxes;
			set => m_owner.SceneState.Chart.ShowAxes = value;
		}
		public ICommand ToggleAxes
		{
			get => m_owner.SceneView.ToggleAxes;
		}

		/// <inheritdoc/>
		public bool ShowGridLines
		{
			get => m_owner.SceneState.Chart.ShowGridLines;
			set => m_owner.SceneState.Chart.ShowGridLines = value;
		}
		public ICommand ToggleGridLines
		{
			get => m_owner.SceneView.ToggleGridLines;
		}

		/// <inheritdoc/>
		public bool Orthographic
		{
			get => m_owner.SceneState.Chart.Orthographic;
			set => m_owner.SceneState.Chart.Orthographic = value;
		}
		public ICommand ToggleOrthographic
		{
			get => m_owner.SceneView.ToggleOrthographic;
		}

		/// <inheritdoc/>
		public bool Antialiasing
		{
			get => m_owner.SceneState.Chart.Antialiasing;
			set => m_owner.SceneState.Chart.Antialiasing = value;
		}
		public ICommand ToggleAntialiasing
		{
			get => m_owner.SceneView.ToggleAntialiasing;
		}

		/// <inheritdoc/>
		public bool LockAspect
		{
			get => m_owner.SceneView.LockAspect;
			set => m_owner.SceneView.LockAspect = value;
		}
		public ICommand ToggleLockAspect
		{
			get => m_owner.SceneView.ToggleLockAspect;
		}

		/// <inheritdoc/>
		public bool MouseCentredZoom
		{
			get => m_owner.SceneState.Chart.MouseCentredZoom;
			set => m_owner.SceneState.Chart.MouseCentredZoom = value;
		}
		public ICommand ToggleMouseCentredZoom
		{
			get => m_owner.SceneView.ToggleMouseCentredZoom;
		}

		/// <inheritdoc/>
		public Colour32 BackgroundColour
		{
			get => m_owner.SceneState.Chart.BackgroundColour;
			set => m_owner.SceneState.Chart.BackgroundColour = value;
		}
		public ICommand SetBackgroundColour
		{
			get => m_owner.SceneView.SetBackgroundColour;
		}

		/// <inheritdoc/>
		public ICommand ShowMeasureToolUI
		{
			get => m_owner.SceneView.ShowMeasureToolUI;
		}

		/// <inheritdoc/>
		public ICommand ShowAnimationUI
	{
			get => m_owner.ToggleAnimationUI;
		}

		/// <inheritdoc/>
		public ICommand ShowLightingUI
		{
			get => m_owner.ShowLightingUI;
		}

		/// <inheritdoc/>
		public ICommand ShowObjectManagerUI
		{
			get => m_owner.SceneView.Scene.ShowObjectManagerUI;
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
		public ICommand AutoRange
		{
			get => m_owner.SceneView.DoAutoRange;
		}

		/// <inheritdoc/>
		public ICommand DoAspect11
		{
			get => m_owner.SceneView.DoAspect11;
		}

		/// <inheritdoc/>
		public bool CanLinkCamera
		{
			get => m_owner.Model.Scenes.Count > 1;
		}
		public ICommand LinkCamera
		{
			get => m_owner.LinkCamera;
		}

		/// <inheritdoc/>
		public EAlignDirection AlignDirection
		{
			get => m_owner.SceneState.AlignDirection;
			set => m_owner.SceneState.AlignDirection = value;
		}

		/// <inheritdoc/>
		public EViewPreset ViewPreset
		{
			get => m_owner.SceneState.ViewPreset;
			set => m_owner.SceneState.ViewPreset = value;
		}

		/// <inheritdoc/>
		public View3d.EFillMode FillMode
		{
			get => m_owner.SceneState.Chart.FillMode;
			set => m_owner.SceneState.Chart.FillMode = value;
		}

		/// <inheritdoc/>
		public View3d.ECullMode CullMode
		{
			get => m_owner.SceneState.Chart.CullMode;
			set => m_owner.SceneState.Chart.CullMode = value;
		}

		/// <summary>Saved views</summary>
		public ICollectionView SavedViews
		{
			get => m_owner.SceneView.Scene.SavedViews;
		}
		public ICommand ApplySavedView
		{
			get => m_owner.SceneView.Scene.ApplySavedView;
		}
		public ICommand SaveCurrentView
		{
			get => m_owner.SceneView.Scene.SaveCurrentView;
		}
		public ICommand RemoveSavedView
		{
			get => m_owner.SceneView.Scene.RemoveSavedView;
		}

		/// <inheritdoc/>
		public ChartControl.ENavMode NavigationMode
		{
			get => m_owner.SceneState.Chart.NavigationMode;
			set => m_owner.SceneState.Chart.NavigationMode = value;
		}

		/// <inheritdoc/>
		public bool BBoxesVisible
		{
			get => m_owner.SceneView.Scene.Window.Diag.BBoxesVisible;
			set => m_owner.SceneView.Scene.Window.Diag.BBoxesVisible = value;
		}
		public ICommand ToggleBBoxesVisible
		{
			get => m_owner.SceneView.Scene.ToggleBBoxesVisible;
		}

		/// <inheritdoc/>
		public float NormalsLength
		{
			get => m_owner.SceneView.Scene.Window.Diag.NormalsLength;
			set => m_owner.SceneView.Scene.Window.Diag.NormalsLength = value;
		}

		/// <inheritdoc/>
		public Colour32 NormalsColour
		{
			get => m_owner.SceneView.Scene.Window.Diag.NormalsColour;
			set => m_owner.SceneView.Scene.Window.Diag.NormalsColour = value;
		}
		public ICommand SetNormalsColour
		{
			get => m_owner.SceneView.Scene.SetNormalsColour;
		}

		/// <inheritdoc/>
		public float FillModePointsSize
		{
			get => m_owner.SceneView.Scene.Window.Diag.FillModePointsSize.x;
			set => m_owner.SceneView.Scene.Window.Diag.FillModePointsSize = new v2(value, value);
		}
	}
	public class SceneAxisCMenu :IChartAxisCMenu
	{
		private readonly SceneUI m_owner;
		private readonly ChartControl.EAxis m_axis;
		private readonly AxisPanel m_axis_panel;
		public SceneAxisCMenu(SceneUI owner, ChartControl.EAxis axis)
		{
			m_owner = owner;
			m_axis = axis;
			m_axis_panel = owner.SceneView.GetAxisPanel(axis);

			//m_axis_panel.PropertyChanged += HandlePropertyChanged;
			//void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
			//{
			//	var prop_name = e.PropertyName switch
			//	{
			//		nameof(SceneUI.OtherScenes) => nameof(LinkToChart),
			//		_ => e.PropertyName,
			//	};
			//	NotifyPropertyChanged(prop_name);
			//}
		}
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <inheritdoc/>
		public bool AllowScroll
		{
			get => m_axis_panel.AllowScroll;
			set => m_axis_panel.AllowScroll = value;
		}
		public ICommand ToggleScrollLock
		{
			get => m_axis_panel.ToggleScrollLock;
		}

		/// <inheritdoc/>
		public bool AllowZoom
		{
			get => m_axis_panel.AllowZoom;
			set => m_axis_panel.AllowZoom = value;
		}
		public ICommand ToggleZoomLock
		{
			get => m_axis_panel.ToggleZoomLock;
		}
	}
}
