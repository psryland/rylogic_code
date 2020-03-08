using System.ComponentModel;
using System.Windows.Input;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public interface IView3dCMenu :INotifyPropertyChanged
	{
		// Architecture:
		//   Rylogic.Gui.WPF.Resources.ContextMenus - contains the XML for View3d and Chart menu items.
		//     These menu items are used in the context menus for View3dControl and ChartControl.
		// 
		//   IView3dCMenu - is the binding interface for context menu items common to View3d.
		//   IChartCMenu - is the binding interface for context menu items related to Charts.
		//      These interfaces declare the properties and commands referenced by the ContextMenu XML.
		//      An app needs to provide an object that implements these interfaces as the DataContext of
		//      a context menu that contains menu items from 'WPF.Resources.ContextMenus'.
		//   
		//   View3dCMenu - is an implementation of 'IView3DCMenu' used by basic View3dControls.
		//     View3dControl creates an instance of this object for its context menu.
		//     The View3dCMenu properties operate on the runtime state of 'View3dControl' and are not saved across restarts.
		//
		//   ChartControl.ChartCMenu - is an implementation of 'IView3DCMenu' and 'IChartCMenu'.
		//     ChartControl creates an instance of this object for its context menu.
		//     Most of the ChartCMenu properties forward to persisted Options properties which trigger refreshes when changed.
		//     Some of the properties of 'ChartControl.ChartCMenu' forward through to 'Scene.View3dCMenu' if they are not persisted.
		//
		//   LDraw.SceneCMenu - is LDraw's implementation of 'IView3DCMenu' and 'IChartCMenu'.
		//     ChartControl creates an instance of this object for its context menu.
		//     Some of the properties of SceneCMenu forward through to View3dCMenu.
		//
		// Notes:
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

		/// <summary>Selection box</summary>
		bool SelectionBoxVisible { get; set; }
		ICommand ToggleSelectionBox { get; }

		/// <summary>Camera Reset/Auto range</summary>
		View3d.ESceneBounds AutoRangeBounds { get; set; }
		ICommand AutoRange { get; }

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

		/// <summary>Bounding boxes</summary>
		bool BBoxesVisible { get; set; }
		ICommand ToggleBBoxesVisible { get; }

		/// <summary>The length of displayed vertex normals</summary>
		float NormalsLength { get; set; }

		/// <summary>The colour of displayed vertex normals</summary>
		Colour32 NormalsColour { get; set; }
		ICommand SetNormalsColour { get; }

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
