using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Rylogic.Extn;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public partial class View3dControl
	{
		public class View3dCMenu :IView3dCMenu
		{
			// Notes:
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

			private readonly View3dControl m_owner;
			public View3dCMenu(View3dControl owner)
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
			public bool OriginPointVisible
			{
				get => m_owner.Window.OriginPointVisible;
				set => m_owner.Window.OriginPointVisible = value;
			}
			public ICommand ToggleOriginPoint
			{
				get => m_owner.ToggleOriginPoint;
			}

			/// <inheritdoc/>
			public bool FocusPointVisible
			{
				get => m_owner.Window.FocusPointVisible;
				set => m_owner.Window.FocusPointVisible = value;
			}
			public ICommand ToggleFocusPoint
			{
				get => m_owner.ToggleFocusPoint;
			}

			/// <inheritdoc/>
			public bool BBoxesVisible
			{
				get => m_owner.Window.BBoxesVisible;
				set => m_owner.Window.BBoxesVisible = value;
			}
			public ICommand ToggleBBoxesVisible
			{
				get => m_owner.ToggleBBoxesVisible;
			}

			/// <summary>Selection box</summary>
			public bool SelectionBoxVisible
			{
				get => m_owner.Window.SelectionBoxVisible;
				set => m_owner.Window.SelectionBoxVisible = value;
			}
			public ICommand ToggleSelectionBox
			{
				get => m_owner.ToggleSelectionBox;
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
				get => m_owner.ResetView;
			}

			/// <inheritdoc/>
			public bool Orthographic
			{
				get => m_owner.Camera.Orthographic;
				set => m_owner.Camera.Orthographic = value;
			}
			public ICommand ToggleOrthographic
			{
				get => m_owner.ToggleOrthographic;
			}

			/// <inheritdoc/>
			public EAlignDirection AlignDirection
			{
				get => m_owner.AlignDirection;
				set => m_owner.AlignDirection = value;
			}

			/// <inheritdoc/>
			public EViewPreset ViewPreset
			{
				get => m_owner.ViewPreset;
				set => m_owner.ViewPreset = value;
			}

			/// <inheritdoc/>
			public Colour32 BackgroundColour
			{
				get => m_owner.Window.BackgroundColour;
				set => m_owner.Window.BackgroundColour = value;
			}
			public ICommand SetBackgroundColour
			{
				get => m_owner.SetBackgroundColour;
			}

			/// <summary>Enable/Disable antialiasing</summary>
			public bool Antialiasing
			{
				get => m_owner.MultiSampling != 1;
				set => m_owner.MultiSampling = value ? 4 : 1;
			}
			public ICommand ToggleAntialiasing
			{
				get => m_owner.ToggleAntialiasing;
			}

			/// <inheritdoc/>
			public View3d.EFillMode FillMode
			{
				get => m_owner.Window.FillMode;
				set => m_owner.Window.FillMode = value;
			}

			/// <inheritdoc/>
			public View3d.ECullMode CullMode
			{
				get => m_owner.Window.CullMode;
				set => m_owner.Window.CullMode = value;
			}

			/// <summary>Saved views</summary>
			public ICollectionView SavedViews
			{
				get => m_owner.SavedViews;
			}
			public ICommand ApplySavedView
			{
				get => m_owner.ApplySavedView;
			}
			public ICommand SaveCurrentView
			{
				get => m_owner.SaveCurrentView;
			}
			public ICommand RemoveSavedView
			{
				get => m_owner.RemoveSavedView;
			}

			/// <inheritdoc/>
			public ICommand ShowAnimationUI
			{
				get => m_owner.ShowAnimationUI;
			}

			/// <inheritdoc/>
			public ICommand ShowLightingUI
			{
				get => m_owner.ShowLightingUI;
			}

			/// <inheritdoc/>
			public ICommand ShowMeasureToolUI
			{
				get => m_owner.ShowMeasureToolUI;
			}

			/// <inheritdoc/>
			public ICommand ShowObjectManagerUI
			{
				get => m_owner.ShowObjectManagerUI;
			}
		}
	}
}
