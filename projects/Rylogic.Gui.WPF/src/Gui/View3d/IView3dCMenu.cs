using System.ComponentModel;
using System.Windows.Input;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public interface IView3dCMenu :INotifyPropertyChanged
	{
		// Note:
		//  - If adding a new context menu option, you'll need to do:
		//       - Add a MenuItem resource to 'ContextMenus.xaml'
		//       - Add any binding variables to 'IView3dCMenu'
		//       - Implement the functionality in the owner, and create forwarding
		//         properties in the implementations of this interface.
		//       - Find the appropriate 'HandleSettingChanged' or 'NotifyPropertyChanged'
		//         method to add a case to, and call the 'NotifyPropertyChanged' on this
		//         object via:
		//             if (ContextMenu.DataContext is IView3dCMenu cmenu)
		//                  cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.<prop_name>))
		//
		//  - This is mainly just to ensure the required binding functions exist. DataTemplate cannot
		//    bind to interfaces so you have to implement this interface with implicit implementations,
		//    not explicit (e.g. bool IView3dCMenu.OriginPointVisible => ... won't work ).
		//  - Typical implementations contain a reference to the owning scene/chart/etc. These implementations
		//    cannot subscribe to events on the owner because there is no way to detect the ContextMenu being
		//    replaced, so any subscriptions would remain on the owner preventing the old CMenu implementation
		//    from being disposed.

		/// <summary>Allow property changed to be triggered externally</summary>
		void NotifyPropertyChanged(string prop_name);

		/// <summary>Origin</summary>
		bool OriginPointVisible { get; set; }
		ICommand ToggleOriginPoint { get; }

		/// <summary>Focus</summary>
		bool FocusPointVisible { get; set; }
		ICommand ToggleFocusPoint { get; }

		/// <summary>Bounding boxes</summary>
		bool BBoxesVisible { get; set; }
		ICommand ToggleBBoxesVisible { get; }

		/// <summary>Selection box</summary>
		bool SelectionBoxVisible { get; set; }
		ICommand ToggleSelectionBox { get; }

		/// <summary>Camera Reset</summary>
		ICommand ResetView { get; }

		/// <summary>Camera orthographic</summary>
		bool Orthographic { get; set; }
		ICommand ToggleOrthographic { get; }

		/// <summary>Directions to align the camera up-axis to</summary>
		EAlignDirection AlignDirection { get; set; }

		/// <summary>Preset views to set the camera to</summary>
		EViewPreset ViewPreset { get; set; }

		/// <summary>The view background colour</summary>
		Colour32 BackgroundColour { get; set; }
		ICommand SetBackgroundColour { get; }

		/// <summary>Enable/Disable antialiasing</summary>
		bool Antialiasing { get; set; }
		ICommand ToggleAntialiasing { get; }

		/// <summary>Fill mode</summary>
		View3d.EFillMode FillMode { get; set; }

		/// <summary>Face culling</summary>
		View3d.ECullMode CullMode { get; set; }

		/// <summary>Saved views</summary>
		ICollectionView SavedViews { get; }
		ICommand ApplySavedView { get; }
		ICommand SaveCurrentView { get; }
		ICommand RemoveSavedView { get; }

		/// <summary>Show the animation controls</summary>
		ICommand ShowAnimationUI { get; }

		/// <summary>Show lighting properties</summary>
		ICommand ShowLightingUI { get; }

		/// <summary>Show the measurement tool</summary>
		ICommand ShowMeasureToolUI { get; }

		/// <summary>Show object manager</summary>
		ICommand ShowObjectManagerUI { get; }
	}
}
