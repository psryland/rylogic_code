using System.ComponentModel;
using System.Windows.Input;
using Rylogic.Gfx;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
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
				get => m_owner.Window.Diag.BBoxesVisible;
				set => m_owner.Window.Diag.BBoxesVisible = value;
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
				get => m_owner.Scene.SetBackgroundColour;
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

			/// <inheritdoc/>
			public float NormalsLength
			{
				get => m_owner.Window.Diag.NormalsLength;
				set => m_owner.Window.Diag.NormalsLength = value;
			}

			/// <inheritdoc/>
			public Colour32 NormalsColour
			{
				get => m_owner.Window.Diag.NormalsColour;
				set => m_owner.Window.Diag.NormalsColour = value;
			}
			public ICommand SetNormalsColour
			{
				get => m_owner.Scene.SetNormalsColour;
			}

			/// <inheritdoc/>
			public float FillModePointsSize
			{
				get => m_owner.Window.Diag.FillModePointsSize.x;
				set => m_owner.Window.Diag.FillModePointsSize = new v2(value, value);
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

		/// <summary>Axis context menu</summary>
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
