using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		// Notes:
		//  - Many commands are available on View3dControl but I want to make
		//    the Options the source of truth for the scene settings, so the
		//    scene commands are implemented again here, usually setting the
		//    Options value first then forwarding to the View3dControl command.

		/// <summary>Initialise the commands</summary>
		private void InitCommands()
		{
			// Objects Menu
			ToggleOriginPoint = Command.Create(this, ToggleOriginPointInternal);
			ToggleFocusPoint = Command.Create(this, ToggleFocusPointInternal);
			ToggleGridLines = Command.Create(this, ToggleGridLinesInternal);
			ToggleAxes = Command.Create(this, ToggleAxesInternal);

			// Tools Menu
			ToggleShowValue = Command.Create(this, ToggleShowValueInternal);
			ToggleShowCrossHair = Command.Create(this, ToggleShowCrossHairInternal);
			ToggleShowTapeMeasure = Command.Create(this, ToggleShowTapeMeasureInternal);

			// Zoom Menu
			DoAutoRange = Command.Create(this, DoAutoRangeInternal);
			DoAspect11 = Command.Create(this, DoAspect11Internal);
			ToggleLockAspect = Command.Create(this, ToggleLockAspectInternal);
			ToggleMouseCentredZoom = Command.Create(this, ToggleMouseCentredZoomInternal);

			// Rendering
			SetBackgroundColour = Command.Create(this, SetBackgroundColourInternal);
			ToggleOrthographic = Command.Create(this, ToggleOrthographicInternal);
			ToggleAntialiasing = Command.Create(this, ToggleAntiAliasingInternal);

			Scene.ContextMenu = this.FindCMenu("ChartCMenu", new ChartCMenu(this));
			XAxisPanel.ContextMenu = this.FindCMenu("ChartAxisCMenu", new ChartAxisCMenu(XAxisPanel));
			YAxisPanel.ContextMenu = this.FindCMenu("ChartAxisCMenu", new ChartAxisCMenu(YAxisPanel));
		}

		/// <summary>Toggle visibility of the origin point</summary>
		public Command ToggleOriginPoint { get; private set; } = null!;
		private void ToggleOriginPointInternal()
		{
			Options.OriginPointVisible = !Options.OriginPointVisible;
			Invalidate();
		}

		/// <summary>Toggle visibility of the focus point</summary>
		public Command ToggleFocusPoint { get; private set; } = null!;
		private void ToggleFocusPointInternal()
		{
			Options.FocusPointVisible = !Options.FocusPointVisible;
			Invalidate();
		}

		/// <summary>Toggle visibility of the grid lines</summary>
		public Command ToggleGridLines { get; private set; } = null!;
		private void ToggleGridLinesInternal()
		{
			Options.ShowGridLines = !Options.ShowGridLines;
			Invalidate();
		}

		/// <summary>Toggle visibility of the axes</summary>
		public Command ToggleAxes { get; private set; } = null!;
		private void ToggleAxesInternal()
		{
			Options.ShowAxes = !Options.ShowAxes;
			Invalidate();
		}

		/// <summary>Toggle the visibility of the value at the pointer</summary>
		public Command ToggleShowValue { get; private set; } = null!;
		private void ToggleShowValueInternal()
		{
			ShowValueAtPointer = !ShowValueAtPointer;
		}

		/// <summary>Toggle the visibility of the cross hair</summary>
		public Command ToggleShowCrossHair { get; private set; } = null!;
		private void ToggleShowCrossHairInternal()
		{
			ShowCrossHair = !ShowCrossHair;
		}

		/// <summary>Toggle the tape measure tool</summary>
		public Command ToggleShowTapeMeasure { get; private set; } = null!;
		private void ToggleShowTapeMeasureInternal()
		{
			ShowTapeMeasure = !ShowTapeMeasure;
		}

		/// <summary>Display the measurement tool</summary>
		public Command ShowMeasureToolUI => Scene.ShowMeasureToolUI;

		/// <summary>Auto range the chart</summary>
		public Command DoAutoRange { get; private set; } = null!;
		private void DoAutoRangeInternal()
		{
			AutoRange();
		}

		/// <summary>Set the pixel aspect ratio to 1:1</summary>
		public Command DoAspect11 { get; private set; } = null!;
		private void DoAspect11Internal()
		{
			Range.Aspect = Scene.Window.Viewport.Width / Scene.Window.Viewport.Height;
			SetCameraFromRange();
			Invalidate();
		}

		/// <summary>Toggle the state of 'LockAspect'</summary>
		public Command ToggleLockAspect { get; private set; } = null!;
		private void ToggleLockAspectInternal()
		{
			LockAspect = !LockAspect;
		}

		/// <summary>Toggle the state of zooming at the mouse pointer location</summary>
		public Command ToggleMouseCentredZoom { get; private set; } = null!;
		private void ToggleMouseCentredZoomInternal()
		{
			Options.MouseCentredZoom = !Options.MouseCentredZoom;
		}

		/// <summary>Set the background colour</summary>
		public Command SetBackgroundColour { get; private set; } = null!;
		private void SetBackgroundColourInternal()
		{
			Scene.SetBackgroundColour.Execute(null);
			Options.BackgroundColour = Window.BackgroundColour;
		}

		/// <summary>Toggle between orthographic and perspective projection</summary>
		public Command ToggleOrthographic { get; private set; } = null!;
		private void ToggleOrthographicInternal()
		{
			Options.Orthographic = !Options.Orthographic;
		}

		/// <summary>Toggle antialiasing on/off</summary>
		public Command ToggleAntialiasing { get; private set; } = null!;
		private void ToggleAntiAliasingInternal()
		{
			Options.Antialiasing = !Options.Antialiasing;
		}

		/// <summary>Context menu binding</summary>
		public class ChartCMenu :IView3dCMenu, IChartCMenu
		{
			// Notes:
			//  - Don't sign up to events on 'm_owner' because that causes leaked references.
			//    When the context menu gets replaced. Instead, have the owner call
			//    'NotifyPropertyChanged'.

			private readonly ChartControl m_owner;
			public ChartCMenu(ChartControl owner)
			{
				m_owner = owner;
			}

			/// <summary></summary>
			public event PropertyChangedEventHandler? PropertyChanged;
			public void NotifyPropertyChanged(string prop_name)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
			}

			#region IView3dCMenu

			/// <inheritdoc/>
			public bool OriginPointVisible
			{
				get => m_owner.Options.OriginPointVisible;
				set => m_owner.Options.OriginPointVisible = value;
			}
			public ICommand ToggleOriginPoint
			{
				get => m_owner.ToggleOriginPoint;
			}

			/// <inheritdoc/>
			public bool FocusPointVisible
			{
				get => m_owner.Options.FocusPointVisible;
				set => m_owner.Options.FocusPointVisible = value;
			}
			public ICommand ToggleFocusPoint
			{
				get => m_owner.ToggleFocusPoint;
			}

			/// <summary>Toggle visibility of the focus point</summary>
			public bool BBoxesVisible
			{
				get => m_owner.Window.BBoxesVisible;
				set => m_owner.Window.BBoxesVisible = value;
			}
			public ICommand ToggleBBoxesVisible
			{
				get => m_owner.Scene.ToggleBBoxesVisible;
			}

			/// <summary>Toggle visibility of the focus point</summary>
			public bool SelectionBoxVisible
			{
				get => m_owner.Window.SelectionBoxVisible;
				set => m_owner.Window.SelectionBoxVisible = value;
			}
			public ICommand ToggleSelectionBox
			{
				get => m_owner.Scene.ToggleSelectionBox;
			}

			/// <inheritdoc/>
			public ICommand ResetView
			{
				get => m_owner.DoAutoRange;
			}

			/// <inheritdoc/>
			public bool Orthographic
			{
				get => m_owner.Options.Orthographic;
				set => m_owner.Options.Orthographic = value;
			}
			public ICommand ToggleOrthographic
			{
				get => m_owner.ToggleOrthographic;
			}

			/// <inheritdoc/>
			public EAlignDirection AlignDirection
			{
				get => EAlignDirection.None;
				set { }
			}

			/// <inheritdoc/>
			public EViewPreset ViewPreset
			{
				get => EViewPreset.Current;
				set { }
			}

			/// <inheritdoc/>
			public Colour32 BackgroundColour
			{
				get => m_owner.Options.BackgroundColour;
				set => m_owner.Options.BackgroundColour = value;
			}
			public ICommand SetBackgroundColour
			{
				get => m_owner.SetBackgroundColour;
			}

			/// <inheritdoc/>
			public bool Antialiasing
			{
				get => m_owner.Options.Antialiasing;
				set => m_owner.Options.Antialiasing = value;
			}
			public ICommand ToggleAntialiasing
			{
				get => m_owner.ToggleAntialiasing;
			}

			/// <inheritdoc/>
			public View3d.EFillMode FillMode
			{
				get => m_owner.Options.FillMode;
				set => m_owner.Options.FillMode = value;
			}

			/// <inheritdoc/>
			public View3d.ECullMode CullMode
			{
				get => m_owner.Options.CullMode;
				set => m_owner.Options.CullMode = value;
			}

			/// <summary>Saved views</summary>
			public ICollectionView SavedViews
			{
				get => m_owner.Scene.SavedViews;
			}
			public ICommand ApplySavedView
			{
				get => m_owner.Scene.ApplySavedView;
			}
			public ICommand RemoveSavedView
			{
				get => m_owner.Scene.RemoveSavedView;
			}
			public ICommand SaveCurrentView
			{
				get => m_owner.Scene.SaveCurrentView;
			}

			/// <inheritdoc/>
			public ICommand ShowAnimationUI
			{
				get => Command.NoOp;
			}

			/// <inheritdoc/>
			public ICommand ShowLightingUI
			{
				get => Command.NoOp;
			}

			/// <inheritdoc/>
			public ICommand ShowMeasureToolUI
			{
				get => Command.NoOp;
			}

			/// <inheritdoc/>
			public ICommand ShowObjectManagerUI
			{
				get => Command.NoOp;
			}

			#endregion

			#region IChartCMenu

			/// <inheritdoc/>
			public bool ShowGridLines
			{
				get => m_owner.Options.ShowGridLines;
				set => m_owner.Options.ShowGridLines = value;
			}
			public ICommand ToggleGridLines
			{
				get => m_owner.ToggleGridLines;
			}

			/// <inheritdoc/>
			public bool ShowAxes
			{
				get => m_owner.Options.ShowAxes;
				set => m_owner.Options.ShowAxes = value;
			}
			public ICommand ToggleAxes
			{
				get => m_owner.ToggleAxes;
			}

			/// <inheritdoc/>
			public bool ShowValueAtPointer
			{
				get => m_owner.ShowValueAtPointer;
				set => m_owner.ShowValueAtPointer = value;
			}
			public ICommand ToggleShowValueAtPointer
			{
				get => m_owner.ToggleShowValue;
			}

			/// <inheritdoc/>
			public bool ShowCrossHair
			{
				get => m_owner.ShowCrossHair;
				set => m_owner.ShowCrossHair = value;
			}
			public ICommand ToggleShowCrossHair
			{
				get => m_owner.ToggleShowCrossHair;
			}

			/// <inheritdoc/>
			public ENavMode NavigationMode
			{
				get => m_owner.Options.NavigationMode;
				set => m_owner.Options.NavigationMode = value;
			}

			/// <inheritdoc/>
			public ICommand DoAutoRange
			{
				get => m_owner.DoAutoRange;
			}

			/// <inheritdoc/>
			public ICommand DoAspect11
			{
				get => m_owner.DoAspect11;
			}

			/// <inheritdoc/>
			public bool LockAspect
			{
				get => m_owner.LockAspect;
				set => m_owner.LockAspect = value;
			}
			public ICommand ToggleLockAspect
			{
				get => m_owner.ToggleLockAspect;
			}

			/// <inheritdoc/>
			public bool MouseCentredZoom
			{
				get => m_owner.Options.MouseCentredZoom;
				set => m_owner.Options.MouseCentredZoom = value;
			}
			public ICommand ToggleMouseCentredZoom
			{
				get => m_owner.ToggleMouseCentredZoom;
			}

			/// <inheritdoc/>
			public bool CanLinkCamera
			{
				get => false;
			}
			public ICommand LinkCamera
			{
				get => Command.NoOp;
			}

			#endregion
		}
		public class ChartAxisCMenu :IChartAxisCMenu
		{
			private readonly ChartDetail.AxisPanel m_owner;
			public ChartAxisCMenu(ChartDetail.AxisPanel owner)
			{
				m_owner = owner;
			}

			/// <summary></summary>
			public event PropertyChangedEventHandler? PropertyChanged;
			public void NotifyPropertyChanged(string prop_name)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
			}

			/// <inheritdoc/>
			public bool AllowScroll
			{
				get => m_owner.AllowScroll;
				set => m_owner.AllowScroll = value;
			}
			public ICommand ToggleScrollLock
			{
				get => m_owner.ToggleScrollLock;
			}

			/// <inheritdoc/>
			public bool AllowZoom
			{
				get => m_owner.AllowZoom;
				set => m_owner.AllowZoom = value;
			}
			public ICommand ToggleZoomLock
			{
				get => m_owner.ToggleZoomLock;
			}
		}
	}
}
