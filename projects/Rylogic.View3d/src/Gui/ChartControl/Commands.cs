using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Rylogic.Gfx;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl :IChartCMenu, IView3dCMenu
	{
		// Notes:
		//  - Many commands are available on View3dControl but I want to make the Options the source
		//    of truth for the scene settings, so the scene commands are implemented again here, usually
		//    setting the Options value first then forwarding to the View3dControl command.
		//  - Boolean properties can't forward the ICommand to View3d because the property changed
		//    notification occurs on the wrong object.

		/// <summary>Initialise the commands</summary>
		private void InitCommands()
		{
			// Tools Menu
			ToggleOriginPoint = Command.Create(this, ToggleOriginPointInternal);
			ToggleFocusPoint = Command.Create(this, ToggleFocusPointInternal);
			ToggleBBoxesVisible = Command.Create(this, ToggleBBoxesVisibleInternal);
			ToggleSelectionBox = Command.Create(this, ToggleSelectionBoxInternal);
			ToggleShowCrossHair = Command.Create(this, ToggleShowCrossHairInternal);
			ToggleShowValueAtPointer = Command.Create(this, ToggleShowValueAtPointerInternal);
			ToggleGridLines = Command.Create(this, ToggleGridLinesInternal);
			ToggleAxes = Command.Create(this, ToggleAxesInternal);
			ToggleShowTapeMeasure = Command.Create(this, ToggleShowTapeMeasureInternal);

			// Camera
			AutoRangeView = Command.Create(this, AutoRangeViewInternal);
			DoAspect11 = Command.Create(this, DoAspect11Internal);
			ToggleLockAspect = Command.Create(this, ToggleLockAspectInternal);
			ToggleOrthographic = Command.Create(this, ToggleOrthographicInternal);
			ToggleMouseCentredZoom = Command.Create(this, ToggleMouseCentredZoomInternal);

			// Rendering
			SetBackgroundColour = Command.Create(this, SetBackgroundColourInternal);
			ToggleAntialiasing = Command.Create(this, ToggleAntiAliasingInternal);
			ToggleShowNormals = Command.Create(this, ToggleShowNormalsInternal);
		}

		/// <summary>Set default cmenu data contexts</summary>
		private void InitCMenus()
		{
			if (Scene.ContextMenu != null && Scene.ContextMenu.DataContext == null)
				Scene.ContextMenu.DataContext = this;
			if (XAxisPanel.ContextMenu != null && XAxisPanel.ContextMenu.DataContext == null)
				XAxisPanel.ContextMenu.DataContext = XAxisPanel;
			if (YAxisPanel.ContextMenu != null && YAxisPanel.ContextMenu.DataContext == null)
				YAxisPanel.ContextMenu.DataContext = YAxisPanel;
		}

		/// <summary>Accessors for setting the context menus in XAML</summary>
		public ContextMenu SceneCMenu
		{
			get => Scene.ContextMenu;
			set => Scene.ContextMenu = value;
		}
		public ContextMenu XAxisCMenu
		{
			get => XAxisPanel.ContextMenu;
			set => XAxisPanel.ContextMenu = value;
		}
		public ContextMenu YAxisCMenu
		{
			get => YAxisPanel.ContextMenu;
			set => YAxisPanel.ContextMenu = value;
		}

		/// <inheritdoc/>
		public bool OriginPointVisible
		{
			get => Options.OriginPointVisible;
			set
			{
				if (OriginPointVisible == value) return;
				Options.OriginPointVisible = value;
				NotifyPropertyChanged(nameof(OriginPointVisible));
			}
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
			get => Options.FocusPointVisible;
			set
			{
				if (FocusPointVisible == value) return;
				Options.FocusPointVisible = value;
				NotifyPropertyChanged(nameof(FocusPointVisible));
			}
		}
		public ICommand ToggleFocusPoint { get; private set; } = null!;
		private void ToggleFocusPointInternal()
		{
			FocusPointVisible = !FocusPointVisible;
			Invalidate();
		}

		/// <summary>Toggle visibility of the focus point</summary>
		public bool BBoxesVisible
		{
			get => Scene.BBoxesVisible;
			set
			{
				if (BBoxesVisible == value) return;
				Scene.BBoxesVisible = value;
				NotifyPropertyChanged(nameof(BBoxesVisible));
			}
		}
		public ICommand ToggleBBoxesVisible { get; private set; } = null!;
		private void ToggleBBoxesVisibleInternal()
		{
			BBoxesVisible = !BBoxesVisible;
			Invalidate();
		}

		/// <summary>Toggle visibility of the focus point</summary>
		public bool SelectionBoxVisible
		{
			get => Window.SelectionBoxVisible;
			set
			{
				if (SelectionBoxVisible == value) return;
				Window.SelectionBoxVisible = value;
				NotifyPropertyChanged(nameof(SelectionBoxVisible));
			}
		}
		public ICommand ToggleSelectionBox { get; private set; } = null!;
		private void ToggleSelectionBoxInternal()
		{
			SelectionBoxVisible = !SelectionBoxVisible;
			Invalidate();
		}

		/// <inheritdoc/>
		public bool ShowCrossHair
		{
			get => m_xhair != null;
			set
			{
				if (ShowCrossHair == value) return;
				if (m_xhair != null)
				{
					MouseMove -= OnMouseMoveCrossHair;
					MouseWheel -= OnMouseWheelCrossHair;
					Cursor = Cursors.Arrow;
					Util.Dispose(ref m_xhair);
				}
				m_xhair = value ? new CrossHair(this) : null;
				if (m_xhair != null)
				{
					Cursor = Cursors.Cross;
					MouseWheel += OnMouseWheelCrossHair;
					MouseMove += OnMouseMoveCrossHair;
				}
				NotifyPropertyChanged(nameof(ShowCrossHair));

				// Handlers
				void OnMouseMoveCrossHair(object? sender, MouseEventArgs e)
				{
					var location = e.GetPosition(this);
					if (m_xhair != null && SceneBounds.Contains(location))
						m_xhair.PositionCrossHair(location);
				}
				void OnMouseWheelCrossHair(object? sender, MouseEventArgs e)
				{
					var location = e.GetPosition(this);
					if (m_xhair != null)
						m_xhair.PositionCrossHair(location);
				}
			}
		}
		public ICommand ToggleShowCrossHair { get; private set; } = null!;
		private void ToggleShowCrossHairInternal()
		{
			ShowCrossHair = !ShowCrossHair;
		}
		private CrossHair? m_xhair;

		/// <inheritdoc/>
		public bool ShowValueAtPointer
		{
			get => m_show_value_at_pointer;
			set
			{
				if (m_show_value_at_pointer == value) return;
				if (m_show_value_at_pointer)
				{
					m_chart_panel.MouseMove -= HandleMouseMove;
					m_popup_show_value.IsOpen = false;
				}
				m_show_value_at_pointer = value;
				if (m_show_value_at_pointer)
				{
					m_popup_show_value.IsOpen = true;
					m_chart_panel.MouseMove += HandleMouseMove;
				}
				NotifyPropertyChanged(nameof(ShowValueAtPointer));

				// Handlers
				void HandleMouseMove(object sender, MouseEventArgs e)
				{
					var pos = e.GetPosition(m_chart_panel);
					m_popup_show_value.HorizontalOffset = pos.X + 10;
					m_popup_show_value.VerticalOffset = pos.Y + 20;
				}
			}
		}
		public ICommand ToggleShowValueAtPointer { get; private set; } = null!;
		private void ToggleShowValueAtPointerInternal()
		{
			ShowValueAtPointer = !ShowValueAtPointer;
		}
		private bool m_show_value_at_pointer;

		/// <inheritdoc/>
		public bool ShowAxes
		{
			get => Options.ShowAxes;
			set
			{
				if (ShowAxes == value) return;
				Options.ShowAxes = value;
				NotifyPropertyChanged(nameof(ShowAxes));
			}
		}
		public ICommand ToggleAxes { get; private set; } = null!;
		private void ToggleAxesInternal()
		{
			ShowAxes = !ShowAxes;
			Invalidate();
		}

		/// <inheritdoc/>
		public bool ShowGridLines
		{
			get => Options.ShowGridLines;
			set
			{
				if (ShowGridLines == value) return;
				Options.ShowGridLines = value;
				NotifyPropertyChanged(nameof(ShowGridLines));
			}
		}
		public ICommand ToggleGridLines { get; private set; } = null!;
		private void ToggleGridLinesInternal()
		{
			ShowGridLines = !ShowGridLines;
			Invalidate();
		}

		/// <summary>Display the measurement tool</summary>
		public ICommand ShowMeasureToolUI => Scene.ShowMeasureToolUI;

		/// <summary>Tape measure tool</summary>
		public bool ShowTapeMeasure
		{
			get => m_tape != null;
			set
			{
				if (ShowTapeMeasure == value) return;
				if (ShowTapeMeasure)
				{
					Cursor = Cursors.Arrow;
					Util.Dispose(ref m_tape);
				}
				m_tape = value ? new TapeMeasure(this) : null;
				if (ShowTapeMeasure)
				{
					Cursor = Cursors.Cross;
				}
				NotifyPropertyChanged(nameof(ShowTapeMeasure));
			}
		}
		public ICommand ToggleShowTapeMeasure { get; private set; } = null!;
		private void ToggleShowTapeMeasureInternal()
		{
			ShowTapeMeasure = !ShowTapeMeasure;
		}
		private TapeMeasure? m_tape;

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
		public ICommand AutoRangeView { get; private set; } = null!;
		private void AutoRangeViewInternal(object? parameter)
		{
			var bounds = parameter is View3d.ESceneBounds b ? b : View3d.ESceneBounds.All;
			AutoRange(who: bounds);
		}
		private View3d.ESceneBounds m_AutoRangeBounds;

		/// <inheritdoc/>
		public ICommand DoAspect11 { get; private set; } = null!;
		private void DoAspect11Internal()
		{
			Range.Aspect = Scene.Window.Viewport.Width / Scene.Window.Viewport.Height;
			SetCameraFromRange();
			Invalidate();
		}

		/// <inheritdoc/>
		public bool LockAspect
		{
			get => Options.LockAspect != null;
			set
			{
				if (LockAspect == value) return;
				Options.LockAspect = value ? (XAxis.Span * SceneBounds.Height) / (YAxis.Span * SceneBounds.Width) : (double?)null;
				NotifyPropertyChanged(nameof(LockAspect));
			}
		}
		public ICommand ToggleLockAspect { get; private set; } = null!;
		private void ToggleLockAspectInternal()
		{
			LockAspect = !LockAspect;
		}

		/// <inheritdoc/>
		public bool Orthographic
		{
			get => Options.Orthographic;
			set
			{
				if (Orthographic == value) return;
				Options.Orthographic = value;
				NotifyPropertyChanged(nameof(Orthographic));
			}
		}
		public ICommand ToggleOrthographic { get; private set; } = null!;
		private void ToggleOrthographicInternal()
		{
			Orthographic = !Orthographic;
		}

		/// <inheritdoc/>
		public bool MouseCentredZoom
		{
			get => Options.MouseCentredZoom;
			set
			{
				if (MouseCentredZoom == value) return;
				Options.MouseCentredZoom = value;
				NotifyPropertyChanged(nameof(MouseCentredZoom));
			}
		}
		public ICommand ToggleMouseCentredZoom { get; private set; } = null!;
		private void ToggleMouseCentredZoomInternal()
		{
			MouseCentredZoom = !MouseCentredZoom;
		}

		/// <inheritdoc/>
		public ENavMode NavigationMode
		{
			get => Options.NavigationMode;
			set
			{
				if (NavigationMode == value) return;
				Options.NavigationMode = value;
				NotifyPropertyChanged(nameof(NavigationMode));
			}
		}

		/// <inheritdoc/>
		public Colour32 BackgroundColour
		{
			get => Options.BackgroundColour;
			set
			{
				if (BackgroundColour == value) return;
				Options.BackgroundColour = value;
				NotifyPropertyChanged(nameof(BackgroundColour));
			}
		}
		public ICommand SetBackgroundColour { get; private set; } = null!;
		private void SetBackgroundColourInternal()
		{
			Scene.SetBackgroundColour.Execute(null);
			BackgroundColour = Window.BackgroundColour;
		}

		/// <inheritdoc/>
		public bool Antialiasing
		{
			get => Options.Antialiasing;
			set
			{
				if (Antialiasing == value) return;
				Options.Antialiasing = value;
				NotifyPropertyChanged(nameof(Antialiasing));
			}
		}
		public ICommand ToggleAntialiasing { get; private set; } = null!;
		private void ToggleAntiAliasingInternal()
		{
			Antialiasing = !Antialiasing;
		}

		/// <inheritdoc/>
		public double ShadowCastRange
		{
			get => Options.ShadowCastRange;
			set
			{
				if (ShadowCastRange == value) return;
				Options.ShadowCastRange = value;
				NotifyPropertyChanged(nameof(ShadowCastRange));
			}
		}

		/// <inheritdoc/>
		public EAlignDirection AlignDirection
		{
			get => Scene.AlignDirection;
			set
			{
				if (AlignDirection == value) return;
				Scene.AlignDirection = value;
				NotifyPropertyChanged(nameof(AlignDirection));
			}
		}

		/// <inheritdoc/>
		public EViewPreset ViewPreset
		{
			get => Scene.ViewPreset;
			set
			{
				if (ViewPreset == value) return;
				Scene.ViewPreset = value;
				NotifyPropertyChanged(nameof(ViewPreset));
			}
		}

		/// <summary>Saved views</summary>
		public ICollectionView SavedViews => Scene.SavedViews;
		public ICommand ApplySavedView => Scene.ApplySavedView;
		public ICommand SaveCurrentView => Scene.SaveCurrentView;
		public ICommand RemoveSavedView => Scene.RemoveSavedView;

		/// <inheritdoc/>
		public View3d.EFillMode FillMode
		{
			get => Options.FillMode;
			set
			{
				if (FillMode == value) return;
				Options.FillMode = value;
				NotifyPropertyChanged(nameof(FillMode));
			}
		}

		/// <inheritdoc/>
		public View3d.ECullMode CullMode
		{
			get => Options.CullMode;
			set
			{
				if (CullMode == value) return;
				Options.CullMode = value;
				NotifyPropertyChanged(nameof(CullMode));
			}
		}

		/// <inheritdoc/>
		public bool ShowNormals
		{
			get => Scene.ShowNormals;
			set
			{
				if (ShowNormals == value) return;
				Scene.ShowNormals = value;
				NotifyPropertyChanged(nameof(ShowNormals));
			}
		}
		public ICommand ToggleShowNormals { get; private set; } = null!;
		private void ToggleShowNormalsInternal()
		{
			ShowNormals = !ShowNormals;
			Invalidate();
		}

		/// <inheritdoc/>
		public float NormalsLength
		{
			get => Scene.NormalsLength;
			set
			{
				if (NormalsLength == value) return;
				Scene.NormalsLength = value;
				NotifyPropertyChanged(nameof(NormalsLength));
			}
		}

		/// <inheritdoc/>
		public Colour32 NormalsColour
		{
			get => Scene.NormalsColour;
			set
			{
				if (NormalsColour == value) return;
				Scene.NormalsColour = value;
				NotifyPropertyChanged(nameof(NormalsColour));
			}
		}
		public ICommand SetNormalsColour => Scene.SetNormalsColour;

		/// <inheritdoc/>
		public float FillModePointsSize
		{
			get => Scene.FillModePointsSize;
			set
			{
				if (FillModePointsSize == value) return;
				Scene.FillModePointsSize = value;
				NotifyPropertyChanged(nameof(FillModePointsSize));
			}
		}

		/// <inheritdoc/>
		public ICommand ShowAnimationUI => Scene.ShowAnimationUI;

		/// <inheritdoc/>
		public ICommand ShowLightingUI => Scene.ShowLightingUI;

		/// <inheritdoc/>
		public ICommand ShowObjectManagerUI => Scene.ShowObjectManagerUI;

		/// <inheritdoc/>
		public bool CanLinkCamera => false;
		public ICommand LinkCamera => Command.NoOp;
	}
}
