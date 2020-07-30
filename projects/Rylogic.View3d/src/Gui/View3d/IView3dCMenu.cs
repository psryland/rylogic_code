﻿using System.ComponentModel;
using System.Windows.Input;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public interface IView3dCMenu :INotifyPropertyChanged
	{
		// Architecture:
		//   Rylogic.Gui.WPF.Resources.ContextMenus - (Rylogic.Gui.WPF\src\Resources\ContextMenus.xaml)
		//     contains the XAML for View3d and Chart menu items. These menu items are used in the context
		//     menus for View3dControl and ChartControl. They are implemented in terms of the interfaces;
		//     IView3dCMenu and IChartCMenu (named accordingly). Note that there can be a View3d and a Chart
		//     version of a context menu item.
		//
		//   IView3dCMenu - is the binding interface for context menu items common to View3d.
		//   IChartCMenu - is the binding interface for context menu items related to Charts.
		//   These interfaces declare the properties and commands referenced by the ContextMenu XAML.
		//      View3dControl and ChartControl implement these interfaces, but for customisation, an app
		//      can provide different implementation and set it as the ContextMenu DataContext.
		//   
		//   View3dMenuItem - is based on the IView3dCMenu interface. These menu items typically just
		//      change the state of the view3d control.
		//   ChartMenuItem - is based on the IChartCMenu interface. These menu items typically modify
		//      the chart options which then cause the chart to change state.
		//
		//   View3dControl and ChartControl do not set a context menu by default. To add the default one
		//      use the 'View3dCMenu' or 'ChartCMenu' static resources. Or, create your own context menu.
		//
		// Usage:
		//   - Import the resource dictionary in App.xaml for 'gui:ContextMenus.Instance' using:
		//        <ResourceDictionary.MergedDictionaries>
		//            <x:Static Member = "gui:ContextMenus.Instance" />
		//        </ResourceDictionary.MergedDictionaries>
		//    - Create a ContextMenu StaticResource using individual menu items
		//    - Set it as the ContextMenu on a View3dControl or ChartControl instance.
		//
		// Notes:
		//  - If adding a new context menu option, you'll need to do:
		//       - Add a MenuItem resource to 'ContextMenus.xaml' (Rylogic.Gui.WPF\src\Resources\ContextMenus.xaml)
		//       - Add any binding variables to 'IView3dCMenu' or 'IChartCMenu'
		//       - Implement the functionality in View3dControl or ChartControl, and any types implementing
		//         IView3dCMenu or IChartCMenu.
		//
		//  - This is mainly just to ensure the required binding functions exist. DataTemplate cannot
		//    bind to interfaces so you have to implement this interface with implicit implementations,
		//    not explicit (e.g. bool IView3dCMenu.OriginPointVisible => ... won't work ).

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

		/// <summary>Camera Reset/Auto range</summary>
		View3d.ESceneBounds AutoRangeBounds { get; set; }
		ICommand AutoRangeView { get; }

		/// <summary>Camera orthographic</summary>
		bool Orthographic { get; set; }
		ICommand ToggleOrthographic { get; }

		/// <summary>Directions to align the camera up-axis to</summary>
		EAlignDirection AlignDirection { get; set; }

		/// <summary>Preset views to set the camera to</summary>
		EViewPreset ViewPreset { get; set; }

		/// <summary>Saved views</summary>
		ICollectionView SavedViews { get; }
		
		/// <summary>Apply a saved view to the camera</summary>
		ICommand ApplySavedView { get; }

		/// <summary>Save the current view</summary>
		ICommand SaveCurrentView { get; }

		/// <summary>Remove the current saved view from the collection of saved views</summary>
		ICommand RemoveSavedView { get; }

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

		/// <summary>The length of displayed vertex normals</summary>
		float NormalsLength { get; set; }

		/// <summary>The colour of displayed vertex normals</summary>
		Colour32 NormalsColour { get; set; }
		ICommand SetNormalsColour { get; }

		/// <summary>The size of 'Points' fill mode points</summary>
		float FillModePointsSize { get; set; }

		/// <summary>Show the animation controls</summary>
		ICommand ShowAnimationUI { get; }

		/// <summary>Show lighting properties</summary>
		ICommand ShowLightingUI { get; }

		/// <summary>Show the measurement tool</summary>
		ICommand ShowMeasureToolUI { get; }

		/// <summary>Show object manager</summary>
		ICommand ShowObjectManagerUI { get; }

		/// <summary>Allow property changed to be triggered externally</summary>
		void NotifyPropertyChanged(string prop_name);
	}
}
